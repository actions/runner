using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Sdk;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Plugins.Log.TestFilePublisher
{
    public class TestFilePublisherLogPlugin : IAgentLogPlugin
    {
        /// <inheritdoc />
        public string FriendlyName => "TestFilePublisher";

        public TestFilePublisherLogPlugin()
        {
            // Default constructor
        }

        /// <summary>
        /// For UTs only
        /// </summary>
        public TestFilePublisherLogPlugin(ITraceLogger logger, ITelemetryDataCollector telemetry, ITestFilePublisher testFilePublisher)
        {
            _logger = logger;
            _telemetry = telemetry;
            _testFilePublisher = testFilePublisher;
        }

        /// <inheritdoc />
        public async Task<bool> InitializeAsync(IAgentLogPluginContext context)
        {
            try
            {
                _logger = _logger ?? new TraceLogger(context);
                _telemetry = _telemetry ?? new TelemetryDataCollector(new ClientFactory(context.VssConnection), _logger);

                await PopulatePipelineConfig(context);

                if (DisablePlugin(context))
                {
                    _telemetry.AddOrUpdate(TelemetryConstants.PluginDisabled, true);
                    await _telemetry.PublishCumulativeTelemetryAsync();
                    return false; // disable the plugin
                }

                _testFilePublisher = _testFilePublisher ??
                                     new TestFilePublisher(context.VssConnection, PipelineConfig, new TestFileTraceListener(context), _logger, _telemetry);
                await _testFilePublisher.InitializeAsync();
                _telemetry.AddOrUpdate(TelemetryConstants.PluginInitialized, true);
            }
            catch (Exception ex)
            {
                context.Trace(ex.ToString());
                _logger?.Warning($"Unable to initialize {FriendlyName}.");
                if (_telemetry != null)
                {
                    _telemetry.AddOrUpdate(TelemetryConstants.PluginDisabled, true);
                    _telemetry.AddOrUpdate(TelemetryConstants.InitializeFailed, ex);
                    await _telemetry.PublishCumulativeTelemetryAsync();
                }
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task ProcessLineAsync(IAgentLogPluginContext context, Pipelines.TaskStepDefinitionReference step, string line)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task FinalizeAsync(IAgentLogPluginContext context)
        {
            using (var timer = new SimpleTimer("Finalize", _logger, TimeSpan.FromMinutes(2),
                new TelemetryDataWrapper(_telemetry, TelemetryConstants.FinalizeAsync)))
            {
                try
                {
                    await _testFilePublisher.PublishAsync();
                }
                catch (Exception ex)
                {
                    _logger.Info($"Error: {ex}");
                    _telemetry.AddOrUpdate("FailedToPublishTestRuns", ex);
                }
            }

            await _telemetry.PublishCumulativeTelemetryAsync();
        }

        /// <summary>
        /// Return true if plugin needs to be disabled
        /// </summary>
        private bool DisablePlugin(IAgentLogPluginContext context)
        {
            // do we want to log that the plugin is disabled due to x reason here?
            if (context.Variables.TryGetValue("Agent.ForceEnable.TestFilePublisherLogPlugin", out var forceEnable)
                && string.Equals("true", forceEnable.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Enable only for build
            if (!context.Variables.TryGetValue("system.hosttype", out var hostType)
                || !string.Equals("Build", hostType.Value, StringComparison.OrdinalIgnoreCase))
            {
                _telemetry.AddOrUpdate("PluginDisabledReason", hostType?.Value);
                return true;
            }

            // Disable for on-prem
            if (!context.Variables.TryGetValue("system.servertype", out var serverType)
                || !string.Equals("Hosted", serverType.Value, StringComparison.OrdinalIgnoreCase))
            {
                _telemetry.AddOrUpdate("PluginDisabledReason", serverType?.Value);
                return true;
            }

            // check for PTR task or some other tasks to enable/disable
            if (context.Steps == null)
            {
                _telemetry.AddOrUpdate("PluginDisabledReason", "NoSteps");
                return true;
            }

            if (context.Steps.Any(x => x.Id.Equals(new Guid("0B0F01ED-7DDE-43FF-9CBB-E48954DAF9B1"))
                                       || x.Id.Equals(new Guid("AC4EE482-65DA-4485-A532-7B085873E532"))
                                       || x.Id.Equals(new Guid("8D8EEBD8-2B94-4C97-85AF-839254CC6DA4"))))
            {
                _telemetry.AddOrUpdate("PluginDisabledReason", "ExplicitPublishTaskPresent");
                return true;
            }

            if (PipelineConfig.BuildId == 0)
            {
                _telemetry.AddOrUpdate("PluginDisabledReason", "BuildIdZero");
                return true;
            }

            if (!PipelineConfig.Patterns.Any())
            {
                _telemetry.AddOrUpdate("PluginDisabledReason", "PatternIsEmpty");
                return true;
            }

            if (!PipelineConfig.SearchFolders.Any())
            {
                _telemetry.AddOrUpdate("PluginDisabledReason", "SearchFolderIsEmpty");
                return true;
            }

            return false;
        }

        private async Task PopulatePipelineConfig(IAgentLogPluginContext context)
        {
            var props = new Dictionary<string, Object>();

            if (context.Variables.TryGetValue("system.teamProject", out var projectName))
            {
                PipelineConfig.ProjectName = projectName.Value;
                _telemetry.AddOrUpdate("ProjectName", PipelineConfig.ProjectName);
                props.Add("ProjectName", PipelineConfig.ProjectName);
            }

            if (context.Variables.TryGetValue("build.buildId", out var buildIdVar) && int.TryParse(buildIdVar.Value, out var buildId))
            {
                PipelineConfig.BuildId = buildId;
                _telemetry.AddOrUpdate("BuildId", PipelineConfig.BuildId);
                props.Add("BuildId", PipelineConfig.BuildId);
            }

            if (context.Variables.TryGetValue("system.definitionid", out var buildDefinitionId))
            {
                _telemetry.AddOrUpdate("BuildDefinitionId", buildDefinitionId.Value);
                props.Add("BuildDefinitionId", buildDefinitionId.Value);
            }

            if (context.Variables.TryGetValue("agent.testfilepublisher.pattern", out var pattern)
                && !string.IsNullOrWhiteSpace(pattern.Value))
            {
                PopulateSearchPatterns(context, pattern.Value);
                props.Add("SearchPatterns", string.Join(",", PipelineConfig.Patterns));
            }

            if (context.Variables.TryGetValue("agent.testfilepublisher.searchfolders", out var searchFolders)
                && !string.IsNullOrWhiteSpace(searchFolders.Value))
            {
                PopulateSearchFolders(context, searchFolders.Value);
                props.Add("SearchFolders", string.Join(",", PipelineConfig.SearchFolders));
            }

            // Publish the initial telemetry event in case we are not able to fire the cumulative one for whatever reason
            await _telemetry.PublishTelemetryAsync("TestFilePublisherInitialize", props);
        }

        private void PopulateSearchFolders(IAgentLogPluginContext context, string searchFolders)
        {
            var folderVariables = searchFolders.Split(",");
            foreach (var folderVar in folderVariables)
            {
                if (context.Variables.TryGetValue(folderVar, out var folderValue))
                {
                    PipelineConfig.SearchFolders.Add(folderValue.Value);
                }
            }
        }

        private void PopulateSearchPatterns(IAgentLogPluginContext context, string searchPattern)
        {
            var patterns = searchPattern.Split(",");
            foreach (var pattern in patterns)
            {
                PipelineConfig.Patterns.Add(pattern);
            }
        }

        private ITraceLogger _logger;
        private ITelemetryDataCollector _telemetry;
        private ITestFilePublisher _testFilePublisher;
        public readonly PipelineConfig PipelineConfig = new PipelineConfig();
    }
}
