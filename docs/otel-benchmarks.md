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
- **Payload: ~1.8 KB/span** of OTLP/JSON, sent as one POST per signal (→ ~18 MB for
  10k spans, uncompressed — motivates gzip + chunking).

## Macro-benchmark (job wall-time, ON vs OFF) — plan

Run a workflow of N trivial steps (`run: ":"`) on the local self-hosted runner with
`ACTIONS_RUNNER_OTLP_ENDPOINT` (a) unset, (b) → fast collector, (c) → dead port; ≥5
reps, compare median **added job wall-time** (from runner `_diag` job span timestamps),
**peak RSS** (`/usr/bin/time -l`, bytes on macOS), and **CPU** (user+sys).

Expected from the micro numbers: emit adds < 1 ms even at 200 steps; the only real
wall-time is the flush POST(s) at job end (≤ 2 s/signal cap, ~6 s worst case only when
the collector is unreachable — best-effort, swallowed).

## Acceptable-overhead thresholds (proposed)
- Disabled (common case): job wall-time delta < 0.1 %; RSS delta within noise.
- Enabled, healthy collector: added job-end wall-time < ~100 ms.
- Per-step emit < ~50 µs. Serialize < ~10 ms for 200 spans.
- Peak RSS delta a few KB/span (cap buffers so a runaway job can't OOM the worker).
