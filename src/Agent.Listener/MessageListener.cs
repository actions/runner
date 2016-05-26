using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Text;

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
        private readonly TimeSpan _sessionCreationRetryInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _sessionConflictRetryLimit = TimeSpan.FromMinutes(4);
        private readonly Dictionary<string, int> _sessionCreationExceptionTracker = new Dictionary<string, int>();
        private readonly TimeSpan _getNextMessageRetryInterval = TimeSpan.FromSeconds(15);

        public TaskAgentSession Session { get; set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _term = HostContext.GetService<ITerminal>();
        }

        public async Task<Boolean> CreateSessionAsync(CancellationToken token)
        {
            Trace.Entering();

            // Settings
            var configManager = HostContext.GetService<IConfigurationManager>();
            _settings = configManager.LoadSettings();
            int agentPoolId = _settings.PoolId;
            var serverUrl = _settings.ServerUrl;
            Trace.Info(_settings);

            // Capabilities.
            // TODO: LOC
            _term.WriteLine("Scanning for tool capabilities.");
            Dictionary<string, string> systemCapabilities = await HostContext.GetService<ICapabilitiesManager>().GetCapabilitiesAsync(_settings, token);

            // Create connection.
            Trace.Verbose("Loading Credentials");
            var credMgr = HostContext.GetService<ICredentialManager>();
            VssCredentials creds = credMgr.LoadCredentials();
            Uri uri = new Uri(serverUrl);
            VssConnection conn = ApiUtil.CreateConnection(uri, creds);

            var agent = new TaskAgentReference
            {
                Id = _settings.AgentId,
                Name = _settings.AgentName,
                Version = Constants.Agent.Version,
            };
            string sessionName = $"{Environment.MachineName ?? "AGENT"}";
            var taskAgentSession = new TaskAgentSession(sessionName, agent, systemCapabilities);

            var agentSvr = HostContext.GetService<IAgentServer>();
            string errorMessage = string.Empty;
            bool encounteringError = false;
            // TODO: LOC
            _term.WriteLine("Connecting to the server.");
            while (true)
            {
                token.ThrowIfCancellationRequested();
                Trace.Info($"Attempt to create session.");
                try
                {
                    Trace.Info("Connecting to the Agent Server...");
                    await agentSvr.ConnectAsync(conn);

                    Session = await agentSvr.CreateAgentSessionAsync(
                                                        _settings.PoolId,
                                                        taskAgentSession,
                                                        token);

                    Trace.Info($"Session created.");
                    if (encounteringError)
                    {
                        _term.WriteLine(StringUtil.Loc("QueueConnected", DateTime.UtcNow));
                        _sessionCreationExceptionTracker.Clear();
                        encounteringError = false;
                    }

                    return true;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Trace.Info("Session creation has been cancelled.");
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Catch exception during create session.");
                    Trace.Error(ex);

                    if (!IsSessionCreationExceptionRetriable(ex))
                    {
                        _term.WriteError(StringUtil.Loc("SessionCreateFailed", ex.Message));
                        return false;
                    }

                    if (!encounteringError) //print the message only on the first error
                    {
                        _term.WriteError(StringUtil.Loc("QueueConError", DateTime.UtcNow, ex.Message, _sessionCreationRetryInterval.TotalSeconds));
                        encounteringError = true;
                    }

                    Trace.Info("Sleeping for {0} seconds before retrying.", _sessionCreationRetryInterval.TotalSeconds);
                    await HostContext.Delay(_sessionCreationRetryInterval, token);
                }
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
            bool encounteringError = false;
            string errorMessage = string.Empty;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                TaskAgentMessage message = null;
                try
                {
                    message = await agentServer.GetAgentMessageAsync(_settings.PoolId,
                                                                Session.SessionId,
                                                                _lastMessageId,
                                                                token);

                    // Decrypt the message body if the session is using encryption
                    message = DecryptMessage(message);

                    if (message != null)
                    {
                        _lastMessageId = message.MessageId;
                    }

                    if (encounteringError) //print the message once only if there was an error
                    {
                        _term.WriteLine(StringUtil.Loc("QueueConnected", DateTime.UtcNow));
                        encounteringError = false;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Trace.Info("Get next message has been cancelled.");
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Catch exception during get next message.");
                    Trace.Error(ex);

                    if (ex is TaskAgentSessionExpiredException && await CreateSessionAsync(token))
                    {
                        Trace.Info($"{nameof(TaskAgentSessionExpiredException)} received, recoverd by recreate session.");
                    }
                    else if (!IsGetNextMessageExceptionRetriable(ex))
                    {
                        throw;
                    }
                    else
                    {
                        //retry after a delay
                        if (!encounteringError)
                        {
                            //print error only on the first consecutive error
                            _term.WriteError(StringUtil.Loc("QueueConError", DateTime.UtcNow, ex.Message, _getNextMessageRetryInterval.TotalSeconds));
                            encounteringError = true;
                        }

                        Trace.Info("Sleeping for {0} seconds before retrying.", _getNextMessageRetryInterval.TotalSeconds);
                        await HostContext.Delay(_getNextMessageRetryInterval, token);
                    }
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

        private TaskAgentMessage DecryptMessage(TaskAgentMessage message)
        {
            if (Session.EncryptionKey == null ||
                Session.EncryptionKey.Value.Length == 0 ||
                message == null ||
                message.IV == null ||
                message.IV.Length == 0)
            {
                return message;
            }

            using (var aes = Aes.Create())
            using (var decryptor = GetMessageDecryptor(aes, message))
            using (var body = new MemoryStream(Convert.FromBase64String(message.Body)))
            using (var cryptoStream = new CryptoStream(body, decryptor, CryptoStreamMode.Read))
            using (var bodyReader = new StreamReader(cryptoStream, Encoding.UTF8))
            {
                message.Body = bodyReader.ReadToEnd();
            }

            return message;
        }

        private ICryptoTransform GetMessageDecryptor(
            Aes aes,
            TaskAgentMessage message)
        {
            if (Session.EncryptionKey.Encrypted)
            {
                // The agent session encryption key uses the AES symmetric algorithm
                var keyManager = HostContext.GetService<IRSAKeyManager>();
                using (var rsa = keyManager.GetKey())
                {
                    return aes.CreateDecryptor(rsa.Decrypt(Session.EncryptionKey.Value, RSAEncryptionPadding.OaepSHA1), message.IV);
                }
            }
            else
            {
                return aes.CreateDecryptor(Session.EncryptionKey.Value, message.IV);
            }
        }

        private bool IsGetNextMessageExceptionRetriable(Exception ex)
        {
            if (ex is TaskAgentNotFoundException ||
                ex is TaskAgentPoolNotFoundException ||
                ex is TaskAgentSessionExpiredException ||
                ex is AccessDeniedException ||
                ex is VssUnauthorizedException)
            {
                Trace.Info($"Non-retriable exception: {ex.Message}");
                return false;
            }
            else
            {
                Trace.Info($"Retriable exception: {ex.Message}");
                return true;
            }
        }

        private bool IsSessionCreationExceptionRetriable(Exception ex)
        {
            if (ex is TaskAgentNotFoundException)
            {
                Trace.Info("The agent no longer exists on the server. Stopping the agent.");
                _term.WriteError(StringUtil.Loc("MissingAgent"));
                return false;
            }
            else if (ex is TaskAgentSessionConflictException)
            {
                Trace.Info("The session for this agent already exists.");
                _term.WriteError(StringUtil.Loc("SessionExist"));
                if (_sessionCreationExceptionTracker.ContainsKey(nameof(TaskAgentSessionConflictException)))
                {
                    _sessionCreationExceptionTracker[nameof(TaskAgentSessionConflictException)]++;
                    if (_sessionCreationExceptionTracker[nameof(TaskAgentSessionConflictException)] * _sessionCreationRetryInterval.TotalSeconds >= _sessionConflictRetryLimit.TotalSeconds)
                    {
                        Trace.Info("The session conflict exception have reached retry limit.");
                        _term.WriteError(StringUtil.Loc("SessionExistStopRetry", _sessionConflictRetryLimit.TotalSeconds));
                        return false;
                    }
                }
                else
                {
                    _sessionCreationExceptionTracker[nameof(TaskAgentSessionConflictException)] = 1;
                }

                Trace.Info("The session conflict exception haven't reached retry limit.");
                return true;
            }
            else if (ex is TaskAgentPoolNotFoundException ||
                     ex is AccessDeniedException ||
                     ex is VssUnauthorizedException)
            {
                Trace.Info($"Non-retriable exception: {ex.Message}");
                return false;
            }
            else
            {
                Trace.Info($"Retriable exception: {ex.Message}");
                return true;
            }
        }
    }
}