using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(Worker))]
    public interface IWorker : IAgentService
    {
        Task<int> RunAsync(string pipeIn, string pipeOut, CancellationTokenSource hostTokenSource);
    }

    public sealed class Worker : AgentService, IWorker
    {
        private readonly TimeSpan WorkerStartTimeout = TimeSpan.FromSeconds(30);

        private void InitializeSecrets(JobRequestMessage message)
        {
            var secretMasker = HostContext.GetService<ISecretMasker>();
            
            // Add mask hints
            if (message?.Environment?.MaskHints != null)
            {
                IDictionary<string, string> variables = message?.Environment?.Variables;

                foreach (MaskHint maskHint in message.Environment.MaskHints)
                {
                    if (maskHint.Type == MaskType.Regex)
                    {
                        secretMasker.Add(new RegexMask(maskHint.Value));
                    }
                    else if (maskHint.Type == MaskType.Variable && variables != null)
                    {
                        string value;
                        if (variables.TryGetValue(maskHint.Value, out value))
                        {
                            if (!string.IsNullOrEmpty(value))
                            {
                                secretMasker.Add(new VariableValueMask(maskHint.Value, value));
                            }
                        }
                    }
                }
            }

            // Add masks for service endpoints
            if (message?.Environment?.Endpoints != null)
            {
                foreach (ServiceEndpoint endpoint in message.Environment.Endpoints)
                {
                    if (endpoint.Authorization != null &&
                        endpoint.Authorization.Parameters != null)
                    {
                        foreach (string value in endpoint.Authorization.Parameters.Values)
                        {
                            secretMasker.Add(new ValueMask(value));
                            if (!Uri.EscapeDataString(value).Equals(value, StringComparison.OrdinalIgnoreCase))
                            {
                                secretMasker.Add(new ValueMask(Uri.EscapeDataString(value)));
                            }
                        }
                    }
                }
            }
        }

        public async Task<int> RunAsync(string pipeIn, string pipeOut, CancellationTokenSource hostTokenSource)
        {
            // Validate args.
            ArgUtil.NotNullOrEmpty(pipeIn, nameof(pipeIn));
            ArgUtil.NotNullOrEmpty(pipeOut, nameof(pipeOut));
            ArgUtil.NotNull(hostTokenSource, nameof(hostTokenSource));
            var jobRunner = HostContext.GetService<IJobRunner>();

            using (var channel = HostContext.CreateService<IProcessChannel>())
            {
                // Start the channel.
                channel.StartClient(pipeIn, pipeOut);

                // Wait for up to 30 seconds for a message from the channel.
                Trace.Info("Waiting to receive the job message from the channel.");
                WorkerMessage channelMessage = await channel.ReceiveAsync(new CancellationTokenSource(WorkerStartTimeout).Token);

                // Deserialize the job message.
                Trace.Info("Message received.");
                ArgUtil.Equal(MessageType.NewJobRequest, channelMessage.MessageType, nameof(channelMessage.MessageType));
                ArgUtil.NotNullOrEmpty(channelMessage.Body, nameof(channelMessage.Body));
                var jobMessage = JsonUtility.FromString<JobRequestMessage>(channelMessage.Body);
                ArgUtil.NotNull(jobMessage, nameof(jobMessage));

                // Set the default thread culture.
                string culture;
                ArgUtil.NotNull(jobMessage.Environment, nameof(jobMessage.Environment));
                ArgUtil.NotNull(jobMessage.Environment.Variables, nameof(jobMessage.Environment.Variables));
                if (!jobMessage.Environment.Variables.TryGetValue(Constants.Variables.System.Culture, out culture))
                {
                    culture = null;
                }

                ArgUtil.NotNullOrEmpty(culture, nameof(culture));
                HostContext.SetDefaultCulture(culture);

                //initialize secret masks
                InitializeSecrets(jobMessage);

                Trace.Verbose($"JobMessage: {channelMessage.Body}");

                // Start the job.
                Task<TaskResult> jobRunnerTask = jobRunner.RunAsync(jobMessage);

                // Start listening for a cancel message from the channel.
                Trace.Info("Listening for cancel message from the channel.");
                CancellationTokenSource channelTokenSource = new CancellationTokenSource();
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
                hostTokenSource.Cancel();   // Expire the host cancellation token.
                // Await the job.
                return TaskResultUtil.TranslateToReturnCode(await jobRunnerTask);
            }
        }
    }
}
