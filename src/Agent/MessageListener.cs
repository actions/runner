using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    public sealed class MessageListener
    {
        public async Task<Boolean> CreateSessionAsync(IHostContext context, Int32 poolId)
        {
            var taskServer = context.GetService<ITaskServer>();
            this.PoolId = poolId;
            const Int32 MaxAttempts = 10;
            Int32 attempt = 0;
            while (++attempt <= MaxAttempts)
            {
                context.Info("Create session attempt {0} of {1}.", attempt, MaxAttempts);
                try
                {
                    this.SessionId = await taskServer.CreateAgentSessionAsync(this.PoolId, context.CancellationToken);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    context.Error("Failed to create session.");
                    if (ex is TaskAgentNotFoundException)
                    {
                        context.Error("The agent no longer exists on the server. Stopping the agent.");
                        context.Error(ex);
                        return false;
                    }
                    else if (ex is TaskAgentSessionConflictException)
                    {
                        context.Error("The session for this agent already exists.");
                    }
                    else
                    {
                        context.Error(ex);
                    }

                    if (attempt >= MaxAttempts)
                    {
                        context.Error("Retries exhausted. Terminating the agent.");
                        return false;
                    }

                    TimeSpan interval = TimeSpan.FromSeconds(30);
                    context.Info("Sleeping for {0} seconds before retrying.", interval.TotalSeconds);
                    await Task.Delay(interval, context.CancellationToken);
                }
            }

            return false;
        }

        public async Task ListenAsync(IHostContext context)
        {
            var dispatcher = context.GetService<IMessageDispatcher>();
            var taskServer = context.GetService<ITaskServer>();
            Int32 lastMessageId = 0;
            while (true)
            {
                AgentMessage message = null;
                try
                {
                    message = await taskServer.GetAgentMessageAsync(this.PoolId, this.SessionId, lastMessageId, context.CancellationToken);
                }
                catch (TimeoutException)
                {
                    context.Verbose("MessageListener.Listen - TimeoutException received.");
                }
                catch (TaskCanceledException)
                {
                    context.Verbose("MessageListener.Listen - TaskCanceledException received.");
                }
                catch (TaskAgentSessionExpiredException)
                {
                    context.Verbose("MessageListener.Listen - TaskAgentSessionExpiredException received.");
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    context.Verbose("MessageListener.Listen - Exception received.");
                    context.Error(ex);
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }

                if (message == null)
                {
                    context.Verbose("MessageListener.Listen - No message retrieved from session '{0}'.", this.SessionId);
                    continue;
                }

                context.Verbose("MessageListener.Listen - Message '{0}' received from session '{1}'.", message.MessageId, this.SessionId);
                try
                {
                    // Check if refresh is required.
                    if (String.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Throw a specific exception so the caller can control the flow appropriately.
                        return;
                    }

                    dispatcher.Dispatch(message);
                }
                finally
                {
                    lastMessageId = message.MessageId;
                    await taskServer.DeleteAgentMessageAsync(this.PoolId, this.SessionId, message, context.CancellationToken);
                }
            }
        }

        public async Task DeleteSessionAsync(IHostContext context)
        {
            var taskServer = context.GetService<ITaskServer>();
            if (!String.IsNullOrEmpty(this.SessionId))
            {
                await taskServer.DeleteAgentSessionAsync(this.PoolId, this.SessionId, context.CancellationToken);
            }
        }

        public Int32 PoolId { get; set; }
        public String SessionId { get; set; }
    }
}