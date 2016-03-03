using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(Agent))]
    public interface IAgent : IAgentService
    {
        Task RunAsync();
    }

    public sealed class Agent : AgentService, IAgent
    {
        private int _poolId;
        private Guid _sessionId = Guid.Empty;

        private async Task DeleteMessageAsync(TaskAgentMessage message)
        {
            if (message != null && _sessionId != Guid.Empty)
            {
                //TODO: decide what tokens to use if HostCotnext is already canceled
                var agentServer = HostContext.GetService<IAgentServer>();
                var cancellationToken = HostContext.CancellationToken;
                CancellationTokenSource tokenSource = null;
                if (cancellationToken.IsCancellationRequested)
                {
                    tokenSource = new CancellationTokenSource();
                    cancellationToken = tokenSource.Token;
                }
                await agentServer.DeleteAgentMessageAsync(_poolId, message.MessageId, _sessionId, cancellationToken);
            }
        }

        //create worker manager, create message listener and start listening to the queue
        public async Task RunAsync()
        {
            var configManager = HostContext.GetService<IConfigurationManager>();
            AgentSettings settings = configManager.LoadSettings();
            _poolId = settings.PoolId;


            var listener = HostContext.GetService<IMessageListener>();
            if (!await listener.CreateSessionAsync())
            {
                return;
            }
            Console.WriteLine("Listening for messages");
            _sessionId = listener.Session.SessionId;
            TaskAgentMessage message = null;
            try
            {
                using (var workerManager = HostContext.GetService<IWorkerManager>())
                {
                    while (true)
                    {
                        try
                        {
                            HostContext.CancellationToken.ThrowIfCancellationRequested();
                            message = await listener.GetNextMessageAsync(); //get next message

                            if (String.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                Trace.Warning("Referesh message received, but not yet handled by agent implementation.");
                            }
                            else if (String.Equals(message.MessageType, JobRequestMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var newJobMessage = JsonUtility.FromString<JobRequestMessage>(message.Body);
                                await workerManager.Run(newJobMessage);
                            }
                            else if (String.Equals(message.MessageType, JobCancelMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var cancelJobMessage = JsonUtility.FromString<JobCancelMessage>(message.Body);
                                workerManager.Cancel(cancelJobMessage);
                            }
                        }
                        finally
                        {
                            if (message != null)
                            {
                                //TODO: make sure we don't mask more important exception
                                await DeleteMessageAsync(message);
                                message = null;
                            }
                        }
                    }
                }
            }
            finally
            {
                //TODO: make sure we don't mask more important exception
                await listener.DeleteSessionAsync();
            }
        }
    }
}
