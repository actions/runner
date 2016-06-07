using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            WebProxy.ApplyProxySettings();
            var jobRunner = HostContext.GetService<IJobRunner>();

            using (var channel = HostContext.CreateService<IProcessChannel>())
            using (var jobRequestCancellationToken = new CancellationTokenSource())
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
                var jobMessage = JsonUtility.FromString<JobRequestMessage>(channelMessage.Body);
                ArgUtil.NotNull(jobMessage, nameof(jobMessage));

                // Initialize the secret masker and set the thread culture.
                InitializeSecretMasker(jobMessage);
                SetCulture(jobMessage);

                // Start the job.
                Trace.Info($"Job message: {channelMessage.Body}");
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
                Trace.Info("Cancellation message received.");
                channelMessage = await channelTask;
                ArgUtil.Equal(MessageType.CancelRequest, channelMessage.MessageType, nameof(channelMessage.MessageType));
                jobRequestCancellationToken.Cancel();   // Expire the host cancellation token.
                // Await the job.
                return TaskResultUtil.TranslateToReturnCode(await jobRunnerTask);
            }
        }

        private void InitializeSecretMasker(JobRequestMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            var secretMasker = HostContext.GetService<ISecretMasker>();

            // Add mask hints
            var variables = message?.Environment?.Variables ?? new Dictionary<string, string>();
            foreach (MaskHint maskHint in (message.Environment.MaskHints ?? new List<MaskHint>()))
            {
                if (maskHint.Type == MaskType.Regex)
                {
                    secretMasker.AddRegex(maskHint.Value);

                    // Also add the JSON escaped string since the job message is traced in the diag log.
                    secretMasker.AddValue(JsonConvert.ToString(maskHint.Value ?? string.Empty));
                }
                else if (maskHint.Type == MaskType.Variable)
                {
                    string value;
                    if (variables.TryGetValue(maskHint.Value, out value) &&
                        !string.IsNullOrEmpty(value))
                    {
                        secretMasker.AddValue(value);

                        // Also add the JSON escaped string since the job message is traced in the diag log.
                        secretMasker.AddValue(JsonConvert.ToString(value));
                    }
                }
                else
                {
                    // TODO: Should we fail instead? Do any additional pains need to be taken here? Should the job message not be traced?
                    Trace.Warning($"Unsupported mask type '{maskHint.Type}'.");
                }
            }

            // TODO: Avoid adding redundant secrets. If the endpoint auth matches the system connection, then it's added as a value secret and as a regex secret. Once as a value secret b/c of the following code that iterates over each endpoint. Once as a regex secret due to the hint sent down in the job message.

            // Add masks for service endpoints
            foreach (ServiceEndpoint endpoint in message.Environment.Endpoints ?? new List<ServiceEndpoint>())
            {
                foreach (string value in endpoint.Authorization?.Parameters?.Values ?? new string[0])
                {
                    secretMasker.AddValue(value);

                    // This is precautionary if the secret is used in an URL. For example, if "allow scripts
                    // access to OAuth token" is checked, then the repository auth key is injected into the
                    // URL for a Git repository's remote configuration.
                    if (!Uri.EscapeDataString(value).Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        secretMasker.AddValue(Uri.EscapeDataString(value));
                    }
                }
            }
        }

        private void SetCulture(JobRequestMessage message)
        {
            // Extract the culture name from the job's variable dictionary.
            // The variable does not exist for TFS 2015 RTM and Update 1.
            // It was introduced in Update 2.
            string culture;
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));
            if (message.Environment.Variables.TryGetValue(Constants.Variables.System.Culture, out culture))
            {
                // Set the default thread culture.
                HostContext.SetDefaultCulture(culture);
            }
        }
    }
}
