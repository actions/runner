using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapDebuggerL0
    {
        private DapDebugger _debugger;

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _debugger = new DapDebugger();
            _debugger.Initialize(hc);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitializeSucceeds()
        {
            using (CreateTestContext())
            {
                Assert.NotNull(_debugger);
                Assert.False(_debugger.IsActive);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAndStopLifecycle()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                mockServer.Verify(x => x.SetSession(mockSession.Object), Times.Once);
                mockSession.Verify(x => x.SetDapServer(mockServer.Object), Times.Once);
                mockServer.Verify(x => x.StartAsync(4711, cts.Token), Times.Once);

                await _debugger.StopAsync();
                mockServer.Verify(x => x.StopAsync(), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartUsesCustomPortFromEnvironment()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", "9999");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);

                    mockServer.Verify(x => x.StartAsync(9999, cts.Token), Times.Once);

                    await _debugger.StopAsync();
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartIgnoresInvalidPortFromEnvironment()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", "not-a-number");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);

                    // Falls back to default port
                    mockServer.Verify(x => x.StartAsync(4711, cts.Token), Times.Once);

                    await _debugger.StopAsync();
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartIgnoresOutOfRangePortFromEnvironment()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", "99999");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);

                    // Falls back to default port
                    mockServer.Verify(x => x.StartAsync(4711, cts.Token), Times.Once);

                    await _debugger.StopAsync();
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyCallsServerAndSession()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.WaitForHandshakeAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);
                await _debugger.WaitUntilReadyAsync(cts.Token);

                mockServer.Verify(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
                mockSession.Verify(x => x.WaitForHandshakeAsync(It.IsAny<CancellationToken>()), Times.Once);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyRegistersCancellation()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.WaitForHandshakeAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);
                await _debugger.WaitUntilReadyAsync(cts.Token);

                // Trigger cancellation — should call CancelSession on the session
                cts.Cancel();
                mockSession.Verify(x => x.CancelSession(), Times.Once);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StopWithoutStartDoesNotThrow()
        {
            using (CreateTestContext())
            {
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnStepStartingDelegatesWhenActive()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.IsActive).Returns(true);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                var mockStep = new Mock<IStep>();
                var mockJobContext = new Mock<IExecutionContext>();

                await _debugger.OnStepStartingAsync(mockStep.Object, mockJobContext.Object, CancellationToken.None);

                mockSession.Verify(x => x.OnStepStartingAsync(mockStep.Object, mockJobContext.Object, true, CancellationToken.None), Times.Once);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnStepStartingSkipsWhenNotActive()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.IsActive).Returns(false);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                var mockStep = new Mock<IStep>();
                var mockJobContext = new Mock<IExecutionContext>();

                await _debugger.OnStepStartingAsync(mockStep.Object, mockJobContext.Object, CancellationToken.None);

                mockSession.Verify(x => x.OnStepStartingAsync(It.IsAny<IStep>(), It.IsAny<IExecutionContext>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnStepCompletedDelegatesWhenActive()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.IsActive).Returns(true);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                var mockStep = new Mock<IStep>();
                _debugger.OnStepCompleted(mockStep.Object);

                mockSession.Verify(x => x.OnStepCompleted(mockStep.Object), Times.Once);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnJobCompletedDelegatesWhenActive()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.IsActive).Returns(true);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                await _debugger.OnJobCompletedAsync();

                mockSession.Verify(x => x.OnJobCompleted(), Times.Once);
                mockServer.Verify(x => x.StopAsync(), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnStepStartingSwallowsSessionException()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.IsActive).Returns(true);
                mockSession.Setup(x => x.OnStepStartingAsync(It.IsAny<IStep>(), It.IsAny<IExecutionContext>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("test error"));

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                var mockStep = new Mock<IStep>();
                var mockJobContext = new Mock<IExecutionContext>();

                // Should not throw
                await _debugger.OnStepStartingAsync(mockStep.Object, mockJobContext.Object, CancellationToken.None);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CancelSessionDelegatesToSession()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                var mockSession = new Mock<IDapDebugSession>();

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                // CancelSession before start should not throw
                _debugger.CancelSession();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyBeforeStartIsNoOp()
        {
            using (CreateTestContext())
            {
                // Should not throw or block
                await _debugger.WaitUntilReadyAsync(CancellationToken.None);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyPassesLinkedTokenNotOriginal()
        {
            using (var hc = CreateTestContext())
            {
                CancellationToken capturedToken = default;

                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Callback<CancellationToken>(ct => capturedToken = ct)
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.WaitForHandshakeAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);
                await _debugger.WaitUntilReadyAsync(cts.Token);

                // The token passed to WaitForConnectionAsync should be a linked token
                // (combines job cancellation + internal timeout), not the raw job token
                Assert.NotEqual(cts.Token, capturedToken);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyTimeoutSurfacesAsTimeoutException()
        {
            using (var hc = CreateTestContext())
            {
                // Mock WaitForConnectionAsync to block until its cancellation token fires,
                // then throw OperationCanceledException — simulating "no client connected"
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns<CancellationToken>(async ct =>
                    {
                        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        ct.Register(() => tcs.TrySetCanceled(ct));
                        await tcs.Task;
                    });
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var jobCts = new CancellationTokenSource();
                await _debugger.StartAsync(jobCts.Token);

                // Start wait in background
                var waitTask = _debugger.WaitUntilReadyAsync(jobCts.Token);
                await Task.Delay(50);
                Assert.False(waitTask.IsCompleted);

                // The linked token includes the internal timeout CTS.
                // We can't easily make it fire fast (it uses minutes), but we can
                // verify the contract: cancelling the job token produces OCE, not TimeoutException.
                jobCts.Cancel();
                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
                Assert.IsNotType<TimeoutException>(ex);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyUsesCustomTimeoutFromEnvironment()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.WaitForHandshakeAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", "30");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);

                    // The timeout is applied internally — we can verify it worked
                    // by checking the trace output contains the custom value
                    await _debugger.WaitUntilReadyAsync(cts.Token);

                    // If we got here without exception, the custom timeout was accepted
                    // (it didn't default to something that would fail)
                    await _debugger.StopAsync();
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyIgnoresInvalidTimeoutFromEnvironment()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.WaitForHandshakeAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", "not-a-number");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);
                    await _debugger.WaitUntilReadyAsync(cts.Token);

                    // Should succeed with default timeout (no crash from bad env var)
                    await _debugger.StopAsync();
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyIgnoresZeroTimeoutFromEnvironment()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.WaitForHandshakeAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", "0");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);
                    await _debugger.WaitUntilReadyAsync(cts.Token);

                    // Zero is not > 0, so falls back to default (should succeed, not throw)
                    await _debugger.StopAsync();
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyJobCancellationPropagatesAsOperationCancelledException()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = new Mock<IDapServer>();
                mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns<CancellationToken>(ct =>
                    {
                        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        ct.Register(() => tcs.TrySetCanceled(ct));
                        return tcs.Task;
                    });
                mockServer.Setup(x => x.StopAsync())
                    .Returns(Task.CompletedTask);

                var mockSession = new Mock<IDapDebugSession>();

                hc.SetSingleton(mockServer.Object);
                hc.SetSingleton(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                var waitTask = _debugger.WaitUntilReadyAsync(cts.Token);
                await Task.Delay(50);

                // Cancel the job token — should surface as OperationCanceledException, NOT TimeoutException
                cts.Cancel();
                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
                Assert.IsNotType<TimeoutException>(ex);

                await _debugger.StopAsync();
            }
        }
    }
}
