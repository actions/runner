using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

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

            using (var registration = cancellationToken.Register(() => CancelProcessTree()))
            {
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

        private void CancelProcessTree()
        {
            ArgUtil.NotNull(_proc, nameof(_proc));

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

        private void ProcessExitedHandler(object sender, EventArgs e)
        {
            if ((_proc.StartInfo.RedirectStandardError || _proc.StartInfo.RedirectStandardOutput) && _asyncStreamReaderCount != 0)
            {
                _waitingOnStreams = true;

                Task.Run(async () =>
                {
                    // Wait 5 seconds and then Cancel/Kill process tree
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    CancelProcessTree();
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
