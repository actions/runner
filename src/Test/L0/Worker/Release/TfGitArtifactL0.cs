using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Release
{
    public sealed class TfGitArtifactL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IExtensionManager> _extensionManager;
        private Mock<ISourceProvider> _sourceProvider;
        private ArtifactDefinition _artifactDefinition;
        private Variables _variables;

        private const string _expectedUrl = "https://hello.com/repos/contoso";
        private const string _expectedBranchName = "/refs/head/testbranch";
        private const string _expectedVersion = "version";
        private const string _expectedRepositoryId = "fe0bd152-bb17-4ec4-b421-21d7e0450edb";
        private const string _expectedProjectId = "ae0bd152-bb17-4ec4-b421-21d7e0450edb";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ShouldThrowIfEndpointsDoNotContainTfGitEndpoint()
        {
            using (TestHostContext tc = Setup())
            {
                var artifact = new TfGitArtifact();

                _ec.Setup(x => x.Endpoints)
                    .Returns(
                        new List<ServiceEndpoint>
                            {
                                new ServiceEndpoint
                                    {
                                        Name = "Some endpoint name",
                                        Url = new Uri("http://contoso.visualstudio.com")
                                    }
                            });

                Assert.Throws<InvalidOperationException>(
                    () => artifact.DownloadAsync(_ec.Object, _artifactDefinition, "temp").SyncResult());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void TfGitArtifactShouldCallGetSourceWithCorrectParameter()
        {
            using (TestHostContext tc = Setup())
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));
                var tfGitArtifact = new TfGitArtifact();
                tfGitArtifact.Initialize(tc);
                var expectedPath = "expectedLocalPath";

                _ec.Setup(x => x.Endpoints)
                    .Returns(
                        new List<ServiceEndpoint>
                            {
                                new ServiceEndpoint
                                    {
                                        Name = _expectedRepositoryId,
                                        Url = new Uri(_expectedUrl),
                                        Authorization = new EndpointAuthorization
                                        {
                                            Scheme = EndpointAuthorizationSchemes.OAuth
                                        }
                                    }
                            });

                await tfGitArtifact.DownloadAsync(_ec.Object, _artifactDefinition, expectedPath);

                // verify required variables are set
                Assert.Equal(_variables.Get(Constants.Variables.Build.SourcesDirectory), expectedPath);
                Assert.Equal(_variables.Get(Constants.Variables.Build.SourceBranch), _expectedBranchName);
                Assert.Equal(_variables.Get(Constants.Variables.Build.SourceVersion), _expectedVersion);

                // verify TfGit endpoint is set correctly
                _sourceProvider.Verify(
                    x => x.GetSourceAsync(
                        It.IsAny<IExecutionContext>(),
                        It.Is<ServiceEndpoint>(y => y.Url.Equals(new Uri(_expectedUrl)) && y.Authorization.Scheme.Equals(EndpointAuthorizationSchemes.OAuth) && y.Name.Equals(_expectedRepositoryId)),
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
                Details = new GitArtifactDetails
                {
                    RepositoryId = _expectedRepositoryId,
                    ProjectId = _expectedProjectId,
                    Branch = _expectedBranchName
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
            _sourceProvider.Setup(x => x.RepositoryType).Returns(WellKnownRepositoryTypes.TfsGit);

            return hc;
        }
    }
}