using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Runner.Worker.Container;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionManager))]
    public interface IActionManager : IRunnerService
    {
        Dictionary<Guid, ContainerInfo> CachedActionContainers { get; }
        Task<List<JobExtensionRunner>> PrepareActionsAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps);
        Definition LoadAction(IExecutionContext executionContext, Pipelines.ActionStep action);
    }

    public sealed class ActionManager : RunnerService, IActionManager
    {
        private const int _defaultFileStreamBufferSize = 4096;

        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
        private const int _defaultCopyBufferSize = 81920;

        private readonly Dictionary<Guid, ContainerInfo> _cachedActionContainers = new Dictionary<Guid, ContainerInfo>();

        public Dictionary<Guid, ContainerInfo> CachedActionContainers => _cachedActionContainers;
        public async Task<List<JobExtensionRunner>> PrepareActionsAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(steps, nameof(steps));

            executionContext.Output("Prepare all required actions");
            Dictionary<string, List<Guid>> imagesToPull = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<Guid>> imagesToBuild = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ActionContainer> imagesToBuildInfo = new Dictionary<string, ActionContainer>(StringComparer.OrdinalIgnoreCase);
            List<JobExtensionRunner> containerSetupSteps = new List<JobExtensionRunner>();
            IEnumerable<Pipelines.ActionStep> actions = steps.OfType<Pipelines.ActionStep>();
            foreach (var action in actions)
            {
                if (action.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry)
                {
                    ArgUtil.NotNull(action, nameof(action));
                    var containerReference = action.Reference as Pipelines.ContainerRegistryReference;
                    ArgUtil.NotNull(containerReference, nameof(containerReference));
                    ArgUtil.NotNullOrEmpty(containerReference.Image, nameof(containerReference.Image));

                    if (!imagesToPull.ContainsKey(containerReference.Image))
                    {
                        imagesToPull[containerReference.Image] = new List<Guid>();
                    }

                    Trace.Info($"Action {action.Name} ({action.Id}) needs to pull image '{containerReference.Image}'");
                    imagesToPull[containerReference.Image].Add(action.Id);
                }
                else if (action.Reference.Type == Pipelines.ActionSourceType.Repository)
                {
                    // only download the repository archive
                    await DownloadRepositoryActionAsync(executionContext, action);

                    // more preparation base on content in the repository (action.yml)
                    var setupInfo = PrepareRepositoryActionAsync(executionContext, action);
                    if (setupInfo != null)
                    {
                        if (!string.IsNullOrEmpty(setupInfo.Image))
                        {
                            if (!imagesToPull.ContainsKey(setupInfo.Image))
                            {
                                imagesToPull[setupInfo.Image] = new List<Guid>();
                            }

                            Trace.Info($"Action {action.Name} ({action.Id}) from repository '{setupInfo.ActionRepository}' needs to pull image '{setupInfo.Image}'");
                            imagesToPull[setupInfo.Image].Add(action.Id);
                        }
                        else
                        {
                            ArgUtil.NotNullOrEmpty(setupInfo.ActionRepository, nameof(setupInfo.ActionRepository));

                            if (!imagesToBuild.ContainsKey(setupInfo.ActionRepository))
                            {
                                imagesToBuild[setupInfo.ActionRepository] = new List<Guid>();
                            }

                            Trace.Info($"Action {action.Name} ({action.Id}) from repository '{setupInfo.ActionRepository}' needs to build image '{setupInfo.Dockerfile}'");
                            imagesToBuild[setupInfo.ActionRepository].Add(action.Id);
                            imagesToBuildInfo[setupInfo.ActionRepository] = setupInfo;
                        }
                    }
                }
            }

            if (imagesToPull.Count > 0)
            {
                foreach (var imageToPull in imagesToPull)
                {
                    Trace.Info($"{imageToPull.Value.Count} steps need to pull image '{imageToPull.Key}'");
                    containerSetupSteps.Add(new JobExtensionRunner(runAsync: this.PullActionContainerAsync,
                                                                   condition: ExpressionManager.Succeeded,
                                                                   displayName: $"Pull {imageToPull.Key}",
                                                                   data: new ContainerSetupInfo(imageToPull.Value, imageToPull.Key)));
                }
            }

            if (imagesToBuild.Count > 0)
            {
                foreach (var imageToBuild in imagesToBuild)
                {
                    var setupInfo = imagesToBuildInfo[imageToBuild.Key];
                    Trace.Info($"{imageToBuild.Value.Count} steps need to build image from '{setupInfo.Dockerfile}'");
                    containerSetupSteps.Add(new JobExtensionRunner(runAsync: this.BuildActionContainerAsync,
                                                                   condition: ExpressionManager.Succeeded,
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

            return containerSetupSteps;
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

                string manifestFile = Path.Combine(actionDirectory, "action.yml");
                string dockerFile = Path.Combine(actionDirectory, "Dockerfile");
                string dockerFileLowerCase = Path.Combine(actionDirectory, "dockerfile");
                if (File.Exists(manifestFile))
                {
                    var manifestManager = HostContext.GetService<IActionManifestManager>();
                    definition.Data = manifestManager.Load(executionContext, manifestFile);

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

                        if (CachedActionContainers.TryGetValue(action.Id, out var container))
                        {
                            Trace.Info($"Image '{containerAction.Image}' already built/pulled, use image: {container.ContainerImage}.");
                            containerAction.Image = container.ContainerImage;
                        }
                    }
                    else if (definition.Data.Execution.ExecutionType == ActionExecutionType.NodeJS)
                    {
                        var nodeAction = definition.Data.Execution as NodeJSActionExecutionData;
                        Trace.Info($"Action node.js file: {nodeAction.Script}.");
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
                    throw new NotSupportedException($"Can't find 'action.yml' or 'Dockerfile' under '{fullPath}'.");
                }
            }
            else if (action.Reference.Type == Pipelines.ActionSourceType.Script)
            {
                definition.Data.Execution = new ScriptActionExecutionData();
                definition.Data.Name = "Run";
                definition.Data.Description = "Execute a script";
            }
            else if (action.Reference.Type == Pipelines.ActionSourceType.AgentPlugin)
            {
                var pluginAction = action.Reference as Pipelines.PluginReference;
                var pluginManager = HostContext.GetService<IRunnerPluginManager>();
                var plugin = pluginManager.GetPluginAction(pluginAction.Plugin);

                ArgUtil.NotNull(plugin, pluginAction.Plugin);
                ArgUtil.NotNullOrEmpty(plugin.PluginTypeName, pluginAction.Plugin);

                definition.Data.Execution = new PluginActionExecutionData()
                {
                    Plugin = plugin.PluginTypeName
                };

                definition.Data.Name = plugin.FriendlyName;
                definition.Data.Description = plugin.Description;
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

            executionContext.Output($"Pull down action image '{setupInfo.Container.Image}'");

            // Pull down docker image with retry up to 3 times
            var dockerManger = HostContext.GetService<IDockerCommandManager>();
            int retryCount = 0;
            int pullExitCode = 0;
            while (retryCount < 3)
            {
                pullExitCode = await dockerManger.DockerPull(executionContext, setupInfo.Container.Image);
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

            executionContext.Output($"Build container for action use: '{setupInfo.Container.Dockerfile}'.");

            // Build docker image with retry up to 3 times
            var dockerManger = HostContext.GetService<IDockerCommandManager>();
            int retryCount = 0;
            int buildExitCode = 0;
            var imageName = $"{dockerManger.DockerInstanceLabel}:{Guid.NewGuid().ToString("N")}";
            while (retryCount < 3)
            {
                buildExitCode = await dockerManger.DockerBuild(executionContext, setupInfo.Container.WorkingDirectory, Directory.GetParent(setupInfo.Container.Dockerfile).FullName, imageName);
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

        private async Task DownloadRepositoryActionAsync(IExecutionContext executionContext, Pipelines.ActionStep repositoryAction)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            var repositoryReference = repositoryAction.Reference as Pipelines.RepositoryPathReference;
            ArgUtil.NotNull(repositoryReference, nameof(repositoryReference));

            if (string.Equals(repositoryReference.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                Trace.Info($"Repository action is in 'self' repository.");
                return;
            }

            if (!string.Equals(repositoryReference.RepositoryType, Pipelines.RepositoryTypes.GitHub, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(repositoryReference.RepositoryType);
            }

            ArgUtil.NotNullOrEmpty(repositoryReference.Name, nameof(repositoryReference.Name));
            ArgUtil.NotNullOrEmpty(repositoryReference.Ref, nameof(repositoryReference.Ref));

            string destDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), repositoryReference.Name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), repositoryReference.Ref);
            if (File.Exists(destDirectory + ".completed"))
            {
                executionContext.Debug($"Action '{repositoryReference.Name}@{repositoryReference.Ref}' already downloaded at '{destDirectory}'.");
                return;
            }
            else
            {
                // make sure we get an clean folder ready to use.
                IOUtil.DeleteDirectory(destDirectory, executionContext.CancellationToken);
                Directory.CreateDirectory(destDirectory);
                executionContext.Output($"Download action repository '{repositoryReference.Name}@{repositoryReference.Ref}'");
            }

#if OS_WINDOWS
            string archiveLink = $"https://api.github.com/repos/{repositoryReference.Name}/zipball/{repositoryReference.Ref}";
#else
            string archiveLink = $"https://api.github.com/repos/{repositoryReference.Name}/tarball/{repositoryReference.Ref}";
#endif
            Trace.Info($"Download archive '{archiveLink}' to '{destDirectory}'.");

            //download and extract action in a temp folder and rename it on success
            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), "_temp_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDirectory);


#if OS_WINDOWS
            string archiveFile = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.zip");
#else
            string archiveFile = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.tar.gz");
#endif
            Trace.Info($"Save archive '{archiveLink}' into {archiveFile}.");
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
                            using (FileStream fs = new FileStream(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _defaultFileStreamBufferSize, useAsync: true))
                            using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                            using (var httpClient = new HttpClient(httpClientHandler))
                            {
                                var authToken = Environment.GetEnvironmentVariable("_GITHUB_ACTION_TOKEN");
                                if (string.IsNullOrEmpty(authToken))
                                {
                                    authToken = executionContext.Variables.Get("PREVIEW_ACTION_TOKEN");
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

                                httpClient.DefaultRequestHeaders.UserAgent.Add(HostContext.UserAgent);
                                using (var result = await httpClient.GetStreamAsync(archiveLink))
                                {
                                    await result.CopyToAsync(fs, _defaultCopyBufferSize, actionDownloadCancellation.Token);
                                    await fs.FlushAsync(actionDownloadCancellation.Token);

                                    // download succeed, break out the retry loop.
                                    break;
                                }
                            }
                        }
                        catch (OperationCanceledException) when (executionContext.CancellationToken.IsCancellationRequested)
                        {
                            Trace.Info($"Action download has been cancelled.");
                            throw;
                        }
                        catch (Exception ex) when (retryCount < 2)
                        {
                            retryCount++;
                            Trace.Error($"Fail to download archive '{archiveLink}' -- Attempt: {retryCount}");
                            Trace.Error(ex);
                            if (actionDownloadTimeout.Token.IsCancellationRequested)
                            {
                                // action download didn't finish within timeout
                                executionContext.Warning($"Action '{archiveLink}' didn't finish download within {timeoutSeconds} seconds.");
                            }
                            else
                            {
                                executionContext.Warning($"Failed to download action '{archiveLink}'. Error {ex.Message}");
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
                executionContext.Debug($"Download '{archiveLink}' to '{archiveFile}'");

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
                File.WriteAllText(destDirectory + ".completed", DateTime.UtcNow.ToString());

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

        private ActionContainer PrepareRepositoryActionAsync(IExecutionContext executionContext, Pipelines.ActionStep repositoryAction)
        {
            var repositoryReference = repositoryAction.Reference as Pipelines.RepositoryPathReference;
            if (string.Equals(repositoryReference.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                Trace.Info($"Repository action is in 'self' repository.");
                return null;
            }

            var setupInfo = new ActionContainer();
            string destDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), repositoryReference.Name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), repositoryReference.Ref);
            string actionEntryDirectory = destDirectory;
            string dockerFileRelativePath = repositoryReference.Name;
            ArgUtil.NotNull(repositoryReference, nameof(repositoryReference));
            if (!string.IsNullOrEmpty(repositoryReference.Path))
            {
                actionEntryDirectory = Path.Combine(destDirectory, repositoryReference.Path);
                dockerFileRelativePath = $"{dockerFileRelativePath}/{repositoryReference.Path}";
                setupInfo.ActionRepository = $"{repositoryReference.Name}/{repositoryReference.Path}@{repositoryReference.Ref}";
            }
            else
            {
                setupInfo.ActionRepository = $"{repositoryReference.Name}@{repositoryReference.Ref}";
            }

            // find the docker file or action.yml file
            var dockerFile = Path.Combine(actionEntryDirectory, "Dockerfile");
            var dockerFileLowerCase = Path.Combine(actionEntryDirectory, "dockerfile");
            var actionManifest = Path.Combine(actionEntryDirectory, "action.yml");
            if (File.Exists(dockerFile))
            {
                executionContext.Debug($"Dockerfile for action: '{dockerFile}'.");
                setupInfo.Dockerfile = dockerFile;
                setupInfo.WorkingDirectory = destDirectory;
                return setupInfo;
            }
            else if (File.Exists(dockerFileLowerCase))
            {
                executionContext.Debug($"Dockerfile for action: '{dockerFileLowerCase}'.");
                setupInfo.Dockerfile = dockerFileLowerCase;
                setupInfo.WorkingDirectory = destDirectory;
                return setupInfo;
            }
            else if (File.Exists(actionManifest))
            {
                executionContext.Debug($"action.yml for action: '{actionManifest}'.");
                var manifestManager = HostContext.GetService<IActionManifestManager>();
                var actionDefinitionData = manifestManager.Load(executionContext, actionManifest);

                if (actionDefinitionData.Execution.ExecutionType == ActionExecutionType.Container)
                {
                    var containerAction = actionDefinitionData.Execution as ContainerActionExecutionData;
                    if (containerAction.Image.EndsWith("Dockerfile") || containerAction.Image.EndsWith("dockerfile"))
                    {
                        var dockerFileFullPath = Path.Combine(actionEntryDirectory, containerAction.Image);
                        executionContext.Debug($"Dockerfile for action: '{dockerFileFullPath}'.");

                        setupInfo.Dockerfile = dockerFileFullPath;
                        setupInfo.WorkingDirectory = destDirectory;
                        return setupInfo;
                    }
                    else if (containerAction.Image.StartsWith("docker://", StringComparison.OrdinalIgnoreCase))
                    {
                        var actionImage = containerAction.Image.Substring("docker://".Length);

                        executionContext.Debug($"Container image for action: '{actionImage}'.");

                        setupInfo.Image = actionImage;
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
                else
                {
                    throw new NotSupportedException(actionDefinitionData.Execution.ExecutionType.ToString());
                }
            }
            else
            {
                var fullPath = IOUtil.ResolvePath(actionEntryDirectory, "."); // resolve full path without access filesystem.
                throw new InvalidOperationException($"Can't find 'action.yml' or 'Dockerfile' under '{fullPath}'.");
            }
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
    }

    public enum ActionExecutionType
    {
        Container,
        NodeJS,
        Plugin,
        Script,
    }

    public sealed class ContainerActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Container;

        public string Image { get; set; }

        public string EntryPoint { get; set; }

        public SequenceToken Arguments { get; set; }

        public MappingToken Environment { get; set; }
    }

    public sealed class NodeJSActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.NodeJS;

        public string Script { get; set; }
    }

    public sealed class PluginActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Plugin;

        public string Plugin { get; set; }
    }

    public sealed class ScriptActionExecutionData : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Script;
    }

    public abstract class ActionExecutionData
    {
        public abstract ActionExecutionType ExecutionType { get; }
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
}

