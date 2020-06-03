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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());
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

                _credMgr.Setup(x => x.LoadCredentials()).Returns(new VssCredentials());

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
            }
        }
    }
}
