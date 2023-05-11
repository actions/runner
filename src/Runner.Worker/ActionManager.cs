using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Container;
using GitHub.Services.Common;
using WebApi = GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using PipelineTemplateConstants = GitHub.DistributedTask.Pipelines.ObjectTemplating.PipelineTemplateConstants;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Worker
{
    public class PrepareResult
    {
        public PrepareResult(List<JobExtensionRunner> containerSetupSteps, Dictionary<Guid, IActionRunner> preStepTracker)
        {
            this.ContainerSetupSteps = containerSetupSteps;
            this.PreStepTracker = preStepTracker;
        }

        public List<JobExtensionRunner> ContainerSetupSteps { get; set; }

        public Dictionary<Guid, IActionRunner> PreStepTracker { get; set; }
    }

    [ServiceLocator(Default = typeof(ActionManager))]
    public interface IActionManager : IRunnerService
    {
        Dictionary<Guid, ContainerInfo> CachedActionContainers { get; }
        Dictionary<Guid, List<Pipelines.ActionStep>> CachedEmbeddedPreSteps { get; }
        Dictionary<Guid, List<Guid>> CachedEmbeddedStepIds { get; }
        Dictionary<Guid, Stack<Pipelines.ActionStep>> CachedEmbeddedPostSteps { get; }
        Task<PrepareResult> PrepareActionsAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps, Guid rootStepId = default(Guid));
        Definition LoadAction(IExecutionContext executionContext, Pipelines.ActionStep action);
    }

    public sealed class ActionManager : RunnerService, IActionManager
    {
        private const int _defaultFileStreamBufferSize = 4096;

        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k).
        private const int _defaultCopyBufferSize = 81920;
        private const string _dotcomApiUrl = "https://api.github.com";

        private readonly Dictionary<Guid, ContainerInfo> _cachedActionContainers = new();
        public Dictionary<Guid, ContainerInfo> CachedActionContainers => _cachedActionContainers;

        private readonly Dictionary<Guid, List<Pipelines.ActionStep>> _cachedEmbeddedPreSteps = new();
        public Dictionary<Guid, List<Pipelines.ActionStep>> CachedEmbeddedPreSteps => _cachedEmbeddedPreSteps;

        private readonly Dictionary<Guid, List<Guid>> _cachedEmbeddedStepIds = new();
        public Dictionary<Guid, List<Guid>> CachedEmbeddedStepIds => _cachedEmbeddedStepIds;

        private readonly Dictionary<Guid, Stack<Pipelines.ActionStep>> _cachedEmbeddedPostSteps = new();
        public Dictionary<Guid, Stack<Pipelines.ActionStep>> CachedEmbeddedPostSteps => _cachedEmbeddedPostSteps;

        public async Task<PrepareResult> PrepareActionsAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps, Guid rootStepId = default(Guid))
        {
            // Assert inputs
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(steps, nameof(steps));
            var state = new PrepareActionsState
            {
                ImagesToBuild = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase),
                ImagesToPull = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase),
                ImagesToBuildInfo = new Dictionary<string, ActionContainer>(StringComparer.OrdinalIgnoreCase),
                PreStepTracker = new Dictionary<Guid, IActionRunner>()
            };
            var containerSetupSteps = new List<JobExtensionRunner>();
            var depth = 0;
            // We are running at the start of a job
            if (rootStepId == default(Guid))
            {
                IOUtil.DeleteDirectory(HostContext.GetDirectory(WellKnownDirectory.Actions), executionContext.CancellationToken);
            }
            // We are running mid job due to a local composite action
            else
            {
                if (!_cachedEmbeddedStepIds.ContainsKey(rootStepId))
                {
                    _cachedEmbeddedStepIds[rootStepId] = new List<Guid>();
                    foreach (var compositeStep in steps)
                    {
                        var guid = Guid.NewGuid();
                        compositeStep.Id = guid;
                        _cachedEmbeddedStepIds[rootStepId].Add(guid);
                    }
                }
                depth = 1;
            }
            IEnumerable<Pipelines.ActionStep> actions = steps.OfType<Pipelines.ActionStep>();
            executionContext.Output("Prepare all required actions");
            PrepareActionsState result = new PrepareActionsState();
            try
            {
                result = await PrepareActionsRecursiveAsync(executionContext, state, actions, depth, rootStepId);
            }
            catch (FailedToResolveActionDownloadInfoException ex)
            {
                // Log the error and fail the PrepareActionsAsync Initialization.
                Trace.Error($"Caught exception from PrepareActionsAsync Initialization: {ex}");
                executionContext.InfrastructureError(ex.Message);
                executionContext.Result = TaskResult.Failed;
                throw;
            }
            if (!FeatureManager.IsContainerHooksEnabled(executionContext.Global.Variables))
            {
                if (state.ImagesToPull.Count > 0)
                {
                    foreach (var imageToPull in result.ImagesToPull)
                    {
                        Trace.Info($"{imageToPull.Value.Count} steps need to pull image '{imageToPull.Key}'");
                        containerSetupSteps.Add(new JobExtensionRunner(runAsync: this.PullActionContainerAsync,
                                                                    condition: $"{PipelineTemplateConstants.Success}()",
                                                                    displayName: $"Pull {imageToPull.Key}",
                                                                    data: new ContainerSetupInfo(imageToPull.Value, imageToPull.Key)));
                    }
                }

                if (result.ImagesToBuild.Count > 0)
                {
                    foreach (var imageToBuild in result.ImagesToBuild)
                    {
                        var setupInfo = result.ImagesToBuildInfo[imageToBuild.Key];
                        Trace.Info($"{imageToBuild.Value.Count} steps need to build image from '{setupInfo.Dockerfile}'");
                        containerSetupSteps.Add(new JobExtensionRunner(runAsync: this.BuildActionContainerAsync,
                                                                    condition: $"{PipelineTemplateConstants.Success}()",
                                                                    displayName: $"Build {setupInfo.ActionRepository}",
                                                                    data: new ContainerSetupInfo(imageToBuild.Value, setupInfo.Dockerfile, setupInfo.WorkingDirectory)));
                    }
                }

#if !OS_LINUX
                if (containerSetupSteps.Count > 0)
                {
                    executionContext.Output("Container action is only supported on Linux, skip pull and build docker images.");
                    containerSetupSteps.Clear();
                }
#endif
            }
            return new PrepareResult(containerSetupSteps, result.PreStepTracker);
        }

        private async Task<PrepareActionsState> PrepareActionsRecursiveAsync(IExecutionContext executionContext, PrepareActionsState state, IEnumerable<Pipelines.ActionStep> actions, Int32 depth = 0, Guid parentStepId = default(Guid))
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            if (depth > Constants.CompositeActionsMaxDepth)
            {
                throw new Exception($"Composite action depth exceeded max depth {Constants.CompositeActionsMaxDepth}");
            }
            var repositoryActions = new List<Pipelines.ActionStep>();

            foreach (var action in actions)
            {
                if (action.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry)
                {
                    ArgUtil.NotNull(action, nameof(action));
                    var containerReference = action.Reference as Pipelines.ContainerRegistryReference;
                    ArgUtil.NotNull(containerReference, nameof(containerReference));
                    ArgUtil.NotNullOrEmpty(containerReference.Image, nameof(containerReference.Image));

                    if (!state.ImagesToPull.ContainsKey(containerReference.Image))
                    {
                        state.ImagesToPull[containerReference.Image] = new List<Guid>();
                    }

                    Trace.Info($"Action {action.Name} ({action.Id}) needs to pull image '{containerReference.Image}'");
                    state.ImagesToPull[containerReference.Image].Add(action.Id);
                }
                else if (action.Reference.Type == Pipelines.ActionSourceType.Repository)
                {
                    repositoryActions.Add(action);
                }
            }

            if (repositoryActions.Count > 0)
            {
                // Get the download info
                var downloadInfos = await GetDownloadInfoAsync(executionContext, repositoryActions);

                // Download each action
                foreach (var action in repositoryActions)
                {
                    var lookupKey = GetDownloadInfoLookupKey(action);
                    if (string.IsNullOrEmpty(lookupKey))
                    {
                        continue;
                    }

                    if (!downloadInfos.TryGetValue(lookupKey, out var downloadInfo))
                    {
                        throw new Exception($"Missing download info for {lookupKey}");
                    }

                    await DownloadRepositoryActionAsync(executionContext, downloadInfo);
                }

                // More preparation based on content in the repository (action.yml)
                foreach (var action in repositoryActions)
                {
                    var setupInfo = PrepareRepositoryActionAsync(executionContext, action);
                    if (setupInfo != null && setupInfo.Container != null)
                    {
                        if (!string.IsNullOrEmpty(setupInfo.Container.Image))
                        {
                            if (!state.ImagesToPull.ContainsKey(setupInfo.Container.Image))
                            {
                                state.ImagesToPull[setupInfo.Container.Image] = new List<Guid>();
                            }

                            Trace.Info($"Action {action.Name} ({action.Id}) from repository '{setupInfo.Container.ActionRepository}' needs to pull image '{setupInfo.Container.Image}'");
                            state.ImagesToPull[setupInfo.Container.Image].Add(action.Id);
                        }
                        else
                        {
                            ArgUtil.NotNullOrEmpty(setupInfo.Container.ActionRepository, nameof(setupInfo.Container.ActionRepository));

                            if (!state.ImagesToBuild.ContainsKey(setupInfo.Container.ActionRepository))
                            {
                                state.ImagesToBuild[setupInfo.Container.ActionRepository] = new List<Guid>();
                            }

                            Trace.Info($"Action {action.Name} ({action.Id}) from repository '{setupInfo.Container.ActionRepository}' needs to build image '{setupInfo.Container.Dockerfile}'");
                            state.ImagesToBuild[setupInfo.Container.ActionRepository].Add(action.Id);
                            state.ImagesToBuildInfo[setupInfo.Container.ActionRepository] = setupInfo.Container;
                        }
                    }
                    else if (setupInfo != null && setupInfo.Steps != null && setupInfo.Steps.Count > 0)
                    {
                        state = await PrepareActionsRecursiveAsync(executionContext, state, setupInfo.Steps, depth + 1, action.Id);
                    }
                    var repoAction = action.Reference as Pipelines.RepositoryPathReference;
                    if (repoAction.RepositoryType != Pipelines.PipelineConstants.SelfAlias)
                    {
                        var definition = LoadAction(executionContext, action);
                        if (definition.Data.Execution.HasPre)
                        {
                            Trace.Info($"Add 'pre' execution for {action.Id}");
                            // Root Step
                            if (depth < 1)
                            {
                                var actionRunner = HostContext.CreateService<IActionRunner>();
                                actionRunner.Action = action;
                                actionRunner.Stage = ActionRunStage.Pre;
                                actionRunner.Condition = definition.Data.Execution.InitCondition;
                                state.PreStepTracker[action.Id] = actionRunner;
                            }
                            // Embedded Step
                            else
                            {
                                if (!_cachedEmbeddedPreSteps.ContainsKey(parentStepId))
                                {
                                    _cachedEmbeddedPreSteps[parentStepId] = new List<Pipelines.ActionStep>();
                                }
                                // Clone action so we can modify the condition without affecting the original
                                var clonedAction = action.Clone() as Pipelines.ActionStep;
                                clonedAction.Condition = definition.Data.Execution.InitCondition;
                                _cachedEmbeddedPreSteps[parentStepId].Add(clonedAction);
                            }
                        }

                        if (definition.Data.Execution.HasPost && depth > 0)
                        {
                            if (!_cachedEmbeddedPostSteps.ContainsKey(parentStepId))
                            {
                                // If we haven't done so already, add the parent to the post steps
                                _cachedEmbeddedPostSteps[parentStepId] = new Stack<Pipelines.ActionStep>();
                            }
                            // Clone action so we can modify the condition without affecting the original
                            var clonedAction = action.Clone() as Pipelines.ActionStep;
                            clonedAction.Condition = definition.Data.Execution.CleanupCondition;
                            _cachedEmbeddedPostSteps[parentStepId].Push(clonedAction);
                        }
                    }
                    else if (depth > 0)
                    {
                        // if we're in a composite action and haven't loaded the local action yet
                        // we assume it has a post step
                        if (!_cachedEmbeddedPostSteps.ContainsKey(parentStepId))
                        {
                            // If we haven't done so already, add the parent to the post steps
                            _cachedEmbeddedPostSteps[parentStepId] = new Stack<Pipelines.ActionStep>();
                        }
                        // Clone action so we can modify the condition without affecting the original
                        var clonedAction = action.Clone() as Pipelines.ActionStep;
                        _cachedEmbeddedPostSteps[parentStepId].Push(clonedAction);
                    }
                }
            }

            return state;
        }

        public Definition LoadAction(IExecutionContext executionContext, Pipelines.ActionStep action)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(action, nameof(action));

            // Initialize the definition wrapper object.
            var definition = new Definition()
            {
                Data = new ActionDefinitionData()
            };

            if (action.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry)
            {
                Trace.Info("Load action that reference container from registry.");
                CachedActionContainers.TryGetValue(action.Id, out var container);
                ArgUtil.NotNull(container, nameof(container));
                definition.Data.Execution = new ContainerActionExecutionData()
                {
                    Image = container.ContainerImage
                };

                Trace.Info($"Using action container image: {container.ContainerImage}.");
            }
            else if (action.Reference.Type == Pipelines.ActionSourceType.Repository)
            {
                string actionDirectory = null;
                var repoAction = action.Reference as Pipelines.RepositoryPathReference;
                if (string.Equals(repoAction.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
                {
                    actionDirectory = executionContext.GetGitHubContext("workspace");
                    if (!string.IsNullOrEmpty(repoAction.Path))
                    {
                        actionDirectory = Path.Combine(actionDirectory, repoAction.Path);
                    }
                }
                else
                {
                    actionDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), repoAction.Name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), repoAction.Ref);
                    if (!string.IsNullOrEmpty(repoAction.Path))
                    {
                        actionDirectory = Path.Combine(actionDirectory, repoAction.Path);
                    }
                }

                Trace.Info($"Load action that reference repository from '{actionDirectory}'");
                definition.Directory = actionDirectory;

                string manifestFile = Path.Combine(actionDirectory, Constants.Path.ActionManifestYmlFile);
                string manifestFileYaml = Path.Combine(actionDirectory, Constants.Path.ActionManifestYamlFile);
                string dockerFile = Path.Combine(actionDirectory, "Dockerfile");
                string dockerFileLowerCase = Path.Combine(actionDirectory, "dockerfile");
                if (File.Exists(manifestFile) || File.Exists(manifestFileYaml))
                {
                    var manifestManager = HostContext.GetService<IActionManifestManager>();
                    if (File.Exists(manifestFile))
                    {
                        definition.Data = manifestManager.Load(executionContext, manifestFile);
                    }
                    else
                    {
                        definition.Data = manifestManager.Load(executionContext, manifestFileYaml);
                    }
                    Trace.Verbose($"Action friendly name: '{definition.Data.Name}'");
                    Trace.Verbose($"Action description: '{definition.Data.Description}'");

                    if (definition.Data.Inputs != null)
                    {
                        foreach (var input in definition.Data.Inputs)
                        {
                            Trace.Verbose($"Action input: '{input.Key.ToString()}' default to '{input.Value.ToString()}'");
                        }
                    }

                    if (definition.Data.Execution.ExecutionType == ActionExecutionType.Container)
                    {
                        var containerAction = definition.Data.Execution as ContainerActionExecutionData;
                        Trace.Info($"Action container Dockerfile/image: {containerAction.Image}.");

                        if (containerAction.Arguments != null)
                        {
                            Trace.Info($"Action container args:  {StringUtil.ConvertToJson(containerAction.Arguments)}.");
                        }

                        if (containerAction.Environment != null)
                        {
                            Trace.Info($"Action container env: {StringUtil.ConvertToJson(containerAction.Environment)}.");
                        }

                        if (!string.IsNullOrEmpty(containerAction.Pre))
                        {
                            Trace.Info($"Action container pre entrypoint: {containerAction.Pre}.");
                        }

                        if (!string.IsNullOrEmpty(containerAction.EntryPoint))
                        {
                            Trace.Info($"Action container entrypoint: {containerAction.EntryPoint}.");
                        }

                        if (!string.IsNullOrEmpty(containerAction.Post))
                        {
                            Trace.Info($"Action container post entrypoint: {containerAction.Post}.");
                        }

                        if (CachedActionContainers.TryGetValue(action.Id, out var container))
                        {
                            Trace.Info($"Image '{containerAction.Image}' already built/pulled, use image: {container.ContainerImage}.");
                            containerAction.Image = container.ContainerImage;
                        }
                    }
                    else if (definition.Data.Execution.ExecutionType == ActionExecutionType.NodeJS)
                    {
                        var nodeAction = definition.Data.Execution as NodeJSActionExecutionData;
                        Trace.Info($"Action pre node.js file: {nodeAction.Pre ?? "N/A"}.");
                        Trace.Info($"Action node.js file: {nodeAction.Script}.");
                        Trace.Info($"Action post node.js file: {nodeAction.Post ?? "N/A"}.");
                    }
                    else if (definition.Data.Execution.ExecutionType == ActionExecutionType.Plugin)
                    {
                        var pluginAction = definition.Data.Execution as PluginActionExecutionData;
                        var pluginManager = HostContext.GetService<IRunnerPluginManager>();
                        var plugin = pluginManager.GetPluginAction(pluginAction.Plugin);

                        ArgUtil.NotNull(plugin, pluginAction.Plugin);
                        ArgUtil.NotNullOrEmpty(plugin.PluginTypeName, pluginAction.Plugin);

                        pluginAction.Plugin = plugin.PluginTypeName;
                        Trace.Info($"Action plugin: {plugin.PluginTypeName}.");

                        if (!string.IsNullOrEmpty(plugin.PostPluginTypeName))
                        {
                            pluginAction.Post = plugin.PostPluginTypeName;
                            Trace.Info($"Action cleanup plugin: {plugin.PluginTypeName}.");
                        }
                    }
                    else if (definition.Data.Execution.ExecutionType == ActionExecutionType.Composite)
                    {
                        var compositeAction = definition.Data.Execution as CompositeActionExecutionData;
                        Trace.Info($"Load {compositeAction.Steps?.Count ?? 0} action steps.");
                        Trace.Verbose($"Details: {StringUtil.ConvertToJson(compositeAction?.Steps)}");
                        Trace.Info($"Load: {compositeAction.Outputs?.Count ?? 0} number of outputs");
                        Trace.Info($"Details: {StringUtil.ConvertToJson(compositeAction?.Outputs)}");

                        if (CachedEmbeddedPreSteps.TryGetValue(action.Id, out var preSteps))
                        {
                            compositeAction.PreSteps = preSteps;
                        }

                        if (CachedEmbeddedPostSteps.TryGetValue(action.Id, out var postSteps))
                        {
                            compositeAction.PostSteps = postSteps;
                        }

                        if (_cachedEmbeddedStepIds.ContainsKey(action.Id))
                        {
                            for (var i = 0; i < compositeAction.Steps.Count; i++)
                            {
                                // Load stored Ids for later load actions
                                compositeAction.Steps[i].Id = _cachedEmbeddedStepIds[action.Id][i];
                                if (string.IsNullOrEmpty(executionContext.Global.Variables.Get("DistributedTask.EnableCompositeActions")) && compositeAction.Steps[i].Reference.Type != Pipelines.ActionSourceType.Script)
                                {
                                    throw new Exception("`uses:` keyword is not currently supported.");
                                }
                            }
                        }
                        else
                        {
                            _cachedEmbeddedStepIds[action.Id] = new List<Guid>();
                            foreach (var compositeStep in compositeAction.Steps)
                            {
                                var guid = Guid.NewGuid();
                                compositeStep.Id = guid;
                                _cachedEmbeddedStepIds[action.Id].Add(guid);
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(definition.Data.Execution.ExecutionType.ToString());
                    }
                }
                else if (File.Exists(dockerFile))
                {
                    if (CachedActionContainers.TryGetValue(action.Id, out var container))
                    {
                        definition.Data.Execution = new ContainerActionExecutionData()
                        {
                            Image = container.ContainerImage
                        };
                    }
                    else
                    {
                        definition.Data.Execution = new ContainerActionExecutionData()
                        {
                            Image = dockerFile
                        };
                    }
                }
                else if (File.Exists(dockerFileLowerCase))
                {
                    if (CachedActionContainers.TryGetValue(action.Id, out var container))
                    {
                        definition.Data.Execution = new ContainerActionExecutionData()
                        {
                            Image = container.ContainerImage
                        };
                    }
                    else
                    {
                        definition.Data.Execution = new ContainerActionExecutionData()
                        {
                            Image = dockerFileLowerCase
                        };
                    }
                }
                else
                {
                    var fullPath = IOUtil.ResolvePath(actionDirectory, "."); // resolve full path without access filesystem.
                    throw new NotSupportedException($"Can't find 'action.yml', 'action.yaml' or 'Dockerfile' under '{fullPath}'. Did you forget to run actions/checkout before running your local action?");
                }
            }
            else if (action.Reference.Type == Pipelines.ActionSourceType.Script)
            {
                definition.Data.Execution = new ScriptActionExecutionData();
                definition.Data.Name = "Run";
                definition.Data.Description = "Execute a script";
            }
            else
            {
                throw new NotSupportedException(action.Reference.Type.ToString());
            }

            return definition;
        }

        private async Task PullActionContainerAsync(IExecutionContext executionContext, object data)
        {
            var setupInfo = data as ContainerSetupInfo;
            ArgUtil.NotNull(setupInfo, nameof(setupInfo));
            ArgUtil.NotNullOrEmpty(setupInfo.Container.Image, nameof(setupInfo.Container.Image));

            executionContext.Output($"##[group]Pull down action image '{setupInfo.Container.Image}'");

            // Pull down docker image with retry up to 3 times
            var dockerManager = HostContext.GetService<IDockerCommandManager>();
            int retryCount = 0;
            int pullExitCode = 0;
            while (retryCount < 3)
            {
                pullExitCode = await dockerManager.DockerPull(executionContext, setupInfo.Container.Image);
                if (pullExitCode == 0)
                {
                    break;
                }
                else
                {
                    retryCount++;
                    if (retryCount < 3)
                    {
                        var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
                        executionContext.Warning($"Docker pull failed with exit code {pullExitCode}, back off {backOff.TotalSeconds} seconds before retry.");
                        await Task.Delay(backOff);
                    }
                }
            }
            executionContext.Output("##[endgroup]");

            if (retryCount == 3 && pullExitCode != 0)
            {
                throw new InvalidOperationException($"Docker pull failed with exit code {pullExitCode}");
            }

            foreach (var stepId in setupInfo.StepIds)
            {
                CachedActionContainers[stepId] = new ContainerInfo() { ContainerImage = setupInfo.Container.Image };
                Trace.Info($"Prepared docker image '{setupInfo.Container.Image}' for action {stepId} ({setupInfo.Container.Image})");
            }
        }

        private async Task BuildActionContainerAsync(IExecutionContext executionContext, object data)
        {
            var setupInfo = data as ContainerSetupInfo;
            ArgUtil.NotNull(setupInfo, nameof(setupInfo));
            ArgUtil.NotNullOrEmpty(setupInfo.Container.Dockerfile, nameof(setupInfo.Container.Dockerfile));

            executionContext.Output($"##[group]Build container for action use: '{setupInfo.Container.Dockerfile}'.");

            // Build docker image with retry up to 3 times
            var dockerManager = HostContext.GetService<IDockerCommandManager>();
            int retryCount = 0;
            int buildExitCode = 0;
            var imageName = $"{dockerManager.DockerInstanceLabel}:{Guid.NewGuid().ToString("N")}";
            while (retryCount < 3)
            {
                buildExitCode = await dockerManager.DockerBuild(
                    executionContext,
                    setupInfo.Container.WorkingDirectory,
                    setupInfo.Container.Dockerfile,
                    Directory.GetParent(setupInfo.Container.Dockerfile).FullName,
                    imageName);
                if (buildExitCode == 0)
                {
                    break;
                }
                else
                {
                    retryCount++;
                    if (retryCount < 3)
                    {
                        var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
                        executionContext.Warning($"Docker build failed with exit code {buildExitCode}, back off {backOff.TotalSeconds} seconds before retry.");
                        await Task.Delay(backOff);
                    }
                }
            }
            executionContext.Output("##[endgroup]");

            if (retryCount == 3 && buildExitCode != 0)
            {
                throw new InvalidOperationException($"Docker build failed with exit code {buildExitCode}");
            }

            foreach (var stepId in setupInfo.StepIds)
            {
                CachedActionContainers[stepId] = new ContainerInfo() { ContainerImage = imageName };
                Trace.Info($"Prepared docker image '{imageName}' for action {stepId} ({setupInfo.Container.Dockerfile})");
            }
        }

        // This implementation is temporary and will be replaced with a REST API call to the service to resolve
        private async Task<IDictionary<string, WebApi.ActionDownloadInfo>> GetDownloadInfoAsync(IExecutionContext executionContext, List<Pipelines.ActionStep> actions)
        {
            executionContext.Output("Getting action download info");

            // Convert to action reference
            var actionReferences = actions
                .GroupBy(x => GetDownloadInfoLookupKey(x))
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .Select(x =>
                {
                    var action = x.First();
                    var repositoryReference = action.Reference as Pipelines.RepositoryPathReference;
                    ArgUtil.NotNull(repositoryReference, nameof(repositoryReference));
                    return new WebApi.ActionReference
                    {
                        NameWithOwner = repositoryReference.Name,
                        Ref = repositoryReference.Ref,
                        Path = repositoryReference.Path,
                    };
                })
                .ToList();

            // Nothing to resolve?
            if (actionReferences.Count == 0)
            {
                return new Dictionary<string, WebApi.ActionDownloadInfo>();
            }

            // Resolve download info
            var launchServer = HostContext.GetService<ILaunchServer>();
            var jobServer = HostContext.GetService<IJobServer>();
            var actionDownloadInfos = default(WebApi.ActionDownloadInfoCollection);
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    if (MessageUtil.IsRunServiceJob(executionContext.Global.Variables.Get(Constants.Variables.System.JobRequestType)))
                    {
                        actionDownloadInfos = await launchServer.ResolveActionsDownloadInfoAsync(executionContext.Global.Plan.PlanId, executionContext.Root.Id, new WebApi.ActionReferenceList { Actions = actionReferences }, executionContext.CancellationToken);
                    }
                    else
                    {
                        actionDownloadInfos = await jobServer.ResolveActionDownloadInfoAsync(executionContext.Global.Plan.ScopeIdentifier, executionContext.Global.Plan.PlanType, executionContext.Global.Plan.PlanId, executionContext.Root.Id, new WebApi.ActionReferenceList { Actions = actionReferences }, executionContext.CancellationToken);
                    }
                    break;
                }
                catch (Exception ex) when (!executionContext.CancellationToken.IsCancellationRequested) // Do not retry if the run is cancelled.
                {
                    // UnresolvableActionDownloadInfoException is a 422 client error, don't retry
                    // Some possible cases are:
                    // * Repo is rate limited
                    // * Repo or tag doesn't exist, or isn't public
                    // * Policy validation failed
                    if (attempt < 3 && !(ex is WebApi.UnresolvableActionDownloadInfoException))
                    {
                        executionContext.Output($"Failed to resolve action download info. Error: {ex.Message}");
                        executionContext.Debug(ex.ToString());
                        if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GITHUB_ACTION_DOWNLOAD_NO_BACKOFF")))
                        {
                            var backoff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                            executionContext.Output($"Retrying in {backoff.TotalSeconds} seconds");
                            await Task.Delay(backoff);
                        }
                    }
                    else
                    {
                        // Some possible cases are:
                        // * Repo is rate limited
                        // * Repo or tag doesn't exist, or isn't public
                        // * Policy validation failed
                        if (ex is WebApi.UnresolvableActionDownloadInfoException)
                        {
                            throw;
                        }
                        else
                        {
                            // This exception will be traced as an infrastructure failure
                            throw new WebApi.FailedToResolveActionDownloadInfoException("Failed to resolve action download info.", ex);
                        }
                    }
                }
            }

            ArgUtil.NotNull(actionDownloadInfos, nameof(actionDownloadInfos));
            ArgUtil.NotNull(actionDownloadInfos.Actions, nameof(actionDownloadInfos.Actions));
            var apiUrl = GetApiUrl(executionContext);
            var defaultAccessToken = executionContext.GetGitHubContext("token");
            var configurationStore = HostContext.GetService<IConfigurationStore>();
            var runnerSettings = configurationStore.GetSettings();

            foreach (var actionDownloadInfo in actionDownloadInfos.Actions.Values)
            {
                // Add secret
                HostContext.SecretMasker.AddValue(actionDownloadInfo.Authentication?.Token);

                // Default auth token
                if (string.IsNullOrEmpty(actionDownloadInfo.Authentication?.Token))
                {
                    actionDownloadInfo.Authentication = new WebApi.ActionDownloadAuthentication { Token = defaultAccessToken };
                }
            }

            return actionDownloadInfos.Actions;
        }

        private async Task DownloadRepositoryActionAsync(IExecutionContext executionContext, WebApi.ActionDownloadInfo downloadInfo)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(downloadInfo, nameof(downloadInfo));
            ArgUtil.NotNullOrEmpty(downloadInfo.NameWithOwner, nameof(downloadInfo.NameWithOwner));
            ArgUtil.NotNullOrEmpty(downloadInfo.Ref, nameof(downloadInfo.Ref));

            string destDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), downloadInfo.NameWithOwner.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), downloadInfo.Ref);
            string watermarkFile = GetWatermarkFilePath(destDirectory);
            if (File.Exists(watermarkFile))
            {
                executionContext.Debug($"Action '{downloadInfo.NameWithOwner}@{downloadInfo.Ref}' already downloaded at '{destDirectory}'.");
                return;
            }
            else
            {
                // make sure we get a clean folder ready to use.
                IOUtil.DeleteDirectory(destDirectory, executionContext.CancellationToken);
                Directory.CreateDirectory(destDirectory);
                executionContext.Output($"Download action repository '{downloadInfo.NameWithOwner}@{downloadInfo.Ref}' (SHA:{downloadInfo.ResolvedSha})");
            }

            await DownloadRepositoryActionAsync(executionContext, downloadInfo, destDirectory);
        }

        private string GetApiUrl(IExecutionContext executionContext)
        {
            string apiUrl = executionContext.GetGitHubContext("api_url");
            if (!string.IsNullOrEmpty(apiUrl))
            {
                return apiUrl;
            }
            // Once the api_url is set for hosted, we can remove this fallback (it doesn't make sense for GHES)
            return _dotcomApiUrl;
        }

        private static string BuildLinkToActionArchive(string apiUrl, string repository, string @ref)
        {
#if OS_WINDOWS
            return $"{apiUrl}/repos/{repository}/zipball/{@ref}";
#else
            return $"{apiUrl}/repos/{repository}/tarball/{@ref}";
#endif
        }

        private async Task DownloadRepositoryActionAsync(IExecutionContext executionContext, WebApi.ActionDownloadInfo downloadInfo, string destDirectory)
        {
            //download and extract action in a temp folder and rename it on success
            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), "_temp_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDirectory);

#if OS_WINDOWS
            string archiveFile = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.zip");
            string link = downloadInfo?.ZipballUrl;
#else
            string archiveFile = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.tar.gz");
            string link = downloadInfo?.TarballUrl;
#endif

            Trace.Info($"Save archive '{link}' into {archiveFile}.");
            try
            {
                int retryCount = 0;

                // Allow up to 20 * 60s for any action to be downloaded from github graph.
                int timeoutSeconds = 20 * 60;
                while (retryCount < 3)
                {
                    using (var actionDownloadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    using (var actionDownloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(actionDownloadTimeout.Token, executionContext.CancellationToken))
                    {
                        try
                        {
                            //open zip stream in async mode
                            using (FileStream fs = new(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _defaultFileStreamBufferSize, useAsync: true))
                            using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                            using (var httpClient = new HttpClient(httpClientHandler))
                            {
                                httpClient.DefaultRequestHeaders.Authorization = CreateAuthHeader(downloadInfo.Authentication?.Token);

                                httpClient.DefaultRequestHeaders.UserAgent.AddRange(HostContext.UserAgents);
                                using (var response = await httpClient.GetAsync(link))
                                {
                                    if (response.IsSuccessStatusCode)
                                    {
                                        using (var result = await response.Content.ReadAsStreamAsync())
                                        {
                                            await result.CopyToAsync(fs, _defaultCopyBufferSize, actionDownloadCancellation.Token);
                                            await fs.FlushAsync(actionDownloadCancellation.Token);

                                            // download succeed, break out the retry loop.
                                            break;
                                        }
                                    }
                                    else if (response.StatusCode == HttpStatusCode.NotFound)
                                    {
                                        // It doesn't make sense to retry in this case, so just stop
                                        throw new ActionNotFoundException(new Uri(link));
                                    }
                                    else
                                    {
                                        // Something else bad happened, let's go to our retry logic
                                        response.EnsureSuccessStatusCode();
                                    }
                                }
                            }
                        }
                        catch (OperationCanceledException) when (executionContext.CancellationToken.IsCancellationRequested)
                        {
                            Trace.Info("Action download has been cancelled.");
                            throw;
                        }
                        catch (ActionNotFoundException)
                        {
                            Trace.Info($"The action at '{link}' does not exist");
                            throw;
                        }
                        catch (Exception ex) when (retryCount < 2)
                        {
                            retryCount++;
                            Trace.Error($"Fail to download archive '{link}' -- Attempt: {retryCount}");
                            Trace.Error(ex);
                            if (actionDownloadTimeout.Token.IsCancellationRequested)
                            {
                                // action download didn't finish within timeout
                                executionContext.Warning($"Action '{link}' didn't finish download within {timeoutSeconds} seconds.");
                            }
                            else
                            {
                                executionContext.Warning($"Failed to download action '{link}'. Error: {ex.Message}");
                            }
                        }
                    }

                    if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GITHUB_ACTION_DOWNLOAD_NO_BACKOFF")))
                    {
                        var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                        executionContext.Warning($"Back off {backOff.TotalSeconds} seconds before retry.");
                        await Task.Delay(backOff);
                    }
                }

                ArgUtil.NotNullOrEmpty(archiveFile, nameof(archiveFile));
                executionContext.Debug($"Download '{link}' to '{archiveFile}'");

                var stagingDirectory = Path.Combine(tempDirectory, "_staging");
                Directory.CreateDirectory(stagingDirectory);

#if OS_WINDOWS
                ZipFile.ExtractToDirectory(archiveFile, stagingDirectory);
#else
                string tar = WhichUtil.Which("tar", require: true, trace: Trace);

                // tar -xzf
                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                {
                    processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Trace.Info(args.Data);
                        }
                    });

                    processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Trace.Error(args.Data);
                        }
                    });

                    int exitCode = await processInvoker.ExecuteAsync(stagingDirectory, tar, $"-xzf \"{archiveFile}\"", null, executionContext.CancellationToken);
                    if (exitCode != 0)
                    {
                        throw new NotSupportedException($"Can't use 'tar -xzf' extract archive file: {archiveFile}. return code: {exitCode}.");
                    }
                }
#endif

                // repository archive from github always contains a nested folder
                var subDirectories = new DirectoryInfo(stagingDirectory).GetDirectories();
                if (subDirectories.Length != 1)
                {
                    throw new InvalidOperationException($"'{archiveFile}' contains '{subDirectories.Length}' directories");
                }
                else
                {
                    executionContext.Debug($"Unwrap '{subDirectories[0].Name}' to '{destDirectory}'");
                    IOUtil.CopyDirectory(subDirectories[0].FullName, destDirectory, executionContext.CancellationToken);
                }

                Trace.Verbose("Create watermark file indicate action download succeed.");
                string watermarkFile = GetWatermarkFilePath(destDirectory);
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());

                executionContext.Debug($"Archive '{archiveFile}' has been unzipped into '{destDirectory}'.");
                Trace.Info("Finished getting action repository.");
            }
            finally
            {
                try
                {
                    //if the temp folder wasn't moved -> wipe it
                    if (Directory.Exists(tempDirectory))
                    {
                        Trace.Verbose("Deleting action temp folder: {0}", tempDirectory);
                        IOUtil.DeleteDirectory(tempDirectory, CancellationToken.None); // Don't cancel this cleanup and should be pretty fast.
                    }
                }
                catch (Exception ex)
                {
                    //it is not critical if we fail to delete the temp folder
                    Trace.Warning("Failed to delete temp folder '{0}'. Exception: {1}", tempDirectory, ex);
                }
            }
        }

        private void ConfigureAuthorizationFromContext(IExecutionContext executionContext, HttpClient httpClient)
        {
            var authToken = Environment.GetEnvironmentVariable("_GITHUB_ACTION_TOKEN");
            if (string.IsNullOrEmpty(authToken))
            {
                // TODO: Deprecate the PREVIEW_ACTION_TOKEN
                authToken = executionContext.Global.Variables.Get("PREVIEW_ACTION_TOKEN");
            }

            if (!string.IsNullOrEmpty(authToken))
            {
                HostContext.SecretMasker.AddValue(authToken);
                var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"PAT:{authToken}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodingToken);
            }
            else
            {
                var accessToken = executionContext.GetGitHubContext("token");
                var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{accessToken}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodingToken);
            }
        }

        private string GetWatermarkFilePath(string directory) => directory + ".completed";

        private ActionSetupInfo PrepareRepositoryActionAsync(IExecutionContext executionContext, Pipelines.ActionStep repositoryAction)
        {
            var repositoryReference = repositoryAction.Reference as Pipelines.RepositoryPathReference;
            if (string.Equals(repositoryReference.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                Trace.Info($"Repository action is in 'self' repository.");
                return null;
            }
            var setupInfo = new ActionSetupInfo();
            var actionContainer = new ActionContainer();
            string destDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), repositoryReference.Name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), repositoryReference.Ref);
            string actionEntryDirectory = destDirectory;
            string dockerFileRelativePath = repositoryReference.Name;
            ArgUtil.NotNull(repositoryReference, nameof(repositoryReference));
            if (!string.IsNullOrEmpty(repositoryReference.Path))
            {
                actionEntryDirectory = Path.Combine(destDirectory, repositoryReference.Path);
                dockerFileRelativePath = $"{dockerFileRelativePath}/{repositoryReference.Path}";
                actionContainer.ActionRepository = $"{repositoryReference.Name}/{repositoryReference.Path}@{repositoryReference.Ref}";
            }
            else
            {
                actionContainer.ActionRepository = $"{repositoryReference.Name}@{repositoryReference.Ref}";
            }

            // find the docker file or action.yml file
            var dockerFile = Path.Combine(actionEntryDirectory, "Dockerfile");
            var dockerFileLowerCase = Path.Combine(actionEntryDirectory, "dockerfile");
            var actionManifest = Path.Combine(actionEntryDirectory, Constants.Path.ActionManifestYmlFile);
            var actionManifestYaml = Path.Combine(actionEntryDirectory, Constants.Path.ActionManifestYamlFile);
            if (File.Exists(actionManifest) || File.Exists(actionManifestYaml))
            {
                executionContext.Debug($"action.yml for action: '{actionManifest}'.");
                var manifestManager = HostContext.GetService<IActionManifestManager>();
                ActionDefinitionData actionDefinitionData = null;
                if (File.Exists(actionManifest))
                {
                    actionDefinitionData = manifestManager.Load(executionContext, actionManifest);
                }
                else
                {
                    actionDefinitionData = manifestManager.Load(executionContext, actionManifestYaml);
                }

                if (actionDefinitionData.Execution.ExecutionType == ActionExecutionType.Container)
                {
                    var containerAction = actionDefinitionData.Execution as ContainerActionExecutionData;
                    if (containerAction.Image.EndsWith("Dockerfile") || containerAction.Image.EndsWith("dockerfile"))
                    {
                        var dockerFileFullPath = Path.Combine(actionEntryDirectory, containerAction.Image);
                        executionContext.Debug($"Dockerfile for action: '{dockerFileFullPath}'.");

                        actionContainer.Dockerfile = dockerFileFullPath;
                        actionContainer.WorkingDirectory = destDirectory;
                        setupInfo.Container = actionContainer;
                        return setupInfo;
                    }
                    else if (containerAction.Image.StartsWith("docker://", StringComparison.OrdinalIgnoreCase))
                    {
                        var actionImage = containerAction.Image.Substring("docker://".Length);

                        executionContext.Debug($"Container image for action: '{actionImage}'.");

                        actionContainer.Image = actionImage;
                        setupInfo.Container = actionContainer;
                        return setupInfo;
                    }
                    else
                    {
                        throw new NotSupportedException($"'{containerAction.Image}' should be either '[path]/Dockerfile' or 'docker://image[:tag]'.");
                    }
                }
                else if (actionDefinitionData.Execution.ExecutionType == ActionExecutionType.NodeJS)
                {
                    Trace.Info($"Action node.js file: {(actionDefinitionData.Execution as NodeJSActionExecutionData).Script}, no more preparation.");
                    return null;
                }
                else if (actionDefinitionData.Execution.ExecutionType == ActionExecutionType.Plugin)
                {
                    Trace.Info($"Action plugin: {(actionDefinitionData.Execution as PluginActionExecutionData).Plugin}, no more preparation.");
                    return null;
                }
                else if (actionDefinitionData.Execution.ExecutionType == ActionExecutionType.Composite)
                {
                    Trace.Info($"Loading Composite steps");
                    var compositeAction = actionDefinitionData.Execution as CompositeActionExecutionData;
                    setupInfo.Steps = compositeAction.Steps;

                    // cache steps ids if not done so already
                    if (!_cachedEmbeddedStepIds.ContainsKey(repositoryAction.Id))
                    {
                        _cachedEmbeddedStepIds[repositoryAction.Id] = new List<Guid>();
                        foreach (var compositeStep in compositeAction.Steps)
                        {
                            var guid = Guid.NewGuid();
                            compositeStep.Id = guid;
                            _cachedEmbeddedStepIds[repositoryAction.Id].Add(guid);
                        }
                    }

                    foreach (var step in compositeAction.Steps)
                    {
                        if (string.IsNullOrEmpty(executionContext.Global.Variables.Get("DistributedTask.EnableCompositeActions")) && step.Reference.Type != Pipelines.ActionSourceType.Script)
                        {
                            throw new Exception("`uses:` keyword is not currently supported.");
                        }
                    }
                    return setupInfo;
                }
                else
                {
                    throw new NotSupportedException(actionDefinitionData.Execution.ExecutionType.ToString());
                }
            }
            else if (File.Exists(dockerFile))
            {
                executionContext.Debug($"Dockerfile for action: '{dockerFile}'.");
                actionContainer.Dockerfile = dockerFile;
                actionContainer.WorkingDirectory = destDirectory;
                setupInfo.Container = actionContainer;
                return setupInfo;
            }
            else if (File.Exists(dockerFileLowerCase))
            {
                executionContext.Debug($"Dockerfile for action: '{dockerFileLowerCase}'.");
                actionContainer.Dockerfile = dockerFileLowerCase;
                actionContainer.WorkingDirectory = destDirectory;
                setupInfo.Container = actionContainer;
                return setupInfo;
            }
            else
            {
                var fullPath = IOUtil.ResolvePath(actionEntryDirectory, "."); // resolve full path without access filesystem.
                throw new InvalidOperationException($"Can't find 'action.yml', 'action.yaml' or 'Dockerfile' under '{fullPath}'. Did you forget to run actions/checkout before running your local action?");
            }
        }

        private static string GetDownloadInfoLookupKey(Pipelines.ActionStep action)
        {
            if (action.Reference.Type != Pipelines.ActionSourceType.Repository)
            {
                return null;
            }

            var repositoryReference = action.Reference as Pipelines.RepositoryPathReference;
            ArgUtil.NotNull(repositoryReference, nameof(repositoryReference));

            if (string.Equals(repositoryReference.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!string.Equals(repositoryReference.RepositoryType, Pipelines.RepositoryTypes.GitHub, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(repositoryReference.RepositoryType);
            }

            ArgUtil.NotNullOrEmpty(repositoryReference.Name, nameof(repositoryReference.Name));
            ArgUtil.NotNullOrEmpty(repositoryReference.Ref, nameof(repositoryReference.Ref));
            return $"{repositoryReference.Name}@{repositoryReference.Ref}";
        }

        private static string GetDownloadInfoLookupKey(WebApi.ActionDownloadInfo info)
        {
            ArgUtil.NotNullOrEmpty(info.NameWithOwner, nameof(info.NameWithOwner));
            ArgUtil.NotNullOrEmpty(info.Ref, nameof(info.Ref));
            return $"{info.NameWithOwner}@{info.Ref}";
        }

        private AuthenticationHeaderValue CreateAuthHeader(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{token}"));
            HostContext.SecretMasker.AddValue(base64EncodingToken);
            return new AuthenticationHeaderValue("Basic", base64EncodingToken);
        }
    }

    public sealed class Definition
    {
        public ActionDefinitionData Data { get; set; }
        public string Directory { get; set; }
    }

    public sealed class ActionDefinitionData
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public MappingToken Inputs { get; set; }

        public ActionExecutionData Execution { get; set; }

        public Dictionary<String, String> Deprecated { get; set; }
    }

    public enum ActionExecutionType
    {
        Container,
        NodeJS,
        Plugin,
        Script,
        Composite,
    }

    public sealed class ContainerActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Container;

        public override bool HasPre => !string.IsNullOrEmpty(Pre);
        public override bool HasPost => !string.IsNullOrEmpty(Post);

        public string Image { get; set; }

        public string EntryPoint { get; set; }

        public SequenceToken Arguments { get; set; }

        public MappingToken Environment { get; set; }

        public string Pre { get; set; }

        public string Post { get; set; }
    }

    public sealed class NodeJSActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.NodeJS;

        public override bool HasPre => !string.IsNullOrEmpty(Pre);
        public override bool HasPost => !string.IsNullOrEmpty(Post);

        public string Script { get; set; }

        public string Pre { get; set; }

        public string Post { get; set; }

        public string NodeVersion { get; set; }
    }

    public sealed class PluginActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Plugin;

        public override bool HasPre => false;

        public override bool HasPost => !string.IsNullOrEmpty(Post);

        public string Plugin { get; set; }

        public string Post { get; set; }
    }

    public sealed class ScriptActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Script;
        public override bool HasPre => false;
        public override bool HasPost => false;
    }

    public sealed class CompositeActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Composite;
        public override bool HasPre => PreSteps.Count > 0;
        public override bool HasPost => PostSteps.Count > 0;
        public List<Pipelines.ActionStep> PreSteps { get; set; }
        public List<Pipelines.ActionStep> Steps { get; set; }
        public Stack<Pipelines.ActionStep> PostSteps { get; set; }
        public MappingToken Outputs { get; set; }
    }

    public abstract class ActionExecutionData
    {
        private string _initCondition = $"{Constants.Expressions.Always}()";
        private string _cleanupCondition = $"{Constants.Expressions.Always}()";

        public abstract ActionExecutionType ExecutionType { get; }

        public abstract bool HasPre { get; }
        public abstract bool HasPost { get; }

        public string CleanupCondition
        {
            get { return _cleanupCondition; }
            set { _cleanupCondition = value; }
        }

        public string InitCondition
        {
            get { return _initCondition; }
            set { _initCondition = value; }
        }
    }

    public class ContainerSetupInfo
    {
        public ContainerSetupInfo(List<Guid> ids, string image)
        {
            StepIds = ids;
            Container = new ActionContainer()
            {
                Image = image
            };
        }

        public ContainerSetupInfo(List<Guid> ids, string dockerfile, string workingDirectory)
        {
            StepIds = ids;
            Container = new ActionContainer()
            {
                Dockerfile = dockerfile,
                WorkingDirectory = workingDirectory
            };
        }

        public List<Guid> StepIds { get; set; }

        public ActionContainer Container { get; set; }
    }

    public class ActionContainer
    {
        public string Image { get; set; }
        public string Dockerfile { get; set; }
        public string WorkingDirectory { get; set; }
        public string ActionRepository { get; set; }
    }

    public class ActionSetupInfo
    {
        public ActionContainer Container { get; set; }
        public List<Pipelines.ActionStep> Steps { get; set; }
    }

    public class PrepareActionsState
    {
        public Dictionary<string, List<Guid>> ImagesToPull;
        public Dictionary<string, List<Guid>> ImagesToBuild;
        public Dictionary<string, ActionContainer> ImagesToBuildInfo;
        public Dictionary<Guid, IActionRunner> PreStepTracker;
    }
}
