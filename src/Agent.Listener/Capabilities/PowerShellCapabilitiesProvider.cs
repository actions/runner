using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Capabilities
{
    public sealed class PowerShellCapabilitiesProvider : AgentService, ICapabilitiesProvider
    {
        public Type ExtensionType => typeof(ICapabilitiesProvider);

        // Only runs on Windows.
        public int Order => 2;

        public async Task<List<Capability>> GetCapabilitiesAsync(AgentSettings settings, CancellationToken cancellationToken)
        {
            Trace.Entering();
            var capabilities = new List<Capability>();
            string powerShellExe = HostContext.GetService<IPowerShellExeUtil>().GetPath();
            string scriptFile = Path.Combine(IOUtil.GetBinPath(), "powershell", "Add-Capabilities.ps1");
            ArgUtil.File(scriptFile, nameof(scriptFile));
            string arguments = $@"-NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command . ""{scriptFile}""";
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived +=
                    (object sender, ProcessDataReceivedEventArgs args) =>
                    {
                        Trace.Info($"STDOUT: {args.Data}");
                        Capability capability;
                        if (TryParseCapability(args.Data, out capability))
                        {
                            Trace.Info($"Adding '{capability.Name}': '{capability.Value}'");
                            capabilities.Add(capability);
                        }
                    };
                processInvoker.ErrorDataReceived +=
                    (object sender, ProcessDataReceivedEventArgs args) =>
                    {
                        Trace.Info($"STDERR: {args.Data}");
                    };
                await processInvoker.ExecuteAsync(
                    workingDirectory: Path.GetDirectoryName(scriptFile),
                    fileName: powerShellExe,
                    arguments: arguments,
                    environment: null,
                    cancellationToken: cancellationToken);
            }

            return capabilities;
        }

        public bool TryParseCapability(string input, out Capability capability)
        {
            Command command;
            string name;
            if (Command.TryParse(input, out command) &&
                string.Equals(command.Area, "agent", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(command.Event, "capability", StringComparison.OrdinalIgnoreCase) &&
                command.Properties.TryGetValue("name", out name) &&
                !string.IsNullOrEmpty(name))
            {
                capability = new Capability(name, command.Data);
                return true;
            }

            capability = null;
            return false;
        }
    }
}
