using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Configuration;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(MessageListener))]
    public interface IMessageListener: IAgentService
    {
        Task<Boolean> CreateSessionAsync();
        Task ListenAsync();
        Task DeleteSessionAsync();
    }

    public sealed class MessageListener : AgentService, IMessageListener
    {
        private AgentSettings _settings;

        public TaskAgentSession Session { get; set; }

        public async Task<Boolean> CreateSessionAsync()
        {
            var configManager = HostContext.GetService<IConfigurationManager>();
            _settings = configManager.GetSettings();

            var taskServer = HostContext.GetService<ITaskServer>();

            const int MaxAttempts = 10;
            int attempt = 0;
            while (++attempt <= MaxAttempts)
            {
                Trace.Info("Create session attempt {0} of {1}.", attempt, MaxAttempts);
                try
                {
                    Session = await taskServer.CreateAgentSessionAsync(
                                                        _settings.PoolId, 
                                                        new TaskAgentSession(), 
                                                        HostContext.CancellationToken);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    Trace.Info("Cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Failed to create session.");
                    if (ex is TaskAgentNotFoundException)
                    {
                        Trace.Error("The agent no longer exists on the server. Stopping the agent.");
                        Trace.Error(ex);
                        return false;
                    }
                    else if (ex is TaskAgentSessionConflictException)
                    {
                        Trace.Error("The session for this agent already exists.");
                    }
                    else
                    {
                        Trace.Error(ex);
                    }

                    if (attempt >= MaxAttempts)
                    {
                        Trace.Error("Retries exhausted. Terminating the agent.");
                        return false;
                    }

                    TimeSpan interval = TimeSpan.FromSeconds(30);
                    Trace.Info("Sleeping for {0} seconds before retrying.", interval.TotalSeconds);
                    await HostContext.Delay(interval);
                }
            }

            return false;
        }

        public async Task ListenAsync()
        {
            if (Session == null)
            {
                throw new InvalidOperationException("Must create a session before listening");
            }

            Debug.Assert(_settings != null, "settings should not be null");

            var dispatcher = HostContext.GetService<IMessageDispatcher>();
            var taskServer = HostContext.GetService<ITaskServer>();

            long? lastMessageId = null;
            while (true)
            {
                TaskAgentMessage message = null;
                try
                {
                    message = await taskServer.GetAgentMessageAsync(_settings.PoolId, 
                                                                Session.SessionId, 
                                                                lastMessageId, 
                                                                HostContext.CancellationToken);
                }
                catch (TimeoutException)
                {
                    Trace.Verbose("MessageListener.Listen - TimeoutException received.");
                }
                catch (TaskCanceledException)
                {
                    Trace.Verbose("MessageListener.Listen - TaskCanceledException received.");
                }
                catch (TaskAgentSessionExpiredException)
                {
                    Trace.Verbose("MessageListener.Listen - TaskAgentSessionExpiredException received.");
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Verbose("MessageListener.Listen - Exception received.");
                    Trace.Error(ex);
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }

                if (message == null)
                {
                    Trace.Verbose("MessageListener.Listen - No message retrieved from session '{0}'.", this.Session.SessionId);
                    continue;
                }

                Trace.Verbose("MessageListener.Listen - Message '{0}' received from session '{1}'.", message.MessageId, this.Session.SessionId);
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
                    await taskServer.DeleteAgentMessageAsync(_settings.PoolId, 
                                                    lastMessageId.Value, 
                                                    Session.SessionId, 
                                                    HostContext.CancellationToken);
                }
            }
        }

        public async Task DeleteSessionAsync()
        {
            var taskServer = HostContext.GetService<ITaskServer>();
            
            if (this.Session != null && this.Session.SessionId != Guid.Empty)
            {
                await taskServer.DeleteAgentSessionAsync(_settings.PoolId, 
                                                    Session.SessionId, 
                                                    HostContext.CancellationToken);
            }
        }
         // // use this class scope cancellation token when we figure out how do we want to handle MessageListener level retry.
        // m_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
   }
}