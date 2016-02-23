using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
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
                TraceSource _trace = context.GetTrace("AgentProcess");
                _trace.Info("Info Hello Agent!");

                //
                // TODO (bryanmac): Need VsoAgent.exe compat shim for SCM
                //                  That shim will also provide a compat arg parse 
                //                  and translate / to -- etc...
                //
                CommandLineParser parser = new CommandLineParser(context);
                parser.Parse(args);
                _trace.Info("Arguments parsed");

                Int32 rc = 0;
                try 
                {
                    rc = ExecuteCommand(context, parser).Result;    
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(StringUtil.Format("An error occured.  {0}", e.Message));
                    _trace.Error(e);
                    rc = 1;
                }

                return rc;
                
            }
        }

        private static async Task<Int32> ExecuteCommand(HostContext context, CommandLineParser parser)
        {
            // TODO Unit test to cover this logic
            TraceSource _trace = context.GetTrace("AgentProcess");
            _trace.Info("ExecuteCommand()");

            var configManager = context.GetService<IConfigurationManager>();
            _trace.Info("Created configuration manager");

            // command is not required, if no command it just starts and/or configures if not configured

            // TODO: Invalid config prints usage

            if (parser.Flags.Contains("help"))
            {
                _trace.Info("help");
                PrintUsage();
            }

            // TODO: make commands a list instead of a hashset

            if (parser.IsCommand("unconfigure"))
            {
                _trace.Info("unconfigure");
                // TODO: Unconfiure, remove config and exit
            }

            if (parser.IsCommand("run") && !configManager.IsConfigured())
            {
                _trace.Info("run");
                Console.WriteLine("Agent is not configured");
                PrintUsage();
            }

            // unattend mode will not prompt for args if not supplied.  Instead will error.
            bool isUnattended = parser.Flags.Contains("unattended");

            if (parser.IsCommand("configure"))
            {
                _trace.Info("configure");    
                configManager.Configure(parser.Args, isUnattended);
                Environment.Exit(0);
            }

            if (parser.Flags.Contains("nostart"))
            {
                _trace.Info("No start option, exiting the agent");
                Environment.Exit(0);
            }

            if (parser.IsCommand("run") && !configManager.IsConfigured())
            {
                throw new InvalidOperationException("Cannot run.  Must configure first.");
            }

            _trace.Info("Done evaluating commands");
            configManager.EnsureConfigured();

            //String workerExe = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Worker.exe");
            //Int32 exitCode = ProcessInvoker.RunExe(context, workerExe, "");
            //_trace.Info("Worker.exe Exit: {0}", exitCode); 

            ICredentialProvider cred = configManager.AcquireCredentials(parser.Args, isUnattended);
            return RunAsync(context).Result;
        }

        public static async Task<Int32> RunAsync(IHostContext context)
        {
            /*
            try
            {
                var listener = context.GetService<IMessageListener>();
                if (await listener.CreateSessionAsync())
                {
                    await listener.ListenAsync();
                }

                await listener.DeleteSessionAsync();
            }
            catch (Exception)
            {
                // TODO: Log exception.
                return 1;
            }
            */

            return 0;
        }

        private static void PrintUsage()
        {
            string usage = @"
usage:
Agent.Listener [command(s)] [arguments] [options] 

It is common to just run Agent or Agent.Listener with no arguments for an interactive configuration.
You will be prompted and walked through all options.


Commands:
-----------------------------------------------------------------------------
(none)         Interactively configure and then run the agent.  
               You will be prompted for data.
configure      Configure the agent and exit.
unconfigure    Unconfigure the agent.
run            Runs the agent interactively.  must be configured.


Options:
-----------------------------------------------------------------------------
--unattend     Unattended config.  You will not be prompted.  
               All answers must be supplied on cli.
--nostart      Do not start the agent after interactive configuration.
--auth         Auth type.  Valid options are PAT (Personal Access Token) and 
               ALT (Alternate Credentials)


Options by Auth Type:
-----------------------------------------------------------------------------
PAT
--token        Personal Access Token data.  Best to paste value in.

ALT
--username     alternate username
--password     alternate password
            ";   
            Console.WriteLine(usage);
            Environment.Exit(0);
        }        
    }
}
