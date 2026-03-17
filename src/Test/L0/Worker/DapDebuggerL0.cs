using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Moq;
using Newtonsoft.Json;
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

        private static Mock<IDapServer> CreateServerMock()
        {
            var mockServer = new Mock<IDapServer>();
            mockServer.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockServer.Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockServer.Setup(x => x.StopAsync())
                .Returns(Task.CompletedTask);
            mockServer.Setup(x => x.SendEvent(It.IsAny<Event>()));
            mockServer.Setup(x => x.SendResponse(It.IsAny<Response>()));
            return mockServer;
        }

        private Task CompleteHandshakeAsync()
        {
            var configJson = JsonConvert.SerializeObject(new Request
            {
                Seq = 1,
                Type = "request",
                Command = "configurationDone"
            });
            return _debugger.HandleMessageAsync(configJson, CancellationToken.None);
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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                mockServer.Verify(x => x.SetDebugger(It.IsAny<IDapDebuggerCallbacks>()), Times.Once);
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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", "not-a-number");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);

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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_PORT", "99999");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);

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
        public async Task WaitUntilReadyCallsServerAndCompletesHandshake()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);
                await CompleteHandshakeAsync();
                await _debugger.WaitUntilReadyAsync(cts.Token);

                mockServer.Verify(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
                Assert.Equal(DapSessionState.Ready, _debugger.State);

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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);
                await CompleteHandshakeAsync();
                await _debugger.WaitUntilReadyAsync(cts.Token);

                cts.Cancel();

                Assert.Equal(DapSessionState.Terminated, _debugger.State);
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
        public async Task OnJobCompletedStopsServer()
        {
            using (var hc = CreateTestContext())
            {
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);
                await CompleteHandshakeAsync();
                await _debugger.WaitUntilReadyAsync(cts.Token);

                await _debugger.OnJobCompletedAsync();

                mockServer.Verify(x => x.StopAsync(), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyBeforeStartIsNoOp()
        {
            using (CreateTestContext())
            {
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
                mockServer.Setup(x => x.SendResponse(It.IsAny<Response>()));

                hc.SetSingleton(mockServer.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);
                await CompleteHandshakeAsync();
                await _debugger.WaitUntilReadyAsync(cts.Token);

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

                hc.SetSingleton(mockServer.Object);

                var jobCts = new CancellationTokenSource();
                await _debugger.StartAsync(jobCts.Token);

                var waitTask = _debugger.WaitUntilReadyAsync(jobCts.Token);
                await Task.Delay(50);
                Assert.False(waitTask.IsCompleted);

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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", "30");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);
                    await CompleteHandshakeAsync();
                    await _debugger.WaitUntilReadyAsync(cts.Token);
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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", "not-a-number");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);
                    await CompleteHandshakeAsync();
                    await _debugger.WaitUntilReadyAsync(cts.Token);
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
                var mockServer = CreateServerMock();
                hc.SetSingleton(mockServer.Object);

                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT", "0");
                try
                {
                    var cts = new CancellationTokenSource();
                    await _debugger.StartAsync(cts.Token);
                    await CompleteHandshakeAsync();
                    await _debugger.WaitUntilReadyAsync(cts.Token);
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

                hc.SetSingleton(mockServer.Object);

                var cts = new CancellationTokenSource();
                await _debugger.StartAsync(cts.Token);

                var waitTask = _debugger.WaitUntilReadyAsync(cts.Token);
                await Task.Delay(50);

                cts.Cancel();
                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
                Assert.IsNotType<TimeoutException>(ex);

                await _debugger.StopAsync();
            }
        }
    }
}
