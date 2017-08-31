using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener
{
    public sealed class JobDispatcherL0
    {
        private Mock<IProcessChannel> _processChannel;
        private Mock<IProcessInvoker> _processInvoker;
        private Mock<IAgentServer> _agentServer;
        private Mock<IConfigurationStore> _configurationStore;

        public JobDispatcherL0()
        {
            _processChannel = new Mock<IProcessChannel>();
            _processInvoker = new Mock<IProcessInvoker>();
            _agentServer = new Mock<IAgentServer>();
            _configurationStore = new Mock<IConfigurationStore>();
        }

        private AgentJobRequestMessage CreateJobRequestMessage()
        {
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = null;
            JobEnvironment environment = new JobEnvironment();
            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            var jobRequest = new AgentJobRequestMessage(plan, timeline, JobId, "someJob", "someJob", environment, tasks);
            return jobRequest as AgentJobRequestMessage;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void DispatchesJobRequest()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var jobDispatcher = new JobDispatcher();
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IAgentServer>(_agentServer.Object);

                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

                _configurationStore.Setup(x => x.GetSettings()).Returns(new AgentSettings() { PoolId = 1 });
                jobDispatcher.Initialize(hc);

                var ts = new CancellationTokenSource();
                AgentJobRequestMessage message = CreateJobRequestMessage();
                string strMessage = JsonUtility.ToString(message);

                _processInvoker.Setup(x => x.ExecuteAsync(It.IsAny<String>(), It.IsAny<String>(), "spawnclient 1 2", null, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<int>(56));

                _processChannel.Setup(x => x.StartServer(It.IsAny<StartProcessDelegate>()))
                    .Callback((StartProcessDelegate startDel) => { startDel("1", "2"); });
                _processChannel.Setup(x => x.SendAsync(MessageType.NewJobRequest, It.Is<string>(s => s.Equals(strMessage)), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var request = new TaskAgentJobRequest();
                PropertyInfo sessionIdProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                _agentServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(request));

                _agentServer.Setup(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TaskResult>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(new TaskAgentJobRequest()));


                //Actt
                jobDispatcher.Run(message);

                //Assert
                await jobDispatcher.WaitAsync(CancellationToken.None);
            }
        }

        // TODO: Fix after JobDispatcher changes.
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Agent")]
        // public async void DispatchesCancellationRequest()
        // {
        //     //Arrange
        //     using (var hc = new TestHostContext(this))
        //     using (var jobDispatcher = new JobDispatcher())
        //     {
        //         hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
        //         hc.SetSingleton<IAgentServer>(_agentServer.Object);

        //         hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
        //         hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
        //         jobDispatcher.Initialize(hc);
        //         var ts = new CancellationTokenSource();
        //         CancellationToken token = ts.Token;
        //         JobRequestMessage message = CreateJobRequestMessage();
        //         string strMessage = JsonUtility.ToString(message);

        //         _processInvoker.Setup(x => x.ExecuteAsync(It.IsAny<String>(), It.IsAny<String>(), "spawnclient 1 2", null, It.IsAny<CancellationToken>()))
        //             .Returns(async(String workingFolder, String filename, String arguments, IDictionary<String, String> environment, CancellationToken cancellationToken) =>
        //             {
        //                 await Task.Delay(5000);
        //                 return 1;
        //             });

        //         _processChannel.Setup(x => x.StartServer(It.IsAny<StartProcessDelegate>()))
        //             .Callback((StartProcessDelegate startDel) => { startDel("1", "2"); });
        //         _processChannel.Setup(x => x.SendAsync(MessageType.NewJobRequest, It.Is<string>(s => s.Equals(strMessage)), It.IsAny<CancellationToken>()))
        //             .Returns(Task.CompletedTask);
        //         _processChannel.Setup(x => x.SendAsync(MessageType.CancelRequest, It.IsAny<String>(), It.IsAny<CancellationToken>()))
        //             .Returns(Task.CompletedTask);

        //         _configurationStore.Setup(x => x.GetSettings()).Returns(new AgentSettings() { PoolId = 1 });

        //         var request = new TaskAgentJobRequest();
        //         PropertyInfo sessionIdProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        //         Assert.NotNull(sessionIdProperty);
        //         sessionIdProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

        //         _agentServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(request));

        //         _agentServer.Setup(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TaskResult>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(new TaskAgentJobRequest()));


        //         //Act
        //         Task<int> runAsyncTask = jobDispatcher.RunAsync(message, ts.Token);
        //         ts.Cancel();
        //         await runAsyncTask;

        //         //Assert
        //         // Verify the cancellation message was sent
        //         _processChannel.Verify(x => x.SendAsync(MessageType.CancelRequest, It.IsAny<String>(), It.IsAny<CancellationToken>()),
        //             "Cancelation message not sent");
        //     }
        // }
    }
}
