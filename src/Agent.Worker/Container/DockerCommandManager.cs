using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Container
{
    [ServiceLocator(Default = typeof(DockerCommandManager))]
    public interface IDockerCommandManager : IAgentService
    {
        string DockerPath { get; }
        string DockerInstanceLabel { get; }
        Task<DockerVersion> DockerVersion(IExecutionContext context);
        Task<int> DockerLogin(IExecutionContext context, string server, string username, string password);
        Task<int> DockerLogout(IExecutionContext context, string server);
        Task<int> DockerPull(IExecutionContext context, string image);
        Task<string> DockerCreate(IExecutionContext context, string displayName, string image, List<MountVolume> mountVolumes, string network, string options, IDictionary<string, string> environment);
        Task<int> DockerStart(IExecutionContext context, string containerId);
        Task<int> DockerLogs(IExecutionContext context, string containerId);
        Task<List<string>> DockerPS(IExecutionContext context, string options);
        Task<int> DockerRemove(IExecutionContext context, string containerId);
        Task<int> DockerNetworkCreate(IExecutionContext context, string network);
        Task<int> DockerNetworkRemove(IExecutionContext context, string network);
        Task<int> DockerNetworkPrune(IExecutionContext context);
        Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command);
        Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> outputs);
        Task<string> DockerInspect(IExecutionContext context, string dockerObject, string options);
    }

    public class DockerCommandManager : AgentService, IDockerCommandManager
    {
        public string DockerPath { get; private set; }

        public string DockerInstanceLabel { get; private set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            DockerPath = WhichUtil.Which("docker", true, Trace);
            DockerInstanceLabel = IOUtil.GetPathHash(hostContext.GetDirectory(WellKnownDirectory.Root)).Substring(0, 6);
        }

        public async Task<DockerVersion> DockerVersion(IExecutionContext context)
        {
            string serverVersionStr = (await ExecuteDockerCommandAsync(context, "version", "--format '{{.Server.Version}}'")).FirstOrDefault();
            ArgUtil.NotNullOrEmpty(serverVersionStr, "Docker.Server.Version");
            context.Output($"Docker daemon version: {serverVersionStr}");

            string clientVersionStr = (await ExecuteDockerCommandAsync(context, "version", "--format '{{.Client.Version}}'")).FirstOrDefault();
            ArgUtil.NotNullOrEmpty(serverVersionStr, "Docker.Client.Version");
            context.Output($"Docker client version: {clientVersionStr}");

            // we interested about major.minor.patch version
            Regex verRegex = new Regex("\\d+\\.\\d+(\\.\\d+)?", RegexOptions.IgnoreCase);

            Version serverVersion = null;
            var serverVersionMatchResult = verRegex.Match(serverVersionStr);
            if (serverVersionMatchResult.Success && !string.IsNullOrEmpty(serverVersionMatchResult.Value))
            {
                if (!Version.TryParse(serverVersionMatchResult.Value, out serverVersion))
                {
                    serverVersion = null;
                }
            }

            Version clientVersion = null;
            var clientVersionMatchResult = verRegex.Match(serverVersionStr);
            if (clientVersionMatchResult.Success && !string.IsNullOrEmpty(clientVersionMatchResult.Value))
            {
                if (!Version.TryParse(clientVersionMatchResult.Value, out clientVersion))
                {
                    clientVersion = null;
                }
            }

            return new DockerVersion(serverVersion, clientVersion);
        }

        public async Task<int> DockerLogin(IExecutionContext context, string server, string username, string password)
        {
#if OS_WINDOWS
            // Wait for 17.07 to switch using stdin for docker registry password.
            return await ExecuteDockerCommandAsync(context, "login", $"--username \"{username}\" --password \"{password.Replace("\"", "\\\"")}\" {server}", new List<string>() { password }, context.CancellationToken);
#else            
            return await ExecuteDockerCommandAsync(context, "login", $"--username \"{username}\" --password-stdin {server}", new List<string>() { password }, context.CancellationToken);
#endif            
        }

        public async Task<int> DockerLogout(IExecutionContext context, string server)
        {
            return await ExecuteDockerCommandAsync(context, "logout", $"{server}", context.CancellationToken);
        }

        public async Task<int> DockerPull(IExecutionContext context, string image)
        {
            return await ExecuteDockerCommandAsync(context, "pull", image, context.CancellationToken);
        }

        public async Task<string> DockerCreate(IExecutionContext context, string displayName, string image, List<MountVolume> mountVolumes, string network, string options, IDictionary<string, string> environment)
        {
            string dockerMountVolumesArgs = string.Empty;
            if (mountVolumes?.Count > 0)
            {
                foreach (var volume in mountVolumes)
                {
                    // replace `"` with `\"` and add `"{0}"` to all path.
                    dockerMountVolumesArgs += $" -v \"{volume.SourceVolumePath.Replace("\"", "\\\"")}\":\"{volume.TargetVolumePath.Replace("\"", "\\\"")}\"";
                    if (volume.ReadOnly)
                    {
                        dockerMountVolumesArgs += ":ro";
                    }
                }
            }

            string dockerEnvArgs = string.Empty;
            if (environment?.Count > 0)
            {
                foreach (var env in environment)
                {
                    dockerEnvArgs += $" -e \"{env.Key}={env.Value.Replace("\"", "\\\"")}\"";
                }
            }

            string node;
            if (!string.IsNullOrEmpty(context.Container.ContainerBringNodePath))
            {
                node = context.Container.ContainerBringNodePath;
            }
            else
            {
                node = context.Container.TranslateToContainerPath(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "node", "bin", $"node{IOUtil.ExeExtension}"));
            }

            string sleepCommand = $"\"{node}\" -e \"setInterval(function(){{}}, 24 * 60 * 60 * 1000);\"";
#if OS_WINDOWS
            string dockerArgs = $"--name {displayName} --label {DockerInstanceLabel} {options} {dockerEnvArgs} {dockerMountVolumesArgs} {image} {sleepCommand}";  // add --network={network} and -v '\\.\pipe\docker_engine:\\.\pipe\docker_engine' when they are available (17.09)
#else
            string dockerArgs = $"--name {displayName} --network={network} --label {DockerInstanceLabel} -v /var/run/docker.sock:/var/run/docker.sock {options} {dockerEnvArgs} {dockerMountVolumesArgs} {image} {sleepCommand}";
#endif
            List<string> outputStrings = await ExecuteDockerCommandAsync(context, "create", dockerArgs);
            return outputStrings.FirstOrDefault();
        }

        public async Task<int> DockerStart(IExecutionContext context, string containerId)
        {
            return await ExecuteDockerCommandAsync(context, "start", containerId, context.CancellationToken);
        }

        public async Task<int> DockerRemove(IExecutionContext context, string containerId)
        {
            return await ExecuteDockerCommandAsync(context, "rm", $"--force {containerId}", context.CancellationToken);
        }

        public async Task<int> DockerLogs(IExecutionContext context, string containerId)
        {
            return await ExecuteDockerCommandAsync(context, "logs", $"--details {containerId}", context.CancellationToken);
        }

        public async Task<List<string>> DockerPS(IExecutionContext context, string options)
        {
            return await ExecuteDockerCommandAsync(context, "ps", options);
        }

        public async Task<int> DockerNetworkCreate(IExecutionContext context, string network)
        {
            return await ExecuteDockerCommandAsync(context, "network", $"create --label {DockerInstanceLabel} {network}", context.CancellationToken);
        }

        public async Task<int> DockerNetworkRemove(IExecutionContext context, string network)
        {
            return await ExecuteDockerCommandAsync(context, "network", $"rm {network}", context.CancellationToken);
        }

        public async Task<int> DockerNetworkPrune(IExecutionContext context)
        {
            return await ExecuteDockerCommandAsync(context, "network", $"prune --force --filter \"label={DockerInstanceLabel}\"", context.CancellationToken);
        }

        public async Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command)
        {
            return await ExecuteDockerCommandAsync(context, "exec", $"{options} {containerId} {command}", context.CancellationToken);
        }

        public async Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> output)
        {
            ArgUtil.NotNull(output, nameof(output));

            string arg = $"exec {options} {containerId} {command}".Trim();
            context.Command($"{DockerPath} {arg}");

            object outputLock = new object();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    lock (outputLock)
                    {
                        output.Add(message.Data);
                    }
                }
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    lock (outputLock)
                    {
                        output.Add(message.Data);
                    }
                }
            };

            return await processInvoker.ExecuteAsync(
                            workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                            fileName: DockerPath,
                            arguments: arg,
                            environment: null,
                            requireExitCodeZero: false,
                            outputEncoding: null,
                            cancellationToken: CancellationToken.None);
        }

        public async Task<string> DockerInspect(IExecutionContext context, string dockerObject, string options)
        {
            return (await ExecuteDockerCommandAsync(context, "inspect", $"{options} {dockerObject}")).FirstOrDefault();
        }

        private Task<int> ExecuteDockerCommandAsync(IExecutionContext context, string command, string options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ExecuteDockerCommandAsync(context, command, options, null, cancellationToken);
        }

        private async Task<int> ExecuteDockerCommandAsync(IExecutionContext context, string command, string options, IList<string> standardIns = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            string arg = $"{command} {options}".Trim();
            context.Command($"{DockerPath} {arg}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            return await processInvoker.ExecuteAsync(
                workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                fileName: DockerPath,
                arguments: arg,
                environment: null,
                requireExitCodeZero: false,
                outputEncoding: null,
                killProcessOnCancel: false,
                contentsToStandardIn: standardIns,
                cancellationToken: cancellationToken);
        }

        private async Task<List<string>> ExecuteDockerCommandAsync(IExecutionContext context, string command, string options)
        {
            string arg = $"{command} {options}".Trim();
            context.Command($"{DockerPath} {arg}");

            List<string> output = new List<string>();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    output.Add(message.Data);
                    context.Output(message.Data);
                }
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    context.Output(message.Data);
                }
            };

            await processInvoker.ExecuteAsync(
                            workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                            fileName: DockerPath,
                            arguments: arg,
                            environment: null,
                            requireExitCodeZero: true,
                            outputEncoding: null,
                            cancellationToken: CancellationToken.None);

            return output;
        }
    }
}