using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(MessageListener))]
    public interface IMessageListener : IAgentService
    {
        Task<Boolean> CreateSessionAsync();
        Task DeleteSessionAsync();
        Task<TaskAgentMessage> GetNextMessageAsync();
        TaskAgentSession Session { get; }
    }

    public sealed class MessageListener : AgentService, IMessageListener
    {
        // TODO: FIX THIS: private long? _lastMessageId;
        private AgentSettings _settings;

        public TaskAgentSession Session { get; set; }

        public async Task<Boolean> CreateSessionAsync()
        {
            Trace.Entering();
            const int MaxAttempts = 10;
            int attempt = 0;

            // Settings
            var configManager = HostContext.GetService<IConfigurationManager>();
            _settings = configManager.LoadSettings();
            int agentPoolId = _settings.PoolId;
            var serverUrl = _settings.ServerUrl;
            Trace.Info(_settings);

            // Load Credentials
            Trace.Verbose("Loading Credentials");
            var credMgr = HostContext.GetService<ICredentialManager>();
            VssCredentials creds = credMgr.LoadCredentials();
            Uri uri = new Uri(serverUrl);
            VssConnection conn = ApiUtil.CreateConnection(uri, creds);

            //session name used to be Environment.MachineName, which is added in a latter coreclr libs than what we have
            //TODO: name the session after Environment.MachineName, when we are ready to consume latest coreclr libs
            //TODO: see if this is available in new std lib ver
            string sessionName = "TODO_machine_name" + Guid.NewGuid().ToString();

            var agentSystemCapabilities = new Dictionary<string, string>();
            //TODO: add capabilities

            var agent = new TaskAgentReference
            {
                Id = _settings.AgentId,
                Name = _settings.AgentName,
                Version = Constants.Agent.Version,
                Enabled = true
            };
            var taskAgentSession = new TaskAgentSession(sessionName, agent, agentSystemCapabilities);

            var agentSvr = HostContext.GetService<IAgentServer>();
            while (++attempt <= MaxAttempts)
            {
                Trace.Info("Create session attempt {0} of {1}.", attempt, MaxAttempts);
                try
                {
                    Trace.Info("Connecting to the Agent Server...");
                    await agentSvr.ConnectAsync(conn);

                    Session = await agentSvr.CreateAgentSessionAsync(
                                                        _settings.PoolId,
                                                        taskAgentSession,
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
                    await HostContext.Delay(interval, HostContext.CancellationToken);
                }
            }

            return false;
        }

        public async Task DeleteSessionAsync()
        {
            var agentServer = HostContext.GetService<IAgentServer>();
            if (this.Session != null && this.Session.SessionId != Guid.Empty)
            {
                //TODO: discuss how to handle cancellation
                //we often have HostContext.CancellationToken already cancelled
                //that is why we create a local cancellation source 
                CancellationTokenSource ts = new CancellationTokenSource();
                await agentServer.DeleteAgentSessionAsync(_settings.PoolId, Session.SessionId, ts.Token);
            }
        }

        public async Task<TaskAgentMessage> GetNextMessageAsync()
        {
            Trace.Entering();
            ArgUtil.NotNull(Session, nameof(Session));
            ArgUtil.NotNull(_settings, nameof(_settings));
            var agentServer = HostContext.GetService<IAgentServer>();
            while (true)
            {
                HostContext.CancellationToken.ThrowIfCancellationRequested();
                TaskAgentMessage message = null;
                try
                {
                    message = await agentServer.GetAgentMessageAsync(_settings.PoolId,
                                                                Session.SessionId,
                                                                null, // TODO: FIX THIS: _lastMessageId,
                                                                HostContext.CancellationToken);
                }
                catch (TimeoutException)
                {
                    Trace.Verbose($"{nameof(TimeoutException)} received.");
                }
                catch (TaskCanceledException)
                {
                    Trace.Verbose($"{nameof(TaskCanceledException)} received.");
                }
                catch (TaskAgentSessionExpiredException)
                {
                    Trace.Verbose($"{nameof(TaskAgentSessionExpiredException)} received.");
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                    // TODO: Throw a specific exception so the caller can control the flow appropriately.
                    throw;
                }

                if (message == null)
                {
                    Trace.Verbose($"No message retrieved from session '{Session.SessionId}'.");
                    continue;
                }

                Trace.Verbose($"Message '{message.MessageId}' received from session '{Session.SessionId}'.");
                return message;
            }
        }
    }
}