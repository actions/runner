using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Moq;
using Xunit;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class RunnerL0 : IDisposable
    {
        private Mock<IConfigurationManager> _configurationManager;
        private Mock<IJobNotification> _jobNotification;
        private Mock<IMessageListener> _messageListener;
        private Mock<IPromptManager> _promptManager;
        private Mock<IJobDispatcher> _jobDispatcher;
        private Mock<IRunnerServer> _runnerServer;
        private Mock<IRunnerConfigUpdater> _runnerConfigUpdater;
        private Mock<ITerminal> _term;
        private Mock<IConfigurationStore> _configStore;
        private Mock<ISelfUpdater> _updater;
        private Mock<IErrorThrottler> _acquireJobThrottler;
        private Mock<ICredentialManager> _credentialManager;
        private Mock<IActionsRunServer> _actionsRunServer;
        private Mock<IRunServer> _runServer;
        private Mock<IBrokerServer> _brokerServer;
        private readonly string _returnJobResultForHosted;

        public RunnerL0()
        {
            _configurationManager = new Mock<IConfigurationManager>();
            _jobNotification = new Mock<IJobNotification>();
            _messageListener = new Mock<IMessageListener>();
            _promptManager = new Mock<IPromptManager>();
            _jobDispatcher = new Mock<IJobDispatcher>();
            _runnerServer = new Mock<IRunnerServer>();
            _runnerConfigUpdater = new Mock<IRunnerConfigUpdater>();
            _term = new Mock<ITerminal>();
            _configStore = new Mock<IConfigurationStore>();
            _updater = new Mock<ISelfUpdater>();
            _acquireJobThrottler = new Mock<IErrorThrottler>();
            _credentialManager = new Mock<ICredentialManager>();
            _actionsRunServer = new Mock<IActionsRunServer>();
            _runServer = new Mock<IRunServer>();
            _brokerServer = new Mock<IBrokerServer>();

            _returnJobResultForHosted = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_RETURN_JOB_RESULT_FOR_HOSTED");
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_RETURN_JOB_RESULT_FOR_HOSTED", null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_RETURN_JOB_RESULT_FOR_HOSTED", _returnJobResultForHosted);
        }

        private Pipelines.AgentJobRequestMessage CreateJobRequestMessage(string jobName)
        {
            TaskOrchestrationPlanReference plan = new();
            TimelineReference timeline = null;
            Guid jobId = Guid.NewGuid();
            return new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, "test", "test", null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null, null, null, null, null);
        }

        private JobCancelMessage CreateJobCancelMessage()
        {
            var message = new JobCancelMessage(Guid.NewGuid(), TimeSpan.FromSeconds(0));
            return message;
        }

        private RunnerSettings CreateRunnerSettings(bool ephemeral = false)
        {
            return new RunnerSettings
            {
                PoolId = 43242,
                AgentId = 5678,
                AgentName = "agent1",
                ServerUrl = "https://github.com",
                Ephemeral = ephemeral
            };
        }

        private RunnerSettings CreateChangedRunnerSettings(RunnerSettings settings)
        {
            return new RunnerSettings
            {
                PoolId = settings.PoolId + 1,
                AgentId = settings.AgentId,
                AgentName = settings.AgentName,
                ServerUrl = settings.ServerUrl,
                Ephemeral = settings.Ephemeral
            };
        }

        private TaskAgentMessage CreateRunnerRefreshConfigTaskAgentMessage(long messageId, RunnerSettings settings)
        {
            return new TaskAgentMessage()
            {
                Body = JsonUtility.ToString(new RunnerRefreshConfigMessage(
                    runnerQualifiedId: $"valid/runner/qualifiedid/{settings.AgentId}",
                    configType: "runner",
                    serviceType: "pipelines",
                    configRefreshUrl: "https://example.test/refresh")),
                MessageId = messageId,
                MessageType = RunnerRefreshConfigMessage.MessageType
            };
        }

        private TaskAgentMessage CreateJobRequestTaskAgentMessage(long messageId)
        {
            return new TaskAgentMessage()
            {
                Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
                MessageId = messageId,
                MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
            };
        }

        private TaskAgentMessage CreateJobCancelTaskAgentMessage(long messageId)
        {
            return new TaskAgentMessage()
            {
                Body = JsonUtility.ToString(CreateJobCancelMessage()),
                MessageId = messageId,
                MessageType = JobCancelMessage.MessageType
            };
        }

        private TaskAgentMessage CreateAgentRefreshTaskAgentMessage(long messageId, ulong agentId)
        {
            return new TaskAgentMessage()
            {
                Body = JsonUtility.ToString(new AgentRefreshMessage(agentId, "2.123.0")),
                MessageId = messageId,
                MessageType = AgentRefreshMessage.MessageType
            };
        }

        private void SetupRunCommandWithMigratedSettings(TestHostContext hc, RunnerSettings settings, RunnerSettings migratedSettings)
        {
            hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
            hc.SetSingleton<IJobNotification>(_jobNotification.Object);
            hc.SetSingleton<IMessageListener>(_messageListener.Object);
            hc.SetSingleton<IPromptManager>(_promptManager.Object);
            hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            hc.SetSingleton<IConfigurationStore>(_configStore.Object);
            hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);

            _configurationManager.Setup(x => x.LoadSettings())
                .Returns(settings);
            _configurationManager.Setup(x => x.LoadMigratedSettings())
                .Returns(migratedSettings);
            _configurationManager.Setup(x => x.IsConfigured())
                .Returns(true);
            _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
        }

        private void SetupRunnerMessageLoop(
            TestHostContext hc,
            RunnerSettings settings,
            IJobDispatcher jobDispatcher,
            RunnerConfigUpdateResult updateResult)
        {
            hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
            hc.SetSingleton<IJobNotification>(_jobNotification.Object);
            hc.SetSingleton<IMessageListener>(_messageListener.Object);
            hc.SetSingleton<IPromptManager>(_promptManager.Object);
            hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            hc.SetSingleton<IConfigurationStore>(_configStore.Object);
            hc.SetSingleton<IRunnerConfigUpdater>(_runnerConfigUpdater.Object);
            hc.SetSingleton<ISelfUpdater>(_updater.Object);
            hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
            hc.EnqueueInstance<IJobDispatcher>(jobDispatcher);

            _configurationManager.Setup(x => x.LoadSettings())
                .Returns(settings);
            _configurationManager.Setup(x => x.LoadMigratedSettings())
                .Returns((RunnerSettings)null);
            _configurationManager.Setup(x => x.IsConfigured())
                .Returns(true);
            _messageListener.Setup(x => x.DeleteSessionAsync())
                .Returns(Task.CompletedTask);
            _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                .Returns(Task.CompletedTask);
            _jobNotification.Setup(x => x.StartClient(It.IsAny<string>()));
            _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
            _runnerConfigUpdater.Setup(x => x.UpdateRunnerConfigAsync(
                    It.IsAny<string>(),
                    "runner",
                    "pipelines",
                    It.IsAny<string>()))
                .ReturnsAsync(updateResult);
        }

        private async Task<int> RunRefreshConfigMessages(
            TestHostContext hc,
            RunnerSettings activeSettings,
            RunnerSettings settingsAfterRestart,
            RunnerConfigUpdateResult updateResult)
        {
            var runner = new Runner.Listener.Runner();
            hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
            hc.SetSingleton<IJobNotification>(_jobNotification.Object);
            hc.SetSingleton<IMessageListener>(_messageListener.Object);
            hc.SetSingleton<IPromptManager>(_promptManager.Object);
            hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            hc.SetSingleton<IConfigurationStore>(_configStore.Object);
            hc.SetSingleton<IRunnerConfigUpdater>(_runnerConfigUpdater.Object);
            hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
            hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);
            hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

            runner.Initialize(hc);

            var messages = new Queue<TaskAgentMessage>();
            messages.Enqueue(new TaskAgentMessage()
            {
                Body = JsonUtility.ToString(new RunnerRefreshConfigMessage(
                    runnerQualifiedId: $"valid/runner/qualifiedid/{activeSettings.AgentId}",
                    configType: "runner",
                    serviceType: "pipelines",
                    configRefreshUrl: "https://example.test/refresh")),
                MessageId = 4234,
                MessageType = RunnerRefreshConfigMessage.MessageType
            });
            messages.Enqueue(new Pipelines.HostedRunnerShutdownMessage("L0 test complete").GetAgentMessage());

            _configurationManager.SetupSequence(x => x.LoadSettings())
                .Returns(activeSettings)
                .Returns(settingsAfterRestart ?? activeSettings);
            _configurationManager.Setup(x => x.LoadMigratedSettings())
                .Returns((RunnerSettings)null);
            _configurationManager.Setup(x => x.IsConfigured())
                .Returns(true);
            _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
            _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => messages.Dequeue());
            _messageListener.Setup(x => x.DeleteSessionAsync())
                .Returns(Task.CompletedTask);
            _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                .Returns(Task.CompletedTask);
            _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                .Callback(() =>
                {
                });
            _runnerConfigUpdater.Setup(x => x.UpdateRunnerConfigAsync(
                    It.IsAny<string>(),
                    "runner",
                    "pipelines",
                    It.IsAny<string>()))
                .ReturnsAsync(updateResult);
            _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

            var command = new CommandSettings(hc, new string[] { "run" });
            Task<int> runnerTask = runner.ExecuteCommand(command);

            await Task.WhenAny(runnerTask, Task.Delay(30000));
            Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
            Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
            return await runnerTask;
        }

        private static async Task WaitForSignal(Task task, string message)
        {
            if (await Task.WhenAny(task, Task.Delay(2000)) != task)
            {
                Assert.Fail(message);
            }

            await task;
        }

        private static async Task<T> WaitForRunnerTask<T>(Task<T> task)
        {
            if (await Task.WhenAny(task, Task.Delay(2000)) != task)
            {
                Assert.Fail("Runner task timed out.");
            }

            return await task;
        }

        private sealed class TestJobDispatcher : IJobDispatcher
        {
            private readonly TaskCompletionSource<TaskResult> _runOnceJobCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool Busy { get; private set; }

            public int RunCount { get; private set; }

            public int CancelCount { get; private set; }

            public TaskCompletionSource<bool> JobDispatched { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public TaskCompletionSource<TaskResult> RunOnceJobCompleted => _runOnceJobCompleted;

            public event EventHandler<JobStatusEventArgs> JobStatus;

            public void Initialize(IHostContext context)
            {
            }

            public void Run(Pipelines.AgentJobRequestMessage message, bool runOnce = false)
            {
                Busy = true;
                RunCount++;
                JobStatus?.Invoke(this, new JobStatusEventArgs(TaskAgentStatus.Busy));
                JobDispatched.TrySetResult(true);
            }

            public bool Cancel(JobCancelMessage message)
            {
                CancelCount++;
                return true;
            }

            public Task WaitAsync(CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public Task ShutdownAsync()
            {
                return Task.CompletedTask;
            }

            public void SetBusy(bool busy)
            {
                Busy = busy;
                JobStatus?.Invoke(this, new JobStatusEventArgs(busy ? TaskAgentStatus.Busy : TaskAgentStatus.Online));
            }

            public void RaiseStatus(TaskAgentStatus status)
            {
                JobStatus?.Invoke(this, new JobStatusEventArgs(status));
            }

            public void CompleteRunOnce(TaskResult result)
            {
                SetBusy(false);
                _runOnceJobCompleted.TrySetResult(result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task RunnerRefreshConfigMessage_FailedUpdate_DoesNotRestartSession()
        {
            using (var hc = new TestHostContext(this))
            {
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    AgentName = "agent1",
                    ServerUrl = "https://github.com"
                };

                var result = await RunRefreshConfigMessages(hc, settings, null, RunnerConfigUpdateResult.Failed());

                Assert.Equal(Constants.Runner.ReturnCode.Success, result);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _runnerConfigUpdater.Verify(x => x.UpdateRunnerConfigAsync(It.IsAny<string>(), "runner", "pipelines", It.IsAny<string>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task RunnerRefreshConfigMessage_NoTargetUpdate_DoesNotRestartSession()
        {
            using (var hc = new TestHostContext(this))
            {
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    AgentName = "agent1",
                    ServerUrl = "https://github.com"
                };

                var result = await RunRefreshConfigMessages(hc, settings, null, RunnerConfigUpdateResult.NoTarget());

                Assert.Equal(Constants.Runner.ReturnCode.Success, result);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _runnerConfigUpdater.Verify(x => x.UpdateRunnerConfigAsync(It.IsAny<string>(), "runner", "pipelines", It.IsAny<string>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task RunnerRefreshConfigMessage_UpdatedActiveSettings_DoesNotRestartSession()
        {
            using (var hc = new TestHostContext(this))
            {
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    AgentName = "agent1",
                    ServerUrl = "https://github.com"
                };

                var result = await RunRefreshConfigMessages(hc, settings, null, RunnerConfigUpdateResult.Updated(settings));

                Assert.Equal(Constants.Runner.ReturnCode.Success, result);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _runnerConfigUpdater.Verify(x => x.UpdateRunnerConfigAsync(It.IsAny<string>(), "runner", "pipelines", It.IsAny<string>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task RunnerRefreshConfigMessage_UpdatedInactiveMigratedSettings_RestartsSession()
        {
            using (var hc = new TestHostContext(this))
            {
                var activeSettings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    AgentName = "agent1",
                    ServerUrl = "https://github.com"
                };
                var targetSettings = new RunnerSettings
                {
                    PoolId = 43243,
                    AgentId = 5678,
                    AgentName = "agent1",
                    ServerUrl = "https://github.com"
                };

                var result = await RunRefreshConfigMessages(hc, activeSettings, targetSettings, RunnerConfigUpdateResult.Updated(targetSettings));
                Assert.Equal(Constants.Runner.ReturnCode.Success, result);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
                _runnerConfigUpdater.Verify(x => x.UpdateRunnerConfigAsync(It.IsAny<string>(), "runner", "pipelines", It.IsAny<string>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigRestartsWhenBusyJobBecomesIdleWithoutMessage()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var secondFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4234, settings);
                        }

                        secondFetchStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(secondFetchStarted.Task, $"{nameof(_messageListener.Object.GetNextMessageAsync)} was not invoked after config refresh.");
                jobDispatcher.SetBusy(false);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(2, fetchCount);
                _runnerConfigUpdater.Verify(x => x.UpdateRunnerConfigAsync($"valid/runner/qualifiedid/{settings.AgentId}", "runner", "pipelines", "https://example.test/refresh"), Times.Once());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4234)), Times.Once());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.OnJobStatus(jobDispatcher, It.Is<JobStatusEventArgs>(e => e.Status == TaskAgentStatus.Online)), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigContinuesHandlingCancelsWhileBusy()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var thirdFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4234, settings);
                        }

                        if (fetchCount == 2)
                        {
                            return CreateJobCancelTaskAgentMessage(4235);
                        }

                        thirdFetchStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(thirdFetchStarted.Task, $"{nameof(_messageListener.Object.GetNextMessageAsync)} was not invoked after job cancel.");
                jobDispatcher.SetBusy(false);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(3, fetchCount);
                Assert.Equal(1, jobDispatcher.CancelCount);
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4234)), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4235)), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigHandlesMessageReturnedAfterIdleCancellation()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var secondFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var releaseSecondFetch = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var thirdFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4234, settings);
                        }

                        if (fetchCount == 2)
                        {
                            secondFetchStarted.TrySetResult(true);
                            await releaseSecondFetch.Task;
                            return CreateJobRequestTaskAgentMessage(4235);
                        }

                        thirdFetchStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(secondFetchStarted.Task, "Second message fetch was not started.");
                jobDispatcher.SetBusy(false);
                releaseSecondFetch.SetResult(true);
                await WaitForSignal(jobDispatcher.JobDispatched.Task, $"{nameof(jobDispatcher.Run)} was not invoked.");
                await WaitForSignal(thirdFetchStarted.Task, "Third message fetch was not started.");
                jobDispatcher.SetBusy(false);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(3, fetchCount);
                Assert.Equal(1, jobDispatcher.RunCount);
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4234)), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4235)), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigHandlesMessageBeforeIdleWake()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var thirdFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4234, settings);
                        }

                        if (fetchCount == 2)
                        {
                            return CreateJobRequestTaskAgentMessage(4235);
                        }

                        thirdFetchStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(thirdFetchStarted.Task, "Third message fetch was not started.");
                jobDispatcher.SetBusy(false);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(3, fetchCount);
                Assert.Equal(1, jobDispatcher.RunCount);
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4234)), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4235)), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigIgnoresStaleOnlineBeforeRestartPending()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var firstFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var releaseFirstFetch = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var secondFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var secondFetchCanceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            firstFetchStarted.TrySetResult(true);
                            await releaseFirstFetch.Task;
                            return CreateRunnerRefreshConfigTaskAgentMessage(4234, settings);
                        }

                        secondFetchStarted.TrySetResult(true);
                        try
                        {
                            await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        }
                        catch (OperationCanceledException)
                        {
                            secondFetchCanceled.TrySetResult(true);
                            throw;
                        }

                        return null;
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(firstFetchStarted.Task, "First message fetch was not started.");
                jobDispatcher.RaiseStatus(TaskAgentStatus.Online);
                releaseFirstFetch.SetResult(true);
                await WaitForSignal(secondFetchStarted.Task, "Second message fetch was not started.");
                Assert.False(secondFetchCanceled.Task.IsCompleted);

                jobDispatcher.SetBusy(false);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(2, fetchCount);
                await WaitForSignal(secondFetchCanceled.Task, "Second message fetch was not canceled after real idle wake.");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigRestartsWhenJobAlreadyIdleBeforeIdleWait()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns((CancellationToken token) =>
                    {
                        fetchCount++;
                        return Task.FromResult(CreateRunnerRefreshConfigTaskAgentMessage(4234, settings));
                    });
                _runnerConfigUpdater.Setup(x => x.UpdateRunnerConfigAsync(It.IsAny<string>(), "runner", "pipelines", It.IsAny<string>()))
                    .Callback(() => jobDispatcher.SetBusy(false))
                    .ReturnsAsync(RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));

                var command = new CommandSettings(hc, new string[] { "run" });
                var result = await WaitForRunnerTask(runner.ExecuteCommand(command));

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(1, fetchCount);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigRestartsWhenJobIdlesBeforeIdleWaitStarts()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var secondFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4234, settings);
                        }

                        secondFetchStarted.TrySetResult(true);
                        jobDispatcher.SetBusy(false);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(secondFetchStarted.Task, "Second message fetch was not started.");
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(2, fetchCount);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigDoesNotOverrideRunOnceCompletion()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings(ephemeral: true);
                var jobDispatcher = new TestJobDispatcher();
                var thirdFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateJobRequestTaskAgentMessage(4234);
                        }

                        if (fetchCount == 2)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4235, settings);
                        }

                        thirdFetchStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(thirdFetchStarted.Task, "Third message fetch was not started.");
                jobDispatcher.CompleteRunOnce(TaskResult.Succeeded);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.Success, result);
                Assert.Equal(3, fetchCount);
                Assert.Equal(1, jobDispatcher.RunCount);
                _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigDoesNotUseIdleWakeBeforeRunOnceCompletion()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings(ephemeral: true);
                var jobDispatcher = new TestJobDispatcher();
                var thirdFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var runOnceCompletionStarted = false;
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateJobRequestTaskAgentMessage(4234);
                        }

                        if (fetchCount == 2)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4235, settings);
                        }

                        if (fetchCount == 3)
                        {
                            thirdFetchStarted.TrySetResult(true);
                            try
                            {
                                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                            }
                            catch (OperationCanceledException) when (!runOnceCompletionStarted)
                            {
                                throw new InvalidOperationException("Idle restart cancelled run-once fetch before job completion.");
                            }

                            throw new OperationCanceledException(token);
                        }

                        throw new InvalidOperationException("Runner should exit after run-once completion without starting another fetch.");
                    });

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(thirdFetchStarted.Task, "Third message fetch was not started.");
                jobDispatcher.SetBusy(false);
                await Task.Yield();
                await Task.Yield();
                runOnceCompletionStarted = true;
                jobDispatcher.CompleteRunOnce(TaskResult.Succeeded);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.Success, result);
                Assert.Equal(3, fetchCount);
                Assert.Equal(1, jobDispatcher.RunCount);
                _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigDoesNotOverrideSelfUpdateCompletion()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                jobDispatcher.SetBusy(true);
                var selfUpdateCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var thirdFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateAgentRefreshTaskAgentMessage(4234, settings.AgentId);
                        }

                        if (fetchCount == 2)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4235, settings);
                        }

                        thirdFetchStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), false, It.IsAny<CancellationToken>()))
                    .Returns(selfUpdateCompleted.Task);

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(thirdFetchStarted.Task, "Third message fetch was not started.");
                selfUpdateCompleted.SetResult(true);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.RunnerUpdating, result);
                Assert.Equal(3, fetchCount);
                _updater.Verify(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), false, It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4234)), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4235)), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerRefreshConfigRestartsAfterSelfUpdateCompletesWithoutUpdateWhileIdle()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                var settings = CreateRunnerSettings();
                var jobDispatcher = new TestJobDispatcher();
                var selfUpdateCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var thirdFetchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var fetchCount = 0;

                SetupRunnerMessageLoop(hc, settings, jobDispatcher, RunnerConfigUpdateResult.Updated(CreateChangedRunnerSettings(settings)));
                runner.Initialize(hc);

                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSessionResult.Success)
                    .ReturnsAsync(CreateSessionResult.Failure);
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                    {
                        fetchCount++;
                        if (fetchCount == 1)
                        {
                            return CreateAgentRefreshTaskAgentMessage(4234, settings.AgentId);
                        }

                        if (fetchCount == 2)
                        {
                            return CreateRunnerRefreshConfigTaskAgentMessage(4235, settings);
                        }

                        thirdFetchStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return null;
                    });
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), false, It.IsAny<CancellationToken>()))
                    .Returns(selfUpdateCompleted.Task);

                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                await WaitForSignal(thirdFetchStarted.Task, "Third message fetch was not started.");
                selfUpdateCompleted.SetResult(false);
                var result = await WaitForRunnerTask(runnerTask);

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, result);
                Assert.Equal(3, fetchCount);
                _updater.Verify(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), false, It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4234)), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.Is<TaskAgentMessage>(m => m.MessageId == 4235)), Times.Once());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        //process 2 new job messages, and one cancel message
        public async Task TestRunAsync()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242
                };

                var message = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message);
                var signalWorkerComplete = new SemaphoreSlim(0, 1);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                        {
                            if (0 == messages.Count)
                            {
                                signalWorkerComplete.Release();
                                await Task.Delay(2000, hc.RunnerShutdownToken);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
                    .Callback(() =>
                    {

                    });
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task runnerTask = runner.ExecuteCommand(command);

                //Assert
                //wait for the runner to run one job
                if (!await signalWorkerComplete.WaitAsync(2000))
                {
                    Assert.Fail($"{nameof(_messageListener.Object.GetNextMessageAsync)} was not invoked.");
                }
                else
                {
                    //Act
                    hc.ShutdownRunner(ShutdownReason.UserCancelled); //stop Runner

                    //Assert
                    Task[] taskToWait2 = { runnerTask, Task.Delay(2000) };
                    //wait for the runner to exit
                    await Task.WhenAny(taskToWait2);

                    Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                    Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                    Assert.True(runnerTask.IsCanceled);

                    _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()), Times.Once(),
                         $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
                    _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
                    _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                    _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                    _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.AtLeastOnce());

                    // verify that we didn't try to delete local settings file (since we're not ephemeral)
                    _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Never());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunAsyncCleanupLocalConfigWhenGetNextMessageReturnsNotFound()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IBrokerServer>(_brokerServer.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                var messageListener = new MessageListener();
                messageListener.Initialize(hc);
                hc.SetSingleton<IMessageListener>(messageListener);

                runner.Initialize(hc);

                var settings = new RunnerSettings
                {
                    AgentId = 1,
                    AgentName = "myagent",
                    PoolId = 43242,
                    PoolName = "default",
                    ServerUrl = "http://myserver",
                    WorkFolder = "_work",
                    Ephemeral = false,
                };

                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _credentialManager.Setup(x => x.LoadCredentials(false)).Returns(new VssCredentials());
                _runnerServer.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>()))
                    .Returns(Task.CompletedTask);
                _runnerServer.Setup(x => x.CreateAgentSessionAsync(
                        settings.PoolId,
                        It.Is<TaskAgentSession>(x => x != null),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new TaskAgentSession()));
                _runnerServer.Setup(x => x.GetAgentMessageAsync(
                        settings.PoolId,
                        It.IsAny<Guid>(),
                        It.IsAny<long?>(),
                        TaskAgentStatus.Online,
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new TaskAgentNotFoundException("runner not found"));
                _jobNotification.Setup(x => x.StartClient(It.IsAny<string>()));
                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                var result = await runner.ExecuteCommand(command);

                //Assert
                Assert.Equal(Constants.Runner.ReturnCode.Success, result);
                _runnerServer.Verify(x => x.CreateAgentSessionAsync(
                    settings.PoolId,
                    It.Is<TaskAgentSession>(x => x != null),
                    It.IsAny<CancellationToken>()), Times.Once());
                _runnerServer.Verify(x => x.GetAgentMessageAsync(
                    settings.PoolId,
                    It.IsAny<Guid>(),
                    It.IsAny<long?>(),
                    TaskAgentStatus.Online,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()), Times.Once());
                _runnerServer.Verify(x => x.DeleteAgentSessionAsync(
                    It.IsAny<int>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()), Times.Never());
                _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Once());
            }
        }

        public static TheoryData<string[], bool, Times> RunAsServiceTestData = new TheoryData<string[], bool, Times>()
        {
            // staring with run command, configured as run as service, should start the runner
            { new [] { "run" }, true, Times.Once() },
            // starting with no argument, configured not to run as service, should start runner interactively
            { new [] { "run" }, false, Times.Once() }
        };
        [Theory]
        [MemberData(nameof(RunAsServiceTestData))]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestExecuteCommandForRunAsService(string[] args, bool configureAsService, Times expectedTimes)
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);

                var command = new CommandSettings(hc, args);

                _configurationManager.Setup(x => x.IsConfigured()).Returns(true);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(new RunnerSettings { });

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(configureAsService);

                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Failure));

                var runner = new Runner.Listener.Runner();
                runner.Initialize(hc);
                await runner.ExecuteCommand(command);

                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), expectedTimes);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestMachineProvisionerCLI()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);

                var command = new CommandSettings(hc, new[] { "run" });

                _configurationManager.Setup(x => x.IsConfigured()).
                    Returns(true);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(new RunnerSettings { });

                _configStore.Setup(x => x.IsServiceConfigured())
                    .Returns(false);

                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Failure));

                var runner = new Runner.Listener.Runner();
                runner.Initialize(hc);
                await runner.ExecuteCommand(command);

                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunWithMigratedSessionConflictReturnsSessionConflict()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                SetupRunCommandWithMigratedSettings(hc, new RunnerSettings(), new RunnerSettings());
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.SessionConflict));

                runner.Initialize(hc);

                var returnCode = await runner.ExecuteCommand(new CommandSettings(hc, new string[] { "run" }));

                Assert.Equal(Constants.Runner.ReturnCode.SessionConflict, returnCode);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _jobNotification.Verify(x => x.StartClient(It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunWithMigratedSessionFailureFallsBackToOriginalSettings()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                SetupRunCommandWithMigratedSettings(hc, new RunnerSettings(), new RunnerSettings());
                _messageListener.SetupSequence(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Failure))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Failure));

                runner.Initialize(hc);

                var returnCode = await runner.ExecuteCommand(new CommandSettings(hc, new string[] { "run" }));

                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, returnCode);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
                _jobNotification.Verify(x => x.StartClient(It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunWithMigratedSessionTokenRevokedDoesNotFallBackToOriginalSettings()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                SetupRunCommandWithMigratedSettings(hc, new RunnerSettings(), new RunnerSettings());
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new TaskAgentAccessTokenExpiredException("token revoked"));

                runner.Initialize(hc);

                var returnCode = await runner.ExecuteCommand(new CommandSettings(hc, new string[] { "run" }));

                Assert.Equal(Constants.Runner.ReturnCode.Success, returnCode);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _jobNotification.Verify(x => x.StartClient(It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunWithMigratedSessionHostedDeprovisionDoesNotFallBackToOriginalSettings()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                SetupRunCommandWithMigratedSettings(hc, new RunnerSettings(), new RunnerSettings());
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new HostedRunnerDeprovisionedException("hosted runner deprovisioned"));

                runner.Initialize(hc);

                var returnCode = await runner.ExecuteCommand(new CommandSettings(hc, new string[] { "run" }));

                Assert.Equal(Constants.Runner.ReturnCode.Success, returnCode);
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _jobNotification.Verify(x => x.StartClient(It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunWithMigratedSessionShutdownCancellationDoesNotFallBackToOriginalSettings()
        {
            using (var hc = new TestHostContext(this))
            {
                var runner = new Runner.Listener.Runner();
                SetupRunCommandWithMigratedSettings(hc, new RunnerSettings(), new RunnerSettings());
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Callback(() => hc.ShutdownRunner(ShutdownReason.UserCancelled))
                    .ThrowsAsync(new OperationCanceledException(hc.RunnerShutdownToken));

                runner.Initialize(hc);

                await Assert.ThrowsAsync<OperationCanceledException>(() => runner.ExecuteCommand(new CommandSettings(hc, new string[] { "run" })));

                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _jobNotification.Verify(x => x.StartClient(It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunOnce()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    Ephemeral = true
                };

                var message = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);

                var runOnceJobCompleted = new TaskCompletionSource<TaskResult>();
                _jobDispatcher.Setup(x => x.RunOnceJobCompleted)
                    .Returns(runOnceJobCompleted);
                _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
                    .Callback(() =>
                    {
                        runOnceJobCompleted.TrySetResult(TaskResult.Succeeded);
                    });
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                //wait for the runner to run one job and exit
                await Task.WhenAny(runnerTask, Task.Delay(30000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.False(runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                if (runnerTask.IsCompleted)
                {
                    Assert.Equal(Constants.Runner.ReturnCode.Success, await runnerTask);
                }

                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once(),
                     $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
                _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.AtLeastOnce());

                // verify that we did try to delete local settings file (since we're ephemeral)
                _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunOnceOnlyTakeOneJobMessage()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    Ephemeral = true
                };

                var message1 = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                };
                var message2 = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
                    MessageId = 4235,
                    MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message1);
                messages.Enqueue(message2);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);

                var runOnceJobCompleted = new TaskCompletionSource<TaskResult>();
                _jobDispatcher.Setup(x => x.RunOnceJobCompleted)
                    .Returns(runOnceJobCompleted);
                _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
                    .Callback(() =>
                    {
                        runOnceJobCompleted.TrySetResult(TaskResult.Succeeded);
                    });
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                //wait for the runner to run one job and exit
                await Task.WhenAny(runnerTask, Task.Delay(30000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                if (runnerTask.IsCompleted)
                {
                    Assert.Equal(Constants.Runner.ReturnCode.Success, await runnerTask);
                }

                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once(),
                     $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
                _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunOnceHandleUpdateMessage()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ISelfUpdater>(_updater.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = true
                };

                var message1 = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(new AgentRefreshMessage(settings.AgentId, "2.123.0")),
                    MessageId = 4234,
                    MessageType = AgentRefreshMessage.MessageType
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message1);
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(true));
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                //wait for the runner to exit with right return code
                await Task.WhenAny(runnerTask, Task.Delay(30000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                if (runnerTask.IsCompleted)
                {
                    Assert.Equal(Constants.Runner.ReturnCode.RunOnceRunnerUpdating, await runnerTask);
                }

                _updater.Verify(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), false, It.IsAny<CancellationToken>()), Times.Once);
                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Never());
                _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRemoveLocalRunnerConfig()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);

                var command = new CommandSettings(hc, new[] { "remove", "--local" });

                _configStore.Setup(x => x.IsConfigured())
                    .Returns(true);

                _configStore.Setup(x => x.HasCredentials())
                    .Returns(true);


                var runner = new Runner.Listener.Runner();
                runner.Initialize(hc);
                await runner.ExecuteCommand(command);

                // verify that we delete the local runner config with the correct remove parameter
                _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestReportAuthMigrationTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = true
                };

                var message1 = new TaskAgentMessage()
                {
                    MessageId = 4234,
                    MessageType = "unknown"
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message1);
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(true));
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                        {
                            hc.GetTrace().Info("Waiting for message");
                            Assert.False(hc.AllowAuthMigration);
                            await Task.Delay(100, token);

                            var traceFile = Path.GetTempFileName();
                            File.Copy(hc.TraceFileName, traceFile, true);
                            Assert.DoesNotContain("Checking for auth migration telemetry to report", File.ReadAllText(traceFile));

                            hc.EnableAuthMigration("L0Test");
                            hc.DeferAuthMigration(TimeSpan.FromSeconds(1), "L0Test");
                            hc.EnableAuthMigration("L0Test");
                            hc.DeferAuthMigration(TimeSpan.FromSeconds(1), "L0Test");

                            await Task.Delay(1000, token);

                            hc.ShutdownRunner(ShutdownReason.UserCancelled);

                            File.Copy(hc.TraceFileName, traceFile, true);
                            Assert.Contains("Checking for auth migration telemetry to report", File.ReadAllText(traceFile));

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new TaskAgent()));

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                var returnCode = await runner.ExecuteCommand(command);

                //Assert
                Assert.Equal(Constants.Runner.ReturnCode.Success, returnCode);

                _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());

                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("L0Test")), It.IsAny<CancellationToken>()), Times.Exactly(4));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerJobRequestMessageFromPipeline()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ISelfUpdater>(_updater.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                hc.EnqueueInstance<IActionsRunServer>(_actionsRunServer.Object);
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = true,
                    ServerUrl = "https://github.com",
                };

                var message1 = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(new RunnerJobRequestRef() { BillingOwnerId = "github", RunnerRequestId = "999" }),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.RunnerJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message1);
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(true));
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000, token);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });
                _actionsRunServer.Setup(x => x.GetJobMessageAsync("999", It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(CreateJobRequestMessage("test")));

                _credentialManager.Setup(x => x.LoadCredentials(false)).Returns(new VssCredentials());

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                var completedTask = new TaskCompletionSource<TaskResult>();
                completedTask.SetResult(TaskResult.Succeeded);
                _jobDispatcher.Setup(x => x.RunOnceJobCompleted).Returns(completedTask);

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                //wait for the runner to exit with right return code
                await Task.WhenAny(runnerTask, Task.Delay(30000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                if (runnerTask.IsCompleted)
                {
                    Assert.Equal(Constants.Runner.ReturnCode.Success, await runnerTask);
                }

                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once());
                _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
                _credentialManager.Verify(x => x.LoadCredentials(false), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerJobRequestMessageFromRunService()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ISelfUpdater>(_updater.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                hc.EnqueueInstance<IRunServer>(_runServer.Object);
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = true,
                    ServerUrl = "https://github.com",
                };

                var message1 = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(new RunnerJobRequestRef() { BillingOwnerId = "github", RunnerRequestId = "999", RunServiceUrl = "https://run-service.com" }),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.RunnerJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message1);
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(true));
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000, token);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });
                _runServer.Setup(x => x.GetJobMessageAsync("999", "github", It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(CreateJobRequestMessage("test")));

                _credentialManager.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                var completedTask = new TaskCompletionSource<TaskResult>();
                completedTask.SetResult(TaskResult.Succeeded);
                _jobDispatcher.Setup(x => x.RunOnceJobCompleted).Returns(completedTask);

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                //wait for the runner to exit with right return code
                await Task.WhenAny(runnerTask, Task.Delay(30000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                if (runnerTask.IsCompleted)
                {
                    Assert.Equal(Constants.Runner.ReturnCode.Success, await runnerTask);
                }

                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once());
                _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
                _credentialManager.Verify(x => x.LoadCredentials(true), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestEphemeralRunnerJobRequestMessageFromRunServiceExitsOnAcknowledgeJobNotFound()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ISelfUpdater>(_updater.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                hc.EnqueueInstance<IRunServer>(_runServer.Object);
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = true,
                    ServerUrl = "https://github.com",
                };

                var message = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(new RunnerJobRequestRef() { BillingOwnerId = "github", RunnerRequestId = "999", RunServiceUrl = "https://run-service.com", ShouldAcknowledge = true }),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.RunnerJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000, token);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.AcknowledgeMessageAsync("999", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RunnerRequestJobNotFoundException("Job not found"));
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                await Task.WhenAny(runnerTask, Task.Delay(30000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                if (runnerTask.IsCompleted)
                {
                    Assert.Equal(Constants.Runner.ReturnCode.Success, await runnerTask);
                }

                _messageListener.Verify(x => x.AcknowledgeMessageAsync("999", It.IsAny<CancellationToken>()), Times.Once());
                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()), Times.Never());
                _runServer.Verify(x => x.GetJobMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
                _credentialManager.Verify(x => x.LoadCredentials(true), Times.Never());
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
                _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Once());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerJobRequestMessageFromRunServiceContinuesOnAcknowledgeJobNotFoundForPersistentRunner()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ISelfUpdater>(_updater.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                hc.EnqueueInstance<IRunServer>(_runServer.Object);
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = false,
                    ServerUrl = "https://github.com",
                };

                var message = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(new RunnerJobRequestRef() { BillingOwnerId = "github", RunnerRequestId = "999", RunServiceUrl = "https://run-service.com", ShouldAcknowledge = true }),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.RunnerJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message);
                var signalWorkerStarted = new SemaphoreSlim(0, 1);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000, token);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.AcknowledgeMessageAsync("999", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RunnerRequestJobNotFoundException("Job not found"));
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });
                _runServer.Setup(x => x.GetJobMessageAsync("999", "github", It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(CreateJobRequestMessage("test")));
                _credentialManager.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());
                _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), false))
                    .Callback(() =>
                    {
                        signalWorkerStarted.Release();
                    });

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                if (!await signalWorkerStarted.WaitAsync(2000))
                {
                    Assert.Fail($"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
                }

                hc.ShutdownRunner(ShutdownReason.UserCancelled);
                await Task.WhenAny(runnerTask, Task.Delay(2000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.True(runnerTask.IsCanceled);
                _messageListener.Verify(x => x.AcknowledgeMessageAsync("999", It.IsAny<CancellationToken>()), Times.Once());
                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), false), Times.Once());
                _runServer.Verify(x => x.GetJobMessageAsync("999", "github", It.IsAny<CancellationToken>()), Times.Once());
                _credentialManager.Verify(x => x.LoadCredentials(true), Times.Once());
                _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerJobRequestMessageFromRunService_AuthMigrationFallback()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ISelfUpdater>(_updater.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);
                hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);
                hc.EnqueueInstance<IRunServer>(_runServer.Object);
                hc.EnqueueInstance<IRunServer>(_runServer.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = true,
                    ServerUrl = "https://github.com",
                };

                var message1 = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(new RunnerJobRequestRef() { BillingOwnerId = "github", RunnerRequestId = "999", RunServiceUrl = "https://run-service.com" }),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.RunnerJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message1);
                messages.Enqueue(message1);
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(true));
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Success));
                _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken token) =>
                        {
                            if (2 == messages.Count)
                            {
                                hc.EnableAuthMigration("L0Test");
                            }

                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000, token);
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
                    .Returns(Task.CompletedTask);
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                var throwError = true;
                _runServer.Setup(x => x.GetJobMessageAsync("999", "github", It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        if (throwError)
                        {
                            Assert.True(hc.AllowAuthMigration);
                            throwError = false;
                            throw new NotSupportedException("some error");
                        }

                        return Task.FromResult(CreateJobRequestMessage("test"));
                    });

                _credentialManager.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                var completedTask = new TaskCompletionSource<TaskResult>();
                completedTask.SetResult(TaskResult.Succeeded);
                _jobDispatcher.Setup(x => x.RunOnceJobCompleted).Returns(completedTask);

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                Task<int> runnerTask = runner.ExecuteCommand(command);

                //Assert
                //wait for the runner to exit with right return code
                await Task.WhenAny(runnerTask, Task.Delay(30000));

                Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
                Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
                if (runnerTask.IsCompleted)
                {
                    Assert.Equal(Constants.Runner.ReturnCode.Success, await runnerTask);
                }

                _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once());
                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
                _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
                _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.AtLeast(2));
                _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                _credentialManager.Verify(x => x.LoadCredentials(true), Times.AtLeast(2));

                Assert.False(hc.AllowAuthMigration);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task TestRunnerEnableAuthMigrationByDefault()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var runner = new Runner.Listener.Runner();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IJobNotification>(_jobNotification.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IPromptManager>(_promptManager.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<ICredentialManager>(_credentialManager.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.EnqueueInstance<IErrorThrottler>(_acquireJobThrottler.Object);

                runner.Initialize(hc);
                var settings = new RunnerSettings
                {
                    PoolId = 43242,
                    AgentId = 5678,
                    Ephemeral = true,
                    ServerUrl = "https://github.com",
                };

                var message1 = new TaskAgentMessage()
                {
                    Body = JsonUtility.ToString(new RunnerJobRequestRef() { BillingOwnerId = "github", RunnerRequestId = "999", RunServiceUrl = "https://run-service.com" }),
                    MessageId = 4234,
                    MessageType = JobRequestMessageTypes.RunnerJobRequest
                };

                var messages = new Queue<TaskAgentMessage>();
                messages.Enqueue(message1);
                messages.Enqueue(message1);
                _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(true));
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<CreateSessionResult>(CreateSessionResult.Failure));
                _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
                    .Callback(() =>
                    {

                    });

                var throwError = true;
                _runServer.Setup(x => x.GetJobMessageAsync("999", "github", It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        if (throwError)
                        {
                            Assert.True(hc.AllowAuthMigration);
                            throwError = false;
                            throw new NotSupportedException("some error");
                        }

                        return Task.FromResult(CreateJobRequestMessage("test"));
                    });

                _credentialManager.Setup(x => x.LoadCredentials(true)).Returns(new VssCredentials());

                _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);

                var credData = new CredentialData()
                {
                    Scheme = Constants.Configuration.OAuth,
                };
                credData.Data["ClientId"] = "testClientId";
                credData.Data["AuthUrl"] = "https://github.com";
                credData.Data["EnableAuthMigrationByDefault"] = "true";
                _configStore.Setup(x => x.GetCredentials()).Returns(credData);

                Assert.False(hc.AllowAuthMigration);

                //Act
                var command = new CommandSettings(hc, new string[] { "run" });
                var returnCode = await runner.ExecuteCommand(command);

                //Assert
                Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, returnCode);

                _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());

                Assert.True(hc.AllowAuthMigration);
            }
        }
    }
}
