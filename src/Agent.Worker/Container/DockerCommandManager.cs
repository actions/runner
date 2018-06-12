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
        Task<DockerVersion> DockerVersion(IExecutionContext context);
        Task<int> DockerLogin(IExecutionContext context, string server, string username, string password);
        Task<int> DockerLogout(IExecutionContext context, string server);
        Task<int> DockerPull(IExecutionContext context, string image);
        Task<string> DockerCreate(IExecutionContext context, string displayName, string image, List<MountVolume> mountVolumes, string network, string options, IDictionary<string, string> environment);
        Task<int> DockerStart(IExecutionContext context, string containerId);
        Task<int> DockerStop(IExecutionContext context, string containerId);
        Task<int> DockerNetworkCreate(IExecutionContext context, string network);
        Task<int> DockerNetworkRemove(IExecutionContext context, string network);
        Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command);
        Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> outputs);
    }

    public class DockerCommandManager : AgentService, IDockerCommandManager
    {
        public string DockerPath { get; private set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            DockerPath = WhichUtil.Which("docker", true, Trace);
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

#if OS_WINDOWS
            string node = Path.Combine("C:\\_a\\externals", "node", "bin", $"node{IOUtil.ExeExtension}"); // Windows container always map externals folder to C:\_a\externals
#else
            string node = Path.Combine("/_a/externals", "node", "bin", $"node{IOUtil.ExeExtension}"); // Linux container always map externals folder to /_a/externals
#endif
            string sleepCommand = $"\"{node}\" -e \"setInterval(function(){{}}, 24 * 60 * 60 * 1000);\"";
#if OS_WINDOWS
            string dockerArgs = $"--name {displayName} --rm {options} {dockerEnvArgs} {dockerMountVolumesArgs} {image} {sleepCommand}";  // add --network={network} and -v '\\.\pipe\docker_engine:\\.\pipe\docker_engine' when they are available (17.09)
#else
            string dockerArgs = $"--name {displayName} --rm --network={network} -v /var/run/docker.sock:/var/run/docker.sock {options} {dockerEnvArgs} {dockerMountVolumesArgs} {image} {sleepCommand}";
#endif
            List<string> outputStrings = await ExecuteDockerCommandAsync(context, "create", dockerArgs);
            return outputStrings.FirstOrDefault();
        }

        public async Task<int> DockerStart(IExecutionContext context, string containerId)
        {
            return await ExecuteDockerCommandAsync(context, "start", containerId, context.CancellationToken);
        }

        public async Task<int> DockerStop(IExecutionContext context, string containerId)
        {
            return await ExecuteDockerCommandAsync(context, "stop", containerId, context.CancellationToken);
        }

        public async Task<int> DockerNetworkCreate(IExecutionContext context, string network)
        {
            return await ExecuteDockerCommandAsync(context, "network", $"create {network}", context.CancellationToken);
        }

        public async Task<int> DockerNetworkRemove(IExecutionContext context, string network)
        {
            return await ExecuteDockerCommandAsync(context, "network", $"rm {network}", context.CancellationToken);
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
            object outputLock = new object();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                if (!string.IsNullOrEmpty(message.Data))
                {
                    lock (outputLock)
                    {
                        output.Add(message.Data);
                        context.Output(message.Data);
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
                        context.Output(message.Data);
                    }
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