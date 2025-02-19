using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Container.ContainerHooks;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(GoActionHandler))]
    public interface IGoActionHandler : IHandler
    {
        GoActionExecutionData Data { get; set; }
    }

    public sealed class GoActionHandler : Handler, IGoActionHandler
    {
        public GoActionExecutionData Data { get; set; }

        public async Task RunAsync(ActionRunStage stage)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.Directory(ActionDirectory, nameof(ActionDirectory));

            // Update the env dictionary.
            AddInputsToEnvironment();
            AddPrependPathToEnvironment();

            // expose context to environment
            foreach (var context in ExecutionContext.ExpressionValues)
            {
                if (context.Value is IEnvironmentContextData runtimeContext && runtimeContext != null)
                {
                    foreach (var env in runtimeContext.GetRuntimeEnvironmentVariables())
                    {
                        Environment[env.Key] = env.Value;
                    }
                }
            }

            // Add Actions Runtime server info
            var systemConnection = ExecutionContext.Global.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            Environment["ACTIONS_RUNTIME_URL"] = systemConnection.Url.AbsoluteUri;
            Environment["ACTIONS_RUNTIME_TOKEN"] = systemConnection.Authorization.Parameters[EndpointAuthorizationParameters.AccessToken];
            if (systemConnection.Data.TryGetValue("CacheServerUrl", out var cacheUrl) && !string.IsNullOrEmpty(cacheUrl))
            {
                Environment["ACTIONS_CACHE_URL"] = cacheUrl;
            }
            if (systemConnection.Data.TryGetValue("PipelinesServiceUrl", out var pipelinesServiceUrl) && !string.IsNullOrEmpty(pipelinesServiceUrl))
            {
                Environment["ACTIONS_RUNTIME_URL"] = pipelinesServiceUrl;
            }
            if (systemConnection.Data.TryGetValue("GenerateIdTokenUrl", out var generateIdTokenUrl) && !string.IsNullOrEmpty(generateIdTokenUrl))
            {
                Environment["ACTIONS_ID_TOKEN_REQUEST_URL"] = generateIdTokenUrl;
                Environment["ACTIONS_ID_TOKEN_REQUEST_TOKEN"] = systemConnection.Authorization.Parameters[EndpointAuthorizationParameters.AccessToken];
            }
            if (systemConnection.Data.TryGetValue("ResultsServiceUrl", out var resultsUrl) && !string.IsNullOrEmpty(resultsUrl))
            {
                Environment["ACTIONS_RESULTS_URL"] = resultsUrl;
            }

            if (ExecutionContext.Global.Variables.GetBoolean("actions_uses_cache_service_v2") ?? false)
            {
                Environment["ACTIONS_CACHE_SERVICE_V2"] = bool.TrueString;
            }

            // Resolve the target script.
            string target = null;
            if (stage == ActionRunStage.Main)
            {
                target = Data.Main;
            }
            else if (stage == ActionRunStage.Pre)
            {
                target = Data.Pre;
            }
            else if (stage == ActionRunStage.Post)
            {
                target = Data.Post;
            }

            // Set extra telemetry base on the current context.
            if (stage == ActionRunStage.Main)
            {
                ExecutionContext.StepTelemetry.HasPreStep = Data.HasPre;
                ExecutionContext.StepTelemetry.HasPostStep = Data.HasPost;
            }

            ArgUtil.NotNullOrEmpty(target, nameof(target));
            var targetBaseName = target;
            target = Path.Combine(ActionDirectory, target + ".out");

            // Resolve the working directory.
            string workingDirectory = ExecutionContext.GetGitHubContext("workspace");
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
            }

            // It appears that node.exe outputs UTF8 when not in TTY mode.
            Encoding outputEncoding = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? Encoding.UTF8 : null;

            using (var stdoutManager = new OutputManager(ExecutionContext, ActionCommandManager))
            using (var stderrManager = new OutputManager(ExecutionContext, ActionCommandManager))
            {
                StepHost.OutputDataReceived += stdoutManager.OnDataReceived;
                StepHost.ErrorDataReceived += stderrManager.OnDataReceived;

                await StepHost.ExecuteAsync(ExecutionContext,
                    workingDirectory: StepHost.ResolvePathForStepHost(ExecutionContext, ActionDirectory),
                    fileName: StepHost.ResolvePathForStepHost(ExecutionContext, "go"),
                    arguments: $"build -o {targetBaseName}.out {targetBaseName}",
                    environment: Environment,
                    requireExitCodeZero: false,
                    outputEncoding: outputEncoding,
                    killProcessOnCancel: false,
                    inheritConsoleHandler: !ExecutionContext.Global.Variables.Retain_Default_Encoding,
                    standardInInput: null,
                    cancellationToken: ExecutionContext.CancellationToken);

                ArgUtil.File(target, nameof(target));

                // Execute the process. Exit code 0 should always be returned.
                // A non-zero exit code indicates infrastructural failure.
                // Task failure should be communicated over STDOUT using ## commands.
                Task<int> step = StepHost.ExecuteAsync(ExecutionContext,
                                                workingDirectory: StepHost.ResolvePathForStepHost(ExecutionContext, workingDirectory),
                                                fileName: StepHost.ResolvePathForStepHost(ExecutionContext, target),
                                                arguments: string.Empty,
                                                environment: Environment,
                                                requireExitCodeZero: false,
                                                outputEncoding: outputEncoding,
                                                killProcessOnCancel: false,
                                                inheritConsoleHandler: !ExecutionContext.Global.Variables.Retain_Default_Encoding,
                                                standardInInput: null,
                                                cancellationToken: ExecutionContext.CancellationToken);

                // Wait for either the node exit or force finish through ##vso command
                await System.Threading.Tasks.Task.WhenAny(step, ExecutionContext.ForceCompleted);

                if (ExecutionContext.ForceCompleted.IsCompleted)
                {
                    ExecutionContext.Debug("The task was marked as \"done\", but the process has not closed after 5 seconds. Treating the task as complete.");
                }
                else
                {
                    var exitCode = await step;
                    ExecutionContext.Debug($"Go Action run completed with exit code {exitCode}");
                    if (exitCode != 0)
                    {
                        ExecutionContext.Result = TaskResult.Failed;
                    }
                }
            }
        }
    }
}
