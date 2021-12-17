using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Sdk;
using GitHub.Runner.Listener.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.Services.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common.Util;
using System.IO;
using System.Net.Http;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class SelfUpdaterL0
    {
        private Mock<IRunnerServer> _runnerServer;
        private Mock<ITerminal> _term;
        private Mock<IConfigurationStore> _configStore;
        private Mock<IJobDispatcher> _jobDispatcher;
        private AgentRefreshMessage _refreshMessage = new AgentRefreshMessage(1, "2.299.0");

        public SelfUpdaterL0()
        {
            _runnerServer = new Mock<IRunnerServer>();
            _term = new Mock<ITerminal>();
            _configStore = new Mock<IConfigurationStore>();
            _jobDispatcher = new Mock<IJobDispatcher>();
            _configStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1, AgentId = 1 });

            _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.299.0", true, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.299.0"), DownloadUrl = $"https://github.com/actions/runner/releases/download/v2.285.1/actions-runner-{BuildConstants.RunnerPackage.PackageName}-2.285.1.tar.gz" }));

            // Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_DOWNLOAD_TIMEOUT", "1");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var updater = new Runner.Listener.SelfUpdater();
                hc.SetSingleton<ITerminal>(_term.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                var p = new ProcessInvokerWrapper();
                p.Initialize(hc);
                hc.EnqueueInstance<IProcessInvoker>(p);
                updater.Initialize(hc);

                _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                             .Callback((int p, int a, string s, string t) =>
                             {
                                 hc.GetTrace().Info(t);
                             })
                             .Returns(Task.FromResult(new TaskAgent()));

                try
                {
                    var result = await updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken);
                    Assert.True(result);
                    Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.299.0")));
                    Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.299.0")));
                }
                finally
                {
                    IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.299.0"), CancellationToken.None);
                    IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.299.0"), CancellationToken.None);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_NoUpdateOnOldVersion()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var updater = new Runner.Listener.SelfUpdater();
                hc.SetSingleton<ITerminal>(_term.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                updater.Initialize(hc);

                _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.200.0", true, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.200.0"), DownloadUrl = $"https://github.com/actions/runner/releases/download/v2.285.1/actions-runner-{BuildConstants.RunnerPackage.PackageName}-2.285.1.tar.gz" }));

                _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                             .Callback((int p, int a, string s, string t) =>
                             {
                                 hc.GetTrace().Info(t);
                             })
                             .Returns(Task.FromResult(new TaskAgent()));

                var result = await updater.SelfUpdate(new AgentRefreshMessage(1, "2.200.0"), _jobDispatcher.Object, true, hc.RunnerShutdownToken);
                Assert.False(result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_DownloadRetry()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var updater = new Runner.Listener.SelfUpdater();
                hc.SetSingleton<ITerminal>(_term.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.299.0", true, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.299.0"), DownloadUrl = $"https://github.com/actions/runner/notexists" }));

                var p = new ProcessInvokerWrapper();
                p.Initialize(hc);
                hc.EnqueueInstance<IProcessInvoker>(p);
                updater.Initialize(hc);

                _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                             .Callback((int p, int a, string s, string t) =>
                             {
                                 hc.GetTrace().Info(t);
                             })
                             .Returns(Task.FromResult(new TaskAgent()));


                var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken));
                Assert.Contains($"failed after {Constants.RunnerDownloadRetryMaxAttempts} download attempts", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_ValidateHash()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var updater = new Runner.Listener.SelfUpdater();
                hc.SetSingleton<ITerminal>(_term.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.299.0", true, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.299.0"), DownloadUrl = $"https://github.com/actions/runner/releases/download/v2.285.1/actions-runner-{BuildConstants.RunnerPackage.PackageName}-2.285.1.tar.gz", HashValue = "bad_hash" }));

                var p = new ProcessInvokerWrapper();
                p.Initialize(hc);
                hc.EnqueueInstance<IProcessInvoker>(p);
                updater.Initialize(hc);

                _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                             .Callback((int p, int a, string s, string t) =>
                             {
                                 hc.GetTrace().Info(t);
                             })
                             .Returns(Task.FromResult(new TaskAgent()));


                var ex = await Assert.ThrowsAsync<Exception>(() => updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken));
                Assert.Contains("did not match expected Runner Hash", ex.Message);
            }
        }
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Runner")]
        // //process 2 new job messages, and one cancel message
        // public async void TestRunAsync()
        // {
        //     using (var hc = new TestHostContext(this))
        //     {
        //         //Arrange
        //         var updater = new Runner.Listener.SelfUpdater();
        //         hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
        //         hc.SetSingleton<IJobNotification>(_jobNotification.Object);
        //         hc.SetSingleton<IMessageListener>(_messageListener.Object);
        //         hc.SetSingleton<IPromptManager>(_promptManager.Object);
        //         hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
        //         hc.SetSingleton<IConfigurationStore>(_configStore.Object);
        //         updater.Initialize(hc);
        //         var settings = new RunnerSettings
        //         {
        //             PoolId = 43242
        //         };

        //         var message = new TaskAgentMessage()
        //         {
        //             Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
        //             MessageId = 4234,
        //             MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
        //         };

        //         var messages = new Queue<TaskAgentMessage>();
        //         messages.Enqueue(message);
        //         var signalWorkerComplete = new SemaphoreSlim(0, 1);
        //         _configurationManager.Setup(x => x.LoadSettings())
        //             .Returns(settings);
        //         _configurationManager.Setup(x => x.IsConfigured())
        //             .Returns(true);
        //         _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
        //             .Returns(Task.FromResult<bool>(true));
        //         _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
        //             .Returns(async () =>
        //                 {
        //                     if (0 == messages.Count)
        //                     {
        //                         signalWorkerComplete.Release();
        //                         await Task.Delay(2000, hc.RunnerShutdownToken);
        //                     }

        //                     return messages.Dequeue();
        //                 });
        //         _messageListener.Setup(x => x.DeleteSessionAsync())
        //             .Returns(Task.CompletedTask);
        //         _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
        //             .Returns(Task.CompletedTask);
        //         _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
        //             .Callback(() =>
        //             {

        //             });
        //         _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
        //             .Callback(() =>
        //             {

        //             });

        //         hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

        //         _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
        //         //Act
        //         var command = new CommandSettings(hc, new string[] { "run" });
        //         Task runnerTask = runner.ExecuteCommand(command);

        //         //Assert
        //         //wait for the runner to run one job
        //         if (!await signalWorkerComplete.WaitAsync(2000))
        //         {
        //             Assert.True(false, $"{nameof(_messageListener.Object.GetNextMessageAsync)} was not invoked.");
        //         }
        //         else
        //         {
        //             //Act
        //             hc.ShutdownRunner(ShutdownReason.UserCancelled); //stop Runner

        //             //Assert
        //             Task[] taskToWait2 = { runnerTask, Task.Delay(2000) };
        //             //wait for the runner to exit
        //             await Task.WhenAny(taskToWait2);

        //             Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
        //             Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
        //             Assert.True(runnerTask.IsCanceled);

        //             _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()), Times.Once(),
        //                  $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
        //             _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        //             _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
        //             _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
        //             _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.AtLeastOnce());

        //             // verify that we didn't try to delete local settings file (since we're not ephemeral)
        //             _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Never());
        //         }
        //     }
        // }

        // public static TheoryData<string[], bool, Times> RunAsServiceTestData = new TheoryData<string[], bool, Times>()
        //                                                             {
        //                                                                 // staring with run command, configured as run as service, should start the runner
        //                                                                 { new [] { "run" }, true, Times.Once() },
        //                                                                 // starting with no argument, configured not to run as service, should start runner interactively
        //                                                                 { new [] { "run" }, false, Times.Once() }
        //                                                             };
        // [Theory]
        // [MemberData(nameof(RunAsServiceTestData))]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Runner")]
        // public async void TestExecuteCommandForRunAsService(string[] args, bool configureAsService, Times expectedTimes)
        // {
        //     using (var hc = new TestHostContext(this))
        //     {
        //         hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
        //         hc.SetSingleton<IPromptManager>(_promptManager.Object);
        //         hc.SetSingleton<IMessageListener>(_messageListener.Object);
        //         hc.SetSingleton<IConfigurationStore>(_configStore.Object);

        //         var command = new CommandSettings(hc, args);

        //         _configurationManager.Setup(x => x.IsConfigured()).Returns(true);
        //         _configurationManager.Setup(x => x.LoadSettings())
        //             .Returns(new RunnerSettings { });

        //         _configStore.Setup(x => x.IsServiceConfigured()).Returns(configureAsService);

        //         _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
        //             .Returns(Task.FromResult(false));

        //         var runner = new Runner.Listener.Runner();
        //         runner.Initialize(hc);
        //         await runner.ExecuteCommand(command);

        //         _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), expectedTimes);
        //     }
        // }

        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Runner")]
        // public async void TestMachineProvisionerCLI()
        // {
        //     using (var hc = new TestHostContext(this))
        //     {
        //         hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
        //         hc.SetSingleton<IPromptManager>(_promptManager.Object);
        //         hc.SetSingleton<IMessageListener>(_messageListener.Object);
        //         hc.SetSingleton<IConfigurationStore>(_configStore.Object);

        //         var command = new CommandSettings(hc, new[] { "run" });

        //         _configurationManager.Setup(x => x.IsConfigured()).
        //             Returns(true);
        //         _configurationManager.Setup(x => x.LoadSettings())
        //             .Returns(new RunnerSettings { });

        //         _configStore.Setup(x => x.IsServiceConfigured())
        //             .Returns(false);

        //         _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
        //             .Returns(Task.FromResult(false));

        //         var runner = new Runner.Listener.Runner();
        //         runner.Initialize(hc);
        //         await runner.ExecuteCommand(command);

        //         _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
        //     }
        // }

        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Runner")]
        // public async void TestRunOnce()
        // {
        //     using (var hc = new TestHostContext(this))
        //     {
        //         //Arrange
        //         var runner = new Runner.Listener.Runner();
        //         hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
        //         hc.SetSingleton<IJobNotification>(_jobNotification.Object);
        //         hc.SetSingleton<IMessageListener>(_messageListener.Object);
        //         hc.SetSingleton<IPromptManager>(_promptManager.Object);
        //         hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
        //         hc.SetSingleton<IConfigurationStore>(_configStore.Object);
        //         runner.Initialize(hc);
        //         var settings = new RunnerSettings
        //         {
        //             PoolId = 43242,
        //             Ephemeral = true
        //         };

        //         var message = new TaskAgentMessage()
        //         {
        //             Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
        //             MessageId = 4234,
        //             MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
        //         };

        //         var messages = new Queue<TaskAgentMessage>();
        //         messages.Enqueue(message);
        //         _configurationManager.Setup(x => x.LoadSettings())
        //             .Returns(settings);
        //         _configurationManager.Setup(x => x.IsConfigured())
        //             .Returns(true);
        //         _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
        //             .Returns(Task.FromResult<bool>(true));
        //         _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
        //             .Returns(async () =>
        //                 {
        //                     if (0 == messages.Count)
        //                     {
        //                         await Task.Delay(2000);
        //                     }

        //                     return messages.Dequeue();
        //                 });
        //         _messageListener.Setup(x => x.DeleteSessionAsync())
        //             .Returns(Task.CompletedTask);
        //         _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
        //             .Returns(Task.CompletedTask);

        //         var runOnceJobCompleted = new TaskCompletionSource<bool>();
        //         _jobDispatcher.Setup(x => x.RunOnceJobCompleted)
        //             .Returns(runOnceJobCompleted);
        //         _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
        //             .Callback(() =>
        //             {
        //                 runOnceJobCompleted.TrySetResult(true);
        //             });
        //         _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
        //             .Callback(() =>
        //             {

        //             });

        //         hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

        //         _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
        //         //Act
        //         var command = new CommandSettings(hc, new string[] { "run" });
        //         Task<int> runnerTask = runner.ExecuteCommand(command);

        //         //Assert
        //         //wait for the runner to run one job and exit
        //         await Task.WhenAny(runnerTask, Task.Delay(30000));

        //         Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
        //         Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
        //         Assert.True(runnerTask.Result == Constants.Runner.ReturnCode.Success);

        //         _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once(),
        //              $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
        //         _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        //         _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
        //         _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
        //         _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.AtLeastOnce());

        //         // verify that we did try to delete local settings file (since we're ephemeral)
        //         _configurationManager.Verify(x => x.DeleteLocalRunnerConfig(), Times.Once());
        //     }
        // }

        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Runner")]
        // public async void TestRunOnceOnlyTakeOneJobMessage()
        // {
        //     using (var hc = new TestHostContext(this))
        //     {
        //         //Arrange
        //         var runner = new Runner.Listener.Runner();
        //         hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
        //         hc.SetSingleton<IJobNotification>(_jobNotification.Object);
        //         hc.SetSingleton<IMessageListener>(_messageListener.Object);
        //         hc.SetSingleton<IPromptManager>(_promptManager.Object);
        //         hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
        //         hc.SetSingleton<IConfigurationStore>(_configStore.Object);
        //         runner.Initialize(hc);
        //         var settings = new RunnerSettings
        //         {
        //             PoolId = 43242,
        //             Ephemeral = true
        //         };

        //         var message1 = new TaskAgentMessage()
        //         {
        //             Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
        //             MessageId = 4234,
        //             MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
        //         };
        //         var message2 = new TaskAgentMessage()
        //         {
        //             Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
        //             MessageId = 4235,
        //             MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
        //         };

        //         var messages = new Queue<TaskAgentMessage>();
        //         messages.Enqueue(message1);
        //         messages.Enqueue(message2);
        //         _configurationManager.Setup(x => x.LoadSettings())
        //             .Returns(settings);
        //         _configurationManager.Setup(x => x.IsConfigured())
        //             .Returns(true);
        //         _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
        //             .Returns(Task.FromResult<bool>(true));
        //         _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
        //             .Returns(async () =>
        //                 {
        //                     if (0 == messages.Count)
        //                     {
        //                         await Task.Delay(2000);
        //                     }

        //                     return messages.Dequeue();
        //                 });
        //         _messageListener.Setup(x => x.DeleteSessionAsync())
        //             .Returns(Task.CompletedTask);
        //         _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
        //             .Returns(Task.CompletedTask);

        //         var runOnceJobCompleted = new TaskCompletionSource<bool>();
        //         _jobDispatcher.Setup(x => x.RunOnceJobCompleted)
        //             .Returns(runOnceJobCompleted);
        //         _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
        //             .Callback(() =>
        //             {
        //                 runOnceJobCompleted.TrySetResult(true);
        //             });
        //         _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
        //             .Callback(() =>
        //             {

        //             });

        //         hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

        //         _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
        //         //Act
        //         var command = new CommandSettings(hc, new string[] { "run" });
        //         Task<int> runnerTask = runner.ExecuteCommand(command);

        //         //Assert
        //         //wait for the runner to run one job and exit
        //         await Task.WhenAny(runnerTask, Task.Delay(30000));

        //         Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
        //         Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
        //         Assert.True(runnerTask.Result == Constants.Runner.ReturnCode.Success);

        //         _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once(),
        //              $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
        //         _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        //         _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
        //         _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
        //         _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
        //     }
        // }

        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Runner")]
        // public async void TestRunOnceHandleUpdateMessage()
        // {
        //     using (var hc = new TestHostContext(this))
        //     {
        //         //Arrange
        //         var runner = new Runner.Listener.Runner();
        //         hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
        //         hc.SetSingleton<IJobNotification>(_jobNotification.Object);
        //         hc.SetSingleton<IMessageListener>(_messageListener.Object);
        //         hc.SetSingleton<IPromptManager>(_promptManager.Object);
        //         hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
        //         hc.SetSingleton<IConfigurationStore>(_configStore.Object);
        //         hc.SetSingleton<ISelfUpdater>(_updater.Object);

        //         runner.Initialize(hc);
        //         var settings = new RunnerSettings
        //         {
        //             PoolId = 43242,
        //             AgentId = 5678,
        //             Ephemeral = true
        //         };

        //         var message1 = new TaskAgentMessage()
        //         {
        //             Body = JsonUtility.ToString(new AgentRefreshMessage(settings.AgentId, "2.123.0")),
        //             MessageId = 4234,
        //             MessageType = AgentRefreshMessage.MessageType
        //         };

        //         var messages = new Queue<TaskAgentMessage>();
        //         messages.Enqueue(message1);
        //         _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
        //                 .Returns(Task.FromResult(true));
        //         _configurationManager.Setup(x => x.LoadSettings())
        //             .Returns(settings);
        //         _configurationManager.Setup(x => x.IsConfigured())
        //             .Returns(true);
        //         _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
        //             .Returns(Task.FromResult<bool>(true));
        //         _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
        //             .Returns(async () =>
        //                 {
        //                     if (0 == messages.Count)
        //                     {
        //                         await Task.Delay(2000);
        //                     }

        //                     return messages.Dequeue();
        //                 });
        //         _messageListener.Setup(x => x.DeleteSessionAsync())
        //             .Returns(Task.CompletedTask);
        //         _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
        //             .Returns(Task.CompletedTask);
        //         _jobNotification.Setup(x => x.StartClient(It.IsAny<String>()))
        //             .Callback(() =>
        //             {

        //             });

        //         hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

        //         _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
        //         //Act
        //         var command = new CommandSettings(hc, new string[] { "run" });
        //         Task<int> runnerTask = runner.ExecuteCommand(command);

        //         //Assert
        //         //wait for the runner to exit with right return code
        //         await Task.WhenAny(runnerTask, Task.Delay(30000));

        //         Assert.True(runnerTask.IsCompleted, $"{nameof(runner.ExecuteCommand)} timed out.");
        //         Assert.True(!runnerTask.IsFaulted, runnerTask.Exception?.ToString());
        //         Assert.True(runnerTask.Result == Constants.Runner.ReturnCode.RunOnceRunnerUpdating);

        //         _updater.Verify(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), false, It.IsAny<CancellationToken>()), Times.Once);
        //         _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Never());
        //         _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        //         _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
        //         _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
        //         _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
        //     }
        // }
    }
}
