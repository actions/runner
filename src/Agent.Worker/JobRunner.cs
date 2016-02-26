using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public class JobRunner
    {
        public JobRunner(IHostContext hostContext) {
            m_hostContext = hostContext;
            m_trace = hostContext.GetTrace("JobRunner");
        }

        //RunAsync takes the same parameters as IWorker.RunAsync and conceptionally is the same thing.
        //Should JobRunner implement IWorker interface?
        public async Task<int> RunAsync(JobRequestMessage message, CancellationToken token)
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

            m_trace.Info("Job id {0}", message.JobId);
            return 0;
        }        

        private IHostContext m_hostContext;
        private readonly TraceSource m_trace;        
    }
}
