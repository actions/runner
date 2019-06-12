using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Sdk
{
    public interface IRunnerLogPlugin
    {
        // Short meaningful name for the plugin.
        // Any outputs from the pluging will be prefixed with the name.
        string FriendlyName { get; }

        // Get call when plugin host load up all plugins for the first time.
        // return `False` will tells the plugin host not longer forward log line to the plugin
        Task<bool> InitializeAsync(IRunnerLogPluginContext context);

        // Get called by plugin host on every log line.
        Task ProcessLineAsync(IRunnerLogPluginContext context, Pipelines.ActionStepDefinitionReference step, string line);

        // Get called by plugin host when all step execute finish.
        Task FinalizeAsync(IRunnerLogPluginContext context);
    }

    public interface IRunnerLogPluginTrace
    {
        // agent log
        void Trace(string message);

        // user log (job log)
        void Output(string message);
    }

    public interface IRunnerLogPluginContext
    {
        // default SystemConnection back to service use the job oauth token
        VssConnection VssConnection { get; }

        // task info for all steps
        IList<Pipelines.ActionStepDefinitionReference> Steps { get; }

        // all endpoints
        IList<ServiceEndpoint> Endpoints { get; }

        // all variables
        IDictionary<string, VariableValue> Variables { get; }

        // all context
        Dictionary<String, PipelineContextData> Context { get; }

        // agent log
        void Trace(string message);

        // user log (job log)
        void Output(string message);
    }

    public class RunnerLogPluginTrace : IRunnerLogPluginTrace
    {
        // agent log
        public void Trace(string message)
        {
            Console.WriteLine($"##[plugin.trace]{message}");
        }

        // user log (job log)
        public void Output(string message)
        {
            Console.WriteLine(message);
        }
    }

    public class RunnerLogPluginContext : IRunnerLogPluginContext
    {
        private string _pluginName;
        private IRunnerLogPluginTrace _trace;


        // default SystemConnection back to service use the job oauth token
        public VssConnection VssConnection { get; }

        // task info for all steps
        public IList<Pipelines.ActionStepDefinitionReference> Steps { get; }

        // all endpoints
        public IList<ServiceEndpoint> Endpoints { get; }

        // all variables
        public IDictionary<string, VariableValue> Variables { get; }

        // all context
        public Dictionary<String, PipelineContextData> Context { get; set; }

        public RunnerLogPluginContext(
            string pluginNme,
            VssConnection connection,
            IList<Pipelines.ActionStepDefinitionReference> steps,
            IList<ServiceEndpoint> endpoints,
            IDictionary<string, VariableValue> variables,
            Dictionary<String, PipelineContextData> Context,
            IRunnerLogPluginTrace trace)
        {
            _pluginName = pluginNme;
            VssConnection = connection;
            Steps = steps;
            Endpoints = endpoints;
            Variables = variables;
            _trace = trace;
        }

        // agent log
        public void Trace(string message)
        {
            _trace.Trace($"{_pluginName}: {message}");
        }

        // user log (job log)
        public void Output(string message)
        {
            _trace.Output($"{_pluginName}: {message}");
        }
    }

    public class RunnerLogPluginHostContext
    {
        private VssConnection _connection;

        public List<String> PluginAssemblies { get; set; }
        public List<ServiceEndpoint> Endpoints { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        public Dictionary<String, PipelineContextData> Context { get; set; }
        public Dictionary<string, Pipelines.ActionStepDefinitionReference> Steps { get; set; }

        [JsonIgnore]
        public VssConnection VssConnection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = InitializeVssConnection();
                }
                return _connection;
            }
        }

        private VssConnection InitializeVssConnection()
        {
            var headerValues = new List<ProductInfoHeaderValue>();
            headerValues.Add(new ProductInfoHeaderValue($"GitHubActionsRunner-Plugin", BuildConstants.RunnerPackage.Version));
            headerValues.Add(new ProductInfoHeaderValue($"({RuntimeInformation.OSDescription.Trim()})"));

            if (VssClientHttpRequestSettings.Default.UserAgent != null && VssClientHttpRequestSettings.Default.UserAgent.Count > 0)
            {
                headerValues.AddRange(VssClientHttpRequestSettings.Default.UserAgent);
            }

            VssClientHttpRequestSettings.Default.UserAgent = headerValues;

            var certSetting = GetCertConfiguration();
            if (certSetting != null)
            {
                if (!string.IsNullOrEmpty(certSetting.ClientCertificateArchiveFile))
                {
                    VssClientHttpRequestSettings.Default.ClientCertificateManager = new RunnerClientCertificateManager(certSetting.ClientCertificateArchiveFile, certSetting.ClientCertificatePassword);
                }

                if (certSetting.SkipServerCertificateValidation)
                {
                    VssClientHttpRequestSettings.Default.ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
            }

            var proxySetting = GetProxyConfiguration();
            if (proxySetting != null)
            {
                if (!string.IsNullOrEmpty(proxySetting.ProxyAddress))
                {
                    VssHttpMessageHandler.DefaultWebProxy = new RunnerWebProxyCore(proxySetting.ProxyAddress, proxySetting.ProxyUsername, proxySetting.ProxyPassword, proxySetting.ProxyBypassList);
                }
            }

            ServiceEndpoint systemConnection = this.Endpoints.FirstOrDefault(e => string.Equals(e.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(systemConnection, nameof(systemConnection));
            ArgUtil.NotNull(systemConnection.Url, nameof(systemConnection.Url));

            VssCredentials credentials = VssUtil.GetVssCredential(systemConnection);
            ArgUtil.NotNull(credentials, nameof(credentials));
            return VssUtil.CreateConnection(systemConnection.Url, credentials);
        }

        public String GetRunnerContext(string contextName)
        {
            this.Context.TryGetValue("runner", out var context);
            var runnerContext = context as DictionaryContextData;
            ArgUtil.NotNull(runnerContext, nameof(runnerContext));
            if (runnerContext.TryGetValue(contextName, out var data))
            {
                return data as StringContextData;
            }
            else
            {
                return null;
            }
        }

        private RunnerCertificateSettings GetCertConfiguration()
        {
            bool skipCertValidation = StringUtil.ConvertToBoolean(GetRunnerContext("SkipCertValidation"));
            string caFile = GetRunnerContext("CAInfo");
            string clientCertFile = GetRunnerContext("ClientCert");

            if (!string.IsNullOrEmpty(caFile) || !string.IsNullOrEmpty(clientCertFile) || skipCertValidation)
            {
                var certConfig = new RunnerCertificateSettings();
                certConfig.SkipServerCertificateValidation = skipCertValidation;
                certConfig.CACertificateFile = caFile;

                if (!string.IsNullOrEmpty(clientCertFile))
                {
                    certConfig.ClientCertificateFile = clientCertFile;
                    string clientCertKey = GetRunnerContext("ClientCertKey");
                    string clientCertArchive = GetRunnerContext("ClientCertArchive");
                    string clientCertPassword = GetRunnerContext("ClientCertPassword");

                    certConfig.ClientCertificatePrivateKeyFile = clientCertKey;
                    certConfig.ClientCertificateArchiveFile = clientCertArchive;
                    certConfig.ClientCertificatePassword = clientCertPassword;

                    certConfig.VssClientCertificateManager = new RunnerClientCertificateManager(clientCertArchive, clientCertPassword);
                }

                return certConfig;
            }
            else
            {
                return null;
            }
        }

        private RunnerWebProxySettings GetProxyConfiguration()
        {
            string proxyUrl = GetRunnerContext("ProxyUrl");
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                string proxyUsername = GetRunnerContext("ProxyUsername");
                string proxyPassword = GetRunnerContext("ProxyPassword");
                List<string> proxyBypassHosts = StringUtil.ConvertFromJson<List<string>>(GetRunnerContext("ProxyBypassList") ?? "[]");
                return new RunnerWebProxySettings()
                {
                    ProxyAddress = proxyUrl,
                    ProxyUsername = proxyUsername,
                    ProxyPassword = proxyPassword,
                    ProxyBypassList = proxyBypassHosts,
                    WebProxy = new RunnerWebProxyCore(proxyUrl, proxyUsername, proxyPassword, proxyBypassHosts)
                };
            }
            else
            {
                return null;
            }
        }
    }

    public class RunnerLogPluginHost
    {
        private readonly TaskCompletionSource<int> _jobFinished = new TaskCompletionSource<int>();
        private readonly Dictionary<string, ConcurrentQueue<string>> _outputQueue = new Dictionary<string, ConcurrentQueue<string>>();
        private readonly Dictionary<string, IRunnerLogPluginContext> _pluginContexts = new Dictionary<string, IRunnerLogPluginContext>();
        private readonly Dictionary<string, TaskCompletionSource<int>> _shortCircuited = new Dictionary<string, TaskCompletionSource<int>>();
        private Dictionary<string, Pipelines.ActionStepDefinitionReference> _steps;
        private List<IRunnerLogPlugin> _plugins;
        private IRunnerLogPluginTrace _trace;
        private int _shortCircuitThreshold;
        private int _shortCircuitMonitorFrequency;

        public RunnerLogPluginHost(
            RunnerLogPluginHostContext hostContext,
            List<IRunnerLogPlugin> plugins,
            IRunnerLogPluginTrace trace = null,
            int shortCircuitThreshold = 1000, // output queue depth >= 1000 lines
            int shortCircuitMonitorFrequency = 10000) // check all output queues every 10 sec
        {
            _steps = hostContext.Steps;
            _plugins = plugins;
            _trace = trace ?? new RunnerLogPluginTrace();
            _shortCircuitThreshold = shortCircuitThreshold;
            _shortCircuitMonitorFrequency = shortCircuitMonitorFrequency;

            foreach (var plugin in _plugins)
            {
                string typeName = plugin.GetType().FullName;
                _outputQueue[typeName] = new ConcurrentQueue<string>();
                _pluginContexts[typeName] = new RunnerLogPluginContext(plugin.FriendlyName, hostContext.VssConnection, hostContext.Steps.Values.ToList(), hostContext.Endpoints, hostContext.Variables, hostContext.Context, _trace);
                _shortCircuited[typeName] = new TaskCompletionSource<int>();
            }
        }

        public async Task Run()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            using (CancellationTokenSource monitorSource = new CancellationTokenSource())
            {
                Task memoryUsageMonitor = StartMemoryUsageMonitor(monitorSource.Token);

                Dictionary<string, Task> processTasks = new Dictionary<string, Task>();
                foreach (var plugin in _plugins)
                {
                    // start process plugins background
                    _trace.Trace($"Start process task for plugin '{plugin.FriendlyName}'");
                    var task = RunAsync(plugin, tokenSource.Token);
                    processTasks[plugin.FriendlyName] = task;
                }

                // waiting for job finish event
                await _jobFinished.Task;
                tokenSource.Cancel();

                _trace.Trace($"Wait for all plugins finish process outputs.");
                foreach (var task in processTasks)
                {
                    try
                    {
                        await task.Value;
                        _trace.Trace($"Plugin '{task.Key}' finished log process.");
                    }
                    catch (Exception ex)
                    {
                        _trace.Output($"Plugin '{task.Key}' failed with: {ex}");
                    }
                }

                // Stop monitor
                monitorSource.Cancel();
                await memoryUsageMonitor;

                // job has finished, all log plugins should start their finalize process
                Dictionary<string, Task> finalizeTasks = new Dictionary<string, Task>();
                foreach (var plugin in _plugins)
                {
                    string typeName = plugin.GetType().FullName;
                    if (!_shortCircuited[typeName].Task.IsCompleted)
                    {
                        _trace.Trace($"Start finalize for plugin '{plugin.FriendlyName}'");
                        var finalize = plugin.FinalizeAsync(_pluginContexts[typeName]);
                        finalizeTasks[plugin.FriendlyName] = finalize;
                    }
                    else
                    {
                        _trace.Trace($"Skip finalize for short circuited plugin '{plugin.FriendlyName}'");
                    }
                }

                _trace.Trace($"Wait for all plugins finish finalization.");
                foreach (var task in finalizeTasks)
                {
                    try
                    {
                        await task.Value;
                        _trace.Trace($"Plugin '{task.Key}' finished job finalize.");
                    }
                    catch (Exception ex)
                    {
                        _trace.Output($"Plugin '{task.Key}' failed with: {ex}");
                    }
                }

                _trace.Trace($"All plugins finished finalization.");
            }
        }

        public void EnqueueOutput(string output)
        {
            if (output != null)
            {
                foreach (var plugin in _plugins)
                {
                    string typeName = plugin.GetType().FullName;
                    if (!_shortCircuited[typeName].Task.IsCompleted)
                    {
                        _outputQueue[typeName].Enqueue(output);
                    }
                }
            }
        }

        public void Finish()
        {
            _trace.Trace("Job has finished, start shutting down log output processing process.");
            _jobFinished.TrySetResult(0);
        }

        private async Task StartMemoryUsageMonitor(CancellationToken token)
        {
            Dictionary<string, Int32> pluginViolateFlags = new Dictionary<string, int>();
            foreach (var queue in _outputQueue)
            {
                pluginViolateFlags[queue.Key] = 0;
            }

            _trace.Trace($"Start output buffer monitor.");
            while (!token.IsCancellationRequested)
            {
                foreach (var queue in _outputQueue)
                {
                    string pluginName = queue.Key;
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (queue.Value.Count > _shortCircuitThreshold)
                    {
                        _trace.Trace($"Plugin '{pluginName}' has too many buffered outputs.");
                        pluginViolateFlags[pluginName]++;
                        if (pluginViolateFlags[pluginName] >= 10)
                        {
                            _trace.Trace($"Short circuit plugin '{pluginName}'.");
                            _shortCircuited[pluginName].TrySetResult(0);
                        }
                    }
                    else if (pluginViolateFlags[pluginName] > 0)
                    {
                        _trace.Trace($"Plugin '{pluginName}' has cleared out buffered outputs.");
                        pluginViolateFlags[pluginName] = 0;
                    }
                }

                await Task.WhenAny(Task.Delay(_shortCircuitMonitorFrequency), Task.Delay(-1, token));
            }

            _trace.Trace($"Output buffer monitor stopped.");
        }

        private async Task RunAsync(IRunnerLogPlugin plugin, CancellationToken token)
        {
            List<string> errors = new List<string>();
            string typeName = plugin.GetType().FullName;
            var context = _pluginContexts[typeName];

            bool initialized = false;
            try
            {
                initialized = await plugin.InitializeAsync(context);
            }
            catch (Exception ex)
            {
                errors.Add($"Fail to initialize: {ex.Message}.");
                context.Trace(ex.ToString());
            }
            finally
            {
                if (!initialized)
                {
                    context.Trace("Skip process outputs base on plugin initialize result.");
                    _shortCircuited[typeName].TrySetResult(0);
                }
            }

            using (var registration = token.Register(() =>
                                      {
                                          var depth = _outputQueue[typeName].Count;
                                          if (depth > 0)
                                          {
                                              context.Output($"Waiting for log plugin to finish, pending process {depth} log lines.");
                                          }
                                      }))
            {
                while (!_shortCircuited[typeName].Task.IsCompleted && !token.IsCancellationRequested)
                {
                    await ProcessOutputQueue(context, plugin, errors);

                    // back-off before pull output queue again.
                    await Task.Delay(500);
                }
            }

            // process all remaining outputs
            context.Trace("Process remaining outputs after job finished.");
            await ProcessOutputQueue(context, plugin, errors);

            // print out the plugin has been short circuited.
            if (_shortCircuited[typeName].Task.IsCompleted)
            {
                if (initialized)
                {
                    context.Output($"Plugin has been short circuited due to exceed memory usage limit.");
                }

                _outputQueue[typeName].Clear();
            }

            // print out error to user.
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    context.Output($"Fail to process output: {error}");
                }
            }
        }

        private async Task ProcessOutputQueue(IRunnerLogPluginContext context, IRunnerLogPlugin plugin, List<string> errors)
        {
            string typeName = plugin.GetType().FullName;
            while (!_shortCircuited[typeName].Task.IsCompleted && _outputQueue[typeName].TryDequeue(out string line))
            {
                try
                {
                    var id = line.Substring(0, line.IndexOf(":"));
                    var message = line.Substring(line.IndexOf(":") + 1);
                    var processLineTask = plugin.ProcessLineAsync(context, _steps[id], message);
                    var completedTask = await Task.WhenAny(_shortCircuited[typeName].Task, processLineTask);
                    if (completedTask == processLineTask)
                    {
                        await processLineTask;
                    }
                }
                catch (Exception ex)
                {
                    // ignore exception
                    // only trace the first 10 errors.
                    if (errors.Count < 10)
                    {
                        errors.Add($"{ex} '(line)'");
                    }
                }
            }
        }
    }
}
