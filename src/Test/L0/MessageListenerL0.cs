using System;
using System.Threading;
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
            this.context.RegisterService<IMessageDispatcher, MockMessageDispatcher>(this.dispatcher = new MockMessageDispatcher());
            this.context.RegisterService<ITaskServer, MockTaskServer>(this.taskServer = new MockTaskServer());
            this.listener = new MessageListener();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void CreatesSession()
        {
            // Arrange.
            String expectedSessionId = Guid.NewGuid().ToString();
            Int32 actualPoolId = 0;
            CancellationToken actualCancellationToken = default(CancellationToken);
            this.taskServer._CreateAgentSessionAsync = (poolId, cancellationToken) =>
            {
                actualPoolId = poolId; // This is a poor man's way of enabling assert-was-called.
                actualCancellationToken = cancellationToken;
                return Task.FromResult(expectedSessionId);
            };

            // Act.
            Boolean result = await this.listener.CreateSessionAsync(this.context, this.poolId);

            // Assert.
            Assert.True(result);
            Assert.Equal(expectedSessionId, this.listener.SessionId);
            Assert.Equal(this.poolId, actualPoolId);
            Assert.Equal(this.context.CancellationToken, actualCancellationToken);
            Assert.Equal(this.poolId, this.listener.PoolId);
        }

        private Int32 poolId;
        private MockHostContext context;
        private MockMessageDispatcher dispatcher;
        private MockTaskServer taskServer;
        private MessageListener listener;
    }
}
