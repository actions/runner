using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker.Handlers;

namespace GitHub.Runner.Worker.Container
{
    public class DockerHookCommandManager : DockerCommandManager
    {
        public override async Task<int> DockerStart(IExecutionContext context, string containerId)
        {
            // check for env var
            // execute script
            
            // Create the handler data.
            var path = GetDockerHook(nameof(DockerStart));
            var scriptDirectory = Path.GetDirectoryName(path);
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            var prependPath = string.Join(Path.PathSeparator.ToString(), context.Global.PrependPath.Reverse<string>());
            Dictionary<string, string> inputs = new()
            {
                ["script"] = $"CONT_ID={containerId} " + "/usr/bin/bash" + " " + path,
                // /bin/bash
                ["shell"] = ScriptHandlerHelpers.GetDefaultShellForScript(path, Trace, prependPath)
            };

            var handlerFactory = HostContext.GetService<IHandlerFactory>();
            var handler = handlerFactory.Create(
                            context,
                            action: new ScriptReference(),
                            stepHost,
                            new ScriptActionExecutionData(),
                            inputs,
                            environment: new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                            context.Global.Variables,
                            actionDirectory: scriptDirectory,
                            localActionContainerSetupSteps: null);
            handler.PrepareExecution(ActionRunStage.Main); // TODO: find out stage

            await handler.RunAsync(ActionRunStage.Main);

            return ((int?) handler.ExecutionContext.CommandResult) ?? 0;
        }

        private string GetDockerHook(string commandName) 
        {
            commandName = string.Format("{0}.sh", commandName.ToLower());
            return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.DockerHooks), commandName);
        }
    }
}
