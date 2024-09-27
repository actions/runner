using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Listener
{
    public enum CreateSessionResult
    {
        Success,
        Failure,
        SessionConflict
    }

    [ServiceLocator(Default = typeof(MessageListener))]
    public interface IMessageListener : IRunnerService
    {
        Task<CreateSessionResult> CreateSessionAsync(CancellationToken token);
        Task DeleteSessionAsync();
        Task<TaskAgentMessage> GetNextMessageAsync(CancellationToken token);
        Task DeleteMessageAsync(TaskAgentMessage message);

        Task RefreshListenerTokenAsync(CancellationToken token);
        void OnJobStatus(object sender, JobStatusEventArgs e);
    }

    public sealed class MessageListener : RunnerService, IMessageListener
    {
        private long? _lastMessageId;
        private RunnerSettings _settings;
        private ITerminal _term;
        private IRunnerServer _runnerServer;
        private IBrokerServer _brokerServer;
        private TaskAgentSession _session;
        private TimeSpan _getNextMessageRetryInterval;
        private bool _accessTokenRevoked = false;
        private readonly TimeSpan _sessionCreationRetryInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _sessionConflictRetryLimit = TimeSpan.FromMinutes(4);
        private readonly TimeSpan _clockSkewRetryLimit = TimeSpan.FromMinutes(30);
        private readonly Dictionary<string, int> _sessionCreationExceptionTracker = new();
        private TaskAgentStatus runnerStatus = TaskAgentStatus.Online;
        private CancellationTokenSource _getMessagesTokenSource;
        private VssCredentials _creds;

        private bool _isBrokerSession = false;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _term = HostContext.GetService<ITerminal>();
            _runnerServer = HostContext.GetService<IRunnerServer>();
            _brokerServer = hostContext.GetService<IBrokerServer>();
        }

        public async Task<CreateSessionResult> CreateSessionAsync(CancellationToken token)
        {
            Trace.Entering();

            // Settings
            var configManager = HostContext.GetService<IConfigurationManager>();
            _settings = configManager.LoadSettings();
            var serverUrl = _settings.ServerUrl;
            Trace.Info(_settings);

            // Create connection.
            Trace.Info("Loading Credentials");
            var credMgr = HostContext.GetService<ICredentialManager>();
            _creds = credMgr.LoadCredentials();

            var agent = new TaskAgentReference
            {
                Id = _settings.AgentId,
                Name = _settings.AgentName,
                Version = BuildConstants.RunnerPackage.Version,
                OSDescription = RuntimeInformation.OSDescription,
            };
            var currentProcess = Process.GetCurrentProcess();
            string sessionName = $"{Environment.MachineName ?? "RUNNER"} (PID: {currentProcess.Id})";
            var taskAgentSession = new TaskAgentSession(sessionName, agent);

            string errorMessage = string.Empty;
            bool encounteringError = false;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                Trace.Info($"Attempt to create session.");
                try
                {
                    Trace.Info("Connecting to the Runner Server...");
                    await _runnerServer.ConnectAsync(new Uri(serverUrl), _creds);
                    Trace.Info("VssConnection created");

                    _term.WriteLine();
                    _term.WriteSuccessMessage("Connected to GitHub");
                    _term.WriteLine();

                    _session = await _runnerServer.CreateAgentSessionAsync(
                                                        _settings.PoolId,
                                                        taskAgentSession,
                                                        token);

                    if (_session.BrokerMigrationMessage != null)
                    {
                        Trace.Info("Runner session is in migration mode: Creating Broker session with BrokerBaseUrl: {0}", _session.BrokerMigrationMessage.BrokerBaseUrl);

                        await _brokerServer.UpdateConnectionIfNeeded(_session.BrokerMigrationMessage.BrokerBaseUrl, _creds);
                        _session = await _brokerServer.CreateSessionAsync(taskAgentSession, token);
                        _isBrokerSession = true;
                    }

                    Trace.Info($"Session created.");
                    if (encounteringError)
                    {
                        _term.WriteLine($"{DateTime.UtcNow:u}: Runner reconnected.");
                        _sessionCreationExceptionTracker.Clear();
                        encounteringError = false;
                    }

                    return CreateSessionResult.Success;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Trace.Info("Session creation has been cancelled.");
                    throw;
                }
                catch (TaskAgentAccessTokenExpiredException)
                {
                    Trace.Info("Runner OAuth token has been revoked. Session creation failed.");
                    _accessTokenRevoked = true;
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Catch exception during create session.");
                    Trace.Error(ex);

                    if (ex is VssOAuthTokenRequestException vssOAuthEx && _creds.Federated is VssOAuthCredential vssOAuthCred)
                    {
                        // "invalid_client" means the runner registration has been deleted from the server.
                        if (string.Equals(vssOAuthEx.Error, "invalid_client", StringComparison.OrdinalIgnoreCase))
                        {
                            _term.WriteError("Failed to create a session. The runner registration has been deleted from the server, please re-configure. Runner registrations are automatically deleted for runners that have not connected to the service recently.");
                            return CreateSessionResult.Failure;
                        }

                        // Check whether we get 401 because the runner registration already removed by the service.
                        // If the runner registration get deleted, we can't exchange oauth token.
                        Trace.Error("Test oauth app registration.");
                        var oauthTokenProvider = new VssOAuthTokenProvider(vssOAuthCred, new Uri(serverUrl));
                        var authError = await oauthTokenProvider.ValidateCredentialAsync(token);
                        if (string.Equals(authError, "invalid_client", StringComparison.OrdinalIgnoreCase))
                        {
                            _term.WriteError("Failed to create a session. The runner registration has been deleted from the server, please re-configure. Runner registrations are automatically deleted for runners that have not connected to the service recently.");
                            return CreateSessionResult.Failure;
                        }
                    }

                    if (!IsSessionCreationExceptionRetriable(ex))
                    {
                        _term.WriteError($"Failed to create session. {ex.Message}");
                        if (ex is TaskAgentSessionConflictException)
                        {
                            return CreateSessionResult.SessionConflict;
                        }
                        return CreateSessionResult.Failure;
                    }

                    if (!encounteringError) //print the message only on the first error
                    {
                        _term.WriteError($"{DateTime.UtcNow:u}: Runner connect error: {ex.Message}. Retrying until reconnected.");
                        encounteringError = true;
                    }

                    Trace.Info("Sleeping for {0} seconds before retrying.", _sessionCreationRetryInterval.TotalSeconds);
                    await HostContext.Delay(_sessionCreationRetryInterval, token);
                }
            }
        }

        public async Task DeleteSessionAsync()
        {
            if (_session != null && _session.SessionId != Guid.Empty)
            {
                if (!_accessTokenRevoked)
                {
                    using (var ts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        await _runnerServer.DeleteAgentSessionAsync(_settings.PoolId, _session.SessionId, ts.Token);

                        if (_isBrokerSession)
                        {
                            await _brokerServer.DeleteSessionAsync(ts.Token);
                        }
                    }
                }
                else
                {
                    Trace.Warning("Runner OAuth token has been revoked. Skip deleting session.");
                }
            }
        }

        public void OnJobStatus(object sender, JobStatusEventArgs e)
        {
            Trace.Info("Received job status event. JobState: {0}", e.Status);
            runnerStatus = e.Status;
            try
            {
                _getMessagesTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                Trace.Info("_getMessagesTokenSource is already disposed.");
            }

        }

        public async Task<TaskAgentMessage> GetNextMessageAsync(CancellationToken token)
        {
            Trace.Entering();
            ArgUtil.NotNull(_session, nameof(_session));
            ArgUtil.NotNull(_settings, nameof(_settings));
            bool encounteringError = false;
            int continuousError = 0;
            int continuousEmptyMessage = 0;
            string errorMessage = string.Empty;
            Stopwatch heartbeat = new();
            heartbeat.Restart();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                TaskAgentMessage message = null;
                _getMessagesTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                try
                {
                    message = await _runnerServer.GetAgentMessageAsync(_settings.PoolId,
                                                                _session.SessionId,
                                                                _lastMessageId,
                                                                runnerStatus,
                                                                BuildConstants.RunnerPackage.Version,
                                                                VarUtil.OS,
                                                                VarUtil.OSArchitecture,
                                                                _settings.DisableUpdate,
                                                                _getMessagesTokenSource.Token);

                    // Decrypt the message body if the session is using encryption
                    message = DecryptMessage(message);


                    if (message != null && message.MessageType == BrokerMigrationMessage.MessageType)
                    {
                        Trace.Info("BrokerMigration message received. Polling Broker for messages...");

                        var migrationMessage = JsonUtility.FromString<BrokerMigrationMessage>(message.Body);

                        await _brokerServer.UpdateConnectionIfNeeded(migrationMessage.BrokerBaseUrl, _creds);
                        message = await _brokerServer.GetRunnerMessageAsync(_session.SessionId,
                                                                        runnerStatus,
                                                                        BuildConstants.RunnerPackage.Version,
                                                                        VarUtil.OS,
                                                                        VarUtil.OSArchitecture,
                                                                        _settings.DisableUpdate,
                                                                        token);
                    }

                    if (message != null)
                    {
                        _lastMessageId = message.MessageId;
                    }

                    if (encounteringError) //print the message once only if there was an error
                    {
                        _term.WriteLine($"{DateTime.UtcNow:u}: Runner reconnected.");
                        encounteringError = false;
                        continuousError = 0;
                    }
                }
                catch (OperationCanceledException) when (_getMessagesTokenSource.Token.IsCancellationRequested && !token.IsCancellationRequested)
                {
                    Trace.Info("Get messages has been cancelled using local token source. Continue to get messages with new status.");
                    continue;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Trace.Info("Get next message has been cancelled.");
                    throw;
                }
                catch (TaskAgentAccessTokenExpiredException)
                {
                    Trace.Info("Runner OAuth token has been revoked. Unable to pull message.");
                    _accessTokenRevoked = true;
                    throw;
                }
                catch (AccessDeniedException e) when (e.ErrorCode == 1)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Catch exception during get next message.");
                    Trace.Error(ex);

                    // don't retry if SkipSessionRecover = true, DT service will delete agent session to stop agent from taking more jobs.
                    if (ex is TaskAgentSessionExpiredException && !_settings.SkipSessionRecover && (await CreateSessionAsync(token) == CreateSessionResult.Success))
                    {
                        Trace.Info($"{nameof(TaskAgentSessionExpiredException)} received, recovered by recreate session.");
                    }
                    else if (!IsGetNextMessageExceptionRetriable(ex))
                    {
                        throw;
                    }
                    else
                    {
                        continuousError++;
                        //retry after a random backoff to avoid service throttling
                        //in case of there is a service error happened and all agents get kicked off of the long poll and all agent try to reconnect back at the same time.
                        if (continuousError <= 5)
                        {
                            // random backoff [15, 30]
                            _getNextMessageRetryInterval = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), _getNextMessageRetryInterval);
                        }
                        else
                        {
                            // more aggressive backoff [30, 60]
                            _getNextMessageRetryInterval = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), _getNextMessageRetryInterval);
                        }

                        if (!encounteringError)
                        {
                            //print error only on the first consecutive error
                            _term.WriteError($"{DateTime.UtcNow:u}: Runner connect error: {ex.Message}. Retrying until reconnected.");
                            encounteringError = true;
                        }

                        // re-create VssConnection before next retry
                        await _runnerServer.RefreshConnectionAsync(RunnerConnectionType.MessageQueue, TimeSpan.FromSeconds(60));

                        Trace.Info("Sleeping for {0} seconds before retrying.", _getNextMessageRetryInterval.TotalSeconds);
                        await HostContext.Delay(_getNextMessageRetryInterval, token);
                    }
                }
                finally
                {
                    _getMessagesTokenSource.Dispose();
                }

                if (message == null)
                {
                    continuousEmptyMessage++;
                    if (heartbeat.Elapsed > TimeSpan.FromMinutes(30))
                    {
                        Trace.Info($"No message retrieved from session '{_session.SessionId}' within last 30 minutes.");
                        heartbeat.Restart();
                        continuousEmptyMessage = 0;
                    }
                    else
                    {
                        Trace.Verbose($"No message retrieved from session '{_session.SessionId}'.");
                    }

                    if (continuousEmptyMessage > 50)
                    {
                        // retried more than 50 times in less than 30mins and still getting empty message
                        // something is not right on the service side, backoff for 15-30s before retry
                        _getNextMessageRetryInterval = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), _getNextMessageRetryInterval);
                        Trace.Info("Sleeping for {0} seconds before retrying.", _getNextMessageRetryInterval.TotalSeconds);
                        await HostContext.Delay(_getNextMessageRetryInterval, token);
                    }

                    continue;
                }

                Trace.Info($"Message '{message.MessageId}' received from session '{_session.SessionId}'.");
                return message;
            }
        }

        public async Task DeleteMessageAsync(TaskAgentMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(_session, nameof(_session));

            if (message != null && _session.SessionId != Guid.Empty)
            {
                using (var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    await _runnerServer.DeleteAgentMessageAsync(_settings.PoolId, message.MessageId, _session.SessionId, cs.Token);
                }
            }
        }

        public async Task RefreshListenerTokenAsync(CancellationToken cancellationToken)
        {
            await _runnerServer.RefreshConnectionAsync(RunnerConnectionType.MessageQueue, TimeSpan.FromSeconds(60));
            await _brokerServer.ForceRefreshConnection(_creds);
        }

        private TaskAgentMessage DecryptMessage(TaskAgentMessage message)
        {
            if (_session.EncryptionKey == null ||
                _session.EncryptionKey.Value.Length == 0 ||
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
            if (_session.EncryptionKey.Encrypted)
            {
                // The agent session encryption key uses the AES symmetric algorithm
                var keyManager = HostContext.GetService<IRSAKeyManager>();
                using (var rsa = keyManager.GetKey())
                {
                    var padding = _session.UseFipsEncryption ? RSAEncryptionPadding.OaepSHA256 : RSAEncryptionPadding.OaepSHA1;
                    return aes.CreateDecryptor(rsa.Decrypt(_session.EncryptionKey.Value, padding), message.IV);
                }
            }
            else
            {
                return aes.CreateDecryptor(_session.EncryptionKey.Value, message.IV);
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
                Trace.Info("The runner no longer exists on the server. Stopping the runner.");
                _term.WriteError("The runner no longer exists on the server. Please reconfigure the runner.");
                return false;
            }
            else if (ex is TaskAgentSessionConflictException)
            {
                Trace.Info("The session for this runner already exists.");
                _term.WriteError("A session for this runner already exists.");
                if (_sessionCreationExceptionTracker.ContainsKey(nameof(TaskAgentSessionConflictException)))
                {
                    _sessionCreationExceptionTracker[nameof(TaskAgentSessionConflictException)]++;
                    if (_sessionCreationExceptionTracker[nameof(TaskAgentSessionConflictException)] * _sessionCreationRetryInterval.TotalSeconds >= _sessionConflictRetryLimit.TotalSeconds)
                    {
                        Trace.Info("The session conflict exception have reached retry limit.");
                        _term.WriteError($"Stop retry on SessionConflictException after retried for {_sessionConflictRetryLimit.TotalSeconds} seconds.");
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
            else if (ex is VssOAuthTokenRequestException && ex.Message.Contains("Current server time is"))
            {
                Trace.Info("Local clock might be skewed.");
                _term.WriteError("The local machine's clock may be out of sync with the server time by more than five minutes. Please sync your clock with your domain or internet time and try again.");
                if (_sessionCreationExceptionTracker.ContainsKey(nameof(VssOAuthTokenRequestException)))
                {
                    _sessionCreationExceptionTracker[nameof(VssOAuthTokenRequestException)]++;
                    if (_sessionCreationExceptionTracker[nameof(VssOAuthTokenRequestException)] * _sessionCreationRetryInterval.TotalSeconds >= _clockSkewRetryLimit.TotalSeconds)
                    {
                        Trace.Info("The OAuth token request exception have reached retry limit.");
                        _term.WriteError($"Stopped retrying OAuth token request exception after {_clockSkewRetryLimit.TotalSeconds} seconds.");
                        return false;
                    }
                }
                else
                {
                    _sessionCreationExceptionTracker[nameof(VssOAuthTokenRequestException)] = 1;
                }

                Trace.Info("The OAuth token request exception haven't reached retry limit.");
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
