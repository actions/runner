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
    public sealed class GitHubArtifactL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IExtensionManager> _extensionManager;
        private Mock<ISourceProvider> _sourceProvider;
        private ArtifactDefinition _artifactDefinition;
        private Variables _variables;

        private const string _expectedGitHubUrl = "https://api.github.com/repos/contoso";
        private const string _githubConnectionName = "githubconnection";
        private const string _expectedBranchName = "/refs/head/testbranch";
        private const string _expectedVersion = "version";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MissingEndpointShouldThrowException()
        {
            using (TestHostContext tc = Setup())
            {
                var artifact = new GitHubArtifact();

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
        public async void GitHubArtifactShouldCallGetSourceWithCorrectParameter()
        {
            using (TestHostContext tc = Setup())
            {
                var gitHubArtifact = new GitHubArtifact();
                gitHubArtifact.Initialize(tc);
                var expectedPath = "expectedLocalPath";

                _ec.Setup(x => x.Endpoints)
                    .Returns(
                        new List<ServiceEndpoint>
                            {
                                new ServiceEndpoint
                                    {
                                        Name = _githubConnectionName,
                                        Url = new Uri("http://contoso.visualstudio.com"),
                                        Authorization = new EndpointAuthorization()
                                    }
                            });

                await gitHubArtifact.DownloadAsync(_ec.Object, _artifactDefinition, expectedPath);

                // verify required variables are set
                Assert.Equal(_variables.Get(Constants.Variables.Build.SourcesDirectory), expectedPath);
                Assert.Equal(_variables.Get(Constants.Variables.Build.SourceBranch), _expectedBranchName);
                Assert.Equal(_variables.Get(Constants.Variables.Build.SourceVersion), _expectedVersion);

                // verify github endpoint is set correctly
                _sourceProvider.Verify(
                    x => x.GetSourceAsync(
                        It.IsAny<IExecutionContext>(), 
                        It.Is<ServiceEndpoint>(y => y.Url.Equals(new Uri(_expectedGitHubUrl)) && y.Authorization.Scheme.Equals(EndpointAuthorizationSchemes.OAuth)), 
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
                                          Details = new GitHubArtifactDetails
                                          {
                                              ConnectionName = _githubConnectionName,
                                              CloneUrl = new Uri(_expectedGitHubUrl),
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
            _sourceProvider.Setup(x => x.RepositoryType).Returns(WellKnownRepositoryTypes.GitHub);

            return hc;
        }
    }
}