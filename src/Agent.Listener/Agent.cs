using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Text;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(Agent))]
    public interface IAgent : IAgentService
    {
        CancellationTokenSource TokenSource { get; set; }
        Task<int> ExecuteCommand(CommandSettings command);
    }

    public sealed class Agent : AgentService, IAgent
    {
        public CancellationTokenSource TokenSource { get; set; }

        private Guid _sessionId;
        private int _poolId;
        private ITerminal _term;
        private bool _inConfigStage;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = HostContext.GetService<ITerminal>();
        }

        public async Task<int> ExecuteCommand(CommandSettings command)
        {
            try
            {
                WebProxy.ApplyProxySettings();
                _inConfigStage = true;
                _term.CancelKeyPress += CtrlCHandler;
                // TODO Unit test to cover this logic
                Trace.Info(nameof(ExecuteCommand));
                var configManager = HostContext.GetService<IConfigurationManager>();

                // command is not required, if no command it just starts and/or configures if not configured

                // TODO: Invalid config prints usage

                if (command.Help)
                {
                    PrintUsage();
                    return Constants.Agent.ReturnCode.Success;
                }

                if (command.Version)
                {
                    _term.WriteLine(Constants.Agent.Version);
                    return Constants.Agent.ReturnCode.Success;
                }

                if (command.Commit)
                {
                    _term.WriteLine(BuildConstants.Source.CommitHash);
                    return Constants.Agent.ReturnCode.Success;
                }

                // Unconfigure, remove config files, service and exit
                if (command.Unconfigure)
                {                    
                    try
                    {
                        await configManager.UnconfigureAsync(command);
                        return Constants.Agent.ReturnCode.Success;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        _term.WriteError(ex.Message);
                        return Constants.Agent.ReturnCode.TerminatedError;
                    }
                }

                if (command.Run && !configManager.IsConfigured())
                {
                    _term.WriteError(StringUtil.Loc("AgentIsNotConfigured"));
                    PrintUsage();
                    return Constants.Agent.ReturnCode.TerminatedError;
                }

                // unattend mode will not prompt for args if not supplied.  Instead will error.
                if (command.Configure)
                {
                    try
                    {
                        await configManager.ConfigureAsync(command);
                        return Constants.Agent.ReturnCode.Success;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        _term.WriteError(ex.Message);
                        return Constants.Agent.ReturnCode.TerminatedError;
                    }
                }

                Trace.Info("Done evaluating commands");
                await configManager.EnsureConfiguredAsync(command);

                _inConfigStage = false;

                if (command.NoStart)
                {
                    Trace.Info("No start.");
                    return Constants.Agent.ReturnCode.Success;
                }

                AgentSettings settings = configManager.LoadSettings();
                bool runAsService = configManager.IsServiceConfigured();
                if (command.Run || !runAsService)
                {
                    // Run the agent interactively
                    Trace.Verbose($"Run as service: '{runAsService}'");
                    return await RunAsync(TokenSource.Token, settings, runAsService);
                }

                if (runAsService)
                {
                    // This is helpful if the user tries to start the agent.listener which is already configured or running as service
                    // However user can execute the agent by calling the run command
                    // TODO: Should we check if the service is running and prompt user to start the service if its not already running?
                    _term.WriteLine(StringUtil.Loc("ConfiguredAsRunAsService"));
                }

                return Constants.Agent.ReturnCode.Success;
            }
            finally
            {
                _term.CancelKeyPress -= CtrlCHandler;
            }
        }

        private void CtrlCHandler(object sender, EventArgs e)
        {
            _term.WriteLine("Exiting...");
            if (_inConfigStage)
            {
                HostContext.Dispose();
                Environment.Exit(Constants.Agent.ReturnCode.TerminatedError);
            }
            else
            {
                TokenSource.Cancel();
            }
        }

        //create worker manager, create message listener and start listening to the queue
        private async Task<int> RunAsync(CancellationToken token, AgentSettings settings, bool runAsService)
        {
            Trace.Info(nameof(RunAsync));

            // Load the settings.
            _poolId = settings.PoolId;

            var listener = HostContext.GetService<IMessageListener>();
            if (!await listener.CreateSessionAsync(token))
            {
                return Constants.Agent.ReturnCode.TerminatedError;
            }

            _term.WriteLine(StringUtil.Loc("ListenForJobs", DateTime.UtcNow));

            _sessionId = listener.Session.SessionId;
            IJobDispatcher jobDispatcher = null;
            try
            {
                var notification = HostContext.GetService<IJobNotification>();
                notification.StartClient(settings.NotificationPipeName, token);
                bool disableAutoUpdate = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("agent.disableupdate"));
                bool autoUpdateInProgress = false;
                Task<bool> selfUpdateTask = null;
                jobDispatcher = HostContext.CreateService<IJobDispatcher>();
                while (!token.IsCancellationRequested)
                {
                    TaskAgentMessage message = null;
                    bool skipMessageDeletion = false;
                    try
                    {
                        Task<TaskAgentMessage> getNextMessage = listener.GetNextMessageAsync(token);
                        if (autoUpdateInProgress)
                        {
                            Trace.Verbose("Auto update task running at backend, waiting for getNextMessage or selfUpdateTask to finish.");
                            Task completeTask = await Task.WhenAny(getNextMessage, selfUpdateTask);
                            if (completeTask == selfUpdateTask)
                            {
                                autoUpdateInProgress = false;
                                if (await selfUpdateTask)
                                {
                                    Trace.Info("Auto update task finished at backend, an agent update is ready to apply exit the current agent instance.");
                                    return Constants.Agent.ReturnCode.AgentUpdating;
                                }
                                else
                                {
                                    Trace.Info("Auto update task finished at backend, there is no available agent update needs to apply, continue message queue looping.");
                                }
                            }
                        }

                        message = await getNextMessage; //get next message
                        if (string.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                        {
                            if (disableAutoUpdate)
                            {
                                Trace.Info("Refresh message received, skip autoupdate since environment variable agent.disableupdate is set.");
                            }
                            else
                            {
                                if (autoUpdateInProgress == false)
                                {
                                    autoUpdateInProgress = true;
                                    var selfUpdater = HostContext.GetService<ISelfUpdater>();
                                    selfUpdateTask = selfUpdater.SelfUpdate(jobDispatcher, !runAsService, token);
                                    Trace.Info("Refresh message received, kick-off selfupdate background process.");
                                }
                                else
                                {
                                    Trace.Info("Refresh message received, skip autoupdate since a previous autoupdate is already running.");
                                }
                            }
                        }
                        else if (string.Equals(message.MessageType, JobRequestMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                        {
                            if (autoUpdateInProgress)
                            {
                                skipMessageDeletion = true;
                            }
                            else
                            {
                                var newJobMessage = JsonUtility.FromString<JobRequestMessage>(message.Body);
                                jobDispatcher.Run(newJobMessage);
                            }
                        }
                        else if (string.Equals(message.MessageType, JobCancelMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                        {
                            var cancelJobMessage = JsonUtility.FromString<JobCancelMessage>(message.Body);
                            bool jobCancelled = jobDispatcher.Cancel(cancelJobMessage);
                            skipMessageDeletion = autoUpdateInProgress && !jobCancelled;
                        }
                    }
                    finally
                    {
                        if (!skipMessageDeletion && message != null)
                        {
                            try
                            {
                                await DeleteMessageAsync(message);
                            }
                            catch (Exception ex)
                            {
                                Trace.Error($"Catch exception during delete message from message queue. message id: {message.MessageId}");
                                Trace.Error(ex);
                            }
                            finally
                            {
                                message = null;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (jobDispatcher != null)
                {
                    await jobDispatcher.ShutdownAsync();
                }

                //TODO: make sure we don't mask more important exception
                await listener.DeleteSessionAsync();
            }

            return Constants.Agent.ReturnCode.Success;
        }

        private async Task DeleteMessageAsync(TaskAgentMessage message)
        {
            if (message != null && _sessionId != Guid.Empty)
            {
                var agentServer = HostContext.GetService<IAgentServer>();
                using (var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    await agentServer.DeleteAgentMessageAsync(_poolId, message.MessageId, _sessionId, cs.Token);
                }
            }
        }

        private void PrintUsage()
        {
            _term.WriteLine(StringUtil.Loc("ListenerHelp"));
        }
    }
}
