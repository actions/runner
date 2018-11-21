using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using System.Threading;
using System.Linq;
using Microsoft.Win32;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(ContainerOperationProvider))]
    public interface IContainerOperationProvider : IAgentService
    {
        Task StartContainerAsync(IExecutionContext executionContext, object data);
        Task StopContainerAsync(IExecutionContext executionContext, object data);
    }

    public class ContainerOperationProvider : AgentService, IContainerOperationProvider
    {
        private const string _nodeJsPathLabel = "com.azure.dev.pipelines.agent.handler.node.path";
        private IDockerCommandManager _dockerManger;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _dockerManger = HostContext.GetService<IDockerCommandManager>();
        }

        public async Task StartContainerAsync(IExecutionContext executionContext, object data)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            ContainerInfo container = data as ContainerInfo;
            ArgUtil.NotNull(container, nameof(container));
            ArgUtil.NotNullOrEmpty(container.ContainerImage, nameof(container.ContainerImage));

            Trace.Info($"Container name: {container.ContainerName}");
            Trace.Info($"Container image: {container.ContainerImage}");
            Trace.Info($"Container registry: {container.ContainerRegistryEndpoint.ToString()}");
            Trace.Info($"Container options: {container.ContainerCreateOptions}");
            Trace.Info($"Skip container image pull: {container.SkipContainerImagePull}");

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
            Version requiredDockerVersion = new Version(17, 6);
#else
            Version requiredDockerVersion = new Version(17, 12);
#endif

            if (dockerVersion.ServerVersion < requiredDockerVersion)
            {
                throw new NotSupportedException(StringUtil.Loc("MinRequiredDockerServerVersion", requiredDockerVersion, _dockerManger.DockerPath, dockerVersion.ServerVersion));
            }
            if (dockerVersion.ClientVersion < requiredDockerVersion)
            {
                throw new NotSupportedException(StringUtil.Loc("MinRequiredDockerClientVersion", requiredDockerVersion, _dockerManger.DockerPath, dockerVersion.ClientVersion));
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
                string defaultWorkingDirectory = executionContext.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory);
                if (string.IsNullOrEmpty(defaultWorkingDirectory))
                {
                    throw new NotSupportedException(StringUtil.Loc("ContainerJobRequireSystemDefaultWorkDir"));
                }

                string workingDirectory = Path.GetDirectoryName(defaultWorkingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                container.MountVolumes.Add(new MountVolume(container.TranslateToHostPath(workingDirectory), workingDirectory));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Temp), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Temp))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Tools), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Tools))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Tasks), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Tasks))));
                container.MountVolumes.Add(new MountVolume(HostContext.GetDirectory(WellKnownDirectory.Externals), container.TranslateToContainerPath(HostContext.GetDirectory(WellKnownDirectory.Externals)), true));

                // Ensure .taskkey file exist so we can mount it.
                string taskKeyFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), ".taskkey");
                if (!File.Exists(taskKeyFile))
                {
                    File.WriteAllText(taskKeyFile, string.Empty);
                }
                container.MountVolumes.Add(new MountVolume(taskKeyFile, container.TranslateToContainerPath(taskKeyFile)));
#endif

#if !OS_WINDOWS
                if (string.IsNullOrEmpty(container.ContainerNetwork)) // create network when Windows support it.
                {
                    // Create local docker network for this job to avoid port conflict when multiple agents run on same machine.
                    container.ContainerNetwork = $"vsts_network_{Guid.NewGuid().ToString("N")}";
                    int networkExitCode = await _dockerManger.DockerNetworkCreate(executionContext, container.ContainerNetwork);
                    if (networkExitCode != 0)
                    {
                        throw new InvalidOperationException($"Docker network create fail with exit code {networkExitCode}");
                    }

                    // Expose docker network to env
                    executionContext.Variables.Set(Constants.Variables.Agent.ContainerNetwork, container.ContainerNetwork);
                }
#endif
                // See if this container brings its own Node.js
                container.ContainerBringNodePath = await _dockerManger.DockerInspect(context: executionContext,
                                                                      dockerObject: container.ContainerImage,
                                                                      options: $"--format=\"{{{{index .Config.Labels \\\"{_nodeJsPathLabel}\\\"}}}}\"");

                container.ContainerId = await _dockerManger.DockerCreate(context: executionContext,
                                                                         displayName: container.ContainerDisplayName,
                                                                         image: container.ContainerImage,
                                                                         mountVolumes: container.MountVolumes,
                                                                         network: container.ContainerNetwork,
                                                                         options: container.ContainerCreateOptions,
                                                                         environment: container.ContainerEnvironmentVariables);
                ArgUtil.NotNullOrEmpty(container.ContainerId, nameof(container.ContainerId));
                executionContext.Variables.Set(Constants.Variables.Agent.ContainerId, container.ContainerId);

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

#if !OS_WINDOWS
            // Ensure bash exist in the image
            int execWhichBashExitCode = await _dockerManger.DockerExec(executionContext, container.ContainerId, string.Empty, $"which bash");
            if (execWhichBashExitCode != 0)
            {
                try
                {
                    // Make sure container is up and running
                    var psOutputs = await _dockerManger.DockerPS(executionContext, container.ContainerId, "--filter status=running");
                    if (psOutputs.FirstOrDefault(x => !string.IsNullOrEmpty(x))?.StartsWith(container.ContainerId) != true)
                    {
                        // container is not up and running, pull docker log for this container.
                        await _dockerManger.DockerPS(executionContext, container.ContainerId, string.Empty);
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
#endif
        }

        public async Task StopContainerAsync(IExecutionContext executionContext, object data)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ContainerInfo container = data as ContainerInfo;
            ArgUtil.NotNull(container, nameof(container));

            if (!string.IsNullOrEmpty(container.ContainerId))
            {
                executionContext.Output($"Stop container: {container.ContainerDisplayName}");

                int stopExitCode = await _dockerManger.DockerStop(executionContext, container.ContainerId);
                if (stopExitCode != 0)
                {
                    executionContext.Error($"Docker stop fail with exit code {stopExitCode}");
                }

                int rmExitCode = await _dockerManger.DockerRemove(executionContext, container.ContainerId);
                if (rmExitCode != 0)
                {
                    executionContext.Error($"Docker rm fail with exit code {rmExitCode}");
                }

                if (!string.IsNullOrEmpty(container.ContainerNetwork))
                {
                    int removeExitCode = await _dockerManger.DockerNetworkRemove(executionContext, container.ContainerNetwork);
                    if (removeExitCode != 0)
                    {
                        executionContext.Error($"Docker network rm fail with exit code {removeExitCode}");
                    }
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
    }
}
