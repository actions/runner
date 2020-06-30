using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(CompositeActionOutputHandler))]
    public interface ICompositeActionOutputHandler : IHandler
    {
        CompositeActionOutputExecutionData Data { get; set; }
    }

    public sealed class CompositeActionOutputHandler: Handler, ICompositeActionOutputHandler
    {
        public CompositeActionOutputExecutionData Data { get; set; }
        public Task RunAsync(ActionRunStage stage)
        {
            Trace.Info("In Composite Action Output Handler");
            return null;
        }
    }
}