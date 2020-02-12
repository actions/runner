using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using Xunit;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ActionCommandManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnablePluginInternalCommand()
        {
            using (TestHostContext _hc = new TestHostContext(this))
            {
                var extensionManger = new Mock<IExtensionManager>();
                var directoryManager = new Mock<IPipelineDirectoryManager>();

                var pluginCommand = new InternalPluginSetRepoPathCommandExtension();
                pluginCommand.Initialize(_hc);

                var envCommand = new SetEnvCommandExtension();
                envCommand.Initialize(_hc);

                extensionManger.Setup(x => x.GetExtensions<IActionCommandExtension>())
                               .Returns(new List<IActionCommandExtension>() { pluginCommand, envCommand });
                _hc.SetSingleton<IExtensionManager>(extensionManger.Object);
                _hc.SetSingleton<IPipelineDirectoryManager>(directoryManager.Object);

                Mock<IExecutionContext> _ec = new Mock<IExecutionContext>();
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                _hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });
                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       _hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });
                ActionCommandManager commandManager = new ActionCommandManager();
                commandManager.Initialize(_hc);

                commandManager.EnablePluginInternalCommand();

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                directoryManager.Verify(x => x.UpdateRepositoryDirectory(_ec.Object, "actions/runner", "somepath", true), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DisablePluginInternalCommand()
        {
            using (TestHostContext _hc = new TestHostContext(this))
            {
                var extensionManger = new Mock<IExtensionManager>();
                var directoryManager = new Mock<IPipelineDirectoryManager>();

                var pluginCommand = new InternalPluginSetRepoPathCommandExtension();
                pluginCommand.Initialize(_hc);

                var envCommand = new SetEnvCommandExtension();
                envCommand.Initialize(_hc);

                extensionManger.Setup(x => x.GetExtensions<IActionCommandExtension>())
                               .Returns(new List<IActionCommandExtension>() { pluginCommand, envCommand });

                _hc.SetSingleton<IExtensionManager>(extensionManger.Object);
                _hc.SetSingleton<IPipelineDirectoryManager>(directoryManager.Object);

                Mock<IExecutionContext> _ec = new Mock<IExecutionContext>();
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                _hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });
                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       _hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });
                ActionCommandManager commandManager = new ActionCommandManager();
                commandManager.Initialize(_hc);

                commandManager.EnablePluginInternalCommand();

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                commandManager.DisablePluginInternalCommand();

                Assert.False(commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                directoryManager.Verify(x => x.UpdateRepositoryDirectory(_ec.Object, "actions/runner", "somepath", true), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StopProcessCommand()
        {
            using (TestHostContext _hc = new TestHostContext(this))
            {
                var extensionManger = new Mock<IExtensionManager>();
                var pluginCommand = new InternalPluginSetRepoPathCommandExtension();
                pluginCommand.Initialize(_hc);

                var envCommand = new SetEnvCommandExtension();
                envCommand.Initialize(_hc);

                extensionManger.Setup(x => x.GetExtensions<IActionCommandExtension>())
                               .Returns(new List<IActionCommandExtension>() { pluginCommand, envCommand });
                _hc.SetSingleton<IExtensionManager>(extensionManger.Object);

                Mock<IExecutionContext> _ec = new Mock<IExecutionContext>();
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                _hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       _hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });

                _ec.Setup(x => x.EnvironmentVariables).Returns(new Dictionary<string, string>());

                ActionCommandManager commandManager = new ActionCommandManager();
                commandManager.Initialize(_hc);

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[stop-commands]stopToken", null));
                Assert.False(commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[stopToken]", null));
                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommand()
        {
            using (TestHostContext _hc = new TestHostContext(this))
            {
                var extensionManager = new Mock<IExtensionManager>();
                var echoCommand = new EchoCommandExtension();
                echoCommand.Initialize(_hc);

                extensionManager.Setup(x => x.GetExtensions<IActionCommandExtension>())
                               .Returns(new List<IActionCommandExtension>() { echoCommand });
                _hc.SetSingleton<IExtensionManager>(extensionManager.Object);

                Mock<IExecutionContext> _ec = new Mock<IExecutionContext>();
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                _hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                _ec.SetupAllProperties();

                ActionCommandManager commandManager = new ActionCommandManager();
                commandManager.Initialize(_hc);

                Assert.False(_ec.Object.EchoOnActionCommand);

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "::echo::on", null));
                Assert.True(_ec.Object.EchoOnActionCommand);

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "::echo::off", null));
                Assert.False(_ec.Object.EchoOnActionCommand);

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "::echo::ON", null));
                Assert.True(_ec.Object.EchoOnActionCommand);

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "::echo::Off   ", null));
                Assert.False(_ec.Object.EchoOnActionCommand);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommandDebugOn()
        {
            using (TestHostContext _hc = new TestHostContext(this))
            {
                // Set up a few things
                // 1. Job request message (with ACTIONS_STEP_DEBUG = true)
                TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
                TimelineReference timeline = new TimelineReference();
                Guid jobId = Guid.NewGuid();
                string jobName = "some job name";
                var jobRequest = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, jobName, jobName, null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null);
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

                _hc.SetSingleton(jobServerQueue.Object);

                var extensionManager = new Mock<IExtensionManager>();
                var echoCommand = new EchoCommandExtension();
                echoCommand.Initialize(_hc);

                extensionManager.Setup(x => x.GetExtensions<IActionCommandExtension>())
                               .Returns(new List<IActionCommandExtension>() { echoCommand });
                _hc.SetSingleton<IExtensionManager>(extensionManager.Object);

                var configurationStore = new Mock<IConfigurationStore>();
                configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings());
                _hc.SetSingleton(configurationStore.Object);

                var pagingLogger = new Mock<IPagingLogger>();
                _hc.EnqueueInstance(pagingLogger.Object);

                ActionCommandManager commandManager = new ActionCommandManager();
                commandManager.Initialize(_hc);

                var _ec = new Runner.Worker.ExecutionContext();
                _ec.Initialize(_hc);

                // Initialize the job (to exercise logic that sets EchoOnActionCommand)
                _ec.InitializeJob(jobRequest, System.Threading.CancellationToken.None);

                _ec.Complete();

                Assert.True(_ec.EchoOnActionCommand);

                Assert.True(commandManager.TryProcessCommand(_ec, "::echo::off", null));
                Assert.False(_ec.EchoOnActionCommand);

                Assert.True(commandManager.TryProcessCommand(_ec, "::echo::on", null));
                Assert.True(_ec.EchoOnActionCommand);
            }
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommandInvalid()
        {
            using (TestHostContext _hc = new TestHostContext(this))
            {
                var extensionManager = new Mock<IExtensionManager>();
                var echoCommand = new EchoCommandExtension();
                echoCommand.Initialize(_hc);

                extensionManager.Setup(x => x.GetExtensions<IActionCommandExtension>())
                               .Returns(new List<IActionCommandExtension>() { echoCommand });
                _hc.SetSingleton<IExtensionManager>(extensionManager.Object);

                Mock<IExecutionContext> _ec = new Mock<IExecutionContext>();
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                _hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                _ec.SetupAllProperties();

                ActionCommandManager commandManager = new ActionCommandManager();
                commandManager.Initialize(_hc);

                // Echo commands below are considered "processed", but are invalid
                // 1. Invalid echo value
                Assert.True(commandManager.TryProcessCommand(_ec.Object, "::echo::invalid", null));
                Assert.Equal(TaskResult.Failed, _ec.Object.CommandResult);
                Assert.False(_ec.Object.EchoOnActionCommand);

                // 2. No value
                Assert.True(commandManager.TryProcessCommand(_ec.Object, "::echo::", null));
                Assert.Equal(TaskResult.Failed, _ec.Object.CommandResult);
                Assert.False(_ec.Object.EchoOnActionCommand);
            }
        }
    }
}
