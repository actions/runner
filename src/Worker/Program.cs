using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CLI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            HostContext hc = new HostContext("Worker");
            Console.WriteLine("Hello Worker!");
            
#if OS_WINDOWS
            Console.WriteLine("Hello Windows");
#endif
            
#if OS_OSX
            Console.WriteLine("Hello OSX");
#endif

#if OS_LINUX
            Console.WriteLine("Hello Linux");
#endif

            TraceSource m_trace = hc.Trace["WorkerProcess"];
            m_trace.Info("Info Hello Worker!");
            m_trace.Warning("Warning Hello Worker!");
            m_trace.Error("Error Hello Worker!");
            m_trace.Verbose("Verbos Hello Worker!");
            
            JobRunner jobRunner = new JobRunner(hc);
            jobRunner.Run();
            
            hc.Dispose();
        }
    }
}
