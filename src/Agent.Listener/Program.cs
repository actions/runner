using Microsoft.VisualStudio.Services.Agent.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    public static class Program
    {
        private static TraceSource s_trace;
        
        public static Int32 Main(String[] args)
        {
            using (HostContext context = new HostContext("Agent"))
            {
                var cancelHandler = new ConsoleCancelEventHandler((sender, e) =>
                {
                    Console.WriteLine("Exiting...");
                    e.Cancel = true;
                    context.CancellationTokenSource.Cancel();
                });
                Console.CancelKeyPress += cancelHandler;
                Int32 rc = 0;
                try 
                {
                    s_trace = context.GetTrace("AgentProcess");
                    s_trace.Info("Info Hello Agent!");

                    //
                    // TODO (bryanmac): Need VsoAgent.exe compat shim for SCM
                    //                  That shim will also provide a compat arg parse 
                    //                  and translate / to -- etc...
                    //
                    CommandLineParser parser = new CommandLineParser(context);
                    parser.Parse(args);
                    s_trace.Info("Arguments parsed");
                    rc = ExecuteCommand(context, parser).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    if (!(e is OperationCanceledException))
                    {
                        Console.Error.WriteLine(StringUtil.Format("An error occured.  {0}", e.Message));
                    }
                    s_trace.Error(e);
                    rc = 1;
                }
                finally
                {
                    Console.CancelKeyPress -= cancelHandler;
                }

                return rc;
                
            }
        }

        private static async Task<Int32> ExecuteCommand(HostContext context, CommandLineParser parser)
        {
            // TODO Unit test to cover this logic
            TraceSource _trace = context.GetTrace("AgentProcess");
            s_trace.Info("ExecuteCommand()");

            var configManager = context.GetService<IConfigurationManager>();
            s_trace.Info("Created configuration manager");

            // command is not required, if no command it just starts and/or configures if not configured

            // TODO: Invalid config prints usage

            if (parser.Flags.Contains("help"))
            {
                s_trace.Info("help");
                PrintUsage();
            }

            // TODO: make commands a list instead of a hashset

            if (parser.IsCommand("unconfigure"))
            {
                s_trace.Info("unconfigure");
                // TODO: Unconfiure, remove config and exit
            }

            if (parser.IsCommand("run") && !configManager.IsConfigured())
            {
                s_trace.Info("run");
                Console.WriteLine("Agent is not configured");
                PrintUsage();
            }

            // unattend mode will not prompt for args if not supplied.  Instead will error.
            bool isUnattended = parser.Flags.Contains("unattended");

            if (parser.IsCommand("configure"))
            {
                s_trace.Info("configure");    
                await configManager.ConfigureAsync(parser.Args, isUnattended);
                return 0;
            }

            if (parser.Flags.Contains("nostart"))
            {
                s_trace.Info("No start option, exiting the agent");
                return 0;
            }

            if (parser.IsCommand("run") && !configManager.IsConfigured())
            {
                throw new InvalidOperationException("Cannot run.  Must configure first.");
            }

            s_trace.Info("Done evaluating commands");
            await configManager.EnsureConfiguredAsync();
            
            return await RunAsync(context);
        }

        public static async Task<Int32> RunAsync(IHostContext context)
        {
            s_trace.Info("RunAsync()");
            
            // Prep the task server with the configured creds
            
            s_trace.Info("Loading Credentials");
            var credMgr = context.GetService<ICredentialManager>();
            VssCredentials creds = credMgr.LoadCredentials();

            s_trace.Info("Loading Settings");
            var cfgMgr = context.GetService<IConfigurationManager>();
            AgentSettings settings = cfgMgr.LoadSettings();
            
            var serverUrl = settings.ServerUrl;
            s_trace.Info("ServerUrl: {0}", serverUrl);
            Uri uri = new Uri(serverUrl);
            VssConnection conn = ApiUtil.CreateConnection(uri, creds);

            var agentServer = context.GetService<IAgentServer>();
            await agentServer.ConnectAsync(conn);

            var agent = context.GetService<IAgent>();
            await agent.RunAsync();            
            return 0;
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
