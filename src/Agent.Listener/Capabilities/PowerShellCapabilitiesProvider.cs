using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            string scriptFile = Path.Combine(IOUtil.GetBinPath(), "powershell", "Add-Capabilities.ps1").Replace("'", "''");
            ArgUtil.File(scriptFile, nameof(scriptFile));
            string arguments = $@"-NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "". '{scriptFile}'""";
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

            // Validate .NET Framework x64 4.5 or higher is installed.
            var regex = new Regex(pattern: @"DotNetFramework_[0-9]+(\.[0-9]+)+_x64", options: RegexOptions.None);
            var minimum = new Version(4, 5);
            bool meetsMinimum =
                capabilities
                // Filter to include only .Net framework x64 capabilities.
                .Where(x => regex.IsMatch(x.Name))
                // Extract the version number.
                .Select(x => x.Name.Substring(startIndex: "DotNetFramework_".Length, length: x.Name.Length - "DotNetFramework__x64".Length))
                // Parse the version number.
                .Select(x =>
                {
                    Version v;
                    return (Version.TryParse(x, out v)) ? v : new Version(0, 0);
                })
                .Any(x => x >= minimum);
            if (!meetsMinimum)
            {
                throw new NonRetryableException(StringUtil.Loc("MinimumNetFramework"));
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
