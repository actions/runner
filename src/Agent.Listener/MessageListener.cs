using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    [ServiceLocator(Default = typeof(MessageListener))]
    public interface IMessageListener
    {
        Task<Boolean> CreateSessionAsync(IHostContext context);
        Task ListenAsync(IHostContext context);
        Task DeleteSessionAsync(IHostContext context);
    }

    public sealed class MessageListener : IMessageListener
    {
        public TaskAgentSession Session { get; set; }

        public async Task<Boolean> CreateSessionAsync(IHostContext context)
        {
            var agentSettings = context.GetService<IAgentSettings>();
            var taskServer = context.GetService<ITaskServer>();
            TraceSource trace = context.Trace[TraceName];
            const Int32 MaxAttempts = 10;
            Int32 attempt = 0;
            while (++attempt <= MaxAttempts)
            {
                trace.Info("Create session attempt {0} of {1}.", attempt, MaxAttempts);
                try
                {
                    this.Session = await taskServer.CreateAgentSessionAsync(agentSettings.PoolId, new TaskAgentSession(), context.CancellationToken);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    trace.Error("Failed to create session.");
                    if (ex is TaskAgentNotFoundException)
                    {
                        trace.Error("The agent no longer exists on the server. Stopping the agent.");
                        trace.Error(ex);
                        return false;
                    }
                    else if (ex is TaskAgentSessionConflictException)
                    {
                        trace.Error("The session for this agent already exists.");
                    }
                    else
                    {
                        trace.Error(ex);
                    }

                    if (attempt >= MaxAttempts)
                    {
                        trace.Error("Retries exhausted. Terminating the agent.");
                        return false;
                    }

                    TimeSpan interval = TimeSpan.FromSeconds(30);
                    trace.Info("Sleeping for {0} seconds before retrying.", interval.TotalSeconds);
                    await context.Delay(interval);
                }
            }

            return false;
        }

        public async Task ListenAsync(IHostContext context)
        {
            var agentSettings = context.GetService<IAgentSettings>();
            var dispatcher = context.GetService<IMessageDispatcher>();
            var taskServer = context.GetService<ITaskServer>();
            TraceSource trace = context.Trace[TraceName];
            Int64? lastMessageId = null;
            while (true)
            {
                TaskAgentMessage message = null;
                try
                {
                    message = await taskServer.GetAgentMessageAsync(agentSettings.PoolId, this.Session.SessionId, lastMessageId, context.CancellationToken);
                }
                catch (TimeoutException)
                {
                    trace.Verbose("MessageListener.Listen - TimeoutException received.");
                }
                catch (TaskCanceledException)
                {
                    trace.Verbose("MessageListener.Listen - TaskCanceledException received.");
                }
                catch (TaskAgentSessionExpiredException)
                {
                    trace.Verbose("MessageListener.Listen - TaskAgentSessionExpiredException received.");
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    trace.Verbose("MessageListener.Listen - Exception received.");
                    trace.Error(ex);
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }

                if (message == null)
                {
                    trace.Verbose("MessageListener.Listen - No message retrieved from session '{0}'.", this.Session.SessionId);
                    continue;
                }

                trace.Verbose("MessageListener.Listen - Message '{0}' received from session '{1}'.", message.MessageId, this.Session.SessionId);
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
                    await taskServer.DeleteAgentMessageAsync(agentSettings.PoolId, lastMessageId.Value, this.Session.SessionId, context.CancellationToken);
                }
            }
        }

        public async Task DeleteSessionAsync(IHostContext context)
        {
            var agentSettings = context.GetService<IAgentSettings>();
            var taskServer = context.GetService<ITaskServer>();
            if (this.Session != null && this.Session.SessionId != Guid.Empty)
            {
                await taskServer.DeleteAgentSessionAsync(agentSettings.PoolId, this.Session.SessionId, context.CancellationToken);
            }
        }

        private const String TraceName = "MessageListener";
         // // use this class scope cancellation token when we figure out how do we want to handle MessageListener level retry.
        // m_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
   }
}