using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class MessageListenerL0
    {
        private RunnerSettings _settings;
        private Mock<IConfigurationManager> _config;
        private Mock<IRunnerServer> _runnerServer;
        private Mock<ICredentialManager> _credMgr;
        private Mock<IConfigurationStore> _store;

        private Mock<IBrokerServer> _brokerServer;

        public MessageListenerL0()
        {
            _settings = new RunnerSettings { AgentId = 1, AgentName = "myagent", PoolId = 123, PoolName = "default", ServerUrl = "http://myserver", WorkFolder = "_work" };
            _config = new Mock<IConfigurationManager>();
            _config.Setup(x => x.LoadSettings()).Returns(_settings);
            _runnerServer = new Mock<IRunnerServer>();
            _credMgr = new Mock<ICredentialManager>();
            _store = new Mock<IConfigurationStore>();
            _brokerServer = new Mock<IBrokerServer>();
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new(this, testName);
            tc.SetSingleton<IConfigurationManager>(_config.Object);
            tc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            tc.SetSingleton<ICredentialManager>(_credMgr.Object);
            tc.SetSingleton<IConfigurationStore>(_store.Object);
            tc.SetSingleton<IBrokerServer>(_brokerServer.Object);
            return tc;
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
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.Equal(CreateSessionResult.Success, result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());
                _brokerServer
                   .Verify(x => x.CreateSessionAsync(
                       It.Is<TaskAgentSession>(y => y != null),
                       tokenSource.Token), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task CreatesSessionWithBrokerMigration()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession()
                {
                    OwnerName = "legacy",
                    BrokerMigrationMessage = new BrokerMigrationMessage(new Uri("https://broker.actions.github.com"))
                };

                var expectedBrokerSession = new TaskAgentSession()
                {
                    OwnerName = "broker"
                };

                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedBrokerSession));

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.Equal(CreateSessionResult.Success, result);

                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                _brokerServer
                    .Verify(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task DeleteSession()
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                Assert.Equal(CreateSessionResult.Success, result);

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
        public async Task DeleteSessionWithBrokerMigration()
        {
            using (TestHostContext tc = CreateTestContext())
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession()
                {
                    OwnerName = "legacy",
                    BrokerMigrationMessage = new BrokerMigrationMessage(new Uri("https://broker.actions.github.com"))
                };

                var expectedBrokerSession = new TaskAgentSession()
                {
                    SessionId = Guid.NewGuid(),
                    OwnerName = "broker"
                };

                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _brokerServer
                    .Setup(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedBrokerSession));

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                Assert.Equal(CreateSessionResult.Success, result);

                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                _brokerServer
                    .Verify(x => x.CreateSessionAsync(
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                _brokerServer
                    .Setup(x => x.DeleteSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                // Act.
                await listener.DeleteSessionAsync();


                //Assert
                _runnerServer
                    .Verify(x => x.DeleteAgentSessionAsync(
                        _settings.PoolId, expectedBrokerSession.SessionId, It.IsAny<CancellationToken>()), Times.Once());
                _brokerServer
                    .Verify(x => x.DeleteSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                Assert.Equal(CreateSessionResult.Success, result);

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
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, TaskAgentStatus status, string runnerVersion, string os, string architecture, bool disableUpdate, CancellationToken cancellationToken) =>
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
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(arMessages.Length));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetNextMessageWithBrokerMigration()
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                Assert.Equal(CreateSessionResult.Success, result);

                var brokerMigrationMesage = new BrokerMigrationMessage(new Uri("https://actions.broker.com"));

                var arMessages = new TaskAgentMessage[]
                {
                        new TaskAgentMessage
                        {
                            Body = JsonUtility.ToString(brokerMigrationMesage),
                            MessageType = BrokerMigrationMessage.MessageType
                        },
                };

                var brokerMessages = new TaskAgentMessage[]
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
                var brokerMessageQueue = new Queue<TaskAgentMessage>(brokerMessages);

                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, TaskAgentStatus status, string runnerVersion, string os, string architecture, bool disableUpdate, CancellationToken cancellationToken) =>
                    {
                        await Task.Yield();
                        return arMessages[0]; // always send migration message
                    });

                _brokerServer
                   .Setup(x => x.GetRunnerMessageAsync(
                       expectedSession.SessionId, TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                   .Returns(async (Guid sessionId, TaskAgentStatus status, string runnerVersion, string os, string architecture, bool disableUpdate, CancellationToken cancellationToken) =>
                   {
                       await Task.Yield();
                       return brokerMessageQueue.Dequeue();
                   });

                TaskAgentMessage message1 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message2 = await listener.GetNextMessageAsync(tokenSource.Token);
                TaskAgentMessage message3 = await listener.GetNextMessageAsync(tokenSource.Token);
                Assert.Equal(brokerMessages[0], message1);
                Assert.Equal(brokerMessages[1], message2);
                Assert.Equal(brokerMessages[4], message3);

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(brokerMessages.Length));

                _brokerServer
                    .Verify(x => x.GetRunnerMessageAsync(
                    expectedSession.SessionId, TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(brokerMessages.Length));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task CreateSessionWithOriginalCredential()
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());

                var originalCred = new CredentialData() { Scheme = Constants.Configuration.OAuth };
                originalCred.Data["authorizationUrl"] = "https://s.server";
                originalCred.Data["clientId"] = "d842fd7b-61b0-4a80-96b4-f2797c353897";

                _store.Setup(x => x.GetCredentials()).Returns(originalCred);
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.Equal(CreateSessionResult.Success, result);
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
        public async Task SkipDeleteSession_WhenGetNextMessageGetTaskAgentAccessTokenExpiredException()
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                Assert.Equal(CreateSessionResult.Success, result);

                _runnerServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Throws(new TaskAgentAccessTokenExpiredException("test"));
                try
                {
                    await listener.GetNextMessageAsync(tokenSource.Token);
                }
                catch (TaskAgentAccessTokenExpiredException)
                {
                    //expected
                }
                finally
                {
                    await listener.DeleteSessionAsync();
                }

                //Assert
                _runnerServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

                _runnerServer
                    .Verify(x => x.DeleteAgentSessionAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<CancellationToken>()), Times.Never);
            }
        }
    }
}
