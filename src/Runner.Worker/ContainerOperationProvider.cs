using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using GitHub.Runner.Worker.Container;
using GitHub.Services.Common;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.Pipelines.ContextData;
using Microsoft.Win32;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

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
        private IContainerManager _containerManager;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _containerManager = HostContext.GetService<IContainerManager>();
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
            AssertOSVersion();
            await _containerManager.ContainerCleanup(executionContext);
            string containerNetwork = await _containerManager.CreateContainerNetworkAsync(executionContext);

            foreach (var container in containers)
            {
                container.ContainerNetwork = containerNetwork;
                await InitializeContainerAsync(executionContext, container);
                await _containerManager.StartContainerAsync(executionContext, container);
                await GatherRuntimeInformation(executionContext, container);
            }

            executionContext.Output("##[group]Waiting for all services to be ready");
            foreach (var container in containers.Where(c => !c.IsJobContainer))
            {
                await _containerManager.ContainerHealthcheck(executionContext, container);
            }
            executionContext.Output("##[endgroup]");
        }

        private async Task GatherRuntimeInformation(IExecutionContext executionContext, ContainerInfo container)
        {
            // Gather runtime container information
            if (!container.IsJobContainer)
            {
                var service = await _containerManager.GetServiceInfo(executionContext, container);
                executionContext.JobContext.Services[container.ContainerNetworkAlias] = service;
            }
            else
            {
                await _containerManager.GetJobContainerInfo(executionContext, container);
                executionContext.JobContext.Container["id"] = new StringContextData(container.ContainerId);
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
                await _containerManager.StopContainerAsync(executionContext, container);
            }
            // Remove the container network
            await _containerManager.RemoveContainerNetworkAsync(executionContext, containers.First().ContainerNetwork);
        }

        private async Task InitializeContainerAsync(IExecutionContext executionContext, ContainerInfo container)
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
            foreach (var volume in container.UserMountVolumes)
            {
                Trace.Info($"User provided volume: {volume.Value}");
                var mount = new MountVolume(volume.Value);
                if (string.Equals(mount.SourceVolumePath, "/", StringComparison.OrdinalIgnoreCase))
                {
                    executionContext.Warning($"Volume mount {volume.Value} is going to mount '/' into the container which may cause file ownership change in the entire file system and cause Actions Runner to lose permission to access the disk.");
                }
            }

            await _containerManager.EnsureImageExists(executionContext, container);

            if (container.IsJobContainer)
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
                container.ContainerEntryPoint = "tail";
                container.ContainerEntryPointArgs = "\"-f\" \"/dev/null\"";
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
        private static void AssertOSVersion()
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

        }
    }
}
