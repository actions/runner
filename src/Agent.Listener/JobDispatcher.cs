using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(JobDispatcher))]
    public interface IJobDispatcher : IDisposable, IAgentService
    {
        Task<int> RunAsync(JobRequestMessage message, CancellationToken jobRequestCancellationToken);
    }

    public sealed class JobDispatcher : AgentService, IJobDispatcher
    {
#if OS_WINDOWS
        private const string WorkerProcessName = "Agent.Worker.exe";
#else
        private const string WorkerProcessName = "Agent.Worker";
#endif        
        //allow up to 30sec for any data to be transmitted over the process channel
        private readonly TimeSpan ChannelTimeout = TimeSpan.FromSeconds(30);

        public async Task<int> RunAsync(JobRequestMessage message, CancellationToken jobRequestCancellationToken)
        {
            // first job request renew succeed.
            TaskCompletionSource<int> firstJobRequestRenewed = new TaskCompletionSource<int>();

            // lock renew cancellation token.
            CancellationTokenSource lockRenewalTokenSource = new CancellationTokenSource();

            // get pool id from config
            var configurationStore = HostContext.GetService<IConfigurationStore>();
            AgentSettings agentSetting = configurationStore.GetSettings();
            int poolId = agentSetting.PoolId;
            long requestId = message.RequestId;
            Guid lockToken = message.LockToken;

            // start renew job request
            Trace.Info("Start renew job request.");
            Task renewJobRequest = RenewJobRequestAsync(poolId, requestId, lockToken, firstJobRequestRenewed, lockRenewalTokenSource.Token);

            // wait till first renew succeed
            // not even start worker if the first renew fail
            await Task.WhenAny(firstJobRequestRenewed.Task, renewJobRequest);

            if (renewJobRequest.IsCompleted)
            {
                // renew job request task complete means we run out of retry for the first job request renew.
                // TODO: not need to return anything.
                Trace.Info("Unable to renew job request for the first time, stop dispatching job to worker.");
                return 0;
            }

            CancellationTokenSource workerProcessCancelTokenSource = new CancellationTokenSource();

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
                        string workerFileName = Path.Combine(assemblyDirectory, WorkerProcessName);
                        workerProcessTask = processInvoker.ExecuteAsync(
                            workingDirectory: assemblyDirectory,
                            fileName: workerFileName,
                            arguments: "spawnclient " + pipeHandleOut + " " + pipeHandleIn,
                            environment: null,
                            cancellationToken: workerProcessCancelTokenSource.Token);
                    });

                // Send the job request message.
                // TODO: Kill the worker process if sending the job message times out. The worker
                // process may have successfully received the job message.
                try
                {
                    Trace.Info("Send job request message to worker.");
                    await processChannel.SendAsync(
                        messageType: MessageType.NewJobRequest,
                        body: JsonUtility.ToString(message),
                        cancellationToken: new CancellationTokenSource(ChannelTimeout).Token);
                }
                catch (OperationCanceledException)
                {
                    // message send been cancelled.
                    // TODO: Kill worker
                    // timeout 45 sec. kill worker.
                    // TODO: currently, fire cancellation token is not killing worker.
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
                    return 0;
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
                    // complete job request
                    await CompleteJobRequestAsync(poolId, requestId, lockToken, result);

                    Trace.Info("Stop renew job request.");
                    // stop renew lock
                    lockRenewalTokenSource.Cancel();
                    // renew job request should never blows up.
                    await renewJobRequest;

                    return 0;
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
                    await processChannel.SendAsync(
                        messageType: MessageType.CancelRequest,
                        body: string.Empty,
                        cancellationToken: new CancellationTokenSource(ChannelTimeout).Token);
                }
                catch (OperationCanceledException)
                {
                    // message send been cancelled.
                    // TODO: currently, fire cancellation token is not killing worker.
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
                completedTask = await Task.WhenAny(workerProcessTask, Task.Delay(-1, new CancellationTokenSource(TimeSpan.FromSeconds(45)).Token));

                // worker haven't exit within 45 sec.
                if (completedTask != workerProcessTask)
                {
                    // TODO: currently, fire cancellation token is not killing worker.
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
                // complete job request with cancel result, stop renew lock, job has finished.
                await CompleteJobRequestAsync(poolId, requestId, lockToken, resultOnAbandonOrCancel);

                Trace.Info("Stop renew job request.");
                // stop renew lock
                lockRenewalTokenSource.Cancel();
                // renew job request should never blows up.
                await renewJobRequest;

                return 0;
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

                    if(!firstJobRequestRenewed.Task.IsCompleted)
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
                await agentServer.FinishAgentRequestAsync(poolId, requestId, lockToken, DateTime.UtcNow, result);
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

        // TODO: REMOVE DEAD CODE.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}
