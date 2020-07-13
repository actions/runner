using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Schema;
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
        CompositeActionExecutionData Data { get; set; }
    }

    public sealed class CompositeActionOutputHandler : Handler, ICompositeActionOutputHandler
    {
        public CompositeActionExecutionData Data { get; set; }


        public Task RunAsync(ActionRunStage stage)
        {
            // Evaluate the mapped outputs value
            if (Data.Outputs != null)
            {
                // Evaluate the outputs in the steps context to easily retrieve the values
                var actionManifestManager = HostContext.GetService<IActionManifestManager>();

                // Format ExpressionValues to Dictionary<string, PipelineContextData>
                var evaluateContext = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in ExecutionContext.ExpressionValues)
                {
                    evaluateContext[pair.Key] = pair.Value;
                }

                // Get the evluated composite outputs' values mapped to the outputs named
                DictionaryContextData actionOutputs = actionManifestManager.EvaluateCompositeOutputs(ExecutionContext, Data.Outputs, evaluateContext);

                // Set the outputs for the outputs object in the whole composite action
                actionManifestManager.SetAllCompositeOutputs(ExecutionContext.FinalizeContext, actionOutputs);
            }

            return Task.CompletedTask;
        }
    }
}