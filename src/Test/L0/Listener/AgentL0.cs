﻿// using GitHub.DistributedTask.WebApi;
// using GitHub.Runner.Listener;
// using GitHub.Runner.Listener.Configuration;
// using Moq;
// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Xunit;
// using GitHub.Services.WebApi;
// using Pipelines = GitHub.DistributedTask.Pipelines;
// using GitHub.Runner.Common.Util;

// namespace GitHub.Runner.Common.Tests.Listener
// {
//     public sealed class AgentL0
//     {
//         private Mock<IConfigurationManager> _configurationManager;
//         private Mock<IJobNotification> _jobNotification;
//         private Mock<IMessageListener> _messageListener;
//         private Mock<IPromptManager> _promptManager;
//         private Mock<IJobDispatcher> _jobDispatcher;
//         private Mock<IRunnerServer> _agentServer;
//         private Mock<ITerminal> _term;
//         private Mock<IConfigurationStore> _configStore;
//         private Mock<IRunnerWebProxy> _proxy;
//         private Mock<IRunnerCertificateManager> _cert;
//         private Mock<ISelfUpdater> _updater;

//         public AgentL0()
//         {
//             _configurationManager = new Mock<IConfigurationManager>();
//             _jobNotification = new Mock<IJobNotification>();
//             _messageListener = new Mock<IMessageListener>();
//             _promptManager = new Mock<IPromptManager>();
//             _jobDispatcher = new Mock<IJobDispatcher>();
//             _agentServer = new Mock<IRunnerServer>();
//             _term = new Mock<ITerminal>();
//             _configStore = new Mock<IConfigurationStore>();
//             _proxy = new Mock<IRunnerWebProxy>();
//             _cert = new Mock<IRunnerCertificateManager>();
//             _updater = new Mock<ISelfUpdater>();
//         }

//         private AgentJobRequestMessage CreateJobRequestMessage(string jobName)
//         {
//             TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
//             TimelineReference timeline = null;
//             JobEnvironment environment = new JobEnvironment();
//             List<TaskInstance> tasks = new List<TaskInstance>();
//             Guid JobId = Guid.NewGuid();
//             var jobRequest = new AgentJobRequestMessage(plan, timeline, JobId, jobName, jobName, environment, tasks);
//             return jobRequest as AgentJobRequestMessage;
//         }

//         private JobCancelMessage CreateJobCancelMessage()
//         {
//             var message = new JobCancelMessage(Guid.NewGuid(), TimeSpan.FromSeconds(0));
//             return message;
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Agent")]
//         //process 2 new job messages, and one cancel message
//         public async void TestRunAsync()
//         {
//             using (var hc = new TestHostContext(this))
//             {
//                 //Arrange
//                 var agent = new Runner.Listener.Runner();
//                 hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
//                 hc.SetSingleton<IJobNotification>(_jobNotification.Object);
//                 hc.SetSingleton<IMessageListener>(_messageListener.Object);
//                 hc.SetSingleton<IPromptManager>(_promptManager.Object);
//                 hc.SetSingleton<IRunnerServer>(_agentServer.Object);
//                 hc.SetSingleton<IRunnerWebProxy>(_proxy.Object);
//                 hc.SetSingleton<IRunnerCertificateManager>(_cert.Object);
//                 hc.SetSingleton<IConfigurationStore>(_configStore.Object);
//                 agent.Initialize(hc);
//                 var settings = new RunnerSettings
//                 {
//                     PoolId = 43242
//                 };

//                 var message = new TaskAgentMessage()
//                 {
//                     Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
//                     MessageId = 4234,
//                     MessageType = JobRequestMessageTypes.AgentJobRequest
//                 };

//                 var messages = new Queue<TaskAgentMessage>();
//                 messages.Enqueue(message);
//                 var signalWorkerComplete = new SemaphoreSlim(0, 1);
//                 _configurationManager.Setup(x => x.LoadSettings())
//                     .Returns(settings);
//                 _configurationManager.Setup(x => x.IsConfigured())
//                     .Returns(true);
//                 _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
//                     .Returns(Task.FromResult<bool>(true));
//                 _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
//                     .Returns(async () =>
//                         {
//                             if (0 == messages.Count)
//                             {
//                                 signalWorkerComplete.Release();
//                                 await Task.Delay(2000, hc.RunnerShutdownToken);
//                             }

//                             return messages.Dequeue();
//                         });
//                 _messageListener.Setup(x => x.DeleteSessionAsync())
//                     .Returns(Task.CompletedTask);
//                 _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
//                     .Returns(Task.CompletedTask);
//                 _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
//                     .Callback(() =>
//                     {

//                     });
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
//                     .Callback(() =>
//                     {

//                     });
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>()))
//                     .Callback(() =>
//                     {

//                     });

//                 hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

//                 _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
//                 //Act
//                 var command = new CommandSettings(hc, new string[] { "run" });
//                 Task agentTask = agent.ExecuteCommand(command);

//                 //Assert
//                 //wait for the agent to run one job
//                 if (!await signalWorkerComplete.WaitAsync(2000))
//                 {
//                     Assert.True(false, $"{nameof(_messageListener.Object.GetNextMessageAsync)} was not invoked.");
//                 }
//                 else
//                 {
//                     //Act
//                     hc.ShutdownRunner(ShutdownReason.UserCancelled); //stop Agent

//                     //Assert
//                     Task[] taskToWait2 = { agentTask, Task.Delay(2000) };
//                     //wait for the Agent to exit
//                     await Task.WhenAny(taskToWait2);

//                     Assert.True(agentTask.IsCompleted, $"{nameof(agent.ExecuteCommand)} timed out.");
//                     Assert.True(!agentTask.IsFaulted, agentTask.Exception?.ToString());
//                     Assert.True(agentTask.IsCanceled);

//                     _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()), Times.Once(),
//                          $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
//                     _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
//                     _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
//                     _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
//                     _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.AtLeastOnce());
//                 }
//             }
//         }

//         public static TheoryData<string[], bool, Times> RunAsServiceTestData = new TheoryData<string[], bool, Times>()
//                                                                     {
//                                                                         // staring with run command, configured as run as service, should start the agent
//                                                                         { new [] { "run" }, true, Times.Once() },
//                                                                         // starting with no argument, configured not to run as service, should start agent interactively
//                                                                         { new [] { "run" }, false, Times.Once() }
//                                                                     };
//         [Theory]
//         [MemberData("RunAsServiceTestData")]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Agent")]
//         public async void TestExecuteCommandForRunAsService(string[] args, bool configureAsService, Times expectedTimes)
//         {
//             using (var hc = new TestHostContext(this))
//             {
//                 hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
//                 hc.SetSingleton<IPromptManager>(_promptManager.Object);
//                 hc.SetSingleton<IMessageListener>(_messageListener.Object);
//                 hc.SetSingleton<IRunnerWebProxy>(_proxy.Object);
//                 hc.SetSingleton<IRunnerCertificateManager>(_cert.Object);
//                 hc.SetSingleton<IConfigurationStore>(_configStore.Object);

//                 var command = new CommandSettings(hc, args);

//                 _configurationManager.Setup(x => x.IsConfigured()).Returns(true);
//                 _configurationManager.Setup(x => x.LoadSettings())
//                     .Returns(new RunnerSettings { });

//                 _configStore.Setup(x => x.IsServiceConfigured()).Returns(configureAsService);

//                 _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
//                     .Returns(Task.FromResult(false));

//                 var agent = new Runner.Listener.Runner();
//                 agent.Initialize(hc);
//                 await agent.ExecuteCommand(command);

//                 _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), expectedTimes);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Agent")]
//         //process 2 new job messages, and one cancel message
//         public async void TestMachineProvisionerCLI()
//         {
//             using (var hc = new TestHostContext(this))
//             {
//                 hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
//                 hc.SetSingleton<IPromptManager>(_promptManager.Object);
//                 hc.SetSingleton<IMessageListener>(_messageListener.Object);
//                 hc.SetSingleton<IRunnerWebProxy>(_proxy.Object);
//                 hc.SetSingleton<IRunnerCertificateManager>(_cert.Object);
//                 hc.SetSingleton<IConfigurationStore>(_configStore.Object);

//                 var command = new CommandSettings(hc, new[] { "run" });

//                 _configurationManager.Setup(x => x.IsConfigured()).
//                     Returns(true);
//                 _configurationManager.Setup(x => x.LoadSettings())
//                     .Returns(new RunnerSettings { });

//                 _configStore.Setup(x => x.IsServiceConfigured())
//                     .Returns(false);

//                 _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
//                     .Returns(Task.FromResult(false));

//                 var agent = new Runner.Listener.Runner();
//                 agent.Initialize(hc);
//                 await agent.ExecuteCommand(command);

//                 _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Agent")]
//         //process 2 new job messages, and one cancel message
//         public async void TestMachineProvisionerCLICompat()
//         {
//             using (var hc = new TestHostContext(this))
//             {
//                 hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
//                 hc.SetSingleton<IPromptManager>(_promptManager.Object);
//                 hc.SetSingleton<IMessageListener>(_messageListener.Object);
//                 hc.SetSingleton<IRunnerWebProxy>(_proxy.Object);
//                 hc.SetSingleton<IRunnerCertificateManager>(_cert.Object);
//                 hc.SetSingleton<IConfigurationStore>(_configStore.Object);

//                 var command = new CommandSettings(hc, new string[] { });

//                 _configurationManager.Setup(x => x.IsConfigured()).
//                     Returns(true);
//                 _configurationManager.Setup(x => x.LoadSettings())
//                     .Returns(new RunnerSettings { });

//                 _configStore.Setup(x => x.IsServiceConfigured())
//                     .Returns(false);

//                 _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
//                     .Returns(Task.FromResult(false));

//                 var agent = new Runner.Listener.Runner();
//                 agent.Initialize(hc);
//                 await agent.ExecuteCommand(command);

//                 _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Agent")]
//         public async void TestRunOnce()
//         {
//             using (var hc = new TestHostContext(this))
//             {
//                 //Arrange
//                 var agent = new Runner.Listener.Runner();
//                 hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
//                 hc.SetSingleton<IJobNotification>(_jobNotification.Object);
//                 hc.SetSingleton<IMessageListener>(_messageListener.Object);
//                 hc.SetSingleton<IPromptManager>(_promptManager.Object);
//                 hc.SetSingleton<IRunnerServer>(_agentServer.Object);
//                 hc.SetSingleton<IRunnerWebProxy>(_proxy.Object);
//                 hc.SetSingleton<IRunnerCertificateManager>(_cert.Object);
//                 hc.SetSingleton<IConfigurationStore>(_configStore.Object);
//                 agent.Initialize(hc);
//                 var settings = new RunnerSettings
//                 {
//                     PoolId = 43242
//                 };

//                 var message = new TaskAgentMessage()
//                 {
//                     Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
//                     MessageId = 4234,
//                     MessageType = JobRequestMessageTypes.AgentJobRequest
//                 };

//                 var messages = new Queue<TaskAgentMessage>();
//                 messages.Enqueue(message);
//                 _configurationManager.Setup(x => x.LoadSettings())
//                     .Returns(settings);
//                 _configurationManager.Setup(x => x.IsConfigured())
//                     .Returns(true);
//                 _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
//                     .Returns(Task.FromResult<bool>(true));
//                 _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
//                     .Returns(async () =>
//                         {
//                             if (0 == messages.Count)
//                             {
//                                 await Task.Delay(2000);
//                             }

//                             return messages.Dequeue();
//                         });
//                 _messageListener.Setup(x => x.DeleteSessionAsync())
//                     .Returns(Task.CompletedTask);
//                 _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
//                     .Returns(Task.CompletedTask);

//                 var runOnceJobCompleted = new TaskCompletionSource<bool>();
//                 _jobDispatcher.Setup(x => x.RunOnceJobCompleted)
//                     .Returns(runOnceJobCompleted);
//                 _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
//                     .Callback(() =>
//                     {
//                         runOnceJobCompleted.TrySetResult(true);
//                     });
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
//                     .Callback(() =>
//                     {

//                     });
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>()))
//                     .Callback(() =>
//                     {

//                     });

//                 hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

//                 _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
//                 //Act
//                 var command = new CommandSettings(hc, new string[] { "run", "--once" });
//                 Task<int> agentTask = agent.ExecuteCommand(command);

//                 //Assert
//                 //wait for the agent to run one job and exit
//                 await Task.WhenAny(agentTask, Task.Delay(30000));

//                 Assert.True(agentTask.IsCompleted, $"{nameof(agent.ExecuteCommand)} timed out.");
//                 Assert.True(!agentTask.IsFaulted, agentTask.Exception?.ToString());
//                 Assert.True(agentTask.Result == Constants.Runner.ReturnCode.Success);

//                 _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once(),
//                      $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
//                 _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
//                 _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
//                 _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
//                 _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.AtLeastOnce());
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Agent")]
//         public async void TestRunOnceOnlyTakeOneJobMessage()
//         {
//             using (var hc = new TestHostContext(this))
//             {
//                 //Arrange
//                 var agent = new Runner.Listener.Runner();
//                 hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
//                 hc.SetSingleton<IJobNotification>(_jobNotification.Object);
//                 hc.SetSingleton<IMessageListener>(_messageListener.Object);
//                 hc.SetSingleton<IPromptManager>(_promptManager.Object);
//                 hc.SetSingleton<IRunnerServer>(_agentServer.Object);
//                 hc.SetSingleton<IRunnerWebProxy>(_proxy.Object);
//                 hc.SetSingleton<IRunnerCertificateManager>(_cert.Object);
//                 hc.SetSingleton<IConfigurationStore>(_configStore.Object);
//                 agent.Initialize(hc);
//                 var settings = new RunnerSettings
//                 {
//                     PoolId = 43242
//                 };

//                 var message1 = new TaskAgentMessage()
//                 {
//                     Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
//                     MessageId = 4234,
//                     MessageType = JobRequestMessageTypes.AgentJobRequest
//                 };
//                 var message2 = new TaskAgentMessage()
//                 {
//                     Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
//                     MessageId = 4235,
//                     MessageType = JobRequestMessageTypes.AgentJobRequest
//                 };

//                 var messages = new Queue<TaskAgentMessage>();
//                 messages.Enqueue(message1);
//                 messages.Enqueue(message2);
//                 _configurationManager.Setup(x => x.LoadSettings())
//                     .Returns(settings);
//                 _configurationManager.Setup(x => x.IsConfigured())
//                     .Returns(true);
//                 _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
//                     .Returns(Task.FromResult<bool>(true));
//                 _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
//                     .Returns(async () =>
//                         {
//                             if (0 == messages.Count)
//                             {
//                                 await Task.Delay(2000);
//                             }

//                             return messages.Dequeue();
//                         });
//                 _messageListener.Setup(x => x.DeleteSessionAsync())
//                     .Returns(Task.CompletedTask);
//                 _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
//                     .Returns(Task.CompletedTask);

//                 var runOnceJobCompleted = new TaskCompletionSource<bool>();
//                 _jobDispatcher.Setup(x => x.RunOnceJobCompleted)
//                     .Returns(runOnceJobCompleted);
//                 _jobDispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<bool>()))
//                     .Callback(() =>
//                     {
//                         runOnceJobCompleted.TrySetResult(true);
//                     });
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
//                     .Callback(() =>
//                     {

//                     });
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>()))
//                     .Callback(() =>
//                     {

//                     });

//                 hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

//                 _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
//                 //Act
//                 var command = new CommandSettings(hc, new string[] { "run", "--once" });
//                 Task<int> agentTask = agent.ExecuteCommand(command);

//                 //Assert
//                 //wait for the agent to run one job and exit
//                 await Task.WhenAny(agentTask, Task.Delay(30000));

//                 Assert.True(agentTask.IsCompleted, $"{nameof(agent.ExecuteCommand)} timed out.");
//                 Assert.True(!agentTask.IsFaulted, agentTask.Exception?.ToString());
//                 Assert.True(agentTask.Result == Constants.Runner.ReturnCode.Success);

//                 _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once(),
//                      $"{nameof(_jobDispatcher.Object.Run)} was not invoked.");
//                 _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
//                 _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
//                 _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
//                 _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Agent")]
//         public async void TestRunOnceHandleUpdateMessage()
//         {
//             using (var hc = new TestHostContext(this))
//             {
//                 //Arrange
//                 var agent = new Runner.Listener.Runner();
//                 hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
//                 hc.SetSingleton<IJobNotification>(_jobNotification.Object);
//                 hc.SetSingleton<IMessageListener>(_messageListener.Object);
//                 hc.SetSingleton<IPromptManager>(_promptManager.Object);
//                 hc.SetSingleton<IRunnerServer>(_agentServer.Object);
//                 hc.SetSingleton<IRunnerWebProxy>(_proxy.Object);
//                 hc.SetSingleton<IRunnerCertificateManager>(_cert.Object);
//                 hc.SetSingleton<IConfigurationStore>(_configStore.Object);
//                 hc.SetSingleton<ISelfUpdater>(_updater.Object);

//                 agent.Initialize(hc);
//                 var settings = new RunnerSettings
//                 {
//                     PoolId = 43242,
//                     AgentId = 5678
//                 };

//                 var message1 = new TaskAgentMessage()
//                 {
//                     Body = JsonUtility.ToString(new AgentRefreshMessage(settings.AgentId, "2.123.0")),
//                     MessageId = 4234,
//                     MessageType = AgentRefreshMessage.MessageType
//                 };

//                 var messages = new Queue<TaskAgentMessage>();
//                 messages.Enqueue(message1);
//                 _updater.Setup(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
//                         .Returns(Task.FromResult(true));
//                 _configurationManager.Setup(x => x.LoadSettings())
//                     .Returns(settings);
//                 _configurationManager.Setup(x => x.IsConfigured())
//                     .Returns(true);
//                 _messageListener.Setup(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()))
//                     .Returns(Task.FromResult<bool>(true));
//                 _messageListener.Setup(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()))
//                     .Returns(async () =>
//                         {
//                             if (0 == messages.Count)
//                             {
//                                 await Task.Delay(2000);
//                             }

//                             return messages.Dequeue();
//                         });
//                 _messageListener.Setup(x => x.DeleteSessionAsync())
//                     .Returns(Task.CompletedTask);
//                 _messageListener.Setup(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()))
//                     .Returns(Task.CompletedTask);
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
//                     .Callback(() =>
//                     {

//                     });
//                 _jobNotification.Setup(x => x.StartClient(It.IsAny<String>(), It.IsAny<String>()))
//                     .Callback(() =>
//                     {

//                     });

//                 hc.EnqueueInstance<IJobDispatcher>(_jobDispatcher.Object);

//                 _configStore.Setup(x => x.IsServiceConfigured()).Returns(false);
//                 //Act
//                 var command = new CommandSettings(hc, new string[] { "run", "--once" });
//                 Task<int> agentTask = agent.ExecuteCommand(command);

//                 //Assert
//                 //wait for the agent to exit with right return code
//                 await Task.WhenAny(agentTask, Task.Delay(30000));

//                 Assert.True(agentTask.IsCompleted, $"{nameof(agent.ExecuteCommand)} timed out.");
//                 Assert.True(!agentTask.IsFaulted, agentTask.Exception?.ToString());
//                 Assert.True(agentTask.Result == Constants.Runner.ReturnCode.RunOnceRunnerUpdating);

//                 _updater.Verify(x => x.SelfUpdate(It.IsAny<AgentRefreshMessage>(), It.IsAny<IJobDispatcher>(), false, It.IsAny<CancellationToken>()), Times.Once);
//                 _jobDispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Never());
//                 _messageListener.Verify(x => x.GetNextMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
//                 _messageListener.Verify(x => x.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once());
//                 _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
//                 _messageListener.Verify(x => x.DeleteMessageAsync(It.IsAny<TaskAgentMessage>()), Times.Once());
//             }
//         }
//     }
// }
