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
            // Create "steps" attribute for ExpressionValues for when we evaluate the Outputs.
            var contextDataWithSteps = Data.ParentExecutionContext.ExpressionValues.Clone() as DictionaryContextData;
            contextDataWithSteps["steps"] = new DictionaryContextData();
            contextDataWithSteps["steps"] = ExecutionContext.StepsContext.GetScope(Data.ParentScopeName);

            // Evaluate the mapped outputs value
            if (Data.Outputs != null) {
                var evaluator = Data.ParentExecutionContext.ToPipelineTemplateEvaluator();
                DictionaryContextData actionOutputs = evaluator.EvaluateStepScopeOutputs(Data.Outputs, contextDataWithSteps, Data.ParentExecutionContext.ExpressionFunctions);
                foreach (var pair in actionOutputs) {
                    var outputsName = pair.Key;
                    var outputsValue = pair.Value as StringContextData;
                    Data.ParentExecutionContext.SetOutput(outputsName, outputsValue, out string test);
                }
            }

            // Create scope for Scopes to avoid null scope situation in the CompleteStep()
            if (!ExecutionContext.Scopes.ContainsKey(Data.ParentScopeName)) {
                ExecutionContext.Scopes[Data.ParentScopeName] = new Pipelines.ContextScope() {
                    Name = Data.ParentScopeName
                };
            }
            
            return Task.CompletedTask;
        }
    }
}