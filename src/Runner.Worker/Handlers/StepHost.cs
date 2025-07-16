using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Linq;
using GitHub.Runner.Worker.Container.ContainerHooks;
using System.Threading.Channels;
using System.Runtime.InteropServices;

namespace GitHub.Runner.Worker.Handlers
{
    /// <summary>
    /// Helper class for node version compatibility checks
    /// </summary>
    public static class NodeCompatibilityChecker
    {
        /// <summary>
        /// Checks if Node24 is requested but running on ARM32 Linux, and falls back to Node20 with a warning.
        /// </summary>
        /// <param name="executionContext">The execution context for logging</param>
        /// <param name="preferredVersion">The preferred Node version</param>
        /// <returns>The adjusted Node version</returns>
        public static string CheckNodeVersionForArm32(IExecutionContext executionContext, string preferredVersion)
        {
            if (preferredVersion != null && preferredVersion.StartsWith("node24", StringComparison.OrdinalIgnoreCase))
            {
                bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                              Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
                bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

                if (isArm32 && isLinux)
                {
                    executionContext.Warning($"Node 24 is not supported on Linux ARM32 platforms. Falling back to Node 20.");
                    return "node20";
                }
            }

            return preferredVersion;
        }
    }

    public interface IStepHost : IRunnerService
    {
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        string ResolvePathForStepHost(IExecutionContext executionContext, string path);

        Task<string> DetermineNodeRuntimeVersion(IExecutionContext executionContext, string preferredVersion);

        Task<int> ExecuteAsync(IExecutionContext context,
                               string workingDirectory,
                               string fileName,
                               string arguments,
                               IDictionary<string, string> environment,
                               bool requireExitCodeZero,
                               Encoding outputEncoding,
                               bool killProcessOnCancel,
                               bool inheritConsoleHandler,
                               string standardInInput,
                               CancellationToken cancellationToken);
    }

    [ServiceLocator(Default = typeof(ContainerStepHost))]
    public interface IContainerStepHost : IStepHost
    {
        ContainerInfo Container { get; set; }
        string PrependPath { get; set; }
    }

    [ServiceLocator(Default = typeof(DefaultStepHost))]
    public interface IDefaultStepHost : IStepHost
    {
    }



    public sealed class DefaultStepHost : RunnerService, IDefaultStepHost
    {
        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public string ResolvePathForStepHost(IExecutionContext executionContext, string path)
        {
            return path;
        }


        public Task<string> DetermineNodeRuntimeVersion(IExecutionContext executionContext, string preferredVersion)
        {
            // Check if Node24 is requested but we're on ARM32 Linux
            string adjustedVersion = NodeCompatibilityChecker.CheckNodeVersionForArm32(executionContext, preferredVersion);
            return Task.FromResult(adjustedVersion);
        }

        public async Task<int> ExecuteAsync(IExecutionContext context,
                                            string workingDirectory,
                                            string fileName,
                                            string arguments,
                                            IDictionary<string, string> environment,
                                            bool requireExitCodeZero,
                                            Encoding outputEncoding,
                                            bool killProcessOnCancel,
                                            bool inheritConsoleHandler,
                                            string standardInInput,
                                            CancellationToken cancellationToken)
        {
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                Channel<string> redirectStandardIn = null;
                if (standardInInput != null)
                {
                    redirectStandardIn = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
                    redirectStandardIn.Writer.TryWrite(standardInInput);
                }
                processInvoker.OutputDataReceived += OutputDataReceived;
                processInvoker.ErrorDataReceived += ErrorDataReceived;

                return await processInvoker.ExecuteAsync(workingDirectory: workingDirectory,
                                                         fileName: fileName,
                                                         arguments: arguments,
                                                         environment: environment,
                                                         requireExitCodeZero: requireExitCodeZero,
                                                         outputEncoding: outputEncoding,
                                                         killProcessOnCancel: killProcessOnCancel,
                                                         redirectStandardIn: redirectStandardIn,
                                                         inheritConsoleHandler: inheritConsoleHandler,
                                                         cancellationToken: cancellationToken);
            }
        }
    }

    public sealed class ContainerStepHost : RunnerService, IContainerStepHost
    {
        public ContainerInfo Container { get; set; }
        public string PrependPath { get; set; }
        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public string ResolvePathForStepHost(IExecutionContext executionContext, string path)
        {
            // make sure container exist.
            ArgUtil.NotNull(Container, nameof(Container));
            if (!FeatureManager.IsContainerHooksEnabled(executionContext.Global?.Variables))
            {
                // TODO: Remove nullcheck with executionContext.Global? by setting up ExecutionContext.Global at GitHub.Runner.Common.Tests.Worker.ExecutionContextL0.GetExpressionValues_ContainerStepHost
                ArgUtil.NotNullOrEmpty(Container.ContainerId, nameof(Container.ContainerId));
            }

            // remove double quotes around the path
            path = path.Trim('\"');

            // try to resolve path inside container if the request path is part of the mount volume
#if OS_WINDOWS
            if (Container.MountVolumes.Exists(x => !string.IsNullOrEmpty(x.SourceVolumePath) && path.StartsWith(x.SourceVolumePath, StringComparison.OrdinalIgnoreCase)))
#else
            if (Container.MountVolumes.Exists(x => !string.IsNullOrEmpty(x.SourceVolumePath) && path.StartsWith(x.SourceVolumePath)))
#endif
            {
                return Container.TranslateToContainerPath(path);
            }
            else
            {
                return path;
            }
        }

        public async Task<string> DetermineNodeRuntimeVersion(IExecutionContext executionContext, string preferredVersion)
        {
            // If Node24 is requested but we're on ARM32, fall back to Node20 with a warning
            preferredVersion = NodeCompatibilityChecker.CheckNodeVersionForArm32(executionContext, preferredVersion);

            string nodeExternal = preferredVersion;

            if (FeatureManager.IsContainerHooksEnabled(executionContext.Global.Variables))
            {
                if (Container.IsAlpine)
                {
                    nodeExternal = CheckPlatformForAlpineContainer(executionContext, preferredVersion);
                }
                executionContext.Debug($"Running JavaScript Action with default external tool: {nodeExternal}");
                return nodeExternal;
            }

            // Best effort to determine a compatible node runtime
            // There may be more variation in which libraries are linked than just musl/glibc,
            // so determine based on known distribtutions instead
            var osReleaseIdCmd = "sh -c \"cat /etc/*release | grep ^ID\"";
            var dockerManager = HostContext.GetService<IDockerCommandManager>();

            var output = new List<string>();
            var execExitCode = await dockerManager.DockerExec(executionContext, Container.ContainerId, string.Empty, osReleaseIdCmd, output);
            if (execExitCode == 0)
            {
                foreach (var line in output)
                {
                    executionContext.Debug(line);
                    if (line.ToLower().Contains("alpine"))
                    {
                        nodeExternal = CheckPlatformForAlpineContainer(executionContext, preferredVersion);
                        return nodeExternal;
                    }
                }
            }
            executionContext.Debug($"Running JavaScript Action with default external tool: {nodeExternal}");
            return nodeExternal;
        }

        public async Task<int> ExecuteAsync(IExecutionContext context,
                                            string workingDirectory,
                                            string fileName,
                                            string arguments,
                                            IDictionary<string, string> environment,
                                            bool requireExitCodeZero,
                                            Encoding outputEncoding,
                                            bool killProcessOnCancel,
                                            bool inheritConsoleHandler,
                                            string standardInInput,
                                            CancellationToken cancellationToken)
        {
            ArgUtil.NotNull(Container, nameof(Container));
            var containerHookManager = HostContext.GetService<IContainerHookManager>();
            if (FeatureManager.IsContainerHooksEnabled(context.Global.Variables))
            {
                TranslateToContainerPath(environment);
                await containerHookManager.RunScriptStepAsync(context,
                                                                                   Container,
                                                                                   workingDirectory,
                                                                                   fileName,
                                                                                   arguments,
                                                                                   environment,
                                                                                   PrependPath);
                return (int)(context.Result ?? 0);
            }

            ArgUtil.NotNullOrEmpty(Container.ContainerId, nameof(Container.ContainerId));
            var dockerManager = HostContext.GetService<IDockerCommandManager>();
            string dockerClientPath = dockerManager.DockerPath;

            // Usage:  docker exec [OPTIONS] CONTAINER COMMAND [ARG...]
            IList<string> dockerCommandArgs = new List<string>();
            dockerCommandArgs.Add($"exec");

            // [OPTIONS]
            dockerCommandArgs.Add($"-i");
            dockerCommandArgs.Add($"--workdir {workingDirectory}");
            foreach (var env in environment)
            {
                // e.g. -e MY_SECRET maps the value into the exec'ed process without exposing
                // the value directly in the command
                dockerCommandArgs.Add(DockerUtil.CreateEscapedOption("-e", env.Key));
            }
            if (!string.IsNullOrEmpty(PrependPath))
            {
                // Prepend tool paths to container's PATH
                var fullPath = !string.IsNullOrEmpty(Container.ContainerRuntimePath) ? $"{PrependPath}:{Container.ContainerRuntimePath}" : PrependPath;
                dockerCommandArgs.Add($"-e PATH=\"{fullPath}\"");
            }

            // CONTAINER
            dockerCommandArgs.Add($"{Container.ContainerId}");

            // COMMAND
            dockerCommandArgs.Add(fileName);

            // [ARG...]
            dockerCommandArgs.Add(arguments);

            string dockerCommandArgstring = string.Join(" ", dockerCommandArgs);
            TranslateToContainerPath(environment);

            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += OutputDataReceived;
                processInvoker.ErrorDataReceived += ErrorDataReceived;

#if OS_WINDOWS
                // It appears that node.exe outputs UTF8 when not in TTY mode.
                outputEncoding = Encoding.UTF8;
#else
                // Let .NET choose the default.
                outputEncoding = null;
#endif
                return await processInvoker.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                                                         fileName: dockerClientPath,
                                                         arguments: dockerCommandArgstring,
                                                         environment: environment,
                                                         requireExitCodeZero: requireExitCodeZero,
                                                         outputEncoding: outputEncoding,
                                                         killProcessOnCancel: killProcessOnCancel,
                                                         redirectStandardIn: null,
                                                         inheritConsoleHandler: inheritConsoleHandler,
                                                         cancellationToken: cancellationToken);
            }
        }

        private string CheckPlatformForAlpineContainer(IExecutionContext executionContext, string preferredVersion)
        {
            // Handle ARM32 architecture specifically for node24
            preferredVersion = NodeCompatibilityChecker.CheckNodeVersionForArm32(executionContext, preferredVersion);

            string nodeExternal = preferredVersion;

            // Check for Alpine container compatibility
            if (!Constants.Runner.PlatformArchitecture.Equals(Constants.Architecture.X64))
            {
                var os = Constants.Runner.Platform.ToString();
                var arch = Constants.Runner.PlatformArchitecture.ToString();
                var msg = $"JavaScript Actions in Alpine containers are only supported on x64 Linux runners. Detected {os} {arch}";
                throw new NotSupportedException(msg);
            }

            nodeExternal = $"{preferredVersion}_alpine";
            executionContext.Debug($"Container distribution is alpine. Running JavaScript Action with external tool: {nodeExternal}");
            return nodeExternal;
        }

        private void TranslateToContainerPath(IDictionary<string, string> environment)
        {
            foreach (var envKey in environment.Keys.ToList())
            {
                environment[envKey] = this.Container.TranslateToContainerPath(environment[envKey]);
            }
        }
    }
}
