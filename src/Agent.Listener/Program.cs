using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.Agent;
using System.Reflection;
using System.IO;

using Microsoft.VisualStudio.Services.Agent.Configuration;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    public static class Program
    {
        public static Int32 Main(String[] args)
        {
            using (HostContext context = new HostContext("Agent"))
            {
                TraceSource m_trace = context.Trace["AgentProcess"];
                m_trace.Info("Info Hello Agent!");

                CommandLineParser parser = new CommandLineParser(context);

                m_trace.Info("Prepare command line arguments");
                parser.Parse(args);

                return ExecuteCommand(context, parser).Result;
            }
        }

        private static async Task<Int32> ExecuteCommand(HostContext context, CommandLineParser parser)
        {
            // TODO Unit test to cover this logic
            TraceSource m_trace = context.Trace["AgentProcess"];
            var configManager = context.GetService<IConfigurationManager>();

            if (parser.Commands.Contains("help") || !parser.HasValidCommand())
            {
                PrintUsage();
            }

            if (parser.Commands.Contains("unconfigure"))
            {
                // TODO: Unconfiure, remove config and exit
            }

            if (parser.Commands.Contains("run") && !configManager.IsConfigured())
            {
                Console.WriteLine("Agent is not configured");
                PrintUsage();
            }

            if (parser.Commands.Contains("configure"))
            {
                Boolean isUnattended = parser.Flags.Contains("unattended");

                if (!configManager.Configure(context, parser.Args, isUnattended))
                {
                    m_trace.Error("Agent configuration failure");
                    PrintUsage();
                }
            }

            if (parser.Flags.Contains("nostart"))
            {
                m_trace.Info("No start option mentioned, exiting the agent");
                Environment.Exit(0);
            }

            if (parser.Flags.Contains("run"))
            {
                if (!configManager.EnsureConfigured(context))
                {
                    Console.WriteLine("Can't not run agent, required configuration is missing");
                    PrintUsage();
                }
            }

            var agentConfiguration = configManager.GetConfiguration();
            PrintConfiguration(m_trace, agentConfiguration);

            //String workerExe = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Worker.exe");
            //Int32 exitCode = ProcessInvoker.RunExe(context, workerExe, "");
            //m_trace.Info("Worker.exe Exit: {0}", exitCode); 

            return RunAsync(context, agentConfiguration).Result;
        }

        private static void PrintConfiguration(TraceSource m_trace, AgentConfiguration configuration)
        {
            m_trace.Info("Server URL: {0}", configuration.Setting.ServerUrl);
            m_trace.Info("Pool Name: {0}", configuration.Setting.PoolName);
            m_trace.Info("Agent Name: {0}", configuration.Setting.AgentName);
            m_trace.Info("Work Folder: {0}", configuration.Setting.WorkFolder);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("Agent.Listener [configure/unconfigure/run/help] [--run] [--UnAttended] [--UserName <UserName>] [--Password <Password>] [--AuthType <Pat/Basic>] [--AgentName <AgentName>] [--PoolName <PoolName>]");
            Environment.Exit(0);
        }

        public static async Task<Int32> RunAsync(IHostContext context, AgentConfiguration configuration)
        {
            try
            {
                var listener = context.GetService<IMessageListener>();
                if (await listener.CreateSessionAsync(context))
                {
                    await listener.ListenAsync(context);
                }

                await listener.DeleteSessionAsync(context);
            }
            catch (Exception)
            {
                // TODO: Log exception.
                return 1;
            }

            return 0;
        }
    }
}
