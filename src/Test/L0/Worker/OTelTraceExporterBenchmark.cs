using System;
using System.Diagnostics;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Worker;
using Xunit;
using Xunit.Abstractions;

namespace GitHub.Runner.Common.Tests.Worker
{
    // Micro-benchmark for the native OTel exporter's hot paths: per-step emit cost,
    // end-of-job serialize cost, payload size, and allocations. Tagged "Benchmark" so
    // it stays out of the normal L0 run. Invoke explicitly:
    //   dotnet test --filter "FullyQualifiedName~OTelTraceExporterBenchmark"
    public sealed class OTelTraceExporterBenchmark : IDisposable
    {
        private const string EndpointEnv = "ACTIONS_RUNNER_OTLP_ENDPOINT";
        private readonly string _originalEndpoint = Environment.GetEnvironmentVariable(EndpointEnv);
        private readonly ITestOutputHelper _out;

        public OTelTraceExporterBenchmark(ITestOutputHelper output) => _out = output;

        public void Dispose() => Environment.SetEnvironmentVariable(EndpointEnv, _originalEndpoint);

        private OTelTraceExporter Enabled(TestHostContext hc)
        {
            hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());
            Environment.SetEnvironmentVariable(EndpointEnv, "http://localhost:4318");
            var exporter = new OTelTraceExporter();
            exporter.Initialize(hc);
            return exporter;
        }

        [Theory]
        [Trait("Category", "Benchmark")]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void EmitAndSerialize(int n)
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Warm up JIT + secret masker + first-step allocations.
            using (var warm = new TestHostContext(this))
            {
                var w = Enabled(warm);
                w.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");
                for (var i = 0; i < 50; i++)
                {
                    w.RecordStepCompletion($"warm {i}", i, start, start.AddSeconds(1), TaskResult.Succeeded, "node20", null, null);
                }
                _ = w.BuildPendingOtlpJsonForTest();
            }

            using var hc = new TestHostContext(this);
            var exporter = Enabled(hc);
            exporter.SetJobInfo("99999", "1", "build", "build", "octo/repo", "CI", "push", "https://github.com");

            // --- emit (per-step record) ---
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var heapBefore = GC.GetTotalMemory(true);
            var allocBefore = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < n; i++)
            {
                exporter.RecordStepCompletion($"step {i}", i, start, start.AddSeconds(1), TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            }
            sw.Stop();
            var emitAllocPerStep = (GC.GetAllocatedBytesForCurrentThread() - allocBefore) / (double)n;
            var emitNsPerStep = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / n;
            var retainedPerSpan = (GC.GetTotalMemory(false) - heapBefore) / (double)n; // spans held until flush

            // --- serialize (one OTLP/JSON build over all accumulated spans) ---
            var swSer = Stopwatch.StartNew();
            var json = exporter.BuildPendingOtlpJsonForTest();
            swSer.Stop();
            var bytesPerSpan = json.Length / (double)n;

            _out.WriteLine(
                $"n={n,6} | emit {emitNsPerStep,8:F0} ns/step | emitAlloc {emitAllocPerStep,8:F0} B/step | " +
                $"retained {retainedPerSpan,8:F0} B/span | serialize {swSer.Elapsed.TotalMilliseconds,8:F2} ms | " +
                $"payload {json.Length,9} B ({bytesPerSpan,6:F0} B/span)");

            Assert.Equal(n, exporter.PendingSpanCountForTest);
        }
    }
}
