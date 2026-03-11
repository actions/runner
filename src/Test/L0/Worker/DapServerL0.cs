using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker.Dap;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapServerL0
    {
        private DapServer _server;

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _server = new DapServer();
            _server.Initialize(hc);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitializeSucceeds()
        {
            using (CreateTestContext())
            {
                Assert.NotNull(_server);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetSessionAcceptsMock()
        {
            using (CreateTestContext())
            {
                var mockSession = new Mock<IDapDebugSession>();
                _server.SetSession(mockSession.Object);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SendEventNoClientDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var evt = new Event
                {
                    EventType = "stopped",
                    Body = new StoppedEventBody
                    {
                        Reason = "entry",
                        ThreadId = 1,
                        AllThreadsStopped = true
                    }
                };

                _server.SendEvent(evt);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SendResponseNoClientDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var response = new Response
                {
                    Type = "response",
                    RequestSeq = 1,
                    Command = "initialize",
                    Success = true
                };

                _server.SendResponse(response);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SendMessageNoClientDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var msg = new ProtocolMessage
                {
                    Type = "response",
                    Seq = 1
                };

                _server.SendMessage(msg);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StopWithoutStartDoesNotThrow()
        {
            using (CreateTestContext())
            {
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAndStopOnAvailablePort()
        {
            using (CreateTestContext())
            {
                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitForConnectionCancelledByCancellationToken()
        {
            using (CreateTestContext())
            {
                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);

                var waitTask = _server.WaitForConnectionAsync(cts.Token);

                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await waitTask;
                });

                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAndStopMultipleTimesDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var cts1 = new CancellationTokenSource();
                await _server.StartAsync(0, cts1.Token);
                await _server.StopAsync();

                _server = new DapServer();
                _server.Initialize(CreateTestContext());
                var cts2 = new CancellationTokenSource();
                await _server.StartAsync(0, cts2.Token);
                await _server.StopAsync();
            }
        }
    }
}
