﻿using System;
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
    public sealed class BrokerMessageListener : RunnerService, IMessageListener
    {
        private RunnerSettings _settings;
        private ITerminal _term;
        private TimeSpan _getNextMessageRetryInterval;
        private TaskAgentStatus _runnerStatus = TaskAgentStatus.Online;
        private CancellationTokenSource _getMessagesTokenSource;
        private VssCredentials _creds;
        private VssCredentials _credsV2;
        private TaskAgentSession _session;
        private IRunnerServer _runnerServer;
        private IBrokerServer _brokerServer;
        private ICredentialManager _credMgr;
        private readonly Dictionary<string, int> _sessionCreationExceptionTracker = new();
        private bool _accessTokenRevoked = false;
        private readonly TimeSpan _sessionCreationRetryInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _sessionConflictRetryLimit = TimeSpan.FromMinutes(4);
        private readonly TimeSpan _clockSkewRetryLimit = TimeSpan.FromMinutes(30);
        private bool _needRefreshCredsV2 = false;
        private bool _handlerInitialized = false;
        private bool _isMigratedSettings = false;
        private const int _maxMigratedSettingsRetries = 3;
        private int _migratedSettingsRetryCount = 0;

        public BrokerMessageListener()
        {
        }

        public BrokerMessageListener(RunnerSettings settings, bool isMigratedSettings = false)
        {
            _settings = settings;
            _isMigratedSettings = isMigratedSettings;
        }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _term = HostContext.GetService<ITerminal>();
            _runnerServer = HostContext.GetService<IRunnerServer>();
            _brokerServer = HostContext.GetService<IBrokerServer>();
            _credMgr = HostContext.GetService<ICredentialManager>();
        }

        public async Task<CreateSessionResult> CreateSessionAsync(CancellationToken token)
        {
            Trace.Entering();

            // Load settings if not provided through constructor
            if (_settings == null)
            {
                var configManager = HostContext.GetService<IConfigurationManager>();
                _settings = configManager.LoadSettings();
                Trace.Info("Settings loaded from config manager");
            }
            else
            {
                Trace.Info("Using provided settings");
                if (_isMigratedSettings)
                {
                    Trace.Info("Using migrated settings from .runner_migrated");
                }
            }

            var serverUrlV2 = _settings.ServerUrlV2;
            var serverUrl = _settings.ServerUrl;
            Trace.Info(_settings);

            if (string.IsNullOrEmpty(_settings.ServerUrlV2))
            {
                throw new InvalidOperationException("ServerUrlV2 is not set");
            }

            // Create connection.
            Trace.Info("Loading Credentials");
            _creds = _credMgr.LoadCredentials(allowAuthUrlV2: false);

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
                    Trace.Info("Connecting to the Broker Server...");
                    _credsV2 = _credMgr.LoadCredentials(allowAuthUrlV2: true);
                    await _brokerServer.ConnectAsync(new Uri(serverUrlV2), _credsV2);
                    Trace.Info("VssConnection created");

                    if (!string.IsNullOrEmpty(serverUrl) &&
                        !string.Equals(serverUrl, serverUrlV2, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.Info("Connecting to the Runner server...");
                        await _runnerServer.ConnectAsync(new Uri(serverUrl), _creds);
                        Trace.Info("VssConnection created");
                    }

                    _term.WriteLine();
                    _term.WriteSuccessMessage("Connected to GitHub");
                    _term.WriteLine();

                    _session = await _brokerServer.CreateSessionAsync(taskAgentSession, token);

                    Trace.Info($"Session created.");
                    if (encounteringError)
                    {
                        _term.WriteLine($"{DateTime.UtcNow:u}: Runner reconnected.");
                        _sessionCreationExceptionTracker.Clear();
                        encounteringError = false;
                    }

                    if (!_handlerInitialized)
                    {
                        // Register event handler for auth migration state change
                        HostContext.AuthMigrationChanged += HandleAuthMigrationChanged;
                        _handlerInitialized = true;
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

                    // If using migrated settings, limit the number of retries before returning failure
                    if (_isMigratedSettings)
                    {
                        _migratedSettingsRetryCount++;
                        Trace.Warning($"Migrated settings retry {_migratedSettingsRetryCount} of {_maxMigratedSettingsRetries}");
                        
                        if (_migratedSettingsRetryCount >= _maxMigratedSettingsRetries)
                        {
                            Trace.Warning("Reached maximum retry attempts for migrated settings. Returning failure to try default settings.");
                            return CreateSessionResult.Failure;
                        }
                    }

                    if (!HostContext.AllowAuthMigration &&
                        ex is VssOAuthTokenRequestException vssOAuthEx &&
                        _credsV2.Federated is VssOAuthCredential vssOAuthCred)
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
                        var oauthTokenProvider = new VssOAuthTokenProvider(vssOAuthCred, new Uri(serverUrlV2));
                        var authError = await oauthTokenProvider.ValidateCredentialAsync(token);
                        if (string.Equals(authError, "invalid_client", StringComparison.OrdinalIgnoreCase))
                        {
                            _term.WriteError("Failed to create a session. The runner registration has been deleted from the server, please re-configure. Runner registrations are automatically deleted for runners that have not connected to the service recently.");
                            return CreateSessionResult.Failure;
                        }
                    }

                    if (!HostContext.AllowAuthMigration &&
                        !IsSessionCreationExceptionRetriable(ex))
                    {
                        _term.WriteError($"Failed to create session. {ex.Message}");
                        if (ex is TaskAgentSessionConflictException)
                        {
                            return CreateSessionResult.SessionConflict;
                        }
                        return CreateSessionResult.Failure;
                    }

                    if (HostContext.AllowAuthMigration)
                    {
                        Trace.Info("Disable migration mode for 60 minutes.");
                        HostContext.DeferAuthMigration(TimeSpan.FromMinutes(60), $"Session creation failed with exception: {ex}");
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
                if (_handlerInitialized)
                {
                    HostContext.AuthMigrationChanged -= HandleAuthMigrationChanged;
                }

                if (!_accessTokenRevoked)
                {
                    using (var ts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        await _brokerServer.DeleteSessionAsync(ts.Token);
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
            _runnerStatus = e.Status;
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
            bool encounteringError = false;
            int continuousError = 0;
            Stopwatch heartbeat = new();
            heartbeat.Restart();
            var maxRetryCount = 10;

            while (true)
            {
                TaskAgentMessage message = null;
                _getMessagesTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                try
                {
                    if (_needRefreshCredsV2)
                    {
                        Trace.Info("Refreshing broker connection.");
                        await RefreshBrokerConnectionAsync();
                        _needRefreshCredsV2 = false;
                    }

                    message = await _brokerServer.GetRunnerMessageAsync(_session.SessionId,
                                                                        _runnerStatus,
                                                                        BuildConstants.RunnerPackage.Version,
                                                                        VarUtil.OS,
                                                                        VarUtil.OSArchitecture,
                                                                        _settings.DisableUpdate,
                                                                        _getMessagesTokenSource.Token);

                    if (message == null)
                    {
                        continue;
                    }

                    return message;
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
                    throw;
                }
                catch (HostedRunnerDeprovisionedException)
                {
                    Trace.Info("Hosted runner has been deprovisioned.");
                    throw;
                }
                catch (AccessDeniedException e) when (e.ErrorCode == 1 && !HostContext.AllowAuthMigration)
                {
                    throw;
                }
                catch (RunnerNotFoundException) when (!HostContext.AllowAuthMigration)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Catch exception during get next message.");
                    Trace.Error(ex);

                    if (!HostContext.AllowAuthMigration &&
                        !IsGetNextMessageExceptionRetriable(ex))
                    {
                        throw new NonRetryableException("Get next message failed with non-retryable error.", ex);
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
                        else if (continuousError >= maxRetryCount)
                        {
                            throw;
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

                        if (HostContext.AllowAuthMigration)
                        {
                            Trace.Info("Disable migration mode for 60 minutes.");
                            HostContext.DeferAuthMigration(TimeSpan.FromMinutes(60), $"Get next message failed with exception: {ex}");
                        }

                        // re-create VssConnection before next retry
                        await RefreshBrokerConnectionAsync();

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
                    if (heartbeat.Elapsed > TimeSpan.FromMinutes(30))
                    {
                        Trace.Info($"No message retrieved within last 30 minutes.");
                        heartbeat.Restart();
                    }
                    else
                    {
                        Trace.Verbose($"No message retrieved.");
                    }

                    continue;
                }

                Trace.Info($"Message '{message.MessageId}' received.");
            }
        }

        public async Task RefreshListenerTokenAsync()
        {
            await RefreshBrokerConnectionAsync();
        }

        public async Task DeleteMessageAsync(TaskAgentMessage message)
        {
            await Task.CompletedTask;
        }

        public async Task AcknowledgeMessageAsync(string runnerRequestId, CancellationToken cancellationToken)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Short timeout
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            Trace.Info($"Acknowledging runner request '{runnerRequestId}'.");
            await _brokerServer.AcknowledgeRunnerRequestAsync(
                runnerRequestId,
                _session.SessionId,
                _runnerStatus,
                BuildConstants.RunnerPackage.Version,
                VarUtil.OS,
                VarUtil.OSArchitecture,
                linkedCts.Token);
        }

        private bool IsGetNextMessageExceptionRetriable(Exception ex)
        {
            if (ex is TaskAgentNotFoundException ||
                ex is TaskAgentPoolNotFoundException ||
                ex is TaskAgentSessionExpiredException ||
                ex is AccessDeniedException ||
                ex is RunnerNotFoundException ||
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

            else if (ex is InvalidOperationException)
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

        private async Task RefreshBrokerConnectionAsync()
        {
            Trace.Info("Reload credentials.");
            _credsV2 = _credMgr.LoadCredentials(allowAuthUrlV2: true);
            await _brokerServer.ConnectAsync(new Uri(_settings.ServerUrlV2), _credsV2);
            Trace.Info("Connection to Broker Server recreated.");
        }

        private void HandleAuthMigrationChanged(object sender, EventArgs e)
        {
            Trace.Info($"Auth migration changed. Current allow auth migration state: {HostContext.AllowAuthMigration}");
            _needRefreshCredsV2 = true;
        }
    }
}
