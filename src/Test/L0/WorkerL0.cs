using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
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
            var message = new JobCancelMessage(jobId, TimeSpan.FromSeconds(0));
            return message;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void DispatchRunNewJob()
        {
            //Arrange
            using (var hc = new TestHostContext(nameof(WorkerL0)))
            {
                var worker = new Microsoft.VisualStudio.Services.Agent.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.SetSingleton<IJobRunner>(_jobRunner.Object);
                worker.Initialize(hc);
                var jobMessage = CreateJobRequestMessage("job1");
                var workerMessage = new WorkerMessage(MessageType.NewJobRequest, JsonUtility.ToString(jobMessage));
                _processChannel.Setup(x => x.StartClient("1", "2"));
                _processChannel.Setup(x => x.ReceiveAsync(hc.CancellationToken))
                    .Returns((CancellationToken cancellationToken) => { return Task.FromResult(workerMessage); });
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<JobRequestMessage>()))
                    .Returns((JobRequestMessage jm) =>
                    {
                        if (jm.JobId.Equals(jobMessage.JobId) && jm.JobName.Equals(jobMessage.JobName))
                        {
                            return Task.FromResult(23);
                        }
                        else
                        {
                            return Task.FromResult(22);
                        }
                    });

                //Act
                var cs = new CancellationTokenSource();
                int exitCode = await worker.RunAsync("1", "2", cs);

                //Assert
                Assert.Equal(23, exitCode);
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void DispatchCancellation()
        {
            //Arrange
            using (var hc = new TestHostContext(nameof(WorkerL0)))
            {
                var worker = new Microsoft.VisualStudio.Services.Agent.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.SetSingleton<IJobRunner>(_jobRunner.Object);
                worker.Initialize(hc);
                var jobMessage = CreateJobRequestMessage("job1");
                var cancelMessage = CreateJobCancelMessage(jobMessage.JobId);
                var workerMessage = new WorkerMessage(MessageType.NewJobRequest, JsonUtility.ToString(jobMessage));
                _processChannel.Setup(x => x.StartClient("1", "2"));

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

                _processChannel.Setup(x => x.ReceiveAsync(hc.CancellationToken))
                    .Returns(async(CancellationToken cancellationToken) => 
                    {
                        if (0 == workerMessages.Count)
                        {
                            await Task.Delay(2000, cancellationToken);
                        }
                        return workerMessages.Dequeue();
                    });
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<JobRequestMessage>()))
                    .Returns(async(JobRequestMessage jm) =>
                    {
                        if (jm.JobId.Equals(jobMessage.JobId) && jm.JobName.Equals(jobMessage.JobName))
                        {
                            await Task.Delay(2000, hc.CancellationToken);
                            return 23;
                        }
                        else
                        {
                            return 22;
                        }
                    });

                //Act
                Task<int> workerTask = worker.RunAsync("1", "2", hc.CancellationTokenSource);
                Task[] taskToWait = { workerTask, Task.Delay(2000) };
                //wait for the Worker to exit
                await Task.WhenAny(taskToWait);

                //Assert
                Assert.True(workerTask.IsCompleted);
                Assert.True(workerTask.IsCanceled);
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
            }
        }      
    }
}
