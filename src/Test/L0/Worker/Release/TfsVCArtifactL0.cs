using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Release
{
    public sealed class TfsVCArtifactL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IExtensionManager> _extensionManager;
        private Mock<ISourceProvider> _sourceProvider;
        private ArtifactDefinition _artifactDefinition;
        private Variables _variables;

        private string _buildDirectory = "r1";
        private const string _repositoryId = "fe0bd152-bb17-4ec4-b421-21d7e0450edb";
        private const string _projectId = "ke0bd152-bb17-4ec4-b421-21d7e0450edb";
        private const string _expectedVersion = "version";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MissingEndpointShouldThrowException()
        {
            using (TestHostContext tc = Setup())
            {
                var artifact = new TfsVCArtifact();

                _ec.Setup(x => x.Endpoints)
                    .Returns(
                        new List<ServiceEndpoint>
                        {
                            new ServiceEndpoint
                            {
                                Name = "Some endpoint name"
                            }
                        });

                Assert.Throws<InvalidOperationException>(
                    () => artifact.DownloadAsync(_ec.Object, _artifactDefinition, "temp").SyncResult());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void TfsVCArtifactShouldCallGetSourceWithCorrectParameter()
        {
            using (TestHostContext tc = Setup())
            {
                var tfsVCArtifact = new TfsVCArtifact();
                tfsVCArtifact.Initialize(tc);
                var workFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    $"_work_{Path.GetRandomFileName()}");
                var sourcesDirectory = Path.Combine(workFolder, _buildDirectory, "temp");
                _artifactDefinition.Details = tfsVCArtifact.GetArtifactDetails(
                    _ec.Object,
                    new AgentArtifactDefinition
                    {
                        Details = JsonConvert.SerializeObject(new Dictionary<string, string>
                        {
                            {ArtifactDefinitionConstants.ProjectId, _projectId},
                            {ArtifactDefinitionConstants.RepositoryId, _repositoryId}
                        })
                    });

                _ec.Setup(x => x.Endpoints)
                    .Returns(
                        new List<ServiceEndpoint>
                        {
                            new ServiceEndpoint
                            {
                                Name = _repositoryId,
                            }
                        });

                await tfsVCArtifact.DownloadAsync(_ec.Object, _artifactDefinition, sourcesDirectory);

                // verify tfsvc endpoint is set correctly
                _sourceProvider.Verify(
                    x => x.GetSourceAsync(
                        It.IsAny<IExecutionContext>(),
                        It.Is<ServiceEndpoint>(y => y.Data.ContainsKey(EndpointData.TfvcWorkspaceMapping) && y.Data.ContainsKey(EndpointData.Clean)
                        && y.Data.ContainsKey(Constants.EndpointData.SourcesDirectory) && y.Data.ContainsKey(Constants.EndpointData.SourceVersion)),
                        It.IsAny<CancellationToken>()));
            }
        }

        private TestHostContext Setup([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();

            _artifactDefinition = new ArtifactDefinition
            {
                Version = _expectedVersion,
                Details = new TfsVCArtifactDetails
                {
                    RepositoryId = _repositoryId,
                    Mappings = string.Empty,
                    ProjectId = _projectId
                }
            };

            _extensionManager = new Mock<IExtensionManager>();
            _sourceProvider = new Mock<ISourceProvider>();

            List<string> warnings;
            _variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

            hc.SetSingleton<IExtensionManager>(_extensionManager.Object);
            _ec.Setup(x => x.Variables).Returns(_variables);
            _extensionManager.Setup(x => x.GetExtensions<ISourceProvider>())
                .Returns(new List<ISourceProvider> { _sourceProvider.Object });
            _sourceProvider.Setup(x => x.RepositoryType).Returns(RepositoryTypes.TfsVersionControl);

            return hc;
        }
    }
}