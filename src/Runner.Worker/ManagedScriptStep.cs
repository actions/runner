using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker.Handlers;
using System.Linq;

// This feature is in BETA and its interfaces may change, use with caution
namespace GitHub.Runner.Worker
{

    [ServiceLocator(Default = typeof(ManagedScriptStep))]
    public interface IManagedScriptStep : IRunnerService, IStep
    {
        string ScriptPath { get; set; }
        ActionRunStage Stage { get; set; }
    }

    public class ManagedScriptStep : RunnerService, IManagedScriptStep
    {
        public ManagedScriptStep() { }

        public string ScriptPath { get; set; }
        public string Condition { get; set; }
        public string DisplayName { get; set; }
        public ActionRunStage Stage { get; set; }
        public TemplateToken ContinueOnError => new BooleanToken(null, null, null, false);
        public TemplateToken Timeout => new NumberToken(null, null, null, 0);

        public IExecutionContext ExecutionContext { get; set; }

        public async Task RunAsync()
        {
            // Log to users so that they know how this step was injected
            ExecutionContext.Output($"A '{DisplayName}' has been configured by the Self Hosted Runner Administrator");

            // Validate script file.
            if (!File.Exists(ScriptPath))
            {
                throw new FileNotFoundException("File doesn't exist");
            }

            ExecutionContext.WriteWebhookPayload();

            // Create the handler data.
            var scriptDirectory = Path.GetDirectoryName(ScriptPath);
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            var prependPath = string.Join(Path.PathSeparator.ToString(), ExecutionContext.Global.PrependPath.Reverse<string>());
            Dictionary<string, string> inputs = new()
            {
                ["path"] = ScriptPath,
                ["shell"] = ScriptHandlerHelpers.WhichShell(ScriptPath, Trace, prependPath)
            };

            // Create the handler
            var handlerFactory = HostContext.GetService<IHandlerFactory>();
            var handler = handlerFactory.Create(
                            ExecutionContext,
                            action: null,
                            stepHost,
                            new ScriptActionExecutionData(),
                            inputs,
                            environment: new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                            ExecutionContext.Global.Variables,
                            actionDirectory: scriptDirectory,
                            localActionContainerSetupSteps: null);
            handler.PrepareExecution(Stage);

            // Setup file commands
            var fileCommandManager = HostContext.CreateService<IFileCommandManager>();
            fileCommandManager.InitializeFiles(ExecutionContext, null);

            // Run the step and process the file commands
            try
            {
                await handler.RunAsync(Stage);
            }
            finally
            {
                fileCommandManager.ProcessFiles(ExecutionContext, ExecutionContext.Global.Container);
            }
        }
    }
}