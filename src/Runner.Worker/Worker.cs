using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using Newtonsoft.Json;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(Worker))]
    public interface IWorker : IRunnerService
    {
        Task<int> RunAsync(string pipeIn, string pipeOut);
    }

    public sealed class Worker : RunnerService, IWorker
    {
        private readonly TimeSpan _workerStartTimeout = TimeSpan.FromSeconds(30);
        private ManualResetEvent _completedCommand = new(false);

        // Do not mask the values of these secrets
        private static HashSet<String> SecretVariableMaskWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            Constants.Variables.Actions.StepDebug,
            Constants.Variables.Actions.RunnerDebug
        };

        public async Task<int> RunAsync(string pipeIn, string pipeOut)
        {
            try
            {
                // Setup way to handle SIGTERM/unloading signals
                _completedCommand.Reset();
                HostContext.Unloading += Worker_Unloading;

                // Validate args.
                ArgUtil.NotNullOrEmpty(pipeIn, nameof(pipeIn));
                ArgUtil.NotNullOrEmpty(pipeOut, nameof(pipeOut));
                VssUtil.InitializeVssClientSettings(HostContext.UserAgents, HostContext.WebProxy);
                var jobRunner = HostContext.CreateService<IJobRunner>();
                var terminal = HostContext.GetService<ITerminal>();

                await using (var secretNotifier = HostContext.GetService<IVSockSecretNotifier>())
                using (var channel = HostContext.CreateService<IProcessChannel>())
                using (var jobRequestCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(HostContext.RunnerShutdownToken))
                using (var channelTokenSource = new CancellationTokenSource())
                {
                    // Start the channel.
                    channel.StartClient(pipeIn, pipeOut);

                    // Wait for up to 30 seconds for a message from the channel.
                    HostContext.WritePerfCounter("WorkerWaitingForJobMessage");
                    Trace.Info("Waiting to receive the job message from the channel.");
                    WorkerMessage channelMessage;
                    using (var csChannelMessage = new CancellationTokenSource(_workerStartTimeout))
                    {
                        channelMessage = await channel.ReceiveAsync(csChannelMessage.Token);
                    }

                    // Deserialize the job message.
                    Trace.Info("Message received.");
                    ArgUtil.Equal(MessageType.NewJobRequest, channelMessage.MessageType, nameof(channelMessage.MessageType));
                    ArgUtil.NotNullOrEmpty(channelMessage.Body, nameof(channelMessage.Body));
                    Pipelines.AgentJobRequestMessage jobMessage = null;
                    try
                    {
                        jobMessage = StringUtil.ConvertFromJson<Pipelines.AgentJobRequestMessage>(channelMessage.Body);
                    }
                    catch (JsonReaderException ex)
                    {
                        if (channelMessage.Body.Length > ex.LinePosition + 10)
                        {
                            var errorChunk = channelMessage.Body.Substring(ex.LinePosition - 10, 20);
                            terminal.WriteError($"Worker received invalid Json at position '{ex.LinePosition}': {errorChunk} ({Convert.ToBase64String(Encoding.UTF8.GetBytes(errorChunk))})");
                        }

                        throw;
                    }

                    ArgUtil.NotNull(jobMessage, nameof(jobMessage));
                    HostContext.WritePerfCounter($"WorkerJobMessageReceived_{jobMessage.RequestId.ToString()}");

                    // Initialize the secret masker and set the thread culture.
                    if (Constants.Runner.Platform == Constants.OSPlatform.Linux &&
                        secretNotifier.TryStartNotifier())
                    {
                        HostContext.SecretMasker.NewSecretAdded += (sender, e) =>
                        {
                            secretNotifier.NotifyNewSecret(e);
                        };
                    }
                    InitializeSecretMasker(jobMessage);
                    SetCulture(jobMessage);
#if !OS_WINDOWS
                    // Steps have not started yet, so every process they launch inherits the new process group.
                    if (jobMessage.Variables.TryGetValue(Constants.Runner.Features.WorkerOwnProcessGroup, out VariableValue ownProcessGroup) &&
                        StringUtil.ConvertToBoolean(ownProcessGroup?.Value))
                    {
                        MoveToOwnProcessGroup();
                    }
#endif

                    // Start the job.
                    Trace.Info($"Job message:{Environment.NewLine} {StringUtil.ConvertToJson(jobMessage)}");
                    Task<TaskResult> jobRunnerTask = jobRunner.RunAsync(jobMessage, jobRequestCancellationToken.Token);

                    // Start listening for a cancel message from the channel.
                    Trace.Info("Listening for cancel message from the channel.");
                    Task<WorkerMessage> channelTask = channel.ReceiveAsync(channelTokenSource.Token);

                    // Wait for one of the tasks to complete.
                    Trace.Info("Waiting for the job to complete or for a cancel message from the channel.");
                    Task.WaitAny(jobRunnerTask, channelTask);
                    // Handle if the job completed.
                    if (jobRunnerTask.IsCompleted)
                    {
                        Trace.Info("Job completed.");
                        channelTokenSource.Cancel(); // Cancel waiting for a message from the channel.
                        return TaskResultUtil.TranslateToReturnCode(await jobRunnerTask);
                    }

                    // Otherwise a cancel message was received from the channel.
                    Trace.Info("Cancellation/Shutdown message received.");
                    channelMessage = await channelTask;
                    switch (channelMessage.MessageType)
                    {
                        case MessageType.CancelRequest:
                            jobRequestCancellationToken.Cancel();   // Expire the host cancellation token.
                            break;
                        case MessageType.RunnerShutdown:
                            HostContext.ShutdownRunner(ShutdownReason.UserCancelled);
                            break;
                        case MessageType.OperatingSystemShutdown:
                            HostContext.ShutdownRunner(ShutdownReason.OperatingSystemShutdown);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(channelMessage.MessageType), channelMessage.MessageType, nameof(channelMessage.MessageType));
                    }

                    // Await the job.
                    return TaskResultUtil.TranslateToReturnCode(await jobRunnerTask);
                }
            }
            finally
            {
                HostContext.Unloading -= Worker_Unloading;
                _completedCommand.Set();
            }
        }

        private void InitializeSecretMasker(Pipelines.AgentJobRequestMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Resources, nameof(message.Resources));

            // Add mask hints for secret variables
            foreach (var variable in (message.Variables ?? new Dictionary<string, VariableValue>()))
            {
                // Need to ignore values on whitelist
                if (variable.Value.IsSecret && !SecretVariableMaskWhitelist.Contains(variable.Key))
                {
                    var value = variable.Value.Value?.Trim() ?? string.Empty;

                    // Add the entire value, even if it contains CR or LF. During expression tracing,
                    // invidual trace info may contain line breaks.
                    HostContext.SecretMasker.AddValue(value);

                    // Also add each individual line. Typically individual lines are processed from STDOUT of child processes.
                    var split = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var item in split)
                    {
                        HostContext.SecretMasker.AddValue(item);
                    }
                }
            }

            // Add mask hints
            foreach (MaskHint maskHint in (message.MaskHints ?? new List<MaskHint>()))
            {
                if (maskHint.Type == MaskType.Regex)
                {
                    HostContext.SecretMasker.AddRegex(maskHint.Value);

                    // We need this because the worker will print out the job message JSON to diag log
                    // and SecretMasker has JsonEscapeEncoder hook up
                    HostContext.SecretMasker.AddValue(maskHint.Value);
                }
                else
                {
                    // TODO: Should we fail instead? Do any additional pains need to be taken here? Should the job message not be traced?
                    Trace.Warning($"Unsupported mask type '{maskHint.Type}'.");
                }
            }

            // TODO: Avoid adding redundant secrets. If the endpoint auth matches the system connection, then it's added as a value secret and as a regex secret. Once as a value secret b/c of the following code that iterates over each endpoint. Once as a regex secret due to the hint sent down in the job message.

            // Add masks for service endpoints
            foreach (ServiceEndpoint endpoint in message.Resources.Endpoints ?? new List<ServiceEndpoint>())
            {
                foreach (string value in endpoint.Authorization?.Parameters?.Values ?? new string[0])
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        HostContext.SecretMasker.AddValue(value);
                    }
                }
            }
        }

        private void SetCulture(Pipelines.AgentJobRequestMessage message)
        {
            // Extract the culture name from the job's variable dictionary.
            VariableValue culture;
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Variables, nameof(message.Variables));
            if (message.Variables.TryGetValue(Constants.Variables.System.Culture, out culture))
            {
                // Set the default thread culture.
                HostContext.SetDefaultCulture(culture.Value);
            }
        }

        /// <summary>
        /// Without a controlling terminal, Runner.Worker inherits the process group of whatever launched the runner,
        /// so job steps share a process group with Runner.Listener and its supervisor. That group is orphaned, which means
        /// a step that leaves a stopped process behind when another process in the group exits (for example .NET's
        /// Process.Kill(entireProcessTree: true), which SIGSTOPs before it kills) makes the kernel send SIGHUP and SIGCONT
        /// to the whole group, killing the runner. Moving the worker into its own group keeps those signals away from
        /// Runner.Listener, and the new group is not orphaned while the listener (same session, other group) is its parent.
        /// With a controlling terminal, Ctrl+C and job control already behave, so the process group is left as it is.
        /// With RUNNER_MANUALLY_TRAP_SIG, run.sh starts the runner in a job-control group that is not orphaned and
        /// forwards SIGINT to that whole group, so the process group is left as it is there too.
        /// https://github.com/actions/runner-images/issues/14833
        /// </summary>
        internal void MoveToOwnProcessGroup()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUNNER_MANUALLY_TRAP_SIG")))
            {
                Trace.Info("RUNNER_MANUALLY_TRAP_SIG is set, keep the current process group.");
                return;
            }

            try
            {
                var native = HostContext.GetService<IProcessGroupNative>();
                int terminalErrno = native.ProbeControllingTerminal();
                if (terminalErrno == 0)
                {
                    Trace.Info("Controlling terminal found, keep the current process group.");
                    return;
                }

                // Only ENXIO (no controlling terminal) and ENOENT (no /dev/tty at all) mean there is no terminal.
                // Any other failure leaves the answer unknown, so keep the current process group.
                if (terminalErrno != ProcessGroupNative.ENXIO && terminalErrno != ProcessGroupNative.ENOENT)
                {
                    Trace.Info($"Unable to check for a controlling terminal, errno {terminalErrno}, keep the current process group.");
                    return;
                }

                int setpgidErrno = native.MoveToNewProcessGroup();
                if (setpgidErrno == 0)
                {
                    Trace.Info($"Moved into own process group {native.GetProcessGroup()}.");
                }
                else
                {
                    Trace.Warning($"Failed to move into own process group, errno {setpgidErrno}.");
                }
            }
            catch (Exception ex)
            {
                // Best effort, the job can still run in the inherited process group.
                Trace.Warning($"Failed to move into own process group: {ex.Message}");
            }
        }

        private void Worker_Unloading(object sender, EventArgs e)
        {
            if (!HostContext.RunnerShutdownToken.IsCancellationRequested)
            {
                HostContext.ShutdownRunner(ShutdownReason.UserCancelled);
                _completedCommand.WaitOne(Constants.Runner.ExitOnUnloadTimeout);
            }
        }
    }
}
