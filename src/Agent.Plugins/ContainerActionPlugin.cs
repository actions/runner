using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Agent.Sdk;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json.Linq;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using System.IO;
using Microsoft.TeamFoundation.Framework.Common;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Common;
using Agent.Plugins.Repository;
using Newtonsoft.Json;

namespace Agent.Plugins.Container
{
    public class ContainerActionTask : IAgentTaskPlugin
    {

        public Guid Id => new Guid("22f9b24a-0e55-484c-870e-1a0041f0167e");
        public string Version => "1.0.0";

        public string Stage => "main";

        public async Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token)
        {
            DockerCommandManager dockerManger = new DockerCommandManager(executionContext);

            // Build/pull container
            var uses = executionContext.GetInput("container", true);
            var runs = executionContext.GetInput("runs");
            var args = executionContext.GetInput("args");

            GetDockerCommand(runs, args, out string entryPoint, out string command);
            executionContext.Debug($"Container ENTRYPOINT override: '{entryPoint}'");
            executionContext.Debug($"Container CMD override: '{command}'");

            Pipelines.ContainerResource containerReference = new Pipelines.ContainerResource() { Alias = "action" };
            containerReference.Properties.Set("entryPoint", entryPoint);
            containerReference.Properties.Set("command", command);

            var workFolder = executionContext.Variables.GetValueOrDefault("agent.WorkFolder")?.Value;
            ArgUtil.NotNullOrEmpty(workFolder, nameof(workFolder));

            var tempDirectory = executionContext.Variables.GetValueOrDefault("agent.tempdirectory")?.Value;
            ArgUtil.NotNullOrEmpty(tempDirectory, nameof(tempDirectory));

            var selfRepo = executionContext.Repositories.Single(x => string.Equals(x.Alias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase));

            if (uses.StartsWith("docker://", StringComparison.OrdinalIgnoreCase))
            {
                // image already build, only need docker pull
                containerReference.Image = uses.Substring("docker://".Length);

                // Pull down docker image with retry up to 3 times
                int retryCount = 0;
                int pullExitCode = 0;
                while (retryCount < 3)
                {
                    pullExitCode = await dockerManger.DockerPull(executionContext, containerReference.Image, token);
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
            }
            else
            {
                // find the docker file
                string dockerFile;
                if (uses.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).StartsWith("./"))
                {
                    // the docker file is in local repository
                    var repoPath = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                    dockerFile = Path.Combine(repoPath, uses, "Dockerfile");
                }
                else
                {
                    // docker file in a github repository
                    var usesSplit = uses.Split('@', StringSplitOptions.RemoveEmptyEntries);
                    ArgUtil.NotNullOrEmpty(usesSplit[0], "repo");
                    ArgUtil.NotNullOrEmpty(usesSplit[1], "ref");

                    var repoSplit = usesSplit[0].Split('/', StringSplitOptions.RemoveEmptyEntries);
                    string githubRepoUrl = $"https://github.com/{repoSplit[0]}/{repoSplit[1]}";
                    string versionRef = usesSplit[1];
                    string dockerFileFolder = string.Empty;
                    if (repoSplit.Length > 2)
                    {
                        dockerFileFolder = string.Join(Path.DirectorySeparatorChar, repoSplit.TakeLast(repoSplit.Length - 2));
                    }

                    var actionRepository = new Pipelines.RepositoryResource();
                    actionRepository.Alias = "__action";
                    actionRepository.Id = githubRepoUrl;
                    actionRepository.Url = new Uri(githubRepoUrl);
                    actionRepository.Version = versionRef;
                    actionRepository.Type = Pipelines.RepositoryTypes.GitHub;

                    var actionRepositoryPath = Path.Combine(tempDirectory, IOUtil.GetPathHash(githubRepoUrl).Substring(0, 5));
                    actionRepository.Properties.Set(Pipelines.RepositoryPropertyNames.Path, actionRepositoryPath);

                    var sourceProvider = new GitHubSourceProvider();
                    await sourceProvider.GetSourceAsync(executionContext, actionRepository, token);

                    dockerFile = Path.Combine(actionRepositoryPath, dockerFileFolder, "Dockerfile");
                }

                executionContext.Output($"Dockerfile for action: '{dockerFile}'.");

                containerReference.Image = $"{dockerManger.DockerInstanceLabel}:{Guid.NewGuid().ToString("N")}";

                var buildExitCode = await dockerManger.DockerBuild(executionContext, Directory.GetParent(dockerFile).FullName, containerReference.Image, token);
                if (buildExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker build failed with exit code {buildExitCode}");
                }
            }

            // run container
            var container = new ContainerInfo(executionContext, containerReference);

            // populate action environment variables.
            // GITHUB_ACTOR=ericsciple
            container.ContainerEnvironmentVariables["GITHUB_ACTOR"] = selfRepo.Properties.Get<Pipelines.VersionInfo>(Pipelines.RepositoryPropertyNames.VersionInfo)?.Author ?? string.Empty;

            // GITHUB_REPOSITORY=bryanmacfarlane/actionstest
            container.ContainerEnvironmentVariables["GITHUB_REPOSITORY"] = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name, string.Empty);

            // GITHUB_WORKSPACE=/github/workspace
            container.ContainerEnvironmentVariables["GITHUB_WORKSPACE"] = "/github/workspace"; //selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path, string.Empty);

            // GITHUB_SHA=1a204f473f6001b7fac9c6453e76702f689a41a9
            container.ContainerEnvironmentVariables["GITHUB_SHA"] = selfRepo.Version;

            // GITHUB_REF=refs/heads/master
            container.ContainerEnvironmentVariables["GITHUB_REF"] = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Ref, string.Empty);

            // GITHUB_TOKEN=TOKEN
            // var repoEndpoint = executionContext.Endpoints.FirstOrDefault(x => x.Id == selfRepo.Endpoint.Id);
            // container.ContainerEnvironmentVariables["GITHUB_TOKEN"] = repoEndpoint.Authorization.Parameters["accessToken"];

            // HOME=/github/home
            container.ContainerEnvironmentVariables["HOME"] = "/github/home";

            // GITHUB_WORKFLOW=test on push
            container.ContainerEnvironmentVariables["GITHUB_WORKFLOW"] = executionContext.Variables["Build.DefinitionName"]?.Value ?? string.Empty;

            // GITHUB_ACTION=dump.env
            // GITHUB_EVENT_NAME=push
            // GITHUB_EVENT_PATH=/github/workflow/event.json

            // foreach (var variable in executionContext.Environment)
            // {
            //     container.ContainerEnvironmentVariables[variable.Key] = container.TranslateToContainerPath(variable.Value);
            // }

            var runExitCode = await dockerManger.DockerRun(executionContext, container, token);
            if (runExitCode != 0)
            {
                throw new InvalidOperationException($"Docker run failed with exit code {runExitCode}");
            }
        }

        // action supports both runs and args to pass in as string or array
        // ex: "echo helloworld" or ["echo", "helloworld"]
        // the `runs` will overwrite the ENTRYPOINT with its first segment
        // the rest of `runs` will concat with `args` and pass to docker run as command
        private void GetDockerCommand(string runs, string args, out string entryPoint, out string command)
        {
            entryPoint = string.Empty;
            command = string.Empty;

            runs = runs.Trim();
            args = args.Trim();
            if (!string.IsNullOrEmpty(runs))
            {
                if (runs.StartsWith('[') && runs.EndsWith(']'))
                {
                    // ["echo", "helloworld"] => "echo" is entrypoint and "helloworld" is command
                    try
                    {
                        var runsSegments = StringUtil.ConvertFromJson<List<string>>(runs);
                        if (runsSegments.Count > 0)
                        {
                            entryPoint = runsSegments[0];
                            for (int index = 1; index < runsSegments.Count; index++)
                            {
                                command = $"{command} \"{runsSegments[index].Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException($"Input '{runs}' is not a valid JSON array. {ex}");
                    }
                }
                else
                {
                    // "echo helloworld" = > "echo" is entrypoint and "helloworld" is command
                    var firstSegmentEndIndex = runs.IndexOf(' ');
                    if (firstSegmentEndIndex > 0)
                    {
                        entryPoint = runs.Substring(0, firstSegmentEndIndex);
                        command = $"{command} {runs.Substring(firstSegmentEndIndex)}";
                    }
                    else
                    {
                        entryPoint = runs;
                    }
                }
            }

            if (!string.IsNullOrEmpty(args))
            {
                if (args.StartsWith('[') && args.EndsWith(']'))
                {
                    // ["hello", "world"]
                    try
                    {
                        var argsSegments = StringUtil.ConvertFromJson<List<string>>(args);
                        if (argsSegments.Count > 0)
                        {
                            command = $"{command} {string.Join(' ', argsSegments.Select(x => $"\"{x.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""))}";
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException($"Input '{args}' is not a valid JSON array. {ex}");
                    }
                }
                else
                {
                    // "hello world"
                    command = $"{command} {args}";
                }
            }

            entryPoint = entryPoint.Trim();
            command = command.Trim();
        }
    }

    public class DockerCommandManager
    {
        public string DockerPath { get; private set; }

        public string DockerInstanceLabel { get; private set; }

        public DockerCommandManager(AgentTaskPluginExecutionContext context)
        {
            DockerPath = WhichUtil.Which("docker", true);
            DockerInstanceLabel = IOUtil.GetPathHash(context.Variables.GetValueOrDefault("agent.RootDirectory")?.Value).Substring(0, 6);
        }

        public async Task<int> DockerPull(AgentTaskPluginExecutionContext context, string image, CancellationToken token)
        {
            return await ExecuteDockerCommandAsync(context, "pull", image, token);
        }

        public async Task<int> DockerBuild(AgentTaskPluginExecutionContext context, string path, string tag, CancellationToken token)
        {
            return await ExecuteDockerCommandAsync(context, "build", $"-t {tag} \"{path}\"", token);
        }

        public async Task<int> DockerRun(AgentTaskPluginExecutionContext context, ContainerInfo container, CancellationToken token)
        {
            IList<string> dockerOptions = new List<string>();
            // OPTIONS
            dockerOptions.Add($"--name {container.ContainerDisplayName}");
            dockerOptions.Add($"--label {DockerInstanceLabel}");

            dockerOptions.Add($"--workdir /github/workspace");

            var envFile = Path.Combine(context.Variables.GetValueOrDefault("agent.tempDirectory")?.Value, ".container_env");
            File.WriteAllLines(envFile, container.ContainerEnvironmentVariables.Select(x => $"{x.Key}={x.Value}"));
            dockerOptions.Add($"--env-file \"{envFile}\"");

            if (!string.IsNullOrEmpty(container.ContainerEntryPoint))
            {
                dockerOptions.Add($"--entrypoint \"{container.ContainerEntryPoint}\"");
            }

            if (!string.IsNullOrEmpty(container.ContainerNetwork))
            {
                dockerOptions.Add($"--network {container.ContainerNetwork}");
            }

            foreach (var volume in container.MountVolumes)
            {
                // replace `"` with `\"` and add `"{0}"` to all path.
                String volumeArg;
                if (String.IsNullOrEmpty(volume.SourceVolumePath))
                {
                    // Anonymous docker volume
                    volumeArg = $"-v \"{volume.TargetVolumePath.Replace("\"", "\\\"")}\"";
                }
                else
                {
                    // Named Docker volume / host bind mount
                    volumeArg = $"-v \"{volume.SourceVolumePath.Replace("\"", "\\\"")}\":\"{volume.TargetVolumePath.Replace("\"", "\\\"")}\"";
                }
                if (volume.ReadOnly)
                {
                    volumeArg += ":ro";
                }
                dockerOptions.Add(volumeArg);
            }
            // IMAGE
            dockerOptions.Add($"{container.ContainerImage}");
            // COMMAND
            dockerOptions.Add($"{container.ContainerCommand}");

            var optionsString = string.Join(" ", dockerOptions);
            return await ExecuteDockerCommandAsync(context, "run", optionsString, token);
        }

        private async Task<int> ExecuteDockerCommandAsync(AgentTaskPluginExecutionContext context, string command, string options, CancellationToken cancellationToken = default(CancellationToken))
        {
            string arg = $"{command} {options}".Trim();
            context.Command($"{DockerPath} {arg}");

            var processInvoker = new ProcessInvoker(context);
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            // await Task.Delay(1000);

            // return 0;
            return await processInvoker.ExecuteAsync(
                workingDirectory: context.Variables.GetValueOrDefault("agent.WorkFolder")?.Value,
                fileName: DockerPath,
                arguments: arg,
                environment: null,
                requireExitCodeZero: false,
                outputEncoding: null,
                killProcessOnCancel: false,
                cancellationToken: cancellationToken);
        }

        private async Task<List<string>> ExecuteDockerCommandAsync(AgentTaskPluginExecutionContext context, string command, string options)
        {
            string arg = $"{command} {options}".Trim();
            context.Command($"{DockerPath} {arg}");

            List<string> output = new List<string>();
            var processInvoker = new ProcessInvoker(context);
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    output.Add(message.Data);
                    context.Output(message.Data);
                }
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    context.Output(message.Data);
                }
            };

            // await Task.Delay(1000);

            await processInvoker.ExecuteAsync(
                            workingDirectory: context.Variables.GetValueOrDefault("agent.WorkFolder")?.Value,
                            fileName: DockerPath,
                            arguments: arg,
                            environment: null,
                            requireExitCodeZero: true,
                            outputEncoding: null,
                            cancellationToken: CancellationToken.None);

            return output;
        }
    }

    public class ContainerInfo
    {
        private IDictionary<string, string> _userMountVolumes;
        private List<MountVolume> _mountVolumes;
        private IDictionary<string, string> _userPortMappings;
        private List<PortMapping> _portMappings;
        private IDictionary<string, string> _environmentVariables;

#if OS_WINDOWS
        private Dictionary<string, string> _pathMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#else
        private Dictionary<string, string> _pathMappings = new Dictionary<string, string>();
#endif

        public ContainerInfo(AgentTaskPluginExecutionContext executionContext, Pipelines.ContainerResource container)
        {
            this.ContainerName = container.Alias;

            string containerImage = container.Properties.Get<string>("image");
            ArgUtil.NotNullOrEmpty(containerImage, nameof(containerImage));

            this.ContainerImage = containerImage;
            this.ContainerDisplayName = $"{container.Alias}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            this.ContainerRegistryEndpoint = container.Endpoint?.Id ?? Guid.Empty;
            this.ContainerCreateOptions = container.Properties.Get<string>("options");
            this.SkipContainerImagePull = container.Properties.Get<bool>("localimage");
            _environmentVariables = container.Environment;
            this.ContainerCommand = container.Properties.Get<string>("command", defaultValue: "");
            this.ContainerEntryPoint = container.Properties.Get<string>("entryPoint", defaultValue: "");
            this.ContainerNetwork = executionContext.Variables.GetValueOrDefault("agent.containernetwork")?.Value;

            var defaultWorkingDirectory = executionContext.Variables.GetValueOrDefault("system.defaultWorkingDirectory")?.Value;
            var tempDirectory = executionContext.Variables.GetValueOrDefault("agent.tempdirectory")?.Value;

            ArgUtil.NotNullOrEmpty(defaultWorkingDirectory, nameof(defaultWorkingDirectory));
            ArgUtil.NotNullOrEmpty(tempDirectory, nameof(tempDirectory));

            var tempHomeDirectory = Path.Combine(tempDirectory, "_github_home");
            Directory.CreateDirectory(tempHomeDirectory);

            _pathMappings[defaultWorkingDirectory] = "/github/workspace";
            _pathMappings[tempHomeDirectory] = "/github/home";

            this.MountVolumes.Add(new MountVolume("/var/run/docker.sock", "/var/run/docker.sock"));
            this.MountVolumes.Add(new MountVolume(tempHomeDirectory, "/github/home"));
            this.MountVolumes.Add(new MountVolume(TranslateToHostPath(defaultWorkingDirectory), "/github/workspace"));

            if (container.Ports?.Count > 0)
            {
                foreach (var port in container.Ports)
                {
                    UserPortMappings[port] = port;
                }
            }
            if (container.Volumes?.Count > 0)
            {
                foreach (var volume in container.Volumes)
                {
                    UserMountVolumes[volume] = volume;
                }
            }
        }

        public string ContainerId { get; set; }
        public string ContainerDisplayName { get; private set; }
        public string ContainerNetwork { get; set; }
        public string ContainerNetworkAlias { get; set; }
        public string ContainerImage { get; set; }
        public string ContainerName { get; set; }
        public string ContainerEntryPoint { get; set; }
        public string ContainerCommand { get; set; }
        public string ContainerBringNodePath { get; set; }
        public Guid ContainerRegistryEndpoint { get; private set; }
        public string ContainerCreateOptions { get; private set; }
        public bool SkipContainerImagePull { get; private set; }
#if !OS_WINDOWS
        public string CurrentUserName { get; set; }
        public string CurrentUserId { get; set; }
#endif
        public bool IsJobContainer { get; set; }

        public IDictionary<string, string> ContainerEnvironmentVariables
        {
            get
            {
                if (_environmentVariables == null)
                {
                    _environmentVariables = new Dictionary<string, string>();
                }

                return _environmentVariables;
            }
        }

        public IDictionary<string, string> UserMountVolumes
        {
            get
            {
                if (_userMountVolumes == null)
                {
                    _userMountVolumes = new Dictionary<string, string>();
                }
                return _userMountVolumes;
            }
        }

        public List<MountVolume> MountVolumes
        {
            get
            {
                if (_mountVolumes == null)
                {
                    _mountVolumes = new List<MountVolume>();
                }

                return _mountVolumes;
            }
        }

        public IDictionary<string, string> UserPortMappings
        {
            get
            {
                if (_userPortMappings == null)
                {
                    _userPortMappings = new Dictionary<string, string>();
                }

                return _userPortMappings;
            }
        }

        public List<PortMapping> PortMappings
        {
            get
            {
                if (_portMappings == null)
                {
                    _portMappings = new List<PortMapping>();
                }

                return _portMappings;
            }
        }

        public string TranslateToContainerPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var mapping in _pathMappings)
                {
#if OS_WINDOWS
                    if (string.Equals(path, mapping.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Value;
                    }

                    if (path.StartsWith(mapping.Key + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith(mapping.Key + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Value + path.Remove(0, mapping.Key.Length);
                    }
#else
                    if (string.Equals(path, mapping.Key))
                    {
                        return mapping.Value;
                    }

                    if (path.StartsWith(mapping.Key + Path.DirectorySeparatorChar))
                    {
                        return mapping.Value + path.Remove(0, mapping.Key.Length);
                    }
#endif
                }
            }

            return path;
        }

        public string TranslateToHostPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var mapping in _pathMappings)
                {
#if OS_WINDOWS
                    if (string.Equals(path, mapping.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Key;
                    }

                    if (path.StartsWith(mapping.Value + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith(mapping.Value + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Key + path.Remove(0, mapping.Value.Length);
                    }
#else
                    if (string.Equals(path, mapping.Value))
                    {
                        return mapping.Key;
                    }

                    if (path.StartsWith(mapping.Value + Path.DirectorySeparatorChar))
                    {
                        return mapping.Key + path.Remove(0, mapping.Value.Length);
                    }
#endif
                }
            }

            return path;
        }

        public void AddPortMappings(List<PortMapping> portMappings)
        {
            foreach (var port in portMappings)
            {
                PortMappings.Add(port);
            }
        }

        // public void ExpandProperties(Variables variables)
        // {
        //     // Expand port mapping
        //     variables.ExpandValues(UserPortMappings);

        //     // Expand volume mounts
        //     variables.ExpandValues(UserMountVolumes);
        //     foreach (var volume in UserMountVolumes.Values)
        //     {
        //         // After mount volume variables are expanded, they are final
        //         MountVolumes.Add(new MountVolume(volume));
        //     }
        // }
    }

    public class MountVolume
    {
        public MountVolume(string sourceVolumePath, string targetVolumePath, bool readOnly = false)
        {
            this.SourceVolumePath = sourceVolumePath;
            this.TargetVolumePath = targetVolumePath;
            this.ReadOnly = readOnly;
        }

        public MountVolume(string fromString)
        {
            ParseVolumeString(fromString);
        }

        private void ParseVolumeString(string volume)
        {
            var volumeSplit = volume.Split(":");
            if (volumeSplit.Length == 3)
            {
                // source:target:ro
                SourceVolumePath = volumeSplit[0];
                TargetVolumePath = volumeSplit[1];
                ReadOnly = String.Equals(volumeSplit[2], "ro", StringComparison.OrdinalIgnoreCase);
            }
            else if (volumeSplit.Length == 2)
            {
                if (String.Equals(volumeSplit[1], "ro", StringComparison.OrdinalIgnoreCase))
                {
                    // target:ro
                    TargetVolumePath = volumeSplit[0];
                    ReadOnly = true;
                }
                else
                {
                    // source:target
                    SourceVolumePath = volumeSplit[0];
                    TargetVolumePath = volumeSplit[1];
                    ReadOnly = false;
                }
            }
            else
            {
                // target - or, default to passing straight through
                TargetVolumePath = volume;
                ReadOnly = false;
            }
        }

        public string SourceVolumePath { get; set; }
        public string TargetVolumePath { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class PortMapping
    {
        public PortMapping(string hostPort, string containerPort, string protocol)
        {
            this.HostPort = hostPort;
            this.ContainerPort = containerPort;
            this.Protocol = protocol;
        }

        public string HostPort { get; set; }
        public string ContainerPort { get; set; }
        public string Protocol { get; set; }
    }

    public class DockerVersion
    {
        public DockerVersion(Version serverVersion, Version clientVersion)
        {
            this.ServerVersion = serverVersion;
            this.ClientVersion = clientVersion;
        }

        public Version ServerVersion { get; set; }
        public Version ClientVersion { get; set; }
    }
}
