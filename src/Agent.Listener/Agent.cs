using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(Agent))]
    public interface IAgent : IAgentService
    {
        Task<int> ExecuteCommand(CommandLineParser parser);
    }

    public sealed class Agent : AgentService, IAgent
    {
        private Guid _sessionId = Guid.Empty;
        private int _poolId;
        
        public async Task<int> ExecuteCommand(CommandLineParser parser)
        {
            // TODO Unit test to cover this logic
            Trace.Info("ExecuteCommand()");

            var configManager = HostContext.GetService<IConfigurationManager>();
            Trace.Info("Created configuration manager");

            // command is not required, if no command it just starts and/or configures if not configured

            // TODO: Invalid config prints usage

            if (parser.Flags.Contains("help"))
            {
                Trace.Info("help");
                PrintUsage();
            }

            if (parser.IsCommand("unconfigure"))
            {
                Trace.Info("unconfigure");
                // TODO: Unconfiure, remove config and exit
            }

            if (parser.IsCommand("run") && !configManager.IsConfigured())
            {
                Trace.Info("run");
                Console.WriteLine("Agent is not configured");
                PrintUsage();
            }

            // unattend mode will not prompt for args if not supplied.  Instead will error.
            bool isUnattended = parser.Flags.Contains("unattended");

            if (parser.IsCommand("configure"))
            {
                Trace.Info("configure");    
                await configManager.ConfigureAsync(parser.Args, isUnattended);
                return 0;
            }

            if (parser.Flags.Contains("nostart"))
            {
                Trace.Info("No start option, exiting the agent");
                return 0;
            }

            if (parser.IsCommand("run") && !configManager.IsConfigured())
            {
                throw new InvalidOperationException("Cannot run.  Must configure first.");
            }

            Trace.Info("Done evaluating commands");
            await configManager.EnsureConfiguredAsync();
            
            return await RunAsync();
        }
                
        //create worker manager, create message listener and start listening to the queue
        private async Task<int> RunAsync()
        {
            Trace.Info("RunAsync()");
            
            var term = HostContext.GetService<ITerminal>();

            var configManager = HostContext.GetService<IConfigurationManager>();
            AgentSettings settings = configManager.LoadSettings();
            _poolId = settings.PoolId;
            
            var listener = HostContext.GetService<IMessageListener>();
            if (!await listener.CreateSessionAsync())
            {
                return 1;
            }
            term.WriteLine("Listening for Jobs");
            
            _sessionId = listener.Session.SessionId;
            TaskAgentMessage message = null;
            try
            {
                using (var workerManager = HostContext.GetService<IWorkerManager>())
                {
                    while (true)
                    {
                        try
                        {
                            HostContext.CancellationToken.ThrowIfCancellationRequested();
                            message = await listener.GetNextMessageAsync(); //get next message

                            if (String.Equals(message.MessageType, AgentRefreshMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                Trace.Warning("Referesh message received, but not yet handled by agent implementation.");
                            }
                            else if (String.Equals(message.MessageType, JobRequestMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var newJobMessage = JsonUtility.FromString<JobRequestMessage>(message.Body);
                                await workerManager.Run(newJobMessage);
                            }
                            else if (String.Equals(message.MessageType, JobCancelMessage.MessageType, StringComparison.OrdinalIgnoreCase))
                            {
                                var cancelJobMessage = JsonUtility.FromString<JobCancelMessage>(message.Body);
                                await workerManager.Cancel(cancelJobMessage);
                            }
                        }
                        finally
                        {
                            await DeleteMessageAsync(message);
                        }
                    }
                }
            }
            finally
            {
                await listener.DeleteSessionAsync();
            }
        }

        private async Task DeleteMessageAsync(TaskAgentMessage message)
        {
            if (message != null && _sessionId != Guid.Empty)
            {
                //TODO: decide what tokens to use if HostCotnext is already canceled
                var agentServer = HostContext.GetService<IAgentServer>();
                var cancellationToken = HostContext.CancellationToken;
                CancellationTokenSource tokenSource = null;
                if (cancellationToken.IsCancellationRequested)
                {
                    tokenSource = new CancellationTokenSource();
                    cancellationToken = tokenSource.Token;
                }
                await agentServer.DeleteAgentMessageAsync(_poolId, message.MessageId, _sessionId, cancellationToken);
            }
        }
        
        private static void PrintUsage()
        {
            string usage = StringUtil.Loc("ListenerHelp");
            Console.WriteLine(usage);
            Console.WriteLine(StringUtil.Loc("Test", "Hello"));
            Environment.Exit(0);
        }        
    }
}
