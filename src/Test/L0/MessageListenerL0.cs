using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class MessageListenerL0
    {
        public MessageListenerL0()
        {
            this.agentSettings = new Mock<IAgentSettings>();
            this.agentSettings.Setup(x => x.PoolId).Returns(123);
            this.dispatcher = new Mock<IMessageDispatcher>();
            this.taskServer = new Mock<ITaskServer>();
            this.context = new TestHostContext();
            this.context.RegisterService<IAgentSettings>(this.agentSettings.Object);
            this.context.RegisterService<IMessageDispatcher>(this.dispatcher.Object);
            this.context.RegisterService<ITaskServer>(this.taskServer.Object);
            this.listener = new MessageListener();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void CreatesSession()
        {
            // Arrange.
            var expectedSession = new TaskAgentSession();
            this.taskServer
                .Setup(x => x.CreateAgentSessionAsync(
                    It.Is<Int32>(y => y == this.agentSettings.Object.PoolId),
                    It.Is<TaskAgentSession>(y => y != null),
                    It.Is<CancellationToken>(y => y == this.context.CancellationToken)))
                .Returns(Task.FromResult(expectedSession));

            // Act.
            Boolean result = await this.listener.CreateSessionAsync(this.context);

            // Assert.
            Assert.True(result);
            Assert.Equal(expectedSession, this.listener.Session);
        }

        private Mock<IAgentSettings> agentSettings;
        private Mock<IMessageDispatcher> dispatcher;
        private Mock<ITaskServer> taskServer;
        private TestHostContext context;
        private MessageListener listener;
    }
}
