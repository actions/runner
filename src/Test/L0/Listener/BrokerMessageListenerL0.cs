using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class BrokerMessageListenerL0
    {
        private readonly RunnerSettings _settings;
        private readonly Mock<IConfigurationManager> _config;
        private readonly Mock<IBrokerServer> _brokerServer;
        private readonly Mock<ICredentialManager> _credMgr;

        public BrokerMessageListenerL0()
        {
            _settings = new RunnerSettings { AgentId = 1, AgentName = "myagent", PoolId = 123, PoolName = "default", ServerUrlV2 = "http://myserver", WorkFolder = "_work" };
            _config = new Mock<IConfigurationManager>();
            _config.Setup(x => x.LoadSettings()).Returns(_settings);
            _brokerServer = new Mock<IBrokerServer>();
            _credMgr = new Mock<ICredentialManager>();
            _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreatesSessionAsync()
        {
            using TestHostContext tc = CreateTestContext();
            using var tokenSource = new CancellationTokenSource();

            // Arrange.
            _brokerServer.Setup(x => x.ConnectAsync(new Uri(_settings.ServerUrlV2), It.Is<VssCredentials>(y => y != null))).Returns(Task.FromResult(new BrokerSession { id = "my-phony-session-id" }));
            BrokerMessageListener listener = new();
            listener.Initialize(tc);

            // Act.
            bool result = await listener.CreateSessionAsync(tokenSource.Token);

            // Assert.
            Assert.True(result);
            Assert.Equal("my-phony-session-id", listener._sessionId);
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new(this, testName);
            tc.SetSingleton<IConfigurationManager>(_config.Object);
            tc.SetSingleton<IBrokerServer>(_brokerServer.Object);
            tc.SetSingleton<ICredentialManager>(_credMgr.Object);
            return tc;
        }
    }
}
