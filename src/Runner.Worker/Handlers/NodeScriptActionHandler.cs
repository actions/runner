using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(NodeScriptActionHandler))]
    public interface INodeScriptActionHandler : IHandler
    {
        NodeJSActionExecutionData Data { get; set; }
    }

    public sealed class NodeScriptActionHandler : Handler, INodeScriptActionHandler
    {
        public NodeJSActionExecutionData Data { get; set; }

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
            if (systemConnection.Data.TryGetValue("GenerateIdTokenUrl", out var generateIdTokenUrl) && !string.IsNullOrEmpty(generateIdTokenUrl))
            {
                Environment["ACTIONS_ID_TOKEN_REQUEST_URL"] = generateIdTokenUrl;
                Environment["ACTIONS_ID_TOKEN_REQUEST_TOKEN"] = systemConnection.Authorization.Parameters[EndpointAuthorizationParameters.AccessToken];
            }

            // Resolve the target script.
            string target = null;
            if (stage == ActionRunStage.Main)
            {
                target = Data.Script;
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
            ExecutionContext.StepTelemetry.Type = Data.NodeVersion;

            ArgUtil.NotNullOrEmpty(target, nameof(target));
            target = Path.Combine(ActionDirectory, target);
            ArgUtil.File(target, nameof(target));

            // Resolve the working directory.
            string workingDirectory = ExecutionContext.GetGitHubContext("workspace");
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
            }

#if OS_OSX
            if (string.Equals(Data.NodeVersion, "node12", StringComparison.OrdinalIgnoreCase) &&
                Constants.Runner.PlatformArchitecture.Equals(Constants.Architecture.Arm64))
            {
                ExecutionContext.Output($"The node12 is not supported on macOS ARM64 platform. Use node16 instead.");
                Data.NodeVersion = "node16";
            }
#endif
            string nodeVersionOut = System.Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedActionsNodeVersion);
            if (nodeVersionOut == "node16" && Data.NodeVersion != "node16")
            {
                Data.NodeVersion = "node16";
            }
            var nodeRuntimeVersion = await StepHost.DetermineNodeRuntimeVersion(ExecutionContext, Data.NodeVersion);
            string file = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), nodeRuntimeVersion, "bin", $"node{IOUtil.ExeExtension}");

            // Format the arguments passed to node.
            // 1) Wrap the script file path in double quotes.
            // 2) Escape double quotes within the script file path. Double-quote is a valid
            // file name character on Linux.
            string arguments = StepHost.ResolvePathForStepHost(StringUtil.Format(@"""{0}""", target.Replace(@"""", @"\""")));

#if OS_WINDOWS
            // It appears that node.exe outputs UTF8 when not in TTY mode.
            Encoding outputEncoding = Encoding.UTF8;
#else
            // Let .NET choose the default.
            Encoding outputEncoding = null;
#endif

            // Remove environment variable that may cause conflicts with the node within the runner.
            Environment.Remove("NODE_ICU_DATA"); // https://github.com/actions/runner/issues/795

            if (Data.NodeVersion == "node12" && (ExecutionContext.Global.Variables.GetBoolean(Constants.Runner.Features.Node12Warning) ?? false))
            {
                if (!ExecutionContext.JobContext.ContainsKey("Node12ActionsWarnings"))
                {                     
                    ExecutionContext.JobContext["Node12ActionsWarnings"] = new ArrayContextData();
                }
                var repoAction = Action as RepositoryPathReference;
                var actionDisplayName = new StringContextData(repoAction.Name ?? repoAction.Path); // local actions don't have a 'Name'                
                ExecutionContext.JobContext["Node12ActionsWarnings"].AssertArray("Node12ActionsWarnings").Add(actionDisplayName);
            }

            using (var stdoutManager = new OutputManager(ExecutionContext, ActionCommandManager))
            using (var stderrManager = new OutputManager(ExecutionContext, ActionCommandManager))
            {
                StepHost.OutputDataReceived += stdoutManager.OnDataReceived;
                StepHost.ErrorDataReceived += stderrManager.OnDataReceived;

                // Execute the process. Exit code 0 should always be returned.
                // A non-zero exit code indicates infrastructural failure.
                // Task failure should be communicated over STDOUT using ## commands.
                Task<int> step = StepHost.ExecuteAsync(workingDirectory: StepHost.ResolvePathForStepHost(workingDirectory),
                                                fileName: StepHost.ResolvePathForStepHost(file),
                                                arguments: arguments,
                                                environment: Environment,
                                                requireExitCodeZero: false,
                                                outputEncoding: outputEncoding,
                                                killProcessOnCancel: false,
                                                inheritConsoleHandler: !ExecutionContext.Global.Variables.Retain_Default_Encoding,
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
                    ExecutionContext.Debug($"Node Action run completed with exit code {exitCode}");
                    if (exitCode != 0)
                    {
                        ExecutionContext.Result = TaskResult.Failed;
                    }
                }
            }
        }
    }
}
