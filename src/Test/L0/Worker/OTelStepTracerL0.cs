using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class OTelStepTracerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TraceID_MatchesOtelExplorer()
        {
            // otel-explorer: MD5("99999-1") = known hex value
            // Verify determinism and format
            var tid1 = OTelStepTracer.NewTraceID(99999, 1);
            var tid2 = OTelStepTracer.NewTraceID(99999, 1);
            Assert.Equal(tid1, tid2);
            Assert.Equal(32, tid1.Length);

            // Different attempt produces different trace ID
            var tid3 = OTelStepTracer.NewTraceID(99999, 2);
            Assert.NotEqual(tid1, tid3);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SpanID_BigEndian_MatchesOtelExplorer()
        {
            // otel-explorer: NewSpanID(42) = BigEndian(42) = 000000000000002a
            var sid = OTelStepTracer.NewSpanID(42);
            Assert.Equal("000000000000002a", sid);
            Assert.Equal(16, sid.Length);

            // Deterministic
            Assert.Equal(sid, OTelStepTracer.NewSpanID(42));

            // Different input
            Assert.NotEqual(sid, OTelStepTracer.NewSpanID(43));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SpanIDFromString_Deterministic()
        {
            var sid1 = OTelStepTracer.NewSpanIDFromString("test-span");
            var sid2 = OTelStepTracer.NewSpanIDFromString("test-span");
            Assert.Equal(sid1, sid2);
            Assert.Equal(16, sid1.Length);

            var sid3 = OTelStepTracer.NewSpanIDFromString("other-span");
            Assert.NotEqual(sid1, sid3);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TraceID_CrossLanguageCompatibility()
        {
            // Verify that our C# MD5 produces the same trace ID as Go's md5.Sum
            // for the same input string. This is critical for trace correlation.
            //
            // Go code: trace.TraceID(md5.Sum([]byte("99999-1")))
            // We can verify the MD5 hash is deterministic across implementations.
            var tid = OTelStepTracer.NewTraceID(99999, 1);

            // MD5("99999-1") should be the same in any language
            var expected = System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes("99999-1"));
            var expectedHex = BitConverter.ToString(expected).Replace("-", "").ToLowerInvariant();

            Assert.Equal(expectedHex, tid);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DefaultAttemptZero_TreatedAsOne()
        {
            var tidZero = OTelStepTracer.NewTraceID(99999, 0);
            var tidOne = OTelStepTracer.NewTraceID(99999, 1);
            Assert.Equal(tidZero, tidOne);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsEnabled_FalseByDefault()
        {
            OTelStepTracer.Reset();
            // Without ACTIONS_RUNNER_OTLP_ENDPOINT set, should be disabled
            // (assuming test environment doesn't have it)
            var original = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT");
            try
            {
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", null);
                OTelStepTracer.Reset();
                Assert.False(OTelStepTracer.IsEnabled);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", original);
                OTelStepTracer.Reset();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsEnabled_TrueWhenEndpointSet()
        {
            var original = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT");
            try
            {
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", "http://localhost:4318");
                OTelStepTracer.Reset();
                Assert.True(OTelStepTracer.IsEnabled);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", original);
                OTelStepTracer.Reset();
            }
        }
    }
}
