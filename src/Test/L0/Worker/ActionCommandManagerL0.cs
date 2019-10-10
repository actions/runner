using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using Xunit;

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

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath"));

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

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath"));

                commandManager.DisablePluginInternalCommand();

                Assert.False(commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath"));

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

                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[stop-commands]stopToken"));
                Assert.False(commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar"));
                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[stopToken]"));
                Assert.True(commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar"));
            }
        }
    }
}
