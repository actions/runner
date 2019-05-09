using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using System.Net.Http;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskManager))]
    public interface ITaskManager : IAgentService
    {
        Dictionary<Guid, ContainerInfo> CachedActionContainers { get; }
        Task DownloadAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps);

        Definition Load(Pipelines.TaskStep task);

        Definition LoadAction(IExecutionContext executionContext, Pipelines.ActionStep action);
    }

    public sealed class TaskManager : AgentService, ITaskManager
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

            executionContext.Output(StringUtil.Loc("EnsureTasksExist"));

            IEnumerable<Pipelines.TaskStep> tasks = steps.OfType<Pipelines.TaskStep>();
            IEnumerable<Pipelines.ActionStep> actions = steps.OfType<Pipelines.ActionStep>();

            //remove duplicate, disabled and built-in tasks
            IEnumerable<Pipelines.TaskStep> uniqueTasks =
                from task in tasks
                group task by new
                {
                    task.Reference.Id,
                    task.Reference.Version
                }
                into taskGrouping
                select taskGrouping.First();

            List<Pipelines.ContainerResource> actionContainers = new List<Pipelines.ContainerResource>();
            foreach (var containerAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry))
            {
                var container = containerAction.Reference as Pipelines.ContainerRegistryActionDefinitionReference;
                actionContainers.Add(executionContext.Containers.Single(x => x.Alias == container.Container));
            }

            List<Pipelines.RepositoryResource> actionRepositories = new List<Pipelines.RepositoryResource>();
            foreach (var repositoryAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.Repository))
            {
                var repository = repositoryAction.Reference as Pipelines.RepositoryActionDefinitionReference;
                actionRepositories.Add(executionContext.Repositories.Single(x => x.Alias == repository.Repository));
            }

            if (uniqueTasks.Count() == 0 && actionContainers.Count() == 0 && actionRepositories.Count() == 0)
            {
                executionContext.Debug("There is no required tasks/actions need to download.");
                return;
            }

            foreach (var task in uniqueTasks.Select(x => x.Reference))
            {
                if (task.Id == Pipelines.PipelineConstants.CheckoutTask.Id && task.Version == Pipelines.PipelineConstants.CheckoutTask.Version)
                {
                    Trace.Info("Skip download checkout task.");
                    continue;
                }

                if (task.Id == Pipelines.PipelineConstants.RunTask.Id && task.Version == Pipelines.PipelineConstants.RunTask.Version)
                {
                    Trace.Info("Skip download run task.");
                    continue;
                }

                await DownloadAsync(executionContext, task);
            }

            foreach (var containerAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry))
            {
                var container = containerAction.Reference as Pipelines.ContainerRegistryActionDefinitionReference;
                var containerResource = actionContainers.Single(x => x.Alias == container.Container);
                await DownloadContainerRegistryActionAsync(executionContext, containerResource, containerAction);
            }

            foreach (var repositoryAction in actions.Where(x => x.Reference.Type == Pipelines.ActionSourceType.Repository))
            {
                var repository = repositoryAction.Reference as Pipelines.RepositoryActionDefinitionReference;
                var repositoryResource = actionRepositories.Single(x => x.Alias == repository.Repository);
                await DownloadRepositoryActionAsync(executionContext, repositoryResource, repositoryAction);
            }
        }

        private async Task DownloadContainerRegistryActionAsync(IExecutionContext executionContext, Pipelines.ContainerResource containerResource, Pipelines.ActionStep containerAction)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            ArgUtil.NotNull(containerResource, nameof(containerResource));
            ArgUtil.NotNullOrEmpty(containerResource.Image, nameof(containerResource.Image));

            executionContext.Output($"Pull down action image '{containerResource.Image}'");

            // Pull down docker image with retry up to 3 times
            var dockerManger = HostContext.GetService<IDockerCommandManager>();
            int retryCount = 0;
            int pullExitCode = 0;
            while (retryCount < 3)
            {
                pullExitCode = await dockerManger.DockerPull(executionContext, containerResource.Image);
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

            CachedActionContainers[containerAction.Id] = new ContainerInfo() { ContainerImage = containerResource.Image };
        }

        private async Task DownloadRepositoryActionAsync(IExecutionContext executionContext, Pipelines.RepositoryResource repositoryResource, Pipelines.ActionStep repositoryAction)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            ArgUtil.NotNull(repositoryResource, nameof(repositoryResource));
            ArgUtil.NotNullOrEmpty(repositoryResource.Alias, nameof(repositoryResource.Alias));

            if (string.Equals(repositoryResource.Alias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                Trace.Info($"Repository action is in 'self' repository.");
                return;
            }

            ArgUtil.NotNullOrEmpty(repositoryResource.Id, nameof(repositoryResource.Id));
            ArgUtil.NotNullOrEmpty(repositoryResource.Version, nameof(repositoryResource.Version));

#if OS_WINDOWS
            string archiveLink = $"https://api.github.com/repos/{repositoryResource.Id}/zipball/{repositoryResource.Version}";
#else
            string archiveLink = $"https://api.github.com/repos/{repositoryResource.Id}/tarball/{repositoryResource.Version}";
#endif

            string destDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), repositoryResource.Id.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), repositoryResource.Version);
            Trace.Info($"Download archive '{archiveLink}' to '{destDirectory}'.");
            if (File.Exists(destDirectory + ".completed"))
            {
                executionContext.Debug($"Action '{repositoryResource.Id}@{repositoryResource.Version}' already downloaded at '{destDirectory}'.");
                return;
            }
            else
            {
                // make sure we get an clean folder ready to use.
                IOUtil.DeleteDirectory(destDirectory, executionContext.CancellationToken);
                Directory.CreateDirectory(destDirectory);
            }

            //download and extract task in a temp folder and rename it on success
            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), "_temp_" + Guid.NewGuid());
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

                // Allow up to 20 * 60s for any task to be downloaded from service. 
                // Base on Kusto, the longest we have on the service today is over 850 seconds.
                // Timeout limit can be overwrite by environment variable
                if (!int.TryParse(Environment.GetEnvironmentVariable("VSTS_TASK_DOWNLOAD_TIMEOUT") ?? string.Empty, out int timeoutSeconds))
                {
                    timeoutSeconds = 20 * 60;
                }

                while (retryCount < 3)
                {
                    using (var taskDownloadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    using (var taskDownloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(taskDownloadTimeout.Token, executionContext.CancellationToken))
                    {
                        try
                        {
                            //open zip stream in async mode
                            using (FileStream fs = new FileStream(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _defaultFileStreamBufferSize, useAsync: true))
                            using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                            using (var httpClient = new HttpClient(httpClientHandler))
                            {
                                httpClient.DefaultRequestHeaders.UserAgent.Add(HostContext.UserAgent);
                                using (var result = await httpClient.GetStreamAsync(archiveLink))
                                {
                                    await result.CopyToAsync(fs, _defaultCopyBufferSize, taskDownloadCancellation.Token);
                                    await fs.FlushAsync(taskDownloadCancellation.Token);

                                    // download succeed, break out the retry loop.
                                    break;
                                }
                            }
                        }
                        catch (OperationCanceledException) when (executionContext.CancellationToken.IsCancellationRequested)
                        {
                            Trace.Info($"Task download has been cancelled.");
                            throw;
                        }
                        catch (Exception ex) when (retryCount < 2)
                        {
                            retryCount++;
                            Trace.Error($"Fail to download archive '{archiveLink}' -- Attempt: {retryCount}");
                            Trace.Error(ex);
                            if (taskDownloadTimeout.Token.IsCancellationRequested)
                            {
                                // task download didn't finish within timeout
                                executionContext.Warning(StringUtil.Loc("TaskDownloadTimeout", archiveLink, timeoutSeconds));
                            }
                            else
                            {
                                executionContext.Warning(StringUtil.Loc("TaskDownloadFailed", archiveLink, ex.Message));
                            }
                        }
                    }

                    if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSTS_TASK_DOWNLOAD_NO_BACKOFF")))
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

                Trace.Verbose("Create watermark file indicate task download succeed.");
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
                        Trace.Verbose("Deleting task temp folder: {0}", tempDirectory);
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
            var repoReference = repositoryAction.Reference as Pipelines.RepositoryActionDefinitionReference;
            ArgUtil.NotNull(repoReference, nameof(repoReference));
            if (!string.IsNullOrEmpty(repoReference.Path))
            {
                actionEntryDirectory = Path.Combine(destDirectory, repoReference.Path);
            }

            // find the docker file
            string dockerFile = Path.Combine(actionEntryDirectory, "Dockerfile");
            if (File.Exists(dockerFile))
            {
                executionContext.Output($"Dockerfile for action: '{dockerFile}'.");

                var dockerManger = HostContext.GetService<IDockerCommandManager>();
                var imageName = $"{dockerManger.DockerInstanceLabel}:{Guid.NewGuid().ToString("N")}";
                var buildExitCode = await dockerManger.DockerBuild(executionContext, Directory.GetParent(dockerFile).FullName, imageName);
                if (buildExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker build failed with exit code {buildExitCode}");
                }

                CachedActionContainers[repositoryAction.Id] = new ContainerInfo() { ContainerImage = imageName };
            }
            else
            {
                var taskJson = Path.Combine(actionEntryDirectory, "task.json");
                var actionManifest = Path.Combine(actionEntryDirectory, "manifest.yml");
                if (File.Exists(taskJson))
                {
                    executionContext.Output($"task.json for action: '{taskJson}'.");
                }
                else if (File.Exists(actionManifest))
                {
                    executionContext.Output($"manifest.yml for action: '{actionManifest}'.");
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
            else
            {
                string actionDirectory = null;
                if (action.Reference.Type == Pipelines.ActionSourceType.Repository)
                {
                    var repoAction = action.Reference as Pipelines.RepositoryActionDefinitionReference;
                    if (string.Equals(repoAction.Repository, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
                    {
                        var selfRepo = executionContext.Repositories.Single(x => string.Equals(x.Alias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase));
                        actionDirectory = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                        if (!string.IsNullOrEmpty(repoAction.Path))
                        {
                            actionDirectory = Path.Combine(actionDirectory, repoAction.Path);
                        }
                    }
                    else
                    {
                        var repo = executionContext.Repositories.Single(x => string.Equals(x.Alias, repoAction.Repository, StringComparison.OrdinalIgnoreCase));
                        actionDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), repo.Id.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), repo.Version);
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

                string manifestFile = Path.Combine(actionDirectory, "manifest.yml");
                string jsontFile = Path.Combine(actionDirectory, Constants.Path.TaskJsonFile);
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
                        else
                        {
                            throw new NotSupportedException(actionDefinitionData.Execution.ExecutionType);
                        }
                    }

                }
                else if (File.Exists(jsontFile))
                {
                    // Deserialize the JSON.
                    Trace.Info($"Loading task definition '{jsontFile}'.");
                    string json = File.ReadAllText(jsontFile);
                    definition.Data = JsonConvert.DeserializeObject<DefinitionData>(json);

                    // Replace the macros within the handler data sections.
                    foreach (HandlerData handlerData in (definition.Data?.Execution?.All as IEnumerable<HandlerData> ?? new HandlerData[0]))
                    {
                        handlerData?.ReplaceMacros(HostContext, definition);
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

            return definition;
        }

        public Definition Load(Pipelines.TaskStep task)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(task, nameof(task));

            if (task.Reference.Id == Pipelines.PipelineConstants.CheckoutTask.Id && task.Reference.Version == Pipelines.PipelineConstants.CheckoutTask.Version)
            {
                var checkoutTask = new Definition()
                {
                    Directory = HostContext.GetDirectory(WellKnownDirectory.Tasks),
                    Data = new DefinitionData()
                    {
                        Author = Pipelines.PipelineConstants.CheckoutTask.Author,
                        Description = Pipelines.PipelineConstants.CheckoutTask.Description,
                        FriendlyName = Pipelines.PipelineConstants.CheckoutTask.FriendlyName,
                        HelpMarkDown = Pipelines.PipelineConstants.CheckoutTask.HelpMarkDown,
                        Inputs = Pipelines.PipelineConstants.CheckoutTask.Inputs.ToArray(),
                        Execution = StringUtil.ConvertFromJson<ExecutionData>(StringUtil.ConvertToJson(Pipelines.PipelineConstants.CheckoutTask.Execution)),
                        PostJobExecution = StringUtil.ConvertFromJson<ExecutionData>(StringUtil.ConvertToJson(Pipelines.PipelineConstants.CheckoutTask.PostJobExecution))
                    }
                };

                return checkoutTask;
            }

            if (task.Reference.Id == Pipelines.PipelineConstants.RunTask.Id && task.Reference.Version == Pipelines.PipelineConstants.RunTask.Version)
            {
                var runTask = new Definition()
                {
                    Directory = HostContext.GetDirectory(WellKnownDirectory.Tasks),
                    Data = new DefinitionData()
                    {
                        Author = Pipelines.PipelineConstants.RunTask.Author,
                        Description = Pipelines.PipelineConstants.RunTask.Description,
                        FriendlyName = Pipelines.PipelineConstants.RunTask.FriendlyName,
                        HelpMarkDown = Pipelines.PipelineConstants.RunTask.HelpMarkDown,
                        Inputs = Pipelines.PipelineConstants.RunTask.Inputs.ToArray(),
                        Execution = StringUtil.ConvertFromJson<ExecutionData>(StringUtil.ConvertToJson(Pipelines.PipelineConstants.RunTask.Execution)),
                    }
                };

                return runTask;
            }

            // if (task.Reference.Id == new Guid("22f9b24a-0e55-484c-870e-1a0041f0167e"))
            // {
            //     var containerTask = new Definition()
            //     {
            //         Directory = HostContext.GetDirectory(WellKnownDirectory.Tasks),
            //         Data = new DefinitionData()
            //         {
            //             Author = "Microsoft",
            //             Description = "Container",
            //             FriendlyName = "Container",
            //             HelpMarkDown = "Container",
            //             Inputs = new TaskInputDefinition[]{
            //                 new TaskInputDefinition()
            //                 {
            //                     Name =  "container",
            //                     Required = true,
            //                     InputType = TaskInputType.String
            //                 },
            //                 new TaskInputDefinition()
            //                 {
            //                     Name =  "runs",
            //                     Required = false,
            //                     InputType = TaskInputType.String
            //                 },
            //                 new TaskInputDefinition()
            //                 {
            //                     Name =  "args",
            //                     Required = false,
            //                     InputType = TaskInputType.String
            //                 },
            //             },
            //             Execution = StringUtil.ConvertFromJson<ExecutionData>(StringUtil.ConvertToJson(
            //                 new Dictionary<string, JObject>()
            //                 {
            //                     {
            //                         "agentPlugin",
            //                         JObject.FromObject(new Dictionary<String, String>(){ { "target", "Agent.Plugins.Container.ContainerActionTask, Agent.Plugins"} })
            //                     }
            //                 }
            //             )),
            //         }
            //     };

            //     return containerTask;
            // }

            // Initialize the definition wrapper object.
            var definition = new Definition() { Directory = GetDirectory(task.Reference) };

            // Deserialize the JSON.
            string file = Path.Combine(definition.Directory, Constants.Path.TaskJsonFile);
            Trace.Info($"Loading task definition '{file}'.");
            string json = File.ReadAllText(file);
            definition.Data = JsonConvert.DeserializeObject<DefinitionData>(json);

            // Replace the macros within the handler data sections.
            foreach (HandlerData handlerData in (definition.Data?.Execution?.All as IEnumerable<HandlerData> ?? new HandlerData[0]))
            {
                handlerData?.ReplaceMacros(HostContext, definition);
            }

            return definition;
        }

        private async Task DownloadAsync(IExecutionContext executionContext, Pipelines.TaskStepDefinitionReference task)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(task, nameof(task));
            ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));
            var taskServer = HostContext.GetService<ITaskServer>();

            // first check to see if we already have the task
            string destDirectory = GetDirectory(task);
            Trace.Info($"Ensuring task exists: ID '{task.Id}', version '{task.Version}', name '{task.Name}', directory '{destDirectory}'.");
            if (File.Exists(destDirectory + ".completed"))
            {
                executionContext.Debug($"Task '{task.Name}' already downloaded at '{destDirectory}'.");
                return;
            }

            // delete existing task folder.
            Trace.Verbose("Deleting task destination folder: {0}", destDirectory);
            IOUtil.DeleteDirectory(destDirectory, CancellationToken.None);

            // Inform the user that a download is taking place. The download could take a while if
            // the task zip is large. It would be nice to print the localized name, but it is not
            // available from the reference included in the job message.
            executionContext.Output(StringUtil.Loc("DownloadingTask0", task.Name));
            string zipFile = string.Empty;
            var version = new TaskVersion(task.Version);

            //download and extract task in a temp folder and rename it on success
            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), "_temp_" + Guid.NewGuid());
            try
            {
                Directory.CreateDirectory(tempDirectory);
                int retryCount = 0;

                // Allow up to 20 * 60s for any task to be downloaded from service. 
                // Base on Kusto, the longest we have on the service today is over 850 seconds.
                // Timeout limit can be overwrite by environment variable
                if (!int.TryParse(Environment.GetEnvironmentVariable("VSTS_TASK_DOWNLOAD_TIMEOUT") ?? string.Empty, out int timeoutSeconds))
                {
                    timeoutSeconds = 20 * 60;
                }

                while (retryCount < 3)
                {
                    using (var taskDownloadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    using (var taskDownloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(taskDownloadTimeout.Token, executionContext.CancellationToken))
                    {
                        try
                        {
                            zipFile = Path.Combine(tempDirectory, string.Format("{0}.zip", Guid.NewGuid()));

                            //open zip stream in async mode
                            using (FileStream fs = new FileStream(zipFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _defaultFileStreamBufferSize, useAsync: true))
                            using (Stream result = await taskServer.GetTaskContentZipAsync(task.Id, version, taskDownloadCancellation.Token))
                            {
                                await result.CopyToAsync(fs, _defaultCopyBufferSize, taskDownloadCancellation.Token);
                                await fs.FlushAsync(taskDownloadCancellation.Token);

                                // download succeed, break out the retry loop.
                                break;
                            }
                        }
                        catch (OperationCanceledException) when (executionContext.CancellationToken.IsCancellationRequested)
                        {
                            Trace.Info($"Task download has been cancelled.");
                            throw;
                        }
                        catch (Exception ex) when (retryCount < 2)
                        {
                            retryCount++;
                            Trace.Error($"Fail to download task '{task.Id} ({task.Name}/{task.Version})' -- Attempt: {retryCount}");
                            Trace.Error(ex);
                            if (taskDownloadTimeout.Token.IsCancellationRequested)
                            {
                                // task download didn't finish within timeout
                                executionContext.Warning(StringUtil.Loc("TaskDownloadTimeout", task.Name, timeoutSeconds));
                            }
                            else
                            {
                                executionContext.Warning(StringUtil.Loc("TaskDownloadFailed", task.Name, ex.Message));
                            }
                        }
                    }

                    if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSTS_TASK_DOWNLOAD_NO_BACKOFF")))
                    {
                        var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                        executionContext.Warning($"Back off {backOff.TotalSeconds} seconds before retry.");
                        await Task.Delay(backOff);
                    }
                }

                Directory.CreateDirectory(destDirectory);
                ZipFile.ExtractToDirectory(zipFile, destDirectory);

                Trace.Verbose("Create watermark file indicate task download succeed.");
                File.WriteAllText(destDirectory + ".completed", DateTime.UtcNow.ToString());

                executionContext.Debug($"Task '{task.Name}' has been downloaded into '{destDirectory}'.");
                Trace.Info("Finished getting task.");
            }
            finally
            {
                try
                {
                    //if the temp folder wasn't moved -> wipe it
                    if (Directory.Exists(tempDirectory))
                    {
                        Trace.Verbose("Deleting task temp folder: {0}", tempDirectory);
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
        }

        private string GetDirectory(Pipelines.TaskStepDefinitionReference task)
        {
            ArgUtil.NotEmpty(task.Id, nameof(task.Id));
            ArgUtil.NotNull(task.Name, nameof(task.Name));
            ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));
            return Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Tasks),
                $"{task.Name}_{task.Id}",
                task.Version);
        }
    }

    public enum ActionType
    {
        Container,
        NodeScript
    }

    public sealed class ActionDefinition
    {
        public ActionDefinitionData Data { get; set; }
        public string Directory { get; set; }
        public ActionType Type { get; set; }
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
        public string HelpMarkDown { get; set; }
        public string HelpUrl { get; set; }
        public string Author { get; set; }
        public OutputVariable[] OutputVariables { get; set; }
        public TaskInputDefinition[] Inputs { get; set; }
        public ExecutionData PreJobExecution { get; set; }
        public ExecutionData Execution { get; set; }
        public ExecutionData PostJobExecution { get; set; }
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
        private AzurePowerShellHandlerData _azurePowerShell;
        private ContainerActionHandlerData _containerAction;
        private NodeScriptActionHandlerData _nodeScriptAction;
        private ShellHandlerData _shell;
        private NodeHandlerData _node;
        private Node10HandlerData _node10;
        private PowerShellHandlerData _powerShell;
        private PowerShell3HandlerData _powerShell3;
        private PowerShellExeHandlerData _powerShellExe;
        private ProcessHandlerData _process;
        private AgentPluginHandlerData _agentPlugin;

        [JsonIgnore]
        public List<HandlerData> All => _all;

#if !OS_WINDOWS || X86
        [JsonIgnore]
#endif
        public AzurePowerShellHandlerData AzurePowerShell
        {
            get
            {
                return _azurePowerShell;
            }

            set
            {
                _azurePowerShell = value;
                Add(value);
            }
        }

        public NodeHandlerData Node
        {
            get
            {
                return _node;
            }

            set
            {
                _node = value;
                Add(value);
            }
        }

        public Node10HandlerData Node10
        {
            get
            {
                return _node10;
            }

            set
            {
                _node10 = value;
                Add(value);
            }
        }

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

        public ShellHandlerData Shell
        {
            get
            {
                return _shell;
            }

            set
            {
                _shell = value;
                Add(value);
            }
        }

#if !OS_WINDOWS || X86
        [JsonIgnore]
#endif
        public PowerShellHandlerData PowerShell
        {
            get
            {
                return _powerShell;
            }

            set
            {
                _powerShell = value;
                Add(value);
            }
        }

#if !OS_WINDOWS
        [JsonIgnore]
#endif
        public PowerShell3HandlerData PowerShell3
        {
            get
            {
                return _powerShell3;
            }

            set
            {
                _powerShell3 = value;
                Add(value);
            }
        }

#if !OS_WINDOWS
        [JsonIgnore]
#endif
        public PowerShellExeHandlerData PowerShellExe
        {
            get
            {
                return _powerShellExe;
            }

            set
            {
                _powerShellExe = value;
                Add(value);
            }
        }

#if !OS_WINDOWS
        [JsonIgnore]
#endif
        public ProcessHandlerData Process
        {
            get
            {
                return _process;
            }

            set
            {
                _process = value;
                Add(value);
            }
        }

        public AgentPluginHandlerData AgentPlugin
        {
            get
            {
                return _agentPlugin;
            }

            set
            {
                _agentPlugin = value;
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

        public bool PreferredOnCurrentPlatform()
        {
#if OS_WINDOWS
            const string CurrentPlatform = "windows";
            return Platforms?.Any(x => string.Equals(x, CurrentPlatform, StringComparison.OrdinalIgnoreCase)) ?? false;
#else
            return false;
#endif
        }

        public void ReplaceMacros(IHostContext context, Definition definition)
        {
            var handlerVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            handlerVariables["currentdirectory"] = definition.Directory;
            VarUtil.ExpandValues(context, source: handlerVariables, target: Inputs);
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

    public abstract class BaseNodeHandlerData : HandlerData
    {
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

    public sealed class NodeHandlerData : BaseNodeHandlerData
    {
        public override int Priority => 2;
    }

    public sealed class Node10HandlerData : BaseNodeHandlerData
    {
        public override int Priority => 1;
    }

    public sealed class PowerShell3HandlerData : HandlerData
    {
        public override int Priority => 3;
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
        public override int Priority => 3;

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

    public sealed class PowerShellHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public override int Priority => 4;

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

    public sealed class AzurePowerShellHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public override int Priority => 5;

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

    public sealed class PowerShellExeHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public string FailOnStandardError
        {
            get
            {
                return GetInput(nameof(FailOnStandardError));
            }

            set
            {
                SetInput(nameof(FailOnStandardError), value);
            }
        }

        public string InlineScript
        {
            get
            {
                return GetInput(nameof(InlineScript));
            }

            set
            {
                SetInput(nameof(InlineScript), value);
            }
        }

        public override int Priority => 5;

        public string ScriptType
        {
            get
            {
                return GetInput(nameof(ScriptType));
            }

            set
            {
                SetInput(nameof(ScriptType), value);
            }
        }

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

    public sealed class ProcessHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public string ModifyEnvironment
        {
            get
            {
                return GetInput(nameof(ModifyEnvironment));
            }

            set
            {
                SetInput(nameof(ModifyEnvironment), value);
            }
        }

        public override int Priority => 6;

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

    public sealed class AgentPluginHandlerData : HandlerData
    {
        public override int Priority => 0;
    }

    public sealed class ShellHandlerData : HandlerData
    {
        public override int Priority => 6;
    }
}
