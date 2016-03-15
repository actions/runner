using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ProcessHandler))]
    public interface IProcessHandler : IHandler
    {
        ProcessHandlerData Data { get; set; }
    }

    public sealed class ProcessHandler : Handler, IProcessHandler
    {
        public ProcessHandlerData Data { get; set; }

        public Task RunAsync()
        {
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.NotNull(TaskDirectory, nameof(TaskDirectory));
            return Task.FromException(new NotImplementedException());
        }
    }
}
