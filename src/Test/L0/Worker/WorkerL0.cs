using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class WorkerL0
    {
        private Mock<IProcessChannel> _processChannel;
        private Mock<IJobRunner> _jobRunner;

        public WorkerL0()
        {
            _processChannel = new Mock<IProcessChannel>();
            _jobRunner = new Mock<IJobRunner>();
        }

        private JobRequestMessage CreateJobRequestMessage(string jobName)
        {
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = null;
            JobEnvironment environment = new JobEnvironment();
            environment.Variables[Constants.Variables.System.Culture] = "en-US";
            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            var jobRequest = new JobRequestMessage(plan, timeline, JobId, jobName, environment, tasks);
            return jobRequest;
        }

        private JobCancelMessage CreateJobCancelMessage(Guid jobId)
        {
            return new JobCancelMessage(jobId, TimeSpan.FromSeconds(0));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void DispatchRunNewJob()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var worker = new Microsoft.VisualStudio.Services.Agent.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.SetSingleton<IJobRunner>(_jobRunner.Object);
                worker.Initialize(hc);
                var jobMessage = CreateJobRequestMessage("job1");
                var arWorkerMessages = new WorkerMessage[]
                    {
                        new WorkerMessage
                        {
                            Body = JsonUtility.ToString(jobMessage),
                            MessageType = MessageType.NewJobRequest
                        }
                    };
                var workerMessages = new Queue<WorkerMessage>(arWorkerMessages);

                _processChannel
                    .Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                    {
                        // Return the job message.
                        if (workerMessages.Count > 0)
                        {
                            return workerMessages.Dequeue();
                        }

                        // Wait for the host cancellation token to expire.
                        await Task.Delay(-1, hc.CancellationToken);
                        return default(WorkerMessage);
                    });
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<JobRequestMessage>()))
                    .Returns(Task.CompletedTask);

                //Act
                await worker.RunAsync(pipeIn: "1", pipeOut: "2", hostTokenSource: hc.CancellationTokenSource);

                //Assert
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
                _jobRunner.Verify(x => x.RunAsync(
                    It.Is<JobRequestMessage>(y => JsonUtility.ToString(y) == arWorkerMessages[0].Body)));
                hc.CancellationTokenSource.Cancel();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void DispatchCancellation()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var worker = new Microsoft.VisualStudio.Services.Agent.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.SetSingleton<IJobRunner>(_jobRunner.Object);
                worker.Initialize(hc);
                var jobMessage = CreateJobRequestMessage("job1");
                var cancelMessage = CreateJobCancelMessage(jobMessage.JobId);
                var arWorkerMessages = new WorkerMessage[]
                    {
                        new WorkerMessage
                        {
                            Body = JsonUtility.ToString(jobMessage),
                            MessageType = MessageType.NewJobRequest
                        },
                        new WorkerMessage
                        {
                            Body = JsonUtility.ToString(cancelMessage),
                            MessageType = MessageType.CancelRequest
                        }

                    };
                var workerMessages = new Queue<WorkerMessage>(arWorkerMessages);

                _processChannel.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(workerMessages.Dequeue()));
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<JobRequestMessage>()))
                    .Returns(async () => await Task.Delay(-1, hc.CancellationToken));

                //Act
                await Assert.ThrowsAsync<TaskCanceledException>(
                    async () => await worker.RunAsync("1", "2", hc.CancellationTokenSource));

                //Assert
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
                _jobRunner.Verify(x => x.RunAsync(
                    It.Is<JobRequestMessage>(y => JsonUtility.ToString(y) == arWorkerMessages[0].Body)));
            }
        }
    }
}
