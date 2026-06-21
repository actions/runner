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
        private const string PropagateEnv = "ACTIONS_RUNNER_OTLP_PROPAGATE";
        private readonly string _originalEndpoint = Environment.GetEnvironmentVariable(EndpointEnv);
        private readonly string _originalPropagate = Environment.GetEnvironmentVariable(PropagateEnv);

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(EndpointEnv, _originalEndpoint);
            Environment.SetEnvironmentVariable(PropagateEnv, _originalPropagate);
        }

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
            Assert.Equal("acad1e2a107636235fd56bb742499bd0", OTelTraceExporter.NewTraceID(99999, 1));
            Assert.Equal(OTelTraceExporter.NewTraceID(99999, 1), OTelTraceExporter.NewTraceID(99999, 1));
            Assert.NotEqual(OTelTraceExporter.NewTraceID(99999, 1), OTelTraceExporter.NewTraceID(99999, 2));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TraceID_CrossLanguageSha256()
        {
            // Trace ID = leading 16 bytes of SHA-256("99999-1"); otel-explorer truncates
            // the same digest identically. (MD5 is avoided — it throws under FIPS.)
            var full = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("99999-1"));
            var expectedHex = BitConverter.ToString(full, 0, 16).Replace("-", "").ToLowerInvariant();
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
            Assert.Equal("81606d47848a59c0", OTelTraceExporter.NewJobSpanID(99999, 1, "build")); // sha256("job-99999-1-build")[:8]
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpanID_Golden()
        {
            Assert.Equal("7a4c67339b7bb8a7", OTelTraceExporter.NewStepSpanID(99999, 1, "build", 3, "Run tests")); // sha256("step-99999-1-build-3-Run tests")[:8]
        }

        // ---- inbound W3C trace context -> job span link ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Traceparent_ParsesValidAndRejectsMalformed()
        {
            Assert.True(OTelTraceExporter.TryParseTraceparent(
                "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", out var t, out var s, out var f));
            Assert.Equal("0af7651916cd43dd8448eb211c80319c", t);
            Assert.Equal("b7ad6b7169203331", s);
            Assert.Equal(1, f);
            // Unknown future version: tolerate, reading only the first four fields.
            Assert.True(OTelTraceExporter.TryParseTraceparent(
                "cc-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01-extra", out _, out _, out _));
            // Rejections.
            Assert.False(OTelTraceExporter.TryParseTraceparent(null, out _, out _, out _));
            Assert.False(OTelTraceExporter.TryParseTraceparent("garbage", out _, out _, out _));
            Assert.False(OTelTraceExporter.TryParseTraceparent( // forbidden version ff
                "ff-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", out _, out _, out _));
            Assert.False(OTelTraceExporter.TryParseTraceparent( // all-zero trace id
                "00-00000000000000000000000000000000-b7ad6b7169203331-01", out _, out _, out _));
            Assert.False(OTelTraceExporter.TryParseTraceparent( // all-zero span id
                "00-0af7651916cd43dd8448eb211c80319c-0000000000000000-01", out _, out _, out _));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobSpan_LinksToInboundParentContext()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            var prevTp = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_PARENT_TRACEPARENT");
            var prevTs = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_PARENT_TRACESTATE");
            try
            {
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_PARENT_TRACEPARENT",
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_PARENT_TRACESTATE", "arc=abc123");
                exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
                exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

                using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
                var spans = doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans");
                JsonElement job = default;
                var found = false;
                foreach (var sp in spans.EnumerateArray())
                {
                    var a = ReadAttrs(sp);
                    if (a.TryGetValue("type", out var ty) && ty == "job") { job = sp; found = true; break; }
                }
                Assert.True(found);
                // The runner's own trace stays deterministic; the upstream context is a LINK.
                Assert.Equal("acad1e2a107636235fd56bb742499bd0", job.GetProperty("traceId").GetString());
                var links = job.GetProperty("links");
                Assert.Equal(1, links.GetArrayLength());
                Assert.Equal("0af7651916cd43dd8448eb211c80319c", links[0].GetProperty("traceId").GetString());
                Assert.Equal("b7ad6b7169203331", links[0].GetProperty("spanId").GetString());
                Assert.Equal("arc=abc123", links[0].GetProperty("traceState").GetString());
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_PARENT_TRACEPARENT", prevTp);
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_PARENT_TRACESTATE", prevTs);
            }
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

            Assert.Equal("acad1e2a107636235fd56bb742499bd0", span.GetProperty("traceId").GetString());
            Assert.Equal("7a4c67339b7bb8a7", span.GetProperty("spanId").GetString());
            Assert.Equal("81606d47848a59c0", span.GetProperty("parentSpanId").GetString()); // job span
            Assert.Equal("Run tests", span.GetProperty("name").GetString());

            var attrs = ReadAttrs(span);
            Assert.Equal("step", attrs["type"]);
            Assert.False(attrs.ContainsKey("source")); // runner identified by scope, not a custom attr
            Assert.Equal("Run tests", attrs["cicd.pipeline.task.name"]);
            Assert.Equal("success", attrs["cicd.pipeline.task.run.result"]);
            Assert.Equal("7a4c67339b7bb8a7", attrs["cicd.pipeline.task.run.id"]);
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

            Assert.Equal("81606d47848a59c0", span.GetProperty("spanId").GetString());       // job
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
            Assert.Equal("my-runner", resourceAttrs["cicd.worker.name"]); // semconv (no github.runner.name dupe)
            Assert.False(resourceAttrs.ContainsKey("github.runner.name"));
            Assert.Equal("2.333.0", resourceAttrs["service.version"]);
            Assert.Equal("Linux", resourceAttrs["os.type"]);
            Assert.Equal("X64", resourceAttrs["host.arch"]);
            Assert.Equal("true", resourceAttrs["github.runner.ephemeral"]); // boolValue
            Assert.Equal("agent", resourceAttrs["cicd.system.component"]); // semconv: runner is the agent
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Resource_MergesOtelResourceAttributesEnv()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            var prev = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
            try
            {
                // Standard OTel env var (spec): comma-separated, values percent-encoded.
                // This is how ARC/Downward-API attaches k8s.* — no runner-specific env names.
                Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES",
                    "k8s.pod.name=runner-abc123,k8s.namespace.name=actions,k8s.node.name=node%2D3");
                exporter.SetResource("my-runner", "42", "default", "2.333.0", "Linux", "X64", "host-1", ephemeral: true);
                exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
                exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

                using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
                var resourceAttrs = ReadAttrsFrom(doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("resource"));
                Assert.Equal("runner-abc123", resourceAttrs["k8s.pod.name"]);
                Assert.Equal("actions", resourceAttrs["k8s.namespace.name"]);
                Assert.Equal("node-3", resourceAttrs["k8s.node.name"]); // percent-decoded
                // Explicitly-set keys win over the env var.
                Assert.Equal("github-actions-runner", resourceAttrs["service.name"]);
            }
            finally
            {
                Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", prev);
            }
        }

        // ---- enrichments (#13): vcs, actor, run url, throttling ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Span_CarriesVcsAndActorContext()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com",
                sha: "abc123", refName: "main", actor: "octocat");
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, null, null, null);

            var attrs = ReadAttrs(SpanAt(exporter, 0));
            Assert.Equal("abc123", attrs["vcs.ref.head.revision"]); // semconv name (not vcs.revision)
            Assert.Equal("main", attrs["vcs.ref.head.name"]);
            Assert.Equal("github", attrs["vcs.provider.name"]);
            Assert.Equal("octo", attrs["vcs.owner.name"]);
            Assert.Equal("repo", attrs["vcs.repository.name"]);
            Assert.Equal("octocat", attrs["github.actor"]);
            Assert.Equal("https://github.com/octo/repo/actions/runs/99999/attempts/1", attrs["cicd.pipeline.run.url.full"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SemconvResult_SkippedMapsToSkipEnum()
        {
            // semconv cicd.pipeline.task.run.result enum uses "skip", not "skipped".
            Assert.Equal("skip", OTelTraceExporter.ToSemconvResult("skipped"));
            Assert.Equal("cancellation", OTelTraceExporter.ToSemconvResult("cancelled"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Resource_HasCicdWorkerAndSchemaUrl()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetResource("my-runner", "42", "default", "2.333.0", "Linux", "X64", "host-1", ephemeral: true);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            var json = exporter.BuildPendingOtlpJsonForTest();
            Assert.Contains("https://opentelemetry.io/schemas/", json); // schema_url declared
            using var doc = JsonDocument.Parse(json);
            var rs = doc.RootElement.GetProperty("resourceSpans")[0];
            var resAttrs = ReadAttrsFrom(rs.GetProperty("resource"));
            Assert.Equal("my-runner", resAttrs["cicd.worker.name"]);
            Assert.Equal("42", resAttrs["cicd.worker.id"]);
            Assert.Equal("2.333.0", rs.GetProperty("scopeSpans")[0].GetProperty("scope").GetProperty("version").GetString());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobSpan_CarriesThrottlingDelay()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, throttlingDelayMs: 4200);

            Assert.Equal("4200", ReadAttrs(SpanAt(exporter, 0))["github.throttling_delay_ms"]);
        }

        // ---- generic child spans (#14) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecordSpan_ParentsToJobAndCarriesType()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordSpan("Resolve actions/checkout@v4", "action_download", t, t.AddSeconds(1),
                new System.Collections.Generic.Dictionary<string, string> { ["github.action"] = "actions/checkout" });

            var span = SpanAt(exporter, 0);
            Assert.Equal("81606d47848a59c0", span.GetProperty("parentSpanId").GetString()); // job span
            var attrs = ReadAttrs(span);
            Assert.Equal("action_download", attrs["type"]);
            Assert.Equal("actions/checkout", attrs["github.action"]);
        }

        // ---- trace-context propagation (#15) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepPropagationEnv_EmptyUnlessOptedIn()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc); // propagate flag not set
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            Assert.Empty(exporter.StepPropagationEnv("Run tests", 3));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepPropagationEnv_TraceparentMatchesStepSpan()
        {
            using var hc = new TestHostContext(this);
            Environment.SetEnvironmentVariable(PropagateEnv, "true");
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");

            var env = exporter.StepPropagationEnv("Run tests", 3);
            // 00-{trace}-{step span}-01, matching the step span IDs the exporter emits.
            Assert.Equal("00-acad1e2a107636235fd56bb742499bd0-7a4c67339b7bb8a7-01", env["TRACEPARENT"]);
            Assert.Equal("http://localhost:4318", env["OTEL_EXPORTER_OTLP_ENDPOINT"]);
            Assert.Contains("github.run_id=99999", env["OTEL_RESOURCE_ATTRIBUTES"]);
        }

        // ---- OTel logs (#16) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecordStepLog_CorrelatesToStepSpan()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepLog("Run tests", 3, "Error", "boom: test failed");

            Assert.Equal(1, exporter.PendingLogCountForTest);
            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpLogsJsonForTest());
            var rec = doc.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs")[0].GetProperty("logRecords")[0];
            Assert.Equal("acad1e2a107636235fd56bb742499bd0", rec.GetProperty("traceId").GetString());
            Assert.Equal("7a4c67339b7bb8a7", rec.GetProperty("spanId").GetString()); // same as step span
            Assert.Equal("ERROR", rec.GetProperty("severityText").GetString());
            Assert.Equal(17, rec.GetProperty("severityNumber").GetInt32());
            Assert.Equal("boom: test failed", rec.GetProperty("body").GetProperty("stringValue").GetString());
        }

        // ---- exception events + PR/task.type (#17/#18) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FailedStep_EmitsExceptionEvent()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Failed, null, null, null,
                errorMessage: "assertion failed: 2 != 3");

            var ev = SpanAt(exporter, 0).GetProperty("events")[0];
            Assert.Equal("exception", ev.GetProperty("name").GetString());
            var attrs = ReadAttrsFrom(ev);
            Assert.Equal("failure", attrs["exception.type"]);
            Assert.Equal("assertion failed: 2 != 3", attrs["exception.message"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TaskType_InferredFromName()
        {
            Assert.Equal("test", OTelTraceExporter.InferTaskType("Run unit tests", null));
            Assert.Equal("deploy", OTelTraceExporter.InferTaskType("Release to prod", null));
            Assert.Equal("build", OTelTraceExporter.InferTaskType("Compile", null));
            Assert.Null(OTelTraceExporter.InferTaskType("Greet", "actions/checkout"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PullRequest_EmitsVcsChangeAndBase()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "pull_request", "https://github.com",
                sha: "abc", refName: "feature", actor: "octocat", baseRef: "main", changeId: "42");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            var attrs = ReadAttrs(SpanAt(exporter, 0));
            Assert.Equal("42", attrs["vcs.change.id"]);
            Assert.Equal("main", attrs["vcs.ref.base.name"]);
        }

        // ---- metrics (#1) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobMetrics_DurationHistogramAndErrorCounter()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordJobCompletion(start, start.AddSeconds(9), TaskResult.Failed);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpMetricsJsonForTest());
            var metrics = doc.RootElement.GetProperty("resourceMetrics")[0].GetProperty("scopeMetrics")[0].GetProperty("metrics");
            var dur = metrics[0];
            Assert.Equal("cicd.pipeline.run.duration", dur.GetProperty("name").GetString());
            var dp = dur.GetProperty("histogram").GetProperty("dataPoints")[0];
            Assert.Equal(9.0, dp.GetProperty("sum").GetDouble());
            var durAttrs = ReadAttrsFrom(dp);
            Assert.Equal("failure", durAttrs["cicd.pipeline.result"]);
            Assert.Equal("CI", durAttrs["cicd.pipeline.name"]);

            var err = metrics[1];
            Assert.Equal("cicd.pipeline.run.errors", err.GetProperty("name").GetString());
            Assert.Equal("1", err.GetProperty("sum").GetProperty("dataPoints")[0].GetProperty("asInt").GetString());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobMetrics_SuccessHasNoErrorCounter()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpMetricsJsonForTest());
            var metrics = doc.RootElement.GetProperty("resourceMetrics")[0].GetProperty("scopeMetrics")[0].GetProperty("metrics");
            Assert.Equal(1, metrics.GetArrayLength()); // duration only, no errors counter
        }

        // ---- helpers ----

        private static JsonElement SpanAt(OTelTraceExporter exporter, int i)
        {
            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            return doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[i].Clone();
        }


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
