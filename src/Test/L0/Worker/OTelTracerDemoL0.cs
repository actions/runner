using System;
using System.IO;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    // Not a real test: a harness that drives the *real* OTel export code to emit a
    // representative job+steps trace, so it can be viewed in otel-explorer.
    //   OTE_DEMO=1 [OTE_DEMO_OUT=/tmp/runner-trace.json] \
    //   [ACTIONS_RUNNER_OTLP_ENDPOINT=http://localhost:4318] \
    //   dotnet test --filter FullyQualifiedName~OTelTracerDemo
    public sealed class OTelTracerDemoL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async System.Threading.Tasks.Task EmitRepresentativeTrace()
        {
            if (Environment.GetEnvironmentVariable("OTE_DEMO") != "1")
            {
                return; // disabled in normal runs
            }

            var endpoint = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT");
            var hadEndpoint = !string.IsNullOrEmpty(endpoint);
            if (!hadEndpoint)
            {
                // Need it enabled to record; point at a likely-dead endpoint, flush is best-effort.
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT", "http://localhost:4318");
            }
            OTelTracer.Reset();

            OTelTracer.SetResource(
                runnerName: "demo-runner", runnerId: "7", runnerGroup: "default",
                runnerVersion: "2.333.0", osType: "Linux", arch: "X64",
                machineName: "demo-host", ephemeral: true);

            // run 99999, attempt 1, job display name "build (ubuntu-latest)"
            OTelTracer.SetJobInfo("99999", "1", "build (ubuntu-latest)", "build",
                "octo/demo", "CI", "push", "https://github.com");

            var t = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
            void Step(string name, int n, double seconds, TaskResult r, string type, string action, string aref)
            {
                var start = t;
                var end = t.AddSeconds(seconds);
                t = end;
                OTelTracer.RecordStepCompletion(name, n, start, end, r, type, action, aref);
            }

            var jobStart = t;
            Step("Set up job", 1, 0.42, TaskResult.Succeeded, "runner", null, null);
            Step("Checkout", 2, 1.13, TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            Step("Setup Node", 3, 2.07, TaskResult.Succeeded, "node20", "actions/setup-node", "v4");
            Step("Install deps", 4, 8.55, TaskResult.Succeeded, "runner", null, null);
            Step("Run tests", 5, 12.31, TaskResult.Failed, "runner", null, null);
            Step("Post Checkout", 6, 0.22, TaskResult.Succeeded, "node20", "actions/checkout", "v4");
            OTelTracer.RecordJobCompletion(jobStart, t, TaskResult.Failed);

            var outPath = Environment.GetEnvironmentVariable("OTE_DEMO_OUT") ?? "/tmp/runner-trace.json";
            File.WriteAllText(outPath, OTelTracer.BuildPendingOtlpJsonForTest());
            Console.WriteLine($"[OTE_DEMO] wrote {OTelTracer.PendingSpanCountForTest} spans to {outPath}");

            await OTelTracer.FlushAsync(default);
            Console.WriteLine("[OTE_DEMO] flushed to OTLP endpoint (best-effort)");
        }
    }
}
