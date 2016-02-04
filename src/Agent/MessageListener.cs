using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    public sealed class MessageListener
    {
        public MessageListener(Int32 poolId) : this(poolId, new HostContext(), new MessageDispatcher(), new TaskServer())
        {
        }

        public MessageListener(Int32 poolId, IHostContext context, IMessageDispatcher dispatcher, ITaskServer taskServer)
        {
            this.poolId = poolId;
            this.context = context;
            this.dispatcher = dispatcher;
            this.taskServer = taskServer;
        }

        // TODO: Figure out a cancellation pattern. A cancellation token should probably live on IHostContext.
        public async Task<Boolean> CreateSessionAsync()
        {
            const Int32 MaxAttempts = 10;
            Int32 attempt = 0;
            while (++attempt <= MaxAttempts)
            {
                this.context.Info("Create session attempt {0} of {1}.", attempt, MaxAttempts);
                try
                {
                    this.SessionId = await this.taskServer.CreateAgentSessionAsync(this.poolId);
                    return true;
                }
                catch (Exception ex)
                {
                    this.context.Error("Failed to create session.");
                    if (ex is TaskAgentNotFoundException)
                    {
                        this.context.Error("The agent no longer exists on the server. Stopping the agent.");
                        this.context.Error(ex);
                        return false;
                    }
                    else if (ex is TaskAgentSessionConflictException)
                    {
                        this.context.Error("The session for this agent already exists.");
                    }
                    else
                    {
                        this.context.Error(ex);
                    }

                    if (attempt >= MaxAttempts)
                    {
                        this.context.Error("Retries exhausted. Terminating the agent.");
                        return false;
                    }

                    TimeSpan interval = TimeSpan.FromSeconds(30);
                    this.context.Info("Sleeping for {0} seconds before retrying.", interval.TotalSeconds);
                    await Task.Delay(interval);
                }
            }

            return false;
        }

        // TODO: Figure out a cancellation pattern. A cancellation token should probably live on IHostContext.
        public async void ListenAsync()
        {
            Int32 lastMessageId = 0;
            while (true)
            {
                AgentMessage message = null;
                try
                {
                    message = await this.taskServer.GetAgentMessageAsync(this.poolId, this.SessionId, lastMessageId);
                }
                catch (TimeoutException)
                {
                    this.context.Verbose("MessageListener.Listen - TimeoutException received.");
                }
                catch (TaskCanceledException)
                {
                    this.context.Verbose("MessageListener.Listen - TaskCanceledException received.");
                }
                catch (TaskAgentSessionExpiredException)
                {
                    this.context.Verbose("MessageListener.Listen - TaskAgentSessionExpiredException received.");
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }
                catch (Exception ex)
                {
                    this.context.Verbose("MessageListener.Listen - Exception received.");
                    this.context.Error(ex);
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }

                if (message == null)
                {
                    this.context.Verbose("MessageListener.Listen - No message retrieved from session '{0}'.", this.SessionId);
                    continue;
                }

                this.context.Verbose("MessageListener.Listen - Message '{0}' received from session '{1}'.", message.MessageId, this.SessionId);
                try
                {
                    // Check if refresh is required.
                    if (String.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Throw a specific exception so the caller can control the flow appropriately.
                        return;
                    }

                    this.dispatcher.Dispatch(message);
                }
                finally
                {
                    lastMessageId = message.MessageId;
                    await this.taskServer.DeleteAgentMessageAsync(this.poolId, this.SessionId, message);
                }
            }
        }

        public async void DeleteSessionAsync()
        {
            if (!String.IsNullOrEmpty(this.SessionId))
            {
                await this.taskServer.DeleteAgentSessionAsync(this.poolId, this.SessionId);
            }
        }

        private readonly Int32 poolId;
        private readonly IHostContext context;
        private readonly IMessageDispatcher dispatcher;
        private readonly ITaskServer taskServer;
        public String SessionId { get; set; }
  }
}