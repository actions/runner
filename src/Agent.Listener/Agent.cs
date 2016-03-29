using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(Agent))]
    public interface IAgent : IAgentService
    {
        CancellationTokenSource TokenSource { get; set; }
        Task<int> ExecuteCommand(CommandLineParser parser);
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

        public async Task<int> ExecuteCommand(CommandLineParser parser)
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

                if (parser.Flags.Contains("help"))
                {
                    PrintUsage();
                    return 0;
                }

                if (parser.Flags.Contains("version"))
                {
                    _term.WriteLine(Constants.Agent.Version);
                    return 0;
                }

                if (parser.Flags.Contains("commit"))
                {
                    _term.WriteLine(BuildConstants.Source.CommitHash);
                    return 0;
                }

                if (parser.IsCommand("unconfigure"))
                {
                    Trace.Info("unconfigure");
                    // TODO: Unconfiure, remove config and exit
                }

                if (parser.IsCommand("run") && !configManager.IsConfigured())
                {
                    Trace.Info("run");
                    _term.WriteError(StringUtil.Loc("AgentIsNotConfigured"));
                    PrintUsage();
                    return 1;
                }

                // unattend mode will not prompt for args if not supplied.  Instead will error.
                bool isUnattended = parser.Flags.Contains("unattended");

                if (parser.IsCommand("configure"))
                {
                    Trace.Info("configure");

                    try
                    {
                        await configManager.ConfigureAsync(parser.Args, parser.Flags, isUnattended);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        _term.WriteError(ex.Message);
                        return 1;
                    }
                }

                if (parser.Flags.Contains("nostart"))
                {
                    Trace.Info("No start option, exiting the agent");
                    return 0;
                }

                if (parser.IsCommand("run") && !configManager.IsConfigured())
                {
                    throw new InvalidOperationException("Cannot run. Must configure first.");
                }

                Trace.Info("Done evaluating commands");
                bool alreadyConfigured = configManager.IsConfigured();
                await configManager.EnsureConfiguredAsync();

                _inConfigStage = false;

                AgentSettings settings = configManager.LoadSettings();
                if (parser.IsCommand("run") || !settings.RunAsService)
                {
                    // Run the agent interactively
                    Trace.Verbose(
                        StringUtil.Format(
                            "Run command mentioned: {0}, run as service option mentioned:{1}",
                            parser.IsCommand("run"),
                            settings.RunAsService));

                    return await RunAsync(TokenSource.Token, settings);
                }

                if (alreadyConfigured)
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

        void CtrlCHandler(object sender, EventArgs e)
        {
            Quit();
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
            TaskAgentMessage message = null;
            try
            {
                using (var workerManager = HostContext.GetService<IWorkerManager>())
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            message = await listener.GetNextMessageAsync(token); //get next message

                            if (string.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                Trace.Warning("Referesh message received, but not yet handled by agent implementation.");
                            }
                            else if (string.Equals(message.MessageType, JobRequestMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var newJobMessage = JsonUtility.FromString<JobRequestMessage>(message.Body);
                                workerManager.Run(newJobMessage);
                            }
                            else if (string.Equals(message.MessageType, JobCancelMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var cancelJobMessage = JsonUtility.FromString<JobCancelMessage>(message.Body);
                                workerManager.Cancel(cancelJobMessage);
                            }
                        }
                        finally
                        {
                            if (message != null)
                            {
                                //TODO: make sure we don't mask more important exception
                                await DeleteMessageAsync(message);
                                message = null;
                            }
                        }
                    }
                }
            }
            finally
            {
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

        private void Quit()
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
    }
}
