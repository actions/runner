﻿using GitHub.DistributedTask.WebApi;
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
        private readonly Dictionary<string, RunnerPluginActionInfo> _actionPlugins = new Dictionary<string, RunnerPluginActionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "checkout",
                new RunnerPluginActionInfo()
                {
                    Description = "Get sources from a Git repository",
                    FriendlyName = "Get sources",
                    PluginTypeName = "GitHub.Runner.Plugins.Repository.CheckoutTask, Runner.Plugins"
                }
            },
            {
                "publish",
                new RunnerPluginActionInfo()
                {
                    PluginTypeName = "GitHub.Runner.Plugins.PipelineArtifact.PublishPipelineArtifact, Runner.Plugins"
                }
            },
            {
                "download",
                new RunnerPluginActionInfo()
                {
                    PluginTypeName = "GitHub.Runner.Plugins.PipelineArtifact.DownloadPipelineArtifact, Runner.Plugins"
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
            if (!_actionPlugins.Any(x => x.Value.PluginTypeName == plugin))
            {
                throw new NotSupportedException(plugin);
            }

            // Resolve the working directory.
            string workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.Directory(workingDirectory, nameof(workingDirectory));

            // Runner.PluginHost
            string file = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), $"Runner.PluginHost{IOUtil.ExeExtension}");
            ArgUtil.File(file, $"Runner.PluginHost{IOUtil.ExeExtension}");

            // Runner.PluginHost's arguments
            string arguments = $"action \"{plugin}\"";

            // construct plugin context
            RunnerActionPluginExecutionContext pluginContext = new RunnerActionPluginExecutionContext
            {
                Inputs = inputs,
                Endpoints = context.Endpoints,
                Context = context.ExpressionValues as Dictionary<string, PipelineContextData>
            };

            // variables
            foreach (var variable in context.Variables.AllVariables)
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
    }
}
