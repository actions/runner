﻿using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Common.Util;
using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener
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
                var agentCertManager = HostContext.GetService<IAgentCertificateManager>();
                VssUtil.InitializeVssClientSettings(HostContext.UserAgent, agentWebProxy.WebProxy, agentCertManager.VssClientCertificateManager);

                _inConfigStage = true;
                _completedCommand.Reset();
                _term.CancelKeyPress += CtrlCHandler;

                //register a SIGTERM handler
                HostContext.Unloading += Agent_Unloading;

                // TODO Unit test to cover this logic
                Trace.Info(nameof(ExecuteCommand));
                var configManager = HostContext.GetService<IConfigurationManager>();

                // command is not required, if no command it just starts if configured

                // TODO: Invalid config prints usage

                if (command.Help)
                {
                    PrintUsage(command);
                    return Constants.Agent.ReturnCode.Success;
                }

                if (command.Version)
                {
                    _term.WriteLine(BuildConstants.RunnerPackage.Version);
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

                // remove config files, remove service, and exit
                if (command.Remove)
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

                // warmup agent process (JIT/CLR)
                // In scenarios where the agent is single use (used and then thrown away), the system provisioning the agent can call `Runner.Listener --warmup` before the machine is made available to the pool for use.
                // this will optimizes the agent process startup time.
                if (command.Warmup)
                {
                    var binDir = HostContext.GetDirectory(WellKnownDirectory.Bin);
                    foreach (var assemblyFile in Directory.EnumerateFiles(binDir, "*.dll"))
                    {
                        try
                        {
                            Trace.Info($"Load assembly: {assemblyFile}.");
                            var assembly = Assembly.LoadFrom(assemblyFile);
                            var types = assembly.GetTypes();
                            foreach (Type loadedType in types)
                            {
                                try
                                {
                                    Trace.Info($"Load methods: {loadedType.FullName}.");
                                    var methods = loadedType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                                    foreach (var method in methods)
                                    {
                                        if (!method.IsAbstract && !method.ContainsGenericParameters)
                                        {
                                            Trace.Verbose($"Prepare method: {method.Name}.");
                                            RuntimeHelpers.PrepareMethod(method.MethodHandle);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Trace.Error(ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.Error(ex);
                        }
                    }

                    return Constants.Agent.ReturnCode.Success;
                }

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
                    PrintUsage(command);
                    return Constants.Agent.ReturnCode.TerminatedError;
                }

                Trace.Verbose($"Configured as service: '{configuredAsService}'");

                //Get the startup type of the agent i.e., autostartup, service, manual
                StartupType startType;
                var startupTypeAsString = command.GetStartupType();
                if (string.IsNullOrEmpty(startupTypeAsString) && configuredAsService)
                {
                    // We need try our best to make the startup type accurate 
                    // The problem is coming from agent autoupgrade, which result an old version service host binary but a newer version agent binary
                    // At that time the servicehost won't pass --startuptype to Runner.Listener while the agent is actually running as service.
                    // We will guess the startup type only when the agent is configured as service and the guess will based on whether STDOUT/STDERR/STDIN been redirect or not
                    Trace.Info($"Try determine agent startup type base on console redirects.");
                    startType = (Console.IsErrorRedirected && Console.IsInputRedirected && Console.IsOutputRedirected) ? StartupType.Service : StartupType.Manual;
                }
                else
                {
                    if (!Enum.TryParse(startupTypeAsString, true, out startType))
                    {
                        Trace.Info($"Could not parse the argument value '{startupTypeAsString}' for StartupType. Defaulting to {StartupType.Manual}");
                        startType = StartupType.Manual;
                    }
                }

                Trace.Info($"Set agent startup type - {startType}");
                HostContext.StartupType = startType;

                // Run the agent interactively or as service
                return await RunAsync(settings, command.RunOnce);
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
                        Trace.Info("Received Ctrl-C signal, stop Runner.Listener and Runner.Worker.");
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
        private async Task<int> RunAsync(AgentSettings settings, bool runOnce = false)
        {
            try
            {
                Trace.Info(nameof(RunAsync));
                _listener = HostContext.GetService<IMessageListener>();
                if (!await _listener.CreateSessionAsync(HostContext.AgentShutdownToken))
                {
                    return Constants.Agent.ReturnCode.TerminatedError;
                }

                HostContext.WritePerfCounter("SessionCreated");
                _term.WriteLine(StringUtil.Loc("ListenForJobs", DateTime.UtcNow));

                IJobDispatcher jobDispatcher = null;
                CancellationTokenSource messageQueueLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(HostContext.AgentShutdownToken);
                try
                {
                    var notification = HostContext.GetService<IJobNotification>();
                    if (!String.IsNullOrEmpty(settings.NotificationSocketAddress))
                    {
                        notification.StartClient(settings.NotificationSocketAddress, settings.MonitorSocketAddress);
                    }
                    else
                    {
                        notification.StartClient(settings.NotificationPipeName, settings.MonitorSocketAddress, HostContext.AgentShutdownToken);
                    }

                    bool autoUpdateInProgress = false;
                    Task<bool> selfUpdateTask = null;
                    bool runOnceJobReceived = false;
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

                                        if (runOnce)
                                        {
                                            return Constants.Agent.ReturnCode.RunOnceAgentUpdating;
                                        }
                                        else
                                        {
                                            return Constants.Agent.ReturnCode.AgentUpdating;
                                        }
                                    }
                                    else
                                    {
                                        Trace.Info("Auto update task finished at backend, there is no available agent update needs to apply, continue message queue looping.");
                                    }
                                }
                            }

                            if (runOnceJobReceived)
                            {
                                Trace.Verbose("One time used agent has start running its job, waiting for getNextMessage or the job to finish.");
                                Task completeTask = await Task.WhenAny(getNextMessage, jobDispatcher.RunOnceJobCompleted.Task);
                                if (completeTask == jobDispatcher.RunOnceJobCompleted.Task)
                                {
                                    Trace.Info("Job has finished at backend, the agent will exit since it is running under onetime use mode.");
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

                                    return Constants.Agent.ReturnCode.Success;
                                }
                            }

                            message = await getNextMessage; //get next message
                            HostContext.WritePerfCounter($"MessageReceived_{message.MessageType}");
                            if (string.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                if (autoUpdateInProgress == false)
                                {
                                    autoUpdateInProgress = true;
                                    var agentUpdateMessage = JsonUtility.FromString<AgentRefreshMessage>(message.Body);
                                    var selfUpdater = HostContext.GetService<ISelfUpdater>();
                                    selfUpdateTask = selfUpdater.SelfUpdate(agentUpdateMessage, jobDispatcher, !runOnce && HostContext.StartupType != StartupType.Service, HostContext.AgentShutdownToken);
                                    Trace.Info("Refresh message received, kick-off selfupdate background process.");
                                }
                                else
                                {
                                    Trace.Info("Refresh message received, skip autoupdate since a previous autoupdate is already running.");
                                }
                            }
                            else if (string.Equals(message.MessageType, JobRequestMessageTypes.AgentJobRequest, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(message.MessageType, JobRequestMessageTypes.PipelineAgentJobRequest, StringComparison.OrdinalIgnoreCase))
                            {
                                if (autoUpdateInProgress || runOnceJobReceived)
                                {
                                    skipMessageDeletion = true;
                                    Trace.Info($"Skip message deletion for job request message '{message.MessageId}'.");
                                }
                                else
                                {
                                    Pipelines.AgentJobRequestMessage pipelineJobMessage = null;
                                    switch (message.MessageType)
                                    {
                                        case JobRequestMessageTypes.AgentJobRequest:
                                            var legacyJobMessage = JsonUtility.FromString<AgentJobRequestMessage>(message.Body);
                                            pipelineJobMessage = Pipelines.AgentJobRequestMessageUtil.Convert(legacyJobMessage);
                                            break;
                                        case JobRequestMessageTypes.PipelineAgentJobRequest:
                                            pipelineJobMessage = JsonUtility.FromString<Pipelines.AgentJobRequestMessage>(message.Body);
                                            break;
                                    }

                                    jobDispatcher.Run(pipelineJobMessage, runOnce);
                                    if (runOnce)
                                    {
                                        Trace.Info("One time used agent received job message.");
                                        runOnceJobReceived = true;
                                    }
                                }
                            }
                            else if (string.Equals(message.MessageType, JobCancelMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var cancelJobMessage = JsonUtility.FromString<JobCancelMessage>(message.Body);
                                bool jobCancelled = jobDispatcher.Cancel(cancelJobMessage);
                                skipMessageDeletion = (autoUpdateInProgress || runOnceJobReceived) && !jobCancelled;

                                if (skipMessageDeletion)
                                {
                                    Trace.Info($"Skip message deletion for cancellation message '{message.MessageId}'.");
                                }
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
            }
            catch (TaskAgentAccessTokenExpiredException)
            {
                Trace.Info("Agent OAuth token has been revoked. Shutting down.");
            }

            return Constants.Agent.ReturnCode.Success;
        }

        private void PrintUsage(CommandSettings command)
        {
            string separator;
            string ext;
#if OS_WINDOWS
            separator = "\\";
            ext = "cmd";
#else
            separator = "/";
            ext = "sh";
#endif

            string commonHelp = StringUtil.Loc("CommandLineHelp_Common");
            string envHelp = StringUtil.Loc("CommandLineHelp_Env");
            if (command.Configure)
            {
                _term.WriteLine(StringUtil.Loc("CommandLineHelp_Configure", separator, ext, commonHelp, envHelp));
            }
            else if (command.Remove)
            {
                _term.WriteLine(StringUtil.Loc("CommandLineHelp_Remove", separator, ext, commonHelp, envHelp));
            }
            else
            {
                _term.WriteLine(StringUtil.Loc("CommandLineHelp", separator, ext));
            }
        }
    }
}
