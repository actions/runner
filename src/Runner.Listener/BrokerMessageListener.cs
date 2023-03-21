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
using GitHub.Services.OAuth;

namespace GitHub.Runner.Listener
{
    public sealed class BrokerMessageListener : RunnerService, IMessageListener
    {
        private long? _lastMessageId;
        private RunnerSettings _settings;
        private ITerminal _term;
        private IRunnerServer _runnerServer;
        private TaskAgentSession _session;
        private TimeSpan _getNextMessageRetryInterval;
        private readonly TimeSpan _sessionCreationRetryInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _sessionConflictRetryLimit = TimeSpan.FromMinutes(4);
        private readonly TimeSpan _clockSkewRetryLimit = TimeSpan.FromMinutes(30);
        private readonly Dictionary<string, int> _sessionCreationExceptionTracker = new();
        private TaskAgentStatus runnerStatus = TaskAgentStatus.Online;
        private CancellationTokenSource _getMessagesTokenSource;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _term = HostContext.GetService<ITerminal>();
        }

        public async Task<Boolean> CreateSessionAsync(CancellationToken token)
        {
            return await Task.FromResult(true);
        }

        public async Task DeleteSessionAsync()
        {
            await Task.CompletedTask;
        }

        public void OnJobStatus(object sender, JobStatusEventArgs e)
        {
            if (StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("USE_BROKER_FLOW")))
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
        }

        public async Task<TaskAgentMessage> GetNextMessageAsync(CancellationToken token)
        {
            Trace.Entering();
            ArgUtil.NotNull(_settings, nameof(_settings));
            bool encounteringError = false;
            int continuousError = 0;
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
                                                                _getMessagesTokenSource.Token);


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
                    throw;
                }
                catch (AccessDeniedException e) when (e.InnerException is InvalidTaskAgentVersionException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.Error("Catch exception during get next message.");
                    Trace.Error(ex);

                    // don't retry if SkipSessionRecover = true, DT service will delete agent session to stop agent from taking more jobs.
                    if (ex is TaskAgentSessionExpiredException && !_settings.SkipSessionRecover && await CreateSessionAsync(token))
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
                        Trace.Verbose($"No message retrieved");
                    }

                    continue;
                }

                Trace.Info($"Message '{message.MessageId}' received");
                return message;
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

        private <TaskAgentMessage> GetMessageAsync(string status, string version)
        {

        }
    }
}
