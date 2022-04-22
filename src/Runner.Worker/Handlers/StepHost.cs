using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Pipelines.ContextData;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Linq;
using GitHub.Runner.Worker.Container.ContainerHooks;
using System.IO;
using System.Threading.Channels;

namespace GitHub.Runner.Worker.Handlers
{
    public interface IStepHost : IRunnerService
    {
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        string ResolvePathForStepHost(string path);

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

        public string ResolvePathForStepHost(string path)
        {
            return path;
        }

        public Task<string> DetermineNodeRuntimeVersion(IExecutionContext executionContext, string preferredVersion)
        {
            return Task.FromResult<string>(preferredVersion);
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
                    await redirectStandardIn.Writer.WriteAsync(standardInInput, cancellationToken); // TODO: catch and rethrow with hook info
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

        public string ResolvePathForStepHost(string path)
        {
            // make sure container exist.
            ArgUtil.NotNull(Container, nameof(Container));
            if (!FeatureFlagManager.IsHookFeatureEnabled())
            {
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
            if (FeatureFlagManager.IsHookFeatureEnabled())
            {
                throw new NotImplementedException("Decide how to determine node version with container hooks.");
            }
            // Best effort to determine a compatible node runtime
            // There may be more variation in which libraries are linked than just musl/glibc,
            // so determine based on known distribtutions instead
            var osReleaseIdCmd = "sh -c \"cat /etc/*release | grep ^ID\"";
            var dockerManager = HostContext.GetService<IDockerCommandManager>();

            var output = new List<string>();
            var execExitCode = await dockerManager.DockerExec(executionContext, Container.ContainerId, string.Empty, osReleaseIdCmd, output);
            string nodeExternal;
            if (execExitCode == 0)
            {
                foreach (var line in output)
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
            if (FeatureFlagManager.IsHookFeatureEnabled())
            {
                TranslateToContainerPath(environment);
                await containerHookManager.ScriptStepAsync(context,
                                                                                   Container,
                                                                                   arguments,
                                                                                   fileName,
                                                                                   environment,
                                                                                   PrependPath,
                                                                                   workingDirectory);
                // normally, ExecuteAsync does not take a 'context', so we have to have an exit code. TODO: is this conversion correct?
                // SucceededWithIssues is a special case, since it is not a failure, but would bubble up as exit code '1', which is failure
                // do we still use it? it has 0 references
                return (int)(context.Result ?? 0);
            }

            ArgUtil.NotNullOrEmpty(Container.ContainerId, nameof(Container.ContainerId));

            return await ExecuteAsync(workingDirectory,
                                fileName,
                                arguments,
                                environment,
                                requireExitCodeZero,
                                outputEncoding,
                                killProcessOnCancel,
                                inheritConsoleHandler,
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
                                            CancellationToken cancellationToken)
        {
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
                dockerCommandArgs.Add($"-e {env.Key}");
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

        private void TranslateToContainerPath(IDictionary<string, string> environment)
        {
            foreach (var envKey in environment.Keys.ToList())
            {
                environment[envKey] = this.Container.TranslateToContainerPath(environment[envKey]);
            }
        }
    }
}
