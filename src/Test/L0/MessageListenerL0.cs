using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.CLI;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class MessageListenerL0
    {
        public MessageListenerL0()
        {
            this.agentSettings = new MockAgentSettings() { PoolId = 123 };
            this.dispatcher = new MockMessageDispatcher();
            this.taskServer = new MockTaskServer();
            this.context = new MockHostContext();
            this.context.RegisterService<IAgentSettings, MockAgentSettings>(this.agentSettings);
            this.context.RegisterService<IMessageDispatcher, MockMessageDispatcher>(this.dispatcher);
            this.context.RegisterService<ITaskServer, MockTaskServer>(this.taskServer);
            this.listener = new MessageListener();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void CreatesSession()
        {
            // Arrange.
            var expectedSession = new TaskAgentSession();
            Int32 actualPoolId = 0;
            TaskAgentSession actualSession = null;
            CancellationToken actualCancellationToken = default(CancellationToken);
            this.taskServer._CreateAgentSessionAsync = (poolId, session, cancellationToken) =>
            {
                actualPoolId = poolId; // This is a poor man's way of enabling assert-was-called.
                actualSession = session;
                actualCancellationToken = cancellationToken;
                return Task.FromResult(expectedSession);
            };

            // Act.
            Boolean result = await this.listener.CreateSessionAsync(this.context);

            // Assert.
            Assert.True(result);
            Assert.Equal(expectedSession, this.listener.Session);
            Assert.Equal(this.agentSettings.PoolId, actualPoolId);
            Assert.Equal(this.context.CancellationToken, actualCancellationToken);
        }

        private MockAgentSettings agentSettings;
        private MockHostContext context;
        private MockMessageDispatcher dispatcher;
        private MockTaskServer taskServer;
        private MessageListener listener;
    }
}
