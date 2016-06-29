using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Tests;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Xunit;

namespace Test.L0.Worker.Release
{
    public sealed class ReleaseDirectoryManagerL0
    {
        private const string StubCollectionId = "1234-5678";
        private const string StubProjectId = "234-567";
        private const string StubReleaseDefinitionId = "2024";
        private string stubWorkFolder;
        private ReleaseDirectoryManager releaseDirectoryManager;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PrepareArtifactsDirectoryShouldReturnValidMapIfTheWorkingDirectoryIsEmpty()
        {
            using (TestHostContext testHostContext = Initialize())
            {

                var map = this.releaseDirectoryManager.PrepareArtifactsDirectory(
                    this.stubWorkFolder,
                    StubCollectionId,
                    StubProjectId,
                    StubReleaseDefinitionId);

                Assert.Equal(map.ReleaseDirectory, string.Format("{0}1", Constants.Release.Path.ReleaseDirectoryPrefix));
                Assert.True(File.Exists(Path.Combine(
                    this.stubWorkFolder,
                    Constants.Release.Path.RootMappingDirectory,
                    StubCollectionId,
                    StubProjectId,
                    StubReleaseDefinitionId,
                    Constants.Release.Path.DefinitionMapping)));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PrepareArtifactsDirectoryShouldReturnValidMapIfTheWorkingDirectoryContainsFolders()
        {
            using (TestHostContext testHostContext = Initialize())
            {
                Directory.CreateDirectory(Path.Combine(
                    this.stubWorkFolder,
                    string.Format("{0}2", Constants.Release.Path.ReleaseDirectoryPrefix)));
                Directory.CreateDirectory(Path.Combine(
                    this.stubWorkFolder,
                    "temp"));
                var map = this.releaseDirectoryManager.PrepareArtifactsDirectory(
                    this.stubWorkFolder,
                    StubCollectionId,
                    StubProjectId,
                    StubReleaseDefinitionId);

                Assert.Equal(map.ReleaseDirectory, "r3");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PrepareArtifactsDirectoryShouldReturnExistingMapIfItExists()
        {
            using (TestHostContext testHostContext = Initialize())
            {

                this.releaseDirectoryManager.PrepareArtifactsDirectory(
                    this.stubWorkFolder,
                    StubCollectionId,
                    StubProjectId,
                    StubReleaseDefinitionId);
                var existingMap = this.releaseDirectoryManager.PrepareArtifactsDirectory(
                    this.stubWorkFolder,
                    StubCollectionId,
                    StubProjectId,
                    StubReleaseDefinitionId);

                Assert.Equal(existingMap.ReleaseDirectory, "r1");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PrepareArtifactsDirectoryShouldReturnMapIfWorkDirectoryDoesNotExist()
        {
            using (TestHostContext testHostContext = Initialize(createWorkDirectory: false))
            {

                this.releaseDirectoryManager.PrepareArtifactsDirectory(
                    this.stubWorkFolder,
                    StubCollectionId,
                    StubProjectId,
                    StubReleaseDefinitionId);
                var existingMap = this.releaseDirectoryManager.PrepareArtifactsDirectory(
                    this.stubWorkFolder,
                    StubCollectionId,
                    StubProjectId,
                    StubReleaseDefinitionId);

                Assert.Equal(existingMap.ReleaseDirectory, "r1");
            }
        }

        private TestHostContext Initialize([CallerMemberName] string name = "", bool createWorkDirectory = true)
        {
            this.stubWorkFolder = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                $"_work_{Path.GetRandomFileName()}");
            if (createWorkDirectory)
            {
                Directory.CreateDirectory(this.stubWorkFolder);
            }

            var hostContext =  new TestHostContext(this, name);
            this.releaseDirectoryManager = new ReleaseDirectoryManager();
            this.releaseDirectoryManager.Initialize(hostContext);

            return hostContext;
        }
    }
}