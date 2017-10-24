using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class ExecutionContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitializeJob_LogsWarningsFromVariables()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: Create a job request message.
                TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
                TimelineReference timeline = new TimelineReference();
                JobEnvironment environment = new JobEnvironment();
                environment.SystemConnection = new ServiceEndpoint();
                environment.Variables["v1"] = "v1-$(v2)";
                environment.Variables["v2"] = "v2-$(v1)";
                List<TaskInstance> tasks = new List<TaskInstance>();
                Guid JobId = Guid.NewGuid();
                string jobName = "some job name";
                var jobRequest = new AgentJobRequestMessage(plan, timeline, JobId, jobName, jobName, environment, tasks);

                // Arrange: Setup the paging logger.
                var pagingLogger = new Mock<IPagingLogger>();
                hc.EnqueueInstance(pagingLogger.Object);

                var ec = new Agent.Worker.ExecutionContext();
                ec.Initialize(hc);

                // Act.
                ec.InitializeJob(jobRequest, CancellationToken.None);

                // Assert.
                pagingLogger.Verify(x => x.Write(It.Is<string>(y => y.IndexOf("##[warning]") >= 0)), Times.Exactly(2));
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);

            // Arrange: Setup the configation store.
            var configurationStore = new Mock<IConfigurationStore>();
            configurationStore.Setup(x => x.GetSettings()).Returns(new AgentSettings());
            hc.SetSingleton(configurationStore.Object);

            // Arrange: Setup the secret masker.
            var secretMasker = new Mock<ISecretMasker>();
            secretMasker.Setup(x => x.MaskSecrets(It.IsAny<string>()))
                .Returns((string x) => x);
            hc.SetSingleton(secretMasker.Object);

            // Arrange: Setup the proxy configation.
            var proxy = new Mock<IVstsAgentWebProxy>();
            hc.SetSingleton(proxy.Object);

            // Arrange: Setup the cert configation.
            var cert = new Mock<IAgentCertificateManager>();
            hc.SetSingleton(cert.Object);

            // Arrange: Create the execution context.
            hc.SetSingleton(new Mock<IJobServerQueue>().Object);
            return hc;
        }

        private JobRequestMessage CreateJobRequestMessage()
        {
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = new TimelineReference();
            JobEnvironment environment = new JobEnvironment();
            environment.SystemConnection = new ServiceEndpoint();
            environment.Variables["v1"] = "v1";
            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            string jobName = "some job name";
            return new AgentJobRequestMessage(plan, timeline, JobId, jobName, jobName, environment, tasks);
        }
    }
}
