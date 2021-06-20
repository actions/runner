using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Sdk
{

    // The implementation of the process invoker does not hook up DataReceivedEvent and ErrorReceivedEvent of Process,
    // instead, we read both STDOUT and STDERR stream manually on separate thread. 
    // The reason is we find a huge perf issue about process STDOUT/STDERR with those events. 
    public sealed class ProcessInvoker : IDisposable
    {
        private Process _proc;
        private Stopwatch _stopWatch;
        private int _asyncStreamReaderCount = 0;
        private bool _waitingOnStreams = false;
        private readonly AsyncManualResetEvent _outputProcessEvent = new AsyncManualResetEvent();
        private readonly TaskCompletionSource<bool> _processExitedCompletionSource = new TaskCompletionSource<bool>();
        private readonly CancellationTokenSource _processStandardInWriteCancellationTokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<string> _errorData = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _outputData = new ConcurrentQueue<string>();
        private readonly TimeSpan _sigintTimeout = TimeSpan.FromMilliseconds(7500);
        private readonly TimeSpan _sigtermTimeout = TimeSpan.FromMilliseconds(2500);
        private ITraceWriter Trace { get; set; }

        private class AsyncManualResetEvent
        {
            private volatile TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

            public Task WaitAsync() { return m_tcs.Task; }

            public void Set()
            {
                var tcs = m_tcs;
                Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
                    tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
                tcs.Task.Wait();
            }

            public void Reset()
            {
                while (true)
                {
                    var tcs = m_tcs;
                    if (!tcs.Task.IsCompleted ||
                        Interlocked.CompareExchange(ref m_tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                        return;
                }
            }
        }

        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public ProcessInvoker(ITraceWriter trace)
        {
            this.Trace = trace;
        }

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

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: null,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: false,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: killProcessOnCancel,
                redirectStandardIn: null,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: killProcessOnCancel,
                redirectStandardIn: redirectStandardIn,
                inheritConsoleHandler: false,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: killProcessOnCancel,
                redirectStandardIn: redirectStandardIn,
                inheritConsoleHandler: inheritConsoleHandler,
                keepStandardInOpen: false,
                highPriorityProcess: false,
                cancellationToken: cancellationToken);
        }

        public async Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            bool keepStandardInOpen,
            bool highPriorityProcess,
            CancellationToken cancellationToken)
        {
            ArgUtil.Null(_proc, nameof(_proc));
            ArgUtil.NotNullOrEmpty(fileName, nameof(fileName));

            Trace.Info("Starting process:");
            Trace.Info($"  File name: '{fileName}'");
            Trace.Info($"  Arguments: '{arguments}'");
            Trace.Info($"  Working directory: '{workingDirectory}'");
            Trace.Info($"  Require exit code zero: '{requireExitCodeZero}'");
            Trace.Info($"  Encoding web name: {outputEncoding?.WebName} ; code page: '{outputEncoding?.CodePage}'");
            Trace.Info($"  Force kill process on cancellation: '{killProcessOnCancel}'");
            Trace.Info($"  Redirected STDIN: '{redirectStandardIn != null}'");
            Trace.Info($"  Persist current code page: '{inheritConsoleHandler}'");
            Trace.Info($"  Keep redirected STDIN open: '{keepStandardInOpen}'");
            Trace.Info($"  High priority process: '{highPriorityProcess}'");

            _proc = new Process();
            _proc.StartInfo.FileName = fileName;
            _proc.StartInfo.Arguments = arguments;
            _proc.StartInfo.WorkingDirectory = workingDirectory;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.CreateNoWindow = !inheritConsoleHandler;
            _proc.StartInfo.RedirectStandardInput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.StartInfo.RedirectStandardOutput = true;

            // Ensure we process STDERR even the process exit event happen before we start read STDERR stream. 
            if (_proc.StartInfo.RedirectStandardError)
            {
                Interlocked.Increment(ref _asyncStreamReaderCount);
            }

            // Ensure we process STDOUT even the process exit event happen before we start read STDOUT stream.
            if (_proc.StartInfo.RedirectStandardOutput)
            {
                Interlocked.Increment(ref _asyncStreamReaderCount);
            }

#if OS_WINDOWS
            // If StandardErrorEncoding or StandardOutputEncoding is not specified the on the
            // ProcessStartInfo object, then .NET PInvokes to resolve the default console output
            // code page:
            //      [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
            //      public extern static uint GetConsoleOutputCP();
            StringUtil.EnsureRegisterEncodings();
#endif
            if (outputEncoding != null)
            {
                _proc.StartInfo.StandardErrorEncoding = outputEncoding;
                _proc.StartInfo.StandardOutputEncoding = outputEncoding;
            }

            // Copy the environment variables.
            if (environment != null && environment.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in environment)
                {
                    _proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            // Indicate GitHub Actions process.
            _proc.StartInfo.Environment["GITHUB_ACTIONS"] = "true";

            // Set CI=true when no one else already set it.
            // CI=true is common set in most CI provider in GitHub
            if (!_proc.StartInfo.Environment.ContainsKey("CI") &&
                Environment.GetEnvironmentVariable("CI") == null)
            {
                _proc.StartInfo.Environment["CI"] = "true";
            }

            // Hook up the events.
            _proc.EnableRaisingEvents = true;
            _proc.Exited += ProcessExitedHandler;

            // Start the process.
            _stopWatch = Stopwatch.StartNew();
            _proc.Start();

            // Decrease invoked process priority, in platform specifc way, relative to parent
            if (!highPriorityProcess)
            {
                DecreaseProcessPriority(_proc);
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

            if (_proc.StartInfo.RedirectStandardInput)
            {
                if (redirectStandardIn != null)
                {
                    StartWriteStream(redirectStandardIn, _proc.StandardInput, keepStandardInOpen);
                }
                else
                {
                    // Close the input stream. This is done to prevent commands from blocking the build waiting for input from the user.
                    _proc.StandardInput.Close();
                }
            }

            var cancellationFinished = new TaskCompletionSource<bool>();
            using (var registration = cancellationToken.Register(async () =>
            {
                await CancelAndKillProcessTree(killProcessOnCancel);
                cancellationFinished.TrySetResult(true);
            }))
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

                if (cancellationToken.IsCancellationRequested)
                {
                    // Ensure cancellation also finish on the cancellationToken.Register thread.
                    await cancellationFinished.Task;
                    Trace.Info($"Process Cancellation finished.");
                }

                Trace.Info($"Finished process {_proc.Id} with exit code {_proc.ExitCode}, and elapsed time {_stopWatch.Elapsed}.");
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

        private async Task CancelAndKillProcessTree(bool killProcessOnCancel)
        {
            ArgUtil.NotNull(_proc, nameof(_proc));
            if (!killProcessOnCancel)
            {
                bool sigint_succeed = await SendSIGINT(_sigintTimeout);
                if (sigint_succeed)
                {
                    Trace.Info("Process cancelled successfully through Ctrl+C/SIGINT.");
                    return;
                }

                bool sigterm_succeed = await SendSIGTERM(_sigtermTimeout);
                if (sigterm_succeed)
                {
                    Trace.Info("Process terminate successfully through Ctrl+Break/SIGTERM.");
                    return;
                }
            }

            Trace.Info("Kill entire process tree since both cancel and terminate signal has been ignored by the target process.");
            KillProcessTree();
        }

        private async Task<bool> SendSIGINT(TimeSpan timeout)
        {
#if OS_WINDOWS
            return await SendCtrlSignal(ConsoleCtrlEvent.CTRL_C, timeout);
#else
            return await SendSignal(Signals.SIGINT, timeout);
#endif
        }

        private async Task<bool> SendSIGTERM(TimeSpan timeout)
        {
#if OS_WINDOWS
            return await SendCtrlSignal(ConsoleCtrlEvent.CTRL_BREAK, timeout);
#else
            return await SendSignal(Signals.SIGTERM, timeout);
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
                    _processStandardInWriteCancellationTokenSource.Cancel();
                });
            }
            else
            {
                _processExitedCompletionSource.TrySetResult(true);
                _processStandardInWriteCancellationTokenSource.Cancel();
            }
        }

        private void StartReadStream(StreamReader reader, ConcurrentQueue<string> dataBuffer)
        {
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

                Trace.Info("STDOUT/STDERR stream read finished.");

                if (Interlocked.Decrement(ref _asyncStreamReaderCount) == 0 && _waitingOnStreams)
                {
                    _processExitedCompletionSource.TrySetResult(true);
                    _processStandardInWriteCancellationTokenSource.Cancel();
                }
            });
        }

        private void StartWriteStream(Channel<string> redirectStandardIn, StreamWriter standardIn, bool keepStandardInOpen)
        {
            Task.Run(async () =>
            {
                // Write the contents as UTF8 to handle all characters.
                var utf8Writer = new StreamWriter(standardIn.BaseStream, new UTF8Encoding(false));

                while (!_processExitedCompletionSource.Task.IsCompleted)
                {
                    ValueTask<string> dequeueTask = redirectStandardIn.Reader.ReadAsync(_processStandardInWriteCancellationTokenSource.Token);
                    string input = await dequeueTask;
                    if (input != null)
                    {
                        utf8Writer.WriteLine(input);
                        utf8Writer.Flush();

                        if (!keepStandardInOpen)
                        {
                            Trace.Info("Close STDIN after the first redirect finished.");
                            standardIn.Close();
                            break;
                        }
                    }
                }

                Trace.Info("STDIN stream write finished.");
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

        private void DecreaseProcessPriority(Process process)
        {
#if OS_LINUX
            int oomScoreAdj = 500;
            string userOomScoreAdj;
            if (process.StartInfo.Environment.TryGetValue("PIPELINE_JOB_OOMSCOREADJ", out userOomScoreAdj))
            {
                int userOomScoreAdjParsed;
                if (int.TryParse(userOomScoreAdj, out userOomScoreAdjParsed) && userOomScoreAdjParsed >= -1000 && userOomScoreAdjParsed <= 1000)
                {
                    oomScoreAdj = userOomScoreAdjParsed;
                }
                else
                {
                    Trace.Info($"Invalid PIPELINE_JOB_OOMSCOREADJ ({userOomScoreAdj}). Valid range is -1000:1000. Using default 500.");
                }
            }
            // Values (up to 1000) make the process more likely to be killed under OOM scenario,
            // protecting the agent by extension. Default of 500 is likely to get killed, but can
            // be adjusted up or down as appropriate.
            WriteProcessOomScoreAdj(process.Id, oomScoreAdj);
#endif
        }

#if OS_WINDOWS
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
                Trace.Verbose($"Catch exception during send {signal} event to process {_proc.Id}");
                Trace.Verbose(ex.ToString());
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
            var pid = _proc?.Id;
            if (pid == null)
            {
                // process already exit, stop here.
                return;
            }

            Dictionary<int, int> processRelationship = new Dictionary<int, int>();
            Trace.Info($"Scan all processes to find relationship between all processes.");
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    if (!proc.SafeHandle.IsInvalid)
                    {
                        PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                        int returnLength = 0;
                        int queryResult = NtQueryInformationProcess(proc.SafeHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), ref returnLength);
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

            Trace.Verbose($"Start killing process tree of process '{pid.Value}'.");
            Stack<ProcessTerminationInfo> processesNeedtoKill = new Stack<ProcessTerminationInfo>();
            processesNeedtoKill.Push(new ProcessTerminationInfo(pid.Value, false));
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
                            Trace.Verbose("Ignore InvalidOperationException during Process.Kill().");
                            Trace.Verbose(ex.ToString());
                        }
                        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
                        {
                            // The associated process could not be terminated
                            // The process is terminating
                            // NativeErrorCode 5 means Access Denied
                            Trace.Verbose("Ignore Win32Exception with NativeErrorCode 5 during Process.Kill().");
                            Trace.Verbose(ex.ToString());
                        }
                        catch (Exception ex)
                        {
                            // Ignore any additional exception
                            Trace.Verbose("Ignore additional exceptions during Process.Kill().");
                            Trace.Verbose(ex.ToString());
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        // process already gone, nothing needs killed.
                        Trace.Verbose("Ignore ArgumentException during Process.GetProcessById().");
                        Trace.Verbose(ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        // Ignore any additional exception
                        Trace.Verbose("Ignore additional exceptions during Process.GetProcessById().");
                        Trace.Verbose(ex.ToString());
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
        };


        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, ref int returnLength);

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
        private async Task<bool> SendSignal(Signals signal, TimeSpan timeout)
        {
            Trace.Info($"Sending {signal} to process {_proc.Id}.");
            int errorCode = kill(_proc.Id, (int)signal);
            if (errorCode != 0)
            {
                Trace.Info($"{signal} signal doesn't fire successfully.");
                Trace.Info($"Error code: {errorCode}.");
                return false;
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

        private void NixKillProcessTree()
        {
            try
            {
                if (_proc?.HasExited == false)
                {
                    _proc?.Kill();
                }
            }
            catch (InvalidOperationException ex)
            {
                Trace.Info("Ignore InvalidOperationException during Process.Kill().");
                Trace.Info(ex.ToString());
            }
        }

#if OS_LINUX
        private void WriteProcessOomScoreAdj(int processId, int oomScoreAdj)
        {
            try
            {
                string procFilePath = $"/proc/{processId}/oom_score_adj";
                if (File.Exists(procFilePath))
                {
                    File.WriteAllText(procFilePath, oomScoreAdj.ToString());
                    Trace.Info($"Updated oom_score_adj to {oomScoreAdj} for PID: {processId}.");
                }
            }
            catch (Exception ex)
            {
                Trace.Info($"Failed to update oom_score_adj for PID: {processId}.");
                Trace.Info(ex.ToString());
            }
        }
#endif

        private enum Signals : int
        {
            SIGINT = 2,
            SIGTERM = 15
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);
#endif
    }

    public sealed class ProcessExitCodeException : Exception
    {
        public int ExitCode { get; private set; }

        public ProcessExitCodeException(int exitCode, string fileName, string arguments)
            : base($"Exit code {exitCode} returned from process: file name '{fileName}', arguments '{arguments}'.")
        {
            ExitCode = exitCode;
        }
    }

    public sealed class ProcessDataReceivedEventArgs : EventArgs
    {
        public ProcessDataReceivedEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}
