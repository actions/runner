using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Configuration;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{

    public sealed class MessageListenerL0
    {
        private AgentSettings _settings;
        private Mock<IConfigurationManager> _config; 
        private Mock<IWorkerManager> _workerManager;
        private Mock<ITaskServer> _taskServer;

        public MessageListenerL0()
        {
            _settings = new AgentSettings { AgentId=1, AgentName="myagent", PoolId=123, PoolName="default", ServerUrl="http://myserver", WorkFolder="_work" };
            _config = new Mock<IConfigurationManager>();
            _config.Setup(x => x.GetSettings()).Returns(_settings);
            _workerManager = new Mock<IWorkerManager>();
            _taskServer = new Mock<ITaskServer>();
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(nameof(MessageListenerL0), testName);
            tc.SetSingleton<IConfigurationManager>(_config.Object);
            tc.SetSingleton<IWorkerManager>(_workerManager.Object);
            tc.SetSingleton<ITaskServer>(_taskServer.Object);
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
                _taskServer
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
            }
        }
    }
}
