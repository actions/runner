using GitHub.DistributedTask.WebApi;
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
                VssUtil.InitializeVssClientSettings(HostContext.UserAgents, HostContext.WebProxy);

                _inConfigStage = true;
                _completedCommand.Reset();
                _term.CancelKeyPress += CtrlCHandler;

                //register a SIGTERM handler
                HostContext.Unloading += Runner_Unloading;

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
                // this will optimizes the runner process startup time.
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
                        // We will guess the startup type only when the runner is configured as service and the guess will based on whether STDOUT/STDERR/STDIN been redirect or not
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

                    // Run the runner interactively or as service
                    return await RunAsync(settings, command.RunOnce);
                }
                else
                {
                    PrintUsage(command);
                    return Constants.Runner.ReturnCode.Success;
                }
            }
            finally
            {
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

        //create worker manager, create message listener and start listening to the queue
        private async Task<int> RunAsync(RunnerSettings settings, bool runOnce = false)
        {
            try
            {
                Trace.Info(nameof(RunAsync));
                _listener = HostContext.GetService<IMessageListener>();
                if (!await _listener.CreateSessionAsync(HostContext.RunnerShutdownToken))
                {
                    return Constants.Runner.ReturnCode.TerminatedError;
                }

                HostContext.WritePerfCounter("SessionCreated");
                _term.WriteLine($"{DateTime.UtcNow:u}: Listening for Jobs");

                IJobDispatcher jobDispatcher = null;
                CancellationTokenSource messageQueueLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(HostContext.RunnerShutdownToken);
                try
                {
                    var notification = HostContext.GetService<IJobNotification>();

                    notification.StartClient(settings.MonitorSocketAddress);

                    bool autoUpdateInProgress = false;
                    Task<bool> selfUpdateTask = null;
                    bool runOnceJobReceived = false;
                    jobDispatcher = HostContext.CreateService<IJobDispatcher>();

                    while (!HostContext.RunnerShutdownToken.IsCancellationRequested)
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
                                        Trace.Info("Auto update task finished at backend, an runner update is ready to apply exit the current runner instance.");
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
                                    var runnerUpdateMessage = JsonUtility.FromString<AgentRefreshMessage>(message.Body);
                                    var selfUpdater = HostContext.GetService<ISelfUpdater>();
                                    selfUpdateTask = selfUpdater.SelfUpdate(runnerUpdateMessage, jobDispatcher, !runOnce && HostContext.StartupType != StartupType.Service, HostContext.RunnerShutdownToken);
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
                                    var jobMessage = StringUtil.ConvertFromJson<Pipelines.AgentJobRequestMessage>(message.Body);
                                    jobDispatcher.Run(jobMessage, runOnce);
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
                Trace.Info("Runner OAuth token has been revoked. Shutting down.");
            }

            return Constants.Runner.ReturnCode.Success;
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
 .{separator}config.{ext} remove  Unconfigures the runner
 .{separator}run.{ext}            Runs the runner interactively. Does not require any options.

Options:
 --help     Prints the help for each command
 --version  Prints the runner version
 --commit   Prints the runner commit

Config Options:
 --unattended           Disable interactive prompts for missing arguments. Defaults will be used for missing options
 --url string           Repository to add the runner to. Required if unattended
 --token string         Registration token. Required if unattended
 --name string          Name of the runner to configure (default {Environment.MachineName ?? "myrunner"})
 --runnergroup string   Name of the runner group to add this runner to (defaults to the default runner group)
 --labels string        Extra labels in addition to the default: 'self-hosted,{Constants.Runner.Platform},{Constants.Runner.PlatformArchitecture}'
 --work string          Relative runner work directory (default {Constants.Path.WorkDirectory})
 --replace              Replace any existing runner with the same name (default false)");
#if OS_WINDOWS
    _term.WriteLine($@" --runasservice   Run the runner as a service");
    _term.WriteLine($@" --windowslogonaccount string   Account to run the service as. Requires runasservice");
    _term.WriteLine($@" --windowslogonpassword string  Password for the service account. Requires runasservice");
#endif
    _term.WriteLine($@"
Examples:
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
