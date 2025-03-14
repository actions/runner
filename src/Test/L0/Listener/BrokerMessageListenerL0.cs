using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
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
        private readonly Mock<IRunnerServer> _runnerServer;
        private readonly Mock<ICredentialManager> _credMgr;
        private Mock<IConfigurationStore> _store;


        public BrokerMessageListenerL0()
        {
            _settings = new RunnerSettings { AgentId = 1, AgentName = "myagent", PoolId = 123, PoolName = "default", ServerUrl = "http://myserver", WorkFolder = "_work", ServerUrlV2 = "http://myserverv2" };
            _config = new Mock<IConfigurationManager>();
            _config.Setup(x => x.LoadSettings()).Returns(_settings);
            _credMgr = new Mock<ICredentialManager>();
            _store = new Mock<IConfigurationStore>();
            _brokerServer = new Mock<IBrokerServer>();
            _runnerServer = new Mock<IRunnerServer>();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreatesSession()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                BrokerMessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.Equal(CreateSessionResult.Success, result);
                _brokerServer
                   .Verify(x => x.CreateSessionAsync(
                       It.Is<TaskAgentSession>(y => y != null),
                       tokenSource.Token), Times.Once());
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new(this, testName);
            tc.SetSingleton<IConfigurationManager>(_config.Object);
            tc.SetSingleton<ICredentialManager>(_credMgr.Object);
            tc.SetSingleton<IConfigurationStore>(_store.Object);
            tc.SetSingleton<IBrokerServer>(_brokerServer.Object);
            tc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            return tc;
        }
    }
}
