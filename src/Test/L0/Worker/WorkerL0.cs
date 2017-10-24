using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class WorkerL0
    {
        private Mock<IProcessChannel> _processChannel;
        private Mock<IJobRunner> _jobRunner;
        private Mock<IVstsAgentWebProxy> _proxy;
        private Mock<IAgentCertificateManager> _cert;

        public WorkerL0()
        {
            _processChannel = new Mock<IProcessChannel>();
            _jobRunner = new Mock<IJobRunner>();
            _proxy = new Mock<IVstsAgentWebProxy>();
            _cert = new Mock<IAgentCertificateManager>();
        }

        private JobRequestMessage CreateJobRequestMessage(string jobName)
        {
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = null;
            JobEnvironment environment = new JobEnvironment();
            var serviceEndpoint = new ServiceEndpoint();
            serviceEndpoint.Authorization = new EndpointAuthorization();
            serviceEndpoint.Authorization.Parameters.Add("nullValue", null);
            environment.Endpoints.Add(serviceEndpoint);
            environment.Variables[Constants.Variables.System.Culture] = "en-US";
            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            var jobRequest = new AgentJobRequestMessage(plan, timeline, JobId, jobName, jobName, environment, tasks);
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
            using (var tokenSource = new CancellationTokenSource())
            {
                var worker = new Microsoft.VisualStudio.Services.Agent.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IJobRunner>(_jobRunner.Object);
                hc.SetSingleton<IVstsAgentWebProxy>(_proxy.Object);
                hc.SetSingleton<IAgentCertificateManager>(_cert.Object);
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

                        // Wait for the text to run
                        await Task.Delay(-1, tokenSource.Token);
                        return default(WorkerMessage);
                    });
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<TaskResult>(TaskResult.Succeeded));

                //Act
                await worker.RunAsync(pipeIn: "1", pipeOut: "2");

                //Assert
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
                _jobRunner.Verify(x => x.RunAsync(
                    It.Is<AgentJobRequestMessage>(y => JsonUtility.ToString(y) == arWorkerMessages[0].Body), It.IsAny<CancellationToken>()));
                tokenSource.Cancel();
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
                hc.EnqueueInstance<IJobRunner>(_jobRunner.Object);
                hc.SetSingleton<IVstsAgentWebProxy>(_proxy.Object);
                hc.SetSingleton<IAgentCertificateManager>(_cert.Object);
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
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(
                    async (JobRequestMessage jm, CancellationToken ct) =>
                    {
                        await Task.Delay(-1, ct);
                        return TaskResult.Canceled;
                    });

                //Act
                await Assert.ThrowsAsync<TaskCanceledException>(
                    async () => await worker.RunAsync("1", "2"));

                //Assert
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
                _jobRunner.Verify(x => x.RunAsync(
                    It.Is<AgentJobRequestMessage>(y => JsonUtility.ToString(y) == arWorkerMessages[0].Body), It.IsAny<CancellationToken>()));
            }
        }
    }
}
