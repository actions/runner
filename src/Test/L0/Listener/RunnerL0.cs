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
    public sealed class RunnerL0
    {
        private Mock<IConfigurationManager> _configurationManager;
        private Mock<IJobNotification> _jobNotification;
        private Mock<IMessageListener> _messageListener;
        private Mock<IPromptManager> _promptManager;
        private Mock<IJobDispatcher> _jobDispatcher;
        private Mock<IRunnerServer> _runnerServer;
        private Mock<ITerminal> _term;
        private Mock<IConfigurationStore> _configStore;
        private Mock<ISelfUpdater> _updater;
        private Mock<IErrorThrottler> _acquireJobThrottler;
        private Mock<ICredentialManager> _credentialManager;
        private Mock<IActionsRunServer> _actionsRunServer;
        private Mock<IRunServer> _runServer;

        public RunnerL0()
        {
            _configurationManager = new Mock<IConfigurationManager>();
            _jobNotification = new Mock<IJobNotification>();
            _messageListener = new Mock<IMessageListener>();
            _promptManager = new Mock<IPromptManager>();
            _jobDispatcher = new Mock<IJobDispatcher>();
            _runnerServer = new Mock<IRunnerServer>();
            _term = new Mock<ITerminal>();
            _configStore = new Mock<IConfigurationStore>();
            _updater = new Mock<ISelfUpdater>();
            _acquireJobThrottler = new Mock<IErrorThrottler>();
            _credentialManager = new Mock<ICredentialManager>();
            _actionsRunServer = new Mock<IActionsRunServer>();
            _runServer = new Mock<IRunServer>();
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
