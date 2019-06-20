using GitHub.Runner.Worker;
using Moq;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class TrackingManagerL0
    {
        private Mock<IExecutionContext> _ec;
        private TrackingManager _trackingManager;
        private string _workFolder;

        public TestHostContext Setup([CallerMemberName] string name = "")
        {
            // Setup the host context.
            TestHostContext hc = new TestHostContext(this, name);

            // Create a random work path.
            _workFolder = hc.GetDirectory(WellKnownDirectory.Work);

            // Setup the execution context.
            _ec = new Mock<IExecutionContext>();
            GitHubContext githubContext = new GitHubContext();
            _ec.Setup(x => x.GetGitHubContext("repository")).Returns("actions/runner");

            // Setup the tracking manager.
            _trackingManager = new TrackingManager();
            _trackingManager.Initialize(hc);

            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreatesTrackingConfig()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
                DateTimeOffset testStartOn = DateTimeOffset.Now;

                // Act.
                _trackingManager.Create(_ec.Object, trackingFile);

                // Assert.
                TrackingConfig config = _trackingManager.LoadIfExists(_ec.Object, trackingFile);
                Assert.Equal("runner", config.PipelineDirectory);
                Assert.Equal($"runner{Path.DirectorySeparatorChar}runner", config.WorkspaceDirectory);
                Assert.Equal("actions/runner", config.RepositoryName);

                Assert.Equal(1, config.Repositories.Count);
                Assert.Equal($"runner{Path.DirectorySeparatorChar}runner", config.Repositories["actions/runner"].RepositoryPath);

                // Manipulate the expected seconds due to loss of granularity when the
                // date-time-offset is serialized in a friendly format.
                Assert.True(testStartOn.AddSeconds(-1) <= config.LastRunOn);
                Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastRunOn);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                Directory.CreateDirectory(_workFolder);
                string filePath = Path.Combine(_workFolder, "trackingconfig.json");
                _trackingManager.Create(_ec.Object, filePath);

                // Act.
                TrackingConfig config = _trackingManager.LoadIfExists(_ec.Object, filePath);

                // Assert.
                Assert.NotNull(config);
                Assert.Equal("actions/runner", config.RepositoryName);
                Assert.Equal("runner", config.PipelineDirectory);
                Assert.Equal($"runner{Path.DirectorySeparatorChar}runner", config.WorkspaceDirectory);
                Assert.Equal(1, config.Repositories.Count);
                Assert.Equal($"runner{Path.DirectorySeparatorChar}runner", config.Repositories["actions/runner"].RepositoryPath);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig_NotExists()
        {
            using (TestHostContext hc = Setup())
            {
                // Act.
                TrackingConfig config = _trackingManager.LoadIfExists(
                    _ec.Object,
                    Path.Combine(_workFolder, "foo.json"));

                // Assert.
                Assert.Null(config);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UpdatesTrackingConfigJobRunProperties()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                TrackingConfig config = new TrackingConfig() { RepositoryName = "actions/runner" };
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");

                // Act.
                _trackingManager.Update(_ec.Object, config, trackingFile);

                // Assert.
                config = _trackingManager.LoadIfExists(_ec.Object, trackingFile);
                Assert.NotNull(config);
                Assert.Equal("actions/runner", config.RepositoryName);
            }
        }
    }
}
