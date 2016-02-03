using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.VisualStudio.Services.Agent.CLI;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class MessageListenerL0
    {
        public MessageListenerL0()
        {
            this.poolId = 123;
            this.context = new MockHostContext();
            this.dispatcher = new MockMessageDispatcher();
            this.taskServer = new MockTaskServer();
            this.listener = new MessageListener(
                poolId: this.poolId,
                context: this.context,
                dispatcher: this.dispatcher,
                taskServer: this.taskServer);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void CreatesSession()
        {
            // Arrange.
            String expectedSessionId = Guid.NewGuid().ToString();
            Int32 actualPoolId = 0;
            this.taskServer._CreateAgentSession = (poolId) =>
            {
                actualPoolId = poolId; // This is a poor man's way of enabling assert-was-called.
                return Task.FromResult(expectedSessionId);
            };

            // Act.
            Boolean result = await this.listener.CreateSessionAsync();

            // Assert.
            Assert.True(result);
            Assert.Equal(expectedSessionId, this.listener.SessionId);
            Assert.Equal(this.poolId, actualPoolId);
        }

        private Int32 poolId;
        private MockHostContext context;
        private MockMessageDispatcher dispatcher;
        private MockTaskServer taskServer;
        private MessageListener listener;
    }
}
