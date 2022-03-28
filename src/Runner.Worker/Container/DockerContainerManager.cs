using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;

namespace GitHub.Runner.Worker.Container
{
    public class DockerContainerManager : RunnerService, IContainerManager
    {
        private IDockerCommandManager dockerManager;

        public string DockerPath => throw new NotImplementedException();

        public string DockerInstanceLabel => throw new NotImplementedException();

        string IContainerManager.DockerPath => throw new NotImplementedException();

        string IContainerManager.DockerInstanceLabel => throw new NotImplementedException();

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            dockerManager = HostContext.GetService<IDockerCommandManager>();
        }
        public Task<DockerVersion> DockerVersion(IExecutionContext context)
        {
            throw new NotImplementedException();
        }
        public async Task<int> EnsureImageExists(IExecutionContext executionContext, string containerImage, string configLocation = "")
        {
            // Pull down docker image with retry up to 3 times
            int retryCount = 0;
            int pullExitCode = 0;
            while (retryCount < 3)
            {
                pullExitCode = await dockerManager.DockerPull(executionContext, containerImage, configLocation);
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

            return pullExitCode;
        }
        public async Task<string> CreateContainerNetworkAsync(IExecutionContext executionContext)
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
        public async Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network)
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
        public async Task ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container)
        {
            string healthCheck = "--format=\"{{if .Config.Healthcheck}}{{print .State.Health.Status}}{{end}}\"";
            string serviceHealth = (await dockerManager.DockerInspect(context: executionContext, dockerObject: container.ContainerId, options: healthCheck)).FirstOrDefault();
            if (string.IsNullOrEmpty(serviceHealth))
            {
                // Container has no HEALTHCHECK
                return;
            }
            var retryCount = 0;
            while (string.Equals(serviceHealth, "starting", StringComparison.OrdinalIgnoreCase))
            {
                TimeSpan backoff = BackoffTimerHelper.GetExponentialBackoff(retryCount, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(32), TimeSpan.FromSeconds(2));
                executionContext.Output($"{container.ContainerNetworkAlias} service is starting, waiting {backoff.Seconds} seconds before checking again.");
                await Task.Delay(backoff, executionContext.CancellationToken);
                serviceHealth = (await dockerManager.DockerInspect(context: executionContext, dockerObject: container.ContainerId, options: healthCheck)).FirstOrDefault();
                retryCount++;
            }
            if (string.Equals(serviceHealth, "healthy", StringComparison.OrdinalIgnoreCase))
            {
                executionContext.Output($"{container.ContainerNetworkAlias} service is healthy.");
            }
            else
            {
                throw new InvalidOperationException($"Failed to initialize, {container.ContainerNetworkAlias} service is {serviceHealth}.");
            }
        }
        public async Task ContainerCleanup(IExecutionContext executionContext)
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
            executionContext.Output("##[group]Clean up resources from previous jobs");
            var staleContainers = await dockerManager.DockerPS(executionContext, $"--all --quiet --no-trunc --filter \"label={dockerManager.DockerInstanceLabel}\"");
            foreach (var staleContainer in staleContainers)
            {
                int containerRemoveExitCode = await dockerManager.DockerRemove(executionContext, staleContainer);
                if (containerRemoveExitCode != 0)
                {
                    executionContext.Warning($"Delete stale containers failed, docker rm fail with exit code {containerRemoveExitCode} for container {staleContainer}");
                }
            }

            int networkPruneExitCode = await dockerManager.DockerNetworkPrune(executionContext);
            if (networkPruneExitCode != 0)
            {
                executionContext.Warning($"Delete stale container networks failed, docker network prune fail with exit code {networkPruneExitCode}");
            }
            executionContext.Output("##[endgroup]");
        }
        public async Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {

            container.ContainerId = await dockerManager.DockerCreate(executionContext, container);
            ArgUtil.NotNullOrEmpty(container.ContainerId, nameof(container.ContainerId));

            // Start container
            int startExitCode = await dockerManager.DockerStart(executionContext, container.ContainerId);
            if (startExitCode != 0)
            {
                throw new InvalidOperationException($"Docker start fail with exit code {startExitCode}");
            }

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
            executionContext.Output("##[endgroup]");
        }
        public async Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container)
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
        public async Task<DictionaryContextData> GetServiceInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            var service = new DictionaryContextData()
            {
                ["id"] = new StringContextData(container.ContainerId),
                ["ports"] = new DictionaryContextData(),
                ["network"] = new StringContextData(container.ContainerNetwork)
            };
            container.AddPortMappings(await dockerManager.DockerPort(executionContext, container.ContainerId));
            foreach (var port in container.PortMappings)
            {
                (service["ports"] as DictionaryContextData)[port.ContainerPort] = new StringContextData(port.HostPort);
            }

            return service;
        }
        public async Task GetJobContainerInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            var configEnvFormat = "--format \"{{range .Config.Env}}{{println .}}{{end}}\"";
            var containerEnv = await dockerManager.DockerInspect(executionContext, container.ContainerId, configEnvFormat);
            container.ContainerRuntimePath = DockerUtil.ParsePathFromConfigEnv(containerEnv);
        }

        public async Task StartContainersAsync(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            string containerNetwork = await CreateContainerNetworkAsync(executionContext);
            foreach (var container in containers)
            {
                container.ContainerNetwork = containerNetwork;
                await StartContainerAsync(executionContext, container);
            }
        }

        public Task<int> DockerPull(IExecutionContext context, string image, string configFileDirectory)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag)
        {
            throw new NotImplementedException();
        }

        public Task<string> DockerCreate(IExecutionContext context, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public async Task<int> ContainerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            return await dockerManager.DockerRun(context, container, stdoutDataReceived, stderrDataReceived);
        }

        public Task<int> DockerStart(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerLogs(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> DockerPS(IExecutionContext context, string options)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerRemove(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerNetworkCreate(IExecutionContext context, string network)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerNetworkRemove(IExecutionContext context, string network)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerNetworkPrune(IExecutionContext context)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> outputs)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> DockerInspect(IExecutionContext context, string dockerObject, string options)
        {
            throw new NotImplementedException();
        }

        public Task<List<PortMapping>> DockerPort(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerLogin(IExecutionContext context, string configFileDirectory, string registry, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> ContainersCreate(IExecutionContext context, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> Container(IExecutionContext context, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }
    }
}
