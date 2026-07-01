# Native OTel export — performance & memory benchmarks

Overhead of the native OpenTelemetry export (PR #4366). Goal: prove the
instrumentation is cheap enough to run on every job and bounded in memory.

## Micro-benchmark (emit + serialize)

`src/Test/L0/Worker/OTelTraceExporterBenchmark.cs` — reuses the exporter's internal
hooks; measures per-step record cost, end-of-job OTLP/JSON serialize cost, payload
size, and allocations. Run:

```
dotnet test src/Test/Test.csproj -c Release \
  --filter "FullyQualifiedName~OTelTraceExporterBenchmark" \
  --logger "console;verbosity=detailed"
```

Results (Release, net8.0, Apple M-series; representative):

| spans  | emit (ns/step) | alloc (B/step) | retained (B/span) | serialize | payload (B/span) |
|-------:|---------------:|---------------:|------------------:|----------:|-----------------:|
| 10     | ~6,500         | ~4,200         | ~5,700            | 0.09 ms   | 1,829            |
| 100    | ~2,800         | ~4,120         | ~4,280            | 0.83 ms   | 1,806            |
| 1,000  | ~2,300         | ~4,110         | ~4,150            | 11.2 ms   | 1,807            |
| 10,000 | ~3,500         | ~4,150         | ~3,230            | 97 ms     | 1,811            |

### Reading it
- **Emit: ~2–6 µs per step.** Negligible next to real step durations (seconds–minutes).
  Cost is 3× SHA-256 (deterministic IDs) + ~30 attribute sets per span.
- **Memory: ~4 KB allocated and retained per span**, held until the single end-of-job
  flush. Linear and freed at job end:
  - 50 steps ≈ 0.2 MB · 200 ≈ 0.8 MB · 1,000 ≈ 4 MB · **10,000 ≈ ~32 MB**
  - This is the case for a per-buffer cap (see below): a pathological job (100k
    steps/annotations) would retain hundreds of MB.
- **Serialize: ~10 µs/span** (one `Utf8JsonWriter` pass at flush). 11 ms @ 1k, ~100 ms @ 10k.
- **Payload: ~1.8 KB/span** of OTLP/JSON, gzipped and chunked into POSTs of at most
  `MaxItemsPerPost` (1,000) spans/logs each (→ ~4 MB uncompressed per request even
  at the 10k buffer cap, safely under the OTel Collector's 20 MiB default body limit).

## Macro-benchmark (job wall-time, ON vs OFF) — measured

A 50-trivial-step workflow (`run: ":"`) on the local self-hosted runner, OTel ON
(healthy collector) vs OFF (endpoint unset → exporter disabled), job wall-time from
the GitHub job `started_at`→`completed_at`:

| | run 1 | run 2 | run 3 |
|---|---|---|---|
| **OTel ON**  | 18 s | 25 s | 18 s |
| **OTel OFF** | 18 s | 25 s | 25 s |

The two distributions **fully overlap**: job-to-job variance (~7 s, from runner
orchestration/scheduling) dwarfs any OTel signal. **Caveat: n=3 per arm.** With
samples this small the honest claim is bounded, not statistical: any ON/OFF delta
is below the ~7 s orchestration noise floor, consistent with the micro numbers
(µs/step emit + a single bounded, gzipped flush POST at job end). The hard
guarantee comes from the failure-mode bounds below, not from these three runs.

## Failure modes (collector down) — measured

The job-end flush is the only place a bad collector can cost wall time. Both
failure classes are measured in L0 (run on every CI pass, not just benchmarked
once), timing a real `FlushAsync` against a real local socket:

| collector failure | test | measured job-end delay |
|---|---|---|
| Connection refused (down, NXDOMAIN-like fast-fail) | `Flush_UnreachableEndpoint_DoesNotThrow` | **< 1 s** (fail + one 200 ms retry) |
| Accepts TCP, then stalls (blackhole / slow-loris) | `Flush_HangingCollector_BoundedByOverallDeadline` | **~4 s**, asserted < 6 s |

The stall case is the worst case: nothing fast-fails, so the 4 s overall flush
deadline (`flushCts.CancelAfter(4 s)`, one deadline across all three signals
including the retry) is what bounds it — and the test asserts that bound holds
against a genuinely hung socket. DNS failure and blackholed IPs collapse to the
same two classes: either the socket op errors fast (row 1) or the cancellation
deadline fires (row 2). Flush is best-effort and swallowed — it never fails the
job; the loss is reported in one end-of-job Warning summary.

## Sampling / scale model

Decision: **server-controlled**. The runner emits every span/metric/log
unconditionally (gated only by the server feature flag + the operator's endpoint
opt-in). Sampling and metric-cardinality control are the **collector's / server's**
responsibility (tail sampling, cardinality limits, rate limiting) — not the runner's.
This keeps the runner simple, preserves tail visibility (all failures), and matches
how export is already gated. Memory is bounded at the source by the per-buffer caps
(see code: `MaxBufferedSpans/Logs/TaskMetrics`).

## Acceptable-overhead thresholds (proposed)
- Disabled (common case): job wall-time delta < 0.1 %; RSS delta within noise.
- Enabled, healthy collector: added job-end wall-time < ~100 ms.
- Enabled, collector down: added job-end wall-time ≤ ~4 s (the flush deadline;
  measured above, asserted in L0).
- Per-step emit < ~50 µs. Serialize < ~10 ms for 200 spans.
- Peak RSS delta a few KB/span (cap buffers so a runaway job can't OOM the worker).
