using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Moq;
using Sdk.RSWebApi.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class JobDispatcherIpcCleanupL0
    {
        private readonly ITestOutputHelper _output;
        public JobDispatcherIpcCleanupL0(ITestOutputHelper output) => _output = output;

        [Theory]
        [InlineData("healthy", false)]
        [InlineData("timeout", false)]
        [InlineData("broken-pipe", false)]
        [InlineData("healthy", true)]
        [InlineData("timeout", true)]
        [InlineData("broken-pipe", true)]
        [InlineData("broken-pipe-worker-fault", false)]
        [InlineData("broken-pipe-worker-fault", true)]
        [InlineData("broken-pipe-gated-renewal", false)]
        [InlineData("broken-pipe-gated-renewal", true)]
        public async Task DispatchStopsRenewalAfterSendOutcome(string outcome, bool runService)
        {
            using var hc = new TestHostContext(this, $"Ipc_{outcome}_{runService}");
            var config = new Mock<IConfigurationStore>();
            var server = new Mock<IRunnerServer>();
            var runServer = new Mock<IRunServer>();
            var channel = new Mock<IProcessChannel>();
            var invoker = new Mock<IProcessInvoker>();
            var notification = new Mock<IJobNotification>();
            var worker = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var parkedRenewal = new TaskCompletionSource<TaskAgentJobRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
            var parkedRunRenewal = new TaskCompletionSource<RenewJobResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            bool gatedRenewal = outcome == "broken-pipe-gated-renewal";
            var renewalEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var renewalCancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken renewalToken = default;
            CancellationToken workerToken = default;
            CancellationTokenRegistration renewalRegistration = default;
            CancellationTokenRegistration workerRegistration = default;
            int renewCalls = 0;
            int sends = 0;

            hc.SetSingleton<IConfigurationStore>(config.Object);
            hc.SetSingleton<IRunnerServer>(server.Object);
            hc.SetSingleton<IRunServer>(runServer.Object);
            hc.SetSingleton<IJobNotification>(notification.Object);
            hc.EnqueueInstance<IProcessChannel>(channel.Object);
            hc.EnqueueInstance<IProcessInvoker>(invoker.Object);
            config.Setup(x => x.GetSettings()).Returns(new RunnerSettings { PoolId = 1 });
            notification.Setup(x => x.JobCompleted(It.IsAny<Guid>())).Returns(Task.CompletedTask);
            runServer.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>())).Returns(Task.CompletedTask);
            var request = new TaskAgentJobRequest();
            typeof(TaskAgentJobRequest).GetProperty("LockedUntil", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(request, DateTime.UtcNow.AddMinutes(5));

            server.Setup(x => x.RenewAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((int pool, long id, Guid token, string orchestration, CancellationToken ct) =>
                {
                    if (Interlocked.Increment(ref renewCalls) == 1)
                    {
                        renewalToken = ct;
                        renewalRegistration = ct.Register(() =>
                        {
                            renewalCancelled.TrySetResult(true);
                            if (!gatedRenewal) parkedRenewal.TrySetCanceled(ct);
                        });
                        return Task.FromResult(request);
                    }
                    renewalEntered.TrySetResult(true);
                    return parkedRenewal.Task;
                });
            runServer.Setup(x => x.RenewJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid plan, Guid job, CancellationToken ct) =>
                {
                    if (Interlocked.Increment(ref renewCalls) == 1)
                    {
                        renewalToken = ct;
                        renewalRegistration = ct.Register(() =>
                        {
                            renewalCancelled.TrySetResult(true);
                            if (!gatedRenewal) parkedRunRenewal.TrySetCanceled(ct);
                        });
                        return Task.FromResult(new RenewJobResponse { LockedUntil = DateTime.UtcNow.AddMinutes(5) });
                    }
                    renewalEntered.TrySetResult(true);
                    return parkedRunRenewal.Task;
                });
            server.Setup(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TaskResult>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TaskAgentJobRequest());
            channel.Setup(x => x.StartServer(It.IsAny<StartProcessDelegate>()))
                .Callback((StartProcessDelegate start) => start("1", "2"));
            invoker.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), "spawnclient 1 2",
                    null, false, null, true, null, false, false, true, It.IsAny<CancellationToken>()))
                .Returns((string directory, string file, string args, IDictionary<string, string> environment,
                    bool zero, Encoding encoding, bool kill, Channel<string> stdin, bool inherit, bool keep, bool priority, CancellationToken ct) =>
                {
                    workerToken = ct;
                    workerRegistration = ct.Register(() => worker.TrySetCanceled(ct));
                    return worker.Task;
                });
            channel.Setup(x => x.SendAsync(MessageType.NewJobRequest, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (MessageType type, string body, CancellationToken ct) =>
                {
                    Interlocked.Increment(ref sends);
                    if (gatedRenewal) await renewalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    if (outcome.StartsWith("broken-pipe", StringComparison.Ordinal))
                    {
                        if (outcome == "broken-pipe-worker-fault") worker.TrySetException(new System.ComponentModel.Win32Exception(5, "synthetic startup failure"));
                        throw new IOException("synthetic broken pipe");
                    }
                    if (outcome == "timeout") throw new OperationCanceledException("synthetic send timeout");
                    worker.TrySetResult(TaskResultUtil.TranslateToReturnCode(TaskResult.Succeeded));
                });

            var message = new AgentJobRequestMessage(new TaskOrchestrationPlanReference(), null, Guid.NewGuid(),
                "synthetic", "synthetic", null, null, null, new Dictionary<string, VariableValue>(),
                new List<MaskHint>(), new JobResources(), new DictionaryContextData(), new WorkspaceOptions(),
                new List<ActionStep>(), null, null, null, null, null,
                runService ? JobRequestMessageTypes.RunnerJobRequest : JobRequestMessageTypes.PipelineAgentJobRequest);
            message.ContextData["github"] = new DictionaryContextData();
            var endpoint = new ServiceEndpoint
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Url = new Uri("https://example.invalid/"),
                // No OAuth credential is needed: ConnectAsync is a mock and accepts null credentials.
                Authorization = new EndpointAuthorization { Scheme = "synthetic-mocked-only" }
            };
            endpoint.Authorization.Parameters.Add("AccessToken", "");
            message.Resources.Endpoints.Add(endpoint);
            var dispatcher = new JobDispatcher();
            dispatcher.Initialize(hc);
            try
            {
                dispatcher.Run(message);
                var dispatch = dispatcher.WaitAsync(CancellationToken.None);
                if (gatedRenewal)
                {
                    await renewalCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    // Cancellation is deliberately acknowledged without completing renewal.
                    // Dispatch must remain pending until the actual renewal task is released.
                    await Task.WhenAny(dispatch, Task.Delay(100));
                    Assert.False(dispatch.IsCompleted, "Dispatch must await renewal completion, not just request cancellation.");
                    parkedRenewal.TrySetCanceled(renewalToken);
                    parkedRunRenewal.TrySetCanceled(renewalToken);
                }
                await dispatch.WaitAsync(TimeSpan.FromSeconds(5));
                // Allow one accelerated renewal cycle to show whether work survived dispatch completion.
                await Task.Delay(50);
                bool leaseCancelled = renewalToken.IsCancellationRequested;
                bool processCancelled = workerToken.IsCancellationRequested;
                _output.WriteLine($"outcome={outcome}; runService={runService}; sends={sends}; renewCalls={renewCalls}; renewalCancelled={leaseCancelled}; workerCancelled={processCancelled}; busy={dispatcher.Busy}");
                Assert.Equal(1, sends);
                Assert.True(renewalToken.CanBeCanceled, "Fixture must reach actual lease renewal.");
                Assert.True(workerToken.CanBeCanceled, "Fixture must reach the full ExecuteAsync overload used by RunAsync.");
                Assert.False(dispatcher.Busy);
                Assert.True(leaseCancelled, "Renewal must be cancelled before dispatch teardown.");
                if (outcome != "healthy") Assert.True(processCancelled, "Failed send must cancel the pending worker.");
                notification.Verify(x => x.JobStarted(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Uri>()), outcome == "healthy" ? Times.Once() : Times.Never());
                server.Verify(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), TaskResult.Succeeded, It.IsAny<CancellationToken>()), outcome == "healthy" && !runService ? Times.Once() : Times.Never());
                if (outcome != "healthy")
                {
                    server.Verify(x => x.FinishAgentRequestAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TaskResult>(), It.IsAny<CancellationToken>()), Times.Never());
                    Assert.DoesNotContain(runServer.Invocations, call => call.Method.Name == nameof(IRunServer.CompleteJobAsync));
                }
            }
            finally
            {
                // Teardown happens after observations; it cannot satisfy the cancellation assertions.
                parkedRenewal.TrySetException(new TaskAgentJobNotFoundException("synthetic teardown"));
                parkedRunRenewal.TrySetException(new TaskOrchestrationJobNotFoundException("synthetic teardown"));
                worker.TrySetCanceled();
                await Task.Delay(30);
                renewalRegistration.Dispose();
                workerRegistration.Dispose();
            }
        }
    }
}
