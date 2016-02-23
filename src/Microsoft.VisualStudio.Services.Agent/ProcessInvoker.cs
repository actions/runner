using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class ProcessDataReceivedEventArgs : EventArgs
    {
        public ProcessDataReceivedEventArgs(String data)
        {
            Data = data;
        }

        public String Data { get; private set; }
    }

    [ServiceLocator(Default = typeof(ProcessInvoker))]
    public interface IProcessInvoker : IDisposable, IAgentService
    {
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;

        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        event EventHandler Exited;

        void Execute(String workingFolder, String filename, String arguments, IDictionary<String, String> environmentVariables);

        Task<int> WaitForExit();
    }
    
    public class ProcessInvoker : AgentService, IProcessInvoker
    {
        private Process _proc;
        private SemaphoreSlim _processExitedSignal = new SemaphoreSlim(0, 1);
        private Stopwatch _stopWatch;

        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public event EventHandler Exited;

        public async Task<int> WaitForExit()
        {            
            while ((!HostContext.CancellationToken.IsCancellationRequested) && (!_proc.HasExited))
            {
                await _processExitedSignal.WaitAsync(TimeSpan.FromSeconds(30), HostContext.CancellationToken);
                if (!_proc.HasExited)
                {
                    Trace.Info(
                        "Waiting on process {0} ({1} seconds elapsed)",
                            _proc.Id,
                            _stopWatch.Elapsed.TotalSeconds);
                }
            }
            HostContext.CancellationToken.ThrowIfCancellationRequested();
            // Wait for process to exit without hard timeout, which will 
            // ensure that we've read everything from the stdout and stderr.
            _proc.WaitForExit();

            Trace.Info("Process finished: fileName={0} arguments={1} exitCode={2} in {3} ms",
                _proc.StartInfo.FileName, _proc.StartInfo.Arguments, _proc.ExitCode, _stopWatch.ElapsedMilliseconds);

            return _proc.ExitCode;
        }

        public void Execute(String workingDirectory, String filename, String arguments, IDictionary<String, String> environmentVariables)
        {
            Debug.Assert(null == _proc);            
            Trace.Info("Starting process {0} {1} on working directory {2}", filename, arguments, workingDirectory);

            _proc = new Process();
            _proc.StartInfo.FileName = filename;
            _proc.StartInfo.Arguments = arguments;
            _proc.StartInfo.WorkingDirectory = workingDirectory;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.RedirectStandardInput = false;
            _proc.StartInfo.RedirectStandardError = null != ErrorDataReceived;
            _proc.StartInfo.RedirectStandardOutput = null != OutputDataReceived;
            //set the following flag to "false" if  you like to see worker console output for debugging
            _proc.StartInfo.CreateNoWindow = true;

            if (environmentVariables != null && environmentVariables.Count > 0)
            {
                foreach (KeyValuePair<String, String> kvp in environmentVariables)
                {
                    _proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            object syncObject = new object();

            _proc.EnableRaisingEvents = true;

            if (_proc.StartInfo.RedirectStandardOutput)
            {
                _proc.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
                {
                    // at the end of the process, the event fires one last time with null
                    if (e.Data != null)
                    {
                        lock (syncObject)
                        {
                            EventHandler<ProcessDataReceivedEventArgs> outputDataReceived = OutputDataReceived;
                            if (null != outputDataReceived)
                            {
                                outputDataReceived(this, new ProcessDataReceivedEventArgs(e.Data));
                            }
                        }
                    }
                };
            }
            if (_proc.StartInfo.RedirectStandardError)
            {
                _proc.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
                {
                    // at the end of the process, the event fires one last time with null
                    if (e.Data != null)
                    {
                        lock (syncObject)
                        {
                            EventHandler<ProcessDataReceivedEventArgs> errorDataReceived = ErrorDataReceived;
                            if (null != errorDataReceived)
                            {
                                errorDataReceived(this, new ProcessDataReceivedEventArgs(e.Data));
                            }
                        }
                    }
                };
            }

            _proc.Exited += delegate (object sender, System.EventArgs e)
            {
                _stopWatch.Stop();
                _processExitedSignal.Release();
                EventHandler exited = Exited;
                if (null != exited)
                {
                    exited(this, null);
                }
            };

            _stopWatch = Stopwatch.StartNew();
            bool newProcessStarted = _proc.Start();
            if (!newProcessStarted)
            {
                Trace.Verbose("Used existing process instead of starting new one for " + filename);
            }
            if (_proc.StartInfo.RedirectStandardOutput) { 
                _proc.BeginOutputReadLine();
            }
            if (_proc.StartInfo.RedirectStandardError)
            {
                _proc.BeginErrorReadLine();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _processExitedSignal.Dispose();
                if (null != _proc)
                {
                    _proc.Dispose();
                    _proc = null;
                }

                disposedValue = true;
            }
        }        

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }

}
