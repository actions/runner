using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Plugins.Log.TestResultParser.Plugin;
using Agent.Sdk;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Xunit;

namespace Test.L0.Plugin.TestResultParser
{
    public class TestResultLogPluginL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_DisableIfNotBuildPipeline()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var logParser = new Mock<ILogParserGateway>();
            var plugin = new TestResultLogPlugin(logParser.Object, logger.Object, telemetry.Object);

            telemetry.Setup(x => x.PublishCumulativeTelemetryAsync()).Returns(Task.FromResult(TaskResult.Succeeded));

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.hosttype", new VariableValue("release") }
            });

            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_DisableWhenHostTypeNotSet()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var logParser = new Mock<ILogParserGateway>();
            var plugin = new TestResultLogPlugin(logParser.Object, logger.Object, telemetry.Object);

            telemetry.Setup(x => x.PublishCumulativeTelemetryAsync()).Returns(Task.FromResult(TaskResult.Succeeded));

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.hosttype", null }
            });

            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_DisableWhenServerTypeNotSet()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var logParser = new Mock<ILogParserGateway>();
            var plugin = new TestResultLogPlugin(logParser.Object, logger.Object, telemetry.Object);

            telemetry.Setup(x => x.PublishCumulativeTelemetryAsync()).Returns(Task.FromResult(TaskResult.Succeeded));

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.servertype", null }
            });

            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_DisableIfOnPremPipeline()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var logParser = new Mock<ILogParserGateway>();
            var plugin = new TestResultLogPlugin(logParser.Object, logger.Object, telemetry.Object);

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.hosttype", new VariableValue("build") },
                {"system.servertype", new VariableValue("OnPrem") }
            });

            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_DisableIfPublishTaskPresent()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var logParser = new Mock<ILogParserGateway>();
            var plugin = new TestResultLogPlugin(logParser.Object, logger.Object, telemetry.Object);

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.hosttype", new VariableValue("build") },
                {"system.servertype", new VariableValue("Hosted") }
            });
            
            agentContext.Setup(x => x.Steps).Returns(new List<TaskStepDefinitionReference>()
            {
                new TaskStepDefinitionReference()
                {
                    Id = new Guid("0B0F01ED-7DDE-43FF-9CBB-E48954DAF9B1")
                }
            });

            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_DisableForInvalidBuildContext()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var logParser = new Mock<ILogParserGateway>();
            var plugin = new TestResultLogPlugin(logParser.Object, logger.Object, telemetry.Object);

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.hosttype", new VariableValue("build") },
                {"system.servertype", new VariableValue("Hosted") }
            });
            agentContext.Setup(x => x.Steps).Returns(new List<TaskStepDefinitionReference>()
            {
                new TaskStepDefinitionReference()
                {
                    Id = new Guid("1B0F01ED-7DDE-43FF-9CBB-E48954DAF9B1")
                }
            });

            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_DisableIfExceptionThrown()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var vssConnection = new Mock<VssConnection>(new Uri("http://fake"), new VssCredentials());
            var logParser = new Mock<ILogParserGateway>();
            var telemetry = new Mock<ITelemetryDataCollector>();

            telemetry.Setup(x => x.PublishCumulativeTelemetryAsync()).Returns(Task.FromResult(TaskResult.Succeeded));

            agentContext.Setup(x => x.VssConnection).Returns(vssConnection.Object);
            agentContext.Setup(x => x.Steps).Returns(new List<TaskStepDefinitionReference>()
            {
                new TaskStepDefinitionReference()
                {
                    Id = new Guid("1B0F01ED-7DDE-43FF-9CBB-E48954DAF9B1")
                }
            });
            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                { "system.hosttype", new VariableValue("build") },
                { "system.servertype", new VariableValue("Hosted") },
                { "build.repository.provider", new VariableValue("GitHub") },
                { "build.buildId", new VariableValue("1") }
            });

            logParser.Setup(x => x.InitializeAsync(It.IsAny<IClientFactory>(), It.IsAny<IPipelineConfig>(), It.IsAny<ITraceLogger>(), It.IsAny<ITelemetryDataCollector>()))
                .Throws(new Exception("some exception"));

            var plugin = new TestResultLogPlugin(logParser.Object, null, telemetry.Object);
            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
            agentContext.Verify(x => x.Output(It.Is<string>(msg => msg.Contains("Unable to initialize TestResultLogParser"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestResultLogPlugin_EnableForBuildPipeline()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var vssConnection = new Mock<VssConnection>(new Uri("http://fake"), new VssCredentials());
            var logParser = new Mock<ILogParserGateway>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();

            telemetry.Setup(x => x.PublishCumulativeTelemetryAsync()).Returns(Task.FromResult(TaskResult.Succeeded));

            agentContext.Setup(x => x.Steps).Returns(new List<TaskStepDefinitionReference>()
            {
                new TaskStepDefinitionReference()
                {
                    Id = new Guid("1B0F01ED-7DDE-43FF-9CBB-E48954DAF9B1")
                }
            });

            agentContext.Setup(x => x.VssConnection).Returns(vssConnection.Object);
            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.hosttype", new VariableValue("build") },
                {"system.servertype", new VariableValue("Hosted") },
                {"build.buildId", new VariableValue("1") },
                {"build.repository.provider", new VariableValue("Github") }
            });
            logParser.Setup(x => x.InitializeAsync(It.IsAny<IClientFactory>(), It.IsAny<IPipelineConfig>(), It.IsAny<ITraceLogger>(), It.IsAny<ITelemetryDataCollector>()))
                .Returns(Task.CompletedTask);

            var plugin = new TestResultLogPlugin(logParser.Object, logger.Object, telemetry.Object);
            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == true);
        }
    }
}
