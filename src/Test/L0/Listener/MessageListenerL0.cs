using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using Moq;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class MessageListenerL0
    {
        private RunnerSettings _settings;
        private Mock<IConfigurationManager> _config;
        private Mock<IRunnerServer> _runnerServer;
        private Mock<ICredentialManager> _credMgr;
        private Mock<IConfigurationStore> _store;

        public MessageListenerL0()
        {
            _settings = new RunnerSettings { AgentId = 1, AgentName = "myagent", PoolId = 123, PoolName = "default", ServerUrl = "http://myserver", WorkFolder = "_work" };
            _config = new Mock<IConfigurationManager>();
            _config.Setup(x => x.LoadSettings()).Returns(_settings);
            _runnerServer = new Mock<IRunnerServer>();
            _credMgr = new Mock<ICredentialManager>();
            _store = new Mock<IConfigurationStore>();
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<IConfigurationManager>(_config.Object);
            tc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            tc.SetSingleton<ICredentialManager>(_credMgr.Object);
            tc.SetSingleton<IConfigurationStore>(_store.Object);
            return tc;
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
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DeleteSession()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                PropertyInfo sessionIdProperty = expectedSession.GetType().GetProperty("SessionId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(expectedSession, Guid.NewGuid());

                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                Assert.True(result);

                _runnerServer
                    .Setup(x => x.DeleteAgentSessionAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                await listener.DeleteSessionAsync();

                //Assert
                _runnerServer
                    .Verify(x => x.DeleteAgentSessionAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void GetNextMessage()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                PropertyInfo sessionIdProperty = expectedSession.GetType().GetProperty("SessionId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(expectedSession, Guid.NewGuid());

                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                Assert.True(result);

                var arMessages = new TaskAgentMessage[]
                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Yield();
                        return messages.Dequeue();
                    });
                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithOriginalCredential()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _runnerServer
                    .Setup(x => x.GetRunnerAuthUrlAsync(
                        _settings.PoolId,
                        _settings.AgentId))
                    .Returns(async () =>
                    {
                        await Task.Delay(10);
                        return "";
                    });

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.False(listener._useMigratedCredentials);
                Assert.True(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.NotNull(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithMigratedCredential()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                var migratedCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                migratedCred.Data["authorizationUrl"] = "https://t.server";
                migratedCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(migratedCred);

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.True(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithHostedCredential()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.False(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithMigratedCredentialFallBackOriginalSucceed()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        123,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Callback(() => { _settings.PoolId = 1234; })
                    .Throws(new TaskAgentPoolNotFoundException("L0 Pool not found"));

                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        1234,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                var originalVssCred = new VssCredentials();
                var migratedVssCred = new VssCredentials();
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(migratedVssCred);
                _credMgr.Setup(x => x.LoadCredentials(false)).Returns(originalVssCred);

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                var migratedCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                migratedCred.Data["authorizationUrl"] = "https://t.server";
                migratedCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(migratedCred);

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        It.IsAny<int>(),
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Exactly(2));
                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        originalVssCred), Times.Once);
                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        migratedVssCred), Times.Once);

                Assert.False(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.NotNull(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithMigratedCredentialFallBackOriginalStillFailed()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Throws(new TaskAgentPoolNotFoundException("L0 Pool not found"));

                var originalVssCred = new VssCredentials();
                var migratedVssCred = new VssCredentials();
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(migratedVssCred);
                _credMgr.Setup(x => x.LoadCredentials(false)).Returns(originalVssCred);

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                var migratedCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                migratedCred.Data["authorizationUrl"] = "https://t.server";
                migratedCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(migratedCred);

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.False(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Exactly(2));
                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        originalVssCred), Times.Once);
                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        migratedVssCred), Times.Once);

                Assert.False(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.NotNull(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithOriginalGetMessageWaitForMigtateToMigrated()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _runnerServer
                    .Setup(x => x.GetRunnerAuthUrlAsync(
                        _settings.PoolId,
                        _settings.AgentId))
                    .Returns(async () =>
                    {
                        await Task.Delay(10);
                        return "";
                    });

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.False(listener._useMigratedCredentials);
                Assert.True(listener._needToCheckAuthorizationUrlUpdate);

                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.NotNull(listener._authorizationUrlMigrationBackgroundTask);

                var arMessages = new TaskAgentMessage[]
                                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Delay(200);
                        return messages.Dequeue();
                    });

                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length));

                _runnerServer
                    .Verify(x => x.GetRunnerAuthUrlAsync(_settings.PoolId, _settings.AgentId), Times.AtLeast(2));

                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<VssCredentials>()), Times.Once);

                var tempLog = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, tempLog, true);
                var traceContent = File.ReadAllLines(tempLog);
                Assert.DoesNotContain(traceContent, x => x.Contains("Try connect service with migrated OAuth endpoint."));

                Assert.False(listener._useMigratedCredentials);
                Assert.True(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.NotNull(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithOriginalGetMessageMigtateToMigrated()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _runnerServer
                    .Setup(x => x.GetRunnerAuthUrlAsync(
                        _settings.PoolId,
                        _settings.AgentId))
                    .Returns(async () =>
                    {
                        await Task.Delay(100);
                        return "https://t.server";
                    });

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.False(listener._useMigratedCredentials);
                Assert.True(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.NotNull(listener._authorizationUrlMigrationBackgroundTask);

                var arMessages = new TaskAgentMessage[]
                                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Delay(200);
                        return messages.Dequeue();
                    });

                var newRunnerServer = new Mock<IRunnerServer>();
                tc.EnqueueInstance<IRunnerServer>(newRunnerServer.Object);

                var keyManager = new Mock<IRSAKeyManager>();
                keyManager.Setup(x => x.GetKey()).Returns(new RSACryptoServiceProvider(2048));
                tc.SetSingleton(keyManager.Object);

                tc.SetSingleton<IJobDispatcher>(new Mock<IJobDispatcher>().Object);
                tc.SetSingleton<ISelfUpdater>(new Mock<ISelfUpdater>().Object);

                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length));

                _runnerServer
                    .Verify(x => x.GetRunnerAuthUrlAsync(_settings.PoolId, _settings.AgentId), Times.Once);

                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<VssCredentials>()), Times.Exactly(2));

                newRunnerServer
                    .Verify(x => x.ConnectAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<VssCredentials>()), Times.Once);

                newRunnerServer
                    .Verify(x => x.GetAgentPoolsAsync(null, TaskAgentPoolType.Automation), Times.Once);

                var tempLog = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, tempLog, true);
                var traceContent = File.ReadAllLines(tempLog);
                Assert.Contains(traceContent, x => x.Contains("Try connect service with Token Service OAuth endpoint."));

                Assert.True(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithOriginalGetMessageMigtateToMigratedWaitForIdle()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _runnerServer
                    .Setup(x => x.GetRunnerAuthUrlAsync(
                        _settings.PoolId,
                        _settings.AgentId))
                    .Returns(async () =>
                    {
                        await Task.Delay(10);
                        return "https://t.server";
                    });

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.False(listener._useMigratedCredentials);
                Assert.True(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.NotNull(listener._authorizationUrlMigrationBackgroundTask);

                var arMessages = new TaskAgentMessage[]
                                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                var busy = true;
                var counter = 0;
                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Delay(200);
                        if (++counter == 4)
                        {
                            busy = false;
                        }
                        return messages.Dequeue();
                    });

                var newRunnerServer = new Mock<IRunnerServer>();
                tc.EnqueueInstance<IRunnerServer>(newRunnerServer.Object);

                var keyManager = new Mock<IRSAKeyManager>();
                keyManager.Setup(x => x.GetKey()).Returns(new RSACryptoServiceProvider(2048));
                tc.SetSingleton(keyManager.Object);

                var jobDispatcher = new Mock<IJobDispatcher>();

                jobDispatcher.Setup(x => x.Busy).Returns(() =>
                {
                    return busy;
                });
                tc.SetSingleton<IJobDispatcher>(jobDispatcher.Object);
                tc.SetSingleton<ISelfUpdater>(new Mock<ISelfUpdater>().Object);

                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length));

                _runnerServer
                    .Verify(x => x.GetRunnerAuthUrlAsync(_settings.PoolId, _settings.AgentId), Times.Once);

                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<VssCredentials>()), Times.Exactly(2));

                newRunnerServer
                    .Verify(x => x.ConnectAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<VssCredentials>()), Times.Once);

                newRunnerServer
                    .Verify(x => x.GetAgentPoolsAsync(null, TaskAgentPoolType.Automation), Times.Once);

                var tempLog = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, tempLog, true);
                var traceContent = File.ReadAllLines(tempLog);
                Assert.Contains(traceContent, x => x.Contains("Job or runner updates in progress, update credentials next time."));
                Assert.Contains(traceContent, x => x.Contains("Try connect service with Token Service OAuth endpoint."));

                Assert.True(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithMigratedGetMessageNotMigrateAgain()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _runnerServer
                    .Setup(x => x.GetRunnerAuthUrlAsync(
                        _settings.PoolId,
                        _settings.AgentId))
                    .Returns(async () =>
                    {
                        await Task.Delay(10);
                        return "https://t.server";
                    });

                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                var migratedCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                migratedCred.Data["authorizationUrl"] = "https://t.server";
                migratedCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(migratedCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.False(listener._useMigratedCredentials);
                Assert.True(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.NotNull(listener._authorizationUrlMigrationBackgroundTask);

                var arMessages = new TaskAgentMessage[]
                                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Delay(200);
                        return messages.Dequeue();
                    });

                var newRunnerServer = new Mock<IRunnerServer>();
                tc.EnqueueInstance<IRunnerServer>(newRunnerServer.Object);

                var keyManager = new Mock<IRSAKeyManager>();
                keyManager.Setup(x => x.GetKey()).Returns(new RSACryptoServiceProvider(2048));
                tc.SetSingleton(keyManager.Object);

                tc.SetSingleton<IJobDispatcher>(new Mock<IJobDispatcher>().Object);
                tc.SetSingleton<ISelfUpdater>(new Mock<ISelfUpdater>().Object);

                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length));

                _runnerServer
                    .Verify(x => x.GetRunnerAuthUrlAsync(_settings.PoolId, _settings.AgentId), Times.Once);

                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<VssCredentials>()), Times.Once);

                newRunnerServer
                    .Verify(x => x.ConnectAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<VssCredentials>()), Times.Never);

                newRunnerServer
                    .Verify(x => x.GetAgentPoolsAsync(null, TaskAgentPoolType.Automation), Times.Never);

                var tempLog = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, tempLog, true);
                var traceContent = File.ReadAllLines(tempLog);
                Assert.Contains(traceContent, x => x.Contains("No needs to update authorization url"));

                Assert.False(listener._useMigratedCredentials);
                Assert.True(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.NotNull(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithOriginalGetMessageMigrateToMigratedFallbackToOriginal()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                var originalVssCred = new VssCredentials();
                var migratedVssCred = new VssCredentials();
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(migratedVssCred);
                _credMgr.Setup(x => x.LoadCredentials(false)).Returns(originalVssCred);

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                var migratedCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                migratedCred.Data["authorizationUrl"] = "https://t.server";
                migratedCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(migratedCred);

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.True(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);

                var arMessages = new TaskAgentMessage[]
                                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                var counter = 0;
                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Delay(200);
                        counter++;

                        if (counter == 5)
                        {
                            throw new TaskAgentNotFoundException("L0 runner not found");
                        }

                        if (counter == 6)
                        {
                            Assert.NotNull(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                            Assert.False(listener._useMigratedCredentials);
                        }

                        return messages.Dequeue();
                    });

                var newRunnerServer = new Mock<IRunnerServer>();
                tc.EnqueueInstance<IRunnerServer>(newRunnerServer.Object);

                var keyManager = new Mock<IRSAKeyManager>();
                keyManager.Setup(x => x.GetKey()).Returns(new RSACryptoServiceProvider(2048));
                tc.SetSingleton(keyManager.Object);

                tc.SetSingleton<IJobDispatcher>(new Mock<IJobDispatcher>().Object);
                tc.SetSingleton<ISelfUpdater>(new Mock<ISelfUpdater>().Object);

                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length + 1));

                _runnerServer
                    .Verify(x => x.GetRunnerAuthUrlAsync(_settings.PoolId, _settings.AgentId), Times.Never);

                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<VssCredentials>()), Times.AtLeast(2));

                newRunnerServer
                    .Verify(x => x.ConnectAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<VssCredentials>()), Times.Never);

                newRunnerServer
                    .Verify(x => x.GetAgentPoolsAsync(null, TaskAgentPoolType.Automation), Times.Never);

                var tempLog = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, tempLog, true);
                var traceContent = File.ReadAllLines(tempLog);
                Assert.Contains(traceContent, x => x.Contains("Fallback to original credentials and try again."));

                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithOriginalGetMessageMigrateToMigratedFallbackToOriginalReattemptMigrated()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                var originalVssCred = new VssCredentials();
                var migratedVssCred = new VssCredentials();
                _credMgr.Setup(x => x.LoadCredentials(true)).Returns(migratedVssCred);
                _credMgr.Setup(x => x.LoadCredentials(false)).Returns(originalVssCred);

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                var migratedCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                migratedCred.Data["authorizationUrl"] = "https://t.server";
                migratedCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(migratedCred);

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                Assert.True(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);

                var arMessages = new TaskAgentMessage[]
                                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                var counter = 0;
                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Delay(200);
                        counter++;

                        if (counter == 2)
                        {
                            throw new TaskAgentNotFoundException("L0 runner not found");
                        }

                        if (counter == 3)
                        {
                            Assert.NotNull(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                        }

                        return messages.Dequeue();
                    });

                var newRunnerServer = new Mock<IRunnerServer>();
                tc.EnqueueInstance<IRunnerServer>(newRunnerServer.Object);

                var keyManager = new Mock<IRSAKeyManager>();
                keyManager.Setup(x => x.GetKey()).Returns(new RSACryptoServiceProvider(2048));
                tc.SetSingleton(keyManager.Object);

                tc.SetSingleton<IJobDispatcher>(new Mock<IJobDispatcher>().Object);
                tc.SetSingleton<ISelfUpdater>(new Mock<ISelfUpdater>().Object);

                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length + 1));

                _runnerServer
                    .Verify(x => x.GetRunnerAuthUrlAsync(_settings.PoolId, _settings.AgentId), Times.Never);

                _runnerServer
                    .Verify(x => x.ConnectAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<VssCredentials>()), Times.Exactly(3));

                newRunnerServer
                    .Verify(x => x.ConnectAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<VssCredentials>()), Times.Never);

                newRunnerServer
                    .Verify(x => x.GetAgentPoolsAsync(null, TaskAgentPoolType.Automation), Times.Never);

                var tempLog = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, tempLog, true);
                var traceContent = File.ReadAllLines(tempLog);
                Assert.Contains(traceContent, x => x.Contains("Fallback to original credentials and try again."));
                Assert.Contains(traceContent, x => x.Contains("Re-attempt to use migrated credential"));

                Assert.True(listener._useMigratedCredentials);
                Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void CreateSessionWithOriginalGetMessageWithOriginalEnvOverwrite()
        {
            try
            {
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_SPSAUTHURL", "1");
                using (TestHostContext tc = CreateTestContext())
                using (var tokenSource = new CancellationTokenSource())
                {
                    Tracing trace = tc.GetTrace();

                    // Arrange.
                    var expectedSession = new TaskAgentSession();
                    _runnerServer
                        .Setup(x => x.CreateAgentSessionAsync(
                            _settings.PoolId,
                            It.Is<TaskAgentSession>(y => y != null),
                            tokenSource.Token))
                        .Returns(Task.FromResult(expectedSession));

                    var originalVssCred = new VssCredentials();
                    var migratedVssCred = new VssCredentials();
                    _credMgr.Setup(x => x.LoadCredentials(false)).Returns(originalVssCred);
                    _credMgr.Setup(x => x.LoadCredentials(true)).Returns(migratedVssCred);

                    var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                    originalCred.Data["authorizationUrl"] = "https://s.server";
                    originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                    var migratedCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                    migratedCred.Data["authorizationUrl"] = "https://t.server";
                    migratedCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                    _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                    _store.Setup(x => x.GetMigratedCredentials()).Returns(migratedCred);

                    // Act.
                    MessageListener listener = new MessageListener();
                    listener.Initialize(tc);

                    bool result = await listener.CreateSessionAsync(tokenSource.Token);
                    trace.Info("result: {0}", result);

                    // Assert.
                    Assert.True(result);
                    _runnerServer
                        .Verify(x => x.CreateAgentSessionAsync(
                            _settings.PoolId,
                            It.Is<TaskAgentSession>(y => y != null),
                            tokenSource.Token), Times.Once());

                    Assert.False(listener._useMigratedCredentials);
                    Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                    Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                    Assert.Null(listener._authorizationUrlMigrationBackgroundTask);

                    var arMessages = new TaskAgentMessage[]
                                    {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        },
                        new TaskAgentMessage
                        {
                            Body = "somebody2",
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        null,  //should be skipped by GetNextMessageAsync implementation
                        null,
                        new TaskAgentMessage
                        {
                            Body = "somebody3",
                            MessageId = 4236,
                            MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                        }
                                    };
                    var messages = new Queue<TaskAgentMessage>(arMessages);

                    _runnerServer
                        .Setup(x => x.GetAgentMessageAsync(
                            _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token))
                        .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                        {
                            await Task.Delay(1);
                            return messages.Dequeue();
                        });

                    TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                    TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                    TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                    Assert.Equal(arMessages[0], message1);
                    Assert.Equal(arMessages[1], message2);
                    Assert.Equal(arMessages[4], message3);

                    //Assert
                    _runnerServer
                        .Verify(x => x.GetAgentMessageAsync(
                            _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tokenSource.Token), Times.Exactly(arMessages.Length));

                    _runnerServer
                        .Verify(x => x.GetRunnerAuthUrlAsync(_settings.PoolId, _settings.AgentId), Times.Never);

                    _runnerServer
                        .Verify(x => x.ConnectAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<VssCredentials>()), Times.Once);

                    Assert.False(listener._useMigratedCredentials);
                    Assert.False(listener._needToCheckAuthorizationUrlUpdate);
                    Assert.Null(listener._authorizationUrlRollbackReattemptDelayBackgroundTask);
                    Assert.Null(listener._authorizationUrlMigrationBackgroundTask);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_SPSAUTHURL", null);
            }
        }
    }
}
