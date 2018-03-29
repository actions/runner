using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Release
{
    public sealed class ReleaseJobExtensionL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IExtensionManager> _extensionManager;
        private Mock<ISourceProvider> _sourceProvider;
        private Mock<IReleaseDirectoryManager> _releaseDirectoryManager;
        private Variables _variables;
        private string stubWorkFolder;
        private ReleaseJobExtension releaseJobExtension;

        private const int id = 10;
        private const int releaseId = 100;
        private const string releaseDefinitionName = "stubRd";
        private readonly Guid projectId = new Guid("B152FEAA-7E65-43C9-BCC4-07F6883EE794");
        private readonly ReleaseTrackingConfig map = new ReleaseTrackingConfig
        {
            ReleaseDirectory = "r1"
        };

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetRootedPathShouldReturnNullIfPathIsNull()
        {
            using (TestHostContext tc = Setup(createWorkDirectory: false))
            {
                var result = releaseJobExtension.GetRootedPath(_ec.Object, null);

                Assert.Equal(null, result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetRootedPathShouldReturnRootedPathIfPathIsResolvedBySourceProvider()
        {
            using (TestHostContext tc = Setup(createWorkDirectory: false))
            {
                var rootedPath = Path.Combine(this.stubWorkFolder, "temp");
                var result = releaseJobExtension.GetRootedPath(_ec.Object, rootedPath);

                Assert.Equal(rootedPath, result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetRootedPathShouldReturnRootedPathIfPathIsRelative()
        {
            using (TestHostContext tc = Setup(createWorkDirectory: false))
            {
                var rootedPath = Path.Combine(this.stubWorkFolder, "temp");
                var result = releaseJobExtension.GetRootedPath(_ec.Object, "temp");

                Assert.Equal(rootedPath, result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PreapreAsyncShouldInitializeAgentIfSkipArtifactDownloadIsTrue()
        {
            using (TestHostContext tc = Setup(createWorkDirectory: false))
            {
                _releaseDirectoryManager.Setup(manager => manager.PrepareArtifactsDirectory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => s.Equals(id.ToString())))).Returns(map);

                releaseJobExtension.InitializeJobExtension(_ec.Object);

                Assert.Equal(Path.Combine(this.stubWorkFolder, "r1", Constants.Release.Path.ArtifactsDirectory), _ec.Object.Variables.Get(Constants.Variables.Release.AgentReleaseDirectory));
                Assert.Equal(Path.Combine(this.stubWorkFolder, "r1", Constants.Release.Path.ArtifactsDirectory), _ec.Object.Variables.Get(Constants.Variables.Release.ArtifactsDirectory));
                Assert.Equal(Path.Combine(this.stubWorkFolder, "r1", Constants.Release.Path.ArtifactsDirectory), _ec.Object.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory));
                Assert.True(Directory.Exists(this.stubWorkFolder));
                _releaseDirectoryManager.VerifyAll();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PreapreAsyncShouldInitializeAgentIfSkipArtifactDownloadIsTrueAndReleaseDefinitionIdIsNull()
        {
            using (TestHostContext tc = Setup(createWorkDirectory: false, useReleaseDefinitionId: false))
            {
                _releaseDirectoryManager.Setup(manager => manager.PrepareArtifactsDirectory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => s.Equals(releaseDefinitionName)))).Returns(map);

                releaseJobExtension.InitializeJobExtension(_ec.Object);

                Assert.Equal(Path.Combine(this.stubWorkFolder, "r1", Constants.Release.Path.ArtifactsDirectory), _ec.Object.Variables.Get(Constants.Variables.Release.AgentReleaseDirectory));
                Assert.Equal(Path.Combine(this.stubWorkFolder, "r1", Constants.Release.Path.ArtifactsDirectory), _ec.Object.Variables.Get(Constants.Variables.Release.ArtifactsDirectory));
                Assert.Equal(Path.Combine(this.stubWorkFolder, "r1", Constants.Release.Path.ArtifactsDirectory), _ec.Object.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory));
                Assert.True(Directory.Exists(this.stubWorkFolder));
                _releaseDirectoryManager.VerifyAll();
            }
        }

        private TestHostContext Setup([CallerMemberName] string name = "", bool createWorkDirectory = true, bool useReleaseDefinitionId = true)
        {
            this.stubWorkFolder = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                $"_work_{Path.GetRandomFileName()}");
            if (createWorkDirectory)
            {
                Directory.CreateDirectory(this.stubWorkFolder);
            }
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();

            _extensionManager = new Mock<IExtensionManager>();
            _sourceProvider = new Mock<ISourceProvider>();
            _releaseDirectoryManager = new Mock<IReleaseDirectoryManager>();
            var _configurationStore = new Mock<IConfigurationStore>();
            _configurationStore.Setup(store => store.GetSettings()).Returns(new AgentSettings { WorkFolder = this.stubWorkFolder });

            List<string> warnings;
            var releaseVariables = useReleaseDefinitionId
                ? GetReleaseVariables(id.ToString(), bool.TrueString)
                : GetReleaseVariables(null, bool.TrueString);
            _variables = new Variables(hc, releaseVariables, out warnings);

            hc.SetSingleton(_releaseDirectoryManager.Object);
            hc.SetSingleton(_extensionManager.Object);
            hc.SetSingleton(_configurationStore.Object);
            _ec.Setup(x => x.Variables).Returns(_variables);
            _extensionManager.Setup(x => x.GetExtensions<ISourceProvider>())
                .Returns(new List<ISourceProvider> { _sourceProvider.Object });
            _sourceProvider.Setup(x => x.RepositoryType).Returns(RepositoryTypes.TfsGit);

            releaseJobExtension = new ReleaseJobExtension();
            releaseJobExtension.Initialize(hc);
            return hc;
        }

        private Dictionary<string, VariableValue> GetReleaseVariables(string releaseDefinitionId, string skipArtifactDownload)
        {
            var releaseVariables = new Dictionary<string, VariableValue>();
            releaseVariables.Add(Constants.Variables.Release.ArtifactsDirectory, this.stubWorkFolder);
            releaseVariables.Add(Constants.Variables.Release.ReleaseDefinitionName, releaseDefinitionName);
            releaseVariables.Add(Constants.Variables.System.TeamProjectId, projectId.ToString());
            releaseVariables.Add(Constants.Variables.Release.ReleaseId, releaseId.ToString());
            releaseVariables.Add(Constants.Variables.Release.SkipArtifactsDownload, skipArtifactDownload);
            if (releaseDefinitionId != null)
            {
                releaseVariables.Add(Constants.Variables.Release.ReleaseDefinitionId, releaseDefinitionId);
            }

            return releaseVariables;
        }
    }
}