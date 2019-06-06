﻿using GitHub.Runner.Common.Capabilities;
using GitHub.Runner.Listener.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
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
                Mock<IConfigurationManager> configurationManager = new Mock<IConfigurationManager>();
                hc.SetSingleton<IConfigurationManager>(configurationManager.Object);
                
                // Arrange
                var provider = new RunnerCapabilitiesProvider();
                provider.Initialize(hc);
                var settings = new RunnerSettings() { AgentName = "IAmAgent007" };

                // Act
                List<Capability> capabilities = await provider.GetCapabilitiesAsync(settings, tokenSource.Token);

                // Assert
                Assert.NotNull(capabilities);
                Capability runnerNameCapability = capabilities.SingleOrDefault(x => string.Equals(x.Name, "Runner.Name", StringComparison.Ordinal));
                Assert.NotNull(runnerNameCapability);
                Assert.Equal("IAmAgent007", runnerNameCapability.Value);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void TestInteractiveSessionCapability()
        {
            using (var hc = new TestHostContext(this))
            using (var tokenSource = new CancellationTokenSource())
            {
                hc.StartupType = StartupType.AutoStartup;
                await VerifyInteractiveSessionCapability(hc, tokenSource.Token, true);

                hc.StartupType = StartupType.Service;
                await VerifyInteractiveSessionCapability(hc, tokenSource.Token, false);

                hc.StartupType = StartupType.Manual;
                await VerifyInteractiveSessionCapability(hc, tokenSource.Token, true);
            }
        }

        private async Task VerifyInteractiveSessionCapability(IHostContext hc, CancellationToken token, bool expectedValue)
        {
            // Arrange
            var provider = new RunnerCapabilitiesProvider();
            provider.Initialize(hc);
            var settings = new RunnerSettings() { AgentName = "IAmAgent007" };

            // Act
            List<Capability> capabilities = await provider.GetCapabilitiesAsync(settings, token);

            // Assert
            Assert.NotNull(capabilities);
            Capability iSessionCapability = capabilities.SingleOrDefault(x => string.Equals(x.Name, "InteractiveSession", StringComparison.Ordinal));
            Assert.NotNull(iSessionCapability);
            bool.TryParse(iSessionCapability.Value, out bool isInteractive);
            Assert.Equal(expectedValue, isInteractive);
        }
    }
}
