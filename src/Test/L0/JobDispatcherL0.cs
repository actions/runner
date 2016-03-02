using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class JobDispatcherL0
    {
        private Mock<IProcessChannel> _processChannel;
        private Mock<IProcessInvoker> _processInvoker;

        public JobDispatcherL0()
        {
            _processChannel = new Mock<IProcessChannel>();
            _processInvoker = new Mock<IProcessInvoker>();
        }

        private JobRequestMessage createJobRequestMessage()
        {
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = null;
            JobEnvironment environment = new JobEnvironment();
            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            var jobRequest = new JobRequestMessage(plan, timeline, JobId, "someJob", environment, tasks);
            return jobRequest;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void ProcessJobMessage()
        {
            //Arrange
            using (var hc = new TestHostContext(nameof(JobDispatcherL0)))
            using (var jobDispatcher = new JobDispatcher())
            {
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                jobDispatcher.Initialize(hc);
                var ts = new CancellationTokenSource();
                CancellationToken token = ts.Token;
                JobRequestMessage message = createJobRequestMessage();
                string strMessage = JsonUtility.ToString(message);

                _processInvoker.Setup(x => x.ExecuteAsync(It.IsAny<String>(), It.IsAny<String>(), "spawnclient 1 2", null, token))
                    .Returns(Task.FromResult<int>(56));

                _processChannel.Setup(x => x.StartServer(It.IsAny<ProcessStartDelegate>()))
                    .Callback((ProcessStartDelegate startDel) => { startDel("1","2"); });
                _processChannel.Setup(x => x.SendAsync(MessageType.NewJobRequest, It.Is<string>(s => s.Equals(strMessage)), token))
                    .Returns(Task.CompletedTask);

                //Actt
                int exitCode = await jobDispatcher.RunAsync(message, ts.Token);
                
                //Assert
                Assert.True(exitCode == 56);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void CancelJobMessage()
        {
            //Arrange
            using (var hc = new TestHostContext(nameof(JobDispatcherL0)))
            using (var jobDispatcher = new JobDispatcher())
            {
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                jobDispatcher.Initialize(hc);
                var ts = new CancellationTokenSource();
                CancellationToken token = ts.Token;
                JobRequestMessage message = createJobRequestMessage();
                string strMessage = JsonUtility.ToString(message);

                _processInvoker.Setup(x => x.ExecuteAsync(It.IsAny<String>(), It.IsAny<String>(), "spawnclient 1 2", null, token))
                    .Returns(async(String workingFolder, String filename, String arguments, IDictionary<String, String> environmentVariables, 
                    CancellationToken cancellationToken) => { await Task.Delay(5000, cancellationToken);  return 56; });

                _processChannel.Setup(x => x.StartServer(It.IsAny<ProcessStartDelegate>()))
                    .Callback((ProcessStartDelegate startDel) => { startDel("1", "2"); });
                _processChannel.Setup(x => x.SendAsync(MessageType.NewJobRequest, It.Is<string>(s => s.Equals(strMessage)), token))
                    .Returns(Task.CompletedTask);
                _processChannel.Setup(x => x.SendAsync(MessageType.CancelRequest, It.IsAny<String>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                //Act
                Task<int> runAsyncTask = jobDispatcher.RunAsync(message, ts.Token);
                ts.Cancel();
                await runAsyncTask;
                
                //Assert
                // Verify the cancellation message was sent
                _processChannel.Verify(x => x.SendAsync(MessageType.CancelRequest, It.IsAny<String>(), It.IsAny<CancellationToken>()),
                    "Cancelation message not sent");
            }
        }
    }
}
