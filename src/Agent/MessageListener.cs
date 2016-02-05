using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    public sealed class MessageListener
    {
        public MessageListener(IHostContext context)
        {
            m_trace = context.Trace["MessageListener"];
            m_taskServer = context.GetService<ITaskServer>();
            m_dispatcher = context.GetService<IMessageDispatcher>();
            
            // use this class scope cancellation token when we figure out how do we want to handle MessageListener level retry.
            m_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        }
        
        public async Task<Boolean> CreateSessionAsync(Int32 poolId)
        {
            this.PoolId = poolId;
            const Int32 MaxAttempts = 10;
            Int32 attempt = 0;
            while (++attempt <= MaxAttempts)
            {
                m_trace.Info("Create session attempt {0} of {1}.", attempt, MaxAttempts);
                try
                {
                    this.SessionId = await m_taskServer.CreateAgentSessionAsync(this.PoolId, m_cancellationTokenSource.Token);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    m_trace.Error("Failed to create session.");
                    if (ex is TaskAgentNotFoundException)
                    {
                        m_trace.Error("The agent no longer exists on the server. Stopping the agent.");
                        m_trace.Error(ex);
                        return false;
                    }
                    else if (ex is TaskAgentSessionConflictException)
                    {
                        m_trace.Error("The session for this agent already exists.");
                    }
                    else
                    {
                        m_trace.Error(ex);
                    }

                    if (attempt >= MaxAttempts)
                    {
                        m_trace.Error("Retries exhausted. Terminating the agent.");
                        return false;
                    }

                    TimeSpan interval = TimeSpan.FromSeconds(30);
                    m_trace.Info("Sleeping for {0} seconds before retrying.", interval.TotalSeconds);
                    await Task.Delay(interval, m_cancellationTokenSource.Token);
                }
            }

            return false;
        }

        public async Task ListenAsync()
        {
            Int32 lastMessageId = 0;
            while (true)
            {
                AgentMessage message = null;
                try
                {
                    message = await m_taskServer.GetAgentMessageAsync(this.PoolId, this.SessionId, lastMessageId, m_cancellationTokenSource.Token);
                }
                catch (TimeoutException)
                {
                    m_trace.Verbose("MessageListener.Listen - TimeoutException received.");
                }
                catch (TaskCanceledException)
                {
                    m_trace.Verbose("MessageListener.Listen - TaskCanceledException received.");
                }
                catch (TaskAgentSessionExpiredException)
                {
                    m_trace.Verbose("MessageListener.Listen - TaskAgentSessionExpiredException received.");
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    m_trace.Verbose("MessageListener.Listen - Exception received.");
                    m_trace.Error(ex);
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    return;
                }

                if (message == null)
                {
                    m_trace.Verbose("MessageListener.Listen - No message retrieved from session '{0}'.", this.SessionId);
                    continue;
                }

                m_trace.Verbose("MessageListener.Listen - Message '{0}' received from session '{1}'.", message.MessageId, this.SessionId);
                try
                {
                    // Check if refresh is required.
                    if (String.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Throw a specific exception so the caller can control the flow appropriately.
                        return;
                    }

                    m_dispatcher.Dispatch(message);
                }
                finally
                {
                    lastMessageId = message.MessageId;
                    await m_taskServer.DeleteAgentMessageAsync(this.PoolId, this.SessionId, message, m_cancellationTokenSource.Token);
                }
            }
        }

        public async Task DeleteSessionAsync()
        {
            if (!String.IsNullOrEmpty(this.SessionId))
            {
                await m_taskServer.DeleteAgentSessionAsync(this.PoolId, this.SessionId, m_cancellationTokenSource.Token);
            }
        }

        public Int32 PoolId { get; set; }
        public String SessionId { get; set; }
        
        private CancellationTokenSource m_cancellationTokenSource;
        private IMessageDispatcher m_dispatcher;
        private ITaskServer m_taskServer;
        private readonly TraceSource m_trace;
        
    }
}