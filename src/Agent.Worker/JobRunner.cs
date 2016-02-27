using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public class JobRunner
    {
        private IHostContext _hostContext;
        private readonly TraceSource _trace;
                
        public JobRunner(IHostContext hostContext) {
            _hostContext = hostContext;
            _trace = hostContext.GetTrace("JobRunner");
        }

        //RunAsync takes the same parameters as IWorker.RunAsync and conceptionally is the same thing.
        //Should JobRunner implement IWorker interface?
        public async Task<int> RunAsync(JobRequestMessage message, CancellationToken token)
        {
            _trace.Info("RunAsync");
            ExecutionContext context = new ExecutionContext(_hostContext, Guid.NewGuid());
            
            _trace.Info("Prepare");
            context.Output("Prepare...");
            context.Debug("Preparing...");
            
            _trace.Info("Run");
            context.Output("Run...");
            context.Debug("Running...");
            
            _trace.Info("Finish");
            context.Output("Finish...");
            context.Debug("Finishing...");

            _trace.Info("Job id {0}", message.JobId);
            await Task.Yield();
            return 0;
        }        
    }
}
