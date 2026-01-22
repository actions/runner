using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Listener.Check;
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Sdk;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Jwt;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Listener
{
    [ServiceLocator(Default = typeof(Runner))]
    public interface IRunner : IRunnerService
    {
        Task<int> ExecuteCommand(CommandSettings command);
    }

    public sealed class Runner : RunnerService, IRunner
    {
        private IMessageListener _listener;
        private ITerminal _term;
        private bool _inConfigStage;
        private ManualResetEvent _completedCommand = new(false);
        private readonly ConcurrentQueue<string> _authMigrationTelemetries = new();
        private Task _authMigrationTelemetryTask;
        private readonly object _authMigrationTelemetryLock = new();
        private Task _authMigrationClaimsCheckTask;
        private readonly object _authMigrationClaimsCheckLock = new();
        private IRunnerServer _runnerServer;
        private CancellationTokenSource _authMigrationTelemetryTokenSource = new();
        private CancellationTokenSource _authMigrationClaimsCheckTokenSource = new();

        // <summary>
        // Helps avoid excessive calls to Run Service when encountering non-retriable errors from /acquirejob.
        // Normally we rely on the HTTP clients to back off between retry attempts. However, acquiring a job
        // involves calls to both Run Serivce and Broker. And Run Service and Broker communicate with each other
        // in an async fashion.
        //
        // When Run Service encounters a non-retriable error, it sends an async message to Broker. The runner will,
        // however, immediately call Broker to get the next message. If the async event from Run Service to Broker
        // has not yet been processed, the next message from Broker may be the same job message.
        //
        // The error throttler helps us back off when encountering successive, non-retriable errors from /acquirejob.
        // </summary>
        private IErrorThrottler _acquireJobThrottler;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = HostContext.GetService<ITerminal>();
            _acquireJobThrottler = HostContext.CreateService<IErrorThrottler>();
            _runnerServer = HostContext.GetService<IRunnerServer>();
        }

        public async Task<int> ExecuteCommand(CommandSettings command)
        {
            try
            {
                VssUtil.InitializeVssClientSettings(HostContext.UserAgents, HostContext.WebProxy);

                _inConfigStage = true;
                _completedCommand.Reset();
                _term.CancelKeyPress += CtrlCHandler;

                //register a SIGTERM handler
                HostContext.Unloading += Runner_Unloading;

                HostContext.AuthMigrationChanged += HandleAuthMigrationChanged;

                // TODO Unit test to cover this logic
                Trace.Info(nameof(ExecuteCommand));
                var configManager = HostContext.GetService<IConfigurationManager>();

                // command is not required, if no command it just starts if configured

                // TODO: Invalid config prints usage

                if (command.Help)
                {
                    PrintUsage(command);
                    return Constants.Runner.ReturnCode.Success;
                }

                if (command.Version)
                {
                    _term.WriteLine(BuildConstants.RunnerPackage.Version);
                    return Constants.Runner.ReturnCode.Success;
                }

                if (command.Commit)
                {
                    _term.WriteLine(BuildConstants.Source.CommitHash);
                    return Constants.Runner.ReturnCode.Success;
                }

                if (command.Check)
                {
                    var url = command.GetUrl();
                    var pat = command.GetGitHubPersonalAccessToken(required: true);
                    var checkExtensions = HostContext.GetService<IExtensionManager>().GetExtensions<ICheckExtension>();
                    var sortedChecks = checkExtensions.OrderBy(x => x.Order);
                    foreach (var check in sortedChecks)
                    {
                        _term.WriteLine($"**********************************************************************************************************************");
                        _term.WriteLine($"**  Check:               {check.CheckName}");
                        _term.WriteLine($"**  Description:         {check.CheckDescription}");
                        _term.WriteLine($"**********************************************************************************************************************");
                        var result = await check.RunCheck(url, pat);
                        if (!result)
                        {
                            _term.WriteLine($"**                                                                                                                  **");
                            _term.WriteLine($"**                                            F A I L                                                               **");
                            _term.WriteLine($"**                                                                                                                  **");
                            _term.WriteLine($"**********************************************************************************************************************");
                            _term.WriteLine($"** Log: {check.CheckLog}");
                            _term.WriteLine($"** Help Doc: {check.HelpLink}");
                            _term.WriteLine($"**********************************************************************************************************************");
                        }
                        else
                        {
                            _term.WriteLine($"**                                                                                                                  **");
                            _term.WriteLine($"**                                            P A S S                                                               **");
                            _term.WriteLine($"**                                                                                                                  **");
                            _term.WriteLine($"**********************************************************************************************************************");
                            _term.WriteLine($"** Log: {check.CheckLog}");
                            _term.WriteLine($"**********************************************************************************************************************");
                        }

                        _term.WriteLine();
                        _term.WriteLine();
                    }

                    return Constants.Runner.ReturnCode.Success;
                }

                // Configure runner prompt for args if not supplied
                // Unattended configure mode will not prompt for args if not supplied and error on any missing or invalid value.
                if (command.Configure)
                {
                    try
                    {
                        await configManager.ConfigureAsync(command);
                        return Constants.Runner.ReturnCode.Success;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        _term.WriteError(ex.Message);
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }
                }

                // remove config files, remove service, and exit
                if (command.Remove)
                {
                    // only remove local config files and exit
                    if (command.RemoveLocalConfig)
                    {
                        configManager.DeleteLocalRunnerConfig();
                        return Constants.Runner.ReturnCode.Success;
                    }
                    try
                    {
                        await configManager.UnconfigureAsync(command);
                        return Constants.Runner.ReturnCode.Success;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        _term.WriteError(ex.Message);
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }
                }

                _inConfigStage = false;

                // warmup runner process (JIT/CLR)
                // In scenarios where the runner is single use (used and then thrown away), the system provisioning the runner can call `Runner.Listener --warmup` before the machine is made available to the pool for use.
                // this will optimize the runner process startup time.
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

                    return Constants.Runner.ReturnCode.Success;
                }

                var base64JitConfig = command.GetJitConfig();
                if (!string.IsNullOrEmpty(base64JitConfig))
                {
                    try
                    {
                        var decodedJitConfig = Encoding.UTF8.GetString(Convert.FromBase64String(base64JitConfig));
                        var jitConfig = StringUtil.ConvertFromJson<Dictionary<string, string>>(decodedJitConfig);
                        foreach (var config in jitConfig)
                        {
                            var configFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), config.Key);
                            var configContent = Convert.FromBase64String(config.Value);
#if OS_WINDOWS
#pragma warning disable CA1416
                            if (configFile == HostContext.GetConfigFile(WellKnownConfigFile.RSACredentials))
                            {
                                configContent = ProtectedData.Protect(configContent, null, DataProtectionScope.LocalMachine);
                            }
#pragma warning restore CA1416
#endif
                            File.WriteAllBytes(configFile, configContent);
                            File.SetAttributes(configFile, File.GetAttributes(configFile) | FileAttributes.Hidden);
                            Trace.Info($"Saved {configContent.Length} bytes to '{configFile}'.");
                        }

                        // make sure we have the right user agent data added from the jitconfig
                        HostContext.LoadDefaultUserAgents();
                        VssUtil.InitializeVssClientSettings(HostContext.UserAgents, HostContext.WebProxy);
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        _term.WriteError(ex.Message);
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }
                }

                RunnerSettings settings = configManager.LoadSettings();

                var store = HostContext.GetService<IConfigurationStore>();
                bool configuredAsService = store.IsServiceConfigured();

                // Run runner
                if (command.Run) // this line is current break machine provisioner.
                {
                    // Error if runner not configured.
                    if (!configManager.IsConfigured())
                    {
                        _term.WriteError("Runner is not configured.");
                        PrintUsage(command);
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }

                    Trace.Verbose($"Configured as service: '{configuredAsService}'");

                    //Get the startup type of the runner i.e., autostartup, service, manual
                    StartupType startType;
                    var startupTypeAsString = command.GetStartupType();
                    if (string.IsNullOrEmpty(startupTypeAsString) && configuredAsService)
                    {
                        // We need try our best to make the startup type accurate
                        // The problem is coming from runner autoupgrade, which result an old version service host binary but a newer version runner binary
                        // At that time the servicehost won't pass --startuptype to Runner.Listener while the runner is actually running as service.
                        // We will guess the startup type only when the runner is configured as service and the guess will be based on whether STDOUT/STDERR/STDIN been redirect or not
                        Trace.Info($"Try determine runner startup type base on console redirects.");
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

                    Trace.Info($"Set runner startup type - {startType}");
                    HostContext.StartupType = startType;

                    if (command.RunOnce)
                    {
                        _term.WriteLine("Warning: '--once' is going to be deprecated in the future, please consider using '--ephemeral' during runner registration.", ConsoleColor.Yellow);
                        _term.WriteLine("https://docs.github.com/en/actions/hosting-your-own-runners/autoscaling-with-self-hosted-runners#using-ephemeral-runners-for-autoscaling", ConsoleColor.Yellow);
                    }

                    var cred = store.GetCredentials();
                    if (cred != null &&
                        cred.Scheme == Constants.Configuration.OAuth &&
                        cred.Data.ContainsKey("EnableAuthMigrationByDefault"))
                    {
                        Trace.Info("Enable auth migration by default.");
                        HostContext.EnableAuthMigration("EnableAuthMigrationByDefault");
                    }

                    // Run the runner interactively or as service
                    return await ExecuteRunnerAsync(settings, command.RunOnce || settings.Ephemeral);
                }
                else
                {
                    PrintUsage(command);
                    return Constants.Runner.ReturnCode.Success;
                }
            }
            finally
            {
                _authMigrationClaimsCheckTokenSource?.Cancel();
                _authMigrationTelemetryTokenSource?.Cancel();
                HostContext.AuthMigrationChanged -= HandleAuthMigrationChanged;
                _term.CancelKeyPress -= CtrlCHandler;
                HostContext.Unloading -= Runner_Unloading;
                _completedCommand.Set();
            }
        }

        private void Runner_Unloading(object sender, EventArgs e)
        {
            if ((!_inConfigStage) && (!HostContext.RunnerShutdownToken.IsCancellationRequested))
            {
                HostContext.ShutdownRunner(ShutdownReason.UserCancelled);
                _completedCommand.WaitOne(Constants.Runner.ExitOnUnloadTimeout);
            }
        }

        private void CtrlCHandler(object sender, EventArgs e)
        {
            _term.WriteLine("Exiting...");
            if (_inConfigStage)
            {
                HostContext.Dispose();
                Environment.Exit(Constants.Runner.ReturnCode.TerminatedError);
            }
            else
            {
                ConsoleCancelEventArgs cancelEvent = e as ConsoleCancelEventArgs;
                if (cancelEvent != null && HostContext.GetService<IConfigurationStore>().IsServiceConfigured())
                {
                    ShutdownReason reason;
                    if (cancelEvent.SpecialKey == ConsoleSpecialKey.ControlBreak)
                    {
                        Trace.Info("Received Ctrl-Break signal from runner service host, this indicate the operating system is shutting down.");
                        reason = ShutdownReason.OperatingSystemShutdown;
                    }
                    else
                    {
                        Trace.Info("Received Ctrl-C signal, stop Runner.Listener and Runner.Worker.");
                        reason = ShutdownReason.UserCancelled;
                    }

                    HostContext.ShutdownRunner(reason);
                }
                else
                {
                    HostContext.ShutdownRunner(ShutdownReason.UserCancelled);
                }
            }
        }

        private IMessageListener GetMessageListener(RunnerSettings settings, bool isMigratedSettings = false)
        {
            if (settings.UseV2Flow)
            {
                Trace.Info($"Using BrokerMessageListener");
                var brokerListener = new BrokerMessageListener(settings, isMigratedSettings);
                brokerListener.Initialize(HostContext);
                return brokerListener;
            }

            return HostContext.GetService<IMessageListener>();
        }

        //create worker manager, create message listener and start listening to the queue
        private async Task<int> RunAsync(RunnerSettings settings, bool runOnce = false)
        {
            try
            {
                Trace.Info(nameof(RunAsync));
                
                // First try using migrated settings if available
                var configManager = HostContext.GetService<IConfigurationManager>();
                RunnerSettings migratedSettings = null;
                
                try 
                {
                    migratedSettings = configManager.LoadMigratedSettings();
                    Trace.Info("Loaded migrated settings from .runner_migrated file");
                    Trace.Info(migratedSettings);
                }
                catch (Exception ex)
                {
                    // If migrated settings file doesn't exist or can't be loaded, we'll use the provided settings
                    Trace.Info($"Failed to load migrated settings: {ex.Message}");
                }
                
                bool usedMigratedSettings = false;
                
                if (migratedSettings != null)
                {
                    // Try to create session with migrated settings first
                    Trace.Info("Attempting to create session using migrated settings");
                    _listener = GetMessageListener(migratedSettings, isMigratedSettings: true);
                    
                    try
                    {
                        CreateSessionResult createSessionResult = await _listener.CreateSessionAsync(HostContext.RunnerShutdownToken);
                        if (createSessionResult == CreateSessionResult.Success)
                        {
                            Trace.Info("Successfully created session with migrated settings");
                            settings = migratedSettings; // Use migrated settings for the rest of the process
                            usedMigratedSettings = true;
                        }
                        else
                        {
                            Trace.Warning($"Failed to create session with migrated settings: {createSessionResult}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Exception when creating session with migrated settings: {ex}");
                    }
                }
                
                // If migrated settings weren't used or session creation failed, use original settings
                if (!usedMigratedSettings)
                {
                    Trace.Info("Falling back to original .runner settings");
                    _listener = GetMessageListener(settings);
                    CreateSessionResult createSessionResult = await _listener.CreateSessionAsync(HostContext.RunnerShutdownToken);
                    if (createSessionResult == CreateSessionResult.SessionConflict)
                    {
                        return Constants.Runner.ReturnCode.SessionConflict;
                    }
                    else if (createSessionResult == CreateSessionResult.Failure)
                    {
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }
                }

                HostContext.WritePerfCounter("SessionCreated");

                _term.WriteLine($"Current runner version: '{BuildConstants.RunnerPackage.Version}'");
                _term.WriteLine($"{DateTime.UtcNow:u}: Listening for Jobs");

                IJobDispatcher jobDispatcher = null;
                CancellationTokenSource messageQueueLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(HostContext.RunnerShutdownToken);

                // Should we try to cleanup ephemeral runners
                bool runOnceJobCompleted = false;
                bool skipSessionDeletion = false;
                bool restartSession = false; // Flag to indicate session restart
                bool restartSessionPending = false;
                try
                {
                    var notification = HostContext.GetService<IJobNotification>();

                    notification.StartClient(settings.MonitorSocketAddress);

                    bool autoUpdateInProgress = false;
                    Task<bool> selfUpdateTask = null;
                    bool runOnceJobReceived = false;
                    jobDispatcher = HostContext.CreateService<IJobDispatcher>();

                    jobDispatcher.JobStatus += _listener.OnJobStatus;

                    while (!HostContext.RunnerShutdownToken.IsCancellationRequested)
                    {
                        // Check if we need to restart the session and can do so (job dispatcher not busy)
                        if (restartSessionPending && !jobDispatcher.Busy)
                        {
                            Trace.Info("Pending session restart detected and job dispatcher is not busy. Restarting session now.");
                            messageQueueLoopTokenSource.Cancel();
                            restartSession = true;
                            break;
                        }
                        
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
                                        Trace.Info("Auto update task finished at backend, a runner update is ready to apply exit the current runner instance.");
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
                                            return Constants.Runner.ReturnCode.RunOnceRunnerUpdating;
                                        }
                                        else
                                        {
                                            return Constants.Runner.ReturnCode.RunnerUpdating;
                                        }
                                    }
                                    else
                                    {
                                        Trace.Info("Auto update task finished at backend, there is no available runner update needs to apply, continue message queue looping.");
                                    }
                                }
                            }

                            if (runOnceJobReceived)
                            {
                                Trace.Verbose("One time used runner has start running its job, waiting for getNextMessage or the job to finish.");
                                Task completeTask = await Task.WhenAny(getNextMessage, jobDispatcher.RunOnceJobCompleted.Task);
                                if (completeTask == jobDispatcher.RunOnceJobCompleted.Task)
                                {
                                    runOnceJobCompleted = true;
                                    Trace.Info("Job has finished at backend, the runner will exit since it is running under onetime use mode.");
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

                                    return Constants.Runner.ReturnCode.Success;
                                }
                            }

                            message = await getNextMessage; //get next message
                            HostContext.WritePerfCounter($"MessageReceived_{message.MessageType}");
                            if (string.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                if (autoUpdateInProgress == false)
                                {
                                    autoUpdateInProgress = true;
                                    AgentRefreshMessage runnerUpdateMessage = JsonUtility.FromString<AgentRefreshMessage>(message.Body);

#if DEBUG
                                    // Can mock the update for testing
                                    if (StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_IS_MOCK_UPDATE")))
                                    {

                                        // The mock_update_messages.json file should be an object with keys being the current version and values being the targeted mock version object
                                        // Example: { "2.283.2": {"targetVersion":"2.284.1"}, "2.284.1": {"targetVersion":"2.285.0"}}
                                        var mockUpdatesPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), "mock_update_messages.json");
                                        if (File.Exists(mockUpdatesPath))
                                        {
                                            var mockUpdateMessages = JsonUtility.FromString<Dictionary<string, AgentRefreshMessage>>(File.ReadAllText(mockUpdatesPath));
                                            if (mockUpdateMessages.ContainsKey(BuildConstants.RunnerPackage.Version))
                                            {
                                                var mockTargetVersion = mockUpdateMessages[BuildConstants.RunnerPackage.Version].TargetVersion;
                                                _term.WriteLine($"Mocking update, using version {mockTargetVersion} instead of {runnerUpdateMessage.TargetVersion}");
                                                Trace.Info($"Mocking update, using version {mockTargetVersion} instead of {runnerUpdateMessage.TargetVersion}");
                                                runnerUpdateMessage = new AgentRefreshMessage(runnerUpdateMessage.AgentId, mockTargetVersion, runnerUpdateMessage.Timeout);
                                            }
                                        }
                                    }
#endif
                                    var selfUpdater = HostContext.GetService<ISelfUpdater>();
                                    selfUpdateTask = selfUpdater.SelfUpdate(runnerUpdateMessage, jobDispatcher, false, HostContext.RunnerShutdownToken);
                                    Trace.Info("Refresh message received, kick-off selfupdate background process.");
                                }
                                else
                                {
                                    Trace.Info("Refresh message received, skip autoupdate since a previous autoupdate is already running.");
                                }
                            }
                            else if (string.Equals(message.MessageType, RunnerRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                if (autoUpdateInProgress == false)
                                {
                                    autoUpdateInProgress = true;
                                    RunnerRefreshMessage brokerRunnerUpdateMessage = JsonUtility.FromString<RunnerRefreshMessage>(message.Body);

                                    var selfUpdater = HostContext.GetService<ISelfUpdaterV2>();
                                    selfUpdateTask = selfUpdater.SelfUpdate(brokerRunnerUpdateMessage, jobDispatcher, false, HostContext.RunnerShutdownToken);
                                    Trace.Info("Refresh message received, kick-off selfupdate background process.");
                                }
                                else
                                {
                                    Trace.Info("Refresh message received, skip autoupdate since a previous autoupdate is already running.");
                                }
                            }
                            else if (string.Equals(message.MessageType, JobRequestMessageTypes.PipelineAgentJobRequest, StringComparison.OrdinalIgnoreCase))
                            {
                                if (autoUpdateInProgress || runOnceJobReceived)
                                {
                                    skipMessageDeletion = true;
                                    Trace.Info($"Skip message deletion for job request message '{message.MessageId}'.");
                                }
                                else
                                {
                                    Trace.Info($"Received job message of length {message.Body.Length} from service, with hash '{IOUtil.GetSha256Hash(message.Body)}'");
                                    var jobMessage = StringUtil.ConvertFromJson<Pipelines.AgentJobRequestMessage>(message.Body);
                                    jobDispatcher.Run(jobMessage, runOnce);
                                    if (runOnce)
                                    {
                                        Trace.Info("One time used runner received job message.");
                                        runOnceJobReceived = true;
                                    }
                                }
                            }
                            // Broker flow
                            else if (MessageUtil.IsRunServiceJob(message.MessageType))
                            {
                                if (autoUpdateInProgress || runOnceJobReceived)
                                {
                                    skipMessageDeletion = true;
                                    Trace.Info($"Skip message deletion for job request message '{message.MessageId}'.");
                                }
                                else
                                {
                                    var messageRef = StringUtil.ConvertFromJson<RunnerJobRequestRef>(message.Body);

                                    // Acknowledge (best-effort)
                                    if (messageRef.ShouldAcknowledge) // Temporary feature flag
                                    {
                                        try
                                        {
                                            await _listener.AcknowledgeMessageAsync(messageRef.RunnerRequestId, messageQueueLoopTokenSource.Token);
                                        }
                                        catch (Exception ex)
                                        {
                                            Trace.Error($"Best-effort acknowledge failed for request '{messageRef.RunnerRequestId}'");
                                            Trace.Error(ex);
                                        }
                                    }

                                    Pipelines.AgentJobRequestMessage jobRequestMessage = null;
                                    if (string.IsNullOrEmpty(messageRef.RunServiceUrl))
                                    {
                                        // Connect
                                        var credMgr = HostContext.GetService<ICredentialManager>();
                                        var creds = credMgr.LoadCredentials(allowAuthUrlV2: false);
                                        var actionsRunServer = HostContext.CreateService<IActionsRunServer>();
                                        await actionsRunServer.ConnectAsync(new Uri(settings.ServerUrl), creds);

                                        // Get job message
                                        jobRequestMessage = await actionsRunServer.GetJobMessageAsync(messageRef.RunnerRequestId, messageQueueLoopTokenSource.Token);
                                    }
                                    else
                                    {
                                        // Connect
                                        var credMgr = HostContext.GetService<ICredentialManager>();
                                        var credsV2 = credMgr.LoadCredentials(allowAuthUrlV2: true);
                                        var runServer = HostContext.CreateService<IRunServer>();
                                        await runServer.ConnectAsync(new Uri(messageRef.RunServiceUrl), credsV2);

                                        // Get job message
                                        try
                                        {
                                            jobRequestMessage = await runServer.GetJobMessageAsync(messageRef.RunnerRequestId, messageRef.BillingOwnerId, messageQueueLoopTokenSource.Token);
                                            _acquireJobThrottler.Reset();
                                        }
                                        catch (Exception ex) when (
                                            ex is TaskOrchestrationJobNotFoundException ||          // HTTP status 404
                                            ex is TaskOrchestrationJobAlreadyAcquiredException ||   // HTTP status 409
                                            ex is TaskOrchestrationJobUnprocessableException)       // HTTP status 422
                                        {
                                            Trace.Info($"Skipping message Job. {ex.Message}");
                                            await _acquireJobThrottler.IncrementAndWaitAsync(messageQueueLoopTokenSource.Token);
                                            continue;
                                        }
                                        catch (Exception ex)
                                        {
                                            Trace.Error($"Caught exception from acquiring job message: {ex}");

                                            if (HostContext.AllowAuthMigration)
                                            {
                                                Trace.Info("Disable migration mode for 60 minutes.");
                                                HostContext.DeferAuthMigration(TimeSpan.FromMinutes(60), $"Acquire job failed with exception: {ex}");
                                            }

                                            continue;
                                        }
                                    }

                                    // Dispatch
                                    jobDispatcher.Run(jobRequestMessage, runOnce);

                                    // Run once?
                                    if (runOnce)
                                    {
                                        Trace.Info("One time used runner received job message.");
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
                            else if (string.Equals(message.MessageType, Pipelines.HostedRunnerShutdownMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var HostedRunnerShutdownMessage = JsonUtility.FromString<Pipelines.HostedRunnerShutdownMessage>(message.Body);
                                skipMessageDeletion = true;
                                skipSessionDeletion = true;
                                Trace.Info($"Service requests the hosted runner to shutdown. Reason: '{HostedRunnerShutdownMessage.Reason}'.");
                                return Constants.Runner.ReturnCode.Success;
                            }
                            else if (string.Equals(message.MessageType, TaskAgentMessageTypes.ForceTokenRefresh))
                            {
                                Trace.Info("Received ForceTokenRefreshMessage");
                                await _listener.RefreshListenerTokenAsync();
                            }
                            else if (string.Equals(message.MessageType, RunnerRefreshConfigMessage.MessageType))
                            {
                                var runnerRefreshConfigMessage = JsonUtility.FromString<RunnerRefreshConfigMessage>(message.Body);
                                Trace.Info($"Received RunnerRefreshConfigMessage for '{runnerRefreshConfigMessage.ConfigType}' config file");
                                var configUpdater = HostContext.GetService<IRunnerConfigUpdater>();
                                await configUpdater.UpdateRunnerConfigAsync(
                                    runnerQualifiedId: runnerRefreshConfigMessage.RunnerQualifiedId,
                                    configType: runnerRefreshConfigMessage.ConfigType,
                                    serviceType: runnerRefreshConfigMessage.ServiceType,
                                    configRefreshUrl: runnerRefreshConfigMessage.ConfigRefreshUrl);

                                // Set flag to schedule session restart if ConfigType is "runner"
                                if (string.Equals(runnerRefreshConfigMessage.ConfigType, "runner", StringComparison.OrdinalIgnoreCase))
                                {
                                    Trace.Info("Runner configuration was updated. Session restart has been scheduled");
                                    restartSessionPending = true;
                                }
                                else
                                {
                                    Trace.Info($"No session restart needed for config type: {runnerRefreshConfigMessage.ConfigType}");
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
                        jobDispatcher.JobStatus -= _listener.OnJobStatus;
                        await jobDispatcher.ShutdownAsync();
                    }

                    if (!skipSessionDeletion)
                    {
                        try
                        {
                            Trace.Info("Deleting Runner Session...");
                            await _listener.DeleteSessionAsync();
                        }
                        catch (Exception ex) when (runOnce)
                        {
                            // ignore exception during delete session for ephemeral runner since the runner might already be deleted from the server side
                            // and the delete session call will end up with 401.
                            Trace.Info($"Ignore any exception during DeleteSession for an ephemeral runner. {ex}");
                        }
                    }

                    messageQueueLoopTokenSource.Dispose();

                    if (settings.Ephemeral && runOnceJobCompleted)
                    {
                        configManager.DeleteLocalRunnerConfig();
                    }
                }

                // After cleanup, check if we need to restart the session
                if (restartSession)
                {
                    Trace.Info("Restarting runner session after config update...");
                    return Constants.Runner.ReturnCode.RunnerConfigurationRefreshed;
                }
            }
            catch (TaskAgentAccessTokenExpiredException)
            {
                Trace.Info("Runner OAuth token has been revoked. Shutting down.");
            }
            catch (HostedRunnerDeprovisionedException)
            {
                Trace.Info("Hosted runner has been deprovisioned. Shutting down.");
            }

            return Constants.Runner.ReturnCode.Success;
        }

        private async Task<int> ExecuteRunnerAsync(RunnerSettings settings, bool runOnce)
        {
            int returnCode = Constants.Runner.ReturnCode.Success;
            bool restart = false;
            do
            {
                restart = false;
                returnCode = await RunAsync(settings, runOnce);
                
                if (returnCode == Constants.Runner.ReturnCode.RunnerConfigurationRefreshed)
                {
                    Trace.Info("Runner configuration was refreshed, restarting session...");
                    // Reload settings in case they changed
                    var configManager = HostContext.GetService<IConfigurationManager>();
                    settings = configManager.LoadSettings();
                    restart = true;
                }
            } while (restart);

            return returnCode;
        }

        private void HandleAuthMigrationChanged(object sender, AuthMigrationEventArgs e)
        {
            Trace.Verbose("Handle AuthMigrationChanged in Runner");
            _authMigrationTelemetries.Enqueue($"{DateTime.UtcNow.ToString("O")}: {e.Trace}");

            // only start the telemetry reporting task once auth migration is changed (enabled or disabled)
            lock (_authMigrationTelemetryLock)
            {
                if (_authMigrationTelemetryTask == null)
                {
                    _authMigrationTelemetryTask = ReportAuthMigrationTelemetryAsync(_authMigrationTelemetryTokenSource.Token);
                }
            }

            // only start the claims check task once auth migration is changed (enabled or disabled)
            lock (_authMigrationClaimsCheckLock)
            {
                if (_authMigrationClaimsCheckTask == null)
                {
                    _authMigrationClaimsCheckTask = CheckOAuthTokenClaimsAsync(_authMigrationClaimsCheckTokenSource.Token);
                }
            }
        }

        private async Task CheckOAuthTokenClaimsAsync(CancellationToken token)
        {
            string[] expectedClaims =
            [
                "owner_id",
                "runner_id",
                "runner_group_id",
                "scale_set_id",
                "is_ephemeral",
                "labels"
            ];

            try
            {
                var credMgr = HostContext.GetService<ICredentialManager>();
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await HostContext.Delay(TimeSpan.FromMinutes(100), token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore cancellation
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!HostContext.AllowAuthMigration)
                    {
                        Trace.Info("Skip checking oauth token claims since auth migration is disabled.");
                        continue;
                    }

                    var baselineCred = credMgr.LoadCredentials(allowAuthUrlV2: false);
                    var authV2Cred = credMgr.LoadCredentials(allowAuthUrlV2: true);

                    if (!(baselineCred.Federated is VssOAuthCredential baselineVssOAuthCred) ||
                        !(authV2Cred.Federated is VssOAuthCredential vssOAuthCredV2) ||
                        baselineVssOAuthCred == null ||
                        vssOAuthCredV2 == null)
                    {
                        Trace.Info("Skip checking oauth token claims for non-oauth credentials");
                        continue;
                    }

                    if (string.Equals(baselineVssOAuthCred.AuthorizationUrl.AbsoluteUri, vssOAuthCredV2.AuthorizationUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.Info("Skip checking oauth token claims for same authorization url");
                        continue;
                    }

                    var baselineProvider = baselineVssOAuthCred.GetTokenProvider(baselineVssOAuthCred.AuthorizationUrl);
                    var v2Provider = vssOAuthCredV2.GetTokenProvider(vssOAuthCredV2.AuthorizationUrl);
                    try
                    {
                        using (var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                        using (var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutTokenSource.Token))
                        {
                            var baselineToken = await baselineProvider.GetTokenAsync(null, requestTokenSource.Token);
                            var v2Token = await v2Provider.GetTokenAsync(null, requestTokenSource.Token);
                            if (baselineToken is VssOAuthAccessToken baselineAccessToken &&
                                v2Token is VssOAuthAccessToken v2AccessToken &&
                                !string.IsNullOrEmpty(baselineAccessToken.Value) &&
                                !string.IsNullOrEmpty(v2AccessToken.Value))
                            {
                                var baselineJwt = JsonWebToken.Create(baselineAccessToken.Value);
                                var baselineClaims = baselineJwt.ExtractClaims();
                                var v2Jwt = JsonWebToken.Create(v2AccessToken.Value);
                                var v2Claims = v2Jwt.ExtractClaims();

                                // Log extracted claims for debugging
                                Trace.Verbose($"Baseline token expected claims: {string.Join(", ", baselineClaims
                                    .Where(c => expectedClaims.Contains(c.Type.ToLowerInvariant()))
                                    .Select(c => $"{c.Type}:{c.Value}"))}");
                                Trace.Verbose($"V2 token expected claims: {string.Join(", ", v2Claims
                                    .Where(c => expectedClaims.Contains(c.Type.ToLowerInvariant()))
                                    .Select(c => $"{c.Type}:{c.Value}"))}");

                                foreach (var claim in expectedClaims)
                                {
                                    // if baseline has the claim, v2 should have it too with exactly same value.
                                    if (baselineClaims.FirstOrDefault(c => c.Type.ToLowerInvariant() == claim) is Claim baselineClaim &&
                                        !string.IsNullOrEmpty(baselineClaim?.Value))
                                    {
                                        var v2Claim = v2Claims.FirstOrDefault(c => c.Type.ToLowerInvariant() == claim);
                                        if (v2Claim?.Value != baselineClaim.Value)
                                        {
                                            Trace.Info($"Token Claim mismatch between two issuers. Expected: {baselineClaim.Type}:{baselineClaim.Value}. Actual: {v2Claim?.Type ?? "Empty"}:{v2Claim?.Value ?? "Empty"}");
                                            HostContext.DeferAuthMigration(TimeSpan.FromMinutes(60), $"Expected claim {baselineClaim.Type}:{baselineClaim.Value} does not match {v2Claim?.Type ?? "Empty"}:{v2Claim?.Value ?? "Empty"}");
                                            break;
                                        }
                                    }
                                }

                                Trace.Info("OAuth token claims check passed.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.Error("Failed to fetch and check OAuth token claims.");
                        Trace.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Error("Failed to check OAuth token claims in background.");
                Trace.Error(ex);
            }
        }

        private async Task ReportAuthMigrationTelemetryAsync(CancellationToken token)
        {
            var configManager = HostContext.GetService<IConfigurationManager>();
            var runnerSettings = configManager.LoadSettings();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await HostContext.Delay(TimeSpan.FromSeconds(60), token);
                }
                catch (TaskCanceledException)
                {
                    // Ignore cancellation
                }

                Trace.Verbose("Checking for auth migration telemetry to report");
                while (_authMigrationTelemetries.TryDequeue(out var telemetry))
                {
                    Trace.Verbose($"Reporting auth migration telemetry: {telemetry}");
                    if (runnerSettings != null)
                    {
                        try
                        {
                            using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                            {
                                await _runnerServer.UpdateAgentUpdateStateAsync(runnerSettings.PoolId, runnerSettings.AgentId, "RefreshConfig", telemetry, tokenSource.Token);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.Error("Failed to report auth migration telemetry.");
                            Trace.Error(ex);
                            _authMigrationTelemetries.Enqueue(telemetry);
                        }
                    }

                    if (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await HostContext.Delay(TimeSpan.FromSeconds(10), token);
                        }
                        catch (TaskCanceledException)
                        {
                            // Ignore cancellation
                        }
                    }
                }
            }
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
            _term.WriteLine($@"
Commands:
 .{separator}config.{ext}         Configures the runner
 .{separator}config.{ext} remove  Deconfigures the runner
 .{separator}run.{ext}            Runs the runner interactively. Does not require any options.

Options:
 --help     Prints the help for each command
 --version  Prints the runner version
 --commit   Prints the runner commit
 --check    Check the runner's network connectivity with GitHub server

Config Options:
 --unattended           Disable interactive prompts for missing arguments. Defaults will be used for missing options
 --url string           Repository to add the runner to. Required if unattended
 --token string         Registration token. Required if unattended
 --name string          Name of the runner to configure (default {Environment.MachineName ?? "myrunner"})
 --runnergroup string   Name of the runner group to add this runner to (defaults to the default runner group)
 --labels string        Custom labels that will be added to the runner. This option is mandatory if --no-default-labels is used.
 --no-default-labels    Disables adding the default labels: 'self-hosted,{Constants.Runner.Platform},{Constants.Runner.PlatformArchitecture}'
 --local                Removes the runner config files from your local machine. Used as an option to the remove command
 --work string          Relative runner work directory (default {Constants.Path.WorkDirectory})
 --replace              Replace any existing runner with the same name (default false)
 --pat                  GitHub personal access token with repo scope. Used for checking network connectivity when executing `.{separator}run.{ext} --check`
 --disableupdate        Disable self-hosted runner automatic update to the latest released version`
 --ephemeral            Configure the runner to only take one job and then let the service deconfigure the runner after the job finishes (default false)");

#if OS_WINDOWS
    _term.WriteLine($@" --runasservice   Run the runner as a service");
    _term.WriteLine($@" --windowslogonaccount string   Account to run the service as. Requires runasservice");
    _term.WriteLine($@" --windowslogonpassword string  Password for the service account. Requires runasservice");
#endif
            _term.WriteLine($@"
Examples:
 Check GitHub server network connectivity:
  .{separator}run.{ext} --check --url <url> --pat <pat>
 Configure a runner non-interactively:
  .{separator}config.{ext} --unattended --url <url> --token <token>
 Configure a runner non-interactively, replacing any existing runner with the same name:
  .{separator}config.{ext} --unattended --url <url> --token <token> --replace [--name <name>]
 Configure a runner non-interactively with three extra labels:
  .{separator}config.{ext} --unattended --url <url> --token <token> --labels L1,L2,L3");
#if OS_WINDOWS
    _term.WriteLine($@" Configure a runner to run as a service:");
    _term.WriteLine($@"  .{separator}config.{ext} --url <url> --token <token> --runasservice");
#endif
        }
    }
}
