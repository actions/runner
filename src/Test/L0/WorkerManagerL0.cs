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
    public sealed class WorkerManagerL0
    {
        private Mock<IJobDispatcher> _jobDispatcher;

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

        private JobCancelMessage createJobCancelMessage(Guid jobId)
        {
            var message = new JobCancelMessage(jobId, TimeSpan.FromSeconds(0));
            return message;
        }

        public WorkerManagerL0()
        {
            _jobDispatcher = new Mock<IJobDispatcher>();
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void TestRun()
        {
            using (var hc = new TestHostContext(nameof(WorkerManagerL0)))
            using (var workerManager = new WorkerManager())
            {
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);
                workerManager.Initialize(hc);
                JobRequestMessage jobMessage = createJobRequestMessage();
                _jobDispatcher.Setup(x => x.RunAsync(jobMessage, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<int>(21));
                await workerManager.Run(jobMessage);
                _jobDispatcher.Verify(x => x.RunAsync(jobMessage, It.IsAny<CancellationToken>()),
                    "IJobDispatcher.RunAsync not invoked");
            }
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void TestCancel()
        {
            //Arrange
            using (var hc = new TestHostContext(nameof(WorkerManagerL0)))
            using (var workerManager = new WorkerManager())
            {
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);
                workerManager.Initialize(hc);                
                JobRequestMessage jobMessage = createJobRequestMessage();
                JobCancelMessage cancelMessage = createJobCancelMessage(jobMessage.JobId);
                bool started = false;
                Task jobTask = null;
                _jobDispatcher.Setup(x => x.RunAsync(jobMessage, It.IsAny<CancellationToken>()))
                    .Returns(async(JobRequestMessage message, CancellationToken token) => 
                    {
                        jobTask = Task.Delay(5000, token);
                        started = true;
                        await jobTask;
                        return 0;
                    });
                await workerManager.Run(jobMessage);                
                int i = 20;
                while (i > 0 && (!started))
                {
                    await Task.Delay(10);
                    i--;
                }
                Assert.True(started);
                if (started)
                {
                    //Act
                    //send cancel message
                    await workerManager.Cancel(cancelMessage);
                    
                    //Assert
                    //wait up to 2 sec for cancellation to be processed
                    Task[] taskToWait = { jobTask, Task.Delay(2000) };
                    await Task.WhenAny(taskToWait);
                    _jobDispatcher.Verify(x => x.RunAsync(jobMessage, It.IsAny<CancellationToken>()),
                        "IJobDispatcher.RunAsync not invoked");
                    Assert.True(jobTask.IsCompleted);
                    Assert.True(jobTask.IsCanceled);
                }
            }
        }
    }
}
