using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(WorkerManager))]
    public interface IWorkerManager : IDisposable
    {
        Task Run(IHostContext context, JobRequestMessage message);
        Task Cancel(IHostContext context, JobCancelMessage message);
    }    

    public class WorkerManager : IWorkerManager
    {
        private const String TraceName = nameof(WorkerManager);
        private ConcurrentDictionary<Guid, IWorker> _jobsInProgress = new ConcurrentDictionary<Guid, IWorker>();

        public async Task Run(IHostContext context, JobRequestMessage jobRequestMessage)
        {
            TraceSource trace = context.Trace[TraceName];
            trace.Info("Job request {0} received.", jobRequestMessage.JobId);
            var worker = context.GetService<IWorker>();
            worker.JobId = jobRequestMessage.JobId;
            worker.ProcessChannel = context.GetService<IProcessChannel>();            
            worker.StateChanged += Worker_StateChanged;
            _jobsInProgress[jobRequestMessage.JobId] = worker;
            worker.ProcessChannel.StartServer( (p1, p2) => 
                {
                    string workingFolder = ""; //TODO: pass working folder from the config to the worker process
                    worker.LaunchProcess(context, p1, p2, workingFolder);
                }
            );
            await worker.ProcessChannel.SendAsync(jobRequestMessage, context.CancellationToken);
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

        public async Task Cancel(IHostContext context, JobCancelMessage jobCancelMessage)
        {
            TraceSource trace = context.Trace[TraceName];
            IWorker worker = null;
            if (!_jobsInProgress.TryGetValue(jobCancelMessage.JobId, out worker))
            {
                trace.Error("Received cancellation for invalid job id {0}.", jobCancelMessage.JobId);
            }            
            await worker.ProcessChannel.SendAsync(jobCancelMessage, context.CancellationToken);            
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
