using System;
using System.Text.Json;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class OTelTracerL0 : IDisposable
    {
        private readonly string _originalEndpoint;

        public OTelTracerL0()
        {
            _originalEndpoint = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", _originalEndpoint);
            OTelTracer.Reset();
        }

        private static void Enable()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", "http://localhost:4318");
            OTelTracer.Reset();
        }

        // ---- shared deterministic ID contract (golden values mirrored in otel-explorer) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TraceID_GoldenAndDeterministic()
        {
            // md5("99999-1")
            Assert.Equal("37912fcf8909bcb43fd643580e6b5ee1", OTelTracer.NewTraceID(99999, 1));
            Assert.Equal(OTelTracer.NewTraceID(99999, 1), OTelTracer.NewTraceID(99999, 1));
            Assert.NotEqual(OTelTracer.NewTraceID(99999, 1), OTelTracer.NewTraceID(99999, 2));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TraceID_CrossLanguageMD5()
        {
            var expected = System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes("99999-1"));
            var expectedHex = BitConverter.ToString(expected).Replace("-", "").ToLowerInvariant();
            Assert.Equal(expectedHex, OTelTracer.NewTraceID(99999, 1));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DefaultAttemptZero_TreatedAsOne()
        {
            Assert.Equal(OTelTracer.NewTraceID(99999, 1), OTelTracer.NewTraceID(99999, 0));
            Assert.Equal(OTelTracer.NewJobSpanID(99999, 1, "build"), OTelTracer.NewJobSpanID(99999, 0, "build"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SpanID_BigEndian_MatchesOtelExplorer()
        {
            Assert.Equal("000000000000002a", OTelTracer.NewSpanID(42));
            Assert.Equal("000000000001869f", OTelTracer.NewSpanID(99999)); // workflow span for run 99999
            Assert.Equal(16, OTelTracer.NewSpanID(42).Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobSpanID_Golden()
        {
            // md5("job-99999-1-build")[:8]
            Assert.Equal("224bc2674c838206", OTelTracer.NewJobSpanID(99999, 1, "build"));
            Assert.Equal(16, OTelTracer.NewJobSpanID(99999, 1, "build").Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpanID_Golden()
        {
            // md5("step-99999-1-build-Run tests")[:8]
            Assert.Equal("8f4170e86c7435ac", OTelTracer.NewStepSpanID(99999, 1, "build", "Run tests"));
        }

        // ---- enable/disable ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsEnabled_FalseByDefault()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", null);
            OTelTracer.Reset();
            Assert.False(OTelTracer.IsEnabled);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsEnabled_TrueWhenEndpointSet()
        {
            Enable();
            Assert.True(OTelTracer.IsEnabled);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FeatureFlagOff_DisablesEvenWithEndpoint()
        {
            Enable(); // endpoint set
            OTelTracer.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com", featureEnabled: false);
            Assert.False(OTelTracer.IsEnabled);
            OTelTracer.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            Assert.Equal(0, OTelTracer.PendingSpanCountForTest);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FeatureFlagOn_WithEndpoint_Records()
        {
            Enable();
            OTelTracer.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com", featureEnabled: true);
            Assert.True(OTelTracer.IsEnabled);
            OTelTracer.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            Assert.Equal(1, OTelTracer.PendingSpanCountForTest);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Disabled_RecordsNothing()
        {
            Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", null);
            OTelTracer.Reset();
            OTelTracer.SetJobInfo("99999", "1", "build", "build", "o/r", "CI", "push", "https://github.com");
            OTelTracer.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            Assert.Equal(0, OTelTracer.PendingSpanCountForTest);
        }

        // ---- step span content + semconv ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpan_LinksToJobAndCarriesSemconv()
        {
            Enable();
            OTelTracer.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            OTelTracer.RecordStepCompletion("Run tests", 3, start, start.AddSeconds(2), TaskResult.Succeeded, "node20", "actions/checkout", "v4");

            Assert.Equal(1, OTelTracer.PendingSpanCountForTest);
            using var doc = JsonDocument.Parse(OTelTracer.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement
                .GetProperty("resourceSpans")[0]
                .GetProperty("scopeSpans")[0]
                .GetProperty("spans")[0];

            Assert.Equal("37912fcf8909bcb43fd643580e6b5ee1", span.GetProperty("traceId").GetString());
            Assert.Equal("8f4170e86c7435ac", span.GetProperty("spanId").GetString());
            Assert.Equal("224bc2674c838206", span.GetProperty("parentSpanId").GetString()); // job span
            Assert.Equal("Run tests", span.GetProperty("name").GetString());

            var attrs = ReadAttrs(span);
            Assert.Equal("step", attrs["type"]);
            Assert.Equal("runner", attrs["source"]);
            Assert.Equal("Run tests", attrs["cicd.pipeline.task.name"]);
            Assert.Equal("success", attrs["cicd.pipeline.task.run.result"]);
            Assert.Equal("8f4170e86c7435ac", attrs["cicd.pipeline.task.run.id"]);
            Assert.True(attrs.ContainsKey("cicd.pipeline.task.run.url.full"));
            Assert.Equal("https://github.com/octo/repo", attrs["vcs.repository.url.full"]);
            Assert.Equal("3", attrs["github.step_number"]); // serialized as intValue string
            Assert.Equal("actions/checkout", attrs["github.action"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpan_FailureSetsErrorAndStatus()
        {
            Enable();
            OTelTracer.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            OTelTracer.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Failed, null, null, null);

            using var doc = JsonDocument.Parse(OTelTracer.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement.GetProperty("resourceSpans")[0]
                .GetProperty("scopeSpans")[0].GetProperty("spans")[0];

            Assert.Equal(2, span.GetProperty("status").GetProperty("code").GetInt32()); // ERROR
            var attrs = ReadAttrs(span);
            Assert.Equal("failure", attrs["github.conclusion"]);
            Assert.Equal("failure", attrs["cicd.pipeline.task.run.result"]);
            Assert.Equal("failure", attrs["error.type"]);
        }

        // ---- job span ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobSpan_LinksToWorkflow()
        {
            Enable();
            OTelTracer.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            OTelTracer.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(OTelTracer.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement.GetProperty("resourceSpans")[0]
                .GetProperty("scopeSpans")[0].GetProperty("spans")[0];

            Assert.Equal("224bc2674c838206", span.GetProperty("spanId").GetString());        // job
            Assert.Equal("000000000001869f", span.GetProperty("parentSpanId").GetString());  // workflow(run_id)
            var attrs = ReadAttrs(span);
            Assert.Equal("job", attrs["type"]);
        }

        // ---- resource (runner info) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Resource_CarriesRunnerInfo()
        {
            Enable();
            OTelTracer.SetResource("my-runner", "42", "default", "2.333.0", "Linux", "X64", "host-1", ephemeral: true);
            OTelTracer.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            OTelTracer.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(OTelTracer.BuildPendingOtlpJsonForTest());
            var resourceAttrs = ReadAttrsFrom(doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("resource"));
            Assert.Equal("github-actions-runner", resourceAttrs["service.name"]);
            Assert.Equal("my-runner", resourceAttrs["github.runner.name"]);
            Assert.Equal("2.333.0", resourceAttrs["service.version"]);
            Assert.Equal("Linux", resourceAttrs["os.type"]);
            Assert.Equal("X64", resourceAttrs["host.arch"]);
            Assert.Equal("true", resourceAttrs["github.runner.ephemeral"]); // boolValue
        }

        // ---- helpers ----

        private static System.Collections.Generic.Dictionary<string, string> ReadAttrs(JsonElement span)
            => ReadAttrsFrom(span);

        private static System.Collections.Generic.Dictionary<string, string> ReadAttrsFrom(JsonElement parent)
        {
            var dict = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var a in parent.GetProperty("attributes").EnumerateArray())
            {
                var key = a.GetProperty("key").GetString();
                var val = a.GetProperty("value");
                string str = val.TryGetProperty("stringValue", out var sv) ? sv.GetString()
                    : val.TryGetProperty("intValue", out var iv) ? iv.GetString()
                    : val.TryGetProperty("boolValue", out var bv) ? (bv.GetBoolean() ? "true" : "false")
                    : null;
                dict[key] = str;
            }
            return dict;
        }
    }
}
