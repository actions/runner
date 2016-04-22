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
                    return 0;
                }

                if (command.Version)
                {
                    _term.WriteLine(Constants.Agent.Version);
                    return 0;
                }

                if (command.Commit)
                {
                    _term.WriteLine(BuildConstants.Source.CommitHash);
                    return 0;
                }

                if (command.Unconfigure)
                {
                    // TODO: Unconfiure, remove config and exit
                }

                if (command.Run && !configManager.IsConfigured())
                {
                    _term.WriteError(StringUtil.Loc("AgentIsNotConfigured"));
                    PrintUsage();
                    return 1;
                }

                // unattend mode will not prompt for args if not supplied.  Instead will error.
                if (command.Configure)
                {
                    try
                    {
                        await configManager.ConfigureAsync(command);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        _term.WriteError(ex.Message);
                        return 1;
                    }
                }

                if (command.NoStart)
                {
                    return 0;
                }

                if (command.Run && !configManager.IsConfigured())
                {
                    // TODO: Is it possible to reach this code? It doesn't appear so.
                    // TODO: LOC
                    throw new InvalidOperationException("CanNotRunAgent");
                }

                Trace.Info("Done evaluating commands");
                await configManager.EnsureConfiguredAsync(command);

                _inConfigStage = false;

                AgentSettings settings = configManager.LoadSettings();
                if (command.Run || !settings.RunAsService)
                {
                    // Run the agent interactively
                    Trace.Verbose($"Run as service: '{settings.RunAsService}'");
                    return await RunAsync(TokenSource.Token, settings);
                }

                if (configManager.IsConfigured())
                {
                    // This is helpful if the user tries to start the agent.listener which is already configured or running as service
                    // However user can execute the agent by calling the run command
                    // TODO: Should we check if the service is running and prompt user to start the service if its not already running?
                    _term.WriteLine(StringUtil.Loc("ConfiguredAsRunAsService", settings.ServiceName));
                }

                return 0;
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
                Environment.Exit(1);
            }
            else
            {
                TokenSource.Cancel();
            }
        }

        //create worker manager, create message listener and start listening to the queue
        private async Task<int> RunAsync(CancellationToken token, AgentSettings settings)
        {
            Trace.Info(nameof(RunAsync));

            // Load the settings.
            _poolId = settings.PoolId;

            var listener = HostContext.GetService<IMessageListener>();
            if (!await listener.CreateSessionAsync(token))
            {
                return 1;
            }

            _term.WriteLine(StringUtil.Loc("ListenForJobs"));

            _sessionId = listener.Session.SessionId;
            IJobDispatcher jobDispatcher = null;
            try
            {
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
                                    return 3;
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
                                    selfUpdateTask = selfUpdater.SelfUpdate(jobDispatcher, !settings.RunAsService, token);
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

            return 0;
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
