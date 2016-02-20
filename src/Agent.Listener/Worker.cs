using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        void LaunchProcess(String pipeHandleOut, String pipeHandleIn);
    }

    public class Worker : IWorker
    {
        public event EventHandler StateChanged;
        public Guid JobId { get; set; }
        public IProcessChannel ProcessChannel { get; set; }
        public Process JobProcess;
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
            JobProcess = new Process();
            State = WorkerState.New;
        }

        public void LaunchProcess(String pipeHandleOut, String pipeHandleIn)
        {
            string clientFileName = "Agent.Worker";
            bool hasExeSuffix = clientFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
#if OS_WINDOWS
            if (!hasExeSuffix)
            {
                clientFileName += ".exe";
            }
#else
                    if (hasExeSuffix) {
                        clientFileName = clientFileName.Substring(0, clientFileName.Length - 4);
                    }
#endif
            //TODO: use ProcessInvoker instead
            JobProcess.StartInfo.FileName = clientFileName;
            JobProcess.StartInfo.Arguments = "spawnclient " + pipeHandleOut + " " + pipeHandleIn;
            JobProcess.EnableRaisingEvents = true;
            JobProcess.Exited += JobProcess_Exited;
            State = WorkerState.Starting;
            JobProcess.Start();
        }

        private void JobProcess_Exited(object sender, EventArgs e)
        {
            JobProcess.Exited -= JobProcess_Exited;
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
