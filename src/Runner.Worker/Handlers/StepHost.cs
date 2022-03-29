using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Linq;

namespace GitHub.Runner.Worker.Handlers
{
    public interface IStepHost : IRunnerService
    {
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        string ResolvePathForStepHost(string path);

        Task<string> DetermineNodeRuntimeVersion(IExecutionContext executionContext, string preferredVersion);

        Task<int> ExecuteAsync(string workingDirectory,
                               string fileName,
                               string arguments,
                               IDictionary<string, string> environment,
                               bool requireExitCodeZero,
                               Encoding outputEncoding,
                               bool killProcessOnCancel,
                               bool inheritConsoleHandler,
                               string standardInInput,
                               CancellationToken cancellationToken);

        Task<int> ExecuteAsync(string workingDirectory,
                               string fileName,
                               string arguments,
                               IDictionary<string, string> environment,
                               bool requireExitCodeZero,
                               Encoding outputEncoding,
                               bool killProcessOnCancel,
                               bool inheritConsoleHandler,
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

        public string ResolvePathForStepHost(string path)
        {
            return path;
        }

        public Task<string> DetermineNodeRuntimeVersion(IExecutionContext executionContext, string preferredVersion)
        {
            return Task.FromResult<string>(preferredVersion);
        }

        public async Task<int> ExecuteAsync(string workingDirectory,
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
        public async Task<int> ExecuteAsync(string workingDirectory,
                                            string fileName,
                                            string arguments,
                                            IDictionary<string, string> environment,
                                            bool requireExitCodeZero,
                                            Encoding outputEncoding,
                                            bool killProcessOnCancel,
                                            bool inheritConsoleHandler,
                                            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(workingDirectory: workingDirectory,
                                                     fileName: fileName,
                                                     arguments: arguments,
                                                     environment: environment,
                                                     requireExitCodeZero: requireExitCodeZero,
                                                     outputEncoding: outputEncoding,
                                                     killProcessOnCancel: killProcessOnCancel,
                                                     inheritConsoleHandler: inheritConsoleHandler,
                                                     standardInInput: null,
                                                     cancellationToken: cancellationToken);

        }
    }

    public sealed class ContainerStepHost : RunnerService, IContainerStepHost
    {
        public ContainerInfo Container { get; set; }
        public string PrependPath { get; set; }
        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public string ResolvePathForStepHost(string path)
        {
            // make sure container exist.
            ArgUtil.NotNull(Container, nameof(Container));
            ArgUtil.NotNullOrEmpty(Container.ContainerId, nameof(Container.ContainerId));

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
            // Best effort to determine a compatible node runtime
            // There may be more variation in which libraries are linked than just musl/glibc,
            // so determine based on known distribtutions instead
            var osReleaseIdCmd = "sh -c \"cat /etc/*release | grep ^ID\"";
            var containerManager = HostContext.GetService<IContainerManager>();

            var outputs = new List<string>();
            var execExitCode = await containerManager.ContainerExec(executionContext, Container.ContainerId, string.Empty, osReleaseIdCmd, outputs);
            string nodeExternal;
            if (execExitCode == 0)
            {
                foreach (var line in outputs)
                {
                    executionContext.Debug(line);
                    if (line.ToLower().Contains("alpine"))
                    {
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
                }
            }
            // Optimistically use the default
            nodeExternal = preferredVersion;
            executionContext.Debug($"Running JavaScript Action with default external tool: {nodeExternal}");
            return nodeExternal;
        }

        public async Task<int> ExecuteAsync(string workingDirectory,
                                            string fileName,
                                            string arguments,
                                            IDictionary<string, string> environment,
                                            bool requireExitCodeZero,
                                            Encoding outputEncoding,
                                            bool killProcessOnCancel,
                                            bool inheritConsoleHandler,
                                            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(workingDirectory,
                         fileName,
                          arguments,
                          environment,
                          requireExitCodeZero,
                          outputEncoding,
                          killProcessOnCancel,
                          inheritConsoleHandler,
                          null,
                          cancellationToken);
        }

        public async Task<int> ExecuteAsync(string workingDirectory,
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
            var fullPath = string.Empty;
            if (!string.IsNullOrEmpty(PrependPath))
            {
                // Prepend tool paths to container's PATH
                fullPath = !string.IsNullOrEmpty(Container.ContainerRuntimePath) ? $"{PrependPath}:{Container.ContainerRuntimePath}" : PrependPath;
            }

            var containerManager = HostContext.CreateService<IContainerManager>();
            return await containerManager.ContainerExec(
                                                        workingDirectory: workingDirectory,
                                                        fileName: fileName,
                                                        arguments: arguments,
                                                        fullPath: fullPath,
                                                        environment: environment,
                                                        container: Container,
                                                        requireExitCodeZero: requireExitCodeZero,
                                                        outputDataReceived: OutputDataReceived,
                                                        errorDataReceived: ErrorDataReceived,
                                                        outputEncoding: outputEncoding,
                                                        killProcessOnCancel: killProcessOnCancel,
                                                        redirectStandardIn: null,
                                                        inheritConsoleHandler: inheritConsoleHandler,
                                                        cancellationToken: cancellationToken);
        }
    }
}
