using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    public enum WorkerState
    {
        New,
        Starting,
        Finished,
    }

    [ServiceLocator(Default = typeof(Worker))]
    public interface IWorker : IDisposable
    {
        event EventHandler StateChanged;
        Guid JobId { get; set; }
        IProcessChannel ProcessChannel { get; set; }
        void LaunchProcess(IHostContext hostContext, String pipeHandleOut, String pipeHandleIn, string workingFolder);
    }

    public class Worker : IWorker
    {
#if OS_WINDOWS
        private const String WorkerProcessName = "Agent.Worker.exe";
#else
        private const String WorkerProcessName = "Agent.Worker";
#endif

        public event EventHandler StateChanged;
        public Guid JobId { get; set; }
        public IProcessChannel ProcessChannel { get; set; }
        private IProcessInvoker _processInvoker;
        private WorkerState _state;
        public WorkerState State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (value != _state)
                {
                    _state = value;
                    if (null != StateChanged)
                    {
                        StateChanged(this, null);
                    }
                }
            }
        }
        public Worker()
        {
            State = WorkerState.New;
        }

        public void LaunchProcess(IHostContext hostContext, String pipeHandleOut, String pipeHandleIn, string workingFolder)
        {
            string workerFileName = Path.Combine(AssemblyUtil.AssemblyDirectory, WorkerProcessName);
            _processInvoker = hostContext.GetService<IProcessInvoker>();
            _processInvoker.Exited += _processInvoker_Exited;
            State = WorkerState.Starting;
            var environmentVariables = new Dictionary<String, String>();            
            _processInvoker.Execute(hostContext, workingFolder, workerFileName, "spawnclient " + pipeHandleOut + " " + pipeHandleIn,
                environmentVariables);
        }        

        private void _processInvoker_Exited(object sender, EventArgs e)
        {
            _processInvoker.Exited -= _processInvoker_Exited;
            if (null != ProcessChannel)
            {
                ProcessChannel.Dispose();
                ProcessChannel = null;
            }
            State = WorkerState.Finished;
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (null != ProcessChannel)
                {
                    ProcessChannel.Dispose();
                    ProcessChannel = null;
                }
                if (null != _processInvoker)
                {
                    _processInvoker.Dispose();
                    _processInvoker = null;
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
