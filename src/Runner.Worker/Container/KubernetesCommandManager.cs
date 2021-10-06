using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using k8s;
using k8s.Exceptions;
using k8s.Models;
using Newtonsoft.Json;
using Microsoft.Rest;

namespace GitHub.Runner.Worker.Container
{
    public class KubernetesCommandManager : RunnerService, IDockerCommandManager
    {
        public string DockerPath { get; private set; }

        public string DockerInstanceLabel { get; private set; }

        public string Type { get; set; }

        private Kubernetes Client { get; set; }

        private string KubeConfig { get; set; }

        private string Namespace { get; set; }

        public string GetRunnerName() {
            string runnerName = Environment.GetEnvironmentVariable("HOSTNAME");
            return runnerName == null ? "runner" : Regex.Replace(runnerName, "[^a-zA-Z0-9-.]", "-");
        }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            Type = "kubernetes";
            DockerPath = WhichUtil.Which("kubectl", true, Trace);
            DockerInstanceLabel = GetRunnerName() + "-container";
            Namespace = Environment.GetEnvironmentVariable("RUNNER_NAMESPACE");
            try
            {
                Client = new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
            }
            catch (KubeConfigException)
            {
                KubeConfig = Environment.GetEnvironmentVariable("KUBECONFIG");
                if (KubeConfig == null)
                {
                    KubeConfig = KubernetesClientConfiguration.KubeConfigDefaultLocation;
                }
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(KubeConfig);
                if (config.Namespace != null)
                {
                    Namespace = config.Namespace;
                }
                Client = new Kubernetes(config);
            }
            if (Namespace == null)
            {
                Namespace = "default";
            }
        }

        public async Task<DockerVersion> DockerVersion(IExecutionContext context)
        {
            VersionInfo version = await Client.GetCodeAsync();
            string serverVersionStr = String.Format("{0}.{1}", version.Major, version.Minor, version);

            context.Output($"Kubernetes server URL: {Client.BaseUri.AbsoluteUri}");
            context.Output($"Kubernetes server API version: {serverVersionStr}");

            // we interested about major.minor.patch version
            Regex verRegex = new Regex("\\d+\\.\\d+(\\.\\d+)?", RegexOptions.IgnoreCase);

            Version serverVersion = null;
            var serverVersionMatchResult = verRegex.Match(serverVersionStr);
            if (serverVersionMatchResult.Success && !string.IsNullOrEmpty(serverVersionMatchResult.Value))
            {
                if (!Version.TryParse(serverVersionMatchResult.Value, out serverVersion))
                {
                    serverVersion = null;
                }
            }

            return new DockerVersion(serverVersion, null);
        }

        public Task<int> DockerPull(IExecutionContext context, string image)
        {
            return Task.Run(() => { return 0; });
        }

        public async Task<int> DockerPull(IExecutionContext context, string image, string configFileDirectory)
        {
            return await Task.Run(() => { return 0; });
        }

        public async Task<int> DockerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag)
        {
            return await Task.Run(() => {
                context.Error("Custom docker file not supported for kubernetes");
                return -1;
            });
        }

        public static IEnumerable<string> SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;

            return Split(commandLine,c =>
                                    {
                                        if (c == '\"')
                                            inQuotes = !inQuotes;

                                        return !inQuotes && c == ' ';
                                    })
                            .Select(arg => TrimMatchingQuotes(arg.Trim(), '\"'))
                            .Where(arg => !string.IsNullOrEmpty(arg));
        }

        public static IEnumerable<string> Split(string str, Func<char, bool> controller)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        public static string TrimMatchingQuotes(string input, char quote)
        {
            if ((input.Length >= 2) && 
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }

        public async Task<string> DockerCreate(IExecutionContext context, ContainerInfo container)
        {
            if (!string.IsNullOrEmpty(container.ContainerCreateOptions)) {
                context.Warning("Container options are not supported for kubernetes");
            }
            if (container.UserPortMappings.Count() > 0) {
                context.Warning("Container ports are not supported for kubernetes");
            }
            if (container.UserMountVolumes.Count() > 0) {
                context.Warning("Container custom volumes are not supported for kubernetes");
            }

            List<V1ContainerPort> ports = new List<V1ContainerPort>();
            foreach (var port in container.UserPortMappings)
            {
                ports.Add(new V1ContainerPort(Convert.ToInt32(port.Value), null, null, port.Key));
            }

            List<V1EnvVar> envVars = new List<V1EnvVar>();
            foreach (var env in container.ContainerEnvironmentVariables)
            {
                if (String.IsNullOrEmpty(env.Value))
                {
                    envVars.Add(new V1EnvVar(env.Key, Environment.GetEnvironmentVariable(env.Key)));
                }
                else
                {
                    envVars.Add(new V1EnvVar(env.Key, env.Value.Replace("\"", "\\\"")));
                }
            }
            envVars.Add(new V1EnvVar("GITHUB_ACTIONS", "true"));

            // Set CI=true when no one else already set it.
            // CI=true is common set in most CI provider in GitHub
            if (!container.ContainerEnvironmentVariables.ContainsKey("CI"))
            {
                envVars.Add(new V1EnvVar("CI", "true"));
            }

            List<V1Volume> volumes = new List<V1Volume>();
            List<V1VolumeMount> volumeMounts = new List<V1VolumeMount>();

            string persistentVolumeClaim = Environment.GetEnvironmentVariable("RUNNER_PERSISTENT_VOLUME_CLAIM");
            if (persistentVolumeClaim != null) {
                V1Volume podVolume = new V1Volume("persistent-storage");
                podVolume.PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource(persistentVolumeClaim);
                volumes.Add(podVolume);
            }

            int volumeIndex = 0;
            foreach (MountVolume volume in container.MountVolumes)
            {
                volumeIndex ++;
                string hostPath = volume.SourceVolumePath.Replace("\"", "\\\"");
                if (String.IsNullOrEmpty(volume.SourceVolumePath))
                {
                    // Anonymous docker volume
                    hostPath = Path.Join(Path.GetTempPath(), $"volume-{volumeIndex}");
                }

                if (persistentVolumeClaim != null && hostPath.Contains("/runner/") && hostPath != "/runner/_work")
                {
                    var subPath = Path.GetFileName(hostPath);
                    volumeMounts.Add(new V1VolumeMount(volume.TargetVolumePath.Replace("\"", "\\\""), "persistent-storage", null, volume.ReadOnly, subPath));
                }
                else
                {
                    string name = $"volume-{volumeIndex}";
                    V1Volume podVolume = new V1Volume(name);
                    podVolume.HostPath = new V1HostPathVolumeSource(hostPath);
                    volumes.Add(podVolume);
                    volumeMounts.Add(new V1VolumeMount(volume.TargetVolumePath.Replace("\"", "\\\""), name, null, volume.ReadOnly));
                }
            }

            V1Container podContainer = new V1Container() {
                Name = "container",
                Image = container.ContainerImage,
                WorkingDir = container.ContainerWorkDirectory,
                Env = envVars,
                Ports = ports,
                VolumeMounts = volumeMounts
            };
            if (!string.IsNullOrEmpty(container.ContainerEntryPointArgs))
            {
                podContainer.Args = SplitCommandLine(container.ContainerEntryPointArgs).ToList();
            }
            if (!string.IsNullOrEmpty(container.ContainerEntryPoint))
            {
                podContainer.Command = new string[] {container.ContainerEntryPoint};
            }

            V1Pod pod = new V1Pod()
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = container.ContainerDisplayName,
                        Annotations = new Dictionary<string, string>()
                        {
                            { "instance", container.ContainerDisplayName },
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new[] {podContainer},
                        Volumes = volumes,
                    },
                };

            context.Debug($"Pod: {JsonConvert.SerializeObject(pod, Formatting.Indented)}");

            try
            {
                context.Output("Creating pod...");
                V1Pod createdPod = await Client.CreateNamespacedPodAsync(pod, Namespace);
                context.Output($"Pod {createdPod.Metadata.Name} created");
                return createdPod.Metadata.Name;
            }
            catch (HttpOperationException ex)
            {
                context.Error($"Unable to create pod. {ex.ToString()}");
                context.Error($"Error: {ex.Response.Content}");
                throw ex;
            }
        }

        public async Task<int> DockerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            // TODO: This is used for container actions
            return await Task.Run(() => {
                return dockerRun();
            });
        }

        public Task<int> dockerRun()
        {
            throw new NotSupportedException("Container operations are only supported on Linux runners");
        }

        public async Task<int> DockerStart(IExecutionContext context, string containerId)
        {
            return await ExecuteKubectlCommandAsync(context, "wait", $"--for=condition=Ready --timeout=60s pod/{containerId}", context.CancellationToken);
        }

        public async Task<int> DockerRemove(IExecutionContext context, string containerId)
        {
            return await ExecuteKubectlCommandAsync(context, "delete pod", $"--wait=true --grace-period=20 {containerId}", context.CancellationToken);
        }

        public async Task<int> DockerLogs(IExecutionContext context, string containerId)
        {
            return await ExecuteKubectlCommandAsync(context, "logs", $"{containerId}", context.CancellationToken);
        }

        public async Task<List<string>> DockerListByLabel(IExecutionContext context)
        {
            return await DockerPS(context, $"--ignore-not-found --no-headers -o custom-columns=Name:.metadata.name {DockerInstanceLabel}\"");
        }

        public async Task<List<string>> DockerListByContainerId(IExecutionContext context, string containerId, string status = null)
        {
            string filter = "";
            if (status != "") {
                filter = $"status.phase={status},";
            }
            return await DockerPS(context, $"--ignore-not-found --no-headers -o custom-columns=Name:.metadata.name --field-selector {filter}metadata.name={containerId}");
        }

        public async Task<List<string>> DockerPS(IExecutionContext context, string options)
        {
            return await ExecuteKubectlCommandAsync(context, "get pods", options);
        }

        public async Task<int> DockerNetworkCreate(IExecutionContext context, string network)
        {
            return await Task.Run(() => {
                context.Debug("Custom network is not supported for kubernetes");
                return 0;
            });
        }

        public async Task<int> DockerNetworkRemove(IExecutionContext context, string network)
        {
            return await Task.Run(() => {
                context.Debug("Custom network is not supported for kubernetes");
                return 0;
            });
        }

        public async Task<int> DockerNetworkPrune(IExecutionContext context)
        {
            return await Task.Run(() => {
                context.Debug("Custom network is not supported for kubernetes");
                return 0;
            });
        }

        public async Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command)
        {
            return await ExecuteKubectlCommandAsync(context, "exec", $"{options} {containerId} -- {command}", context.CancellationToken);
        }

        public async Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> output)
        {
            ArgUtil.NotNull(output, nameof(output));

            string arg = $"exec --namespace {Namespace} {options} {containerId} -- {command}".Trim();
            context.Command($"{DockerPath} {arg}");

            object outputLock = new object();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    lock (outputLock)
                    {
                        output.Add(message.Data);
                    }
                }
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    lock (outputLock)
                    {
                        output.Add(message.Data);
                    }
                }
            };

            if (!Constants.Runner.Platform.Equals(Constants.OSPlatform.Linux))
            {
                throw new NotSupportedException("Container operations are only supported on Linux runners");
            }
            return await processInvoker.ExecuteAsync(
                            workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                            fileName: DockerPath,
                            arguments: arg,
                            environment: null,
                            requireExitCodeZero: false,
                            outputEncoding: null,
                            cancellationToken: CancellationToken.None);
        }

        public async Task<List<string>> DockerGetEnv(IExecutionContext context, string containerID)
        {
            return await ExecuteKubectlCommandAsync(context, "get pods", "-o jsonpath=\"{range .spec.containers[0].env[*]}{.name}{'='}{.value}{'\\n'}{end}\" " + containerID);
        }

        public async Task<string> DockerReadyStatus(IExecutionContext context, string containerID)
        {
            var statuses = await ExecuteKubectlCommandAsync(context, "get pods", "-o jsonpath=\"{range .status.containerStatuses[*]}{.ready}{'\\n'}{end}\" " + containerID);
            if (statuses.Count() == 0) {
                return null;
            }
            return statuses.FirstOrDefault() == "true" ? "healthy" : "starting";
        }

        public async Task<List<PortMapping>> DockerPort(IExecutionContext context, string containerId)
        {
            return await Task.Run(() => {
                return new List<PortMapping>();
            });
        }

        public async Task<string> DockerIP(IExecutionContext context, string containerId)
        {
            List<string> podIp = await ExecuteKubectlCommandAsync(context, "get pods", "-o jsonpath=\"{.status.podIP}\" " + containerId);
            if (podIp.Count() == 1 && podIp[0] != "") {
                return podIp[0];
            }
            throw new InvalidOperationException($"Failed to get IP from pod {containerId}");
        }

        public Task<int> DockerLogin(IExecutionContext context, string configFileDirectory, string registry, string username, string password)
        {
            return Task.Run(() => {
                context.Warning("Custom docker credentials are not supported for kubernetes");
                return 0;
            });
        }

        private Task<int> ExecuteKubectlCommandAsync(IExecutionContext context, string command, string options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ExecuteKubectlCommandAsync(context, command, options, null, cancellationToken);
        }

        private async Task<int> ExecuteKubectlCommandAsync(IExecutionContext context, string command, string options, IDictionary<string, string> environment, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived, CancellationToken cancellationToken = default(CancellationToken))
        {
            string arg = GetKubectlArgs(command, options);
            context.Command($"{DockerPath} {arg}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += stdoutDataReceived;
            processInvoker.ErrorDataReceived += stderrDataReceived;


            if (!Constants.Runner.Platform.Equals(Constants.OSPlatform.Linux))
            {
                throw new NotSupportedException("Container operations are only supported on Linux runners");
            }
            return await processInvoker.ExecuteAsync(
                workingDirectory: context.GetGitHubContext("workspace"),
                fileName: DockerPath,
                arguments: arg,
                environment: environment,
                requireExitCodeZero: false,
                outputEncoding: null,
                killProcessOnCancel: false,
                cancellationToken: cancellationToken);
        }

        private string GetKubectlArgs(string command, string options)
        {
            string arg = $"--namespace {Namespace} {command} {options}".Trim();
            if (KubeConfig != null)
            {
                arg = $"--kubeconfig {KubeConfig} {arg}";
            }

            return arg;
        }

        private async Task<int> ExecuteKubectlCommandAsync(IExecutionContext context, string command, string options, string workingDirectory, CancellationToken cancellationToken = default(CancellationToken))
        {
            string arg = GetKubectlArgs(command, options);
            context.Command($"{DockerPath} {arg}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            if (!Constants.Runner.Platform.Equals(Constants.OSPlatform.Linux))
            {
                throw new NotSupportedException("Container operations are only supported on Linux runners");
            }
            return await processInvoker.ExecuteAsync(
                workingDirectory: workingDirectory ?? context.GetGitHubContext("workspace"),
                fileName: DockerPath,
                arguments: arg,
                environment: null,
                requireExitCodeZero: false,
                outputEncoding: null,
                killProcessOnCancel: false,
                redirectStandardIn: null,
                cancellationToken: cancellationToken);
        }

        private async Task<List<string>> ExecuteKubectlCommandAsync(IExecutionContext context, string command, string options)
        {
            string arg = GetKubectlArgs(command, options);
            context.Command($"{DockerPath} {arg}");

            List<string> output = new List<string>();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
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

            await processInvoker.ExecuteAsync(
                            workingDirectory: context.GetGitHubContext("workspace"),
                            fileName: DockerPath,
                            arguments: arg,
                            environment: null,
                            requireExitCodeZero: true,
                            outputEncoding: null,
                            cancellationToken: CancellationToken.None);

            return output;
        }
    }
}
