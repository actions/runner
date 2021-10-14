using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Expressions;
using Pipelines = GitHub.DistributedTask.Pipelines;


namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(MakefileHandler))]
    public interface IMakefileHandler : IHandler
    {
        MakefileExecutionData Data { get; set; }
    }
    public sealed class MakefileHandler : Handler, IMakefileHandler
    {
        public MakefileExecutionData Data { get; set; }

        public async Task RunAsync(ActionRunStage stage)
        {
            // Validate args
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            // Create a script handler for each target
            var handlers = Data.Targets.Select(target =>
            {
                var handler = HostContext.CreateService<IScriptHandler>();

                // IScriptHandler does not need .Action
                // handler.Action = action;
                handler.Data = new ScriptActionExecutionData();
                handler.Environment = Environment;
                handler.RuntimeVariables = RuntimeVariables;
                handler.ExecutionContext = ExecutionContext;
                handler.StepHost = StepHost;
                handler.Inputs = new Dictionary<string, string>
                {
                    ["script"] = $"make {target}"
                };
                handler.ActionDirectory = ActionDirectory;
                handler.LocalActionContainerSetupSteps = LocalActionContainerSetupSteps;

                return handler;
            });

            foreach (var handler in handlers)
            {
                await handler.RunAsync(stage);
            }
        }
    }
}
