using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Threading;
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
        private ConcurrentDictionary<Guid, Tuple<IWorker,Task<int>, CancellationTokenSource>> _jobsInProgress 
            = new ConcurrentDictionary<Guid, Tuple<IWorker, Task<int>, CancellationTokenSource>>();

        public Task Run(JobRequestMessage jobRequestMessage)
        {            
            Trace.Info("Job request {0} received.", jobRequestMessage.JobId);
            var worker = HostContext.GetService<IWorker>();
            Task<int> workerTask;
            var cancellationTokenSource = new CancellationTokenSource();
            workerTask = worker.RunAsync(jobRequestMessage, cancellationTokenSource.Token);
            _jobsInProgress[jobRequestMessage.JobId] = new Tuple<IWorker, Task<int>, CancellationTokenSource>(worker, workerTask, cancellationTokenSource);
            workerTask.ContinueWith( (task) => 
            {
                if (task.Status == TaskStatus.Canceled)
                {
                    Trace.Info("Job request {0} was canceled.", jobRequestMessage.JobId);
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    Trace.Error("Job request {0} failed witn an exception.", jobRequestMessage.JobId);
                    Trace.Error(task.Exception);
                }
                else
                {
                    Trace.Info("Job request {0} processed with return code {1}.", jobRequestMessage.JobId, task.Result);
                }
                Tuple<IWorker, Task<int>, CancellationTokenSource> deletedJob;                
                if (_jobsInProgress.TryRemove(jobRequestMessage.JobId, out deletedJob))
                {
                    deletedJob.Item1.Dispose();
                    deletedJob.Item3.Dispose();
                }
            });           
            return Task.CompletedTask;
        }

        public Task Cancel(JobCancelMessage jobCancelMessage)
        {
            Tuple<IWorker, Task<int>, CancellationTokenSource> worker = null;
            if (!_jobsInProgress.TryGetValue(jobCancelMessage.JobId, out worker))
            {
                Trace.Error("Received cancellation for invalid job id {0}.", jobCancelMessage.JobId);
            }
            else
            {
                worker.Item3.Cancel();
            }
            return Task.CompletedTask;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //TODO: decide if we should wait for workers to complete here
                foreach (var item in _jobsInProgress)                    
                {
                    item.Value.Item1.Dispose();
                    item.Value.Item3.Dispose();
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
