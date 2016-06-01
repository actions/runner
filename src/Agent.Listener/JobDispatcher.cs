using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(JobDispatcher))]
    public interface IJobDispatcher : IAgentService
    {
        void Run(JobRequestMessage message);
        bool Cancel(JobCancelMessage message);
        Task WaitAsync(CancellationToken token);
        Task ShutdownAsync();
    }

    // This implementation of IDobDispatcher is not thread safe.
    // It is base on the fact that the current design of agent is dequeue
    // and process one message from message queue everytime.
    // In addition, it only execute one job every time, 
    // and server will not send another job while this one is still running.
    public sealed class JobDispatcher : AgentService, IJobDispatcher
    {
        private int _poolId;
        private static readonly string _workerProcessName = $"Agent.Worker{IOUtil.ExeExtension}";

        // this is not thread-safe
        private readonly Queue<Guid> _jobDispatchedQueue = new Queue<Guid>();
        private readonly ConcurrentDictionary<Guid, WorkerDispatcher> _jobInfos = new ConcurrentDictionary<Guid, WorkerDispatcher>();

        //allow up to 30sec for any data to be transmitted over the process channel
        private readonly TimeSpan ChannelTimeout = TimeSpan.FromSeconds(30);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            // get pool id from config
            var configurationStore = hostContext.GetService<IConfigurationStore>();
            AgentSettings agentSetting = configurationStore.GetSettings();
            _poolId = agentSetting.PoolId;
        }

        public void Run(JobRequestMessage jobRequestMessage)
        {
            Trace.Info($"Job request {jobRequestMessage.JobId} received.");

            WorkerDispatcher currentDispatch = null;
            if (_jobDispatchedQueue.Count > 0)
            {
                Guid dispatchedJobId = _jobDispatchedQueue.Dequeue();
                if (_jobInfos.TryGetValue(dispatchedJobId, out currentDispatch))
                {
                    Trace.Verbose($"Retrive previous WorkerDispather for job {currentDispatch.JobId}.");
                }
            }

            WorkerDispatcher newDispatch = new WorkerDispatcher(jobRequestMessage.JobId, jobRequestMessage.RequestId);
            newDispatch.WorkerDispatch = RunAsync(jobRequestMessage, currentDispatch, newDispatch.WorkerCancellationTokenSource.Token);

            _jobInfos.TryAdd(newDispatch.JobId, newDispatch);
            _jobDispatchedQueue.Enqueue(newDispatch.JobId);
        }

        public bool Cancel(JobCancelMessage jobCancelMessage)
        {
            Trace.Info($"Job cancellation request {jobCancelMessage.JobId} received.");

            WorkerDispatcher workerDispatcher;
            if (!_jobInfos.TryGetValue(jobCancelMessage.JobId, out workerDispatcher))
            {
                Trace.Verbose($"Job request {jobCancelMessage.JobId} is not a current running job, ignore cancllation request.");
                return false;
            }
            else
            {
                if (workerDispatcher.Cancel())
                {
                    Trace.Verbose($"Fired cancellation token for job request {workerDispatcher.JobId}.");
                }

                return true;
            }
        }

        public async Task WaitAsync(CancellationToken token)
        {
            WorkerDispatcher currentDispatch = null;
            Guid dispatchedJobId;
            if (_jobDispatchedQueue.Count > 0)
            {
                dispatchedJobId = _jobDispatchedQueue.Dequeue();
                if (_jobInfos.TryGetValue(dispatchedJobId, out currentDispatch))
                {
                    Trace.Verbose($"Retrive previous WorkerDispather for job {currentDispatch.JobId}.");
                }
            }
            else
            {
                Trace.Verbose($"There is no running WorkerDispather needs to await.");
            }

            if (currentDispatch != null)
            {
                using (var registration = token.Register(() => { if (currentDispatch.Cancel()) { Trace.Verbose($"Fired cancellation token for job request {currentDispatch.JobId}."); } }))
                {
                    try
                    {
                        Trace.Info($"Waiting WorkerDispather for job {currentDispatch.JobId} run to finish.");
                        await currentDispatch.WorkerDispatch;
                        Trace.Info($"Job request {currentDispatch.JobId} processed succeed.");
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Worker Dispatch failed witn an exception for job request {currentDispatch.JobId}.");
                        Trace.Error(ex);
                    }
                    finally
                    {
                        WorkerDispatcher workerDispatcher;
                        if (_jobInfos.TryRemove(currentDispatch.JobId, out workerDispatcher))
                        {
                            Trace.Verbose($"Remove WorkerDispather from {nameof(_jobInfos)} dictionary for job {currentDispatch.JobId}.");
                            workerDispatcher.Dispose();
                        }
                    }
                }
            }
        }

        public async Task ShutdownAsync()
        {
            Trace.Info($"Shutting down JobDispather. Make sure all WorkerDispatcher has finished.");
            WorkerDispatcher currentDispatch = null;
            if (_jobDispatchedQueue.Count > 0)
            {
                Guid dispatchedJobId = _jobDispatchedQueue.Dequeue();
                if (_jobInfos.TryGetValue(dispatchedJobId, out currentDispatch))
                {
                    try
                    {
                        Trace.Info($"Ensure WorkerDispather for job {currentDispatch.JobId} run to finish.");
                        await EnsureDispatchFinished(currentDispatch);
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Catching worker dispatch exception for job request {currentDispatch.JobId} durning job dispatcher shut down.");
                        Trace.Error(ex);
                    }
                    finally
                    {
                        WorkerDispatcher workerDispatcher;
                        if (_jobInfos.TryRemove(currentDispatch.JobId, out workerDispatcher))
                        {
                            Trace.Verbose($"Remove WorkerDispather from {nameof(_jobInfos)} dictionary for job {currentDispatch.JobId}.");
                            workerDispatcher.Dispose();
                        }
                    }
                }
            }
        }

        private async Task EnsureDispatchFinished(WorkerDispatcher jobDispatch)
        {
            if (!jobDispatch.WorkerDispatch.IsCompleted)
            {
                // base on the current design, server will only send one job for a given agent everytime.
                // if the agent received a new job request while a previous job request is still running, this typically indicate two situations
                // 1. an agent bug cause server and agent mismatch on the state of the job request, ex. agent not renew jobrequest properly but think it still own the job reqest, however server already abandon the jobrequest.
                // 2. a server bug or design change that allow server send more than one job request to an given agent that haven't finish previous job request.
                var agentServer = HostContext.GetService<IAgentServer>();
                TaskAgentJobRequest request = await agentServer.GetAgentRequestAsync(_poolId, jobDispatch.RequestId, CancellationToken.None);
                if (request.Result != null)
                {
                    // job request has been finished, the server already has result.
                    // this means agent is busted since it still running that request.
                    // cancel the zombie worker, run next job request.
                    Trace.Error($"Received job request while previous job {jobDispatch.JobId} still running on worker. Cancel the previous job since the job request have been finished on server side with result: {request.Result.Value}.");
                    jobDispatch.WorkerCancellationTokenSource.Cancel();

                    // wait 45 sec for worker to finish.
                    Task completedTask = await Task.WhenAny(jobDispatch.WorkerDispatch, Task.Delay(TimeSpan.FromSeconds(45)));
                    if (completedTask != jobDispatch.WorkerDispatch)
                    {
                        // at this point, the job exectuion might encounter some dead lock and even not able to be canclled.
                        // no need to localize the exception string should never happen.
                        throw new InvalidOperationException("Job dispatch process has encountered unexpected error, the dispatch task is not able to be canceled within 45 seconds.");
                    }
                }
                else
                {
                    // something seriously wrong on server side. stop agent from continue running.
                    // no need to localize the exception string should never happen.
                    throw new InvalidOperationException("Server send a new job request while the previous job request haven't finished.");
                }
            }

            try
            {
                await jobDispatch.WorkerDispatch;
                Trace.Info($"Job request {jobDispatch.JobId} processed succeed.");
            }
            catch (Exception ex)
            {
                Trace.Error($"Worker Dispatch failed witn an exception for job request {jobDispatch.JobId}.");
                Trace.Error(ex);
            }
            finally
            {
                WorkerDispatcher workerDispatcher;
                if (_jobInfos.TryRemove(jobDispatch.JobId, out workerDispatcher))
                {
                    Trace.Verbose($"Remove WorkerDispather from {nameof(_jobInfos)} dictionary for job {jobDispatch.JobId}.");
                    workerDispatcher.Dispose();
                }
            }
        }

        private async Task RunAsync(JobRequestMessage message, WorkerDispatcher previousJobDispatch, CancellationToken jobRequestCancellationToken)
        {
            if (previousJobDispatch != null)
            {
                Trace.Verbose($"Make sure the previous job request {previousJobDispatch.JobId} has successfully finished on worker.");
                await EnsureDispatchFinished(previousJobDispatch);
            }
            else
            {
                Trace.Verbose($"This is the first job request.");
            }

            var term = HostContext.GetService<ITerminal>();
            term.WriteLine(StringUtil.Loc("RunningJob", DateTime.UtcNow, message.JobName));

            // first job request renew succeed.
            TaskCompletionSource<int> firstJobRequestRenewed = new TaskCompletionSource<int>();
            var notification = HostContext.GetService<IJobNotification>();

            // lock renew cancellation token.
            using (var lockRenewalTokenSource = new CancellationTokenSource())
            using (var workerProcessCancelTokenSource = new CancellationTokenSource())
            {
                await notification.JobStarted(message.JobId);
                long requestId = message.RequestId;
                Guid lockToken = message.LockToken;

                // start renew job request
                Trace.Info("Start renew job request.");
                Task renewJobRequest = RenewJobRequestAsync(_poolId, requestId, lockToken, firstJobRequestRenewed, lockRenewalTokenSource.Token);

                // wait till first renew succeed or job request is canceled
                // not even start worker if the first renew fail
                await Task.WhenAny(firstJobRequestRenewed.Task, renewJobRequest, Task.Delay(-1, jobRequestCancellationToken));

                if (renewJobRequest.IsCompleted)
                {
                    // renew job request task complete means we run out of retry for the first job request renew.
                    // TODO: not need to return anything.
                    Trace.Info("Unable to renew job request for the first time, stop dispatching job to worker.");
                    return;
                }

                if (jobRequestCancellationToken.IsCancellationRequested)
                {
                    await CompleteJobRequestAsync(_poolId, requestId, lockToken, TaskResult.Canceled);
                    return;
                }

                Task<int> workerProcessTask = null;
                using (var processChannel = HostContext.CreateService<IProcessChannel>())
                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                {
                    // Start the process channel.
                    // It's OK if StartServer bubbles an execption after the worker process has already started.
                    // The worker will shutdown after 30 seconds if it hasn't received the job message.
                    processChannel.StartServer(
                        // Delegate to start the child process.
                        startProcess: (string pipeHandleOut, string pipeHandleIn) =>
                        {
                            // Validate args.
                            ArgUtil.NotNullOrEmpty(pipeHandleOut, nameof(pipeHandleOut));
                            ArgUtil.NotNullOrEmpty(pipeHandleIn, nameof(pipeHandleIn));

                            // Start the child process.
                            var assemblyDirectory = IOUtil.GetBinPath();
                            string workerFileName = Path.Combine(assemblyDirectory, _workerProcessName);
                            workerProcessTask = processInvoker.ExecuteAsync(
                                workingDirectory: assemblyDirectory,
                                fileName: workerFileName,
                                arguments: "spawnclient " + pipeHandleOut + " " + pipeHandleIn,
                                environment: null,
                                cancellationToken: workerProcessCancelTokenSource.Token);
                        });

                    // Send the job request message.
                    // Kill the worker process if sending the job message times out. The worker
                    // process may have successfully received the job message.
                    try
                    {
                        Trace.Info("Send job request message to worker.");
                        using (var csSendJobRequest = new CancellationTokenSource(ChannelTimeout))
                        {
                            await processChannel.SendAsync(
                                messageType: MessageType.NewJobRequest,
                                body: JsonUtility.ToString(message),
                                cancellationToken: csSendJobRequest.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // message send been cancelled.
                        // timeout 45 sec. kill worker.
                        Trace.Info("Job request message sending been cancelled, kill running worker.");
                        workerProcessCancelTokenSource.Cancel();
                        try
                        {
                            await workerProcessTask;
                        }
                        catch (OperationCanceledException)
                        {
                            // worker process been killed.
                        }

                        Trace.Info("Stop renew job request.");
                        // stop renew lock
                        lockRenewalTokenSource.Cancel();
                        // renew job request should never blows up.
                        await renewJobRequest;

                        // not finish the job request since the job haven't run on worker at all, we will not going to set a result to server.
                        return;
                    }

                    TaskResult resultOnAbandonOrCancel = TaskResult.Succeeded;
                    // wait for renewlock, worker process or cancellation token been fired.
                    var completedTask = await Task.WhenAny(renewJobRequest, workerProcessTask, Task.Delay(-1, jobRequestCancellationToken));
                    if (completedTask == workerProcessTask)
                    {
                        // worker finished successfully, complete job request with result, stop renew lock, job has finished.
                        int returnCode = await workerProcessTask;
                        Trace.Info("Worker finished. Code: " + returnCode);

                        TaskResult result = TaskResultUtil.TranslateFromReturnCode(returnCode);
                        Trace.Info($"finish job request with result: {result}");
                        term.WriteLine(StringUtil.Loc("JobCompleted", DateTime.UtcNow, message.JobName, result));
                        // complete job request
                        await CompleteJobRequestAsync(_poolId, requestId, lockToken, result);
                        await notification.JobCompleted(message.JobId);

                        Trace.Info("Stop renew job request.");
                        // stop renew lock
                        lockRenewalTokenSource.Cancel();
                        // renew job request should never blows up.
                        await renewJobRequest;

                        return;
                    }
                    else if (completedTask == renewJobRequest)
                    {
                        resultOnAbandonOrCancel = TaskResult.Abandoned;
                    }
                    else
                    {
                        resultOnAbandonOrCancel = TaskResult.Canceled;
                    }

                    // renew job request completed or job request cancellation token been fired for RunAsync(jobrequestmessage)
                    // cancel worker gracefully first, then kill it after 45 sec
                    try
                    {
                        Trace.Info("Send job cancellation message to worker.");
                        using (var csSendCancel = new CancellationTokenSource(ChannelTimeout))
                        {
                            await processChannel.SendAsync(
                                messageType: MessageType.CancelRequest,
                                body: string.Empty,
                                cancellationToken: csSendCancel.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // message send been cancelled.
                        Trace.Info("Job cancel message sending been cancelled, kill running worker.");
                        workerProcessCancelTokenSource.Cancel();
                        try
                        {
                            await workerProcessTask;
                        }
                        catch (OperationCanceledException)
                        {
                            // worker process been killed.
                        }
                    }

                    // wait worker to exit within 45 sec, then kill worker.
                    using (var csKillWorker = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
                    {
                        completedTask = await Task.WhenAny(workerProcessTask, Task.Delay(-1, csKillWorker.Token));
                    }

                    // worker haven't exit within 45 sec.
                    if (completedTask != workerProcessTask)
                    {
                        Trace.Info("worker process haven't exit after 45 sec, kill running worker.");
                        workerProcessCancelTokenSource.Cancel();
                        try
                        {
                            await workerProcessTask;
                        }
                        catch (OperationCanceledException)
                        {
                            // worker process been killed.
                        }
                    }

                    Trace.Info($"finish job request with result: {resultOnAbandonOrCancel}");
                    term.WriteLine(StringUtil.Loc("JobCompleted", DateTime.UtcNow, message.JobName, resultOnAbandonOrCancel));
                    // complete job request with cancel result, stop renew lock, job has finished.
                    //TODO: don't finish job request on abandon
                    await CompleteJobRequestAsync(_poolId, requestId, lockToken, resultOnAbandonOrCancel);
                    await notification.JobCompleted(message.JobId);

                    Trace.Info("Stop renew job request.");
                    // stop renew lock
                    lockRenewalTokenSource.Cancel();
                    // renew job request should never blows up.
                    await renewJobRequest;
                }
            }
        }

        private async Task RenewJobRequestAsync(int poolId, long requestId, Guid lockToken, TaskCompletionSource<int> firstJobRequestRenewed, CancellationToken token)
        {
            var agentServer = HostContext.GetService<IAgentServer>();
            int firstRenewRetryLimit = 5;
            TaskAgentJobRequest request = null;

            // renew lock during job running.
            // stop renew only if cancellation token for lock renew task been signal or exception still happen after retry.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    request = await agentServer.RenewAgentRequestAsync(poolId, requestId, lockToken, token);

                    Trace.Info($"Successfully renew job request, job is valid till {request.LockedUntil.Value}");

                    if (!firstJobRequestRenewed.Task.IsCompleted)
                    {
                        // fire first renew successed event.
                        firstJobRequestRenewed.TrySetResult(0);
                    }

                    // renew again after 60 sec delay
                    await Task.Delay(TimeSpan.FromSeconds(60), token);
                }
                catch (Exception ex)
                {
                    if (ex is TaskAgentJobNotFoundException)
                    {
                        // no need for retry. the job is not valid anymore.
                        Trace.Info("TaskAgentJobNotFoundException received, job is no longer valid, stop renew job request.");
                        return;
                    }
                    else if (ex is TaskAgentJobTokenExpiredException)
                    {
                        // no need for retry. the job is not valid anymore.
                        Trace.Info("TaskAgentJobTokenExpiredException received, job is no longer valid, stop renew job request.");
                        return;
                    }
                    else if (ex is TaskCanceledException)
                    {
                        // TaskCanceledException may caused by http timeout or _lockRenewalTokenSource.Cance();
                        if (token.IsCancellationRequested)
                        {
                            Trace.Info("job renew has been canceled, stop renew job request.");
                            return;
                        }
                    }

                    // retry
                    TimeSpan remainingTime = TimeSpan.Zero;
                    if (!firstJobRequestRenewed.Task.IsCompleted)
                    {
                        // retry 5 times every 10 sec for the first renew
                        if (firstRenewRetryLimit-- > 0)
                        {
                            remainingTime = TimeSpan.FromSeconds(10);
                        }
                    }
                    else
                    {
                        // retry till reach lockeduntil
                        remainingTime = request.LockedUntil.Value - DateTime.UtcNow;
                    }

                    if (remainingTime > TimeSpan.Zero)
                    {
                        Trace.Verbose($"Retrying lock renewal. Job is still locked for: {remainingTime.TotalSeconds} seconds.");
                        TimeSpan delayTime = remainingTime.TotalSeconds > 60 ? TimeSpan.FromMinutes(1) : remainingTime;
                        await Task.Delay(delayTime, token);
                    }
                    else
                    {
                        return;
                    }
                }
            }

            return;
        }

        private async Task CompleteJobRequestAsync(int poolId, long requestId, Guid lockToken, TaskResult result)
        {
            var agentServer = HostContext.GetService<IAgentServer>();
            try
            {
                using (var csFinishRequest = new CancellationTokenSource(ChannelTimeout))
                {
                    await agentServer.FinishAgentRequestAsync(poolId, requestId, lockToken, DateTime.UtcNow, result, csFinishRequest.Token);
                }
            }
            catch (TaskAgentJobNotFoundException)
            {
                Trace.Info("TaskAgentJobNotFoundException received, job is no longer valid.");
            }
            catch (TaskAgentJobTokenExpiredException)
            {
                Trace.Info("TaskAgentJobTokenExpiredException received, job is no longer valid.");
            }
        }

        private class WorkerDispatcher : IDisposable
        {
            public long RequestId { get; }
            public Guid JobId { get; }
            public Task WorkerDispatch { get; set; }
            public CancellationTokenSource WorkerCancellationTokenSource { get; private set; }
            private readonly object _lock = new object();

            public WorkerDispatcher(Guid jobId, long requestId)
            {
                JobId = jobId;
                RequestId = requestId;
                WorkerCancellationTokenSource = new CancellationTokenSource();
            }

            public bool Cancel()
            {
                if (WorkerCancellationTokenSource != null)
                {
                    lock (_lock)
                    {
                        if (WorkerCancellationTokenSource != null)
                        {
                            WorkerCancellationTokenSource.Cancel();
                            return true;
                        }
                    }
                }

                return false;
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
                    if (WorkerCancellationTokenSource != null)
                    {
                        lock (_lock)
                        {
                            if (WorkerCancellationTokenSource != null)
                            {
                                WorkerCancellationTokenSource.Dispose();
                                WorkerCancellationTokenSource = null;
                            }
                        }
                    }
                }
            }
        }
    }
}
