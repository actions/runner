﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using GitHub.Runner.Worker.Container;
using GitHub.Services.Common;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.Runner.Worker.Container.ContainerHooks;
#if OS_WINDOWS // keep win specific imports around even through we don't support containers on win at the moment
using System.ServiceProcess;
using Microsoft.Win32;
#endif

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ContainerOperationProvider))]
    public interface IContainerOperationProvider : IRunnerService
    {
        Task StartContainersAsync(IExecutionContext executionContext, object data);
        Task StopContainersAsync(IExecutionContext executionContext, object data);
    }

    public class ContainerOperationProvider : RunnerService, IContainerOperationProvider
    {
        private IDockerCommandManager _dockerManager;
        private IContainerHookManager _containerHookManager;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _dockerManager = HostContext.GetService<IDockerCommandManager>();
            _containerHookManager = HostContext.GetService<IContainerHookManager>();
        }

        public async Task StartContainersAsync(IExecutionContext executionContext, object data)
        {
            Trace.Entering();
            if (!Constants.Runner.Platform.Equals(Constants.OSPlatform.Linux))
            {
                throw new NotSupportedException("Container operations are only supported on Linux runners");
            }
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            List<ContainerInfo> containers = data as List<ContainerInfo>;
            ArgUtil.NotNull(containers, nameof(containers));

            var postJobStep = new JobExtensionRunner(runAsync: this.StopContainersAsync,
                                                condition: $"{PipelineTemplateConstants.Always}()",
                                                displayName: "Stop containers",
                                                data: data);

            executionContext.Debug($"Register post job cleanup for stopping/deleting containers.");
            executionContext.RegisterPostJobStep(postJobStep);
            if (FeatureManager.IsContainerHooksEnabled(executionContext.Global.Variables))
            {
                // Initialize the containers
                containers.ForEach(container => UpdateRegistryAuthForGitHubToken(executionContext, container));
                containers.Where(container => container.IsJobContainer).ForEach(container => MountWellKnownDirectories(executionContext, container));
                await _containerHookManager.PrepareJobAsync(executionContext, containers);
                return;
            }
            await AssertCompatibleOS(executionContext);

            // Clean up containers left by previous runs
            executionContext.Output("##[group]Clean up resources from previous jobs");
            var staleContainers = await _dockerManager.DockerPS(executionContext, $"--all --quiet --no-trunc --filter \"label={_dockerManager.DockerInstanceLabel}\"");
            foreach (var staleContainer in staleContainers)
            {
                int containerRemoveExitCode = await _dockerManager.DockerRemove(executionContext, staleContainer);
                if (containerRemoveExitCode != 0)
                {
                    executionContext.Warning($"Delete stale containers failed, docker rm fail with exit code {containerRemoveExitCode} for container {staleContainer}");
                }
            }

            int networkPruneExitCode = await _dockerManager.DockerNetworkPrune(executionContext);
            if (networkPruneExitCode != 0)
            {
                executionContext.Warning($"Delete stale container networks failed, docker network prune fail with exit code {networkPruneExitCode}");
            }
            executionContext.Output("##[endgroup]");

            // Create local docker network for this job to avoid port conflict when multiple runners run on same machine.
            // All containers within a job join the same network
            executionContext.Output("##[group]Create local container network");
            var containerNetwork = $"github_network_{Guid.NewGuid().ToString("N")}";
            await CreateContainerNetworkAsync(executionContext, containerNetwork);
            executionContext.JobContext.Container["network"] = new StringContextData(containerNetwork);
            executionContext.Output("##[endgroup]");

            foreach (var container in containers)
            {
                container.ContainerNetwork = containerNetwork;
                await StartContainerAsync(executionContext, container);
            }

            await RunContainersHealthcheck(executionContext, containers);
        }

        public async Task RunContainersHealthcheck(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            executionContext.Output("##[group]Waiting for all services to be ready");

            var unhealthyContainers = new List<ContainerInfo>();
            foreach (var container in containers.Where(c => !c.IsJobContainer))
            {
                var healthcheck = await ContainerHealthcheck(executionContext, container);

                if (!string.Equals(healthcheck, "healthy", StringComparison.OrdinalIgnoreCase))
                {
                    unhealthyContainers.Add(container);
                }
                else
                {
                    executionContext.Output($"{container.ContainerNetworkAlias} service is healthy.");
                }
            }
            executionContext.Output("##[endgroup]");

            if (unhealthyContainers.Count > 0)
            {
                foreach (var container in unhealthyContainers)
                {
                    executionContext.Output($"##[group]Service container {container.ContainerNetworkAlias} failed.");
                    await _dockerManager.DockerLogs(context: executionContext, containerId: container.ContainerId);
                    executionContext.Error($"Failed to initialize container {container.ContainerImage}");
                    container.FailedInitialization = true;
                    executionContext.Output("##[endgroup]");
                }
                throw new InvalidOperationException("One or more containers failed to start.");
            }
        }

        public async Task StopContainersAsync(IExecutionContext executionContext, object data)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            List<ContainerInfo> containers = data as List<ContainerInfo>;
            ArgUtil.NotNull(containers, nameof(containers));

            if (FeatureManager.IsContainerHooksEnabled(executionContext.Global.Variables))
            {
                await _containerHookManager.CleanupJobAsync(executionContext, containers);
                return;
            }

            foreach (var container in containers)
            {
                await StopContainerAsync(executionContext, container);
            }
            // Remove the container network
            await RemoveContainerNetworkAsync(executionContext, containers.First().ContainerNetwork);
        }

        private async Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(container, nameof(container));
            ArgUtil.NotNullOrEmpty(container.ContainerImage, nameof(container.ContainerImage));

            Trace.Info($"Container name: {container.ContainerName}");
            Trace.Info($"Container image: {container.ContainerImage}");
            Trace.Info($"Container options: {container.ContainerCreateOptions}");

            var groupName = container.IsJobContainer ? "Starting job container" : $"Starting {container.ContainerNetworkAlias} service container";
            executionContext.Output($"##[group]{groupName}");

            foreach (var port in container.UserPortMappings)
            {
                Trace.Info($"User provided port: {port.Value}");
            }
            foreach (var mount in container.UserMountVolumes)
            {
                Trace.Info($"User provided volume: {mount.UserProvidedValue}");
                if (string.Equals(mount.SourceVolumePath, "/", StringComparison.OrdinalIgnoreCase))
                {
                    executionContext.Warning($"Volume mount {mount.UserProvidedValue} is going to mount '/' into the container which may cause file ownership change in the entire file system and cause Actions Runner to lose permission to access the disk.");
                }
            }

            UpdateRegistryAuthForGitHubToken(executionContext, container);

            // Before pulling, generate client authentication if required
            var configLocation = await ContainerRegistryLogin(executionContext, container);

            // Pull down docker image with retry up to 3 times
            int retryCount = 0;
            int pullExitCode = 0;
            while (retryCount < 3)
            {
                pullExitCode = await _dockerManager.DockerPull(executionContext, container.ContainerImage, configLocation);
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

            // Remove credentials after pulling
            ContainerRegistryLogout(configLocation);

            if (retryCount == 3 && pullExitCode != 0)
            {
                throw new InvalidOperationException($"Docker pull failed with exit code {pullExitCode}");
            }

            if (container.IsJobContainer)
            {
                MountWellKnownDirectories(executionContext, container);
            }

            container.ContainerId = await _dockerManager.DockerCreate(executionContext, container);
            ArgUtil.NotNullOrEmpty(container.ContainerId, nameof(container.ContainerId));

            // Start container
            int startExitCode = await _dockerManager.DockerStart(executionContext, container.ContainerId);
            if (startExitCode != 0)
            {
                throw new InvalidOperationException($"Docker start fail with exit code {startExitCode}");
            }

            try
            {
                // Make sure container is up and running
                var psOutputs = await _dockerManager.DockerPS(executionContext, $"--all --filter id={container.ContainerId} --filter status=running --no-trunc --format \"{{{{.ID}}}} {{{{.Status}}}}\"");
                if (psOutputs.FirstOrDefault(x => !string.IsNullOrEmpty(x))?.StartsWith(container.ContainerId) != true)
                {
                    // container is not up and running, pull docker log for this container.
                    await _dockerManager.DockerPS(executionContext, $"--all --filter id={container.ContainerId} --no-trunc --format \"{{{{.ID}}}} {{{{.Status}}}}\"");
                    int logsExitCode = await _dockerManager.DockerLogs(executionContext, container.ContainerId);
                    if (logsExitCode != 0)
                    {
                        executionContext.Warning($"Docker logs fail with exit code {logsExitCode}");
                    }

                    executionContext.Warning($"Docker container {container.ContainerId} is not in running state.");
                }
            }
            catch (Exception ex)
            {
                // pull container log is best effort.
                Trace.Error("Catch exception when check container log and container status.");
                Trace.Error(ex);
            }

            // Gather runtime container information
            if (!container.IsJobContainer)
            {
                var service = new DictionaryContextData()
                {
                    ["id"] = new StringContextData(container.ContainerId),
                    ["ports"] = new DictionaryContextData(),
                    ["network"] = new StringContextData(container.ContainerNetwork)
                };
                container.AddPortMappings(await _dockerManager.DockerPort(executionContext, container.ContainerId));
                foreach (var port in container.PortMappings)
                {
                    (service["ports"] as DictionaryContextData)[port.ContainerPort] = new StringContextData(port.HostPort);
                }
                executionContext.JobContext.Services[container.ContainerNetworkAlias] = service;
            }
            else
            {
                var configEnvFormat = "--format \"{{range .Config.Env}}{{println .}}{{end}}\"";
                var containerEnv = await _dockerManager.DockerInspect(executionContext, container.ContainerId, configEnvFormat);
                container.ContainerRuntimePath = DockerUtil.ParsePathFromConfigEnv(containerEnv);
                executionContext.JobContext.Container["id"] = new StringContextData(container.ContainerId);
            }
            executionContext.Output("##[endgroup]");
        }

        private void MountWellKnownDirectories(IExecutionContext executionContext, ContainerInfo container)
        {
            // Configure job container - Mount workspace and tools, set up environment, and start long running process
            var githubContext = executionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));
            var workingDirectory = githubContext["workspace"] as StringContextData;
            ArgUtil.NotNullOrEmpty(workingDirectory, nameof(workingDirectory));
            container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Work), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Work))));
#if OS_WINDOWS
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Externals), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Externals))));
#else
            container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Externals), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Externals)), true));
#endif
            container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Temp), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Temp))));
            container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Actions), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Actions))));
            container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Tools), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Tools))));

            var tempHomeDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), "_github_home");
            Directory.CreateDirectory(tempHomeDirectory);
            container.MountVolumes.Add(new MountVolume(tempHomeDirectory, "/github/home"));
            container.AddPathTranslateMapping(tempHomeDirectory, "/github/home");
            container.ContainerEnvironmentVariables["HOME"] = container.TranslateToContainerPath(tempHomeDirectory);

            var tempWorkflowDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), "_github_workflow");
            Directory.CreateDirectory(tempWorkflowDirectory);
            container.MountVolumes.Add(new MountVolume(tempWorkflowDirectory, "/github/workflow"));
            container.AddPathTranslateMapping(tempWorkflowDirectory, "/github/workflow");

            container.ContainerWorkDirectory = container.TranslateToContainerPath(workingDirectory);
            if (!FeatureManager.IsContainerHooksEnabled(executionContext.Global.Variables))
            {
                container.ContainerEntryPoint = "tail";
                container.ContainerEntryPointArgs = "\"-f\" \"/dev/null\"";
            }
        }

        private async Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(container, nameof(container));

            if (!string.IsNullOrEmpty(container.ContainerId))
            {
                if (!container.IsJobContainer && !container.FailedInitialization)
                {
                        executionContext.Output($"Print service container logs: {container.ContainerDisplayName}");

                        int logsExitCode = await _dockerManager.DockerLogs(executionContext, container.ContainerId);
                        if (logsExitCode != 0)
                        {
                            executionContext.Warning($"Docker logs fail with exit code {logsExitCode}");
                        }
                }

                executionContext.Output($"Stop and remove container: {container.ContainerDisplayName}");

                int rmExitCode = await _dockerManager.DockerRemove(executionContext, container.ContainerId);
                if (rmExitCode != 0)
                {
                    executionContext.Warning($"Docker rm fail with exit code {rmExitCode}");
                }
            }
        }

#if !OS_WINDOWS
        private async Task<List<string>> ExecuteCommandAsync(IExecutionContext context, string command, string arg)
        {
            context.Command($"{command} {arg}");

            List<string> outputs = new List<string>();
            object outputLock = new object();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    lock (outputLock)
                    {
                        outputs.Add(message.Data);
                    }
                }
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    lock (outputLock)
                    {
                        outputs.Add(message.Data);
                    }
                }
            };

            await processInvoker.ExecuteAsync(
                            workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                            fileName: command,
                            arguments: arg,
                            environment: null,
                            requireExitCodeZero: true,
                            outputEncoding: null,
                            cancellationToken: CancellationToken.None);

            foreach (var outputLine in outputs)
            {
                context.Output(outputLine);
            }

            return outputs;
        }
#endif

        private async Task CreateContainerNetworkAsync(IExecutionContext executionContext, string network)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            int networkExitCode = await _dockerManager.DockerNetworkCreate(executionContext, network);
            if (networkExitCode != 0)
            {
                throw new InvalidOperationException($"Docker network create failed with exit code {networkExitCode}");
            }
        }

        private async Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(network, nameof(network));

            executionContext.Output($"Remove container network: {network}");

            int removeExitCode = await _dockerManager.DockerNetworkRemove(executionContext, network);
            if (removeExitCode != 0)
            {
                executionContext.Warning($"Docker network rm failed with exit code {removeExitCode}");
            }
        }

        private async Task<string> ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container)
        {
            string healthCheck = "--format=\"{{if .Config.Healthcheck}}{{print .State.Health.Status}}{{end}}\"";
            string serviceHealth = (await _dockerManager.DockerInspect(context: executionContext, dockerObject: container.ContainerId, options: healthCheck)).FirstOrDefault();
            if (string.IsNullOrEmpty(serviceHealth))
            {
                // Container has no HEALTHCHECK
                return String.Empty;
            }
            var retryCount = 0;
            while (string.Equals(serviceHealth, "starting", StringComparison.OrdinalIgnoreCase))
            {
                TimeSpan backoff = BackoffTimerHelper.GetExponentialBackoff(retryCount, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(32), TimeSpan.FromSeconds(2));
                executionContext.Output($"{container.ContainerNetworkAlias} service is starting, waiting {backoff.Seconds} seconds before checking again.");
                await Task.Delay(backoff, executionContext.CancellationToken);
                serviceHealth = (await _dockerManager.DockerInspect(context: executionContext, dockerObject: container.ContainerId, options: healthCheck)).FirstOrDefault();
                retryCount++;
            }
            return serviceHealth;
        }

        private async Task<string> ContainerRegistryLogin(IExecutionContext executionContext, ContainerInfo container)
        {
            if (string.IsNullOrEmpty(container.RegistryAuthUsername) || string.IsNullOrEmpty(container.RegistryAuthPassword))
            {
                // No valid client config can be generated
                return "";
            }
            var configLocation = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), $".docker_{Guid.NewGuid()}");
            try
            {
                var dirInfo = Directory.CreateDirectory(configLocation);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to create directory to store registry client credentials: {e.Message}");
            }
            var loginExitCode = await _dockerManager.DockerLogin(
                executionContext,
                configLocation,
                container.RegistryServer,
                container.RegistryAuthUsername,
                container.RegistryAuthPassword);

            if (loginExitCode != 0)
            {
                throw new InvalidOperationException($"Docker login for '{container.RegistryServer}' failed with exit code {loginExitCode}");
            }
            return configLocation;
        }

        private void ContainerRegistryLogout(string configLocation)
        {
            try
            {
                if (!string.IsNullOrEmpty(configLocation) && Directory.Exists(configLocation))
                {
                    Directory.Delete(configLocation, recursive: true);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to remove directory containing Docker client credentials: {e.Message}");
            }
        }

        private void UpdateRegistryAuthForGitHubToken(IExecutionContext executionContext, ContainerInfo container)
        {
            var registryIsTokenCompatible = container.RegistryServer.Equals("ghcr.io", StringComparison.OrdinalIgnoreCase) || container.RegistryServer.Equals("containers.pkg.github.com", StringComparison.OrdinalIgnoreCase);
            var isFallbackTokenFromHostedGithub = HostContext.GetService<IConfigurationStore>().GetSettings().IsHostedServer;
            if (!registryIsTokenCompatible || !isFallbackTokenFromHostedGithub)
            {
                return;
            }

            var registryCredentialsNotSupplied = string.IsNullOrEmpty(container.RegistryAuthUsername) && string.IsNullOrEmpty(container.RegistryAuthPassword);
            if (registryCredentialsNotSupplied)
            {
                container.RegistryAuthUsername = executionContext.GetGitHubContext("actor");
                container.RegistryAuthPassword = executionContext.GetGitHubContext("token");
            }
        }

        private async Task AssertCompatibleOS(IExecutionContext executionContext)
        {
            // Check whether we are inside a container.
            // Our container feature requires to map working directory from host to the container.
            // If we are already inside a container, we will not able to find out the real working direcotry path on the host.
#if OS_WINDOWS
#pragma warning disable CA1416
            // service CExecSvc is Container Execution Agent.
            ServiceController[] scServices = ServiceController.GetServices();
            if (scServices.Any(x => String.Equals(x.ServiceName, "cexecsvc", StringComparison.OrdinalIgnoreCase) && x.Status == ServiceControllerStatus.Running))
            {
                throw new NotSupportedException("Container feature is not supported when runner is already running inside container.");
            }
#pragma warning restore CA1416
#else
            var initProcessCgroup = File.ReadLines("/proc/1/cgroup");
            if (initProcessCgroup.Any(x => x.IndexOf(":/docker/", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                throw new NotSupportedException("Container feature is not supported when runner is already running inside container.");
            }
#endif

#if OS_WINDOWS
#pragma warning disable CA1416
            // Check OS version (Windows server 1803 is required)
            object windowsInstallationType = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "InstallationType", defaultValue: null);
            ArgUtil.NotNull(windowsInstallationType, nameof(windowsInstallationType));
            object windowsReleaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", defaultValue: null);
            ArgUtil.NotNull(windowsReleaseId, nameof(windowsReleaseId));
            executionContext.Debug($"Current Windows version: '{windowsReleaseId} ({windowsInstallationType})'");

            if (int.TryParse(windowsReleaseId.ToString(), out int releaseId))
            {
                if (!windowsInstallationType.ToString().StartsWith("Server", StringComparison.OrdinalIgnoreCase) || releaseId < 1803)
                {
                    throw new NotSupportedException("Container feature requires Windows Server 1803 or higher.");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ReleaseId");
            }
#pragma warning restore CA1416
#endif

            // Check docker client/server version
            executionContext.Output("##[group]Checking docker version");
            DockerVersion dockerVersion = await _dockerManager.DockerVersion(executionContext);
            executionContext.Output("##[endgroup]");

            ArgUtil.NotNull(dockerVersion.ServerVersion, nameof(dockerVersion.ServerVersion));
            ArgUtil.NotNull(dockerVersion.ClientVersion, nameof(dockerVersion.ClientVersion));

#if OS_WINDOWS
            Version requiredDockerEngineAPIVersion = new Version(1, 30);  // Docker-EE version 17.6
#else
            Version requiredDockerEngineAPIVersion = new Version(1, 35); // Docker-CE version 17.12
#endif

            if (dockerVersion.ServerVersion < requiredDockerEngineAPIVersion)
            {
                throw new NotSupportedException($"Min required docker engine API server version is '{requiredDockerEngineAPIVersion}', your docker ('{_dockerManager.DockerPath}') server version is '{dockerVersion.ServerVersion}'");
            }
            if (dockerVersion.ClientVersion < requiredDockerEngineAPIVersion)
            {
                throw new NotSupportedException($"Min required docker engine API client version is '{requiredDockerEngineAPIVersion}', your docker ('{_dockerManager.DockerPath}') client version is '{dockerVersion.ClientVersion}'");
            }
        }
    }
}
