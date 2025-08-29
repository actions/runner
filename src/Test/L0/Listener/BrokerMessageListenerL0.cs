using System;
using System.IO;
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

        public BrokerMessageListenerL0()
        {
            _settings = new RunnerSettings { AgentId = 1, AgentName = "myagent", PoolId = 123, PoolName = "default", ServerUrl = "http://myserver", WorkFolder = "_work", ServerUrlV2 = "http://myserverv2" };
            _config = new Mock<IConfigurationManager>();
            _config.Setup(x => x.LoadSettings()).Returns(_settings);
            _credMgr = new Mock<ICredentialManager>();
            _brokerServer = new Mock<IBrokerServer>();
            _runnerServer = new Mock<IRunnerServer>();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task CreatesSession()
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

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task HandleAuthMigrationChanged()
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

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

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

                tc.EnableAuthMigration("L0Test");

                var traceFile = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, traceFile, true);
                Assert.Contains("Auth migration changed", File.ReadAllText(traceFile));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task CreatesSession_DeferAuthMigration()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var throwException = true;
                var expectedSession = new TaskAgentSession();
                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(async (TaskAgentSession session, CancellationToken token) =>
                    {
                        await Task.Yield();
                        if (throwException)
                        {
                            throwException = false;
                            throw new NotSupportedException("Error during create session");
                        }

                        return expectedSession;
                    });

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                // Act.
                BrokerMessageListener listener = new();
                listener.Initialize(tc);

                tc.EnableAuthMigration("L0Test");
                Assert.True(tc.AllowAuthMigration);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.Equal(CreateSessionResult.Success, result);
                _brokerServer
                   .Verify(x => x.CreateSessionAsync(
                       It.Is<TaskAgentSession>(y => y != null),
                       tokenSource.Token), Times.Exactly(2));
                _credMgr.Verify(x => x.LoadCredentials(true), Times.Exactly(2));

                Assert.False(tc.AllowAuthMigration);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetNextMessage()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var expectedSession = new TaskAgentSession();
                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                var expectedMessage = new TaskAgentMessage();
                _brokerServer
                    .Setup(x => x.GetRunnerMessageAsync(
                        It.IsAny<Guid?>(),
                        It.IsAny<TaskAgentStatus>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(expectedMessage));

                // Act.
                BrokerMessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);
                Assert.Equal(CreateSessionResult.Success, result);

                TaskAgentMessage message = await listener.GetNextMessageAsync(tokenSource.Token);
                trace.Info("message: {0}", message);

                // Assert.
                Assert.Equal(expectedMessage, message);
                _brokerServer
                   .Verify(x => x.GetRunnerMessageAsync(
                       It.IsAny<Guid?>(),
                       It.IsAny<TaskAgentStatus>(),
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<bool>(),
                       It.IsAny<CancellationToken>()), Times.Once());

                _brokerServer.Verify(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>()), Times.Once());

                _credMgr.Verify(x => x.LoadCredentials(true), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetNextMessage_EnableAuthMigration()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var expectedSession = new TaskAgentSession();
                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                var expectedMessage = new TaskAgentMessage();
                _brokerServer
                    .Setup(x => x.GetRunnerMessageAsync(
                        It.IsAny<Guid?>(),
                        It.IsAny<TaskAgentStatus>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(expectedMessage));

                // Act.
                BrokerMessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);
                Assert.Equal(CreateSessionResult.Success, result);

                tc.EnableAuthMigration("L0Test");

                TaskAgentMessage message = await listener.GetNextMessageAsync(tokenSource.Token);
                trace.Info("message: {0}", message);

                // Assert.
                Assert.Equal(expectedMessage, message);
                _brokerServer
                   .Verify(x => x.GetRunnerMessageAsync(
                       It.IsAny<Guid?>(),
                       It.IsAny<TaskAgentStatus>(),
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<bool>(),
                       It.IsAny<CancellationToken>()), Times.Once());

                _brokerServer.Verify(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>()), Times.Exactly(2));

                _credMgr.Verify(x => x.LoadCredentials(true), Times.Exactly(2));

                Assert.True(tc.AllowAuthMigration);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetNextMessage_AuthMigrationFallback()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                tc.EnableAuthMigration("L0Test");

                // Arrange.
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var expectedSession = new TaskAgentSession();
                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                var expectedMessage = new TaskAgentMessage();
                _brokerServer
                    .Setup(x => x.GetRunnerMessageAsync(
                        It.IsAny<Guid?>(),
                        It.IsAny<TaskAgentStatus>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(async (Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, bool disableUpdate, CancellationToken token) =>
                    {
                        await Task.Yield();
                        if (tc.AllowAuthMigration)
                        {
                            throw new NotSupportedException("Error during get message");
                        }

                        return expectedMessage;
                    });

                // Act.
                BrokerMessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);
                Assert.Equal(CreateSessionResult.Success, result);

                Assert.True(tc.AllowAuthMigration);

                TaskAgentMessage message = await listener.GetNextMessageAsync(tokenSource.Token);
                trace.Info("message: {0}", message);

                // Assert.
                Assert.Equal(expectedMessage, message);
                _brokerServer
                   .Verify(x => x.GetRunnerMessageAsync(
                       It.IsAny<Guid?>(),
                       It.IsAny<TaskAgentStatus>(),
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<bool>(),
                       It.IsAny<CancellationToken>()), Times.Exactly(2));

                _brokerServer.Verify(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>()), Times.Exactly(3));

                _credMgr.Verify(x => x.LoadCredentials(true), Times.Exactly(3));

                Assert.False(tc.AllowAuthMigration);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task CreatesSessionWithProvidedSettings()
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

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                // Make sure the config is never called when settings are provided
                _config.Setup(x => x.LoadSettings()).Throws(new InvalidOperationException("Should not be called"));

                // Act.
                // Use the constructor that accepts settings
                BrokerMessageListener listener = new(_settings);
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.Equal(CreateSessionResult.Success, result);
                _brokerServer
                   .Verify(x => x.CreateSessionAsync(
                       It.Is<TaskAgentSession>(y => y != null),
                       tokenSource.Token), Times.Once());
                
                // Verify LoadSettings was never called
                _config.Verify(x => x.LoadSettings(), Times.Never());
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new(this, testName);
            tc.SetSingleton<IConfigurationManager>(_config.Object);
            tc.SetSingleton<ICredentialManager>(_credMgr.Object);
            tc.SetSingleton<IBrokerServer>(_brokerServer.Object);
            tc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            return tc;
        }
    }
}
