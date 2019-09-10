using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Services.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(RunnerLogPlugin))]
    public interface IRunnerLogPlugin : IRunnerService
    {
        Task StartAsync(IExecutionContext context, List<IStep> steps, CancellationToken token);
        Task WaitAsync(IExecutionContext context);
        void Write(Guid stepId, string message);
    }

    public sealed class RunnerLogPlugin : RunnerService, IRunnerLogPlugin
    {
        private readonly Guid _instanceId = Guid.NewGuid();

        private Task<int> _pluginHostProcess = null;

        private readonly Channel<string> _redirectedStdin = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });

        private readonly ConcurrentQueue<string> _outputs = new ConcurrentQueue<string>();

        private readonly Dictionary<string, PluginInfo> _logPlugins = new Dictionary<string, PluginInfo>()
        {
            {"TestResultLogPlugin",  new PluginInfo("Runner.Plugins.Log.TestResultParser.Plugin.TestResultLogPlugin, Runner.Plugins", "Test Result Parser plugin")},
            {"TestFilePublisherPlugin",  new PluginInfo("Runner.Plugins.Log.TestFilePublisher.TestFilePublisherLogPlugin, Runner.Plugins", "Test File Publisher plugin")}
        };

        private class PluginInfo
        {
            public PluginInfo(string assemblyName, string friendlyName)
            {
                AssemblyName = assemblyName;
                FriendlyName = friendlyName;
            }

            public string FriendlyName { get; set; }
            public string AssemblyName { get; set; }
        }

        public Task StartAsync(IExecutionContext context, List<IStep> steps, CancellationToken token)
        {
            Trace.Entering();
            ArgUtil.NotNull(context, nameof(context));

            List<PluginInfo> enabledPlugins = new List<PluginInfo>();
            if (context.Variables.GetBoolean("runner.disablelogplugin") ?? false)
            {
                // all log plugs are disabled
                context.Debug("All log plugins are disabled.");
            }
            else
            {
                foreach (var plugin in _logPlugins)
                {
                    if (context.Variables.GetBoolean($"runner.disablelogplugin.{plugin.Key}") ?? false)
                    {
                        // skip plugin 
                        context.Debug($"Log plugin '{plugin.Key}' is disabled.");
                        continue;
                    }
                    else
                    {
                        enabledPlugins.Add(plugin.Value);
                    }
                }
            }

            if (enabledPlugins.Count > 0)
            {
                // Resolve the working directory.
                string workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                ArgUtil.Directory(workingDirectory, nameof(workingDirectory));

                // Runner.PluginHost
                string file = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), $"Runner.PluginHost{IOUtil.ExeExtension}");
                ArgUtil.File(file, $"Runner.PluginHost{IOUtil.ExeExtension}");

                // Runner.PluginHost's arguments
                string arguments = $"log \"{_instanceId.ToString("D")}\"";

                var processInvoker = HostContext.CreateService<IProcessInvoker>();

                processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                {
                    if (e.Data != null)
                    {
                        _outputs.Enqueue(e.Data);
                    }
                };
                processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                {
                    if (e.Data != null)
                    {
                        _outputs.Enqueue(e.Data);
                    }
                };

                _pluginHostProcess = processInvoker.ExecuteAsync(workingDirectory: workingDirectory,
                                                             fileName: file,
                                                             arguments: arguments,
                                                             environment: null,
                                                             requireExitCodeZero: true,
                                                             outputEncoding: Encoding.UTF8,
                                                             killProcessOnCancel: true,
                                                             redirectStandardIn: _redirectedStdin,
                                                             inheritConsoleHandler: false,
                                                             keepStandardInOpen: true,
                                                             cancellationToken: token);

                // construct plugin context
                RunnerLogPluginHostContext pluginContext = new RunnerLogPluginHostContext
                {
                    PluginAssemblies = new List<string>(),
                    Endpoints = context.Endpoints,
                    Variables = new Dictionary<string, VariableValue>(),
                    Steps = new Dictionary<string, Pipelines.ActionStepDefinitionReference>(),
                    Context = context.ExpressionValues
                };

                // plugins 
                pluginContext.PluginAssemblies.AddRange(_logPlugins.Values.Select(x => x.AssemblyName));

                // variables
                foreach (var variable in context.Variables.AllVariables)
                {
                    pluginContext.Variables[variable.Name] = new VariableValue(variable.Value, variable.Secret);
                }

                // steps
                foreach (var step in steps)
                {
                    var taskStep = step as IActionRunner;
                    if (taskStep != null)
                    {
                        pluginContext.Steps[taskStep.ExecutionContext.Id.ToString("D")] = taskStep.Action.Reference;
                    }
                }

                Trace.Info("Send serialized context through STDIN");
                _redirectedStdin.Writer.TryWrite(JsonUtility.ToString(pluginContext));

                foreach (var plugin in _logPlugins)
                {
                    context.Output($"Plugin: '{plugin.Value.FriendlyName}' is running in background.");
                }
            }

            return Task.CompletedTask;
        }

        public async Task WaitAsync(IExecutionContext context)
        {
            Trace.Entering();

            if (_pluginHostProcess != null)
            {
                Trace.Info("Send instruction code through STDIN to stop plugin host");

                // plugin host will stop the routine process and give every plugin a chance to participate into job finalization
                _redirectedStdin.Writer.TryWrite($"##vso[logplugin.finish]{_instanceId.ToString("D")}");

                // print out outputs from plugin host and wait for plugin finish
                Trace.Info("Waiting for plugin host exit");
                foreach (var plugin in _logPlugins)
                {
                    context.Debug($"Waiting for log plugin '{plugin.Value.FriendlyName}' to finish.");
                }

                while (!_pluginHostProcess.IsCompleted)
                {
                    while (_outputs.TryDequeue(out string output))
                    {
                        if (output.StartsWith(Constants.PluginTracePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            Trace.Info(output.Substring(Constants.PluginTracePrefix.Length));
                        }
                        else
                        {
                            context.Output(output);
                        }
                    }

                    await Task.WhenAny(Task.Delay(250), _pluginHostProcess);
                }

                // try process output queue again, in case we have buffered outputs haven't process on process exit
                while (_outputs.TryDequeue(out string output))
                {
                    if (output.StartsWith(Constants.PluginTracePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.Info(output.Substring(Constants.PluginTracePrefix.Length));
                    }
                    else
                    {
                        context.Output(output);
                    }
                }

                await _pluginHostProcess;
            }
        }

        public void Write(Guid stepId, string message)
        {
            if (_pluginHostProcess != null && message != null)
            {
                var lines = message.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (line != null)
                    {
                        _redirectedStdin.Writer.TryWrite($"{stepId}:{line}");
                    }
                }
            }
        }
    }
}
