# OpenTelemetry deterministic ID contract

**Status**: Normative. This document is the source of truth for the deterministic
trace/span IDs the runner's native OTLP exporter emits (see
[ADR 4366](adrs/4366-native-otel-export.md)). It is owned by this repository.

Any independent consumer that reconstructs a GitHub Actions trace from the public
API (timeline / REST) MUST compute IDs by this spec to merge with runner-emitted
spans. [`stefanpenner/otel-explorer`](https://github.com/stefanpenner/otel-explorer)
is **one example consumer** of this published contract, not a byte-compatibility
obligation: this document, not that repo, defines the contract.

The reference implementation is `OTelTraceExporter` in
`src/Runner.Worker/OTelTraceExporter.cs` (functions `NewTraceID`, `NewJobSpanID`,
`NewStepSpanID`, `NewSpanIDFromString`, `NewSpanID`).

## Why deterministic IDs

Two systems describe the same run from different vantage points:

- the **runner**, post-hoc, from its `TimelineRecord`s (sub-second step timing,
  runner-host context), and
- the **GitHub API path**, which reconstructs the run/workflow span and
  server-side queue time.

Deriving every ID from stable identifiers both sides already know means the two
sets of spans **dedupe and merge into one trace** with no shared state, no
coordination, and no W3C propagation between them. The same property makes
**re-runs** trivial: each attempt is its own trace.

## Algorithm

```
id = truncate( SHA-256( UTF-8( input_string ) ), N bytes )  → lowercase hex
```

- **SHA-256**, not MD5. MD5 throws on a FIPS-enabled host; SHA-256 gives the same
  deterministic-from-`run_id` behavior. Only the **leading bytes** of the digest
  are used.
- **Trace ID**: leading **16 bytes** → **32 hex chars**.
- **Span ID**: leading **8 bytes** → **16 hex chars**.
- Hex is lowercase, zero-padded per byte (`x2`).
- `run_attempt` of `0` (or absent) is normalized to `1` before hashing.
- IDs are intentionally **non-random**, so the W3C trace-context Level-2
  randomness flag is never set.

## Input strings

| ID | Bytes | Input string (UTF-8) |
|----|-------|----------------------|
| Trace ID | 16 | `"{run_id}-{run_attempt}"` |
| Job span | 8 | `"job-{run_id}-{run_attempt}-{job_name}"` |
| Step span | 8 | `"step-{run_id}-{run_attempt}-{job_name}-{step_number}-{step_name}"` |
| Child span | 8 | `"{span_type}-{run_id}-{run_attempt}-{job_name}-{name}-{start_unix_nano}"` |

Field definitions:

- **`run_id`** — GitHub `github.run_id` (the workflow run id).
- **`run_attempt`** — `github.run_attempt`, 1-based (`0`/absent → `1`).
- **`job_name`** — the job's **display name** (matches the GitHub API `job.name`),
  not the `github.job` context key.
- **`step_number`** — the **1-based** top-level step number, matching the GitHub
  API `step.number`. It disambiguates two top-level steps that share a display
  name while keeping the ID mergeable with the API trace.
- **`step_name`** — the step's display name.
- **`span_type`**, **`name`**, **`start_unix_nano`** — for generic child spans
  (e.g. action download). The start time is included so repeated same-named
  operations don't collide. Child spans are runner-only; the API path has no
  counterpart, so they are not a merge point.

### Workflow / run span ID (API-path only)

The trace also contains a **run/workflow span** whose ID is the **big-endian
8-byte encoding of `run_id`** (`NewSpanID(run_id)`), *not* a hash. The runner
**does not emit this span** and **does not parent its job span to it** — the job
span is the trace root (`parentSpanId` omitted). The encoding is retained in the
runner and published here only so the **API path** can emit the run span into the
same trace; the runner's job span merges by sharing the trace ID.

```
run/workflow span = bigendian_uint64(run_id)   → 16 hex
```

## Trace shape

```
trace  sha256("{run_id}-{run_attempt}")[:16]
└─ (run/workflow span  bigendian(run_id))      ← emitted by the API path, not the runner
   └─ job span   sha256("job-…")[:8]            ← runner: trace ROOT (no parent)
      ├─ step span   sha256("step-…-{n}-…")[:8]
      │  └─ child span  sha256("{type}-…")[:8]
      └─ …
```

An inbound scheduler `traceparent` (if present) is attached to the job span as a
**link**, not a parent — it does not change any ID above.

### Span kinds

Per the CI/CD span conventions (cicd-spans), **task-run spans are `INTERNAL`**
and **pipeline-run spans are `SERVER`** (and carry `cicd.pipeline.result`). The
runner's job and step spans are task runs (`cicd.pipeline.task.*`), so the
runner emits them as `INTERNAL`; the `SERVER` pipeline-run span belongs to the
API path with the run/workflow span above. Span kind is therefore a structural
discriminator: `SERVER` = the run, `INTERNAL` = tasks.

## On enumerability of trace IDs

Trace IDs derive from the **public, enumerable `run_id`** (a known, guessable
property of a run). This is **acceptable for CI traces**: the data is workflow
metadata, the collector is operator-controlled, and the merge property requires a
shared, reproducible trace ID. It is a deliberate trade-off, not an oversight — do
not treat a runner trace ID as a secret.

## Conformance vectors

Golden values from the L0 suite (`src/Test/L0/Worker/OTelTraceExporterL0.cs`). A
conforming implementation MUST reproduce these exactly.

| Function | Input | Output |
|----------|-------|--------|
| Trace ID | `run_id=99999, run_attempt=1` → `sha256("99999-1")[:16]` | `acad1e2a107636235fd56bb742499bd0` |
| Job span | `99999, 1, "build"` → `sha256("job-99999-1-build")[:8]` | `81606d47848a59c0` |
| Step span | `99999, 1, "build", 3, "Run tests"` → `sha256("step-99999-1-build-3-Run tests")[:8]` | `7a4c67339b7bb8a7` |
| Run/workflow span | `bigendian(99999)` | `000000000001869f` |
| Run/workflow span | `bigendian(42)` | `000000000000002a` |

Derived check — the propagated `TRACEPARENT` for that step is
`00-acad1e2a107636235fd56bb742499bd0-7a4c67339b7bb8a7-01`
(`00-{traceId}-{stepSpanId}-01`).

Normalization checks:

- `run_attempt = 0` produces the same IDs as `run_attempt = 1`.
- The same inputs always produce the same IDs; different `run_attempt` produces a
  different trace ID.
