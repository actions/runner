﻿using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System.Linq;
using System.Threading;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Handlers;
using GitHub.Services.Common;
using Microsoft.Win32;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

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
        private const string _nodeJsPathLabel = "com.azure.dev.pipelines.agent.handler.node.path";
        private IDockerCommandManager _dockerManger;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _dockerManger = HostContext.GetService<IDockerCommandManager>();
        }

        public async Task StartContainersAsync(IExecutionContext executionContext, object data)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            List<ContainerInfo> containers = data as List<ContainerInfo>;
            ArgUtil.NotNull(containers, nameof(containers));

            // Check whether we are inside a container.
            // Our container feature requires to map working directory from host to the container.
            // If we are already inside a container, we will not able to find out the real working direcotry path on the host.
#if OS_WINDOWS
            // service CExecSvc is Container Execution Agent.
            ServiceController[] scServices = ServiceController.GetServices();
            if (scServices.Any(x => String.Equals(x.ServiceName, "cexecsvc", StringComparison.OrdinalIgnoreCase) && x.Status == ServiceControllerStatus.Running))
            {
                throw new NotSupportedException(StringUtil.Loc("AgentAlreadyInsideContainer"));
            }
#elif OS_RHEL6
            // Red Hat and CentOS 6 do not support the container feature
            throw new NotSupportedException(StringUtil.Loc("AgentDoesNotSupportContainerFeatureRhel6"));
#else
            var initProcessCgroup = File.ReadLines("/proc/1/cgroup");
            if (initProcessCgroup.Any(x => x.IndexOf(":/docker/", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                throw new NotSupportedException(StringUtil.Loc("AgentAlreadyInsideContainer"));
            }
#endif

#if OS_WINDOWS
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
                    throw new NotSupportedException(StringUtil.Loc("ContainerWindowsVersionRequirement"));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ReleaseId");
            }
#endif

            // Check docker client/server version
            DockerVersion dockerVersion = await _dockerManger.DockerVersion(executionContext);
            ArgUtil.NotNull(dockerVersion.ServerVersion, nameof(dockerVersion.ServerVersion));
            ArgUtil.NotNull(dockerVersion.ClientVersion, nameof(dockerVersion.ClientVersion));

#if OS_WINDOWS
            Version requiredDockerEngineAPIVersion = new Version(1, 30);  // Docker-EE version 17.6
#else
            Version requiredDockerEngineAPIVersion = new Version(1, 35); // Docker-CE version 17.12
#endif

            if (dockerVersion.ServerVersion < requiredDockerEngineAPIVersion)
            {
                throw new NotSupportedException(StringUtil.Loc("MinRequiredDockerServerVersion", requiredDockerEngineAPIVersion, _dockerManger.DockerPath, dockerVersion.ServerVersion));
            }
            if (dockerVersion.ClientVersion < requiredDockerEngineAPIVersion)
            {
                throw new NotSupportedException(StringUtil.Loc("MinRequiredDockerClientVersion", requiredDockerEngineAPIVersion, _dockerManger.DockerPath, dockerVersion.ClientVersion));
            }

            // Clean up containers left by previous runs
            executionContext.Debug($"Delete stale containers from previous jobs");
            var staleContainers = await _dockerManger.DockerPS(executionContext, $"--all --quiet --no-trunc --filter \"label={_dockerManger.DockerInstanceLabel}\"");
            foreach (var staleContainer in staleContainers)
            {
                int containerRemoveExitCode = await _dockerManger.DockerRemove(executionContext, staleContainer);
                if (containerRemoveExitCode != 0)
                {
                    executionContext.Warning($"Delete stale containers failed, docker rm fail with exit code {containerRemoveExitCode} for container {staleContainer}");
                }
            }

            executionContext.Debug($"Delete stale container networks from previous jobs");
            int networkPruneExitCode = await _dockerManger.DockerNetworkPrune(executionContext);
            if (networkPruneExitCode != 0)
            {
                executionContext.Warning($"Delete stale container networks failed, docker network prune fail with exit code {networkPruneExitCode}");
            }

            // Create local docker network for this job to avoid port conflict when multiple agents run on same machine.
            // All containers within a job join the same network
            await CreateContainerNetworkAsync(executionContext, containers.First().ContainerNetwork);

            // Expose the network name through variable
            executionContext.SetVariable("Agent.ContainerNetwork", containers.First().ContainerNetwork);

            foreach (var container in containers)
            {
                await StartContainerAsync(executionContext, container);
            }

            foreach (var container in containers.Where(c => !c.IsJobContainer))
            {
                await ContainerHealthcheck(executionContext, container);
            }
        }

        public async Task StopContainersAsync(IExecutionContext executionContext, object data)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            List<ContainerInfo> containers = data as List<ContainerInfo>;
            ArgUtil.NotNull(containers, nameof(containers));

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
            Trace.Info($"Container registry: {container.ContainerRegistryEndpoint.ToString()}");
            Trace.Info($"Container options: {container.ContainerCreateOptions}");
            Trace.Info($"Skip container image pull: {container.SkipContainerImagePull}");
            foreach (var port in container.UserPortMappings)
            {
                Trace.Info($"User provided port: {port.Value}");
            }
            foreach (var volume in container.UserMountVolumes)
            {
                Trace.Info($"User provided volume: {volume.Value}");
            }

            // Login to private docker registry
            string registryServer = string.Empty;
            if (container.ContainerRegistryEndpoint != Guid.Empty)
            {
                var registryEndpoint = executionContext.Endpoints.FirstOrDefault(x => x.Type == "dockerregistry" && x.Id == container.ContainerRegistryEndpoint);
                ArgUtil.NotNull(registryEndpoint, nameof(registryEndpoint));

                string username = string.Empty;
                string password = string.Empty;
                registryEndpoint.Authorization?.Parameters?.TryGetValue("registry", out registryServer);
                registryEndpoint.Authorization?.Parameters?.TryGetValue("username", out username);
                registryEndpoint.Authorization?.Parameters?.TryGetValue("password", out password);

                ArgUtil.NotNullOrEmpty(registryServer, nameof(registryServer));
                ArgUtil.NotNullOrEmpty(username, nameof(username));
                ArgUtil.NotNullOrEmpty(password, nameof(password));

                int loginExitCode = await _dockerManger.DockerLogin(executionContext, registryServer, username, password);
                if (loginExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker login fail with exit code {loginExitCode}");
                }
            }

            try
            {
                if (!container.SkipContainerImagePull)
                {
                    if (!string.IsNullOrEmpty(registryServer) &&
                        registryServer.IndexOf("index.docker.io", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        var registryServerUri = new Uri(registryServer);
                        if (!container.ContainerImage.StartsWith(registryServerUri.Authority, StringComparison.OrdinalIgnoreCase))
                        {
                            container.ContainerImage = $"{registryServerUri.Authority}/{container.ContainerImage}";
                        }
                    }

                    // Pull down docker image with retry up to 3 times
                    int retryCount = 0;
                    int pullExitCode = 0;
                    while (retryCount < 3)
                    {
                        pullExitCode = await _dockerManger.DockerPull(executionContext, container.ContainerImage);
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

                // Mount folder into container
#if OS_WINDOWS
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Externals), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Externals))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Work), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Work))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Tools), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Tools))));
#else

                string workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                // string workingDirectory = Path.GetDirectoryName(defaultWorkingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                container.MountVolumes.Add(new MountVolume(container.TranslateToHostPath(workingDirectory), workingDirectory));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Temp), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Temp))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Tools), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Tools))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Actions), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Actions))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Externals), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Externals)), true));

                // Ensure .taskkey file exist so we can mount it.
                string taskKeyFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), ".taskkey");
                if (!File.Exists(taskKeyFile))
                {
                    File.WriteAllText(taskKeyFile, string.Empty);
                }
                container.MountVolumes.Add(new MountVolume(taskKeyFile, container.TranslateToContainerPath(taskKeyFile)));
#endif

                if (container.IsJobContainer)
                {
                    // See if this container brings its own Node.js
                    container.ContainerBringNodePath = await _dockerManger.DockerInspect(context: executionContext,
                                                                        dockerObject: container.ContainerImage,
                                                                        options: $"--format=\"{{{{index .Config.Labels \\\"{_nodeJsPathLabel}\\\"}}}}\"");

                    string node;
                    if (!string.IsNullOrEmpty(container.ContainerBringNodePath))
                    {
                        node = container.ContainerBringNodePath;
                    }
                    else
                    {
                        node = container.TranslateToContainerPath(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "node", "bin", $"node{IOUtil.ExeExtension}"));
                    }
                    string sleepCommand = $"\"{node}\" -e \"setInterval(function(){{}}, 24 * 60 * 60 * 1000);\"";
                    container.ContainerCommand = sleepCommand;
                }

                container.ContainerId = await _dockerManger.DockerCreate(executionContext, container);
                ArgUtil.NotNullOrEmpty(container.ContainerId, nameof(container.ContainerId));
                if (container.IsJobContainer)
                {
                    executionContext.Variables.Set(Constants.Variables.Agent.ContainerId, container.ContainerId);
                }

                // Start container
                int startExitCode = await _dockerManger.DockerStart(executionContext, container.ContainerId);
                if (startExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker start fail with exit code {startExitCode}");
                }
            }
            finally
            {
                // Logout for private registry
                if (!string.IsNullOrEmpty(registryServer))
                {
                    int logoutExitCode = await _dockerManger.DockerLogout(executionContext, registryServer);
                    if (logoutExitCode != 0)
                    {
                        executionContext.Error($"Docker logout fail with exit code {logoutExitCode}");
                    }
                }
            }

            try
            {
                // Make sure container is up and running
                var psOutputs = await _dockerManger.DockerPS(executionContext, $"--all --filter id={container.ContainerId} --filter status=running --no-trunc --format \"{{{{.ID}}}} {{{{.Status}}}}\"");
                if (psOutputs.FirstOrDefault(x => !string.IsNullOrEmpty(x))?.StartsWith(container.ContainerId) != true)
                {
                    // container is not up and running, pull docker log for this container.
                    await _dockerManger.DockerPS(executionContext, $"--all --filter id={container.ContainerId} --no-trunc --format \"{{{{.ID}}}} {{{{.Status}}}}\"");
                    int logsExitCode = await _dockerManger.DockerLogs(executionContext, container.ContainerId);
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

            // Get port mappings of running container
            if (executionContext.Container == null && !container.IsJobContainer)
            {
                container.AddPortMappings(await _dockerManger.DockerPort(executionContext, container.ContainerId));
                foreach (var port in container.PortMappings)
                {
                    executionContext.Variables.Set(
                        $"{Constants.Variables.Agent.ServicePortPrefix}.{container.ContainerNetworkAlias}.ports.{port.ContainerPort}",
                        $"{port.HostPort}");
                }
            }

#if !OS_WINDOWS
            if (container.IsJobContainer)
            {
                // Ensure bash exist in the image
                int execWhichBashExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"sh -c \"command -v bash\"");
                if (execWhichBashExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker exec fail with exit code {execWhichBashExitCode}");
                }

                // Get current username
                container.CurrentUserName = (await ExecuteCommandAsync(executionContext, "whoami", string.Empty)).FirstOrDefault();
                ArgUtil.NotNullOrEmpty(container.CurrentUserName, nameof(container.CurrentUserName));

                // Get current userId
                container.CurrentUserId = (await ExecuteCommandAsync(executionContext, "id", $"-u {container.CurrentUserName}")).FirstOrDefault();
                ArgUtil.NotNullOrEmpty(container.CurrentUserId, nameof(container.CurrentUserId));

                executionContext.Output(StringUtil.Loc("CreateUserWithSameUIDInsideContainer", container.CurrentUserId));

                // Create an user with same uid as the agent run as user inside the container.
                // All command execute in docker will run as Root by default, 
                // this will cause the agent on the host machine doesn't have permission to any new file/folder created inside the container.
                // So, we create a user account with same UID inside the container and let all docker exec command run as that user.
                string containerUserName = string.Empty;

                // We need to find out whether there is a user with same UID inside the container
                List<string> userNames = new List<string>();
                int execGrepExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"bash -c \"grep {container.CurrentUserId} /etc/passwd | cut -f1 -d:\"", userNames);
                if (execGrepExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker exec fail with exit code {execGrepExitCode}");
                }

                if (userNames.Count > 0)
                {
                    // check all potential username that might match the UID.
                    foreach (string username in userNames)
                    {
                        int execIdExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"id -u {username}");
                        if (execIdExitCode == 0)
                        {
                            containerUserName = username;
                            break;
                        }
                    }
                }

                // Create a new user with same UID
                if (string.IsNullOrEmpty(containerUserName))
                {
                    containerUserName = $"{container.CurrentUserName}_azpcontainer";
                    int execUseraddExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"useradd -m -u {container.CurrentUserId} {containerUserName}");
                    if (execUseraddExitCode != 0)
                    {
                        throw new InvalidOperationException($"Docker exec fail with exit code {execUseraddExitCode}");
                    }
                }

                executionContext.Output(StringUtil.Loc("GrantContainerUserSUDOPrivilege", containerUserName));

                // Create a new group for giving sudo permission
                int execGroupaddExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"groupadd azure_pipelines_sudo");
                if (execGroupaddExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker exec fail with exit code {execGroupaddExitCode}");
                }

                // Add the new created user to the new created sudo group.
                int execUsermodExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"usermod -a -G azure_pipelines_sudo {containerUserName}");
                if (execUsermodExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker exec fail with exit code {execUsermodExitCode}");
                }

                // Allow the new sudo group run any sudo command without providing password.
                int execEchoExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"su -c \"echo '%azure_pipelines_sudo ALL=(ALL:ALL) NOPASSWD:ALL' >> /etc/sudoers\"");
                if (execUsermodExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker exec fail with exit code {execEchoExitCode}");
                }

                bool setupDockerGroup = executionContext.Variables.GetBoolean("VSTS_SETUP_DOCKERGROUP") ?? StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("VSTS_SETUP_DOCKERGROUP"), true);
                if (setupDockerGroup)
                {
                    executionContext.Output(StringUtil.Loc("AllowContainerUserRunDocker", containerUserName));
                    // Get docker.sock group id on Host
                    string dockerSockGroupId = (await ExecuteCommandAsync(executionContext, "stat", $"-c %g /var/run/docker.sock")).FirstOrDefault();

                    // We need to find out whether there is a group with same GID inside the container
                    string existingGroupName = null;
                    List<string> groupsOutput = new List<string>();
                    int execGroupGrepExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"bash -c \"cat /etc/group\"", groupsOutput);
                    if (execGroupGrepExitCode != 0)
                    {
                        throw new InvalidOperationException($"Docker exec fail with exit code {execGroupGrepExitCode}");
                    }

                    if (groupsOutput.Count > 0)
                    {
                        // check all potential groups that might match the GID.
                        foreach (string groupOutput in groupsOutput)
                        {
                            if (!string.IsNullOrEmpty(groupOutput))
                            {
                                var groupSegments = groupOutput.Split(':');
                                if (groupSegments.Length != 4)
                                {
                                    Trace.Warning($"Unexpected output from /etc/group: '{groupOutput}'");
                                }
                                else
                                {
                                    // the output of /etc/group should looks like `group:x:gid:`
                                    var groupName = groupSegments[0];
                                    var groupId = groupSegments[2];

                                    if (string.Equals(dockerSockGroupId, groupId))
                                    {
                                        existingGroupName = groupName;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(existingGroupName))
                    {
                        // create a new group with same gid
                        existingGroupName = "azure_pipelines_docker";
                        int execDockerGroupaddExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"groupadd -g {dockerSockGroupId} azure_pipelines_docker");
                        if (execDockerGroupaddExitCode != 0)
                        {
                            throw new InvalidOperationException($"Docker exec fail with exit code {execDockerGroupaddExitCode}");
                        }
                    }

                    // Add the new created user to the docker socket group.
                    int execGroupUsermodExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"usermod -a -G {existingGroupName} {containerUserName}");
                    if (execGroupUsermodExitCode != 0)
                    {
                        throw new InvalidOperationException($"Docker exec fail with exit code {execGroupUsermodExitCode}");
                    }
                }
            }
#endif
        }

        private async Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(container, nameof(container));

            if (!string.IsNullOrEmpty(container.ContainerId))
            {
                executionContext.Output($"Stop and remove container: {container.ContainerDisplayName}");

                int rmExitCode = await _dockerManger.DockerRemove(executionContext, container.ContainerId);
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
            int networkExitCode = await _dockerManger.DockerNetworkCreate(executionContext, network);
            if (networkExitCode != 0)
            {
                throw new InvalidOperationException($"Docker network create failed with exit code {networkExitCode}");
            }
            // Expose docker network to env
            executionContext.Variables.Set(Constants.Variables.Agent.ContainerNetwork, network);
        }

        private async Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(network, nameof(network));

            executionContext.Output($"Remove container network: {network}");

            int removeExitCode = await _dockerManger.DockerNetworkRemove(executionContext, network);
            if (removeExitCode != 0)
            {
                executionContext.Warning($"Docker network rm failed with exit code {removeExitCode}");
            }
            // Remove docker network from env
            executionContext.Variables.Set(Constants.Variables.Agent.ContainerNetwork, null);
        }

        private async Task ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container)
        {
            string healthCheck = "--format=\"{{if .Config.Healthcheck}}{{print .State.Health.Status}}{{end}}\"";
            string serviceHealth = await _dockerManger.DockerInspect(context: executionContext, dockerObject: container.ContainerId, options: healthCheck);
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
                serviceHealth = await _dockerManger.DockerInspect(context: executionContext, dockerObject: container.ContainerId, options: healthCheck);
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
    }
}
