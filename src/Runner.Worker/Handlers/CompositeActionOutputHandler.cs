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

            var contextDataWithSteps = Data.ParentExecutionContext.ExpressionValues.Clone() as DictionaryContextData;
            contextDataWithSteps["steps"] = new DictionaryContextData();
            // var testContext = contextDataWithSteps["steps"] as DictionaryContextData;
            // testContext[Data.ParentScopeName] = new DictionaryContextData();
            // var parentContext = testContext[Data.ParentScopeName] as DictionaryContextData;

            // TODO FOR CLEANUP:
            // Stuff built in to use:
            //          StepsContext.GetScope(Step Name)
            // foreach (var pair in Data.ScopeAndContextNames)
            // {
            //     Trace.Info("in for");
            //     // Clone to prevent the System.InvalidOperationException: Collection was modified error
            //     DictionaryContextData currentOutputs = ExecutionContext.StepsContext.GetOutput(pair.Key, pair.Value).Clone() as DictionaryContextData;
            //     Trace.Info($"Scope {pair.Key}'s outputs: {StringUtil.ConvertToJson(currentOutputs)}");
            //     Trace.Info($"Context Name: {pair.Value}");
            //     // foreach (var outputs in currentOutputs) {
            //     //     StringContextData outputsValue = outputs.Value as StringContextData;
            //     //     // finalOutputs[outputs.Key] = outputsValue.ToString();
            //     //     Data.ParentExecutionContext.StepsContext.SetOutput(pair.Key, pair.Value, outputs.Key, outputsValue, out string test);
            //     //     Trace.Info($"Composite Action Output Handler Reference: {test}");
            //     //     Trace.Info($"ParentExecutionContext StepsContext Test Scope {StringUtil.ConvertToJson(Data.ParentExecutionContext.StepsContext.GetScope(pair.Key))}");
            //     // }
            //     testContext[pair.Value] = new DictionaryContextData();
            //     var test2 = testContext[pair.Value] as DictionaryContextData;
            //     test2["outputs"] = new DictionaryContextData();
            //     foreach (var outputs in currentOutputs) {
            //         var test3 = test2["outputs"] as DictionaryContextData;
            //         test3[outputs.Key] = outputs.Value;
            //     }
            //     // parentContext[pair.Value] = currentOutputs;
            // }
            contextDataWithSteps["steps"] = ExecutionContext.StepsContext.GetScope(Data.ParentScopeName);

            Trace.Info($"Parent Step Context: {StringUtil.ConvertToJson(contextDataWithSteps["steps"])}");

            // Look at Execution Context and see if we are in the right scope?
            // Afterwards, how do we set the steps.composite1[outputs] = ....
            var outputsAction = new Dictionary<String, String>();
            if (Data.Outputs != null) {
                var evaluator = Data.ParentExecutionContext.ToPipelineTemplateEvaluator();

                // Evaluate outputs so that we can store it later in the "outputs" object in the composite action step
                DictionaryContextData actionOutputs = evaluator.EvaluateStepScopeOutputs(Data.Outputs, contextDataWithSteps, Data.ParentExecutionContext.ExpressionFunctions);
                foreach (var pair in actionOutputs) {
                    Trace.Info($"Composite Action Output Handler. Original Output Key: {pair.Key}");


                    var outputsName = pair.Key;
                    
                    var outputsValue = pair.Value as StringContextData;
                    Trace.Info($"Composite Action Output Handler. Original Output Value: {StringUtil.ConvertToJson(outputsValue)}");

                    // You can 
                    Data.ParentExecutionContext.SetOutput(outputsName, outputsValue, out string test);

                    outputsAction.Add(outputsName, outputsValue.ToString());

                    // Set outputs in parent scope for steps
                    // ex: get the step name aka composite1 in our example
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