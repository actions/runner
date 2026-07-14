using System;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class RetryBackoffsL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Fixed_ReturnsSameDelay()
        {
            var backoff = RetryBackoffs.Fixed(TimeSpan.FromMilliseconds(250));

            var first = backoff(1, 5, null);
            var second = backoff(4, 5, new InvalidOperationException("ignored"));

            Assert.Equal(TimeSpan.FromMilliseconds(250), first);
            Assert.Equal(TimeSpan.FromMilliseconds(250), second);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Random_StaysWithinBounds()
        {
            var min = TimeSpan.FromMilliseconds(100);
            var max = TimeSpan.FromMilliseconds(200);
            var backoff = RetryBackoffs.Random(min, max);

            for (var i = 0; i < 100; i++)
            {
                var delay = backoff(i + 1, 100, null);
                Assert.InRange(delay, min, max);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Exponential_IsBoundedByMax()
        {
            var min = TimeSpan.FromMilliseconds(100);
            var max = TimeSpan.FromMilliseconds(500);
            var delta = TimeSpan.FromMilliseconds(100);
            var backoff = RetryBackoffs.Exponential(min, max, delta);

            var first = backoff(1, 5, null);
            var second = backoff(2, 5, null);
            var saturated = backoff(10, 10, null);

            Assert.InRange(first, min, max);
            Assert.InRange(second, first, max);
            Assert.Equal(max, saturated);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ExponentialFullJitter_IsWithinBounds()
        {
            var min = TimeSpan.FromMilliseconds(50);
            var max = TimeSpan.FromMilliseconds(500);
            var delta = TimeSpan.FromMilliseconds(100);
            var backoff = RetryBackoffs.ExponentialFullJitter(min, max, delta);

            for (var attempt = 1; attempt <= 10; attempt++)
            {
                var delay = backoff(attempt, 10, null);
                Assert.InRange(delay, min, max);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DecorrelatedJitter_IsWithinBounds()
        {
            var min = TimeSpan.FromMilliseconds(100);
            var max = TimeSpan.FromMilliseconds(1000);
            var backoff = RetryBackoffs.DecorrelatedJitter(min, max);

            for (var i = 0; i < 100; i++)
            {
                var delay = backoff(i + 1, 100, null);
                Assert.InRange(delay, min, max);
            }
        }
    }
}
