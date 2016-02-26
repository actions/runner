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
        //JobDispatcherItem is used to keep track of a single JobDispatcher, its running task,
        //and a cancellation token than can be used to stop the dispatcher
        private struct JobDispatcherItem : IDisposable
        {
            public IJobDispatcher Dispatcher { get; set; }
            public Task<int> DispatcherTask { get; set; }
            public CancellationTokenSource Token { get; set; }

            public JobDispatcherItem(IJobDispatcher dispatcher, Task<int> task, CancellationTokenSource token)
            {
                Dispatcher = dispatcher;
                DispatcherTask = task;
                Token = token;
            }

            public void Dispose()
            {
                Dispatcher.Dispose();
                Token.Dispose();
            }
        }
             
        private ConcurrentDictionary<Guid, JobDispatcherItem> _jobsInProgress 
            = new ConcurrentDictionary<Guid, JobDispatcherItem>();

        public Task Run(JobRequestMessage jobRequestMessage)
        {            
            Trace.Info("Job request {0} received.", jobRequestMessage.JobId);
            var jobDispatcher = HostContext.GetService<IJobDispatcher>();
            Task<int> jobDispatcherTask;
            var cancellationTokenSource = new CancellationTokenSource();
            jobDispatcherTask = jobDispatcher.RunAsync(jobRequestMessage, cancellationTokenSource.Token);
            _jobsInProgress[jobRequestMessage.JobId] = new JobDispatcherItem(jobDispatcher, jobDispatcherTask, cancellationTokenSource);
            jobDispatcherTask.ContinueWith( (task) => 
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
                JobDispatcherItem deletedJob;                
                if (_jobsInProgress.TryRemove(jobRequestMessage.JobId, out deletedJob))
                {
                    deletedJob.Dispose();                    
                }
            });           
            return Task.CompletedTask;
        }

        public Task Cancel(JobCancelMessage jobCancelMessage)
        {
            JobDispatcherItem worker;
            if (!_jobsInProgress.TryGetValue(jobCancelMessage.JobId, out worker))
            {
                Trace.Error("Received cancellation for invalid job id {0}.", jobCancelMessage.JobId);
            }
            else
            {
                worker.Token.Cancel();
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
