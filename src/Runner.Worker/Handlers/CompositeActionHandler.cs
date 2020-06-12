using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using System.Collections.Generic;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(CompositeActionHandler))]
    public interface ICompositeActionHandler : IHandler
    {
        CompositeActionExecutionData Data { get; set; }
    }

    // TODO: IMPLEMENT LOGIC FOR HANDLER CODE
    public sealed class CompositeActionHandler : Handler, ICompositeActionHandler
    {
        public CompositeActionExecutionData Data { get; set; }

        public override void PrintActionDetails(ActionRunStage stage)
        {

        }

        public async Task RunAsync(ActionRunStage stage) 
        {
            // DELETE LATER
            // await Task.Yield();


            // Basically, the only difference from ScriptHandler.cs is that "contents" is not just each step under "run: "
            // It might make more sense to:
            // 1) Abstract the core functionality of the ScriptHandler.cs that we need for BOTH CompositeActionHandler.cs and ScriptHandler.cs
            // 2) Call those functions in both handlers
            // * There is already a file called ScriptHandlerHelpers.cs that might be a good location to add more functions. 

            // Copied from ScriptHandler.cs
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));

            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            // Resolve steps
            var target = Data.Steps;

            // For now, just assume it is 1 Run step
            // We will adapt this in the future. 
            // Copied from ActionRunner.cs RunAsync() function => Maybe we don't need a handler and need to avoid this preplicatoin in the future?
            var runStepInputs= target[0].Inputs;
            var templateEvaluator = ExecutionContext.ToPipelineTemplateEvaluator();
            var inputs = templateEvaluator.EvaluateStepInputs(runStepInputs, ExecutionContext.ExpressionValues, ExecutionContext.ExpressionFunctions);
            var taskManager = HostContext.GetService<IActionManager>();

            var userInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var runValue = "";
            foreach (KeyValuePair<string, string> input in inputs)
            {
                userInputs.Add(input.Key);
                userInputs.Add(input.Value);
                if (input.Key.Equals("run"))
                {
                    runValue = input.Value;
                }

                // string message = "";
                // if (definition.Data?.Deprecated?.TryGetValue(input.Key, out message) == true)
                // {
                //     ExecutionContext.Warning(String.Format("Input '{0}' has been deprecated with message: {1}", input.Key, message));
                // }

                // Get the run bash value that we want to run
                // In the future, we would apply and validate the template => maybe using the manifest manager to recursively load the json schema.
            }

            // Let's think about validating inputs later
            // Validate inputs only for actions with action.yml
            // var unexpectedInputs = new List<string>();
            // foreach (var input in userInputs)
            // {
            //     if (!validInputs.Contains(input))
            //     {
            //         unexpectedInputs.Add(input);
            //     }
            // }

            // if (unexpectedInputs.Count > 0)
            // {
            //     ExecutionContext.Warning($"Unexpected input(s) '{string.Join("', '", unexpectedInputs)}', valid inputs are ['{string.Join("', '", validInputs)}']");
            // }


            //TODO:
            // 6/11/20 EOD thoughts
            // => What functions do I need to use from ScriptHandlerHelpers.cs?
            // Do I need to Reverse the string for prepending the path??
            //      ^ why or why not?
            // How do I process the Inputs?
            //      => It's a TemplateToken
            // How do I incorporate Async? we call the StepHost for Async when you call ExecuteAsync()

            // Detect operating system for fileName + arguments

            
            
            // Resolve the working directory.
            string workingDirectory = ExecutionContext.GetGitHubContext("workspace");
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
            }

            // A lot of copying from ScriptHandler.cs => Should we just invoke the ScriptHandler in the future and just modify it
            // to meet our needs for a composite action?

            string shell = null;
            if (!Inputs.TryGetValue("shell", out shell) || string.IsNullOrEmpty(shell))
            {
                // TODO: figure out how defaults interact with template later
                // for now, we won't check job.defaults if we are inside a template.
                if (string.IsNullOrEmpty(ExecutionContext.ScopeName) && ExecutionContext.JobDefaults.TryGetValue("run", out var runDefaults))
                {
                    if (runDefaults.TryGetValue("shell", out shell))
                    {
                        ExecutionContext.Debug("Overwrite 'shell' base on job defaults.");
                    }
                }
            }

            var isContainerStepHost = StepHost is ContainerStepHost;

            string prependPath = string.Join(Path.PathSeparator.ToString(), ExecutionContext.PrependPath.Reverse<string>());
            string commandPath, argFormat, shellCommand;

            if (string.IsNullOrEmpty(shell))
            {
#if OS_WINDOWS
                shellCommand = "pwsh";
                commandPath = WhichUtil.Which(shellCommand, require: false, Trace, prependPath);
                if (string.IsNullOrEmpty(commandPath))
                {
                    shellCommand = "powershell";
                    Trace.Info($"Defaulting to {shellCommand}");
                    commandPath = WhichUtil.Which(shellCommand, require: true, Trace, prependPath);
                }
                ArgUtil.NotNullOrEmpty(commandPath, "Default Shell");
#else
                shellCommand = "sh";
                commandPath = WhichUtil.Which("bash", false, Trace, prependPath) ?? WhichUtil.Which("sh", true, Trace, prependPath);
#endif
                argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
            }
            else
            {
                var parsed = ScriptHandlerHelpers.ParseShellOptionString(shell);
                shellCommand = parsed.shellCommand;
                // For non-ContainerStepHost, the command must be located on the host by Which
                commandPath = WhichUtil.Which(parsed.shellCommand, !isContainerStepHost, Trace, prependPath);
                argFormat = $"{parsed.shellArgs}".TrimStart();
                if (string.IsNullOrEmpty(argFormat))
                {
                    argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
                }
            }

            // No arg format was given, shell must be a built-in
            if (string.IsNullOrEmpty(argFormat) || !argFormat.Contains("{0}"))
            {
                throw new ArgumentException("Invalid shell option. Shell must be a valid built-in (bash, sh, cmd, powershell, pwsh) or a format string containing '{0}'");
            }




            // Use StepHost.ExecuteAsync() to execute code
            // What does StepHost.ExecuteAsync do?
            //  basically just adds configuration stuff to the code for it to run and then invoke
            //  processInvoker.ExecuteAsync() which basically runs the process with the necessary configurations for the process.
            using (var stdoutManager = new OutputManager(ExecutionContext, ActionCommandManager))
            using (var stderrManager = new OutputManager(ExecutionContext, ActionCommandManager))
            {
                StepHost.OutputDataReceived += stdoutManager.OnDataReceived;
                StepHost.ErrorDataReceived += stderrManager.OnDataReceived;

                // Execute
                int exitCode = await StepHost.ExecuteAsync(workingDirectory: StepHost.ResolvePathForStepHost(workingDirectory),
                                            fileName: fileName,
                                            arguments: arguments,
                                            environment: Environment,
                                            requireExitCodeZero: false,
                                            outputEncoding: null,
                                            killProcessOnCancel: false,
                                            inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                            cancellationToken: ExecutionContext.CancellationToken);

                // Error
                if (exitCode != 0)
                {
                    ExecutionContext.Error($"Process completed with exit code {exitCode}.");
                    ExecutionContext.Result = TaskResult.Failed;
                }
            }

        }
    }
}