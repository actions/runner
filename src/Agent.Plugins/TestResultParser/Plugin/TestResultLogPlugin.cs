using System;
using System.Linq;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Plugins.Log.TestResultParser.Plugin;
using Agent.Plugins.TestResultParser.Plugin;
using Agent.Sdk;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Plugins.Log
{
    public class TestResultLogPlugin : IAgentLogPlugin
    {
        /// <inheritdoc />
        public string FriendlyName => "Test Result Log Parser";

        /// <inheritdoc />
        public async Task<bool> InitializeAsync(IAgentLogPluginContext context)
        {
            try
            {
                _logger = new TraceLogger(context);
                _clientFactory = new ClientFactory(context.VssConnection);

                PopulatePipelineConfig(context);

                if (CheckForPluginDisable(context))
                {
                    return false; // disable the plugin
                }

                await InputDataParser.InitializeAsync(_clientFactory, _pipelineConfig, _logger);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Unable to initialize {FriendlyName}");
                context.Trace(ex.ToString());
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task ProcessLineAsync(IAgentLogPluginContext context, Pipelines.TaskStepDefinitionReference step, string line)
        {
            await InputDataParser.ProcessDataAsync(line);
        }

        /// <inheritdoc />
        public async Task FinalizeAsync(IAgentLogPluginContext context)
        {
            await InputDataParser.CompleteAsync();
        }

        private bool CheckForPluginDisable(IAgentLogPluginContext context)
        {
            // Enable only for build
            if (context.Variables.TryGetValue("system.hosttype", out var hostType)
                && !string.Equals("Build", hostType.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Disable for on-prem
            if (context.Variables.TryGetValue("system.servertype", out var serverType)
                && !string.Equals("Hosted", serverType.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // check for PTR task or some other tasks to enable/disable
            return context.Steps == null
                   || context.Steps.Any(x => x.Id.Equals(new Guid("0B0F01ED-7DDE-43FF-9CBB-E48954DAF9B1")))
                   || _pipelineConfig.BuildId == 0;
        }

        private void PopulatePipelineConfig(IAgentLogPluginContext context)
        {
            if (context.Variables.TryGetValue("system.teamProjectId", out var projectGuid))
            {
                _pipelineConfig.Project = new Guid(projectGuid.Value);
            }

            if (context.Variables.TryGetValue("build.buildId", out var buildId))
            {
                _pipelineConfig.BuildId = int.Parse(buildId.Value);
            }
        }

        public ILogParserGateway InputDataParser { get; set; } = new LogParserGateway(); //for testing purpose
        private IClientFactory _clientFactory;
        private ITraceLogger _logger;
        private readonly IPipelineConfig _pipelineConfig = new PipelineConfig();
    }
}
