using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class OTelTraceExporterL0 : IDisposable
    {
        private const string EndpointEnv = "ACTIONS_RUNNER_OTLP_ENDPOINT";
        private readonly string _originalEndpoint = Environment.GetEnvironmentVariable(EndpointEnv);

        public void Dispose() => Environment.SetEnvironmentVariable(EndpointEnv, _originalEndpoint);

        private OTelTraceExporter Enabled(TestHostContext hc, string endpoint = "http://localhost:4318")
        {
            // An enabled exporter builds a proxy-aware HttpClientHandler via the factory.
            hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());
            Environment.SetEnvironmentVariable(EndpointEnv, endpoint);
            var exporter = new OTelTraceExporter();
            exporter.Initialize(hc);
            return exporter;
        }

        // ---- shared deterministic ID contract (golden values mirrored in otel-explorer) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TraceID_GoldenAndDeterministic()
        {
            Assert.Equal("37912fcf8909bcb43fd643580e6b5ee1", OTelTraceExporter.NewTraceID(99999, 1));
            Assert.Equal(OTelTraceExporter.NewTraceID(99999, 1), OTelTraceExporter.NewTraceID(99999, 1));
            Assert.NotEqual(OTelTraceExporter.NewTraceID(99999, 1), OTelTraceExporter.NewTraceID(99999, 2));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TraceID_CrossLanguageMD5()
        {
            var expected = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes("99999-1"));
            var expectedHex = BitConverter.ToString(expected).Replace("-", "").ToLowerInvariant();
            Assert.Equal(expectedHex, OTelTraceExporter.NewTraceID(99999, 1));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DefaultAttemptZero_TreatedAsOne()
        {
            Assert.Equal(OTelTraceExporter.NewTraceID(99999, 1), OTelTraceExporter.NewTraceID(99999, 0));
            Assert.Equal(OTelTraceExporter.NewJobSpanID(99999, 1, "build"), OTelTraceExporter.NewJobSpanID(99999, 0, "build"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SpanID_BigEndian_MatchesOtelExplorer()
        {
            Assert.Equal("000000000000002a", OTelTraceExporter.NewSpanID(42));
            Assert.Equal("000000000001869f", OTelTraceExporter.NewSpanID(99999)); // workflow span for run 99999
            Assert.Equal(16, OTelTraceExporter.NewSpanID(42).Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobSpanID_Golden()
        {
            Assert.Equal("224bc2674c838206", OTelTraceExporter.NewJobSpanID(99999, 1, "build")); // md5("job-99999-1-build")[:8]
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpanID_Golden()
        {
            Assert.Equal("8f4170e86c7435ac", OTelTraceExporter.NewStepSpanID(99999, 1, "build", "Run tests")); // md5("step-99999-1-build-Run tests")[:8]
        }

        // ---- enable / disable ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsEnabled_FalseByDefault()
        {
            using var hc = new TestHostContext(this);
            Environment.SetEnvironmentVariable(EndpointEnv, null);
            var exporter = new OTelTraceExporter();
            exporter.Initialize(hc);
            Assert.False(exporter.IsEnabled);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsEnabled_TrueWhenEndpointSet()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            Assert.True(exporter.IsEnabled);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FeatureFlagOff_DisablesEvenWithEndpoint()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com", featureEnabled: false);
            Assert.False(exporter.IsEnabled);
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            Assert.Equal(0, exporter.PendingSpanCountForTest);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Disabled_RecordsNothing()
        {
            using var hc = new TestHostContext(this);
            Environment.SetEnvironmentVariable(EndpointEnv, null);
            var exporter = new OTelTraceExporter();
            exporter.Initialize(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            Assert.Equal(0, exporter.PendingSpanCountForTest);
        }

        // ---- step span content + semconv ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpan_LinksToJobAndCarriesSemconv()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordStepCompletion("Run tests", 3, start, start.AddSeconds(2), TaskResult.Succeeded, "node20", "actions/checkout", "v4");

            Assert.Equal(1, exporter.PendingSpanCountForTest);
            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0];

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
            Assert.Equal("3", attrs["github.step_number"]);
            Assert.Equal("actions/checkout", attrs["github.action"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpan_FailureSetsErrorAndStatus()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Failed, null, null, null);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0];

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
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0];

            Assert.Equal("224bc2674c838206", span.GetProperty("spanId").GetString());       // job
            Assert.Equal("000000000001869f", span.GetProperty("parentSpanId").GetString()); // workflow(run_id)
            Assert.Equal("job", ReadAttrs(span)["type"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EmbeddedStep_IsNotRecorded()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            // Composite/embedded sub-steps would collide by name and have no API counterpart.
            exporter.RecordStepCompletion("Build", 1, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, null, null, null, isEmbedded: true);
            Assert.Equal(0, exporter.PendingSpanCountForTest);

            // ... but a top-level step with the same name still records.
            exporter.RecordStepCompletion("Build", 1, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, null, null, null, isEmbedded: false);
            Assert.Equal(1, exporter.PendingSpanCountForTest);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Json_ControlCharsInName_ProducesValidJson()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            // A raw control char + quote + backslash must not break the batch.
            exporter.RecordStepCompletion("step\u0001name", 1, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, null, null, null);

            // Parses without throwing == valid JSON, and round-trips the name.
            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0];
            Assert.Equal("step\u0001name", span.GetProperty("name").GetString());
        }

        // ---- best-effort resilience ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async System.Threading.Tasks.Task Flush_UnreachableEndpoint_DoesNotThrow()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc, "http://127.0.0.1:0");
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, null, null, null);

            var ex = await Record.ExceptionAsync(() => exporter.FlushAsync(default));
            Assert.Null(ex);
        }

        // ---- secret masking (uses the host's real SecretMasker) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SecretMasker_ScrubsSpanStrings()
        {
            using var hc = new TestHostContext(this);
            hc.SecretMasker.AddValue("s3cr3t");
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepCompletion("deploy s3cr3t", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, null, "octo/deploy s3cr3t", null);

            var json = exporter.BuildPendingOtlpJsonForTest();
            Assert.DoesNotContain("s3cr3t", json);
            Assert.Contains("***", json);
        }

        // ---- resource (runner info) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Resource_CarriesRunnerInfo()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetResource("my-runner", "42", "default", "2.333.0", "Linux", "X64", "host-1", ephemeral: true);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            var resourceAttrs = ReadAttrsFrom(doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("resource"));
            Assert.Equal("github-actions-runner", resourceAttrs["service.name"]);
            Assert.Equal("my-runner", resourceAttrs["github.runner.name"]);
            Assert.Equal("2.333.0", resourceAttrs["service.version"]);
            Assert.Equal("Linux", resourceAttrs["os.type"]);
            Assert.Equal("X64", resourceAttrs["host.arch"]);
            Assert.Equal("true", resourceAttrs["github.runner.ephemeral"]); // boolValue
        }

        // ---- helpers ----

        private static System.Collections.Generic.Dictionary<string, string> ReadAttrs(JsonElement span) => ReadAttrsFrom(span);

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
