using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Services.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System.Linq;
using GitHub.Services.Common;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener
{
    [ServiceLocator(Default = typeof(JobDispatcher))]
    public interface IJobDispatcher : IRunnerService
    {
        TaskCompletionSource<bool> RunOnceJobCompleted { get; }
        void Run(Pipelines.AgentJobRequestMessage message, bool runOnce = false);
        bool Cancel(JobCancelMessage message);
        Task WaitAsync(CancellationToken token);
        TaskResult GetLocalRunJobResult(AgentJobRequestMessage message);
        Task ShutdownAsync();
    }

    // This implementation of IDobDispatcher is not thread safe.
    // It is base on the fact that the current design of runner is dequeue
    // and process one message from message queue everytime.
    // In addition, it only execute one job every time, 
    // and server will not send another job while this one is still running.
    public sealed class JobDispatcher : RunnerService, IJobDispatcher
    {
        private readonly Lazy<Dictionary<long, TaskResult>> _localRunJobResult = new Lazy<Dictionary<long, TaskResult>>();
        private int _poolId;
        RunnerSettings _runnerSetting;
        private static readonly string _workerProcessName = $"Runner.Worker{IOUtil.ExeExtension}";

        // this is not thread-safe
        private readonly Queue<Guid> _jobDispatchedQueue = new Queue<Guid>();
        private readonly ConcurrentDictionary<Guid, WorkerDispatcher> _jobInfos = new ConcurrentDictionary<Guid, WorkerDispatcher>();

        //allow up to 30sec for any data to be transmitted over the process channel
        //timeout limit can be overwrite by environment GITHUB_ACTIONS_RUNNER_CHANNEL_TIMEOUT
        private TimeSpan _channelTimeout;

        private TaskCompletionSource<bool> _runOnceJobCompleted = new TaskCompletionSource<bool>();

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            // get pool id from config
            var configurationStore = hostContext.GetService<IConfigurationStore>();
            _runnerSetting = configurationStore.GetSettings();
            _poolId = _runnerSetting.PoolId;

            int channelTimeoutSeconds;
            if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_CHANNEL_TIMEOUT") ?? string.Empty, out channelTimeoutSeconds))
            {
                channelTimeoutSeconds = 30;
            }

            // _channelTimeout should in range [30,  300] seconds
            _channelTimeout = TimeSpan.FromSeconds(Math.Min(Math.Max(channelTimeoutSeconds, 30), 300));
            Trace.Info($"Set runner/worker IPC timeout to {_channelTimeout.TotalSeconds} seconds.");
        }

        public TaskCompletionSource<bool> RunOnceJobCompleted => _runOnceJobCompleted;

        public void Run(Pipelines.AgentJobRequestMessage jobRequestMessage, bool runOnce = false)
        {
            Trace.Info($"Job request {jobRequestMessage.RequestId} for plan {jobRequestMessage.Plan.PlanId} job {jobRequestMessage.JobId} received.");

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
            if (runOnce)
            {
                Trace.Info("Start dispatcher for one time used runner.");
                newDispatch.WorkerDispatch = RunOnceAsync(jobRequestMessage, currentDispatch, newDispatch.WorkerCancellationTokenSource.Token, newDispatch.WorkerCancelTimeoutKillTokenSource.Token);
            }
            else
            {
                newDispatch.WorkerDispatch = RunAsync(jobRequestMessage, currentDispatch, newDispatch.WorkerCancellationTokenSource.Token, newDispatch.WorkerCancelTimeoutKillTokenSource.Token);
            }

            _jobInfos.TryAdd(newDispatch.JobId, newDispatch);
            _jobDispatchedQueue.Enqueue(newDispatch.JobId);
        }

        public bool Cancel(JobCancelMessage jobCancelMessage)
        {
            Trace.Info($"Job cancellation request {jobCancelMessage.JobId} received, cancellation timeout {jobCancelMessage.Timeout.TotalMinutes} minutes.");

            WorkerDispatcher workerDispatcher;
            if (!_jobInfos.TryGetValue(jobCancelMessage.JobId, out workerDispatcher))
            {
                Trace.Verbose($"Job request {jobCancelMessage.JobId} is not a current running job, ignore cancllation request.");
                return false;
            }
            else
            {
                if (workerDispatcher.Cancel(jobCancelMessage.Timeout))
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
                using (var registration = token.Register(() => { if (currentDispatch.Cancel(TimeSpan.FromSeconds(60))) { Trace.Verbose($"Fired cancellation token for job request {currentDispatch.JobId}."); } }))
                {
                    try
                    {
                        Trace.Info($"Waiting WorkerDispather for job {currentDispatch.JobId} run to finish.");
                        await currentDispatch.WorkerDispatch;
                        Trace.Info($"Job request {currentDispatch.JobId} processed succeed.");
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Worker Dispatch failed with an exception for job request {currentDispatch.JobId}.");
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

        public TaskResult GetLocalRunJobResult(AgentJobRequestMessage message)
        {
            return _localRunJobResult.Value[message.RequestId];
        }

        public async Task ShutdownAsync()
        {
            Trace.Info($"Shutting down JobDispatcher. Make sure all WorkerDispatcher has finished.");
            WorkerDispatcher currentDispatch = null;
            if (_jobDispatchedQueue.Count > 0)
            {
                Guid dispatchedJobId = _jobDispatchedQueue.Dequeue();
                if (_jobInfos.TryGetValue(dispatchedJobId, out currentDispatch))
                {
                    try
                    {
                        Trace.Info($"Ensure WorkerDispather for job {currentDispatch.JobId} run to finish, cancel any running job.");
                        await EnsureDispatchFinished(currentDispatch, cancelRunningJob: true);
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

        private async Task EnsureDispatchFinished(WorkerDispatcher jobDispatch, bool cancelRunningJob = false)
        {
            if (!jobDispatch.WorkerDispatch.IsCompleted)
            {
                if (cancelRunningJob)
                {
                    // cancel running job when shutting down the runner.
                    // this will happen when runner get Ctrl+C or message queue loop crashed.
                    jobDispatch.WorkerCancellationTokenSource.Cancel();
                    // wait for worker process exit then return.
                    await jobDispatch.WorkerDispatch;

                    return;
                }

                // base on the current design, server will only send one job for a given runner everytime.
                // if the runner received a new job request while a previous job request is still running, this typically indicate two situations
                // 1. an runner bug cause server and runner mismatch on the state of the job request, ex. runner not renew jobrequest properly but think it still own the job reqest, however server already abandon the jobrequest.
                // 2. a server bug or design change that allow server send more than one job request to an given runner that haven't finish previous job request.
                var runnerServer = HostContext.GetService<IRunnerServer>();
                TaskAgentJobRequest request = null;
                try
                {
                    request = await runnerServer.GetAgentRequestAsync(_poolId, jobDispatch.RequestId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    // we can't even query for the jobrequest from server, something totally busted, stop runner/worker.
                    Trace.Error($"Catch exception while checking jobrequest {jobDispatch.JobId} status. Cancel running worker right away.");
                    Trace.Error(ex);

                    jobDispatch.WorkerCancellationTokenSource.Cancel();
                    // make sure worker process exit before we rethrow, otherwise we might leave orphan worker process behind.
                    await jobDispatch.WorkerDispatch;

                    // rethrow original exception
                    throw;
                }

                if (request.Result != null)
                {
                    // job request has been finished, the server already has result.
                    // this means runner is busted since it still running that request.
                    // cancel the zombie worker, run next job request.
                    Trace.Error($"Received job request while previous job {jobDispatch.JobId} still running on worker. Cancel the previous job since the job request have been finished on server side with result: {request.Result.Value}.");
                    jobDispatch.WorkerCancellationTokenSource.Cancel();

                    // wait 45 sec for worker to finish.
                    Task completedTask = await Task.WhenAny(jobDispatch.WorkerDispatch, Task.Delay(TimeSpan.FromSeconds(45)));
                    if (completedTask != jobDispatch.WorkerDispatch)
                    {
                        // at this point, the job exectuion might encounter some dead lock and even not able to be canclled.
                        // no need to localize the exception string should never happen.
                        throw new InvalidOperationException($"Job dispatch process for {jobDispatch.JobId} has encountered unexpected error, the dispatch task is not able to be canceled within 45 seconds.");
                    }
                }
                else
                {
                    // something seriously wrong on server side. stop runner from continue running.
                    // no need to localize the exception string should never happen.
                    throw new InvalidOperationException($"Server send a new job request while the previous job request {jobDispatch.JobId} haven't finished.");
                }
            }

            try
            {
                await jobDispatch.WorkerDispatch;
                Trace.Info($"Job request {jobDispatch.JobId} processed succeed.");
            }
            catch (Exception ex)
            {
                Trace.Error($"Worker Dispatch failed with an exception for job request {jobDispatch.JobId}.");
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

        private async Task RunOnceAsync(Pipelines.AgentJobRequestMessage message, WorkerDispatcher previousJobDispatch, CancellationToken jobRequestCancellationToken, CancellationToken workerCancelTimeoutKillToken)
        {
            try
            {
                await RunAsync(message, previousJobDispatch, jobRequestCancellationToken, workerCancelTimeoutKillToken);
            }
            finally
            {
                Trace.Info("Fire signal for one time used runner.");
                _runOnceJobCompleted.TrySetResult(true);
            }
        }

        private async Task RunAsync(Pipelines.AgentJobRequestMessage message, WorkerDispatcher previousJobDispatch, CancellationToken jobRequestCancellationToken, CancellationToken workerCancelTimeoutKillToken)
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
            term.WriteLine($"{DateTime.UtcNow:u}: Running job: {message.JobDisplayName}");

            // first job request renew succeed.
            TaskCompletionSource<int> firstJobRequestRenewed = new TaskCompletionSource<int>();
            var notification = HostContext.GetService<IJobNotification>();

            // lock renew cancellation token.
            using (var lockRenewalTokenSource = new CancellationTokenSource())
            using (var workerProcessCancelTokenSource = new CancellationTokenSource())
            {
                long requestId = message.RequestId;
                Guid lockToken = Guid.Empty; // lockToken has never been used, keep this here of compat

                // start renew job request
                Trace.Info($"Start renew job request {requestId} for job {message.JobId}.");
                Task renewJobRequest = RenewJobRequestAsync(_poolId, requestId, lockToken, firstJobRequestRenewed, lockRenewalTokenSource.Token);

                // wait till first renew succeed or job request is canceled
                // not even start worker if the first renew fail
                await Task.WhenAny(firstJobRequestRenewed.Task, renewJobRequest, Task.Delay(-1, jobRequestCancellationToken));

                if (renewJobRequest.IsCompleted)
                {
                    // renew job request task complete means we run out of retry for the first job request renew.
                    Trace.Info($"Unable to renew job request for job {message.JobId} for the first time, stop dispatching job to worker.");
                    return;
                }

                if (jobRequestCancellationToken.IsCancellationRequested)
                {
                    Trace.Info($"Stop renew job request for job {message.JobId}.");
                    // stop renew lock
                    lockRenewalTokenSource.Cancel();
                    // renew job request should never blows up.
                    await renewJobRequest;

                    // complete job request with result Cancelled
                    await CompleteJobRequestAsync(_poolId, message, lockToken, TaskResult.Canceled);
                    return;
                }

                HostContext.WritePerfCounter($"JobRequestRenewed_{requestId.ToString()}");

                Task<int> workerProcessTask = null;
                object _outputLock = new object();
                List<string> workerOutput = new List<string>();
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

                            if (HostContext.RunMode == RunMode.Normal)
                            {
                                // Save STDOUT from worker, worker will use STDOUT report unhandle exception.
                                processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stdout)
                                {
                                    if (!string.IsNullOrEmpty(stdout.Data))
                                    {
                                        lock (_outputLock)
                                        {
                                            workerOutput.Add(stdout.Data);
                                        }
                                    }
                                };

                                // Save STDERR from worker, worker will use STDERR on crash.
                                processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stderr)
                                {
                                    if (!string.IsNullOrEmpty(stderr.Data))
                                    {
                                        lock (_outputLock)
                                        {
                                            workerOutput.Add(stderr.Data);
                                        }
                                    }
                                };
                            }
                            else if (HostContext.RunMode == RunMode.Local)
                            {
                                processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) => Console.WriteLine(e.Data);
                                processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) => Console.WriteLine(e.Data);
                            }

                            // Start the child process.
                            HostContext.WritePerfCounter("StartingWorkerProcess");
                            var assemblyDirectory = HostContext.GetDirectory(WellKnownDirectory.Bin);
                            string workerFileName = Path.Combine(assemblyDirectory, _workerProcessName);
                            workerProcessTask = processInvoker.ExecuteAsync(
                                workingDirectory: assemblyDirectory,
                                fileName: workerFileName,
                                arguments: "spawnclient " + pipeHandleOut + " " + pipeHandleIn,
                                environment: null,
                                requireExitCodeZero: false,
                                outputEncoding: null,
                                killProcessOnCancel: true,
                                redirectStandardIn: null,
                                inheritConsoleHandler: false,
                                keepStandardInOpen: false,
                                highPriorityProcess: true,
                                cancellationToken: workerProcessCancelTokenSource.Token);
                        });

                    // Send the job request message.
                    // Kill the worker process if sending the job message times out. The worker
                    // process may have successfully received the job message.
                    try
                    {
                        Trace.Info($"Send job request message to worker for job {message.JobId}.");
                        HostContext.WritePerfCounter($"RunnerSendingJobToWorker_{message.JobId}");
                        using (var csSendJobRequest = new CancellationTokenSource(_channelTimeout))
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
                        // timeout 30 sec. kill worker.
                        Trace.Info($"Job request message sending for job {message.JobId} been cancelled, kill running worker.");
                        workerProcessCancelTokenSource.Cancel();
                        try
                        {
                            await workerProcessTask;
                        }
                        catch (OperationCanceledException)
                        {
                            Trace.Info("worker process has been killed.");
                        }

                        Trace.Info($"Stop renew job request for job {message.JobId}.");
                        // stop renew lock
                        lockRenewalTokenSource.Cancel();
                        // renew job request should never blows up.
                        await renewJobRequest;

                        // not finish the job request since the job haven't run on worker at all, we will not going to set a result to server.
                        return;
                    }

                    // we get first jobrequest renew succeed and start the worker process with the job message.
                    // send notification to machine provisioner.
                    var systemConnection = message.Resources.Endpoints.SingleOrDefault(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
                    var accessToken = systemConnection?.Authorization?.Parameters["AccessToken"];
                    await notification.JobStarted(message.JobId, accessToken, systemConnection.Url);

                    HostContext.WritePerfCounter($"SentJobToWorker_{requestId.ToString()}");

                    try
                    {
                        TaskResult resultOnAbandonOrCancel = TaskResult.Succeeded;
                        // wait for renewlock, worker process or cancellation token been fired.
                        var completedTask = await Task.WhenAny(renewJobRequest, workerProcessTask, Task.Delay(-1, jobRequestCancellationToken));
                        if (completedTask == workerProcessTask)
                        {
                            // worker finished successfully, complete job request with result, attach unhandled exception reported by worker, stop renew lock, job has finished.
                            int returnCode = await workerProcessTask;
                            Trace.Info($"Worker finished for job {message.JobId}. Code: " + returnCode);

                            string detailInfo = null;
                            if (!TaskResultUtil.IsValidReturnCode(returnCode))
                            {
                                detailInfo = string.Join(Environment.NewLine, workerOutput);
                                Trace.Info($"Return code {returnCode} indicate worker encounter an unhandled exception or app crash, attach worker stdout/stderr to JobRequest result.");
                                await LogWorkerProcessUnhandledException(message, detailInfo);
                            }

                            TaskResult result = TaskResultUtil.TranslateFromReturnCode(returnCode);
                            Trace.Info($"finish job request for job {message.JobId} with result: {result}");
                            term.WriteLine($"{DateTime.UtcNow:u}: Job {message.JobDisplayName} completed with result: {result}");

                            Trace.Info($"Stop renew job request for job {message.JobId}.");
                            // stop renew lock
                            lockRenewalTokenSource.Cancel();
                            // renew job request should never blows up.
                            await renewJobRequest;

                            // complete job request
                            await CompleteJobRequestAsync(_poolId, message, lockToken, result, detailInfo);

                            // print out unhandled exception happened in worker after we complete job request.
                            // when we run out of disk space, report back to server has higher priority.
                            if (!string.IsNullOrEmpty(detailInfo))
                            {
                                Trace.Error("Unhandled exception happened in worker:");
                                Trace.Error(detailInfo);
                            }

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
                        // cancel worker gracefully first, then kill it after worker cancel timeout
                        try
                        {
                            Trace.Info($"Send job cancellation message to worker for job {message.JobId}.");
                            using (var csSendCancel = new CancellationTokenSource(_channelTimeout))
                            {
                                var messageType = MessageType.CancelRequest;
                                if (HostContext.RunnerShutdownToken.IsCancellationRequested)
                                {
                                    switch (HostContext.RunnerShutdownReason)
                                    {
                                        case ShutdownReason.UserCancelled:
                                            messageType = MessageType.RunnerShutdown;
                                            break;
                                        case ShutdownReason.OperatingSystemShutdown:
                                            messageType = MessageType.OperatingSystemShutdown;
                                            break;
                                    }
                                }

                                await processChannel.SendAsync(
                                    messageType: messageType,
                                    body: string.Empty,
                                    cancellationToken: csSendCancel.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // message send been cancelled.
                            Trace.Info($"Job cancel message sending for job {message.JobId} been cancelled, kill running worker.");
                            workerProcessCancelTokenSource.Cancel();
                            try
                            {
                                await workerProcessTask;
                            }
                            catch (OperationCanceledException)
                            {
                                Trace.Info("worker process has been killed.");
                            }
                        }

                        // wait worker to exit 
                        // if worker doesn't exit within timeout, then kill worker.
                        completedTask = await Task.WhenAny(workerProcessTask, Task.Delay(-1, workerCancelTimeoutKillToken));

                        // worker haven't exit within cancellation timeout.
                        if (completedTask != workerProcessTask)
                        {
                            Trace.Info($"worker process for job {message.JobId} haven't exit within cancellation timout, kill running worker.");
                            workerProcessCancelTokenSource.Cancel();
                            try
                            {
                                await workerProcessTask;
                            }
                            catch (OperationCanceledException)
                            {
                                Trace.Info("worker process has been killed.");
                            }
                        }

                        Trace.Info($"finish job request for job {message.JobId} with result: {resultOnAbandonOrCancel}");
                        term.WriteLine($"{DateTime.UtcNow:u}: Job {message.JobDisplayName} completed with result: {resultOnAbandonOrCancel}");
                        // complete job request with cancel result, stop renew lock, job has finished.

                        Trace.Info($"Stop renew job request for job {message.JobId}.");
                        // stop renew lock
                        lockRenewalTokenSource.Cancel();
                        // renew job request should never blows up.
                        await renewJobRequest;

                        // complete job request
                        await CompleteJobRequestAsync(_poolId, message, lockToken, resultOnAbandonOrCancel);
                    }
                    finally
                    {
                        // This should be the last thing to run so we don't notify external parties until actually finished
                        await notification.JobCompleted(message.JobId);
                    }
                }
            }
        }

        public async Task RenewJobRequestAsync(int poolId, long requestId, Guid lockToken, TaskCompletionSource<int> firstJobRequestRenewed, CancellationToken token)
        {
            var runnerServer = HostContext.GetService<IRunnerServer>();
            TaskAgentJobRequest request = null;
            int firstRenewRetryLimit = 5;
            int encounteringError = 0;

            // renew lock during job running.
            // stop renew only if cancellation token for lock renew task been signal or exception still happen after retry.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    request = await runnerServer.RenewAgentRequestAsync(poolId, requestId, lockToken, token);

                    Trace.Info($"Successfully renew job request {requestId}, job is valid till {request.LockedUntil.Value}");

                    if (!firstJobRequestRenewed.Task.IsCompleted)
                    {
                        // fire first renew succeed event.
                        firstJobRequestRenewed.TrySetResult(0);
                    }

                    if (encounteringError > 0)
                    {
                        encounteringError = 0;
                        runnerServer.SetConnectionTimeout(RunnerConnectionType.JobRequest, TimeSpan.FromSeconds(60));
                        HostContext.WritePerfCounter("JobRenewRecovered");
                    }

                    // renew again after 60 sec delay
                    await HostContext.Delay(TimeSpan.FromSeconds(60), token);
                }
                catch (TaskAgentJobNotFoundException)
                {
                    // no need for retry. the job is not valid anymore.
                    Trace.Info($"TaskAgentJobNotFoundException received when renew job request {requestId}, job is no longer valid, stop renew job request.");
                    return;
                }
                catch (TaskAgentJobTokenExpiredException)
                {
                    // no need for retry. the job is not valid anymore.
                    Trace.Info($"TaskAgentJobTokenExpiredException received renew job request {requestId}, job is no longer valid, stop renew job request.");
                    return;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // OperationCanceledException may caused by http timeout or _lockRenewalTokenSource.Cance();
                    // Stop renew only on cancellation token fired.
                    Trace.Info($"job renew has been canceled, stop renew job request {requestId}.");
                    return;
                }
                catch (Exception ex)
                {
                    Trace.Error($"Catch exception during renew runner jobrequest {requestId}.");
                    Trace.Error(ex);
                    encounteringError++;

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
                        // retry till reach lockeduntil + 5 mins extra buffer.
                        remainingTime = request.LockedUntil.Value + TimeSpan.FromMinutes(5) - DateTime.UtcNow;
                    }

                    if (remainingTime > TimeSpan.Zero)
                    {
                        TimeSpan delayTime;
                        if (!firstJobRequestRenewed.Task.IsCompleted)
                        {
                            Trace.Info($"Retrying lock renewal for jobrequest {requestId}. The first job renew request has failed.");
                            delayTime = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
                        }
                        else
                        {
                            Trace.Info($"Retrying lock renewal for jobrequest {requestId}. Job is valid until {request.LockedUntil.Value}.");
                            if (encounteringError > 5)
                            {
                                delayTime = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
                            }
                            else
                            {
                                delayTime = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                            }
                        }

                        // Re-establish connection to server in order to avoid affinity with server.
                        // Reduce connection timeout to 30 seconds (from 60s)
                        HostContext.WritePerfCounter("ResetJobRenewConnection");
                        await runnerServer.RefreshConnectionAsync(RunnerConnectionType.JobRequest, TimeSpan.FromSeconds(30));

                        try
                        {
                            // back-off before next retry.
                            await HostContext.Delay(delayTime, token);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            Trace.Info($"job renew has been canceled, stop renew job request {requestId}.");
                        }
                    }
                    else
                    {
                        Trace.Info($"Lock renewal has run out of retry, stop renew lock for jobrequest {requestId}.");
                        HostContext.WritePerfCounter("JobRenewReachLimit");
                        return;
                    }
                }
            }
        }

        // TODO: We need send detailInfo back to DT in order to add an issue for the job
        private async Task CompleteJobRequestAsync(int poolId, Pipelines.AgentJobRequestMessage message, Guid lockToken, TaskResult result, string detailInfo = null)
        {
            Trace.Entering();
            if (HostContext.RunMode == RunMode.Local)
            {
                _localRunJobResult.Value[message.RequestId] = result;
                return;
            }

            if (PlanUtil.GetFeatures(message.Plan).HasFlag(PlanFeatures.JobCompletedPlanEvent))
            {
                Trace.Verbose($"Skip FinishAgentRequest call from Listener because Plan version is {message.Plan.Version}");
                return;
            }

            var runnerServer = HostContext.GetService<IRunnerServer>();
            int completeJobRequestRetryLimit = 5;
            List<Exception> exceptions = new List<Exception>();
            while (completeJobRequestRetryLimit-- > 0)
            {
                try
                {
                    await runnerServer.FinishAgentRequestAsync(poolId, message.RequestId, lockToken, DateTime.UtcNow, result, CancellationToken.None);
                    return;
                }
                catch (TaskAgentJobNotFoundException)
                {
                    Trace.Info($"TaskAgentJobNotFoundException received, job {message.JobId} is no longer valid.");
                    return;
                }
                catch (TaskAgentJobTokenExpiredException)
                {
                    Trace.Info($"TaskAgentJobTokenExpiredException received, job {message.JobId} is no longer valid.");
                    return;
                }
                catch (Exception ex)
                {
                    Trace.Error($"Catch exception during complete runner jobrequest {message.RequestId}.");
                    Trace.Error(ex);
                    exceptions.Add(ex);
                }

                // delay 5 seconds before next retry.
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            // rethrow all catched exceptions during retry.
            throw new AggregateException(exceptions);
        }

        // log an error issue to job level timeline record
        private async Task LogWorkerProcessUnhandledException(Pipelines.AgentJobRequestMessage message, string errorMessage)
        {
            try
            {
                var systemConnection = message.Resources.Endpoints.SingleOrDefault(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection));
                ArgUtil.NotNull(systemConnection, nameof(systemConnection));

                var jobServer = HostContext.GetService<IJobServer>();
                VssCredentials jobServerCredential = VssUtil.GetVssCredential(systemConnection);
                Uri jobServerUrl = systemConnection.Url;

                // Make sure SystemConnection Url match Config Url base for OnPremises server
                if (!message.Variables.ContainsKey(Constants.Variables.System.ServerType) ||
                    string.Equals(message.Variables[Constants.Variables.System.ServerType]?.Value, "OnPremises", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Uri result = null;
                        Uri configUri = new Uri(_runnerSetting.ServerUrl);
                        if (Uri.TryCreate(new Uri(configUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)), jobServerUrl.PathAndQuery, out result))
                        {
                            //replace the schema and host portion of messageUri with the host from the
                            //server URI (which was set at config time)
                            jobServerUrl = result;
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        //cannot parse the Uri - not a fatal error
                        Trace.Error(ex);
                    }
                    catch (UriFormatException ex)
                    {
                        //cannot parse the Uri - not a fatal error
                        Trace.Error(ex);
                    }
                }

                VssConnection jobConnection = VssUtil.CreateConnection(jobServerUrl, jobServerCredential);
                await jobServer.ConnectAsync(jobConnection);
                var timeline = await jobServer.GetTimelineAsync(message.Plan.ScopeIdentifier, message.Plan.PlanType, message.Plan.PlanId, message.Timeline.Id, CancellationToken.None);
                ArgUtil.NotNull(timeline, nameof(timeline));
                TimelineRecord jobRecord = timeline.Records.FirstOrDefault(x => x.Id == message.JobId && x.RecordType == "Job");
                ArgUtil.NotNull(jobRecord, nameof(jobRecord));
                jobRecord.ErrorCount++;
                jobRecord.Issues.Add(new Issue() { Type = IssueType.Error, Message = errorMessage });
                await jobServer.UpdateTimelineRecordsAsync(message.Plan.ScopeIdentifier, message.Plan.PlanType, message.Plan.PlanId, message.Timeline.Id, new TimelineRecord[] { jobRecord }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Trace.Error("Fail to report unhandled exception from Runner.Worker process");
                Trace.Error(ex);
            }
        }

        private class WorkerDispatcher : IDisposable
        {
            public long RequestId { get; }
            public Guid JobId { get; }
            public Task WorkerDispatch { get; set; }
            public CancellationTokenSource WorkerCancellationTokenSource { get; private set; }
            public CancellationTokenSource WorkerCancelTimeoutKillTokenSource { get; private set; }
            private readonly object _lock = new object();

            public WorkerDispatcher(Guid jobId, long requestId)
            {
                JobId = jobId;
                RequestId = requestId;
                WorkerCancelTimeoutKillTokenSource = new CancellationTokenSource();
                WorkerCancellationTokenSource = new CancellationTokenSource();
            }

            public bool Cancel(TimeSpan timeout)
            {
                if (WorkerCancellationTokenSource != null && WorkerCancelTimeoutKillTokenSource != null)
                {
                    lock (_lock)
                    {
                        if (WorkerCancellationTokenSource != null && WorkerCancelTimeoutKillTokenSource != null)
                        {
                            WorkerCancellationTokenSource.Cancel();

                            // make sure we have at least 60 seconds for cancellation.
                            if (timeout.TotalSeconds < 60)
                            {
                                timeout = TimeSpan.FromSeconds(60);
                            }

                            WorkerCancelTimeoutKillTokenSource.CancelAfter(timeout.Subtract(TimeSpan.FromSeconds(15)));
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
                    if (WorkerCancellationTokenSource != null || WorkerCancelTimeoutKillTokenSource != null)
                    {
                        lock (_lock)
                        {
                            if (WorkerCancellationTokenSource != null)
                            {
                                WorkerCancellationTokenSource.Dispose();
                                WorkerCancellationTokenSource = null;
                            }

                            if (WorkerCancelTimeoutKillTokenSource != null)
                            {
                                WorkerCancelTimeoutKillTokenSource.Dispose();
                                WorkerCancelTimeoutKillTokenSource = null;
                            }
                        }
                    }
                }
            }
        }
    }
}
