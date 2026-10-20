using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Moq;
using Sdk.RSWebApi.Contracts;
using Xunit;

using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class JobDispatcherL0
    {
        private Mock<IProcessChannel> _processChannel;
        private Mock<IProcessInvoker> _processInvoker;
        private Mock<IRunnerServer> _runnerServer;

        private Mock<IRunServer> _runServer;
        private Mock<IConfigurationStore> _configurationStore;

        public JobDispatcherL0()
        {
            _processChannel = new Mock<IProcessChannel>();
            _processInvoker = new Mock<IProcessInvoker>();
            _runnerServer = new Mock<IRunnerServer>();
            _runServer = new Mock<IRunServer>();
            _configurationStore = new Mock<IConfigurationStore>();
        }

        private Pipelines.AgentJobRequestMessage CreateJobRequestMessage(string billingOwnerId = null)
        {
            TaskOrchestrationPlanReference plan = new();
            TimelineReference timeline = null;
            Guid jobId = Guid.NewGuid();
            var result = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, "someJob", "someJob", null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null, null, null, null, null);
            result.ContextData["github"] = new Pipelines.ContextData.DictionaryContextData();
            result.BillingOwnerId = billingOwnerId;
            return result;
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [InlineData(null)]
        [InlineData("billingOwnerId")]
        public async void DispatchesJobRequest(string billingOwnerId)
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var jobDispatcher = new JobDispatcher();
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                jobDispatcher.Initialize(hc);

                var ts = new CancellationTokenSource();
                Pipelines.AgentJobRequestMessage message = CreateJobRequestMessage(billingOwnerId);
                string strMessage = JsonUtility.ToString(message);

                _processInvoker.Setup(x => x.ExecuteAsync(It.IsAny<String>(), It.IsAny<String>(), "spawnclient 1 2", null, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<int>(56));

                _processChannel.Setup(x => x.StartServer(It.IsAny<StartProcessDelegate>()))
                    .Callback((StartProcessDelegate startDel) => { startDel("1", "2"); });
                _processChannel.Setup(x => x.SendAsync(MessageType.NewJobRequest, It.Is<string>(s => s.Equals(strMessage)), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var request = new TaskAgentJobRequest();
                PropertyInfo sessionIdProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(request));

                _runnerServer.Setup(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TaskResult>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(new TaskAgentJobRequest()));


                //Actt
                jobDispatcher.Run(message);

                //Assert
                await jobDispatcher.WaitAsync(CancellationToken.None);

                Assert.False(jobDispatcher.RunOnceJobCompleted.Task.IsCompleted, "JobDispatcher should not set task complete token for regular agent.");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatcherRenewJobRequest()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                int poolId = 1;
                Int64 requestId = 1000;
                int count = 0;

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequest));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                TaskAgentJobRequest request = new();
                PropertyInfo lockUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (!firstJobRequestRenewed.Task.IsCompletedSuccessfully)
                                {
                                    trace.Info("First renew happens.");
                                }

                                if (count < 5)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request);
                                }
                                else if (count == 5)
                                {
                                    cancellationTokenSource.Cancel();
                                    return Task.FromResult<TaskAgentJobRequest>(request);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Should not reach here.");
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), poolId, requestId, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                Assert.True(firstJobRequestRenewed.Task.IsCompletedSuccessfully);
                _runnerServer.Verify(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatcherRenewJobRequestStopOnJobNotFoundExceptions()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                int poolId = 1;
                Int64 requestId = 1000;
                int count = 0;

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestStopOnJobNotFoundExceptions));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                TaskAgentJobRequest request = new();
                PropertyInfo lockUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (!firstJobRequestRenewed.Task.IsCompletedSuccessfully)
                                {
                                    trace.Info("First renew happens.");
                                }

                                if (count < 5)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request);
                                }
                                else if (count == 5)
                                {
                                    cancellationTokenSource.CancelAfter(10000);
                                    throw new TaskAgentJobNotFoundException("");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Should not reach here.");
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), poolId, requestId, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                Assert.True(firstJobRequestRenewed.Task.IsCompletedSuccessfully, "First renew should succeed.");
                Assert.False(cancellationTokenSource.IsCancellationRequested);
                _runnerServer.Verify(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatcherRenewJobOnRunServiceStopOnJobNotFoundExceptions()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                int poolId = 1;
                Int64 requestId = 1000;
                int count = 0;

                var trace = hc.GetTrace(nameof(DispatcherRenewJobOnRunServiceStopOnJobNotFoundExceptions));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                TaskAgentJobRequest request = new();
                PropertyInfo lockUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                hc.SetSingleton<IRunServer>(_runServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                _ = _runServer.Setup(x => x.RenewJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (!firstJobRequestRenewed.Task.IsCompletedSuccessfully)
                                {
                                    trace.Info("First renew happens.");
                                }

                                if (count < 5)
                                {
                                    var response = new RenewJobResponse()
                                    {
                                        LockedUntil = request.LockedUntil.Value
                                    };
                                    return Task.FromResult<RenewJobResponse>(response);
                                }
                                else if (count == 5)
                                {
                                    cancellationTokenSource.CancelAfter(10000);
                                    throw new TaskOrchestrationJobNotFoundException("");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Should not reach here.");
                                }
                            });


                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);
                EnableRunServiceJobForJobDispatcher(jobDispatcher);

                // Set the value of the _isRunServiceJob field to true
                var isRunServiceJobField = typeof(JobDispatcher).GetField("_isRunServiceJob", BindingFlags.NonPublic | BindingFlags.Instance);
                isRunServiceJobField.SetValue(jobDispatcher, true);

                await jobDispatcher.RenewJobRequestAsync(GetAgentJobRequestMessage(), GetServiceEndpoint(), poolId, requestId, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                Assert.True(firstJobRequestRenewed.Task.IsCompletedSuccessfully, "First renew should succeed.");
                Assert.False(cancellationTokenSource.IsCancellationRequested);
                _runServer.Verify(x => x.RenewJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatcherRenewJobRequestStopOnJobTokenExpiredExceptions()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                int poolId = 1;
                Int64 requestId = 1000;
                int count = 0;

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestStopOnJobTokenExpiredExceptions));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                TaskAgentJobRequest request = new();
                PropertyInfo lockUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (!firstJobRequestRenewed.Task.IsCompletedSuccessfully)
                                {
                                    trace.Info("First renew happens.");
                                }

                                if (count < 5)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request);
                                }
                                else if (count == 5)
                                {
                                    cancellationTokenSource.CancelAfter(10000);
                                    throw new TaskAgentJobTokenExpiredException("");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Should not reach here.");
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), poolId, requestId, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                Assert.True(firstJobRequestRenewed.Task.IsCompletedSuccessfully, "First renew should succeed.");
                Assert.False(cancellationTokenSource.IsCancellationRequested);
                _runnerServer.Verify(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void RenewJobRequestNewAgentNameUpdatesSettings()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var count = 0;
                var oldName = "OldName";
                var newName = "NewName";
                var oldSettings = new RunnerSettings { AgentName = oldName };
                var reservedAgent = new TaskAgentReference { Name = newName };

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestStopOnJobTokenExpiredExceptions));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                var request = new Mock<TaskAgentJobRequest>();
                request.Object.ReservedAgent = reservedAgent;
                PropertyInfo lockUntilProperty = request.Object.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request.Object, DateTime.UtcNow.AddMinutes(5));
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(oldSettings);
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (count < 5)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request.Object);
                                }
                                else if (count == 5 || count == 6 || count == 7)
                                {
                                    throw new TimeoutException("");
                                }
                                else
                                {
                                    cancellationTokenSource.Cancel();
                                    return Task.FromResult<TaskAgentJobRequest>(request.Object);
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);


                // Act
                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), 0, 0, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                // Assert
                _configurationStore.Verify(x => x.SaveSettings(It.Is<RunnerSettings>(settings => settings.AgentName == newName)), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void RenewJobRequestSameAgentNameIgnored()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var count = 0;
                var oldName = "OldName";
                var newName = "OldName";
                var oldSettings = new RunnerSettings { AgentName = oldName };
                var reservedAgent = new TaskAgentReference { Name = newName };

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestStopOnJobTokenExpiredExceptions));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                var request = new Mock<TaskAgentJobRequest>();
                request.Object.ReservedAgent = reservedAgent;
                PropertyInfo lockUntilProperty = request.Object.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request.Object, DateTime.UtcNow.AddMinutes(5));
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(oldSettings);
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (count < 5)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request.Object);
                                }
                                else if (count == 5 || count == 6 || count == 7)
                                {
                                    throw new TimeoutException("");
                                }
                                else
                                {
                                    cancellationTokenSource.Cancel();
                                    return Task.FromResult<TaskAgentJobRequest>(request.Object);
                                }
                            });
                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                // Act
                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), 0, 0, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                // Assert
                _configurationStore.Verify(x => x.SaveSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void RenewJobRequestNullAgentNameIgnored()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var count = 0;
                var oldName = "OldName";
                var oldSettings = new RunnerSettings { AgentName = oldName };

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestStopOnJobTokenExpiredExceptions));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                var request = new Mock<TaskAgentJobRequest>();
                PropertyInfo lockUntilProperty = request.Object.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request.Object, DateTime.UtcNow.AddMinutes(5));
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(oldSettings);
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (count < 5)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request.Object);
                                }
                                else if (count == 5 || count == 6 || count == 7)
                                {
                                    throw new TimeoutException("");
                                }
                                else
                                {
                                    cancellationTokenSource.Cancel();
                                    return Task.FromResult<TaskAgentJobRequest>(request.Object);
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                // Act
                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), 0, 0, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                // Assert
                _configurationStore.Verify(x => x.SaveSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatcherRenewJobRequestRecoverFromExceptions()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                int poolId = 1;
                Int64 requestId = 1000;
                int count = 0;

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestRecoverFromExceptions));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                TaskAgentJobRequest request = new();
                PropertyInfo lockUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (!firstJobRequestRenewed.Task.IsCompletedSuccessfully)
                                {
                                    trace.Info("First renew happens.");
                                }

                                if (count < 5)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request);
                                }
                                else if (count == 5 || count == 6 || count == 7)
                                {
                                    throw new TimeoutException("");
                                }
                                else
                                {
                                    cancellationTokenSource.Cancel();
                                    return Task.FromResult<TaskAgentJobRequest>(request);
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), poolId, requestId, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                Assert.True(firstJobRequestRenewed.Task.IsCompletedSuccessfully, "First renew should succeed.");
                Assert.True(cancellationTokenSource.IsCancellationRequested);
                _runnerServer.Verify(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(8));
                _runnerServer.Verify(x => x.RefreshConnectionAsync(RunnerConnectionType.JobRequest, It.IsAny<TimeSpan>()), Times.Exactly(3));
                _runnerServer.Verify(x => x.SetConnectionTimeout(RunnerConnectionType.JobRequest, It.IsAny<TimeSpan>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatcherRenewJobRequestFirstRenewRetrySixTimes()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                int poolId = 1;
                Int64 requestId = 1000;
                int count = 0;

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestFirstRenewRetrySixTimes));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                TaskAgentJobRequest request = new();
                PropertyInfo lockUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (!firstJobRequestRenewed.Task.IsCompletedSuccessfully)
                                {
                                    trace.Info("First renew happens.");
                                }

                                if (count <= 5)
                                {
                                    throw new TimeoutException("");
                                }
                                else
                                {
                                    cancellationTokenSource.CancelAfter(10000);
                                    throw new InvalidOperationException("Should not reach here.");
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), poolId, requestId, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                Assert.False(firstJobRequestRenewed.Task.IsCompletedSuccessfully, "First renew should failed.");
                Assert.False(cancellationTokenSource.IsCancellationRequested);
                _runnerServer.Verify(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatcherRenewJobRequestStopOnExpiredRequest()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                int poolId = 1;
                Int64 requestId = 1000;
                int count = 0;

                var trace = hc.GetTrace(nameof(DispatcherRenewJobRequestStopOnExpiredRequest));
                TaskCompletionSource<int> firstJobRequestRenewed = new();
                CancellationTokenSource cancellationTokenSource = new();

                TaskAgentJobRequest request = new();
                PropertyInfo lockUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(lockUntilProperty);
                lockUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Returns(() =>
                            {
                                count++;
                                if (!firstJobRequestRenewed.Task.IsCompletedSuccessfully)
                                {
                                    trace.Info("First renew happens.");
                                }

                                if (count == 1)
                                {
                                    return Task.FromResult<TaskAgentJobRequest>(request);
                                }
                                else if (count < 5)
                                {
                                    throw new TimeoutException("");
                                }
                                else if (count == 5)
                                {
                                    lockUntilProperty.SetValue(request, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(11)));
                                    throw new TimeoutException("");
                                }
                                else
                                {
                                    cancellationTokenSource.CancelAfter(10000);
                                    throw new InvalidOperationException("Should not reach here.");
                                }
                            });

                var jobDispatcher = new JobDispatcher();
                jobDispatcher.Initialize(hc);

                await jobDispatcher.RenewJobRequestAsync(It.IsAny<AgentJobRequestMessage>(), It.IsAny<ServiceEndpoint>(), poolId, requestId, Guid.Empty, Guid.NewGuid().ToString(), firstJobRequestRenewed, cancellationTokenSource.Token);

                Assert.True(firstJobRequestRenewed.Task.IsCompletedSuccessfully, "First renew should succeed.");
                Assert.False(cancellationTokenSource.IsCancellationRequested);
                _runnerServer.Verify(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
                _runnerServer.Verify(x => x.RefreshConnectionAsync(RunnerConnectionType.JobRequest, It.IsAny<TimeSpan>()), Times.Exactly(3));
                _runnerServer.Verify(x => x.SetConnectionTimeout(RunnerConnectionType.JobRequest, It.IsAny<TimeSpan>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void DispatchesOneTimeJobRequest()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var jobDispatcher = new JobDispatcher();
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                jobDispatcher.Initialize(hc);

                var ts = new CancellationTokenSource();
                Pipelines.AgentJobRequestMessage message = CreateJobRequestMessage();
                string strMessage = JsonUtility.ToString(message);

                _processInvoker.Setup(x => x.ExecuteAsync(It.IsAny<String>(), It.IsAny<String>(), "spawnclient 1 2", null, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<int>(56));

                _processChannel.Setup(x => x.StartServer(It.IsAny<StartProcessDelegate>()))
                    .Callback((StartProcessDelegate startDel) => { startDel("1", "2"); });
                _processChannel.Setup(x => x.SendAsync(MessageType.NewJobRequest, It.Is<string>(s => s.Equals(strMessage)), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var request = new TaskAgentJobRequest();
                PropertyInfo sessionIdProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));

                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(request));

                _runnerServer.Setup(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TaskResult>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(new TaskAgentJobRequest()));

                //Act
                jobDispatcher.Run(message, true);

                //Assert
                await jobDispatcher.WaitAsync(CancellationToken.None);

                Assert.True(jobDispatcher.RunOnceJobCompleted.Task.IsCompleted, "JobDispatcher should set task complete token for one time agent.");
                if (jobDispatcher.RunOnceJobCompleted.Task.IsCompleted)
                {
                    var result = await jobDispatcher.RunOnceJobCompleted.Task;
                    Assert.Equal(TaskResult.Succeeded, result);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task WorkerLaunchFailureFailsJobRequest()
        {
            using (var hc = new TestHostContext(this))
            {
                var jobDispatcher = new JobDispatcher();
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IJobNotification>(new Mock<IJobNotification>().Object);
                hc.SetSingleton<ITerminal>(new Mock<ITerminal>().Object);

                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                jobDispatcher.Initialize(hc);

                Pipelines.AgentJobRequestMessage message = CreateJobRequestMessage();
                message.Variables[Constants.Runner.Features.FailJobOnWorkerLaunchFailure] = new VariableValue("true");

                SetupWorkerLaunchFailure();

                var request = new TaskAgentJobRequest();
                PropertyInfo lockedUntilProperty = request.GetType().GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                lockedUntilProperty.SetValue(request, DateTime.UtcNow.AddMinutes(5));
                _runnerServer.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(request));
                _runnerServer.Setup(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TaskResult>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<TaskAgentJobRequest>(new TaskAgentJobRequest()));

                jobDispatcher.Run(message, true);
                await jobDispatcher.WaitAsync(CancellationToken.None);

                Assert.True(jobDispatcher.RunOnceJobCompleted.Task.IsCompleted, "JobDispatcher should complete the job when the worker fails to launch.");
                Assert.Equal(TaskResult.Failed, await jobDispatcher.RunOnceJobCompleted.Task);
                _processChannel.Verify(x => x.SendAsync(It.IsAny<MessageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
                _runnerServer.Verify(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), TaskResult.Failed, It.IsAny<CancellationToken>()), Times.Once);
                // Lock renewal is stopped after the first renew instead of renewing indefinitely.
                _runnerServer.Verify(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task WorkerLaunchFailureFailsRunServiceJobWithAnnotation()
        {
            using (var hc = new TestHostContext(this))
            {
                var jobDispatcher = new JobDispatcher();
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunServer>(_runServer.Object);
                hc.SetSingleton<IJobNotification>(new Mock<IJobNotification>().Object);
                hc.SetSingleton<ITerminal>(new Mock<ITerminal>().Object);

                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

                _configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1 });
                jobDispatcher.Initialize(hc);

                var message = GetAgentJobRequestMessage();
                typeof(AgentJobRequestMessage).GetProperty(nameof(AgentJobRequestMessage.MessageType)).SetValue(message, JobRequestMessageTypes.RunnerJobRequest);
                message.Variables[Constants.Runner.Features.FailJobOnWorkerLaunchFailure] = new VariableValue("true");
                var systemConnection = GetServiceEndpoint();
                systemConnection.Name = WellKnownServiceEndpointNames.SystemVssConnection;
                systemConnection.Url = new Uri("https://pipelines.actions.githubusercontent.com");
                systemConnection.Authorization.Parameters["AccessToken"] = CreateUnsignedJwt();
                message.Resources.Endpoints.Add(systemConnection);

                SetupWorkerLaunchFailure();

                _runServer.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>())).Returns(Task.CompletedTask);
                _runServer.Setup(x => x.RenewJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new RenewJobResponse() { LockedUntil = DateTime.UtcNow.AddMinutes(5) }));

                IList<Annotation> reportedAnnotations = null;
                _runServer.Setup(x => x.CompleteJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), TaskResult.Failed, It.IsAny<Dictionary<string, VariableValue>>(), It.IsAny<IList<StepResult>>(), It.IsAny<IList<Annotation>>(), It.IsAny<string>(), It.IsAny<IList<Telemetry>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback((Guid planId, Guid jobId, TaskResult result, Dictionary<string, VariableValue> outputs, IList<StepResult> stepResults, IList<Annotation> jobAnnotations, string environmentUrl, IList<Telemetry> telemetry, string billingOwnerId, string infrastructureFailureCategory, CancellationToken token) => reportedAnnotations = jobAnnotations)
                    .Returns(Task.CompletedTask);

                jobDispatcher.Run(message, true);
                await jobDispatcher.WaitAsync(CancellationToken.None);

                Assert.Equal(TaskResult.Failed, await jobDispatcher.RunOnceJobCompleted.Task);
                _processChannel.Verify(x => x.SendAsync(It.IsAny<MessageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
                _runServer.Verify(x => x.CompleteJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), TaskResult.Failed, It.IsAny<Dictionary<string, VariableValue>>(), It.IsAny<IList<StepResult>>(), It.IsAny<IList<Annotation>>(), It.IsAny<string>(), It.IsAny<IList<Telemetry>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
                Assert.NotNull(reportedAnnotations);
                var annotation = Assert.Single(reportedAnnotations);
                Assert.Contains("failed to start the worker process", annotation.Message);
                Assert.Contains("An Application Control policy has blocked this file.", annotation.Message);
            }
        }

        private void SetupWorkerLaunchFailure()
        {
            _processChannel.Setup(x => x.StartServer(It.IsAny<StartProcessDelegate>()))
                .Callback((StartProcessDelegate startDel) => { startDel("1", "2"); });
            _processInvoker.Setup(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    "spawnclient 1 2",
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Encoding>(),
                    It.IsAny<bool>(),
                    It.IsAny<Channel<string>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<int>(new Win32Exception(4551, "An Application Control policy has blocked this file.")));
        }

        private static string CreateUnsignedJwt()
        {
            static string Base64Url(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{Base64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{Base64Url("{\"orch_id\":\"orchestration\"}")}.";
        }

        private static void EnableRunServiceJobForJobDispatcher(JobDispatcher jobDispatcher)
        {
            // Set the value of the _isRunServiceJob field to true
            var isRunServiceJobField = typeof(JobDispatcher).GetField("_isRunServiceJob", BindingFlags.NonPublic | BindingFlags.Instance);
            isRunServiceJobField.SetValue(jobDispatcher, true);
        }

        private static ServiceEndpoint GetServiceEndpoint()
        {
            var serviceEndpoint = new ServiceEndpoint
            {
                Authorization = new EndpointAuthorization
                {
                    Scheme = EndpointAuthorizationSchemes.OAuth
                }
            };
            serviceEndpoint.Authorization.Parameters.Add("AccessToken", "token");
            return serviceEndpoint;
        }

        private static AgentJobRequestMessage GetAgentJobRequestMessage()
        {
            var message = new AgentJobRequestMessage(
                new TaskOrchestrationPlanReference()
                {
                    PlanType = "Build",
                    PlanId = Guid.NewGuid(),
                    Version = 1
                },
                new TimelineReference()
                {
                    Id = Guid.NewGuid()
                },
                Guid.NewGuid(),
                "jobDisplayName",
                "jobName",
                null,
                null,
                new List<TemplateToken>(),
                new Dictionary<string, VariableValue>()
                {
                    {
                        "variables",
                        new VariableValue()
                        {
                            IsSecret = false,
                            Value = "variables"
                        }
                    }
                },
                new List<MaskHint>()
                {
                    new MaskHint()
                    {
                        Type = MaskType.Variable,
                        Value = "maskHints"
                    }
                },
                new JobResources(),
                new DictionaryContextData(),
                new WorkspaceOptions(),
                new List<JobStep>(),
                new List<string>()
                {
                    "fileTable"
                },
                null,
                new List<TemplateToken>(),
                new ActionsEnvironmentReference("env"),
                null
            );
            return message;
        }
    }
}
