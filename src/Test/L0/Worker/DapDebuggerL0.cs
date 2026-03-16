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

                mockServer.Verify(x => x.WaitForConnectionAsync(cts.Token), Times.Once);
                mockSession.Verify(x => x.WaitForHandshakeAsync(cts.Token), Times.Once);

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

                await _debugger.OnStepStartingAsync(mockStep.Object, mockJobContext.Object, true, CancellationToken.None);

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

                await _debugger.OnStepStartingAsync(mockStep.Object, mockJobContext.Object, true, CancellationToken.None);

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

                _debugger.OnJobCompleted();

                mockSession.Verify(x => x.OnJobCompleted(), Times.Once);

                await _debugger.StopAsync();
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
                await _debugger.OnStepStartingAsync(mockStep.Object, mockJobContext.Object, true, CancellationToken.None);

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
    }
}
