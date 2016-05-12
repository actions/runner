using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Microsoft.VisualStudio.Services.Agent.Util;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener
{
    public sealed class AgentCapabilitiesProviderTestL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void TestGetCapabilities()
        {
            using (var hc = new TestHostContext(this))
            using (var tokenSource = new CancellationTokenSource())
            {
                // Arrange
                var provider = new AgentCapabilitiesProvider();
                provider.Initialize(hc);
                var settings = new AgentSettings() { AgentName = "IAmAgent007" };

                // Act
                List<Capability> capabilities = await provider.GetCapabilitiesAsync(settings, tokenSource.Token);

                // Assert
                Assert.NotNull(capabilities);
                Capability agentNameCapability = capabilities.SingleOrDefault(x => string.Equals(x.Name, "Agent.Name", StringComparison.Ordinal));
                Assert.NotNull(agentNameCapability);
                Assert.Equal("IAmAgent007", agentNameCapability.Value);
            }
        }
    }
}
