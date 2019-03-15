using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestFilePublisher;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Sdk;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Xunit;

namespace Test.L0.Plugin.TestFilePublisher
{
    public class TestFilePublisherLogPluginL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisherLogPlugin_DisableIfNotBuildPipeline()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

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
        public async Task TestFilePublisherLogPlugin_DisableWhenHostTypeNotSet()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

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
        public async Task TestFilePublisherLogPlugin_DisableWhenServerTypeNotSet()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

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
        public async Task TestFilePublisherLogPlugin_DisableIfOnPremPipeline()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

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
        public async Task TestFilePublisherLogPlugin_DisableIfPublishTaskPresent()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

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
        public async Task TestFilePublisherLogPlugin_DisableForInvalidBuildContext()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

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
        public async Task TestFilePublisherLogPlugin_DisableForInvalidSearchPattern()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                { "system.hosttype", new VariableValue("build") },
                { "system.servertype", new VariableValue("Hosted") },
                { "build.repository.provider", new VariableValue("GitHub") },
                { "build.buildId", new VariableValue("1") }
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
        public async Task TestFilePublisherLogPlugin_DisableForInvalidSearchFolders()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                { "system.hosttype", new VariableValue("build") },
                { "system.servertype", new VariableValue("Hosted") },
                { "build.repository.provider", new VariableValue("GitHub") },
                { "build.buildId", new VariableValue("1") },
                { "agent.testfilepublisher.pattern", new VariableValue("test-*.xml")}
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
        public async Task TestFilePublisherLogPlugin_DisableForNonResolvableSearchFolders()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                { "system.hosttype", new VariableValue("build") },
                { "system.servertype", new VariableValue("Hosted") },
                { "build.repository.provider", new VariableValue("GitHub") },
                { "build.buildId", new VariableValue("1") },
                { "agent.testfilepublisher.pattern", new VariableValue("test-*.xml")},
                { "agent.testfilepublisher.searchfolders", new VariableValue("agent.tempdirectory")}
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
        public async Task TestFilePublisherLogPlugin_DisableIfExceptionThrown()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var vssConnection = new Mock<VssConnection>(new Uri("http://fake"), new VssCredentials());
            var testFilePublisher = new Mock<ITestFilePublisher>();
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
                { "build.buildId", new VariableValue("1") },
                { "agent.tempdirectory", new VariableValue("/tmp")},
                { "agent.testfilepublisher.pattern", new VariableValue("test-*.xml")},
                { "agent.testfilepublisher.searchfolders", new VariableValue("agent.tempdirectory")}
            });

            testFilePublisher.Setup(x => x.InitializeAsync()).Throws(new Exception("some exception"));

            var plugin = new TestFilePublisherLogPlugin(null, telemetry.Object, testFilePublisher.Object);
            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
            agentContext.Verify(x => x.Output(It.Is<string>(msg => msg.Contains("Unable to initialize TestFilePublisher"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisherLogPlugin_EnableForBuildPipeline()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var vssConnection = new Mock<VssConnection>(new Uri("http://fake"), new VssCredentials());
            var testFilePublisher = new Mock<ITestFilePublisher>();
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
                { "system.hosttype", new VariableValue("build") },
                { "system.servertype", new VariableValue("Hosted") },
                { "build.repository.provider", new VariableValue("GitHub") },
                { "build.buildId", new VariableValue("1") },
                { "agent.tempdirectory", new VariableValue("/tmp")},
                { "agent.testfilepublisher.pattern", new VariableValue("test-*.xml")},
                { "agent.testfilepublisher.searchfolders", new VariableValue("agent.tempdirectory")}
            });
            testFilePublisher.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);

            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);
            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == true);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisherLogPlugin_ValidateFoldersAreResolved()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var vssConnection = new Mock<VssConnection>(new Uri("http://fake"), new VssCredentials());
            var testFilePublisher = new Mock<ITestFilePublisher>();
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
                { "system.hosttype", new VariableValue("build") },
                { "system.servertype", new VariableValue("Hosted") },
                { "build.repository.provider", new VariableValue("GitHub") },
                { "build.buildId", new VariableValue("1") },
                { "agent.tempdirectory", new VariableValue("/tmp")},
                { "system.defaultworkingdirectory", new VariableValue("/def")},
                { "agent.testfilepublisher.pattern", new VariableValue("test-*.xml")},
                { "agent.testfilepublisher.searchfolders", new VariableValue("agent.tempdirectory,system.defaultworkingdirectory")}
            });
            testFilePublisher.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);

            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);
            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == true);
            Assert.True(plugin.PipelineConfig.SearchFolders.Count == 2);
            Assert.True(plugin.PipelineConfig.SearchFolders[0].Equals("/tmp") && plugin.PipelineConfig.SearchFolders[1].Equals("/def"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisherLogPlugin_LogExceptionForFailures()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var vssConnection = new Mock<VssConnection>(new Uri("http://fake"), new VssCredentials());
            var testFilePublisher = new Mock<ITestFilePublisher>();
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
                { "system.hosttype", new VariableValue("build") },
                { "system.servertype", new VariableValue("Hosted") },
                { "build.repository.provider", new VariableValue("GitHub") },
                { "build.buildId", new VariableValue("1") },
                { "agent.tempdirectory", new VariableValue("/tmp")},
                { "system.defaultworkingdirectory", new VariableValue("/def")},
                { "agent.testfilepublisher.pattern", new VariableValue("test-*.xml")},
                { "agent.testfilepublisher.searchfolders", new VariableValue("agent.tempdirectory,system.defaultworkingdirectory")}
            });
            testFilePublisher.Setup(x => x.PublishAsync()).Throws<Exception>();

            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);
            await plugin.FinalizeAsync(agentContext.Object);

            logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Error"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisherLogPlugin_DisableIfMavenPresent()
        {
            var agentContext = new Mock<IAgentLogPluginContext>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testFilePublisher = new Mock<ITestFilePublisher>();
            var plugin = new TestFilePublisherLogPlugin(logger.Object, telemetry.Object, testFilePublisher.Object);

            agentContext.Setup(x => x.Variables).Returns(new Dictionary<string, VariableValue>()
            {
                {"system.hosttype", new VariableValue("build") },
                {"system.servertype", new VariableValue("Hosted") }
            });

            agentContext.Setup(x => x.Steps).Returns(new List<TaskStepDefinitionReference>()
            {
                new TaskStepDefinitionReference()
                {
                    Id = new Guid("ac4ee482-65da-4485-a532-7b085873e532")
                }
            });

            var result = await plugin.InitializeAsync(agentContext.Object);

            Assert.True(result == false);
        }
    }
}
