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
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Runner.Common.Util;
using GitHub.Services.OAuth;

namespace GitHub.Runner.Listener
{
    public sealed class BrokerMessageListener : RunnerService, IMessageListener
    {
        private RunnerSettings _settings;
        private ITerminal _term;
        private TimeSpan _getNextMessageRetryInterval;
        private TaskAgentStatus runnerStatus = TaskAgentStatus.Online;
        private CancellationTokenSource _getMessagesTokenSource;
        private IBrokerServer _brokerServer;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _term = HostContext.GetService<ITerminal>();
            _brokerServer = HostContext.GetService<IBrokerServer>();
        }

        public async Task<Boolean> CreateSessionAsync(CancellationToken token)
        {
            await RefreshBrokerConnection();
            return await Task.FromResult(true);
        }

        public async Task DeleteSessionAsync()
        {
            await Task.CompletedTask;
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
                    message = await _brokerServer.GetRunnerMessageAsync(_getMessagesTokenSource.Token, runnerStatus, BuildConstants.RunnerPackage.Version);

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
                catch (AccessDeniedException e) when (e.ErrorCode == 1)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Catch exception during get next message.");
                    Trace.Error(ex);

                    if (!IsGetNextMessageExceptionRetriable(ex))
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

                        // re-create VssConnection before next retry
                        await RefreshBrokerConnection();

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

        public async Task DeleteMessageAsync(TaskAgentMessage message)
        {
            await Task.CompletedTask;
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

        private async Task RefreshBrokerConnection()
        {
            var configManager = HostContext.GetService<IConfigurationManager>();
            _settings = configManager.LoadSettings();

            if (_settings.ServerUrlV2 == null)
            {
                throw new InvalidOperationException("ServerUrlV2 is not set");
            }

            var credMgr = HostContext.GetService<ICredentialManager>();
            VssCredentials creds = credMgr.LoadCredentials();
            await _brokerServer.ConnectAsync(new Uri(_settings.ServerUrlV2), creds);
        }
    }
}
