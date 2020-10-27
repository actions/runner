using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class JobExtensionL0
    {
        private IExecutionContext _jobEc;
        private Pipelines.AgentJobRequestMessage _message;

        private Mock<IPipelineDirectoryManager> _directoryManager;
        private Mock<IActionManager> _actionManager;
        private Mock<IJobServerQueue> _jobServerQueue;
        private Mock<IConfigurationStore> _config;
        private Mock<IPagingLogger> _logger;
        private Mock<IContainerOperationProvider> _containerProvider;
        private Mock<IDiagnosticLogManager> _diagnosticLogManager;

        private CancellationTokenSource _tokenSource;
        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _jobEc = new Runner.Worker.ExecutionContext();
            _actionManager = new Mock<IActionManager>();
            _jobServerQueue = new Mock<IJobServerQueue>();
            _config = new Mock<IConfigurationStore>();
            _logger = new Mock<IPagingLogger>();
            _containerProvider = new Mock<IContainerOperationProvider>();
            _diagnosticLogManager = new Mock<IDiagnosticLogManager>();
            _directoryManager = new Mock<IPipelineDirectoryManager>();
            _directoryManager.Setup(x => x.PrepareDirectory(It.IsAny<IExecutionContext>(), It.IsAny<Pipelines.WorkspaceOptions>()))
                             .Returns(new TrackingConfig() { PipelineDirectory = "runner", WorkspaceDirectory = "runner/runner" });

            IActionRunner step1 = new ActionRunner();
            IActionRunner step2 = new ActionRunner();
            IActionRunner step3 = new ActionRunner();
            IActionRunner step4 = new ActionRunner();
            IActionRunner step5 = new ActionRunner();

            _logger.Setup(x => x.Setup(It.IsAny<Guid>(), It.IsAny<Guid>()));
            var settings = new RunnerSettings
            {
                AgentId = 1,
                AgentName = "runner",
                ServerUrl = "https://pipelines.actions.githubusercontent.com/abcd",
                WorkFolder = "_work",
            };

            _config.Setup(x => x.GetSettings())
                .Returns(settings);

            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }

            _tokenSource = new CancellationTokenSource();
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = new Timeline(Guid.NewGuid());

            List<Pipelines.ActionStep> steps = new List<Pipelines.ActionStep>()
            {
                new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "action1",
                },
                new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "action2",
                },
                new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "action3",
                },
                new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "action4",
                },
                new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "action5",
                }
            };

            Guid jobId = Guid.NewGuid();
            _message = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, "test", "test", null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), steps, null, null, null, null);
            GitHubContext github = new GitHubContext();
            github["repository"] = new Pipelines.ContextData.StringContextData("actions/runner");
            _message.ContextData.Add("github", github);

            hc.SetSingleton(_actionManager.Object);
            hc.SetSingleton(_config.Object);
            hc.SetSingleton(_jobServerQueue.Object);
            hc.SetSingleton(_containerProvider.Object);
            hc.SetSingleton(_directoryManager.Object);
            hc.SetSingleton(_diagnosticLogManager.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // JobExecutionContext
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // Initial Job
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step1
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step2
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step3
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step4
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step5
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // prepare1
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // prepare2

            hc.EnqueueInstance<IActionRunner>(step1);
            hc.EnqueueInstance<IActionRunner>(step2);
            hc.EnqueueInstance<IActionRunner>(step3);
            hc.EnqueueInstance<IActionRunner>(step4);
            hc.EnqueueInstance<IActionRunner>(step5);

            _jobEc.Initialize(hc);
            _jobEc.InitializeJob(_message, _tokenSource.Token);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionBuildStepsList()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>()))
                              .Returns(Task.FromResult(new PrepareResult(new List<JobExtensionRunner>(), new Dictionary<Guid, IActionRunner>())));

                List<IStep> result = await jobExtension.InitializeJob(_jobEc, _message);

                var trace = hc.GetTrace();

                trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

                Assert.Equal(5, result.Count);

                Assert.Equal("action1", result[0].DisplayName);
                Assert.Equal("action2", result[1].DisplayName);
                Assert.Equal("action3", result[2].DisplayName);
                Assert.Equal("action4", result[3].DisplayName);
                Assert.Equal("action5", result[4].DisplayName);

                Assert.NotNull(result[0].ExecutionContext);
                Assert.NotNull(result[1].ExecutionContext);
                Assert.NotNull(result[2].ExecutionContext);
                Assert.NotNull(result[3].ExecutionContext);
                Assert.NotNull(result[4].ExecutionContext);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionBuildPreStepsList()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>()))
                              .Returns(Task.FromResult(new PrepareResult(new List<JobExtensionRunner>() { new JobExtensionRunner(null, "", "prepare1", null), new JobExtensionRunner(null, "", "prepare2", null) }, new Dictionary<Guid, IActionRunner>())));

                List<IStep> result = await jobExtension.InitializeJob(_jobEc, _message);

                var trace = hc.GetTrace();

                trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

                Assert.Equal(7, result.Count);

                Assert.Equal("prepare1", result[0].DisplayName);
                Assert.Equal("prepare2", result[1].DisplayName);
                Assert.Equal("action1", result[2].DisplayName);
                Assert.Equal("action2", result[3].DisplayName);
                Assert.Equal("action3", result[4].DisplayName);
                Assert.Equal("action4", result[5].DisplayName);
                Assert.Equal("action5", result[6].DisplayName);

                Assert.NotNull(result[0].ExecutionContext);
                Assert.NotNull(result[1].ExecutionContext);
                Assert.NotNull(result[2].ExecutionContext);
                Assert.NotNull(result[3].ExecutionContext);
                Assert.NotNull(result[4].ExecutionContext);
                Assert.NotNull(result[5].ExecutionContext);
                Assert.NotNull(result[6].ExecutionContext);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UploadDiganosticLogIfEnvironmentVariableSet()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.Variables[Constants.Variables.Actions.RunnerDebug] = "true";

                _jobEc = new Runner.Worker.ExecutionContext();
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                _diagnosticLogManager.Verify(x =>
                    x.UploadDiagnosticLogs(
                        It.IsAny<IExecutionContext>(),
                        It.IsAny<IExecutionContext>(),
                        It.IsAny<Pipelines.AgentJobRequestMessage>(),
                        It.IsAny<DateTime>()),
                    Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DontUploadDiagnosticLogIfEnvironmentVariableFalse()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.Variables[Constants.Variables.Actions.RunnerDebug] = "false";

                _jobEc = new Runner.Worker.ExecutionContext();
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                _diagnosticLogManager.Verify(x =>
                    x.UploadDiagnosticLogs(
                        It.IsAny<IExecutionContext>(),
                        It.IsAny<IExecutionContext>(),
                        It.IsAny<Pipelines.AgentJobRequestMessage>(),
                        It.IsAny<DateTime>()),
                    Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DontUploadDiagnosticLogIfEnvironmentVariableMissing()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                _diagnosticLogManager.Verify(x =>
                    x.UploadDiagnosticLogs(
                        It.IsAny<IExecutionContext>(),
                        It.IsAny<IExecutionContext>(),
                        It.IsAny<Pipelines.AgentJobRequestMessage>(),
                        It.IsAny<DateTime>()),
                    Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnsureFinalizeJobRunsIfMessageHasNoEnvironmentUrl()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.ActionsEnvironment = new ActionsEnvironmentReference("production");

                _jobEc = new Runner.Worker.ExecutionContext {Result = TaskResult.Succeeded};
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(TaskResult.Succeeded, _jobEc.Result);
            }
        }

        [Fact] [Trait("Level", "L0")] [Trait("Category", "Worker")]
        public void EnsureFinalizeJobHandlesNullEnvironmentUrl()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.ActionsEnvironment = new ActionsEnvironmentReference("production")
                {
                    Url = null
                };

                _jobEc = new Runner.Worker.ExecutionContext {Result = TaskResult.Succeeded};
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(TaskResult.Succeeded, _jobEc.Result);
            }
        }

        [Fact] [Trait("Level", "L0")] [Trait("Category", "Worker")]
        public void EnsureFinalizeJobHandlesNullEnvironment()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.ActionsEnvironment = null;

                _jobEc = new Runner.Worker.ExecutionContext {Result = TaskResult.Succeeded};
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(TaskResult.Succeeded, _jobEc.Result);
            }
        }
    }
}
