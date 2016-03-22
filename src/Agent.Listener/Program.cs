using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    public static class Program
    {
        private static Tracing s_trace;

        public static int Main(string[] args)
        {
            return MainAsync(args).GetAwaiter().GetResult();
        }


        public async static Task<int> MainAsync(string[] args)
        {
            using (HostContext context = new HostContext("Agent"))
            {
                int rc = 0;
                try
                {
                    s_trace = context.GetTrace("AgentProcess");
                    s_trace.Info($"Version: {Constants.Agent.Version}");
                    s_trace.Info($"Commit: {BuildConstants.Source.CommitHash}");

                    //
                    // TODO (bryanmac): Need VsoAgent.exe compat shim for SCM
                    //                  That shim will also provide a compat arg parse 
                    //                  and translate / to -- etc...
                    //
                    CommandLineParser parser = new CommandLineParser(context);
                    parser.Parse(args);
                    s_trace.Info("Arguments parsed");
                    
                    IAgent agent = context.GetService<IAgent>();
                    using (agent.TokenSource = new CancellationTokenSource())
                    {
                        rc = await agent.ExecuteCommand(parser);
                    }
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

                return rc;
            }
        }
    }
}
