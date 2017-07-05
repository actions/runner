using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(Agent))]
    public interface IAgent : IAgentService
    {
        Task<int> ExecuteCommand(CommandSettings command);
    }

    public sealed class Agent : AgentService, IAgent
    {
        private IMessageListener _listener;
        private ITerminal _term;
        private bool _inConfigStage;
        private ManualResetEvent _completedCommand = new ManualResetEvent(false);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = HostContext.GetService<ITerminal>();
        }

        public async Task<int> ExecuteCommand(CommandSettings command)
        {
            try
            {
                var agentWebProxy = HostContext.GetService<IVstsAgentWebProxy>();
                VssHttpMessageHandler.DefaultWebProxy = agentWebProxy;

                _inConfigStage = true;
                _completedCommand.Reset();
                _term.CancelKeyPress += CtrlCHandler;

                //register a SIGTERM handler
                HostContext.Unloading += Agent_Unloading;

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

                // Configure agent prompt for args if not supplied
                // Unattend configure mode will not prompt for args if not supplied and error on any missing or invalid value.
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

                _inConfigStage = false;

                AgentSettings settings = configManager.LoadSettings();

                var store = HostContext.GetService<IConfigurationStore>();
                bool configuredAsService = store.IsServiceConfigured();

                // Run agent
                //if (command.Run) // this line is current break machine provisioner.
                //{

                // Error if agent not configured.
                if (!configManager.IsConfigured())
                {
                    _term.WriteError(StringUtil.Loc("AgentIsNotConfigured"));
                    PrintUsage();
                    return Constants.Agent.ReturnCode.TerminatedError;
                }

                Trace.Verbose($"Configured as service: '{configuredAsService}'");

                //Get the startup type of the agent i.e., autostartup, service, manualinteractive
                var startupTypeAsString = command.GetStartupType();
                if(!Enum.TryParse(startupTypeAsString, true, out StartupType startType))
                {                    
                    Trace.Info($"Could not parse the argument value '{startupTypeAsString}' for StartupType. Defaulting to {StartupType.ManualInteractive}");
                    startType = StartupType.ManualInteractive;
                }
                else
                {   
                    Trace.Info($"Startup type of the agent - {startType}");
                }

                Trace.Info($"Setting the startup type in HostContext.StartupType");
                HostContext.StartupType = startType;

#if OS_WINDOWS
                if (store.IsAutoLogonConfigured())
                {
                    if(HostContext.StartupType != StartupType.Service)
                    {
                        Trace.Info($"Autologon is configured on the machine, dumping all the autologon related registry settings");
                        var autoLogonRegManager = HostContext.GetService<IAutoLogonRegistryManager>();
                        autoLogonRegManager.DumpAutoLogonRegistrySettings();
                    }
                    else
                    {
                        Trace.Info($"Autologon is configured on the machine but current Agent.Listner.exe is launched from the windows service");
                    }
                }
#endif
                // Run the agent interactively or as service
                return await RunAsync(settings, HostContext.StartupType == StartupType.Service);
            }
            finally
            {
                _term.CancelKeyPress -= CtrlCHandler;
                HostContext.Unloading -= Agent_Unloading;
                _completedCommand.Set();
            }
        }

        private void Agent_Unloading(object sender, EventArgs e)
        {
            if ((!_inConfigStage) && (!HostContext.AgentShutdownToken.IsCancellationRequested))
            {
                HostContext.ShutdownAgent(ShutdownReason.UserCancelled);
                _completedCommand.WaitOne(Constants.Agent.ExitOnUnloadTimeout);
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
                ConsoleCancelEventArgs cancelEvent = e as ConsoleCancelEventArgs;
                if (cancelEvent != null && HostContext.GetService<IConfigurationStore>().IsServiceConfigured())
                {
                    ShutdownReason reason;
                    if (cancelEvent.SpecialKey == ConsoleSpecialKey.ControlBreak)
                    {
                        Trace.Info("Received Ctrl-Break signal from agent service host, this indicate the operating system is shutting down.");
                        reason = ShutdownReason.OperatingSystemShutdown;
                    }
                    else
                    {
                        Trace.Info("Received Ctrl-C signal, stop agent.listener and agent.worker.");
                        reason = ShutdownReason.UserCancelled;
                    }

                    HostContext.ShutdownAgent(reason);
                }
                else
                {
                    HostContext.ShutdownAgent(ShutdownReason.UserCancelled);
                }
            }
        }

        //create worker manager, create message listener and start listening to the queue
        private async Task<int> RunAsync(AgentSettings settings, bool runAsService)
        {
            Trace.Info(nameof(RunAsync));
            _listener = HostContext.GetService<IMessageListener>();
            if (!await _listener.CreateSessionAsync(HostContext.AgentShutdownToken))
            {
                return Constants.Agent.ReturnCode.TerminatedError;
            }

            _term.WriteLine(StringUtil.Loc("ListenForJobs", DateTime.UtcNow));

            IJobDispatcher jobDispatcher = null;
            CancellationTokenSource messageQueueLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(HostContext.AgentShutdownToken);
            try
            {
                var notification = HostContext.GetService<IJobNotification>();
                if (!String.IsNullOrEmpty(settings.NotificationSocketAddress))
                {
                    notification.StartClient(settings.NotificationSocketAddress);
                }
                else
                {
                    notification.StartClient(settings.NotificationPipeName, HostContext.AgentShutdownToken);
                }
                // this is not a reliable way to disable auto update.
                // we need server side work to really enable the feature
                // https://github.com/Microsoft/vsts-agent/issues/446 (Feature: Allow agent / pool to opt out of automatic updates)
                bool disableAutoUpdate = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("agent.disableupdate"));
                bool autoUpdateInProgress = false;
                Task<bool> selfUpdateTask = null;
                jobDispatcher = HostContext.CreateService<IJobDispatcher>();

                while (!HostContext.AgentShutdownToken.IsCancellationRequested)
                {
                    TaskAgentMessage message = null;
                    bool skipMessageDeletion = false;
                    try
                    {
                        Task<TaskAgentMessage> getNextMessage = _listener.GetNextMessageAsync(messageQueueLoopTokenSource.Token);
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
                                    Trace.Info("Stop message queue looping.");
                                    messageQueueLoopTokenSource.Cancel();
                                    try
                                    {
                                        await getNextMessage;
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.Info($"Ignore any exception after cancel message loop. {ex}");
                                    }

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
                                    var agentUpdateMessage = JsonUtility.FromString<AgentRefreshMessage>(message.Body);
                                    var selfUpdater = HostContext.GetService<ISelfUpdater>();
                                    selfUpdateTask = selfUpdater.SelfUpdate(agentUpdateMessage, jobDispatcher, !runAsService, HostContext.AgentShutdownToken);
                                    Trace.Info("Refresh message received, kick-off selfupdate background process.");
                                }
                                else
                                {
                                    Trace.Info("Refresh message received, skip autoupdate since a previous autoupdate is already running.");
                                }
                            }
                        }
                        else if (string.Equals(message.MessageType, JobRequestMessageTypes.AgentJobRequest, StringComparison.OrdinalIgnoreCase))
                        {
                            if (autoUpdateInProgress)
                            {
                                skipMessageDeletion = true;
                            }
                            else
                            {
                                var newJobMessage = JsonUtility.FromString<AgentJobRequestMessage>(message.Body);
                                jobDispatcher.Run(newJobMessage);
                            }
                        }
                        else if (string.Equals(message.MessageType, JobCancelMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                        {
                            var cancelJobMessage = JsonUtility.FromString<JobCancelMessage>(message.Body);
                            bool jobCancelled = jobDispatcher.Cancel(cancelJobMessage);
                            skipMessageDeletion = autoUpdateInProgress && !jobCancelled;
                        }
                        else
                        {
                            Trace.Error($"Received message {message.MessageId} with unsupported message type {message.MessageType}.");
                        }
                    }
                    finally
                    {
                        if (!skipMessageDeletion && message != null)
                        {
                            try
                            {
                                await _listener.DeleteMessageAsync(message);
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
                await _listener.DeleteSessionAsync();

                messageQueueLoopTokenSource.Dispose();
            }

            return Constants.Agent.ReturnCode.Success;
        }

        private void PrintUsage()
        {
            _term.WriteLine(StringUtil.Loc("ListenerHelp"));
        }
    }
}
