using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Worker;
using Moq;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;
using System;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class PipelineDirectoryManagerL0
    {
        private PipelineDirectoryManager _pipelineDirectoryManager;
        private Mock<IExecutionContext> _ec;
        private Pipelines.WorkspaceOptions _workspaceOptions;
        private TrackingConfig _existingConfig;
        private TrackingConfig _newConfig;
        private string _trackingFile;
        private Mock<ITrackingManager> _trackingManager;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreatesPipelineDirectories()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(default(TrackingConfig));
                _trackingManager.Setup(x => x.Create(_ec.Object, _trackingFile)).Returns(new TrackingConfig(_ec.Object));

                // Act.
                _newConfig = _pipelineDirectoryManager.PrepareDirectory(_ec.Object, _workspaceOptions);

                // Assert.
                _trackingManager.Verify(x => x.Create(_ec.Object, _trackingFile));
                Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _newConfig.WorkspaceDirectory)));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DeletesResourceDirectoryWhenCleanIsResources()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _existingConfig = new TrackingConfig(_ec.Object);
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(_existingConfig);

                _workspaceOptions.Clean = Pipelines.PipelineConstants.WorkspaceCleanOptions.Resources;
                string workspaceDirectory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _existingConfig.WorkspaceDirectory);
                string sourceFile = Path.Combine(workspaceDirectory, "some subdirectory", "some source file");
                Directory.CreateDirectory(Path.GetDirectoryName(sourceFile));
                File.WriteAllText(path: sourceFile, contents: "some source contents");

                // Act.
                _pipelineDirectoryManager.PrepareDirectory(_ec.Object, _workspaceOptions);

                // Assert.
                Assert.True(Directory.Exists(workspaceDirectory));
                Assert.Equal(0, Directory.GetFileSystemEntries(workspaceDirectory, "*", SearchOption.AllDirectories).Length);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DeletesNonResourceDirectoryWhenCleanIsOutputs()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _existingConfig = new TrackingConfig(_ec.Object);
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(_existingConfig);

                _workspaceOptions.Clean = Pipelines.PipelineConstants.WorkspaceCleanOptions.Outputs;
                string nonResourceDirectory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _existingConfig.PipelineDirectory, "somedir");
                string sourceFile = Path.Combine(nonResourceDirectory, "some subdirectory", "some source file");
                Directory.CreateDirectory(Path.GetDirectoryName(sourceFile));
                File.WriteAllText(path: sourceFile, contents: "some source contents");

                // Act.
                _pipelineDirectoryManager.PrepareDirectory(_ec.Object, _workspaceOptions);

                // Assert.
                Assert.False(Directory.Exists(nonResourceDirectory));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecreatesPipelinesDirectoryWhenCleanIsAll()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _existingConfig = new TrackingConfig(_ec.Object);
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(_existingConfig);

                _workspaceOptions.Clean = Pipelines.PipelineConstants.WorkspaceCleanOptions.All;

                string pipelinesDirectory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _existingConfig.PipelineDirectory);
                string looseFile = Path.Combine(pipelinesDirectory, "some loose directory", "some loose file");
                Directory.CreateDirectory(Path.GetDirectoryName(looseFile));
                File.WriteAllText(path: looseFile, contents: "some loose file contents");

                // Act.
                _pipelineDirectoryManager.PrepareDirectory(_ec.Object, _workspaceOptions);

                // Assert.
                Assert.Equal(1, Directory.GetFileSystemEntries(pipelinesDirectory, "*", SearchOption.AllDirectories).Length);
                Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _existingConfig.WorkspaceDirectory)));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UpdatesExistingConfig()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _existingConfig = new TrackingConfig(_ec.Object);
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(_existingConfig);

                // Act.
                _pipelineDirectoryManager.PrepareDirectory(_ec.Object, _workspaceOptions);

                // Assert.
                _trackingManager.Verify(x => x.LoadIfExists(_ec.Object, _trackingFile));
                _trackingManager.Verify(x => x.Update(_ec.Object, _existingConfig, _trackingFile));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UpdatesRepositoryDirectoryWorkspaceRepo()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _existingConfig = new TrackingConfig(_ec.Object);
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(_existingConfig);

                // Act.
                _pipelineDirectoryManager.UpdateRepositoryDirectory(_ec.Object, "actions/runner", Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _existingConfig.PipelineDirectory, "my_new_path"), true);

                // Assert.
                _trackingManager.Verify(x => x.LoadIfExists(_ec.Object, _trackingFile));
                _trackingManager.Verify(x => x.Update(_ec.Object, _existingConfig, _trackingFile));
                _ec.Verify(x => x.SetGitHubContext("workspace", Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _existingConfig.PipelineDirectory, "my_new_path")));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UpdatesRepositoryDirectoryNoneWorkspaceRepo()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _existingConfig = new TrackingConfig(_ec.Object);
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(_existingConfig);

                // Act.
                _pipelineDirectoryManager.UpdateRepositoryDirectory(_ec.Object, "actions/notrunner", Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), _existingConfig.PipelineDirectory, "notrunner"), false);

                // Assert.
                _trackingManager.Verify(x => x.LoadIfExists(_ec.Object, _trackingFile));
                _trackingManager.Verify(x => x.Update(_ec.Object, _existingConfig, _trackingFile));
                _ec.Verify(x => x.SetGitHubContext("workspace", It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UpdatesRepositoryDirectoryThrowOnInvalidPath()
        {
            // Arrange.
            using (TestHostContext hc = Setup())
            {
                _existingConfig = new TrackingConfig(_ec.Object);
                _trackingManager.Setup(x => x.LoadIfExists(_ec.Object, _trackingFile)).Returns(_existingConfig);

                // Act.
                Assert.ThrowsAny<ArgumentException>(()=> _pipelineDirectoryManager.UpdateRepositoryDirectory(_ec.Object, "actions/notrunner", Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), "not_under_pipeline_directory"), false));
            }
        }


        private TestHostContext Setup(
            [CallerMemberName] string name = "")
        {
            // Setup the host context.
            TestHostContext hc = new TestHostContext(this, name);

            // Setup the execution context.
            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Global).Returns(new GlobalContext());

            GitHubContext githubContext = new GitHubContext();
            _ec.Setup(x => x.GetGitHubContext("repository")).Returns("actions/runner");

            // Store the expected tracking file path.
            _trackingFile = Path.Combine(
                hc.GetDirectory(WellKnownDirectory.Work),
                Constants.Pipeline.Path.PipelineMappingDirectory,
                "actions/runner",
                Constants.Pipeline.Path.TrackingConfigFile);

            _workspaceOptions = new Pipelines.WorkspaceOptions();

            // Setup the tracking manager.
            _trackingManager = new Mock<ITrackingManager>();
            hc.SetSingleton<ITrackingManager>(_trackingManager.Object);

            // Setup the build directory manager.
            _pipelineDirectoryManager = new PipelineDirectoryManager();
            _pipelineDirectoryManager.Initialize(hc);
            return hc;
        }
    }
}
