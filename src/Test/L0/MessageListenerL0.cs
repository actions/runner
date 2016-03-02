using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Configuration;
using Moq;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{

    public sealed class MessageListenerL0
    {
        private AgentSettings _settings;
        private Mock<IConfigurationManager> _config;
        private Mock<IAgentServer> _agentServer;

        public MessageListenerL0()
        {
            _settings = new AgentSettings { AgentId = 1, AgentName = "myagent", PoolId = 123, PoolName = "default", ServerUrl = "http://myserver", WorkFolder = "_work" };
            _config = new Mock<IConfigurationManager>();
            _config.Setup(x => x.LoadSettings()).Returns(_settings);
            _agentServer = new Mock<IAgentServer>();
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(nameof(MessageListenerL0), testName);
            tc.SetSingleton<IConfigurationManager>(_config.Object);
            tc.SetSingleton<IAgentServer>(_agentServer.Object);
            return tc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void CreatesSession()
        {
            using (TestHostContext tc = CreateTestContext())
            {
                TraceSource trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                _agentServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tc.CancellationToken))
                    .Returns(Task.FromResult(expectedSession));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync();
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                Assert.Equal(expectedSession, listener.Session);
                _agentServer
                    .Verify(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tc.CancellationToken), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void DeleteSession()
        {
            using (TestHostContext tc = CreateTestContext())
            {
                TraceSource trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                PropertyInfo sessionIdProperty = expectedSession.GetType().GetProperty("SessionId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(expectedSession, Guid.NewGuid());

                _agentServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tc.CancellationToken))
                    .Returns(Task.FromResult(expectedSession));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync();
                Assert.True(result);
                Assert.Equal(expectedSession, listener.Session);

                _agentServer
                    .Setup(x => x.DeleteAgentSessionAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                await listener.DeleteSessionAsync();
                
                //Assert
                _agentServer
                    .Verify(x => x.DeleteAgentSessionAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void GetNextMessage()
        {
            using (TestHostContext tc = CreateTestContext())
            {
                TraceSource trace = tc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                PropertyInfo sessionIdProperty = expectedSession.GetType().GetProperty("SessionId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(expectedSession, Guid.NewGuid());

                _agentServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        _settings.PoolId,
                        It.Is<TaskAgentSession>(y => y != null),
                        tc.CancellationToken))
                    .Returns(Task.FromResult(expectedSession));

                // Act.
                MessageListener listener = new MessageListener();
                listener.Initialize(tc);

                bool result = await listener.CreateSessionAsync();
                Assert.True(result);
                Assert.Equal(expectedSession, listener.Session);

                var arMessages = new TaskAgentMessage[]
                {
                        new TaskAgentMessage
                        {
                            Body = "somebody1",
                            MessageId = 4234,
                            MessageType = JobRequestMessage.MessageType
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
                            MessageType = JobRequestMessage.MessageType
                        }
                };
                var messages = new Queue<TaskAgentMessage>(arMessages);

                _agentServer
                    .Setup(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tc.CancellationToken))
                    //.Returns(Task.FromResult<TaskAgentMessage>(messages.Dequeue()));
                    .Returns(async (Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken) =>
                    {
                        await Task.Yield();
                        return messages.Dequeue();
                    });
                TaskAgentMessage message1 = await listener.GetNextMessageAsync();
                TaskAgentMessage message2 = await listener.GetNextMessageAsync();
                TaskAgentMessage message3 = await listener.GetNextMessageAsync();
                Assert.Equal(arMessages[0], message1);
                Assert.Equal(arMessages[1], message2);
                Assert.Equal(arMessages[4], message3);

                //Assert
                _agentServer
                    .Verify(x => x.GetAgentMessageAsync(
                        _settings.PoolId, expectedSession.SessionId, It.IsAny<long?>(), tc.CancellationToken), Times.Exactly(arMessages.Length));
            }
        }
    }
}
