using System;
using System.Runtime.CompilerServices;

using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.VisualStudio.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Tests;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

using Moq;

using Xunit;

using ArtifactProvider = Microsoft.VisualStudio.Services.Agent.Worker.Release.ArtifactProvider;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ArtifactProviderL0
    {
        private Mock<IBuildArtifact> _buildArtifact;

        private Mock<IJenkinsArtifact> _jenkinsArtifact;

        private Mock<IExecutionContext> _executionContext;

        public ArtifactProviderL0()
        {
            _buildArtifact = new Mock<IBuildArtifact>();
            _jenkinsArtifact = new Mock<IJenkinsArtifact>();
            _executionContext = new Mock<IExecutionContext>();
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<IBuildArtifact>(_buildArtifact.Object);
            tc.SetSingleton<IJenkinsArtifact>(_jenkinsArtifact.Object);
            tc.SetSingleton<IExecutionContext>(_executionContext.Object);

            return tc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void ArtifactProviderDownloadShouldCallCorrectDownloadImplementation()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                ArtifactDefinition artifactDefinition = new ArtifactDefinition();
                var artifactProvider = new ArtifactProvider();
                artifactProvider.Initialize(hc);

                foreach (AgentArtifactType type in new[] { AgentArtifactType.Build, AgentArtifactType.Jenkins })
                {
                    artifactDefinition.ArtifactType = type;
                    await artifactProvider.Download(_executionContext.Object, artifactDefinition, "test");

                    if (type == AgentArtifactType.Build)
                    {
                        _buildArtifact.Verify(
                            x =>
                            x.Download(
                                It.IsAny<ArtifactDefinition>(),
                                It.IsAny<IExecutionContext>(),
                                It.IsAny<string>()),
                            Times.Once);
                    }
                    if (type == AgentArtifactType.Jenkins)
                    {
                        _jenkinsArtifact.Verify(
                            x =>
                            x.Download(
                                It.IsAny<ArtifactDefinition>(),
                                It.IsAny<IExecutionContext>(),
                                It.IsAny<string>()),
                            Times.Once);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ArtifactProviderGetArtifactDetailsShouldCallCorrectImplementation()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                AgentArtifactDefinition agentArtifactDefinition = new AgentArtifactDefinition();
                var artifactProvider = new ArtifactProvider();
                artifactProvider.Initialize(hc);

                foreach (AgentArtifactType type in new[] { AgentArtifactType.Build, AgentArtifactType.Jenkins })
                {
                    agentArtifactDefinition.ArtifactType = type;
                    artifactProvider.GetArtifactDetails(_executionContext.Object, agentArtifactDefinition);

                    if (type == AgentArtifactType.Build)
                    {
                        _buildArtifact.Verify(
                            x =>
                            x.GetArtifactDetails(
                                It.IsAny<AgentArtifactDefinition>(),
                                It.IsAny<IExecutionContext>()),
                            Times.Once);
                    }
                    if (type == AgentArtifactType.Jenkins)
                    {
                        _jenkinsArtifact.Verify(
                            x =>
                            x.GetArtifactDetails(
                                It.IsAny<AgentArtifactDefinition>(),
                                It.IsAny<IExecutionContext>()),
                            Times.Once);
                    }
                }
            }
        }
    }
}