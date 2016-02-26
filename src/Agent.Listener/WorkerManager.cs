using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(WorkerManager))]
    public interface IWorkerManager : IDisposable, IAgentService
    {
        Task Run(JobRequestMessage message);
        Task Cancel(JobCancelMessage message);
    }    

    public sealed class WorkerManager : AgentService, IWorkerManager
    {        
        private ConcurrentDictionary<Guid, IWorker> _jobsInProgress = new ConcurrentDictionary<Guid, IWorker>();

        public async Task Run(JobRequestMessage jobRequestMessage)
        {            
            Trace.Info("Job request {0} received.", jobRequestMessage.JobId);
            // TODO: Dispose of the worker since it implements IDisposable.
            var worker = HostContext.CreateService<IWorker>();
            worker.JobId = jobRequestMessage.JobId;
            worker.ProcessChannel = HostContext.CreateService<IProcessChannel>();
            worker.StateChanged += Worker_StateChanged;
            _jobsInProgress[jobRequestMessage.JobId] = worker;
            worker.ProcessChannel.StartServer( (pipeHandleOut, pipeHandleIn) => 
                {
                    worker.LaunchProcess(pipeHandleOut, pipeHandleIn, IOUtil.GetBinPath());
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: This is not thread-safe.
                foreach (IWorker worker in _jobsInProgress.Values)
                {
                    worker.StateChanged -= Worker_StateChanged;
                    worker.Dispose();
                }
            }
        }
    }
}
