using GitHub.Runner.Common.Util;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class HostContextL0
    {
        private HostContext _hc;
        private CancellationTokenSource _tokenSource;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CreateServiceReturnsNewInstance()
        {
            try
            {
                // Arrange.
                Setup();

                // Act.
                var reference1 = _hc.CreateService<IRunnerServer>();
                var reference2 = _hc.CreateService<IRunnerServer>();

                // Assert.
                Assert.NotNull(reference1);
                Assert.IsType<RunnerServer>(reference1);
                Assert.NotNull(reference2);
                Assert.IsType<RunnerServer>(reference2);
                Assert.False(object.ReferenceEquals(reference1, reference2));
            }
            finally
            {
                // Cleanup.
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetServiceReturnsSingleton()
        {
            try
            {
                // Arrange.
                Setup();

                // Act.
                var reference1 = _hc.GetService<IRunnerServer>();
                var reference2 = _hc.GetService<IRunnerServer>();

                // Assert.
                Assert.NotNull(reference1);
                Assert.IsType<RunnerServer>(reference1);
                Assert.NotNull(reference2);
                Assert.True(object.ReferenceEquals(reference1, reference2));
            }
            finally
            {
                // Cleanup.
                Teardown();
            }
        }

        public void Setup([CallerMemberName] string testName = "")
        {
            _tokenSource = new CancellationTokenSource();
            _hc = new HostContext(
                hostType: "L0Test",
                logFile: Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), $"trace_{nameof(HostContextL0)}_{testName}.log"));
        }

        public void Teardown()
        {
            _hc?.Dispose();
            _tokenSource?.Dispose();
        }
    }
}
