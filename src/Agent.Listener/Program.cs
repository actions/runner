using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    public static class Program
    {
        public static Int32 Main(String[] args)
        {
            using(HostContext context = new HostContext("Agent"))
            {
                TraceSource m_trace = context.Trace["AgentProcess"];
                m_trace.Info("Info Hello Agent!");
            
                //String workerExe = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Worker.exe");
                //Int32 exitCode = ProcessInvoker.RunExe(context, workerExe, "");
                //m_trace.Info("Worker.exe Exit: {0}", exitCode); 

                return RunAsync(context, m_trace, args).Result;
            }
        }

        public static async Task<Int32> RunAsync(IHostContext context, TraceSource trace, String[] args)
        {
            try
            {
                var clArgs = new ProgramArguments(context, args);
                if (clArgs.Configure)
                {
                    throw new System.NotImplementedException();
                }

                var listener = context.GetService<IMessageListener>();
                if (await listener.CreateSessionAsync(context))
                {
                    await listener.ListenAsync(context);
                }

                await listener.DeleteSessionAsync(context);
            }
            catch (Exception ex)
            {
                trace.Error(ex);
                return 1;
            }

            return 0;
        }
    }
}
