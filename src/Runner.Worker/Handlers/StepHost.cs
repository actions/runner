using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker.Container;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Handlers
{
    public interface IStepHost : IRunnerService
    {
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        string ResolvePathForStepHost(string path);

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
#if OS_WINDOWS
            if (Container.MountVolumes.Exists(x => path.StartsWith(x.SourceVolumePath, StringComparison.OrdinalIgnoreCase)))
#else
            if (Container.MountVolumes.Exists(x => path.StartsWith(x.SourceVolumePath)))
#endif
            {
                return Container.TranslateToContainerPath(path);
            }
            else
            {
                return path;
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
            // make sure container exist.
            ArgUtil.NotNull(Container, nameof(Container));
            ArgUtil.NotNullOrEmpty(Container.ContainerId, nameof(Container.ContainerId));

            var dockerManger = HostContext.GetService<IDockerCommandManager>();
            string containerEnginePath = dockerManger.DockerPath;

            ContainerStandardInPayload payload = new ContainerStandardInPayload()
            {
                ExecutionHandler = fileName,
                ExecutionHandlerWorkingDirectory = workingDirectory,
                ExecutionHandlerArguments = arguments,
                ExecutionHandlerEnvironment = environment,
                ExecutionHandlerPrependPath = PrependPath
            };

            // copy the intermediate script (containerHandlerInvoker.js) into Agent_TempDirectory
            // Background:
            //    We rely on environment variables to send task execution information from agent to task execution engine (node/powershell)
            //    Those task execution information will include all the variables and secrets customer has.
            //    The only way to pass environment variables to `docker exec` is through command line arguments, ex: `docker exec -e myenv=myvalue -e mysecert=mysecretvalue ...`
            //    Since command execution may get log into system event log which might cause secret leaking.
            //    We use this intermediate script to read everything from STDIN, then launch the task execution engine (node/powershell) and redirect STDOUT/STDERR

            string tempDir = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.TempDirectory);
            File.Copy(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), "containerHandlerInvoker.js.template"), Path.Combine(tempDir, "containerHandlerInvoker.js"), true);

            string node;
            if (!string.IsNullOrEmpty(Container.ContainerBringNodePath))
            {
                node = Container.ContainerBringNodePath;
            }
            else
            {
                node = Container.TranslateToContainerPath(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "node", "bin", $"node{IOUtil.ExeExtension}"));
            }

            string entryScript = Container.TranslateToContainerPath(Path.Combine(tempDir, "containerHandlerInvoker.js"));

#if !OS_WINDOWS
            string containerExecutionArgs = $"exec -i -u {Container.CurrentUserId} {Container.ContainerId} {node} {entryScript}";
#else
            string containerExecutionArgs = $"exec -i {Container.ContainerId} {node} {entryScript}";
#endif

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

                var redirectStandardIn = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
                redirectStandardIn.Writer.TryWrite(JsonUtility.ToString(payload));

                return await processInvoker.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                                                         fileName: containerEnginePath,
                                                         arguments: containerExecutionArgs,
                                                         environment: null,
                                                         requireExitCodeZero: requireExitCodeZero,
                                                         outputEncoding: outputEncoding,
                                                         killProcessOnCancel: killProcessOnCancel,
                                                         redirectStandardIn: redirectStandardIn,
                                                         inheritConsoleHandler: inheritConsoleHandler,
                                                         cancellationToken: cancellationToken);
            }
        }

        private class ContainerStandardInPayload
        {
            [JsonProperty("handler")]
            public String ExecutionHandler { get; set; }

            [JsonProperty("args")]
            public String ExecutionHandlerArguments { get; set; }

            [JsonProperty("workDir")]
            public String ExecutionHandlerWorkingDirectory { get; set; }

            [JsonProperty("environment")]
            public IDictionary<string, string> ExecutionHandlerEnvironment { get; set; }

            [JsonProperty("prependPath")]
            public string ExecutionHandlerPrependPath { get; set; }
        }
    }
}
