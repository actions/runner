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
using GitHub.Services.OAuth;
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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
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

                _credMgr
                    .Verify(x => x.LoadCredentials(true), Times.Exactly(brokerMessages.Length));

                _brokerServer
                    .Verify(x => x.UpdateConnectionIfNeeded(brokerMigrationMesage.BrokerBaseUrl, It.IsAny<VssCredentials>()), Times.Exactly(brokerMessages.Length));

                _brokerServer
                    .Verify(x => x.ForceRefreshConnection(It.IsAny<VssCredentials>()), Times.Never);
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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());

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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
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
                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Returns(Task.FromResult(expectedSession));

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());

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

                tc.EnableAuthMigration("L0Test");

                var traceFile = Path.GetTempFileName();
                File.Copy(tc.TraceFileName, traceFile, true);
                Assert.Contains("Auth migration changed", File.ReadAllText(traceFile));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetNextMessageWithBrokerMigration_AuthMigrationFallback()
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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
                _store.Setup(x => x.GetCredentials()).Returns(new CredentialData() { Scheme = Constants.Configuration.OAuthAccessToken });
                _store.Setup(x => x.GetMigratedCredentials()).Returns(default(CredentialData));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                tc.EnableAuthMigration("L0Test");

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

                var counter = 0;
                _brokerServer
                   .Setup(x => x.GetRunnerMessageAsync(
                       expectedSession.SessionId, TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                   .Returns(async (Guid sessionId, TaskAgentStatus status, string runnerVersion, string os, string architecture, bool disableUpdate, CancellationToken cancellationToken) =>
                   {
                       counter++;
                       await Task.Yield();
                       if (counter == 2)
                       {
                           throw new NotSupportedException("Something wrong.");
                       }

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
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(brokerMessages.Length + 1));

                _brokerServer
                    .Verify(x => x.GetRunnerMessageAsync(
                    expectedSession.SessionId, TaskAgentStatus.Online, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(brokerMessages.Length + 1));

                _credMgr
                    .Verify(x => x.LoadCredentials(true), Times.Exactly(brokerMessages.Length + 1));

                _brokerServer
                    .Verify(x => x.UpdateConnectionIfNeeded(brokerMigrationMesage.BrokerBaseUrl, It.IsAny<VssCredentials>()), Times.Exactly(brokerMessages.Length + 1));

                _brokerServer
                    .Verify(x => x.ForceRefreshConnection(It.IsAny<VssCredentials>()), Times.Once());

                Assert.False(tc.AllowAuthMigration);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetNextMessageWithBrokerMigration_EnableAuthMigration()
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

                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
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
                       if (!tc.AllowAuthMigration)
                       {
                           tc.EnableAuthMigration("L0Test");
                       }

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

                _credMgr
                    .Verify(x => x.LoadCredentials(true), Times.Exactly(brokerMessages.Length));

                _brokerServer
                    .Verify(x => x.UpdateConnectionIfNeeded(brokerMigrationMesage.BrokerBaseUrl, It.IsAny<VssCredentials>()), Times.Exactly(brokerMessages.Length));

                _brokerServer
                    .Verify(x => x.ForceRefreshConnection(It.IsAny<VssCredentials>()), Times.Once());

                Assert.True(tc.AllowAuthMigration);
            }
        }

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
                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(federatedCreds);

                var expectedSession = new TaskAgentSession();

                // Reproduces the real message shape from the issue: a token-expiry check against
                // the server's clock, surfaced by the service as an OAuth "invalid_client" error.
                var skewException = new VssOAuthTokenRequestException(
                    "The token expired on 08/24/2026 19:15:44. Current server time is 08/25/2026 01:44:14.")
                {
                    Error = "invalid_client",
                };

                _runnerServer
                    .SetupSequence(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Throws(skewException)
                    .Returns(Task.FromResult(expectedSession));

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert: the clock-skew invalid_client did not terminate the call - it fell
                // through to the existing clock-skew retry path, and the next attempt (clock now
                // caught up) succeeded.
                Assert.Equal(CreateSessionResult.Success, result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
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
                _credMgr.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(federatedCreds);

                // A genuine deleted-registration response: invalid_client with no clock-skew
                // sentinel anywhere in the message.
                var deletedException = new VssOAuthTokenRequestException("Client authentication failed.")
                {
                    Error = "invalid_client",
                };

                _runnerServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token))
                    .Throws(deletedException);

                // Act.
                MessageListener listener = new();
                listener.Initialize(tc);

                CreateSessionResult result = await listener.CreateSessionAsync(tokenSource.Token);
                trace.Info("result: {0}", result);

                // Assert: still terminates immediately as a deleted registration - exactly one
                // attempt, no retry.
                Assert.Equal(CreateSessionResult.Failure, result);
                _runnerServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tokenSource.Token), Times.Once());

                mockTerm.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("registration has been deleted"))), Times.Once);
            }
        }
    }
}
