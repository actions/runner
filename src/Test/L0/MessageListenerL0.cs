using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent;
using Moq;
using Xunit;

using Microsoft.VisualStudio.Services.Agent.Configuration;

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
            this.listener = new MessageListener();
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext thc = new TestHostContext(nameof(MessageListenerL0), testName);
            thc.RegisterService<IAgentSettings>(this.agentSettings.Object);
            thc.RegisterService<IMessageDispatcher>(this.dispatcher.Object);
            thc.RegisterService<ITaskServer>(this.taskServer.Object);
            return thc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void CreatesSession()
        {
            using (TestHostContext thc = CreateTestContext())
            {
                TraceSource trace = thc.GetTrace();

                // Arrange.
                var expectedSession = new TaskAgentSession();
                this.taskServer
                    .Setup(x => x.CreateAgentSessionAsync(
                        It.Is<Int32>(y => y == this.agentSettings.Object.PoolId),
                        It.Is<TaskAgentSession>(y => y != null),
                        It.Is<CancellationToken>(y => y == thc.CancellationToken)))
                    .Returns(Task.FromResult(expectedSession));

                // Act.
                Boolean result = await this.listener.CreateSessionAsync(thc);
                trace.Info("result: {0}", result);

                // Assert.
                Assert.True(result);
                Assert.Equal(expectedSession, this.listener.Session);
            }
        }
 
        private Mock<IAgentSettings> agentSettings;
        private Mock<IMessageDispatcher> dispatcher;
        private Mock<ITaskServer> taskServer;
        private MessageListener listener;
    }
}
