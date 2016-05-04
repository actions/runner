using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(MessageListener))]
    public interface IMessageListener : IAgentService
    {
        Task<Boolean> CreateSessionAsync(CancellationToken token);
        Task DeleteSessionAsync();
        Task<TaskAgentMessage> GetNextMessageAsync(CancellationToken token);
        TaskAgentSession Session { get; }
    }

    public sealed class MessageListener : AgentService, IMessageListener
    {
        private long? _lastMessageId;
        private AgentSettings _settings;
        private ITerminal _term;

        public TaskAgentSession Session { get; set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _term = HostContext.GetService<ITerminal>();
        }

        public async Task<Boolean> CreateSessionAsync(CancellationToken token)
        {
            Trace.Entering();
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
            string sessionName = $"{Environment.MachineName ?? string.Empty}_{Guid.NewGuid().ToString()}";
            var capProvider = HostContext.GetService<ICapabilitiesProvider>();
            Dictionary<string, string> agentSystemCapabilities = await capProvider.GetCapabilitiesAsync(_settings.AgentName, token);

            var agent = new TaskAgentReference
            {
                Id = _settings.AgentId,
                Name = _settings.AgentName,
                Version = Constants.Agent.Version,
                Enabled = true
            };
            var taskAgentSession = new TaskAgentSession(sessionName, agent, agentSystemCapabilities);

            var agentSvr = HostContext.GetService<IAgentServer>();
            string errorMessage = string.Empty;
            bool firstAttempt = true; //tells us if this is the first time we try to connect
            while (true)
            {
                attempt++;
                Trace.Info($"Create session attempt {attempt}.");
                try
                {
                    Trace.Info("Connecting to the Agent Server...");
                    await agentSvr.ConnectAsync(conn);

                    Session = await agentSvr.CreateAgentSessionAsync(
                                                        _settings.PoolId,
                                                        taskAgentSession,
                                                        token);
                    if (!firstAttempt)
                    {
                        _term.WriteLine(StringUtil.Loc("QueueConnected", DateTime.UtcNow));
                    }

                    return true;
                }
                catch (OperationCanceledException ex)
                {
                    if (token.IsCancellationRequested) //Distinguish timeout from user cancellation
                    {
                        Trace.Info("Cancelled");
                        throw;
                    }
                    errorMessage = ex.Message;
                }
                catch (Exception ex)
                {
                    Trace.Error("Failed to create session.");
                    if (ex is TaskAgentNotFoundException)
                    {
                        Trace.Error("The agent no longer exists on the server. Stopping the agent.");
                        _term.WriteError(StringUtil.Loc("MissingAgent"));
                    }

                    if (ex is TaskAgentSessionConflictException)
                    {
                        Trace.Error("The session for this agent already exists.");
                    }

                    Trace.Error(ex);
                    if (IsFatalException(ex))
                    {
                        return false;
                    }
                    errorMessage = ex.Message;
                }

                TimeSpan interval = TimeSpan.FromSeconds(30);
                if (firstAttempt) //print the message only on the first error
                {
                    _term.WriteError(StringUtil.Loc("QueueConError", DateTime.UtcNow, errorMessage, interval.TotalSeconds));
                    firstAttempt = false;
                }
                Trace.Info("Sleeping for {0} seconds before retrying.", interval.TotalSeconds);
                await HostContext.Delay(interval, token);
            }
        }

        public async Task DeleteSessionAsync()
        {
            var agentServer = HostContext.GetService<IAgentServer>();
            if (Session != null && Session.SessionId != Guid.Empty)
            {
                using (var ts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    await agentServer.DeleteAgentSessionAsync(_settings.PoolId, Session.SessionId, ts.Token);
                }
            }
        }

        public async Task<TaskAgentMessage> GetNextMessageAsync(CancellationToken token)
        {
            Trace.Entering();
            ArgUtil.NotNull(Session, nameof(Session));
            ArgUtil.NotNull(_settings, nameof(_settings));
            var agentServer = HostContext.GetService<IAgentServer>();
            int consecutiveErrors = 0; //number of consecutive exceptions thrown by GetAgentMessageAsync
            string errorMessage = string.Empty;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                TaskAgentMessage message = null;
                try
                {
                    consecutiveErrors++;
                    message = await agentServer.GetAgentMessageAsync(_settings.PoolId,
                                                                Session.SessionId,
                                                                _lastMessageId,
                                                                token);
                    if (message != null)
                    {
                        _lastMessageId = message.MessageId;
                    }

                    if (consecutiveErrors > 1) //print the message once only if there was an error
                    {
                        _term.WriteLine(StringUtil.Loc("QueueConnected", DateTime.UtcNow));
                    }

                    consecutiveErrors = 0;
                }
                catch (TimeoutException ex)
                {
                    Trace.Verbose($"{nameof(TimeoutException)} received.");
                    //retry after a delay
                    errorMessage = ex.Message;
                }
                catch (TaskAgentSessionExpiredException)
                {
                    Trace.Verbose($"{nameof(TaskAgentSessionExpiredException)} received.");
                    if (!await CreateSessionAsync(token))
                    {
                        throw;
                    }

                    consecutiveErrors = 0;
                }
                catch (OperationCanceledException ex)
                {
                    Trace.Verbose($"{nameof(OperationCanceledException)} received.");
                    //we get here when the agent is stopped with CTRL-C or service is stopped or HttpClient has timed out                     
                    if (token.IsCancellationRequested) //Distinguish timeout from user cancellation
                    {
                        throw;
                    }

                    //retry after a delay
                    errorMessage = ex.Message;
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                    if (IsFatalException(ex))
                    {
                        throw;
                    }

                    //retry after a delay
                    errorMessage = ex.Message;
                }

                //print an error and add a delay
                if (consecutiveErrors > 0)
                {
                    TimeSpan interval = TimeSpan.FromSeconds(15);
                    if (consecutiveErrors == 1)
                    {
                        //print error only on the first consecutive error
                        _term.WriteError(StringUtil.Loc("QueueConError", DateTime.UtcNow, errorMessage, interval.TotalSeconds));
                    }

                    Trace.Info("Sleeping for {0} seconds before retrying.", interval.TotalSeconds);
                    await HostContext.Delay(interval, token);
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

        private bool IsFatalException(Exception ex)
        {
            return ex is TaskAgentPoolNotFoundException || ex is AccessDeniedException || ex is TaskAgentNotFoundException;
        }
    }
}