using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Linq;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.Pipelines.ContextData;

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
            if(System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier.Contains("alpine")) {
                return Task.FromResult<string>(preferredVersion + "_alpine");
            }
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
                                            CancellationToken cancellationToken)
        {
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += OutputDataReceived;
                processInvoker.ErrorDataReceived += ErrorDataReceived;

                return await processInvoker.ExecuteAsync(workingDirectory: workingDirectory,
                                                         fileName: fileName,
                                                         arguments: arguments,
                                                         environment: environment,
                                                         requireExitCodeZero: requireExitCodeZero,
                                                         outputEncoding: outputEncoding,
                                                         killProcessOnCancel: killProcessOnCancel,
                                                         redirectStandardIn: null,
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
            ArgUtil.NotNullOrEmpty(Container.ContainerId, nameof(Container.ContainerId));

            // remove double quotes around the path
            path = path.Trim('\"');

            // try to resolve path inside container if the request path is part of the mount volume
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? Container.MountVolumes.Exists(x => !string.IsNullOrEmpty(x.SourceVolumePath) && path.StartsWith(x.SourceVolumePath, StringComparison.OrdinalIgnoreCase)) : Container.MountVolumes.Exists(x => !string.IsNullOrEmpty(x.SourceVolumePath) && path.StartsWith(x.SourceVolumePath)))
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
            // make sure container exist.
            ArgUtil.NotNull(Container, nameof(Container));
            ArgUtil.NotNullOrEmpty(Container.ContainerId, nameof(Container.ContainerId));

            var dockerManager = HostContext.GetService<IDockerCommandManager>();
            string dockerClientPath = dockerManager.DockerPath;

            // Usage:  docker exec [OPTIONS] CONTAINER COMMAND [ARG...]
            IList<string> dockerCommandArgs = new List<string>();
            dockerCommandArgs.Add($"exec");

            // [OPTIONS]
            dockerCommandArgs.Add($"-i");
            dockerCommandArgs.Add($"--workdir {workingDirectory}");

            if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) || Container.Os == "windows") {
                foreach (var env in environment)
                {
                    // e.g. -e MY_SECRET maps the value into the exec'ed process without exposing
                    // the value directly in the command
                    dockerCommandArgs.Add(DockerUtil.CreateEscapedOption("-e", env.Key));
                }
            } else {
                ISet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var orgenvironment = environment;
                environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var env in orgenvironment)
                {
                    if(keys.Add(env.Key)) {
                        // e.g. -e MY_SECRET maps the value into the exec'ed process without exposing
                        // the value directly in the command
                        dockerCommandArgs.Add(DockerUtil.CreateEscapedOption("-e", env.Key));
                        environment[env.Key] = env.Value;
                    } else {
                        dockerCommandArgs.Add(DockerUtil.CreateEscapedOption("-e", env.Key, env.Value));
                    }
                }
            }
            if (!string.IsNullOrEmpty(PrependPath))
            {
                // Prepend tool paths to container's PATH
                var fullPath = !string.IsNullOrEmpty(Container.ContainerRuntimePath) ? $"{PrependPath}{(Container.Os == "windows" ? ";" : ":")}{Container.ContainerRuntimePath}" : PrependPath;
                dockerCommandArgs.Add($"-e PATH=\"{fullPath}\"");
            }

            // CONTAINER
            dockerCommandArgs.Add($"{Container.ContainerId}");

            // windows container seem to ignore the custom path without cmd prefix
            if(Container?.Os == "windows") {
                dockerCommandArgs.Add("cmd /c");
                var pattern = new Regex("[;<>|^\n%]");
                Func<string, string> escapeCmd = val => pattern.Replace(val, "^$0");

                // COMMAND
                dockerCommandArgs.Add(escapeCmd(fileName));

                // [ARG...]
                dockerCommandArgs.Add(escapeCmd(arguments));
            } else {
                // COMMAND
                dockerCommandArgs.Add(fileName);

                // [ARG...]
                dockerCommandArgs.Add(arguments);
            }

            string dockerCommandArgstring = string.Join(" ", dockerCommandArgs);

            // make sure all env are using container path
            foreach (var envKey in environment.Keys.ToList())
            {
                environment[envKey] = this.Container.TranslateToContainerPath(environment[envKey]);
            }

            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += OutputDataReceived;
                processInvoker.ErrorDataReceived += ErrorDataReceived;

                if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                    // It appears that node.exe outputs UTF8 when not in TTY mode.
                    outputEncoding = Encoding.UTF8;
                } else {
                    // Let .NET choose the default.
                    outputEncoding = null;
                }

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
    }
}
