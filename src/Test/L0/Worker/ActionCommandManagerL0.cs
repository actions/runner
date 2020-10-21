using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Moq;
using Xunit;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ActionCommandManagerL0
    {
        private ActionCommandManager _commandManager;
        private Mock<IExecutionContext> _ec;
        private Mock<IExtensionManager> _extensionManager;
        private Mock<IPipelineDirectoryManager> _pipelineDirectoryManager;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnablePluginInternalCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });
                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });

                _commandManager.EnablePluginInternalCommand();

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                _pipelineDirectoryManager.Verify(x => x.UpdateRepositoryDirectory(_ec.Object, "actions/runner", "somepath", true), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DisablePluginInternalCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });
                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });

                _commandManager.EnablePluginInternalCommand();

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                _commandManager.DisablePluginInternalCommand();

                Assert.False(_commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                _pipelineDirectoryManager.Verify(x => x.UpdateRepositoryDirectory(_ec.Object, "actions/runner", "somepath", true), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StopProcessCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });

                _ec.Object.Global.EnvironmentVariables = new Dictionary<string, string>();

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[stop-commands]stopToken", null));
                Assert.False(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[stopToken]", null));
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                Assert.False(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::on", null));
                Assert.True(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::off", null));
                Assert.False(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::ON", null));
                Assert.True(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::Off   ", null));
                Assert.False(_ec.Object.EchoOnActionCommand);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommandDebugOn()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Set up a few things
                // 1. Job request message (with ACTIONS_STEP_DEBUG = true)
                TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
                TimelineReference timeline = new TimelineReference();
                Guid jobId = Guid.NewGuid();
                string jobName = "some job name";
                var jobRequest = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, jobName, jobName, null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null, null, null, null);
                jobRequest.Resources.Repositories.Add(new Pipelines.RepositoryResource()
                {
                    Alias = Pipelines.PipelineConstants.SelfAlias,
                    Id = "github",
                    Version = "sha1"
                });
                jobRequest.ContextData["github"] = new Pipelines.ContextData.DictionaryContextData();
                jobRequest.Variables["ACTIONS_STEP_DEBUG"] = "true";

                // Some service dependencies
                var jobServerQueue = new Mock<IJobServerQueue>();
                jobServerQueue.Setup(x => x.QueueTimelineRecordUpdate(It.IsAny<Guid>(), It.IsAny<TimelineRecord>()));

                hc.SetSingleton(jobServerQueue.Object);

                var configurationStore = new Mock<IConfigurationStore>();
                configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings());
                hc.SetSingleton(configurationStore.Object);

                var pagingLogger = new Mock<IPagingLogger>();
                hc.EnqueueInstance(pagingLogger.Object);

                // Initialize the job (to exercise logic that sets EchoOnActionCommand)
                var ec = new Runner.Worker.ExecutionContext();
                ec.Initialize(hc);
                ec.InitializeJob(jobRequest, System.Threading.CancellationToken.None);

                ec.Complete();

                Assert.True(ec.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(ec, "::echo::off", null));
                Assert.False(ec.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(ec, "::echo::on", null));
                Assert.True(ec.EchoOnActionCommand);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommandInvalid()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                // Echo commands below are considered "processed", but are invalid
                // 1. Invalid echo value
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::invalid", null));
                Assert.Equal(TaskResult.Failed, _ec.Object.CommandResult);
                Assert.False(_ec.Object.EchoOnActionCommand);

                // 2. No value
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::", null));
                Assert.Equal(TaskResult.Failed, _ec.Object.CommandResult);
                Assert.False(_ec.Object.EchoOnActionCommand);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AddMatcherTranslatesFilePath()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Create a problem matcher config file
                var hostDirectory = hc.GetDirectory(WellKnownDirectory.Temp);
                var hostFile = Path.Combine(hostDirectory, "my-matcher.json");
                Directory.CreateDirectory(hostDirectory);
                var content = @"
{
    ""problemMatcher"": [
        {
            ""owner"": ""my-matcher"",
            ""pattern"": [
                {
                    ""regexp"": ""^ERROR: (.+)$"",
                    ""message"": 1
                }
            ]
        }
    ]
}";
                File.WriteAllText(hostFile, content);

                // Setup translation info
                var container = new ContainerInfo();
                var containerDirectory = "/some-container-directory";
                var containerFile = Path.Combine(containerDirectory, "my-matcher.json");
                container.AddPathTranslateMapping(hostDirectory, containerDirectory);

                // Act
                _commandManager.TryProcessCommand(_ec.Object, $"::add-matcher::{containerFile}", container);

                // Assert
                _ec.Verify(x => x.AddMatchers(It.IsAny<IssueMatchersConfig>()), Times.Once);
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hostContext = new TestHostContext(this, testName);

            // Mock extension manager
            _extensionManager = new Mock<IExtensionManager>();
            var commands = new IActionCommandExtension[]
            {
                new AddMatcherCommandExtension(),
                new EchoCommandExtension(),
                new InternalPluginSetRepoPathCommandExtension(),
                new SetEnvCommandExtension(),
            };
            foreach (var command in commands)
            {
                command.Initialize(hostContext);
            }
            _extensionManager.Setup(x => x.GetExtensions<IActionCommandExtension>())
                .Returns(new List<IActionCommandExtension>(commands));
            hostContext.SetSingleton<IExtensionManager>(_extensionManager.Object);

            // Mock pipeline directory manager
            _pipelineDirectoryManager = new Mock<IPipelineDirectoryManager>();
            hostContext.SetSingleton<IPipelineDirectoryManager>(_pipelineDirectoryManager.Object);

            // Execution context
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.Global).Returns(new GlobalContext());

            // Command manager
            _commandManager = new ActionCommandManager();
            _commandManager.Initialize(hostContext);

            return hostContext;
        }
    }
}
