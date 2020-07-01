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
            Trace.Info($"Original Scope + Contexts: {StringUtil.ConvertToJson(Data.ScopeAndContextNames)}");

            // TODO: Figure out why ScopeName and ContextName is null after aadding composite action steps that don't have an ID.
            Trace.Info($"Parent ExecutionContext.ScopeName {ExecutionContext.ScopeName}");
            Trace.Info($"Parent ExecutionContext.ContextName {ExecutionContext.ContextName}");

            // TODO: Add the parent info to the handler _ParentScope + ContextName, etc. 
            // ^ don't rely on executioncontext's parent stuff.
            // Pass parent execution context down to handler data attribute.


            // Test code to see if outputs are processed
            // Pull coressponding outputs and place them in the Whole Composite Action Job Outputs Object
            // int limit = Exe
            // Dictionary<string, string> finalOutputs = new Dictionary<string, string>();
            foreach (var pair in Data.ScopeAndContextNames)
            {
                Trace.Info("in for");
                // Clone to prevent the System.InvalidOperationException: Collection was modified error
                DictionaryContextData currentOutputs = ExecutionContext.StepsContext.GetOutput(pair.Key, pair.Value).Clone() as DictionaryContextData;
                Trace.Info($"Scope {pair.Key}'s outputs: {StringUtil.ConvertToJson(currentOutputs)}");
                foreach (var outputs in currentOutputs) {
                    StringContextData outputsValue = outputs.Value as StringContextData;
                    // finalOutputs[outputs.Key] = outputsValue.ToString();
                    Data.ParentExecutionContext.StepsContext.SetOutput(pair.Key, pair.Value, outputs.Key, outputsValue, out string test);
                    Trace.Info($"Composite Action Output Handler Reference: {test}");
                }
            }

            // Trace.Info($"Final outputs: {StringUtil.ConvertToJson(finalOutputs)}");
            // Trace.Info($"Current StepsContext: {StringUtil.ConvertToJson(ExecutionContext.StepsContext)}");
            // Trace.Info($"Parent StepsContext: {StringUtil.ConvertToJson(Data.ParentExecutionContext.StepsContext)}");

            Trace.Info($"Output Handler Scopes {StringUtil.ConvertToJson(ExecutionContext.StepsContext.GetScope(ExecutionContext.ContextName))}");

            // Prepare outputs to be sent over to the Composite Action Outputs Handler.
            // TODO: Pass this stuff to the Output Handler! The values will then be evaluated correctly!
            // TODO: 7/1/20 left off => look at how ScriptHandler does it
            // OHHHHHHHHHM, WE NEED TO SET IT UP ABOVE!!!!!!!
            // Figure out why it's still null when we search it.

            // SEE:
            //             [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]Evaluating: steps.random-number-generator.outputs.random-id
            // 4459 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]Evaluating Index:
            //    1 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]..Evaluating Index:
            //    2 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]....Evaluating Index:
            //    3 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]......Evaluating steps:
            //    4 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]......=> null
            //    5 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]....=> null
            //    6 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]..=> null
            //    7 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]=> null
            //    8 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]Result: null
            //    9 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]Evaluating: steps.food.outputs.test
            //   10 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]Evaluating Index:
            //   11 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]..Evaluating Index:
            //   12 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]....Evaluating Index:
            //   13 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]......Evaluating steps:
            //   14 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]......=> null
            //   15 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]....=> null
            //   16 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]..=> null
            //   17 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]=> null
            //   18 [2020-07-01 20:58:23Z VERB JobServerQueue] Enqueue web console line queue: ##[debug]Result: null
            var outputsAction = new Dictionary<String, String>();
            if (Data.Outputs != null) {
                var evaluator = Data.ParentExecutionContext.ToPipelineTemplateEvaluator();
                DictionaryContextData actionOutputs = evaluator.EvaluateStepScopeOutputs(Data.Outputs, Data.ParentExecutionContext as DictionaryContextData, Data.ParentExecutionContext.ExpressionFunctions);
                foreach (var pair in actionOutputs) {
                    Trace.Info($"Composite Action Output Handler. Original Output Key: {pair.Key}");

                    // TODO: Figure out how to support mapping to specific ids
                    // outputs:
                    //  random-number: ${{ steps.random-number-generator.outputs.random-id }}
                    //  food: ${{ steps.food.outputs.test }}

                    // Just convert it to a string for now?
                    // Changed action.yaml to reflect this. 
                    // We have to make sure that in the OutputHandler, it has access to all the context variables (github, etc. )
                    // Yes, but we still have to consider other context variables that are not steps....outputs
                    // ex: steps.random-number-generator.outputs.random-id
                    // And we can easily just compare if the strings are equal to each other in the output handler?


                    var outputsName = pair.Key;
                    var outputsValue = pair.Value;
                    Trace.Info($"Composite Action Output Handler. Original Output Value: {StringUtil.ConvertToJson(outputsValue)}");
                    outputsAction.Add(outputsName, outputsValue.ToString());
                }
            }

            Trace.Info($"Outputs Action Output Handler Repre: {StringUtil.ConvertToJson(outputsAction)}");

            // Add the outputs from the composite steps to the corresponding outputs object. 
            // foreach (var pair in finalOutputs)
            // {
            //     Data.ParentExecutionContext.SetOutput(pair.Key, pair.Value, out string reference);
            //     Trace.Info($"Reference: {reference}");
            // }


            // Remove/Pop each of the composite steps scop from the StepsContext to save memory and
            // also to prevent any shenanigans from happening (ex: other actions accessing step level scope stuff)
            Trace.Info($"Parent ExecutionContext.ScopeName {ExecutionContext.ScopeName}");
            Trace.Info($"Parent ExecutionContext.ContextName {ExecutionContext.ContextName}");


            var parentScopeName = !String.IsNullOrEmpty(ExecutionContext.ScopeName) ? ExecutionContext.ScopeName : ExecutionContext.ContextName;
            Trace.Info($"Parent Scope Name {parentScopeName}");

            // TODO: Figure out if we need to include workflow step ID for parentScopeName
            if (!ExecutionContext.Scopes.ContainsKey(parentScopeName)) {
                ExecutionContext.Scopes[parentScopeName] = new Pipelines.ContextScope() {
                    Name = parentScopeName
                };
            }
            
            return Task.CompletedTask;
        }
    }
}