using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
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
        public async Task GetNextMessage_RecreatesSessionOnSessionExpired()
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
                var throwSessionExpired = true;
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
                        if (throwSessionExpired)
                        {
                            throwSessionExpired = false;
                            throw new TaskAgentSessionExpiredException("Runner session is invalid");
                        }

                        return expectedMessage;
                    });

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
                       It.IsAny<CancellationToken>()), Times.Exactly(2));

                // Session recreated once on the expired exception (plus the initial create above).
                _brokerServer
                   .Verify(x => x.CreateSessionAsync(
                       It.Is<TaskAgentSession>(y => y != null),
                       tokenSource.Token), Times.Exactly(2));
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

        // Mirrors MessageListenerL0's coverage for the same fix (actions/runner#4648), for the
        // broker listener - flagged in review as missing so a future change couldn't regress
        // just the broker path without either suite catching it.
        //
        // Covers actions/runner#4648: an OAuth "invalid_client" caused purely by clock skew
        // (the token request itself carries "Current server time is ..." in its message, the
        // same sentinel IsSessionCreationExceptionRetriable already checks) must not be treated
        // as a deleted registration. It must fall through to the existing clock-skew retry path
        // instead of returning CreateSessionResult.Failure (which Runner.cs maps to
        // ReturnCode.TerminatedError, so systemd never retries).
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task CreateSession_ClockSkewInvalidClient_RetriesInsteadOfTerminating()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var mockTerm = new Mock<ITerminal>();
                tc.SetSingleton<ITerminal>(mockTerm.Object);

                var rsaKeyManager = new Mock<IRSAKeyManager>();
                rsaKeyManager.Setup(x => x.GetKey()).Returns(RSA.Create(2048));
                tc.SetSingleton<IRSAKeyManager>(rsaKeyManager.Object);

                var oauth = new OAuthCredential();
                oauth.CredentialData = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                oauth.CredentialData.Data.Add("clientId", "someClientId");
                oauth.CredentialData.Data.Add("authorizationUrl", "https://s.server");
                var federatedCreds = oauth.GetVssCredentials(tc, false);
                // BrokerMessageListener.CreateSessionAsync loads _credsV2 via
                // LoadCredentials(allowAuthUrlV2: true) - that is the credential object the
                // invalid_client guard actually inspects.
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(federatedCreds);

                var expectedSession = new TaskAgentSession();

                // Reproduces the real message shape from the issue: a token-expiry check against
                // the server's clock, surfaced by the service as an OAuth "invalid_client" error.
                var skewException = new VssOAuthTokenRequestException(
                    "The token expired on 08/24/2026 19:15:44. Current server time is 08/25/2026 01:44:14.")
                {
                    Error = "invalid_client",
                };

                _brokerServer
                    .SetupSequence(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Throws(skewException)
                    .Returns(Task.FromResult(expectedSession));

                // Act.
                BrokerMessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert: the clock-skew invalid_client did not terminate the call - it fell
                // through to the existing clock-skew retry path, and the next attempt (clock now
                // caught up) succeeded.
                Assert.Equal(CreateSessionResult.Success, result);
                _brokerServer
                    .Verify(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Exactly(2));

                mockTerm.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("registration has been deleted"))), Times.Never);
            }
        }

        // Companion to the test above: a genuine deleted-registration invalid_client (no
        // clock-skew sentinel in the message) must keep terminating immediately, exactly as
        // before. This fix must not weaken true deleted-registration handling.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task CreateSession_InvalidClientWithoutClockSkew_StillTerminatesAsDeletedRegistration()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var mockTerm = new Mock<ITerminal>();
                tc.SetSingleton<ITerminal>(mockTerm.Object);

                var rsaKeyManager = new Mock<IRSAKeyManager>();
                rsaKeyManager.Setup(x => x.GetKey()).Returns(RSA.Create(2048));
                tc.SetSingleton<IRSAKeyManager>(rsaKeyManager.Object);

                var oauth = new OAuthCredential();
                oauth.CredentialData = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                oauth.CredentialData.Data.Add("clientId", "someClientId");
                oauth.CredentialData.Data.Add("authorizationUrl", "https://s.server");
                var federatedCreds = oauth.GetVssCredentials(tc, false);
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(federatedCreds);

                // A genuine deleted-registration response: invalid_client with no clock-skew
                // sentinel anywhere in the message.
                var deletedException = new VssOAuthTokenRequestException("Client authentication failed.")
                {
                    Error = "invalid_client",
                };

                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Throws(deletedException);

                // Act.
                BrokerMessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert: still terminates immediately as a deleted registration - exactly one
                // attempt, no retry.
                Assert.Equal(CreateSessionResult.Failure, result);
                _brokerServer
                    .Verify(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                mockTerm.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("registration has been deleted"))), Times.Once);
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
