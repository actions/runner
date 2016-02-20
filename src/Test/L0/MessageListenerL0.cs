using System;
using System.Diagnostics;
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
            this.workerManager = new Mock<IWorkerManager>();
            this.taskServer = new Mock<ITaskServer>();
            this.listener = new MessageListener();
        }

        private TestHostContext CreateTestContext(string testName)
        {
            TestHostContext thc = new TestHostContext("MessageListenerL0", "CreatesSession");
            thc.RegisterService<IAgentSettings>(this.agentSettings.Object);
            thc.RegisterService<IWorkerManager>(this.workerManager.Object);
            thc.RegisterService<ITaskServer>(this.taskServer.Object);            
            return thc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void CreatesSession()
        {
            using (TestHostContext thc = CreateTestContext("CreatesSession"))
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
        private Mock<IWorkerManager> workerManager;
        private Mock<ITaskServer> taskServer;
        private MessageListener listener;
    }
}
