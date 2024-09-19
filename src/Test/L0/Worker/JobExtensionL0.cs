using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using Xunit;
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
        private Mock<IJobHookProvider> _jobHookProvider;
        private Mock<ISnapshotOperationProvider> _snapshotOperationProvider;

        private Pipelines.Snapshot _requestedSnapshot;

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
            _jobHookProvider = new Mock<IJobHookProvider>();
            _snapshotOperationProvider = new Mock<ISnapshotOperationProvider>();

            _requestedSnapshot = null;
            _snapshotOperationProvider
                .Setup(p => p.CreateSnapshotRequestAsync(It.IsAny<IExecutionContext>(), It.IsAny<Pipelines.Snapshot>()))
                .Returns((IExecutionContext _, object data) =>
                {
                    _requestedSnapshot = data as Pipelines.Snapshot;
                    return Task.CompletedTask;
                });
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
            TaskOrchestrationPlanReference plan = new();
            TimelineReference timeline = new Timeline(Guid.NewGuid());

            List<Pipelines.ActionStep> steps = new()
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
            _message = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, "test", "test", null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), steps, null, null, null, null, null);
            GitHubContext github = new();
            github["repository"] = new Pipelines.ContextData.StringContextData("actions/runner");
            github["secret_source"] = new Pipelines.ContextData.StringContextData("Actions");
            _message.ContextData.Add("github", github);
            _message.Resources.Endpoints.Add(new ServiceEndpoint()
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Url = new Uri("https://pipelines.actions.githubusercontent.com"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = "Test",
                    Parameters = {
                        {"AccessToken", "token"}
                    }
                },
            });

            hc.SetSingleton(_actionManager.Object);
            hc.SetSingleton(_config.Object);
            hc.SetSingleton(_jobServerQueue.Object);
            hc.SetSingleton(_containerProvider.Object);
            hc.SetSingleton(_directoryManager.Object);
            hc.SetSingleton(_diagnosticLogManager.Object);
            hc.SetSingleton(_jobHookProvider.Object);
            hc.SetSingleton(_snapshotOperationProvider.Object);
            hc.SetSingleton(new Mock<IOSWarningChecker>().Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // JobExecutionContext
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // job start hook
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // Initial Job
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step1
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step2
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step3
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step4
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step5
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // prepare1
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // prepare2
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // job complete hook

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

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>(), It.IsAny<Guid>()))
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

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>(), It.IsAny<Guid>()))
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
        public async Task JobExtensionBuildFailsWithoutContainerIfRequired()
        {
            Environment.SetEnvironmentVariable(Constants.Variables.Actions.RequireJobContainer, "true");
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>(), It.IsAny<Guid>()))
                              .Returns(Task.FromResult(new PrepareResult(new List<JobExtensionRunner>() { new JobExtensionRunner(null, "", "prepare1", null), new JobExtensionRunner(null, "", "prepare2", null) }, new Dictionary<Guid, IActionRunner>())));

                await Assert.ThrowsAsync<ArgumentException>(() => jobExtension.InitializeJob(_jobEc, _message));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task UploadDiganosticLogIfEnvironmentVariableSet()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.Variables[Constants.Variables.Actions.RunnerDebug] = "true";

                _jobEc = new Runner.Worker.ExecutionContext();
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

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
        public async Task DontUploadDiagnosticLogIfEnvironmentVariableFalse()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.Variables[Constants.Variables.Actions.RunnerDebug] = "false";

                _jobEc = new Runner.Worker.ExecutionContext();
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

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
        public async Task DontUploadDiagnosticLogIfEnvironmentVariableMissing()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

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
        public async Task EnsureFinalizeJobRunsIfMessageHasNoEnvironmentUrl()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.ActionsEnvironment = new ActionsEnvironmentReference("production");

                _jobEc = new Runner.Worker.ExecutionContext { Result = TaskResult.Succeeded };
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(TaskResult.Succeeded, _jobEc.Result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EnsureFinalizeJobHandlesNullEnvironmentUrl()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.ActionsEnvironment = new ActionsEnvironmentReference("production")
                {
                    Url = null
                };

                _jobEc = new Runner.Worker.ExecutionContext { Result = TaskResult.Succeeded };
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(TaskResult.Succeeded, _jobEc.Result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EnsureFinalizeJobHandlesNullEnvironment()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.ActionsEnvironment = null;

                _jobEc = new Runner.Worker.ExecutionContext { Result = TaskResult.Succeeded };
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(TaskResult.Succeeded, _jobEc.Result);
            }
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EnsurePreAndPostHookStepsIfEnvExists()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_HOOK_JOB_STARTED", "/foo/bar");
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_HOOK_JOB_COMPLETED", "/bar/foo");
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>(), It.IsAny<Guid>()))
                              .Returns(Task.FromResult(new PrepareResult(new List<JobExtensionRunner>(), new Dictionary<Guid, IActionRunner>())));

                List<IStep> result = await jobExtension.InitializeJob(_jobEc, _message);

                var trace = hc.GetTrace();

                var hookStart = result.First() as JobExtensionRunner;

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(Constants.Hooks.JobStartedStepName, hookStart.DisplayName);
                Assert.Equal(Constants.Hooks.JobCompletedStepName, (_jobEc.PostJobSteps.Last() as JobExtensionRunner).DisplayName);
            }

            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_HOOK_JOB_STARTED", null);
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_HOOK_JOB_COMPLETED", null);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EnsureNoPreAndPostHookSteps()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _message.ActionsEnvironment = null;

                _jobEc = new Runner.Worker.ExecutionContext { Result = TaskResult.Succeeded };
                _jobEc.Initialize(hc);
                _jobEc.InitializeJob(_message, _tokenSource.Token);

                var x = _jobEc.JobSteps;

                await jobExtension.FinalizeJob(_jobEc, _message, DateTime.UtcNow);

                Assert.Equal(TaskResult.Succeeded, _jobEc.Result);
                Assert.Equal(0, _jobEc.PostJobSteps.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EnsureNoSnapshotPostJobStep()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>(), It.IsAny<Guid>()))
                    .Returns(Task.FromResult(new PrepareResult(new List<JobExtensionRunner>(), new Dictionary<Guid, IActionRunner>())));

                _message.Snapshot = null;
                await jobExtension.InitializeJob(_jobEc, _message);

                var postJobSteps = _jobEc.PostJobSteps;
                Assert.Equal(0, postJobSteps.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public Task EnsureSnapshotPostJobStepForStringToken()
        {
            var snapshot = new Pipelines.Snapshot("TestImageNameFromStringToken");
            var imageNameValueStringToken = new StringToken(null, null, null, snapshot.ImageName);
            return EnsureSnapshotPostJobStepForToken(imageNameValueStringToken, snapshot);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public Task EnsureSnapshotPostJobStepForMappingToken()
        {
            var snapshot = new Pipelines.Snapshot("TestImageNameFromMappingToken");
            var imageNameValueStringToken = new StringToken(null, null, null, snapshot.ImageName);
            var mappingToken = new MappingToken(null, null, null)
            {
                { new StringToken(null,null,null, PipelineTemplateConstants.ImageName), imageNameValueStringToken }
            };

            return EnsureSnapshotPostJobStepForToken(mappingToken, snapshot);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public Task EnsureSnapshotPostJobStepForMappingToken_1()
        {
            var snapshot = new Pipelines.Snapshot("TestImageNameFromMappingToken") {
                Condition = $"{PipelineTemplateConstants.Success}() && 1==0",
                Version = "2.*"
            };
            var imageNameValueStringToken = new StringToken(null, null, null, snapshot.ImageName);
            var condition = new StringToken(null, null, null, snapshot.Condition);
            var version = new StringToken(null, null, null, snapshot.Version);

            var mappingToken = new MappingToken(null, null, null)
            {
                { new StringToken(null,null,null, PipelineTemplateConstants.ImageName), imageNameValueStringToken },
                { new StringToken(null,null,null, PipelineTemplateConstants.If), condition },
                { new StringToken(null,null,null, PipelineTemplateConstants.CustomImageVersion), version }
            };

            return EnsureSnapshotPostJobStepForToken(mappingToken, snapshot, skipSnapshotStep: true);
        }

        private async Task EnsureSnapshotPostJobStepForToken(TemplateToken snapshotToken, Pipelines.Snapshot expectedSnapshot, bool skipSnapshotStep = false)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var jobExtension = new JobExtension();
                jobExtension.Initialize(hc);

                _actionManager.Setup(x => x.PrepareActionsAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.JobStep>>(), It.IsAny<Guid>()))
                    .Returns(Task.FromResult(new PrepareResult(new List<JobExtensionRunner>(), new Dictionary<Guid, IActionRunner>())));

                _message.Snapshot = snapshotToken;

                await jobExtension.InitializeJob(_jobEc, _message);

                var postJobSteps = _jobEc.PostJobSteps;

                Assert.Equal(1, postJobSteps.Count);
                var snapshotStep = postJobSteps.First();
                _jobEc.JobSteps.Enqueue(snapshotStep);

                var _stepsRunner = new StepsRunner();
                _stepsRunner.Initialize(hc);
                await _stepsRunner.RunAsync(_jobEc);

                Assert.Equal("Create custom image", snapshotStep.DisplayName);
                Assert.Equal(expectedSnapshot.Condition ?? $"{PipelineTemplateConstants.Success}()", snapshotStep.Condition);

                // Run the mock snapshot step, so we can verify it was executed with the expected snapshot object.
                // await snapshotStep.RunAsync();
                if (skipSnapshotStep)
                {
                    Assert.Null(_requestedSnapshot);
                }
                else
                {
                    Assert.NotNull(_requestedSnapshot);
                    Assert.Equal(expectedSnapshot.ImageName, _requestedSnapshot.ImageName);
                    Assert.Equal(expectedSnapshot.Condition ?? $"{PipelineTemplateConstants.Success}()", _requestedSnapshot.Condition);
                    Assert.Equal(expectedSnapshot.Version ?? "1.*", _requestedSnapshot.Version);
                }
            }
        }
    }
}
