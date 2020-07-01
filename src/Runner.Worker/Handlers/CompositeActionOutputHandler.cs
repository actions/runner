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

            // TODO: Add the parent info to the handler _ParentScope + ContextName, etc. 
            // ^ don't rely on executioncontext's parent stuff.
            // Pass parent execution context down to handler data attribute.


            // Pull coressponding outputs and place them in the Whole Composite Action Job Outputs Object
            // int limit = Exe
            Dictionary<string, string> finalOutputs = new Dictionary<string, string>();

            // The ScopeNames are in the same order as how the Steps Were Processed
            // So the scopeNames will have the most recent value which is what we want!
            foreach (var pair in Data.ScopeAndContextNames)
            {
                DictionaryContextData currentOutputs = ExecutionContext.StepsContext.GetOutput(pair.Key, pair.Value);
                Trace.Info($"Scope {pair.Key}'s outputs: {currentOutputs}");
                foreach (var outputs in currentOutputs) {
                    StringContextData outputsValue = outputs.Value as StringContextData;
                    finalOutputs[outputs.Key] = outputsValue.ToString();
                }
            }

            Trace.Info($"Final outputs: {StringUtil.ConvertToJson(finalOutputs)}");

            // Add the outputs from the composite steps to the corresponding outputs object. 


            // Remove/Pop each of the composite steps scop from the StepsContext to save memory and
            // also to prevent any shenanigans from happening (ex: other actions accessing step level scope stuff)

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