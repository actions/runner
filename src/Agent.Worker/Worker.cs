using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(Worker))]
    public interface IWorker : IAgentService
    {
        Task<int> RunAsync(string pipeIn, string pipeOut);
    }

    public sealed class Worker : AgentService, IWorker
    {
        private readonly TimeSpan _workerStartTimeout = TimeSpan.FromSeconds(30);

        public async Task<int> RunAsync(string pipeIn, string pipeOut)
        {
            // Validate args.
            ArgUtil.NotNullOrEmpty(pipeIn, nameof(pipeIn));
            ArgUtil.NotNullOrEmpty(pipeOut, nameof(pipeOut));
            var agentWebProxy = HostContext.GetService<IVstsAgentWebProxy>();
            var agentCertManager = HostContext.GetService<IAgentCertificateManager>();
            VssUtil.InitializeVssClientSettings(HostContext.UserAgent, agentWebProxy.WebProxy, agentCertManager.VssClientCertificateManager);

            var jobRunner = HostContext.CreateService<IJobRunner>();

            using (var channel = HostContext.CreateService<IProcessChannel>())
            using (var jobRequestCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(HostContext.AgentShutdownToken))
            using (var channelTokenSource = new CancellationTokenSource())
            {
                // Start the channel.
                channel.StartClient(pipeIn, pipeOut);

                // Wait for up to 30 seconds for a message from the channel.
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
                var jobMessage = JsonUtility.FromString<Pipelines.AgentJobRequestMessage>(channelMessage.Body);
                ArgUtil.NotNull(jobMessage, nameof(jobMessage));

                // Initialize the secret masker and set the thread culture.
                InitializeSecretMasker(jobMessage);
                SetCulture(jobMessage);

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
                    case MessageType.AgentShutdown:
                        HostContext.ShutdownAgent(ShutdownReason.UserCancelled);
                        break;
                    case MessageType.OperatingSystemShutdown:
                        HostContext.ShutdownAgent(ShutdownReason.OperatingSystemShutdown);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(channelMessage.MessageType), channelMessage.MessageType, nameof(channelMessage.MessageType));
                }

                // Await the job.
                return TaskResultUtil.TranslateToReturnCode(await jobRunnerTask);
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
                if (variable.Value.IsSecret)
                {
                    HostContext.SecretMasker.AddValue(variable.Value.Value);
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

            // Add masks for secure file download tickets
            foreach (SecureFile file in message.Resources.SecureFiles ?? new List<SecureFile>())
            {
                if (!string.IsNullOrEmpty(file.Ticket))
                {
                    HostContext.SecretMasker.AddValue(file.Ticket);
                }
            }
        }

        private void SetCulture(Pipelines.AgentJobRequestMessage message)
        {
            // Extract the culture name from the job's variable dictionary.
            // The variable does not exist for TFS 2015 RTM and Update 1.
            // It was introduced in Update 2.
            VariableValue culture;
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Variables, nameof(message.Variables));
            if (message.Variables.TryGetValue(Constants.Variables.System.Culture, out culture))
            {
                // Set the default thread culture.
                HostContext.SetDefaultCulture(culture.Value);
            }
        }
    }
}
