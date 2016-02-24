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
    public interface IWorker : IDisposable, IAgentService
    {
        event EventHandler StateChanged;
        Guid JobId { get; set; }
        IProcessChannel ProcessChannel { get; set; }
        //TODO: instead of LaunchProcess, do something like Task RunAsync(...) and make sure you take a cancellation token. The way the IWorkerManager can handle cancelling the worker is to simply signal the cancellation token that it handed to the IWorker.RunAsync method.
        void LaunchProcess(String pipeHandleOut, String pipeHandleIn, string workingFolder);
    }

    public sealed class Worker : AgentService, IWorker
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
        
        public void LaunchProcess(String pipeHandleOut, String pipeHandleIn, string workingFolder)
        {
            string workerFileName = Path.Combine(IOUtil.GetBinPath(), WorkerProcessName);
            _processInvoker = HostContext.CreateService<IProcessInvoker>();
            _processInvoker.Exited += _processInvoker_Exited;
            State = WorkerState.Starting;
            var environmentVariables = new Dictionary<String, String>();            
            _processInvoker.Execute(workingFolder, workerFileName, "spawnclient " + pipeHandleOut + " " + pipeHandleIn,
                environmentVariables);
        }        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                ProcessChannel?.Dispose();
                ProcessChannel = null;
                _processInvoker?.Dispose();
                _processInvoker = null;
            }
        }
    }
}
