using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(CapabilitiesProvider))]
    public interface ICapabilitiesProvider : IAgentService
    {
        Task<Dictionary<string, string>> GetCapabilitiesAsync(string agentName, CancellationToken token);
    }

    public class Capability
    {
        public Capability(string name, string tool = null, string[] paths = null)
        {
            Name = name;
            Tool = tool;
            Paths = paths;
        }

        //Name of the capability as requried by the task definition.
        public string Name { get; private set; }

        //Name of an executable file, which if found in the file system means the capability is present.
        public string Tool { get; private set; }

        //An array of paths, which if found in the file system means the capability is present.
        public string[] Paths { get; private set; }
    }

    public class ToolCapability
    {
        public ToolCapability(string name, string command, string commandArgs = null)
        {
            Name = name;
            Command = command;
            CommandArgs = commandArgs;
        }

        //Name of the capability as requried by the task definition.
        public string Name { get; private set; }

        //Name of an executable file, which can determine if a capability is present.
        //The script is expected to print on the standard output the value for the capability.
        public string Command { get; private set; }

        //Arguments passed to Command.
        public string CommandArgs { get; private set; }
    }

    public sealed class CapabilitiesProvider : AgentService, ICapabilitiesProvider
    {
        private List<Capability> _regularCapabilities =
            new List<Capability>
            {
                new Capability( "ant" ),
                new Capability( "bundler", "bundle" ),
                new Capability( "clang" ),
                new Capability( "cmake" ),
                new Capability( "curl" ),
                new Capability( "git" ),
                new Capability( "gulp" ),
                new Capability( "java" ),
                new Capability( "JDK", "javac" ),
                new Capability( "make" ),
                new Capability( "maven", "mvn" ),
                new Capability( "MSBuild", "xbuild" ),
                new Capability( "node.js", "node" ),
                new Capability( "node.js", "nodejs" ),
                new Capability( "npm" ),
                new Capability( "python" ),
                new Capability( "python3" ),
                new Capability( "sh" ),
                new Capability( "subversion", "svn" ),
                new Capability( "ruby" ),
                new Capability( "rake" ),
                new Capability( "Xamarin.iOS", "mdtool", new string[] { "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" } ),
                new Capability( "Xamarin.Android", "mandroid", new string[] { "/Library/Frameworks/Xamarin.Android.framework/Commands/mandroid" } )
            };

        private static readonly List<ToolCapability> _toolCapabilities =
            new List<ToolCapability>
            {
                new ToolCapability( "xcode", "xcode-select", "-p" ),
            };

        private static readonly string[] _ignoredEnvVariables = new string[] {
            "TERM_PROGRAM",
            "TERM",
            "TERM_PROGRAM_VERSION",
            "SHLVL",
            "ls_colors",
            "comp_wordbreaks"
        };

        // Ignore env vars specified in the 'VSO_AGENT_IGNORE' env var
        private const string EnvIgnore = "VSO_AGENT_IGNORE";

        private Dictionary<string, string> _capsCache;

        public List<Capability> RegularCapabilities
        {
            get
            {
                return _regularCapabilities;
            }
        }

        public List<ToolCapability> ToolCapabilities
        {
            get
            {
                return _toolCapabilities;
            }
        }

        public async Task<Dictionary<string, string>> GetCapabilitiesAsync(string agentName, CancellationToken token)
        {
            if (_capsCache != null)
            {
                return new Dictionary<string, string>(_capsCache, StringComparer.OrdinalIgnoreCase);
            }

            var caps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            GetFilteredEnvironmentVars(caps);

            GetRegularCapabilities(caps, token);

            await GetToolCapabilities(caps, token);

            caps["Agent.Name"] = agentName ?? string.Empty;
#if OS_LINUX
            caps["Agent.OS"] = "linux";
#endif

#if OS_OSX
            caps["Agent.OS"] = "darwin";
#endif

#if OS_WINDOWS
            caps["Agent.OS"] = "Windows_NT";
#endif

            caps["Agent.ComputerName"] = System.Environment.MachineName ?? string.Empty;

            foreach (var cap in caps)
            {
                Trace.Info($"Capability: {cap.Key} Value: {cap.Value}");
            }

            _capsCache = new Dictionary<string, string>(caps, StringComparer.OrdinalIgnoreCase);

            return caps;
        }

        private void GetRegularCapabilities(Dictionary<string, string> capabilities, CancellationToken token)
        {
            try
            {
                //TODO: allow paths to embed environment variables with "$VARNAME" or some other syntax and parse them
                var paths = new string[]
                    {
                        Path.Combine(System.Environment.GetEnvironmentVariable("ANDROID_STUDIO") ?? string.Empty, "/tools/android"),
                        Path.Combine(System.Environment.GetEnvironmentVariable("HOME") ?? string.Empty, "/Library/Developer/Xamarin/android-sdk-macosx/tools/android")
                    };
                _regularCapabilities.Add(new Capability("AndroidSDK", "android", paths));
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
            }

            foreach (var cap in _regularCapabilities)
            {
                var whichTool = HostContext.GetService<IWhichUtil>();
                string capPath = whichTool.Which(cap.Tool ?? cap.Name);
                if (!string.IsNullOrEmpty(capPath))
                {
                    capabilities[cap.Name] = capPath;
                }
                else if (cap.Paths != null)
                {
                    foreach (var path in cap.Paths)
                    {
                        if (File.Exists(path))
                        {
                            capabilities[cap.Name] = path;
                            break;
                        }
                    }
                }

                token.ThrowIfCancellationRequested();
            }
        }

        private async Task GetToolCapabilities(Dictionary<string, string> capabilities, CancellationToken token)
        {
            foreach (var cap in _toolCapabilities)
            {
                var whichTool = HostContext.GetService<IWhichUtil>();
                var toolPath = whichTool.Which(cap.Command);
                if (string.IsNullOrEmpty(toolPath))
                {
                    continue;
                }

                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                {
                    string toolOutput = string.Empty;
                    var outputHandler = new EventHandler<DataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            toolOutput += args.Data;
                        }
                    });
                    processInvoker.OutputDataReceived += outputHandler;
                    try
                    {
                        await processInvoker.ExecuteAsync(
                                    workingDirectory: string.Empty,
                                    fileName: toolPath,
                                    arguments: cap.CommandArgs,
                                    environment: null,
                                    cancellationToken: token);
                        //toolOutput does not to be a valid file path
                        if (!string.IsNullOrEmpty(toolOutput))
                        {
                            capabilities[cap.Name] = toolOutput;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                    }
                    finally
                    {
                        processInvoker.OutputDataReceived -= outputHandler;
                    }
                }

                token.ThrowIfCancellationRequested();
            }
        }

        private void GetFilteredEnvironmentVars(Dictionary<string, string> vars)
        {
            IDictionary envVars = System.Environment.GetEnvironmentVariables();

            // Begin with ignoring env vars declared herein
            var ignoredEnvVariables = new HashSet<string>(_ignoredEnvVariables);

            // Also ignore env vars specified in the 'VSO_AGENT_IGNORE' env var
            if (envVars.Contains(EnvIgnore))
            {
                var additionalIgnoredVars = ((string)envVars[EnvIgnore] ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var ignored in additionalIgnoredVars)
                {
                    var ignoreTrimmed = ignored.Trim();
                    if (!string.IsNullOrEmpty(ignoreTrimmed))
                    {
                        ignoredEnvVariables.Add(ignoreTrimmed);
                    }
                }
            }

            // Get filtered env vars
            foreach (DictionaryEntry envVar in envVars)
            {
                string varName = (string)envVar.Key;
                string varValue = (string)envVar.Value ?? string.Empty;
                if (!ignoredEnvVariables.Contains(varName) && varValue.Length < 1024)
                {
                    vars[varName] = varValue;
                }
            }
        }
    }
}
