using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Common.Tests;
using System.Runtime.CompilerServices;
using GitHub.Services.WebApi;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class ErrorThrottlerL0
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public async Task TestIncrementAndWait(int totalAttempts)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var errorThrottler = new ErrorThrottler();
                errorThrottler.Initialize(hc);
                var eventArgs = new List<DelayEventArgs>();
                hc.Delaying += (sender, args) =>
                {
                    eventArgs.Add(args);
                };

                // Act
                for (int attempt = 1; attempt <= totalAttempts; attempt++)
                {
                    await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                }

                // Assert
                Assert.Equal(totalAttempts - 1, eventArgs.Count);
                for (int i = 0; i < eventArgs.Count; i++)
                {
                    // Expected milliseconds
                    int expectedMin;
                    int expectedMax;

                    switch (i)
                    {
                        case 0:
                            expectedMin = 1000; // Min backoff
                            expectedMax = 1000;
                            break;
                        case 1:
                            expectedMin = 1800; // Min + 0.8 * Coefficient
                            expectedMax = 2200; // Min + 1.2 * Coefficient
                            break;
                        case 2:
                            expectedMin = 3400; // Min + 0.8 * Coefficient * 3
                            expectedMax = 4600; // Min + 1.2 * Coefficient * 3
                            break;
                        case 3:
                            expectedMin = 6600; // Min + 0.8 * Coefficient * 7
                            expectedMax = 9400; // Min + 1.2 * Coefficient * 7
                            break;
                        case 4:
                            expectedMin = 13000; // Min + 0.8 * Coefficient * 15
                            expectedMax = 19000; // Min + 1.2 * Coefficient * 15
                            break;
                        case 5:
                            expectedMin = 25800; // Min + 0.8 * Coefficient * 31
                            expectedMax = 38200; // Min + 1.2 * Coefficient * 31
                            break;
                        case 6:
                            expectedMin = 51400; // Min + 0.8 * Coefficient * 63
                            expectedMax = 60000; // Max backoff
                            break;
                        case 7:
                            expectedMin = 60000;
                            expectedMax = 60000;
                            break;
                        default:
                            throw new NotSupportedException("Unexpected eventArgs count");
                    }

                    var actualMilliseconds = eventArgs[i].Delay.TotalMilliseconds;
                    Assert.True(expectedMin <= actualMilliseconds, $"Unexpected min delay for eventArgs[{i}]. Expected min {expectedMin}, actual {actualMilliseconds}");
                    Assert.True(expectedMax >= actualMilliseconds, $"Unexpected max delay for eventArgs[{i}]. Expected max {expectedMax}, actual {actualMilliseconds}");
                }
            }
        }

        [Fact]
        public async Task TestReset()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var errorThrottler = new ErrorThrottler();
                errorThrottler.Initialize(hc);
                var eventArgs = new List<DelayEventArgs>();
                hc.Delaying += (sender, args) =>
                {
                    eventArgs.Add(args);
                };

                // Act
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                errorThrottler.Reset();
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);

                // Assert
                Assert.Equal(4, eventArgs.Count);
                for (int i = 0; i < eventArgs.Count; i++)
                {
                    // Expected milliseconds
                    int expectedMin;
                    int expectedMax;

                    switch (i)
                    {
                        case 0:
                        case 2:
                            expectedMin = 1000; // Min backoff
                            expectedMax = 1000;
                            break;
                        case 1:
                        case 3:
                            expectedMin = 1800; // Min + 0.8 * Coefficient
                            expectedMax = 2200; // Min + 1.2 * Coefficient
                            break;
                        default:
                            throw new NotSupportedException("Unexpected eventArgs count");
                    }

                    var actualMilliseconds = eventArgs[i].Delay.TotalMilliseconds;
                    Assert.True(expectedMin <= actualMilliseconds, $"Unexpected min delay for eventArgs[{i}]. Expected min {expectedMin}, actual {actualMilliseconds}");
                    Assert.True(expectedMax >= actualMilliseconds, $"Unexpected max delay for eventArgs[{i}]. Expected max {expectedMax}, actual {actualMilliseconds}");
                }
            }
        }

        [Fact]
        public async Task TestReceivesCancellationToken()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var errorThrottler = new ErrorThrottler();
                errorThrottler.Initialize(hc);
                var eventArgs = new List<DelayEventArgs>();
                hc.Delaying += (sender, args) =>
                {
                    eventArgs.Add(args);
                };
                var cancellationTokenSource1 = new CancellationTokenSource();
                var cancellationTokenSource2 = new CancellationTokenSource();
                var cancellationTokenSource3 = new CancellationTokenSource();

                // Act
                await errorThrottler.IncrementAndWaitAsync(cancellationTokenSource1.Token);
                await errorThrottler.IncrementAndWaitAsync(cancellationTokenSource2.Token);
                await errorThrottler.IncrementAndWaitAsync(cancellationTokenSource3.Token);

                // Assert
                Assert.Equal(2, eventArgs.Count);
                Assert.Equal(cancellationTokenSource2.Token, eventArgs[0].Token);
                Assert.Equal(cancellationTokenSource3.Token, eventArgs[1].Token);
            }
        }

        [Fact]
        public async Task TestReceivesSender()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var errorThrottler = new ErrorThrottler();
                errorThrottler.Initialize(hc);
                var senders = new List<object>();
                hc.Delaying += (sender, args) =>
                {
                    senders.Add(sender);
                };

                // Act
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);
                await errorThrottler.IncrementAndWaitAsync(CancellationToken.None);

                // Assert
                Assert.Equal(2, senders.Count);
                Assert.Equal(hc, senders[0]);
                Assert.Equal(hc, senders[1]);
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            return new TestHostContext(this, testName);
        }
    }
}
