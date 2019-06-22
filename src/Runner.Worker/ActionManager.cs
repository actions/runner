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
using Newtonsoft.Json.Linq;
using GitHub.Runner.Worker.Container;
using System.Net.Http;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Net.Http.Headers;
using System.Text;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionManager))]
    public interface IActionManager : IRunnerService
    {
        Dictionary<Guid, ContainerInfo> CachedActionContainers { get; }
        Task DownloadAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps);
        Definition LoadAction(IExecutionContext executionContext, Pipelines.ActionStep action);
    }

    public sealed class ActionManager : RunnerService, IActionManager
    {
        private const int _defaultFileStreamBufferSize = 4096;

        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
        private const int _defaultCopyBufferSize = 81920;

        private readonly Dictionary<Guid, ContainerInfo> _cachedActionContainers = new Dictionary<Guid, ContainerInfo>();

        public Dictionary<Guid, ContainerInfo> CachedActionContainers => _cachedActionContainers;

        public async Task DownloadAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(steps, nameof(steps));

            executionContext.Output("Download all required actions.");

            IEnumerable<Pipelines.ActionStep> actions = steps.OfType<Pipelines.ActionStep>();

            HashSet<string> actionContainers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var containerAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry))
            {
                var container = containerAction.Reference as Pipelines.ContainerRegistryReference;
                actionContainers.Add(container.Image);
            }

            HashSet<string> actionRepositories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var repositoryAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.Repository))
            {
                var repository = repositoryAction.Reference as Pipelines.RepositoryPathReference;
                if (!string.IsNullOrEmpty(repository.Name))
                {
                    actionRepositories.Add($"{repository.Type}:{repository.Name}@{repository.Ref}");
                }
            }

            if (actionContainers.Count() == 0 &&
                actionRepositories.Count() == 0)
            {
                executionContext.Debug("There is no required tasks/actions need to download.");
                return;
            }

            foreach (var containerAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry))
            {
                await DownloadContainerRegistryActionAsync(executionContext, containerAction);
            }

            foreach (var repositoryAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.Repository))
            {
                await DownloadRepositoryActionAsync(executionContext, repositoryAction);
            }
        }

        private async Task DownloadContainerRegistryActionAsync(IExecutionContext executionContext, Pipelines.ActionStep containerAction)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(containerAction, nameof(containerAction));

            var containerReference = containerAction.Reference as Pipelines.ContainerRegistryReference;
            ArgUtil.NotNull(containerReference, nameof(containerReference));
            ArgUtil.NotNullOrEmpty(containerReference.Image, nameof(containerReference.Image));

            executionContext.Output($"Pull down action image '{containerReference.Image}'");

            // Pull down docker image with retry up to 3 times
            var dockerManger = HostContext.GetService<IDockerCommandManager>();
            int retryCount = 0;
            int pullExitCode = 0;
            while (retryCount < 3)
            {
                pullExitCode = await dockerManger.DockerPull(executionContext, containerReference.Image);
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

            CachedActionContainers[containerAction.Id] = new ContainerInfo() { ContainerImage = containerReference.Image };
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

#if OS_WINDOWS
            string archiveLink = $"https://api.github.com/repos/{repositoryReference.Name}/zipball/{repositoryReference.Ref}";
#else
            string archiveLink = $"https://api.github.com/repos/{repositoryReference.Name}/tarball/{repositoryReference.Ref}";
#endif

            string destDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Actions), repositoryReference.Name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), repositoryReference.Ref);
            Trace.Info($"Download archive '{archiveLink}' to '{destDirectory}'.");
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
            }

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
                                executionContext.Warning(StringUtil.Loc("ActionDownloadTimeout", archiveLink, timeoutSeconds));
                            }
                            else
                            {
                                executionContext.Warning(StringUtil.Loc("ActionDownloadFailed", archiveLink, ex.Message));
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
                string tar = WhichUtil.Which("tar", trace: Trace);
                if (string.IsNullOrEmpty(tar))
                {
                    throw new NotSupportedException($"tar -xzf");
                }

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
                    executionContext.Warning(StringUtil.Loc("FailedDeletingTempDirectory0Message1", tempDirectory, ex.Message));
                }
            }

            string actionEntryDirectory = destDirectory;
            ArgUtil.NotNull(repositoryReference, nameof(repositoryReference));
            if (!string.IsNullOrEmpty(repositoryReference.Path))
            {
                actionEntryDirectory = Path.Combine(destDirectory, repositoryReference.Path);
            }

            // find the docker file
            string dockerFile = Path.Combine(actionEntryDirectory, "Dockerfile");
            if (File.Exists(dockerFile))
            {
                executionContext.Output($"Dockerfile for action: '{dockerFile}'.");

                var dockerManger = HostContext.GetService<IDockerCommandManager>();
                var imageName = $"{dockerManger.DockerInstanceLabel}:{Guid.NewGuid().ToString("N")}";
                var buildExitCode = await dockerManger.DockerBuild(executionContext, destDirectory, Directory.GetParent(dockerFile).FullName, imageName);
                if (buildExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker build failed with exit code {buildExitCode}");
                }

                CachedActionContainers[repositoryAction.Id] = new ContainerInfo() { ContainerImage = imageName };
            }
            else
            {
                var actionManifest = Path.Combine(actionEntryDirectory, "action.yml");
                if (File.Exists(actionManifest))
                {
                    executionContext.Output($"action.yml for action: '{actionManifest}'.");
                }
                else
                {
                    throw new InvalidOperationException($"'{actionEntryDirectory}' does not contains any action entry file.");
                }
            }
        }

        public Definition LoadAction(IExecutionContext executionContext, Pipelines.ActionStep action)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(action, nameof(action));

            // Initialize the definition wrapper object.
            var definition = new Definition()
            {
                Data = new DefinitionData()
                {
                    Execution = new ExecutionData()
                }
            };

            if (action.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry)
            {
                Trace.Info("Load action that reference container from registry.");
                CachedActionContainers.TryGetValue(action.Id, out var container);
                ArgUtil.NotNull(container, nameof(container));
                definition.Data.Execution.ContainerAction = new ContainerActionHandlerData()
                {
                    ContainerImage = container.ContainerImage
                };
                Trace.Info($"Using action container image: {container.ContainerImage}.");
            }
            else if (action.Reference.Type == Pipelines.ActionSourceType.Repository)
            {
                string actionDirectory = null;
                if (action.Reference.Type == Pipelines.ActionSourceType.Repository)
                {
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
                }
                else
                {
                    throw new NotSupportedException(action.Reference.Type.ToString());
                }

                Trace.Info($"Load action that reference repository from '{actionDirectory}'");
                definition.Directory = actionDirectory;

                string manifestFile = Path.Combine(actionDirectory, "action.yml");
                string dockerFile = Path.Combine(actionDirectory, "Dockerfile");
                if (File.Exists(manifestFile))
                {
                    using (var yamlInput = new StringReader(File.ReadAllText(manifestFile)))
                    {
                        var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().WithNamingConvention(new CamelCaseNamingConvention()).Build();
                        var actionDefinitionData = deserializer.Deserialize<ActionDefinitionData>(yamlInput);

                        definition.Data.FriendlyName = actionDefinitionData.Name;
                        Trace.Verbose($"Action friendly name: '{definition.Data.FriendlyName}'");

                        definition.Data.Description = actionDefinitionData.Description;
                        Trace.Verbose($"Action description: '{definition.Data.Description}'");

                        definition.Data.Author = actionDefinitionData.Author;
                        Trace.Verbose($"Action author: '{definition.Data.Author}'");

                        if (actionDefinitionData.Inputs != null)
                        {
                            List<TaskInputDefinition> inputs = new List<TaskInputDefinition>();
                            foreach (var input in actionDefinitionData.Inputs)
                            {
                                Trace.Verbose($"Action input: '{input.Key}' default to '{input.Value.Default}'");
                                inputs.Add(new TaskInputDefinition() { Name = input.Key, DefaultValue = input.Value.Default });
                            }

                            definition.Data.Inputs = inputs.ToArray();
                        }

                        if (actionDefinitionData.Outputs != null)
                        {
                            List<OutputVariable> outputs = new List<OutputVariable>();
                            foreach (var output in actionDefinitionData.Outputs)
                            {
                                Trace.Verbose($"Action output: '{output.Key}' for '{output.Value.Description}'");
                                outputs.Add(new OutputVariable() { Name = output.Key, Description = output.Value.Description });
                            }

                            definition.Data.OutputVariables = outputs.ToArray();
                        }

                        if (string.Equals(actionDefinitionData.Execution.ExecutionType, "docker", StringComparison.OrdinalIgnoreCase))
                        {
                            definition.Data.Execution.ContainerAction = new ContainerActionHandlerData
                            {
                                Target = actionDefinitionData.Execution.Image,
                                Arguments = actionDefinitionData.Execution.Arguments?.ToList(),
                                Environment = actionDefinitionData.Execution.Environment,
                                EntryPoint = actionDefinitionData.Execution.EntryPoint
                            };

                            Trace.Info($"Action container Dockerfile: {actionDefinitionData.Execution.Image}.");

                            if (actionDefinitionData.Execution.Arguments != null)
                            {
                                Trace.Info($"Action container args: [{string.Join(", ", actionDefinitionData.Execution.Arguments)}].");
                            }

                            if (actionDefinitionData.Execution.Environment != null)
                            {
                                Trace.Info($"Action container env: [{string.Join(", ", actionDefinitionData.Execution.Environment.Keys)}].");
                            }

                            if (CachedActionContainers.TryGetValue(action.Id, out var container))
                            {
                                definition.Data.Execution.ContainerAction.ContainerImage = container.ContainerImage;
                                Trace.Info($"Using action container image: {container.ContainerImage}.");
                            }
                        }
                        else if (string.Equals(actionDefinitionData.Execution.ExecutionType, "node", StringComparison.OrdinalIgnoreCase))
                        {
                            definition.Data.Execution.NodeAction = new NodeScriptActionHandlerData
                            {
                                Target = actionDefinitionData.Execution.Script
                            };

                            Trace.Info($"Action node.js file: {actionDefinitionData.Execution.Script}.");
                        }
                        else if (!string.IsNullOrEmpty(actionDefinitionData.Execution.Plugin))
                        {
                            var pluginManager = HostContext.GetService<IRunnerPluginManager>();
                            var plugin = pluginManager.GetPluginAction(actionDefinitionData.Execution.Plugin);

                            ArgUtil.NotNull(plugin, actionDefinitionData.Execution.Plugin);
                            ArgUtil.NotNullOrEmpty(plugin.PluginTypeName, actionDefinitionData.Execution.Plugin);

                            definition.Data.Execution.RunnerPlugin = new RunnerPluginHandlerData()
                            {
                                Target = plugin.PluginTypeName
                            };

                            Trace.Info($"Action plugin: {plugin.PluginTypeName}.");
                        }
                        else
                        {
                            throw new NotSupportedException(actionDefinitionData.Execution.ExecutionType);
                        }
                    }

                }
                else if (File.Exists(dockerFile))
                {
                    definition.Data.Execution.ContainerAction = new ContainerActionHandlerData
                    {
                        Target = "Dockerfile",
                    };

                    if (CachedActionContainers.TryGetValue(action.Id, out var container))
                    {
                        definition.Data.Execution.ContainerAction.ContainerImage = container.ContainerImage;
                    }
                }
                else
                {
                    throw new NotSupportedException($"'{actionDirectory}' doesn't contain a valid action entrypoint.");
                }
            }
            else if (action.Reference.Type == Pipelines.ActionSourceType.Script)
            {
                definition.Data.Execution.ScriptAction = new ScriptActionHandlerData();
                definition.Data.FriendlyName = "Run";
                definition.Data.Description = "Execute a script";
                definition.Data.Author = "GitHub";
            }
            else if (action.Reference.Type == Pipelines.ActionSourceType.AgentPlugin)
            {
                var pluginAction = action.Reference as Pipelines.PluginReference;
                var pluginManager = HostContext.GetService<IRunnerPluginManager>();
                var plugin = pluginManager.GetPluginAction(pluginAction.Plugin);

                ArgUtil.NotNull(plugin, pluginAction.Plugin);
                ArgUtil.NotNullOrEmpty(plugin.PluginTypeName, pluginAction.Plugin);

                definition.Data.Execution.RunnerPlugin = new RunnerPluginHandlerData()
                {
                    Target = plugin.PluginTypeName
                };

                definition.Data.FriendlyName = plugin.FriendlyName;
                definition.Data.Description = plugin.Description;
                definition.Data.Author = plugin.Author;
            }

            return definition;
        }
    }

    public sealed class Definition
    {
        public DefinitionData Data { get; set; }
        public string Directory { get; set; }
    }

    public sealed class DefinitionData
    {
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string HelpUrl { get; set; }
        public string Author { get; set; }
        public OutputVariable[] OutputVariables { get; set; }
        public TaskInputDefinition[] Inputs { get; set; }
        public ExecutionData Execution { get; set; }
    }

    public sealed class ActionDefinitionData
    {
        [YamlMember]
        public string Name { get; set; }

        [YamlMember]
        public string Description { get; set; }

        [YamlMember]
        public string Author { get; set; }

        [YamlMember]
        public Dictionary<string, ActionInputDefinition> Inputs { get; set; }

        [YamlMember]
        public Dictionary<string, ActionOutput> Outputs { get; set; }

        [YamlMember(Alias = "runs")]
        public ActionExecutionData Execution { get; set; }
    }


    public sealed class ActionInputDefinition
    {
        [YamlMember]
        public string Default { get; set; }
    }

    public sealed class ActionOutput
    {
        [YamlMember]
        public string Description { get; set; }
    }

    public sealed class ActionExecutionData
    {
        [YamlMember(Alias = "using")]
        public string ExecutionType { get; set; }

        [YamlMember]
        public string Image { get; set; }

        [YamlMember]
        public string Plugin { get; set; }

        [YamlMember(Alias = "main")]
        public string Script { get; set; }

        [YamlMember(Alias = "args")]
        public string[] Arguments { get; set; }

        [YamlMember(Alias = "entrypoint")]
        public string EntryPoint { get; set; }

        [YamlMember(Alias = "env")]
        public Dictionary<string, string> Environment { get; set; }
    }

    public sealed class OutputVariable
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class ExecutionData
    {
        private readonly List<HandlerData> _all = new List<HandlerData>();
        private ContainerActionHandlerData _containerAction;
        private NodeScriptActionHandlerData _nodeScriptAction;
        private ScriptActionHandlerData _scriptAction;
        private RunnerPluginHandlerData _runnerPlugin;

        [JsonIgnore]
        public List<HandlerData> All => _all;

        public ContainerActionHandlerData ContainerAction
        {
            get
            {
                return _containerAction;
            }

            set
            {
                _containerAction = value;
                Add(value);
            }
        }

        public NodeScriptActionHandlerData NodeAction
        {
            get
            {
                return _nodeScriptAction;
            }

            set
            {
                _nodeScriptAction = value;
                Add(value);
            }
        }

        public ScriptActionHandlerData ScriptAction
        {
            get
            {
                return _scriptAction;
            }

            set
            {
                _scriptAction = value;
                Add(value);
            }
        }

        public RunnerPluginHandlerData RunnerPlugin
        {
            get
            {
                return _runnerPlugin;
            }

            set
            {
                _runnerPlugin = value;
                Add(value);
            }
        }

        private void Add(HandlerData data)
        {
            if (data != null)
            {
                _all.Add(data);
            }
        }
    }

    public abstract class HandlerData
    {
        public Dictionary<string, string> Inputs { get; }

        public string[] Platforms { get; set; }

        [JsonIgnore]
        public abstract int Priority { get; }

        public string Target
        {
            get
            {
                return GetInput(nameof(Target));
            }

            set
            {
                SetInput(nameof(Target), value);
            }
        }

        public HandlerData()
        {
            Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        protected string GetInput(string name)
        {
            string value;
            if (Inputs.TryGetValue(name, out value))
            {
                return value ?? string.Empty;
            }

            return string.Empty;
        }

        protected void SetInput(string name, string value)
        {
            Inputs[name] = value;
        }
    }

    public sealed class ContainerActionHandlerData : HandlerData
    {
        public override int Priority => 3;

        public string ContainerImage
        {
            get
            {
                return GetInput(nameof(ContainerImage));
            }

            set
            {
                SetInput(nameof(ContainerImage), value);
            }
        }

        public string EntryPoint
        {
            get
            {
                return GetInput(nameof(EntryPoint));
            }

            set
            {
                SetInput(nameof(EntryPoint), value);
            }
        }

        public List<string> Arguments { get; set; }
        public Dictionary<string, string> Environment { get; set; }
    }

    public sealed class NodeScriptActionHandlerData : HandlerData
    {
        public override int Priority => 2;

        public string WorkingDirectory
        {
            get
            {
                return GetInput(nameof(WorkingDirectory));
            }

            set
            {
                SetInput(nameof(WorkingDirectory), value);
            }
        }
    }

    public sealed class RunnerPluginHandlerData : HandlerData
    {
        public override int Priority => 0;
    }

    public sealed class ScriptActionHandlerData : HandlerData
    {
        public override int Priority => 1;
    }
}
