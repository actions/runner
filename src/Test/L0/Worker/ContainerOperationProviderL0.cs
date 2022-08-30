using System;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ContainerOperationProviderL0
    {
        // private Mock<DockerCommandManager> _commandManager;
        //  private ContainerOperationProvider operationProvider;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnablePluginInternalCommand()
        {
            
                // _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                //    .Returns((string tag, string line) =>
                //             {
                //                 hc.GetTrace().Info($"{tag} {line}");
                //                 return 1;
                //             });
                // _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                //    .Callback((Issue issue, string message) =>
                //    {
                //        hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                //    });

                // _commandManager.EnablePluginInternalCommand();

                // Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                // _pipelineDirectoryManager.Verify(x => x.UpdateRepositoryDirectory(_ec.Object, "actions/runner", "somepath", true), Times.Once);
            
        }
        // private TestHostContext CreateTestContext([CallerMemberName] string testName = "") {
        //     return null;
        // }

    }
}