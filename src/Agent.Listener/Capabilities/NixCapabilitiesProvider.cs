using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Capabilities
{
    public sealed class NixCapabilitiesProvider : AgentService, ICapabilitiesProvider
    {
        private List<Capability> _capabilities; // Cache the capabilities for configure-then-run scenario.

        public Type ExtensionType => typeof(ICapabilitiesProvider);

        // Only runs on Linux/OSX.
        public int Order => 2;

        public async Task<List<Capability>> GetCapabilitiesAsync(AgentSettings settings, CancellationToken cancellationToken)
        {
            Trace.Entering();

            // Check the cache.
            if (_capabilities != null)
            {
                Trace.Info("Found in cached.");
                return _capabilities;
            }

            // Build the list of capabilities.
            var builder = new CapabilitiesBuilder(HostContext, cancellationToken);
            builder.Check(
                name: "AndroidSDK",
                fileName: "android",
                filePaths: new[]
                {
                    Path.Combine(Environment.GetEnvironmentVariable("ANDROID_STUDIO") ?? string.Empty, "tools/android"),
                    Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? string.Empty, "Library/Developer/Xamarin/android-sdk-macosx/tools/android"),
                });
            builder.Check(name: "ant");
            builder.Check(name: "AzureGuestAgent", fileName: "waagent");
            builder.Check(name: "bundler", fileName: "bundle");
            builder.Check(name: "clang");
            builder.Check(name: "cmake");
            builder.Check(name: "curl");
            builder.Check(name: "git");
            builder.Check(name: "gulp");
            builder.Check(name: "java");
            builder.Check(name: "JDK", fileName: "javac");
            builder.Check(name: "make");
            builder.Check(name: "maven", fileName: "mvn");
            builder.Check(name: "MSBuild", fileName: "xbuild");
            builder.Check(name: "node.js", fileName: "node");
            builder.Check(name: "node.js", fileName: "nodejs");
            builder.Check(name: "npm");
            builder.Check(name: "python");
            builder.Check(name: "python3");
            builder.Check(name: "sh");
            builder.Check(name: "subversion", fileName: "svn");
            builder.Check(name: "ruby");
            builder.Check(name: "rake");
            builder.Check(name: "svn");
            builder.Check(
                name: "Xamarin.iOS",
                fileName: "mdtool",
                filePaths: new string[] { 
                    "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool",
                    "/Applications/Visual Studio.app/Contents/MacOS/vstool"
                });
            builder.Check(
                name: "Xamarin.Android",
                fileName: "generator",
                filePaths: new string[] { "/Library/Frameworks/Xamarin.Android.framework/Commands/generator" });
            await builder.CheckToolOutputAsync(
                name: "xcode",
                fileName: "xcode-select",
                arguments: "-p");

            // Cache and return the values.
            _capabilities = builder.ToList();
            return _capabilities;
        }

        private sealed class CapabilitiesBuilder
        {
            private readonly List<Capability> _capabilities = new List<Capability>();
            private readonly CancellationToken _cancellationToken;
            private readonly IHostContext _hostContext;
            private readonly Tracing _trace;
            private readonly IWhichUtil _whichUtil;

            public CapabilitiesBuilder(IHostContext hostContext, CancellationToken cancellationToken)
            {
                ArgUtil.NotNull(hostContext, nameof(hostContext));
                _hostContext = hostContext;
                _cancellationToken = cancellationToken;
                _trace = _hostContext.GetTrace(this.GetType().Name);
                _whichUtil = _hostContext.GetService<IWhichUtil>();
            }

            public void Check(string name, string fileName = null, string[] filePaths = null)
            {
                ArgUtil.NotNullOrEmpty(name, nameof(name));
                _cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Which the file.
                    string filePath = _whichUtil.Which(fileName ?? name);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        // Fallback to the well-known locations.
                        foreach (string candidateFilePath in filePaths ?? new string[0])
                        {
                            _trace.Info($"Checking file: '{candidateFilePath}'");
                            if (File.Exists(candidateFilePath))
                            {
                                filePath = candidateFilePath;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        _trace.Info($"Adding '{name}': '{filePath}'");
                        _capabilities.Add(new Capability(name, filePath));
                    }
                }
                catch (Exception ex)
                {
                    _trace.Error(ex);
                }
            }

            public async Task CheckToolOutputAsync(string name, string fileName, string arguments)
            {
                _trace.Entering();
                ArgUtil.NotNullOrEmpty(name, nameof(name));
                ArgUtil.NotNullOrEmpty(fileName, nameof(fileName));
                try
                {
                    // Attempt to locate the tool.
                    string filePath = _whichUtil.Which(fileName);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        return;
                    }

                    // Invoke the tool and capture the output.
                    var output = new StringBuilder();
                    using (var processInvoker = _hostContext.CreateService<IProcessInvoker>())
                    {
                        processInvoker.OutputDataReceived +=
                            (object sender, ProcessDataReceivedEventArgs args) =>
                            {
                                if (!string.IsNullOrEmpty(args.Data))
                                {
                                    output.Append(args.Data);
                                }
                            };
                        await processInvoker.ExecuteAsync(
                            workingDirectory: string.Empty,
                            fileName: filePath,
                            arguments: arguments ?? string.Empty,
                            environment: null,
                            cancellationToken: _cancellationToken);
                    }

                    // Add the capability.
                    if (output.Length > 0)
                    {
                        string value = output.ToString();
                        _trace.Info($"Adding '{name}': '{value}'");
                        _capabilities.Add(new Capability(name, value));
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _trace.Error(ex);
                }
            }

            public List<Capability> ToList() => new List<Capability>(_capabilities);
        }
    }
}
