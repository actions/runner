using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Handlers;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(JobHookProvider))]
    public interface IJobHookProvider : IRunnerService
    {
        Task RunHook(IExecutionContext executionContext, object data);
    }

    public class JobHookData
    {
        public string Path {get; private set;}
        public ActionRunStage Stage {get; private set;}

        public JobHookData(ActionRunStage stage, string path)
        {
            Path = path;
            Stage = stage;
        }
    }

    public class JobHookProvider : RunnerService, IJobHookProvider
    {
        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
        }

        public async Task RunHook(IExecutionContext executionContext, object data)
        {
            // Get Inputs
            var hookData = data as JobHookData;
            ArgUtil.NotNull(hookData, nameof(JobHookData));

            var displayName = hookData.Stage == ActionRunStage.Pre ? "job started hook" : "job completed hook";
            // Log to users so that they know how this step was injected
            executionContext.Output($"A {displayName} has been configured by the self-hosted runner administrator");

            // Validate script file.
            if (!File.Exists(hookData.Path))
            {
                throw new FileNotFoundException("File doesn't exist");
            }

            executionContext.WriteWebhookPayload();

            // Create the handler data.
            var scriptDirectory = Path.GetDirectoryName(hookData.Path);
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            var prependPath = string.Join(Path.PathSeparator.ToString(), executionContext.Global.PrependPath.Reverse<string>());
            Dictionary<string, string> inputs = new()
            {
                ["path"] = hookData.Path,
                ["shell"] = HostContext.GetDefaultShellForScript(hookData.Path, Trace, prependPath)
            };

            // Create the handler
            var handlerFactory = HostContext.GetService<IHandlerFactory>();
            var handler = handlerFactory.Create(
                            executionContext,
                            action: null,
                            stepHost,
                            new ScriptActionExecutionData(),
                            inputs,
                            environment: new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                            executionContext.Global.Variables,
                            actionDirectory: scriptDirectory,
                            localActionContainerSetupSteps: null);
            handler.PrepareExecution(hookData.Stage);

            // Setup file commands
            var fileCommandManager = HostContext.CreateService<IFileCommandManager>();
            fileCommandManager.InitializeFiles(executionContext, null);

            // Run the step and process the file commands
            try
            {
                await handler.RunAsync(hookData.Stage);
            }
            finally
            {
                fileCommandManager.ProcessFiles(executionContext, executionContext.Global.Container);
            }
        }
    }
}
