using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{
    public class DockerContainerManager : RunnerService, IContainerManager
    {
        private IDockerCommandManager dockerManager;

        public string ContainerManagerName => "Docker";

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            dockerManager = HostContext.GetService<IDockerCommandManager>();
        }

        public async Task<string> RegistryLoginAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            var configLocation = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), $".docker_{Guid.NewGuid()}");
            try
            {
                var dirInfo = Directory.CreateDirectory(configLocation);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to create directory to store registry client credentials: {e.Message}");
            }

            int loginExitCode = await dockerManager.DockerLogin(
                executionContext,
                configLocation,
                container.RegistryServer,
                container.RegistryAuthUsername,
                container.RegistryAuthPassword);
            

            if (loginExitCode != 0)
            {
                throw new Exception($"Docker login for '{container.RegistryServer}' failed with exit code {loginExitCode}");
            }

            return configLocation;
        }
        public void RegistryLogout(string configLocation)
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
        public Task<int> ContainerPullAsync(IExecutionContext executionContext, string container)
        {
            return ContainerPullAsync(executionContext, container, string.Empty);
        }
        public async Task<int> ContainerPullAsync(IExecutionContext executionContext, string containerImage, string configLocation)
        {
            return await dockerManager.DockerPull(executionContext, containerImage, configLocation);
        }
        public async Task<string> NetworkCreateAsync(IExecutionContext executionContext)
        {
            // Create local docker network for this job to avoid port conflict when multiple runners run on same machine.
            // All containers within a job join the same network
            executionContext.Output("##[group]Create local container network");
            var containerNetwork = $"github_network_{Guid.NewGuid().ToString("N")}";
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            int networkExitCode = await dockerManager.DockerNetworkCreate(executionContext, containerNetwork);
            if (networkExitCode != 0)
            {
                throw new InvalidOperationException($"Docker network create failed with exit code {networkExitCode}");
            }
            executionContext.JobContext.Container["network"] = new StringContextData(containerNetwork);
            executionContext.Output("##[endgroup]");

            return containerNetwork;
        }
        public async Task NetworkRemoveAsync(IExecutionContext executionContext, string network)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(network, nameof(network));

            executionContext.Output($"Remove container network: {network}");

            int removeExitCode = await dockerManager.DockerNetworkRemove(executionContext, network);
            if (removeExitCode != 0)
            {
                executionContext.Warning($"Docker network rm failed with exit code {removeExitCode}");
            }
        }

        public async Task<string> ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container)
        {
            const string healthCheck = "--format=\"{{if .Config.Healthcheck}}{{print .State.Health.Status}}{{end}}\"";
            return (await dockerManager.DockerInspect(context: executionContext, dockerObject: container.ContainerId, options: healthCheck)).FirstOrDefault();
        }

        public async Task ContainerCleanupAsync(IExecutionContext executionContext)
        {
            // Check docker client/server version
            executionContext.Output("##[group]Checking docker version");
            DockerVersion dockerVersion = await dockerManager.DockerVersion(executionContext);
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
                throw new NotSupportedException($"Min required docker engine API server version is '{requiredDockerEngineAPIVersion}', your docker ('{dockerManager.DockerPath}') server version is '{dockerVersion.ServerVersion}'");
            }
            if (dockerVersion.ClientVersion < requiredDockerEngineAPIVersion)
            {
                throw new NotSupportedException($"Min required docker engine API client version is '{requiredDockerEngineAPIVersion}', your docker ('{dockerManager.DockerPath}') client version is '{dockerVersion.ClientVersion}'");
            }

            // Clean up containers left by previous runs
            await RemoveStaleContainersAsync(executionContext);
            await NetworkPruneAsync(executionContext);
        }
        public async Task NetworkPruneAsync(IExecutionContext executionContext)
        {
            int networkPruneExitCode = await dockerManager.DockerNetworkPrune(executionContext);
            if (networkPruneExitCode != 0)
            {
                executionContext.Warning($"Delete stale container networks failed, docker network prune fail with exit code {networkPruneExitCode}");
            }
        }
        private async Task RemoveStaleContainersAsync(IExecutionContext executionContext)
        {
            var staleContainers = await dockerManager.DockerPS(executionContext, $"--all --quiet --no-trunc --filter \"label={dockerManager.DockerInstanceLabel}\"");
            foreach (var staleContainer in staleContainers)
            {
                int containerRemoveExitCode = await dockerManager.DockerRemove(executionContext, staleContainer);
                if (containerRemoveExitCode != 0)
                {
                    executionContext.Warning($"Delete stale containers failed, docker rm fail with exit code {containerRemoveExitCode} for container {staleContainer}");
                }
            }
        }

        public async Task<int> ContainerStartAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            return await dockerManager.DockerStart(executionContext, container.ContainerId);
        }
        public async Task LogContainerStartupInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            try
            {
                // Make sure container is up and running
                var psOutputs = await dockerManager.DockerPS(executionContext, $"--all --filter id={container.ContainerId} --filter status=running --no-trunc --format \"{{{{.ID}}}} {{{{.Status}}}}\"");
                if (psOutputs.FirstOrDefault(x => !string.IsNullOrEmpty(x))?.StartsWith(container.ContainerId) != true)
                {
                    // container is not up and running, pull docker log for this container.
                    await dockerManager.DockerPS(executionContext, $"--all --filter id={container.ContainerId} --no-trunc --format \"{{{{.ID}}}} {{{{.Status}}}}\"");
                    int logsExitCode = await dockerManager.DockerLogs(executionContext, container.ContainerId);
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
        }

        public async Task<string> ContainerCreateAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            var containerId = await dockerManager.DockerCreate(executionContext, container);
            ArgUtil.NotNullOrEmpty(containerId, nameof(containerId));
            return containerId;
        }

        public async Task ContainerRemoveAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(container, nameof(container));

            if (!string.IsNullOrEmpty(container.ContainerId))
            {
                if (!container.IsJobContainer)
                {
                    // Print logs for service container jobs (not the "action" job itself b/c that's already logged).
                    executionContext.Output($"Print service container logs: {container.ContainerDisplayName}");

                    int logsExitCode = await dockerManager.DockerLogs(executionContext, container.ContainerId);
                    if (logsExitCode != 0)
                    {
                        executionContext.Warning($"Docker logs fail with exit code {logsExitCode}");
                    }
                }

                executionContext.Output($"Stop and remove container: {container.ContainerDisplayName}");

                int rmExitCode = await dockerManager.DockerRemove(executionContext, container.ContainerId);
                if (rmExitCode != 0)
                {
                    executionContext.Warning($"Docker rm fail with exit code {rmExitCode}");
                }
            }
        }
        public async Task<List<PortMapping>> ContainerPort(IExecutionContext executionContext, ContainerInfo container)
        {
            return await dockerManager.DockerPort(executionContext, container.ContainerId);
        }
        public async Task<string> ContainerGetRuntimePathAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            var configEnvFormat = "--format \"{{range .Config.Env}}{{println .}}{{end}}\"";
            var containerEnv = await dockerManager.DockerInspect(executionContext, container.ContainerId, configEnvFormat);
            return DockerUtil.ParsePathFromConfigEnv(containerEnv);
        }
        public async Task ContainerStartAllJobDependencies(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            await Task.CompletedTask;
        }
        public async Task<int> ContainerBuildAsync(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag = "")
        {
            if (string.IsNullOrEmpty(tag))
            {
                tag = GenerateContainerTag();
            }
            return await dockerManager.DockerBuild(context, workingDirectory, dockerFile, dockerContext, tag);
        }
        public async Task<int> ContainerBuildAsync(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext)
        {
            return await dockerManager.DockerBuild(context, workingDirectory, dockerFile, dockerContext, string.Empty);
        }
        public async Task<int> ContainerRunAsync(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            return await dockerManager.DockerRun(context, container, stdoutDataReceived, stderrDataReceived);
        }
        public async Task<int> ContainerExecAsync(IExecutionContext context, string containerId, string options, string command, List<string> outputs)
        {
            return await dockerManager.DockerExec(context, containerId, options, command, outputs);
        }
        public async Task<int> ContainerExecAsync(string workingDirectory, string fileName, string arguments, string fullPath, IDictionary<string, string> environment, ContainerInfo container, bool requireExitCodeZero, EventHandler<ProcessDataReceivedEventArgs> outputDataReceived, EventHandler<ProcessDataReceivedEventArgs> errorDataReceived, Encoding outputEncoding, bool killProcessOnCancel, object redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken)
        {
            return await dockerManager.DockerExec(
                workingDirectory,
                fileName,
                arguments,
                fullPath,
                environment,
                container,
                requireExitCodeZero,
                outputDataReceived,
                errorDataReceived,
                outputEncoding,
                killProcessOnCancel,
                redirectStandardIn,
                inheritConsoleHandler,
                cancellationToken);
        }
        public string GenerateContainerTag() => $"{dockerManager.DockerInstanceLabel}:{Guid.NewGuid().ToString("N")}";
        public async Task ContainerPruneAsync(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            await Task.CompletedTask;
        }
    }
}
