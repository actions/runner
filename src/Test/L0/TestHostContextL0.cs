using System;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class TestHostContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetService_UnregisteredService_FailsLoud()
        {
            using (var hc = new TestHostContext(this))
            {
                // A test that forgets to mock a service dependency must fail loudly,
                // not silently exercise the real implementation.
                Assert.Throws<Exception>(() => hc.GetService<IJobServer>());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetService_OTelTraceExporter_ResolvesDefaultWithoutRegistration()
        {
            using (var hc = new TestHostContext(this))
            {
                // Allowlisted leaf telemetry service: production code resolves it
                // lazily on many paths, and it is inert unless its env vars are set.
                var exporter = hc.GetService<IOTelTraceExporter>();
                Assert.NotNull(exporter);
                Assert.Same(exporter, hc.GetService<IOTelTraceExporter>());
            }
        }
    }
}
