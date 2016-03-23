using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(WorkerManager))]
    public interface IWorkerManager : IDisposable, IAgentService
    {
        void Run(JobRequestMessage message);
        void Cancel(JobCancelMessage message);
    }

    public sealed class WorkerManager : AgentService, IWorkerManager
    {
        //allow up to 45sec for jobs to be cancelled, when WorkerManager is disposed
        private readonly TimeSpan WaitToExitTimeout = TimeSpan.FromSeconds(45);

        //JobDispatcherItem is used to keep track of a single JobDispatcher, its running task,
        //and a cancellation token than can be used to stop the dispatcher
        private class JobDispatcherItem : IDisposable
        {
            public IJobDispatcher Dispatcher { get; set; }
            public Task DispatcherTask { get; set; }
            public CancellationTokenSource Token { get; set; }

            public JobDispatcherItem(IJobDispatcher dispatcher, Task task, CancellationTokenSource token)
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
             
        private Dictionary<Guid, JobDispatcherItem> _jobsInProgress 
            = new Dictionary<Guid, JobDispatcherItem>();
        private bool _disposed;
        private readonly object _lock = new Object();

        public void Run(JobRequestMessage jobRequestMessage)
        {
            Trace.Info("Job request {0} received.", jobRequestMessage.JobId);
            var jobDispatcher = HostContext.CreateService<IJobDispatcher>();
            Task<int> jobDispatcherTask;
            var cancellationTokenSource = new CancellationTokenSource();
            jobDispatcherTask = jobDispatcher.RunAsync(jobRequestMessage, cancellationTokenSource.Token);
            var postJobDispatcherAction = new Action<Task<int>, Object>((task, obj) =>
            {
                lock (_lock)
                {
                    //protect from running after WorkerManager has been disposed
                    if ((obj as WorkerManager)._disposed)
                    {
                        return;
                    }
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
                    if (_jobsInProgress.TryGetValue(jobRequestMessage.JobId, out deletedJob))
                    {
                        _jobsInProgress.Remove(jobRequestMessage.JobId);
                        deletedJob.Dispose();
                    }
                }
            });

            lock (_lock)
            {
                var jobItem = new JobDispatcherItem(jobDispatcher, null, cancellationTokenSource);
                // store the new job item, so that it can be cancelled later if needed
                _jobsInProgress[jobRequestMessage.JobId] = jobItem;
                //run code after the job completes, which prints the result int the trace
                jobItem.DispatcherTask = jobDispatcherTask.ContinueWith(postJobDispatcherAction, this);
            }
        }

        public void Cancel(JobCancelMessage jobCancelMessage)
        {
            Trace.Info("Job cancellation request {0} received.", jobCancelMessage.JobId);
            JobDispatcherItem worker;
            lock (_lock)
            {
                if (!_jobsInProgress.TryGetValue(jobCancelMessage.JobId, out worker))
                {
                    Trace.Error("Received cancellation for invalid job id {0}.", jobCancelMessage.JobId);
                }
                else
                {
                    worker.Token.Cancel();
                }
            }
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
                //cancel all running jobs                
                var jobsList = new List<Task>();
                lock (_lock)
                {                    
                    foreach (JobDispatcherItem dispatcherItem in _jobsInProgress.Values)
                    {
                        if (dispatcherItem.DispatcherTask != null)
                        {
                            jobsList.Add(dispatcherItem.DispatcherTask);
                            dispatcherItem.Token.Cancel();
                        }
                    }
                }
                if (jobsList.Count > 0)
                {
                    Trace.Info($"Waiting for {jobsList.Count} jobs to complete");
                    //wait up to 45 seconds for jobs to exit
                    Task.WaitAll(jobsList.ToArray(), WaitToExitTimeout);
                }
                lock (_lock)
                {
                    foreach (JobDispatcherItem dispatcherItem in _jobsInProgress.Values)
                    {
                        dispatcherItem.Dispose();
                    }
                    _jobsInProgress.Clear();
                    _disposed = true;
                }
            }            
        }
    }
}
