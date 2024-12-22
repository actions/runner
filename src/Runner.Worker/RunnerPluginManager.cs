using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Runner.Common.Util;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using GitHub.DistributedTask.Pipelines.ContextData;
using System.Threading.Channels;
using GitHub.Runner.Common;
using System.Linq;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(RunnerPluginManager))]
    public interface IRunnerPluginManager : IRunnerService
    {
        RunnerPluginActionInfo GetPluginAction(string plugin);
        Task RunPluginActionAsync(IExecutionContext context, string plugin, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, EventHandler<ProcessDataReceivedEventArgs> outputHandler);
    }

    public sealed class RunnerPluginManager : RunnerService, IRunnerPluginManager
    {
        private readonly Dictionary<string, RunnerPluginActionInfo> _actionPlugins = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "checkout",
                new RunnerPluginActionInfo()
                {
                    Description = "Get sources from a Git repository",
                    FriendlyName = "Get sources",
                    PluginTypeName = "GitHub.Runner.Plugins.Repository.v1_0.CheckoutTask, Runner.Plugins"
                }
            },
            {
                "checkoutV1_1",
                new RunnerPluginActionInfo()
                {
                    Description = "Get sources from a Git repository",
                    FriendlyName = "Get sources",
                    PluginTypeName = "GitHub.Runner.Plugins.Repository.v1_1.CheckoutTask, Runner.Plugins",
                    PostPluginTypeName = "GitHub.Runner.Plugins.Repository.v1_1.CleanupTask, Runner.Plugins"
                }
            },
            {
                "publish",
                new RunnerPluginActionInfo()
                {
                    PluginTypeName = "GitHub.Runner.Plugins.Artifact.PublishArtifact, Runner.Plugins"
                }
            },
            {
                "download",
                new RunnerPluginActionInfo()
                {
                    PluginTypeName = "GitHub.Runner.Plugins.Artifact.DownloadArtifact, Runner.Plugins"
                }
            }
        };

        public RunnerPluginActionInfo GetPluginAction(string plugin)
        {
            if (_actionPlugins.ContainsKey(plugin))
            {
                return _actionPlugins[plugin];
            }
            else
            {
                return null;
            }
        }

        public async Task RunPluginActionAsync(IExecutionContext context, string plugin, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, EventHandler<ProcessDataReceivedEventArgs> outputHandler)
        {
            ArgUtil.NotNullOrEmpty(plugin, nameof(plugin));

            // Only allow plugins we defined
            if (!_actionPlugins.Any(x => x.Value.PluginTypeName == plugin || x.Value.PostPluginTypeName == plugin))
            {
                throw new NotSupportedException(plugin);
            }

            // Resolve the working directory.
            string workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.Directory(workingDirectory, nameof(workingDirectory));

            string file;

            // Runner.PluginHost's arguments
            string arguments = $"action \"{plugin}\"";

            var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if(string.IsNullOrWhiteSpace(binpath)) {
                file = Environment.ProcessPath;
            } else {
                // Runner.PluginHost
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                string ext = ".dll";
#else
                string ext = IOUtil.ExeExtension;
#endif
                file = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), $"Runner.PluginHost{ext}");
                ArgUtil.File(file, $"Runner.PluginHost{ext}");

#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                var dotnet = global::Sdk.Utils.DotNetMuxer.MuxerPath ?? WhichUtil.Which("dotnet", true);
                arguments = $"\"{file}\" {arguments}";
                file = dotnet;
#endif
            }
            // construct plugin context
            RunnerActionPluginExecutionContext pluginContext = new()
            {
                Inputs = inputs,
                Endpoints = context.Global.Endpoints,
                Context = context.ExpressionValues
            };

            // variables
            foreach (var variable in context.Global.Variables.AllVariables)
            {
                pluginContext.Variables[variable.Name] = new VariableValue(variable.Value, variable.Secret);
            }

            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                var redirectStandardIn = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
                redirectStandardIn.Writer.TryWrite(JsonUtility.ToString(pluginContext));

                processInvoker.OutputDataReceived += outputHandler;
                processInvoker.ErrorDataReceived += outputHandler;

                // Execute the process. Exit code 0 should always be returned.
                // A non-zero exit code indicates infrastructural failure.
                // Task failure should be communicated over STDOUT using ## commands.
                await processInvoker.ExecuteAsync(workingDirectory: workingDirectory,
                                                  fileName: file,
                                                  arguments: arguments,
                                                  environment: environment,
                                                  requireExitCodeZero: true,
                                                  outputEncoding: Encoding.UTF8,
                                                  killProcessOnCancel: false,
                                                  redirectStandardIn: redirectStandardIn,
                                                  cancellationToken: context.CancellationToken);
            }
        }
        private Assembly ResolveAssembly(AssemblyLoadContext context, AssemblyName assembly)
        {
            string assemblyFilename = assembly.Name + ".dll";
            return context.LoadFromAssemblyPath(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), assemblyFilename));
        }
    }

    public class RunnerPluginActionInfo
    {
        public string Description { get; set; }
        public string FriendlyName { get; set; }
        public string PluginTypeName { get; set; }
        public string PostPluginTypeName { get; set; }
    }
}
