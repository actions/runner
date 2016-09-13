using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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
                var proxyConfig = HostContext.GetService<IProxyConfiguration>();
                proxyConfig.ApplyProxySettings();

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
                bool runAsService = configManager.IsServiceConfigured();

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

                // Run the agent interactively or as service
                Trace.Verbose($"Run as service: '{runAsService}'");
                return await RunAsync(TokenSource.Token, settings, runAsService);

                //}

                // #if OS_WINDOWS
                //                 // this code is for migrated .net windows agent that running as windows service.
                //                 // leave the code as is untill we have a real plan for auto-migration.
                //                 if (runAsService && configManager.IsConfigured() && File.Exists(Path.Combine(IOUtil.GetBinPath(), "VsoAgentService.exe")))
                //                 {
                //                     // The old .net windows servicehost doesn't pass correct args while invoke Agent.Listener.exe
                //                     // When we detect the agent is a migrated .net windows agent, we will just run the agent.listener.exe even the servicehost doesn't pass correct args.
                //                     Trace.Verbose($"Run the agent for compat reason.");
                //                     return await RunAsync(TokenSource.Token, settings, runAsService);
                //                 }
                // #endif

                //                 Trace.Info("Doesn't match any existing command option, print usage.");
                //                 PrintUsage();
                //                 return Constants.Agent.ReturnCode.TerminatedError;
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
            if ((!_inConfigStage) && (!TokenSource.IsCancellationRequested))
            {
                TokenSource.Cancel();
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
                TokenSource.Cancel();
            }
        }

        //create worker manager, create message listener and start listening to the queue
        private async Task<int> RunAsync(CancellationToken token, AgentSettings settings, bool runAsService)
        {
            Trace.Info(nameof(RunAsync));
            _listener = HostContext.GetService<IMessageListener>();
            if (!await _listener.CreateSessionAsync(token))
            {
                return Constants.Agent.ReturnCode.TerminatedError;
            }

            _term.WriteLine(StringUtil.Loc("ListenForJobs", DateTime.UtcNow));

            IJobDispatcher jobDispatcher = null;
            CancellationTokenSource messageQueueLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                var notification = HostContext.GetService<IJobNotification>();
                notification.StartClient(settings.NotificationPipeName, token);
                // this is not a reliable way to disable auto update.
                // we need server side work to really enable the feature
                // https://github.com/Microsoft/vsts-agent/issues/446 (Feature: Allow agent / pool to opt out of automatic updates)
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
