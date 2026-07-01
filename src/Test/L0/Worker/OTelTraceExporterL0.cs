using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class OTelTraceExporterL0 : IDisposable
    {
        private const string EndpointEnv = "ACTIONS_RUNNER_OTLP_ENDPOINT";
        private const string PropagateEnv = "ACTIONS_RUNNER_OTLP_PROPAGATE";
        private const string HeadersEnv = "ACTIONS_RUNNER_OTLP_HEADERS";
        private readonly string _originalEndpoint = Environment.GetEnvironmentVariable(EndpointEnv);
        private readonly string _originalPropagate = Environment.GetEnvironmentVariable(PropagateEnv);
        private readonly string _originalHeaders = Environment.GetEnvironmentVariable(HeadersEnv);

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(EndpointEnv, _originalEndpoint);
            Environment.SetEnvironmentVariable(PropagateEnv, _originalPropagate);
            Environment.SetEnvironmentVariable(HeadersEnv, _originalHeaders);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepPropagationEnv_DoesNotLeakAuthHeaders()
        {
            Environment.SetEnvironmentVariable(HeadersEnv, "authorization=Bearer super-secret-token");
            Environment.SetEnvironmentVariable(PropagateEnv, "true");
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");

            var env = exporter.StepPropagationEnv("Build", 1);

            // The trace context + endpoint are safe to hand to step processes.
            Assert.True(env.ContainsKey("TRACEPARENT"));
            Assert.True(env.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT"));
            // The collector auth header is a credential and must NOT reach user step env.
            Assert.False(env.ContainsKey("OTEL_EXPORTER_OTLP_HEADERS"));
            Assert.DoesNotContain(env.Values, v => v != null && v.Contains("super-secret-token"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Initialize_ScrubsOtlpHeadersFromProcessEnv()
        {
            // Host step processes inherit the worker's env (ProcessInvoker copies it),
            // so leaving the raw collector credential in the process env would hand it
            // to every untrusted step — the exact leak StepPropagationEnv guards against.
            Environment.SetEnvironmentVariable(HeadersEnv, "authorization=Bearer super-secret-token");
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            Assert.Null(Environment.GetEnvironmentVariable(HeadersEnv));
            // ... and the exporter still captured the header before scrubbing.
            Assert.True(exporter.IsEnabled);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Initialize_ScrubsOtlpHeaders_EvenWhenExportDisabled()
        {
            // A credential configured without an endpoint is inert for export but
            // still a credential — it must not linger for step processes to inherit.
            Environment.SetEnvironmentVariable(EndpointEnv, null);
            Environment.SetEnvironmentVariable(HeadersEnv, "authorization=Bearer super-secret-token");
            using var hc = new TestHostContext(this);
            var exporter = new OTelTraceExporter();
            exporter.Initialize(hc);
            Assert.Null(Environment.GetEnvironmentVariable(HeadersEnv));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Metrics_CarryRunAttempt()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "2", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordStepCompletion("Build", 1, t, t.AddSeconds(2), TaskResult.Succeeded, "node20", null, null);
            exporter.RecordJobCompletion(t, t.AddSeconds(9), TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpMetricsJsonForTest());
            var metrics = doc.RootElement.GetProperty("resourceMetrics")[0].GetProperty("scopeMetrics")[0].GetProperty("metrics");
            foreach (var m in metrics.EnumerateArray())
            {
                var name = m.GetProperty("name").GetString();
                var pts = m.TryGetProperty("histogram", out var h) ? h.GetProperty("dataPoints")
                        : m.GetProperty("sum").GetProperty("dataPoints");
                foreach (var dp in pts.EnumerateArray())
                {
                    var a = ReadAttrsFrom(dp);
                    Assert.True(a.ContainsKey("github.run_attempt"), $"{name} missing run.attempt");
                    Assert.Equal("2", a["github.run_attempt"]);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DurationHistogram_HasExplicitBucketsNotEmptyBounds()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordJobCompletion(t, t.AddSeconds(9), TaskResult.Succeeded); // 9s -> (5,10] bucket

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpMetricsJsonForTest());
            var metrics = doc.RootElement.GetProperty("resourceMetrics")[0].GetProperty("scopeMetrics")[0].GetProperty("metrics");
            Assert.Equal("github.pipeline.run.duration", metrics[0].GetProperty("name").GetString());
            var dp = metrics[0].GetProperty("histogram").GetProperty("dataPoints")[0];
            var bounds = dp.GetProperty("explicitBounds");
            var buckets = dp.GetProperty("bucketCounts");
            Assert.True(bounds.GetArrayLength() > 0, "duration histogram must have explicit bounds (not empty)");
            Assert.Equal(bounds.GetArrayLength() + 1, buckets.GetArrayLength()); // OTLP histogram invariant
            var total = 0;
            foreach (var b in buckets.EnumerateArray()) total += int.Parse(b.GetString());
            Assert.Equal(1, total);                                   // the single observation, bucketed
            Assert.Equal(9.0, dp.GetProperty("sum").GetDouble());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Buffers_AreCappedToBoundMemory()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var over = OTelTraceExporter.MaxBufferedSpans + 50;
            for (var i = 0; i < over; i++)
            {
                exporter.RecordStepCompletion($"step {i}", i, t, t.AddSeconds(1), TaskResult.Succeeded, "node20", null, null);
            }
            // A runaway job can't grow the buffer past the cap; excess is dropped + counted.
            Assert.Equal(OTelTraceExporter.MaxBufferedSpans, exporter.PendingSpanCountForTest);
            Assert.True(exporter.DroppedSpanCountForTest >= 50, $"expected >=50 dropped, got {exporter.DroppedSpanCountForTest}");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobRootSpan_IsNeverDroppedAtBufferCap()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < OTelTraceExporter.MaxBufferedSpans; i++)
            {
                exporter.RecordStepCompletion($"step {i}", i, t, t.AddSeconds(1), TaskResult.Succeeded, "node20", null, null);
            }
            Assert.Equal(OTelTraceExporter.MaxBufferedSpans, exporter.PendingSpanCountForTest);

            // The job span is the trace ROOT and is recorded LAST. If the cap evicted
            // it, every exported step span would parent to a span that never arrives.
            exporter.RecordJobCompletion(t, t.AddSeconds(9), TaskResult.Succeeded);

            Assert.Equal(OTelTraceExporter.MaxBufferedSpans + 1, exporter.PendingSpanCountForTest);
            Assert.Contains("\"spanId\":\"81606d47848a59c0\"", exporter.BuildPendingOtlpJsonForTest()); // job root present
        }

        // Captures OTLP POSTs at the HttpClientHandler layer — exactly what the
        // exporter's transport puts on the wire.
        private sealed class CapturingClientHandler : HttpClientHandler
        {
            public readonly List<(Uri Uri, byte[] Body)> Requests = new();

            protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Requests.Add((request.RequestUri, await request.Content.ReadAsByteArrayAsync(ct)));
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            }
        }

        private static string GunzipString(byte[] body)
        {
            using var ms = new System.IO.MemoryStream(body);
            using var gs = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
            using var sr = new System.IO.StreamReader(gs);
            return sr.ReadToEnd();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async System.Threading.Tasks.Task Flush_ChunksOversizedSignalsAcrossPosts()
        {
            using var hc = new TestHostContext(this);
            var handler = new CapturingClientHandler();
            var factory = new Mock<IHttpClientHandlerFactory>();
            factory.Setup(f => f.CreateClientHandler(It.IsAny<RunnerWebProxy>())).Returns(handler);
            hc.SetSingleton<IHttpClientHandlerFactory>(factory.Object);
            Environment.SetEnvironmentVariable(EndpointEnv, "http://localhost:4318");
            var exporter = new OTelTraceExporter();
            exporter.Initialize(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var total = OTelTraceExporter.MaxItemsPerPost + 1;
            for (var i = 0; i < total; i++)
            {
                exporter.RecordStepCompletion($"step {i}", i, t, t.AddSeconds(1), TaskResult.Succeeded, "node20", null, null);
            }

            await exporter.FlushAsync(default);

            // One giant POST can exceed a collector's body limit (413, non-retryable ->
            // the whole signal silently dropped); bounded chunks keep every request small.
            var tracePosts = handler.Requests.Where(r => r.Uri.AbsolutePath == "/v1/traces").ToList();
            Assert.Equal(2, tracePosts.Count);
            var counts = tracePosts.Select(r =>
            {
                using var doc = JsonDocument.Parse(GunzipString(r.Body));
                return doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans").GetArrayLength();
            }).ToList();
            Assert.Equal(total, counts.Sum());                                       // nothing lost across chunks
            Assert.All(counts, c => Assert.InRange(c, 1, OTelTraceExporter.MaxItemsPerPost));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GzipUtf8_RoundTripsAndCompresses()
        {
            // Repetitive payload like real OTLP/JSON spans.
            var json = string.Concat(System.Linq.Enumerable.Repeat(
                "{\"key\":\"cicd.pipeline.task.name\",\"value\":{\"stringValue\":\"Build\"}},", 500));
            var gz = OTelHttpTransport.GzipUtf8(json);

            Assert.True(gz.Length < System.Text.Encoding.UTF8.GetByteCount(json) / 2, "expected >2x compression");
            using var ms = new System.IO.MemoryStream(gz);
            using var gs = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
            using var sr = new System.IO.StreamReader(gs);
            Assert.Equal(json, sr.ReadToEnd());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecordSpan_HonorsClientSpanKind()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordSpan("pull image: alpine", "container", t, t.AddSeconds(1), null, spanKind: 3); // CLIENT
            Assert.Equal(3, SpanAt(exporter, 0).GetProperty("kind").GetInt32());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Attributes_SerializeWithCorrectAnyValueWireTypes()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordSpan("pull image: alpine", "container", t, t.AddSeconds(1), new Dictionary<string, object>
            {
                ["d"] = 0.75,           // double -> doubleValue, NOT stringValue
                ["f"] = 0.5f,           // float -> doubleValue
                ["b"] = true,
                ["l"] = 42L,
                ["s"] = "text",
                ["arr"] = new object[] { "a", 1L, 2.5 }, // array -> arrayValue with typed elements
            });

            var raw = new Dictionary<string, JsonElement>();
            foreach (var a in SpanAt(exporter, 0).GetProperty("attributes").EnumerateArray())
            {
                raw[a.GetProperty("key").GetString()] = a.GetProperty("value").Clone();
            }

            // OTLP AnyValue: each CLR type must land on its own wire type — a double
            // silently coerced to stringValue breaks numeric queries downstream.
            Assert.Equal(0.75, raw["d"].GetProperty("doubleValue").GetDouble());
            Assert.Equal(0.5, raw["f"].GetProperty("doubleValue").GetDouble());
            Assert.True(raw["b"].GetProperty("boolValue").GetBoolean());
            Assert.Equal("42", raw["l"].GetProperty("intValue").GetString());
            Assert.Equal("text", raw["s"].GetProperty("stringValue").GetString());
            var values = raw["arr"].GetProperty("arrayValue").GetProperty("values");
            Assert.Equal(3, values.GetArrayLength());
            Assert.Equal("a", values[0].GetProperty("stringValue").GetString());
            Assert.Equal("1", values[1].GetProperty("intValue").GetString());
            Assert.Equal(2.5, values[2].GetProperty("doubleValue").GetDouble());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DescribePartialSuccess_FlagsRejectedItems()
        {
            Assert.Null(OTelHttpTransport.DescribePartialSuccess("{}"));
            Assert.Null(OTelHttpTransport.DescribePartialSuccess("{\"partialSuccess\":{}}"));
            var s = OTelHttpTransport.DescribePartialSuccess("{\"partialSuccess\":{\"rejectedSpans\":\"3\",\"errorMessage\":\"bad batch\"}}");
            Assert.NotNull(s);
            Assert.Contains("3 rejected", s);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Logs_HaveSchemaUrlAndObservedTime()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepLog("Run tests", 3, "Error", "boom");

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpLogsJsonForTest());
            var scopeLogs = doc.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs")[0];
            // 1.34.0 is the earliest semconv release defining every emitted attribute
            // (vcs.provider.name, cicd.worker.*, cicd.pipeline.task.run.result, ...);
            // declaring an older schema would misdirect schema-aware transformations.
            Assert.Equal("https://opentelemetry.io/schemas/1.34.0", scopeLogs.GetProperty("schemaUrl").GetString());
            var rec = scopeLogs.GetProperty("logRecords")[0];
            Assert.True(rec.TryGetProperty("observedTimeUnixNano", out _));
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
                    if (a.TryGetValue("github.record_type", out var ty) && ty == "job") { job = sp; found = true; break; }
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
            Assert.Equal("step", attrs["github.record_type"]);
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
        public void JobSpan_IsRoot_NoDanglingParent()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            var span = doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0];

            Assert.Equal("81606d47848a59c0", span.GetProperty("spanId").GetString());       // job
            // The job is the root of the runner's trace: no parentSpanId pointing at a
            // workflow/run span the runner never emits (that was a dangling parent).
            Assert.False(span.TryGetProperty("parentSpanId", out _));
            Assert.Equal("job", ReadAttrs(span)["github.record_type"]);
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
            // Failure is not silent: one end-of-job summary (logged at Warning) says
            // what was lost, so a fleet operator can grep it instead of correlating
            // per-signal Info lines across _diag files.
            Assert.NotNull(exporter.LastFlushSummaryForTest);
            Assert.Contains("failed", exporter.LastFlushSummaryForTest);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExportSummary_NullWhenCleanAndDescriptiveOnLoss()
        {
            // Clean flush -> no summary line at all.
            Assert.Null(OTelTraceExporter.BuildExportSummary(3, 0, 0, 0, 0));
            // Failed POSTs and cap-drops each produce the single summary.
            var s = OTelTraceExporter.BuildExportSummary(3, 2, 7, 0, 1);
            Assert.Contains("2/3", s);
            Assert.Contains("7 span(s)", s);
            Assert.Contains("1 task-metric(s)", s);
            Assert.NotNull(OTelTraceExporter.BuildExportSummary(3, 0, 0, 5, 0)); // drops alone warn too
        }

        // ---- TLS: custom CA trust (the safe primitive for self-signed collectors) ----

        private static System.Security.Cryptography.X509Certificates.X509Certificate2 NewSelfSignedCert(string cn)
        {
            using var rsa = System.Security.Cryptography.RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                $"CN={cn}", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CustomCaTrust_AcceptsOnlyCertsFromTheBundle()
        {
            using var collectorCert = NewSelfSignedCert("collector.internal");
            using var otherCert = NewSelfSignedCert("mitm.example");
            var trusted = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection { collectorCert };

            // The bundled CA (here: the self-signed collector cert itself) validates ...
            Assert.True(OTelTraceExporter.ValidateWithCustomTrustRoots(collectorCert, trusted));
            // ... any other cert — e.g. a MITM's — does not.
            Assert.False(OTelTraceExporter.ValidateWithCustomTrustRoots(otherCert, trusted));
            Assert.False(OTelTraceExporter.ValidateWithCustomTrustRoots(null, trusted));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Initialize_LoadsCustomCaBundle()
        {
            var caFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"otel-ca-{Guid.NewGuid():N}.pem");
            var prev = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_OTLP_CERTIFICATE");
            try
            {
                using var cert = NewSelfSignedCert("collector.internal");
                System.IO.File.WriteAllText(caFile, cert.ExportCertificatePem());
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_CERTIFICATE", caFile);
                using var hc = new TestHostContext(this);
                var exporter = Enabled(hc);
                Assert.True(exporter.IsEnabled); // a valid PEM CA bundle loads cleanly
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_CERTIFICATE", prev);
                System.IO.File.Delete(caFile);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Initialize_Failure_DisablesExportInsteadOfThrowing()
        {
            // Telemetry must never throw into the job path: Initialize runs before
            // JobRunner's try block, so a failure here must disable export, not escape.
            using var hc = new TestHostContext(this);
            var factory = new Mock<IHttpClientHandlerFactory>();
            factory.Setup(f => f.CreateClientHandler(It.IsAny<RunnerWebProxy>()))
                .Throws(new InvalidOperationException("boom"));
            hc.SetSingleton<IHttpClientHandlerFactory>(factory.Object);
            Environment.SetEnvironmentVariable(EndpointEnv, "http://localhost:4318");
            Environment.SetEnvironmentVariable(PropagateEnv, "true");

            var exporter = new OTelTraceExporter();
            Assert.Null(Record.Exception(() => exporter.Initialize(hc)));
            Assert.False(exporter.IsEnabled);

            // Every later hook is a safe no-op on the disabled exporter.
            Assert.Null(Record.Exception(() =>
            {
                exporter.SetResource("my-runner", "42", "default", "2.333.0", "Linux", "X64", "host-1", ephemeral: false);
                exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
                exporter.RecordStepCompletion("Build", 1, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded, "node20", null, null);
            }));
            Assert.Empty(exporter.StepPropagationEnv("Build", 1));
            Assert.Equal(0, exporter.PendingSpanCountForTest);
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
            Assert.Equal("linux", resourceAttrs["os.type"]);   // semconv enum value
            Assert.Equal("amd64", resourceAttrs["host.arch"]); // semconv enum value
            Assert.Equal("true", resourceAttrs["github.runner.ephemeral"]); // boolValue
            Assert.Equal("agent", resourceAttrs["cicd.system.component"]); // semconv: runner is the agent
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Resource_OsTypeAndHostArch_UseSemconvEnumValues()
        {
            // semconv os.type enum is lowercase and has no "macOS" (Apple is "darwin");
            // host.arch enum is amd64|arm32|arm64|x86 — not the runner's "X64"/"ARM" names.
            Assert.Equal("linux", OTelTraceExporter.ToSemconvOsType("Linux"));
            Assert.Equal("darwin", OTelTraceExporter.ToSemconvOsType("macOS"));
            Assert.Equal("windows", OTelTraceExporter.ToSemconvOsType("Windows"));
            Assert.Equal("amd64", OTelTraceExporter.ToSemconvHostArch("X64"));
            Assert.Equal("x86", OTelTraceExporter.ToSemconvHostArch("X86"));
            Assert.Equal("arm32", OTelTraceExporter.ToSemconvHostArch("ARM"));
            Assert.Equal("arm64", OTelTraceExporter.ToSemconvHostArch("ARM64"));

            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetResource("my-runner", "42", "default", "2.333.0", "macOS", "ARM64", "host-1", ephemeral: false);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobCompletion(DateTime.UtcNow, DateTime.UtcNow, TaskResult.Succeeded);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpJsonForTest());
            var resourceAttrs = ReadAttrsFrom(doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("resource"));
            Assert.Equal("darwin", resourceAttrs["os.type"]);
            Assert.Equal("arm64", resourceAttrs["host.arch"]);
            Assert.Equal("macOS", resourceAttrs["os.name"]); // raw value preserved (os.name is free-form)
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
                new System.Collections.Generic.Dictionary<string, object>
                {
                    ["github.action"] = "actions/checkout",
                    ["cicd.pipeline.task.run.result"] = "success",
                });

            var span = SpanAt(exporter, 0);
            Assert.Equal("81606d47848a59c0", span.GetProperty("parentSpanId").GetString()); // job span (no parent step given)
            var attrs = ReadAttrs(span);
            Assert.Equal("action_download", attrs["github.record_type"]);
            Assert.Equal("actions/checkout", attrs["github.action"]);
            // Action-resolution spans now carry a result like every other task span.
            Assert.Equal("success", attrs["cicd.pipeline.task.run.result"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecordSpan_ParentsToStep_WhenStepGiven()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // Action resolution happens during "Set up job", so the span nests under that step.
            exporter.RecordSpan("Resolve actions/checkout@v4", "action_download", t, t.AddSeconds(1),
                new System.Collections.Generic.Dictionary<string, object> { ["github.action"] = "actions/checkout" },
                parentStepName: "Set up job", parentStepNumber: 1);

            var span = SpanAt(exporter, 0);
            var expected = OTelTraceExporter.NewStepSpanID(99999, 1, "build", 1, "Set up job");
            Assert.Equal(expected, span.GetProperty("parentSpanId").GetString());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecordSpan_ProcessExitCode_IsSemconvIntAttribute()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // Same shape as the ScriptHandler/ContainerOperationProvider hook sites:
            // the registered semconv key is process.exit.code (dots, type int) — a
            // negative exit code must serialize culture-invariantly as an intValue.
            exporter.RecordSpan("process: bash", "process", t, t.AddSeconds(1),
                new System.Collections.Generic.Dictionary<string, object>
                {
                    ["process.executable.name"] = "bash",
                    ["process.exit.code"] = (long)-1,
                });

            var span = SpanAt(exporter, 0);
            var found = false;
            foreach (var a in span.GetProperty("attributes").EnumerateArray())
            {
                if (a.GetProperty("key").GetString() == "process.exit.code")
                {
                    found = true;
                    // OTLP/JSON int64 is a string-encoded intValue, never a stringValue.
                    Assert.Equal("-1", a.GetProperty("value").GetProperty("intValue").GetString());
                    Assert.False(a.GetProperty("value").TryGetProperty("stringValue", out _));
                }
            }
            Assert.True(found, "expected process.exit.code attribute");
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StripUserInfo_RemovesUrlCredentials()
        {
            Assert.Equal("http://localhost:4318", OTelTraceExporter.StripUserInfo("http://localhost:4318"));
            Assert.Equal("http://localhost:4318", OTelTraceExporter.StripUserInfo("http://user:t0ps3cret@localhost:4318"));
            Assert.Equal("https://otlp.example.com/custom", OTelTraceExporter.StripUserInfo("https://user@otlp.example.com/custom"));
            Assert.Equal("not a url", OTelTraceExporter.StripUserInfo("not a url")); // pass-through
            Assert.Null(OTelTraceExporter.StripUserInfo(null));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepPropagationEnv_StripsEndpointUserinfo()
        {
            // Basic-auth-in-URL collectors put a credential in the endpoint itself;
            // it must never reach untrusted step processes (headers are already withheld).
            Environment.SetEnvironmentVariable(PropagateEnv, "true");
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc, "http://runner:t0ps3cret@localhost:4318");
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");

            var env = exporter.StepPropagationEnv("Run tests", 3);
            Assert.Equal("http://localhost:4318", env["OTEL_EXPORTER_OTLP_ENDPOINT"]);
            Assert.DoesNotContain(env.Values, v => v != null && v.Contains("t0ps3cret"));

            // ... and the credential is registered with the masker so the endpoint
            // can never appear raw in diag/transport logs.
            Assert.DoesNotContain("t0ps3cret", hc.SecretMasker.MaskSecrets("posting to http://runner:t0ps3cret@localhost:4318/v1/traces"));
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StepSpan_CarriesActionStage()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordStepCompletion("Post Checkout", 14, t, t.AddSeconds(1), TaskResult.Succeeded, "node20", "actions/checkout", "v4", stepStage: "Post");

            Assert.Equal("Post", ReadAttrs(SpanAt(exporter, 0))["github.action_stage"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecordJobLog_CorrelatesToJobSpan()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordJobLog("Warning", "the runner is shutting down");

            Assert.Equal(1, exporter.PendingLogCountForTest);
            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpLogsJsonForTest());
            var rec = doc.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs")[0].GetProperty("logRecords")[0];
            Assert.Equal("81606d47848a59c0", rec.GetProperty("spanId").GetString()); // same as the job span
            Assert.Equal("WARNING", rec.GetProperty("severityText").GetString());
            Assert.Equal("the runner is shutting down", rec.GetProperty("body").GetProperty("stringValue").GetString());
        }

        // ---- exception events + PR/task.type (#17/#18) ----

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FailedStep_EmitsExceptionEvent_AndStatusMessage()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Failed, null, null, null,
                errorMessage: "assertion failed: 2 != 3");

            var span = SpanAt(exporter, 0);
            var ev = span.GetProperty("events")[0];
            Assert.Equal("exception", ev.GetProperty("name").GetString());
            var attrs = ReadAttrsFrom(ev);
            // A CI conclusion is not an exception class; semconv exception.type must be
            // a real type or omitted (message-only exception events are valid).
            Assert.False(attrs.ContainsKey("exception.type"));
            Assert.Equal("assertion failed: 2 != 3", attrs["exception.message"]);
            // Recording Errors guidance: the error description rides on span status.
            var status = span.GetProperty("status");
            Assert.Equal(2, status.GetProperty("code").GetInt32());
            Assert.Equal("assertion failed: 2 != 3", status.GetProperty("message").GetString());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FailedStep_WithoutErrorMessage_HasNoEmptyExceptionEvent()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            exporter.RecordStepCompletion("Run tests", 3, DateTime.UtcNow, DateTime.UtcNow, TaskResult.Failed, null, null, null);

            var span = SpanAt(exporter, 0);
            // An exception event with neither type nor message is invalid semconv.
            Assert.False(span.TryGetProperty("events", out _));
            var status = span.GetProperty("status");
            Assert.Equal(2, status.GetProperty("code").GetInt32());
            Assert.False(status.TryGetProperty("message", out _));
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JobAndStepSpans_AreInternalTaskRuns()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordStepCompletion("Build", 1, t, t.AddSeconds(2), TaskResult.Succeeded, "node20", null, null);
            exporter.RecordJobCompletion(t, t.AddSeconds(9), TaskResult.Succeeded);

            // cicd-spans: task-run spans SHOULD be INTERNAL. Both the job and its steps
            // carry cicd.pipeline.task.* attributes — they are task runs; the SERVER
            // pipeline-run span belongs to the API-side consumer, not the runner.
            Assert.Equal(1, SpanAt(exporter, 0).GetProperty("kind").GetInt32()); // step
            Assert.Equal(1, SpanAt(exporter, 1).GetProperty("kind").GetInt32()); // job
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
            Assert.Equal("github.pipeline.run.duration", dur.GetProperty("name").GetString());
            var dp = dur.GetProperty("histogram").GetProperty("dataPoints")[0];
            Assert.Equal(9.0, dp.GetProperty("sum").GetDouble());
            var durAttrs = ReadAttrsFrom(dp);
            Assert.Equal("failure", durAttrs["cicd.pipeline.result"]);
            Assert.Equal("CI", durAttrs["cicd.pipeline.name"]);

            var err = metrics[1];
            Assert.Equal("github.pipeline.run.errors", err.GetProperty("name").GetString());
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
            // run.duration + the job's task.duration (no errors counter on success).
            Assert.Equal(2, metrics.GetArrayLength());
            Assert.Equal("github.pipeline.run.duration", metrics[0].GetProperty("name").GetString());
            Assert.Equal("github.pipeline.task.duration", metrics[1].GetProperty("name").GetString());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TaskMetrics_DurationPerStepAndJob()
        {
            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
            var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            exporter.RecordStepCompletion("Build", 1, t, t.AddSeconds(2), TaskResult.Succeeded, "node20", null, null);
            exporter.RecordStepCompletion("Unit tests", 2, t.AddSeconds(2), t.AddSeconds(5), TaskResult.Failed, "node20", null, null);
            exporter.RecordJobCompletion(t, t.AddSeconds(9), TaskResult.Failed);

            using var doc = JsonDocument.Parse(exporter.BuildPendingOtlpMetricsJsonForTest());
            var metrics = doc.RootElement.GetProperty("resourceMetrics")[0].GetProperty("scopeMetrics")[0].GetProperty("metrics");
            JsonElement taskDur = default; var found = false;
            foreach (var m in metrics.EnumerateArray())
            {
                if (m.GetProperty("name").GetString() == "github.pipeline.task.duration") { taskDur = m; found = true; }
            }
            Assert.True(found, "expected github.pipeline.task.duration metric");

            var byName = new System.Collections.Generic.Dictionary<string, (string type, string result, double sum)>();
            foreach (var dp in taskDur.GetProperty("histogram").GetProperty("dataPoints").EnumerateArray())
            {
                var a = ReadAttrsFrom(dp);
                byName[a["cicd.pipeline.task.name"]] = (a["github.record_type"], a["cicd.pipeline.task.run.result"], dp.GetProperty("sum").GetDouble());
            }
            Assert.Equal(3, byName.Count);                       // 2 steps + the job
            Assert.Equal(("step", "success", 2.0), byName["Build"]);
            Assert.Equal(("step", "failure", 3.0), byName["Unit tests"]);
            Assert.Equal(("job", "failure", 9.0), byName["build"]);
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
