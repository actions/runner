using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CLI
{
    public class JobRunner
    {
        public JobRunner(IHostContext hostContext) {
            m_hostContext = hostContext;
            m_trace = hostContext.Trace["JobRunner"];
        }
        
        public void Run()
        {
            ExecutionContext context = new ExecutionContext(m_hostContext);
            m_trace.Verbose("Prepare");
            context.LogInfo("Prepare...");
            context.LogVerbose("Preparing...");
            
            m_trace.Verbose("Run");
            context.LogInfo("Run...");
            context.LogVerbose("Running...");
            
            m_trace.Verbose("Finish");
            context.LogInfo("Finish...");
            context.LogVerbose("Finishing...");
        }
        
        private IHostContext m_hostContext;
        private readonly TraceSource m_trace;
    }
}
