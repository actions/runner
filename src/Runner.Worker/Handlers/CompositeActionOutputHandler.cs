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
        CompositeActionExecutionData Data { get; set; }
    }

    public sealed class CompositeActionOutputHandler : Handler, ICompositeActionOutputHandler
    {
        public CompositeActionExecutionData Data { get; set; }
        public Task RunAsync(ActionRunStage stage)
        {
            // Create "steps" attribute for ExpressionValues for when we evaluate the Outputs.
            // var contextDataWithSteps = ExecutionContext.ExpressionValues.Clone() as DictionaryContextData;
            // contextDataWithSteps["steps"] = new DictionaryContextData();
            // contextDataWithSteps["steps"] = ExecutionContext.StepsContext.GetScope(Data.ParentScopeName);
            // ^We don't need this since StepsRunner sets this already for us. 

            // Trace.Info($"Scope: {StringUtil.ConvertToJson(ExecutionContext.StepsContext.GetScope(ExecutionContext.FinalizeContext.ScopeName))}");

            // By default, ExecutionContext.ExpressionValues["steps"] is null. We need to sync it with the StepsContext.
            // ExecutionContext.ExpressionValues["steps"] = new DictionaryContextData();
            // ExecutionContext.ExpressionValues["steps"] = ExecutionContext.StepsContext.GetScope(ExecutionContext.FinalizeContext.ScopeName);

            Trace.Info($"Steps: {StringUtil.ConvertToJson(ExecutionContext.ExpressionValues["steps"])}");

            // Evaluate the mapped outputs value
            if (Data.Outputs != null)
            {
                // Evaluate in the steps context
                var evaluator = ExecutionContext.ToPipelineTemplateEvaluator();

                // TODO: Check if Errors thrown are outputted in a good user experience way
                // ex: fromJson('not json"'
                DictionaryContextData actionOutputs = evaluator.EvaluateStepScopeOutputs(Data.Outputs, ExecutionContext.ExpressionValues, ExecutionContext.ExpressionFunctions);
                foreach (var pair in actionOutputs)
                {
                    var outputsName = pair.Key;
                    var outputsValue = pair.Value as StringContextData;
                    // Set output in the whole composite scope. 
                    if (!String.IsNullOrEmpty(outputsName) && !String.IsNullOrEmpty(outputsValue))
                    {
                        ExecutionContext.FinalizeContext.SetOutput(outputsName, outputsValue, out string test);
                    }
                }
            }

            // Create scope for Scopes to avoid null scope situation in the CompleteStep()
            // if (!ExecutionContext.Scopes.ContainsKey(Data.ParentScopeName))
            // {
            //     ExecutionContext.Scopes[Data.ParentScopeName] = new Pipelines.ContextScope()
            //     {
            //         Name = Data.ParentScopeName
            //     };
            // }

            return Task.CompletedTask;
        }
    }
}