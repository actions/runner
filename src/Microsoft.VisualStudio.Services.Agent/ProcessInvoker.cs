using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ProcessInvoker))]
    public interface IProcessInvoker : IDisposable, IAgentService
    {
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            CancellationToken cancellationToken);
    }

    // The implementation of the process invoker does not hock up DataReceivedEvent and ErrorReceivedEvent of Process,
    // instead, we read both STDOUT and STDERR stream manually on seperate thread. 
    // The reason is we find a huge perf issue about process STDOUT/STDERR with those events. 
    // 
    // Missing functionalities:
    //       1. Cancel/Kill process tree
    //       2. Make sure STDOUT and STDERR not process out of order 
    public sealed class ProcessInvoker : AgentService, IProcessInvoker
    {
        private Process _proc;
        private Stopwatch _stopWatch;
        private int _asyncStreamReaderCount = 0;
        private bool _waitingOnStreams = false;
        private readonly AsyncManualResetEvent _outputProcessEvent = new AsyncManualResetEvent();
        private readonly TaskCompletionSource<bool> _processExitedCompletionSource = new TaskCompletionSource<bool>();
        private readonly ConcurrentQueue<string> _errorData = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _outputData = new ConcurrentQueue<string>();

        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: false,
                cancellationToken: cancellationToken);
        }

        public async Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            CancellationToken cancellationToken)
        {
            ArgUtil.Null(_proc, nameof(_proc));
            ArgUtil.NotNullOrEmpty(fileName, nameof(fileName));

            Trace.Info($"Starting process with file name '{fileName}', arguments '{arguments}', and working directory '{workingDirectory}'.");
            _proc = new Process();
            _proc.StartInfo.FileName = fileName;
            _proc.StartInfo.Arguments = arguments;
            _proc.StartInfo.WorkingDirectory = workingDirectory;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.CreateNoWindow = true;
            _proc.StartInfo.RedirectStandardInput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.StartInfo.RedirectStandardOutput = true;

            // Copy the environment variables.
            if (environment != null && environment.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in environment)
                {
                    _proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            // Set the TF_BUILD env variable.
            _proc.StartInfo.Environment[Constants.TFBuild] = "True";

            // Hook up the events.
            _proc.EnableRaisingEvents = true;
            _proc.Exited += ProcessExitedHandler;

            // Start the process.
            _stopWatch = Stopwatch.StartNew();
            _proc.Start();

            // Close the input stream. This is done to prevent commands from blocking the build waiting for input from the user.
            if (_proc.StartInfo.RedirectStandardInput)
            {
                _proc.StandardInput.Dispose();
            }

            // Start the standard error notifications, if appropriate.
            if (_proc.StartInfo.RedirectStandardError)
            {
                StartReadStream(_proc.StandardError, _errorData);
            }

            // Start the standard output notifications, if appropriate.
            if (_proc.StartInfo.RedirectStandardOutput)
            {
                StartReadStream(_proc.StandardOutput, _outputData);
            }

            using (var registration = cancellationToken.Register(async () => await CancelAndKillProcessTree()))
            {
                Trace.Info($"Process started with process id {_proc.Id}, waiting for process exit.");
                while (true)
                {
                    Task outputSignal = _outputProcessEvent.WaitAsync();
                    var signaled = await Task.WhenAny(outputSignal, _processExitedCompletionSource.Task);

                    if (signaled == outputSignal)
                    {
                        ProcessOutput();
                    }
                    else
                    {
                        _stopWatch.Stop();
                        break;
                    }
                }

                // Just in case there was some pending output when the process shut down go ahead and check the
                // data buffers one last time before returning
                ProcessOutput();

                Trace.Info($"Finished process with exit code {_proc.ExitCode}, and elapsed time {_stopWatch.Elapsed}.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Wait for process to finish.
            if (_proc.ExitCode != 0 && requireExitCodeZero)
            {
                throw new ProcessExitCodeException(exitCode: _proc.ExitCode, fileName: fileName, arguments: arguments);
            }

            return _proc.ExitCode;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_proc != null)
                {
                    _proc.Dispose();
                    _proc = null;
                }
            }
        }

        private void ProcessOutput()
        {
            List<string> errorData = new List<string>();
            List<string> outputData = new List<string>();

            string errorLine;
            while (_errorData.TryDequeue(out errorLine))
            {
                errorData.Add(errorLine);
            }

            string outputLine;
            while (_outputData.TryDequeue(out outputLine))
            {
                outputData.Add(outputLine);
            }

            _outputProcessEvent.Reset();

            // Write the error lines.
            if (errorData != null && this.ErrorDataReceived != null)
            {
                foreach (string line in errorData)
                {
                    if (line != null)
                    {
                        this.ErrorDataReceived(this, new ProcessDataReceivedEventArgs(line));
                    }
                }
            }

            // Process the output lines.
            if (outputData != null && this.OutputDataReceived != null)
            {
                foreach (string line in outputData)
                {
                    if (line != null)
                    {
                        // The line is output from the process that was invoked.
                        this.OutputDataReceived(this, new ProcessDataReceivedEventArgs(line));
                    }
                }
            }
        }

        private async Task CancelAndKillProcessTree()
        {
            ArgUtil.NotNull(_proc, nameof(_proc));
#if OS_WINDOWS
            await WindowsCancelAndKillProcessTree();
#else
            await NixCancelAndKillProcessTree();
#endif
        }

        private void ProcessExitedHandler(object sender, EventArgs e)
        {
            if ((_proc.StartInfo.RedirectStandardError || _proc.StartInfo.RedirectStandardOutput) && _asyncStreamReaderCount != 0)
            {
                _waitingOnStreams = true;

                Task.Run(async () =>
                {
                    // Wait 5 seconds and then Cancel/Kill process tree
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    KillProcessTree();
                    _processExitedCompletionSource.TrySetResult(true);
                });
            }
            else
            {
                _processExitedCompletionSource.TrySetResult(true);
            }
        }

        private void StartReadStream(StreamReader reader, ConcurrentQueue<string> dataBuffer)
        {
            Interlocked.Increment(ref _asyncStreamReaderCount);
            Task.Run(() =>
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line != null)
                    {
                        dataBuffer.Enqueue(line);
                        _outputProcessEvent.Set();
                    }
                }

                if (Interlocked.Decrement(ref _asyncStreamReaderCount) == 0 && _waitingOnStreams)
                {
                    _processExitedCompletionSource.TrySetResult(true);
                }
            });
        }

        private void KillProcessTree()
        {
#if OS_WINDOWS
            WindowsKillProcessTree();
#else
            NixKillProcessTree();
#endif
        }

#if OS_WINDOWS
        private async Task WindowsCancelAndKillProcessTree()
        {
            bool ctrl_c_succeed = await SendCtrlSignal(ConsoleCtrlEvent.CTRL_C, TimeSpan.FromSeconds(10));
            if (ctrl_c_succeed)
            {
                Trace.Info("Process cancelled successfully through Ctrl+C.");
                return;
            }

            bool ctrl_break_succeed = await SendCtrlSignal(ConsoleCtrlEvent.CTRL_BREAK, TimeSpan.FromSeconds(5));
            if (ctrl_break_succeed)
            {
                Trace.Info("Process terminate successfully through Ctrl+Break.");
                return;
            }

            Trace.Info("Kill entire process tree since both cancel and terminate signal has been ignored by the target process.");
            KillProcessTree();
        }

        private async Task<bool> SendCtrlSignal(ConsoleCtrlEvent signal, TimeSpan timeout)
        {
            Trace.Info($"Sending {signal} to process {_proc.Id}.");
            ConsoleCtrlDelegate ctrlEventHandler = new ConsoleCtrlDelegate(ConsoleCtrlHandler);
            try
            {
                if (!FreeConsole())
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!AttachConsole(_proc.Id))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!SetConsoleCtrlHandler(ctrlEventHandler, true))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!GenerateConsoleCtrlEvent(signal, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Trace.Info($"Successfully send {signal} to process {_proc.Id}.");
                Trace.Info($"Waiting for process exit or {timeout.TotalSeconds} seconds after {signal} signal fired.");
                var completedTask = await Task.WhenAny(Task.Delay(timeout), _processExitedCompletionSource.Task);
                if (completedTask == _processExitedCompletionSource.Task)
                {
                    Trace.Info("Process exit successfully.");
                    return true;
                }
                else
                {
                    Trace.Info($"Process did not honor {signal} signal within {timeout.TotalSeconds} seconds.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.Info($"{signal} signal doesn't fire successfully.");
                Trace.Error($"Catch exception during send {signal} event to process {_proc.Id}");
                Trace.Error(ex);
                return false;
            }
            finally
            {
                FreeConsole();
                SetConsoleCtrlHandler(ctrlEventHandler, false);
            }
        }

        private bool ConsoleCtrlHandler(ConsoleCtrlEvent ctrlType)
        {
            switch (ctrlType)
            {
                case ConsoleCtrlEvent.CTRL_C:
                    Trace.Info($"Ignore Ctrl+C to current process.");
                    // We return True, so the default Ctrl handler will not take action.
                    return true;
                case ConsoleCtrlEvent.CTRL_BREAK:
                    Trace.Info($"Ignore Ctrl+Break to current process.");
                    // We return True, so the default Ctrl handler will not take action.
                    return true;
            }

            // If the function handles the control signal, it should return TRUE. 
            // If it returns FALSE, the next handler function in the list of handlers for this process is used.
            return false;
        }

        private void WindowsKillProcessTree()
        {
            Dictionary<int, int> processRelationship = new Dictionary<int, int>();
            Trace.Info($"Scean all processes to find relationship between all processes.");
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    if (!proc.SafeHandle.IsInvalid)
                    {
                        PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                        int returnLength = 0;
                        int queryResult = NtQueryInformationProcess(proc.SafeHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation, ref pbi, pbi.Size, ref returnLength);
                        if (queryResult == 0) // == 0 is OK
                        {
                            Trace.Verbose($"Process: {proc.Id} is child process of {pbi.InheritedFromUniqueProcessId}.");
                            processRelationship[proc.Id] = (int)pbi.InheritedFromUniqueProcessId;
                        }
                        else
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ignore all exceptions, since KillProcessTree is best effort.
                    Trace.Verbose("Ignore any catched exception during detecting process relationship.");
                    Trace.Verbose(ex.ToString());
                }
            }

            Trace.Verbose($"Start killing process tree of process '{_proc.Id}'.");
            Stack<ProcessTerminationInfo> processesNeedtoKill = new Stack<ProcessTerminationInfo>();
            processesNeedtoKill.Push(new ProcessTerminationInfo(_proc.Id, false));
            while (processesNeedtoKill.Count() > 0)
            {
                ProcessTerminationInfo procInfo = processesNeedtoKill.Pop();
                List<int> childProcessesIds = new List<int>();
                if (!procInfo.ChildPidExpanded)
                {
                    Trace.Info($"Find all child processes of process '{procInfo.Pid}'.");
                    childProcessesIds = processRelationship.Where(p => p.Value == procInfo.Pid).Select(k => k.Key).ToList();
                }

                if (childProcessesIds.Count > 0)
                {
                    Trace.Info($"Need kill all child processes trees before kill process '{procInfo.Pid}'.");
                    processesNeedtoKill.Push(new ProcessTerminationInfo(procInfo.Pid, true));
                    foreach (var childPid in childProcessesIds)
                    {
                        Trace.Info($"Child process '{childPid}' needs be killed first.");
                        processesNeedtoKill.Push(new ProcessTerminationInfo(childPid, false));
                    }
                }
                else
                {
                    Trace.Info($"Kill process '{procInfo.Pid}'.");
                    try
                    {
                        Process leafProcess = Process.GetProcessById(procInfo.Pid);
                        try
                        {
                            leafProcess.Kill();
                        }
                        catch (InvalidOperationException ex)
                        {
                            // The process has already exited
                            Trace.Error("Ignore InvalidOperationException during Process.Kill().");
                            Trace.Error(ex);
                        }
                        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
                        {
                            // The associated process could not be terminated
                            // The process is terminating
                            // NativeErrorCode 5 means Access Denied
                            Trace.Error("Ignore Win32Exception with NativeErrorCode 5 during Process.Kill().");
                            Trace.Error(ex);
                        }
                        catch (Exception ex)
                        {
                            // Ignore any additional exception
                            Trace.Error("Ignore additional exceptions during Process.Kill().");
                            Trace.Error(ex);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        // process already gone, nothing needs killed.
                        Trace.Error("Ignore ArgumentException during Process.GetProcessById().");
                        Trace.Error(ex);
                    }
                    catch (Exception ex)
                    {
                        // Ignore any additional exception
                        Trace.Error("Ignore additional exceptions during Process.GetProcessById().");
                        Trace.Error(ex);
                    }
                }
            }
        }

        private class ProcessTerminationInfo
        {
            public ProcessTerminationInfo(int pid, bool expanded)
            {
                Pid = pid;
                ChildPidExpanded = expanded;
            }

            public int Pid { get; }
            public bool ChildPidExpanded { get; }
        }

        private enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1
        }

        private enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public long ExitStatus;
            public long PebBaseAddress;
            public long AffinityMask;
            public long BasePriority;
            public long UniqueProcessId;
            public long InheritedFromUniqueProcessId;

            public uint Size
            {
                get { return (6 * sizeof(long)); }
            }
        };

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, uint processInformationLength, ref int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        // Delegate type to be used as the Handler Routine for SetConsoleCtrlHandler
        private delegate Boolean ConsoleCtrlDelegate(ConsoleCtrlEvent CtrlType);
#else
        private async Task NixCancelAndKillProcessTree()
        {
            // TODO: replace Task.Delay(1) with Send SIGINT/SIGTERM/SIGKILL
            await Task.Delay(1);
            KillProcessTree();
        }

        private void NixKillProcessTree()
        {
            try
            {
                // TODO: Send Ctrl+C/Break to process group.
                if (!_proc.HasExited)
                {
                    _proc.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // InvalidOperationException can occur if process got terminated by itself between 
                // HasExited and Kill() calls above.
            }
        }
#endif        

    }

    public sealed class ProcessExitCodeException : Exception
    {
        public int ExitCode { get; private set; }

        public ProcessExitCodeException(int exitCode, string fileName, string arguments)
            : base(StringUtil.Loc("ProcessExitCode", exitCode, fileName, arguments))
        {
            ExitCode = exitCode;
        }
    }

    public class ProcessDataReceivedEventArgs : EventArgs
    {
        public ProcessDataReceivedEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; private set; }
    }
}
