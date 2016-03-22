using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ProcessInvoker))]
    public interface IProcessInvoker : IDisposable, IAgentService
    {
        event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        event EventHandler<DataReceivedEventArgs> ErrorDataReceived;

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            CancellationToken cancellationToken);
    }

    public sealed class ProcessInvoker : AgentService, IProcessInvoker
    {
        //TraceInterval defines how long between printing trace messages,
        //while waiting for the process to exit
        private static readonly TimeSpan TraceInterval = TimeSpan.FromSeconds(30);

        private Process _proc;
        private SemaphoreSlim _processExitedSignal = new SemaphoreSlim(0, 1);
        private Stopwatch _stopWatch;

        public event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<DataReceivedEventArgs> ErrorDataReceived;

        private async Task<int> WaitForExit(CancellationToken cancellationToken)
        {
            // Wait for the cancellation token to be set or the process to exit.
            try
            {
                while (!cancellationToken.IsCancellationRequested && !_proc.HasExited)
                {
                    await _processExitedSignal.WaitAsync(TraceInterval, cancellationToken);
                    if (!_proc.HasExited)
                    {
                        Trace.Verbose($"Waiting on process {_proc.Id} ({_stopWatch.Elapsed} elapsed)");
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                if (cancellationToken.IsCancellationRequested && !_proc.HasExited)
                {
                    _proc.Kill();
                }
            }
            // Wait for process to exit without hard timeout, which will 
            // ensure that we've read everything from the stdout and stderr.
            _proc.WaitForExit();

            Trace.Info(
                "Finished process with file name '{0}', arguments '{1}', exit code {2}, and elapsed time {3}.",
                _proc.StartInfo.FileName,
                _proc.StartInfo.Arguments,
                _proc.ExitCode,
                _stopWatch.Elapsed);
            return _proc.ExitCode;
        }

        private void Execute(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment)
        {
            ArgUtil.Null(_proc, nameof(_proc));
            ArgUtil.NotNullOrEmpty(fileName, nameof(fileName));
            Trace.Info("Starting process with file name '{0}', arguments '{1}', and working directory '{2}'.", fileName, arguments, workingDirectory);

            // Setup the start info.
            _proc = new Process();
            _proc.StartInfo.FileName = fileName;
            _proc.StartInfo.Arguments = arguments;
            _proc.StartInfo.WorkingDirectory = workingDirectory;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.RedirectStandardInput = false;
            _proc.StartInfo.RedirectStandardError = ErrorDataReceived != null;
            _proc.StartInfo.RedirectStandardOutput = OutputDataReceived != null;
            _proc.StartInfo.CreateNoWindow = true;

            // Copy the environment variables.
            if (environment != null && environment.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in environment)
                {
                    _proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            // Hook up the events.
            _proc.EnableRaisingEvents = true;
            _proc.Exited += OnExited;
            if (_proc.StartInfo.RedirectStandardOutput)
            {
                _proc.OutputDataReceived += OnOutputDataReceived;
            }

            if (_proc.StartInfo.RedirectStandardError)
            {
                _proc.ErrorDataReceived += OnErrorDataReceived;
            }

            // Start the process.
            _stopWatch = Stopwatch.StartNew();
            bool newProcessStarted = _proc.Start();
            if (!newProcessStarted)
            {
                Trace.Verbose("Used existing process instead of starting new one for " + fileName);
            }

            // Start reading output.
            if (_proc.StartInfo.RedirectStandardOutput)
            {
                _proc.BeginOutputReadLine();
            }

            if (_proc.StartInfo.RedirectStandardError)
            {
                _proc.BeginErrorReadLine();
            }
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            CancellationToken cancellationToken)
        {
            Execute(workingDirectory, fileName, arguments, environment);
            return WaitForExit(cancellationToken);
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
                _processExitedSignal.Dispose();
                if (_proc != null)
                {
                    _proc.Exited -= OnExited;
                    _proc.ErrorDataReceived -= OnErrorDataReceived;
                    _proc.OutputDataReceived -= OnOutputDataReceived;
                    _proc.Dispose();
                    _proc = null;
                }
            }
        }

        private void OnExited(object sender, EventArgs e)
        {
            _stopWatch.Stop();
            _processExitedSignal.Release();
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            // at the end of the process, the event fires one last time with null
            if (e.Data != null)
            {
                ErrorDataReceived?.Invoke(sender, e);
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // at the end of the process, the event fires one last time with null
            if (e.Data != null)
            {
                OutputDataReceived?.Invoke(sender, e);
            }
        }
    }
}
