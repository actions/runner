using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(WorkerManager))]
    public interface IWorkerManager : IDisposable, IAgentService
    {
        Task Run(JobRequestMessage message);
        Task Cancel(JobCancelMessage message);
    }    

    public class WorkerManager : AgentService, IWorkerManager
    {        
        private ConcurrentDictionary<Guid, IWorker> _jobsInProgress = new ConcurrentDictionary<Guid, IWorker>();

        public async Task Run(JobRequestMessage jobRequestMessage)
        {            
            Trace.Info("Job request {0} received.", jobRequestMessage.JobId);
            var worker = HostContext.GetService<IWorker>();
            worker.JobId = jobRequestMessage.JobId;
            worker.ProcessChannel = HostContext.GetService<IProcessChannel>();            
            worker.StateChanged += Worker_StateChanged;
            _jobsInProgress[jobRequestMessage.JobId] = worker;
            worker.ProcessChannel.StartServer( (p1, p2) => 
                {
                    string workingFolder = ""; //TODO: pass working folder from the config to the worker process
                    worker.LaunchProcess(p1, p2, workingFolder);
                }
            );
            await worker.ProcessChannel.SendAsync(jobRequestMessage, HostContext.CancellationToken);
        }

        private void Worker_StateChanged(object sender, EventArgs e)
        {
            var worker = sender as Worker;
            if (worker.State == WorkerState.Finished)
            {                
                IWorker deletedJob;
                if (_jobsInProgress.TryRemove(worker.JobId, out deletedJob))
                {
                    deletedJob.StateChanged -= Worker_StateChanged;
                    deletedJob.Dispose();
                }
            }
        }

        public async Task Cancel(JobCancelMessage jobCancelMessage)
        {            
            IWorker worker = null;
            if (!_jobsInProgress.TryGetValue(jobCancelMessage.JobId, out worker))
            {
                Trace.Error("Received cancellation for invalid job id {0}.", jobCancelMessage.JobId);
            }            
            await worker.ProcessChannel.SendAsync(jobCancelMessage, HostContext.CancellationToken);            
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (var item in _jobsInProgress)                    
                {
                    item.Value.StateChanged -= Worker_StateChanged;
                    item.Value.Dispose();
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
