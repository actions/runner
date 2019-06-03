using GitHub.DistributedTask.WebApi;
using Runner.Sdk;
using Runner.Common.Util;
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

namespace Runner.Common.Worker
{
    [ServiceLocator(Default = typeof(AgentPluginManager))]
    public interface IAgentPluginManager : IAgentService
    {
        AgentTaskPluginInfo GetPluginTask(Guid taskId, string taskVersion);
        AgentActionPluginInfo GetPluginAction(string plugin);
        Task RunPluginTaskAsync(IExecutionContext context, string plugin, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, EventHandler<ProcessDataReceivedEventArgs> outputHandler);
    }

    public sealed class AgentPluginManager : AgentService, IAgentPluginManager
    {
        private readonly Dictionary<Guid, Dictionary<string, AgentTaskPluginInfo>> _supportedTasks = new Dictionary<Guid, Dictionary<string, AgentTaskPluginInfo>>();

        private readonly HashSet<string> _taskPlugins = new HashSet<string>()
        {
            "Runner.Plugins.Repository.CheckoutTask, Runner.Plugins"
        };

        private readonly Dictionary<string, AgentActionPluginInfo> _actionPlugins = new Dictionary<string, AgentActionPluginInfo>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "checkout",
                new AgentActionPluginInfo()
                {
                    Author = "GitHub",
                    Description = "Get sources from a Git repository",
                    FriendlyName = "Get sources",
                    PluginTypeName = "Runner.Plugins.Repository.CheckoutTask, Runner.Plugins"
                }
            }
        };

        private readonly HashSet<string> _commandPlugins = new HashSet<string>();

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            // Load task plugins
            foreach (var pluginTypeName in _taskPlugins)
            {
                IAgentTaskPlugin taskPlugin = null;
                AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                try
                {
                    Trace.Info($"Load task plugin from '{pluginTypeName}'.");
                    Type type = Type.GetType(pluginTypeName, throwOnError: true);
                    taskPlugin = Activator.CreateInstance(type) as IAgentTaskPlugin;
                }
                finally
                {
                    AssemblyLoadContext.Default.Resolving -= ResolveAssembly;
                }

                ArgUtil.NotNull(taskPlugin, nameof(taskPlugin));
                ArgUtil.NotNull(taskPlugin.Id, nameof(taskPlugin.Id));
                ArgUtil.NotNullOrEmpty(taskPlugin.Version, nameof(taskPlugin.Version));
                ArgUtil.NotNullOrEmpty(taskPlugin.Stage, nameof(taskPlugin.Stage));
                if (!_supportedTasks.ContainsKey(taskPlugin.Id))
                {
                    _supportedTasks[taskPlugin.Id] = new Dictionary<string, AgentTaskPluginInfo>(StringComparer.OrdinalIgnoreCase);
                }

                if (!_supportedTasks[taskPlugin.Id].ContainsKey(taskPlugin.Version))
                {
                    _supportedTasks[taskPlugin.Id][taskPlugin.Version] = new AgentTaskPluginInfo();
                }

                Trace.Info($"Loaded task plugin id '{taskPlugin.Id}' ({taskPlugin.Version}) ({taskPlugin.Stage}).");
                if (taskPlugin.Stage == "pre")
                {
                    _supportedTasks[taskPlugin.Id][taskPlugin.Version].TaskPluginPreJobTypeName = pluginTypeName;
                }
                else if (taskPlugin.Stage == "main")
                {
                    _supportedTasks[taskPlugin.Id][taskPlugin.Version].TaskPluginTypeName = pluginTypeName;
                }
                else if (taskPlugin.Stage == "post")
                {
                    _supportedTasks[taskPlugin.Id][taskPlugin.Version].TaskPluginPostJobTypeName = pluginTypeName;
                }
            }
        }

        public AgentActionPluginInfo GetPluginAction(string plugin)
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

        public AgentTaskPluginInfo GetPluginTask(Guid taskId, string taskVersion)
        {
            if (_supportedTasks.ContainsKey(taskId) && _supportedTasks[taskId].ContainsKey(taskVersion))
            {
                return _supportedTasks[taskId][taskVersion];
            }
            else
            {
                return null;
            }
        }

        public async Task RunPluginTaskAsync(IExecutionContext context, string plugin, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, EventHandler<ProcessDataReceivedEventArgs> outputHandler)
        {
            ArgUtil.NotNullOrEmpty(plugin, nameof(plugin));

            // Only allow plugins we defined
            if (!_taskPlugins.Contains(plugin))
            {
                throw new NotSupportedException(plugin);
            }

            // Resolve the working directory.
            string workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.Directory(workingDirectory, nameof(workingDirectory));

            // Runner.PluginHost
            string file = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), $"Runner.PluginHost{Util.IOUtil.ExeExtension}");
            ArgUtil.File(file, $"Runner.PluginHost{Util.IOUtil.ExeExtension}");

            // Runner.PluginHost's arguments
            string arguments = $"task \"{plugin}\"";

            // construct plugin context
            AgentTaskPluginExecutionContext pluginContext = new AgentTaskPluginExecutionContext
            {
                Inputs = inputs,
                Repositories = context.Repositories,
                Endpoints = context.Endpoints,
                Context = context.ExpressionValues as Dictionary<string, PipelineContextData>
            };

            // // environment
            // foreach(var env in environment)
            // {
            //     pluginContext.Environment[env.Key] = env.Value;
            // }
            // variables
            // foreach (var publicVar in runtimeVariables.Public)
            // {
            //     pluginContext.Variables[publicVar.Key] = publicVar.Value;
            // }
            // foreach (var publicVar in runtimeVariables.Private)
            // {
            //     pluginContext.Variables[publicVar.Key] = new VariableValue(publicVar.Value, true);
            // }
            // // task variables (used by wrapper task)
            // foreach (var publicVar in context.TaskVariables.Public)
            // {
            //     pluginContext.TaskVariables[publicVar.Key] = publicVar.Value;
            // }
            // foreach (var publicVar in context.TaskVariables.Private)
            // {
            //     pluginContext.TaskVariables[publicVar.Key] = new VariableValue(publicVar.Value, true);
            // }

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

    public class AgentCommandPluginInfo
    {
        public string CommandPluginTypeName { get; set; }
        public string DisplayName { get; set; }
    }

    public class AgentTaskPluginInfo
    {
        public string TaskPluginPreJobTypeName { get; set; }
        public string TaskPluginTypeName { get; set; }
        public string TaskPluginPostJobTypeName { get; set; }
    }

    public class AgentActionPluginInfo
    {
        public string Author { get; set; }
        public string Description { get; set; }
        public string FriendlyName { get; set; }
        public string HelpUrl { get; set; }
        public string PluginTypeName { get; set; }
    }
}
