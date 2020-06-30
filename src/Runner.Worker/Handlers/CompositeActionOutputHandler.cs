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

            // TODO: Add the parent info to the handler _ParentScope + ContextName, etc. 
            // ^ don't rely on executioncontext's parent stuff.
            // Pass parent execution context down to handler data attribute.

            // Pull coressponding outputs and place them in the Whole Composite Action Job Outputs Object
            // int limit = Exe

            // Add the outputs from the composite steps to the corresponding outputs object. p

            // Remove/Pop each of the composite steps scop from the StepsContext
            
            return null;
        }
    }
}