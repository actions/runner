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
    [CollectionDefinition("Ephemeral drain", DisableParallelization = true)]
    public sealed class EphemeralDrainCollection { }

    [Collection("Ephemeral drain")]
    public sealed class EphemeralDrainL0 : IDisposable
    {
        private readonly string _hostedReturnCode = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_RETURN_JOB_RESULT_FOR_HOSTED");

        public EphemeralDrainL0()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_RETURN_JOB_RESULT_FOR_HOSTED", null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_RETURN_JOB_RESULT_FOR_HOSTED", _hostedReturnCode);
        }

        // Real Runner + real message listener. Only remote services and worker
        // execution are mocked, with explicit barriers at dispatch boundaries.
        [Theory]
        [InlineData("idle", true)]
        [InlineData("idle", false)]
        [InlineData("before-listening", true)]
        [InlineData("before-listening", false)]
        [InlineData("poll", true)]
        [InlineData("ack", true)]
        [InlineData("acquire", true)]
        [InlineData("worker", true)]
        [InlineData("cancel", true)]
        public async Task DrainAtBoundary(string boundary, bool brokerFlow)
        {
            using var hc = new TestHostContext(this, $"{boundary}-{brokerFlow}");
            var settings = new RunnerSettings
            {
                AgentId = 123,
                AgentName = "drain-test",
                PoolId = 456,
                ServerUrl = "https://github.example",
                ServerUrlV2 = "https://broker.example",
                UseV2Flow = brokerFlow,
                Ephemeral = true,
                WorkFolder = "_work"
            };
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.LoadSettings()).Returns(settings);
            config.Setup(x => x.IsConfigured()).Returns(true);
            var store = new Mock<IConfigurationStore>();
            store.Setup(x => x.GetCredentials()).Returns(new CredentialData { Scheme = Constants.Configuration.OAuthAccessToken });
            var creds = new Mock<ICredentialManager>();
            creds.Setup(x => x.LoadCredentials(It.IsAny<bool>())).Returns(new VssCredentials());
            var broker = new Mock<IBrokerServer>();
            var runnerServer = new Mock<IRunnerServer>();
            var runServer = new Mock<IRunServer>();
            var dispatcher = new Mock<IJobDispatcher>();
            hc.SetSingleton<IConfigurationManager>(config.Object);
            hc.SetSingleton<IConfigurationStore>(store.Object);
            hc.SetSingleton<ICredentialManager>(creds.Object);
            hc.SetSingleton<IBrokerServer>(broker.Object);
            hc.SetSingleton<IRunnerServer>(runnerServer.Object);
            hc.SetSingleton<IJobNotification>(new Mock<IJobNotification>().Object);
            hc.SetSingleton<IPromptManager>(new Mock<IPromptManager>().Object);
            hc.EnqueueInstance<IRunServer>(runServer.Object);
            hc.EnqueueInstance<IJobDispatcher>(dispatcher.Object);
            hc.EnqueueInstance<IErrorThrottler>(new Mock<IErrorThrottler>().Object);

            IMessageListener listener = brokerFlow ? new BrokerMessageListener() : new MessageListener();
            listener.Initialize(hc);
            hc.SetSingleton<IMessageListener>(listener);
            broker.Setup(x => x.CreateSessionAsync(It.IsAny<TaskAgentSession>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TaskAgentSession());
            runnerServer.Setup(x => x.CreateAgentSessionAsync(It.IsAny<int>(), It.IsAny<TaskAgentSession>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TaskAgentSession());

            var pollEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releasePoll = new TaskCompletionSource<TaskAgentMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ackEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseAck = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var acquireEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseAcquire = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var workerEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancelReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var drainWritten = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var workerCompleted = new TaskCompletionSource<TaskResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var polls = 0;
            var ran = false;
            var shutdownBeforeCompletion = false;
            var request = new Pipelines.AgentJobRequestMessage(
                new TaskOrchestrationPlanReference(), null, Guid.NewGuid(), "test", "test", null,
                null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(),
                new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(),
                new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null, null, null, null, null);

            async Task<TaskAgentMessage> Poll(CancellationToken token)
            {
                if (Interlocked.Increment(ref polls) == 1)
                {
                    pollEntered.TrySetResult(true);
                    return await releasePoll.Task.WaitAsync(token);
                }
                if (boundary == "cancel" && polls == 2)
                {
                    await drainWritten.Task.WaitAsync(token);
                    return null;
                }
                if (boundary == "cancel" && polls == 3)
                {
                    return new TaskAgentMessage
                    {
                        MessageId = 2,
                        MessageType = JobCancelMessage.MessageType,
                        Body = JsonUtility.ToString(new JobCancelMessage(request.JobId, TimeSpan.FromSeconds(30)))
                    };
                }
                await Task.Delay(Timeout.Infinite, token);
                return null;
            }
            broker.Setup(x => x.GetRunnerMessageAsync(It.IsAny<Guid?>(), It.IsAny<TaskAgentStatus>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((Guid? session, TaskAgentStatus status, string version, string os, string arch, bool disable, CancellationToken token) => Poll(token));
            runnerServer.Setup(x => x.GetAgentMessageAsync(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<long?>(),
                It.IsAny<TaskAgentStatus>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((int pool, Guid session, long? last, TaskAgentStatus status, string version, string os, string arch, bool disable, CancellationToken token) => Poll(token));
            broker.Setup(x => x.AcknowledgeRunnerRequestAsync(It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<TaskAgentStatus>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (string id, Guid? session, TaskAgentStatus status, string version, string os, string arch, CancellationToken token) =>
                {
                    ackEntered.TrySetResult(true);
                    await releaseAck.Task.WaitAsync(token);
                });
            runServer.Setup(x => x.GetJobMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (string id, string owner, CancellationToken token) =>
                {
                    acquireEntered.TrySetResult(true);
                    await releaseAcquire.Task.WaitAsync(token);
                    return request;
                });
            dispatcher.Setup(x => x.RunOnceJobCompleted).Returns(workerCompleted);
            dispatcher.Setup(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true)).Callback(() =>
            {
                ran = true;
                workerEntered.TrySetResult(true);
            });
            dispatcher.Setup(x => x.Cancel(It.IsAny<JobCancelMessage>())).Returns(() =>
            {
                cancelReceived.TrySetResult(true);
                return true;
            });
            dispatcher.Setup(x => x.ShutdownAsync()).Returns(() =>
            {
                shutdownBeforeCompletion = ran && !workerCompleted.Task.IsCompleted;
                return Task.CompletedTask;
            });

            string sentinel = Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), ".drain");
            Assert.False(File.Exists(sentinel), "Test must not overwrite an existing sentinel");
            Task<int> execution = null;
            try
            {
                if (boundary == "before-listening") File.WriteAllText(sentinel, "drain");
                var runner = new Runner.Listener.Runner();
                runner.Initialize(hc);
                execution = runner.ExecuteCommand(new CommandSettings(hc, new[] { "run" }));
                if (boundary == "before-listening")
                {
                    Assert.Equal(0, await execution.WaitAsync(TimeSpan.FromSeconds(8)));
                    Assert.Equal(0, polls);
                    Assert.False(ran);
                    return;
                }
                await pollEntered.Task.WaitAsync(TimeSpan.FromSeconds(8));
                if (boundary == "idle" || boundary == "poll") File.WriteAllText(sentinel, "drain");
                if (boundary == "idle")
                {
                    releasePoll.SetResult(null);
                    Assert.Equal(0, await execution.WaitAsync(TimeSpan.FromSeconds(8)));
                    Assert.Equal(1, polls);
                    Assert.False(ran);
                    return;
                }
                releasePoll.SetResult(new TaskAgentMessage
                {
                    MessageId = 1,
                    MessageType = JobRequestMessageTypes.RunnerJobRequest,
                    Body = JsonUtility.ToString(new RunnerJobRequestRef
                    {
                        RunnerRequestId = "job",
                        BillingOwnerId = "owner",
                        RunServiceUrl = "https://run.example",
                        ShouldAcknowledge = true
                    })
                });
                await ackEntered.Task.WaitAsync(TimeSpan.FromSeconds(8));
                if (boundary == "ack") File.WriteAllText(sentinel, "drain");
                releaseAck.SetResult(true);
                await acquireEntered.Task.WaitAsync(TimeSpan.FromSeconds(8));
                if (boundary == "acquire") File.WriteAllText(sentinel, "drain");
                releaseAcquire.SetResult(true);
                await workerEntered.Task.WaitAsync(TimeSpan.FromSeconds(8));
                if (boundary == "worker" || boundary == "cancel") File.WriteAllText(sentinel, "drain");
                drainWritten.TrySetResult(true);
                if (boundary == "cancel") await cancelReceived.Task.WaitAsync(TimeSpan.FromSeconds(8));
                Assert.False(execution.IsCompleted);
                Assert.False(hc.RunnerShutdownToken.IsCancellationRequested);
                dispatcher.Verify(x => x.ShutdownAsync(), Times.Never());
                workerCompleted.SetResult(boundary == "cancel" ? TaskResult.Canceled : TaskResult.Succeeded);
                Assert.Equal(0, await execution.WaitAsync(TimeSpan.FromSeconds(8)));
                Assert.False(shutdownBeforeCompletion);
                dispatcher.Verify(x => x.Run(It.IsAny<Pipelines.AgentJobRequestMessage>(), true), Times.Once());
            }
            finally
            {
                if (execution != null && !execution.IsCompleted)
                {
                    hc.ShutdownRunner(ShutdownReason.UserCancelled);
                    try { await execution.WaitAsync(TimeSpan.FromSeconds(3)); } catch { }
                }
                File.Delete(sentinel);
            }
        }
    }
}
