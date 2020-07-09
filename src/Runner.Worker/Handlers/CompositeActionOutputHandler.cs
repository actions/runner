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
            // Evaluate the mapped outputs value
            if (Data.Outputs != null)
            {
                // Evaluate the outputs in the steps context to easily retrieve the values
                var evaluator = ExecutionContext.ToPipelineTemplateEvaluator();
                DictionaryContextData actionOutputs = evaluator.EvaluateCompositeOutputs(Data.Outputs, ExecutionContext.ExpressionValues, ExecutionContext.ExpressionFunctions);

                // Each pair is structured like this
                // We ignore "description" for now
                // "key": "output_id",
                // {
                //     "value": {
                //         "t": 2,
                //         "d": [
                //         {
                //             "k": "description",
                //             "v": "string (can be null)"
                //         },
                //         {
                //             "k": "value",
                //             "v": "string/expression"
                //         }
                //         ]
                //     }
                // }
                foreach (var pair in actionOutputs)
                {
                    var outputsName = pair.Key;
                    var outputsAttributes = pair.Value as DictionaryContextData;
                    outputsAttributes.TryGetValue("value", out var val);
                    var outputsValue = val as StringContextData;

                    // Set output in the whole composite scope. 
                    if (!String.IsNullOrEmpty(outputsName) && !String.IsNullOrEmpty(outputsValue))
                    {
                        ExecutionContext.FinalizeContext.SetOutput(outputsName, outputsValue, out string test);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}