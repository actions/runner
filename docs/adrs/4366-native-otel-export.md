# ADR 4366: Native OpenTelemetry export

**Date**: 2026-06-17 (revised 2026-06-28)

**Status**: Accepted

## Context

The runner already collects the timing and result data the GitHub Actions
timeline API exposes (job and per-step start/finish/result via `TimelineRecord`).
Operators who want this data in their own observability stack today have to poll
the timeline/REST API after the fact and translate it to OpenTelemetry out of
band — post-hoc, second-granularity, and without runner-host context.

We want the runner to emit this telemetry **natively as OpenTelemetry**, in real
time, with sub-second precision, so it can be sent straight to any OTLP collector
(the OpenTelemetry Collector, Grafana/Tempo, Jaeger, Honeycomb, etc.).

## Decision

Add an opt-in, best-effort **OTLP/HTTP exporter** — `IOTelTraceExporter`
(`OTelTraceExporter`, a `RunnerService` resolved via `HostContext.GetService<>()`)
— implemented entirely with the .NET base class library (`System.Text.Json`,
`System.Security.Cryptography`, `System.Net.Http`). It emits all three OTLP
signals for each job the runner executes.

### Signals

| Signal  | Endpoint      | Payload |
|---------|---------------|---------|
| Traces  | `/v1/traces`  | One **job span** (trace root) + one **step span** per top-level step + optional generic child spans (e.g. action download). |
| Metrics | `/v1/metrics` | `github.pipeline.run.duration` (histogram), `github.pipeline.run.errors` (monotonic counter, failures only), `github.pipeline.task.duration` (histogram, one point per job/step). |
| Logs    | `/v1/logs`    | Job- and step-level annotations (`::warning::` / `::error::` / `::notice::`), each correlated to its span via `traceId`/`spanId`. |

Runner identity is attached once as the OTLP **Resource**:
`service.name=github-actions-runner`, `service.version`, `host.name`, `host.arch`,
`os.type`, semconv `cicd.worker.{name,id}` + `cicd.system.component=agent`,
`service.instance.id`, and `github.runner.{group,ephemeral}`. The standard
`OTEL_RESOURCE_ATTRIBUTES` env var is honored and merged (so ARC can attach
`k8s.*` via the Downward API), with explicitly-set keys taking precedence.

Spans and steps carry GitHub/VCS context (`github.*`, `vcs.*`) and CI/CD
semantic-convention attributes (`cicd.pipeline.*`). On failure the span gets
`error.type` (low-cardinality classifier), an ERROR status carrying the error
message as status `message`, and — when a message exists — a message-only
`exception` event (a CI conclusion is not an exception class, so
`exception.type` is omitted rather than faked).

### Trace shape — the job span is the trace root

The job span is the **root of the runner's trace** (`parentSpanId` omitted). The
runner owns the *job*, not the *workflow run*, so it does **not** parent the job
to a run/workflow span it never emits — doing so left a dangling `parentSpanId`
that broke trace assembly (regression-guarded by `JobSpan_IsRoot_NoDanglingParent`).

Two relationships replace that link:

- **The authoritative run span comes from the GitHub API path** (a separate
  consumer that reconstructs the run from the timeline/REST API) and is merged
  downstream by the **deterministic ID contract** (see below), so the run span
  and the runner's job span land in the same trace without the runner emitting
  the run span itself.
- **An inbound scheduler context is attached as a span LINK, not a parent.** If an
  upstream controller (e.g. ARC) injects a W3C `traceparent` via
  `ACTIONS_RUNNER_PARENT_TRACEPARENT` (+ `…_TRACESTATE`), `AddInboundParentLink`
  parses it and adds a link (`cicd.system.component=controller`) to the job span.
  This expresses cross-trace causality without re-rooting the runner's own
  deterministic trace; `tracestate` and the inbound sampled flag ride on the link.

### Deterministic, content-derived span IDs

Span/trace IDs are derived only from identifiers the runner always has — run id,
run attempt, and job/step **display names** — via **SHA-256 of a UTF-8 input
string, truncated** (16 bytes for a trace id, 8 for a span id). This lets a
consumer that also reconstructs the trace from the GitHub API compute the same
IDs and merge the two sources (server-side queue time from the API + runner-native
step precision).

```
traceId      = sha256("{run_id}-{run_attempt}")[:16]                              (32 hex)
job span      = sha256("job-{run_id}-{run_attempt}-{job_name}")[:8]               (16 hex)
step span     = sha256("step-{run_id}-{run_attempt}-{job_name}-{step_number}-{step_name}")[:8]
child span    = sha256("{type}-{run_id}-{run_attempt}-{job_name}-{name}-{startNano}")[:8]
```

SHA-256 (not MD5) because MD5 throws on a FIPS-enabled host. The IDs are
intentionally deterministic, so the W3C Level-2 randomness flag is never set. The
full normative spec, rationale, and golden conformance vectors live in
[`docs/otel-id-contract.md`](../otel-id-contract.md) — that document, not any
external repo, is the source of truth for the contract.

Only **top-level** steps get spans; composite/embedded sub-steps are skipped
(their display names aren't unique and have no counterpart in the
API-reconstructed trace).

### Transport

- **OTLP/JSON over HTTP**, built with `Utf8JsonWriter` (correct escaping — a
  control char in a step name can't invalidate the batch). No protobuf, no gRPC,
  no SDK (see *Alternatives* below).
- **gzip** `Content-Encoding`. A job's spans/logs are highly repetitive, so this
  typically shrinks the body ~10x (see `docs/otel-benchmarks.md`);
  `CompressionLevel.Fastest` keeps CPU negligible. Always-on with no opt-out: the
  OTLP spec requires every server component to accept gzip request bodies.
- **Chunked POSTs.** Spans/logs are sent in batches of at most `MaxItemsPerPost`
  (1,000) per request — ~4 MB uncompressed even at the 10k buffer cap, safely
  under the OTel Collector's 20 MiB default decompressed-body limit — so a 413
  (non-retryable per OTLP) can never drop a whole signal, and per-POST
  serialization memory stays bounded. All chunks share the one flush deadline.
- **One retry** on a transient failure (exactly the OTLP/HTTP retryable codes —
  429/502/503/504 — or a transport exception), bounded by the flush deadline.
  The server's `Retry-After` is honored when it fits the deadline (skipping the
  retry when it can't); otherwise the delay is a jittered 100–400 ms so fleets
  don't retry in lockstep. With this deliberate single-retry bound, the spec's
  exponential-backoff SHOULD degenerates to that one jittered wait. Per-request
  HTTP timeout is 5 s.
- Sent over the runner's **proxy-aware** `HostContext.CreateHttpClientHandler()`,
  honoring the runner's existing proxy config. For self-signed/private-CA
  collectors, `ACTIONS_RUNNER_OTLP_CERTIFICATE` points at a PEM CA bundle that
  is trusted for the collector connection (hostname still checked; a bad bundle
  fails closed). `ACTIONS_RUNNER_OTLP_INSECURE=true` instead disables **all**
  server-certificate validation — a MITM can then read the export traffic,
  including any `ACTIONS_RUNNER_OTLP_HEADERS` credential — so enabling it logs
  an explicit warning; prefer the CA bundle.
- **Per-buffer memory caps** (`MaxBufferedSpans`/`Logs`/`TaskMetrics` = 10 000
  each). A pathological job (e.g. unbounded `::warning::` annotations) can't grow
  the buffers without bound; excess is dropped and counted, never OOMing the
  worker. The job span — the trace root, recorded last — bypasses the span cap
  (one per job, worst case cap+1): dropping it would orphan every exported step
  span, which all parent to its deterministic ID.
- **Secret masking at flush.** Every exported string — span names, attribute and
  resource values, log bodies — is run through the runner's `SecretMasker` before
  serialization, the same scrubbing applied to all other off-box telemetry.
  Collector auth-header values from `ACTIONS_RUNNER_OTLP_HEADERS` are registered
  with the masker too, and the variable itself is scrubbed from the worker's
  process env right after it is read — host step processes inherit that env, so
  leaving it set would hand the raw credential to every step.

### Lifecycle

1. `JobRunner.RunAsync` → `SetResource(...)` captures runner identity.
2. `ExecutionContext.InitializeJob` → `SetJobInfo(...)` captures run/job
   identifiers once, so every span ID and parent link stays consistent.
3. `ExecutionContext.Complete()` → `RecordStepCompletion` / `RecordJobCompletion`
   build pending spans/logs/metrics from the timeline record.
4. `JobRunner.CompleteJobAsync` → `FlushAsync()` POSTs all three signals.

### Gating

Two independent gates, both must allow export:

| Gate | Controlled by | Default |
|------|---------------|---------|
| `ACTIONS_RUNNER_OTLP_ENDPOINT` set | Runner operator (env) | unset → **off** |
| `actions_runner_otel_export` feature flag | GitHub (server) | unprovisioned → **on** |

```
enabled = endpoint configured && (feature flag ?? true)
```

The endpoint env var is the operator's explicit opt-in. The feature flag is a
server-side **kill switch** layered on top. The flag defaults **on** when
unprovisioned: the operator already opted in by configuring an endpoint, and a
`?? false` default would silently break that opt-in on self-hosted / GHES runners
where the flag isn't provisioned (the primary audience). On github.com the flag
is always provisioned, so GitHub retains full control by sending `false`; the
default only matters where the server doesn't know the flag, and there "on" is
correct. **Export still requires the operator to set an endpoint** — an
unprovisioned flag alone exports nothing.

## Alternatives considered: OpenTelemetry .NET SDK

The first question a maintainer will ask is *"why not just use the
OpenTelemetry .NET SDK?"* We hand-rolled the exporter deliberately:

- **Zero new dependencies.** The runner ships with **no OpenTelemetry NuGet
  packages** (confirmed: `grep -i opentelemetry src/Runner.Worker/Runner.Worker.csproj`
  → none). The runner is deliberately dependency-averse — it is widely deployed,
  security-sensitive, and trim/AOT-sensitive. Pulling in the SDK + an OTLP
  exporter + their transitive graph (gRPC/protobuf or the HTTP exporter stack)
  onto every runner is a large, hard-to-justify supply-chain and binary-size
  cost for what is ~1 file of BCL code.
- **Content-derived IDs.** The design needs **deterministic, content-derived
  span IDs** (SHA-256 of identifier strings) so runner spans dedupe/merge with
  API-reconstructed spans. The SDK's `Activity`/`IdGenerator` model assumes
  random IDs and has no clean hook to derive an ID from span content at creation.
- **Post-hoc reconstruction, not live tracing.** Spans are reconstructed *after*
  the fact from `TimelineRecord`s (start/end/result already known), not traced
  live through an `Activity`/`ActivityListener`. The SDK is built around live,
  in-process activities; bending it to emit fully-formed historical spans fights
  the grain.
- **Native primitives already exist.** The runner already has a native
  `SecretMasker` (mandatory for anything leaving the box) and a proxy-aware
  `HttpClientHandler`. The SDK exporter would need to be taught both.

OTLP wire choices, with rationale:

- **JSON over protobuf** — no protobuf dependency or codegen; trivially
  inspectable; serialized safely with `Utf8JsonWriter`. The ~size cost is erased
  by gzip.
- **HTTP over gRPC** — every OTLP collector accepts OTLP/HTTP; it traverses the
  runner's existing HTTP proxy stack; no HTTP/2 / gRPC dependency.

Sub-alternatives rejected:

- **SDK exporter only** (hand-build `Activity`s, export via the SDK's OTLP
  exporter) — still drags in the SDK + exporter dependency graph and still has no
  clean content-derived-ID hook; saves little over a full hand-roll.
- **Sidecar / collector-only** (runner does nothing; an external agent scrapes
  the API) — that's exactly today's status quo we're replacing: post-hoc,
  second-granularity, no runner-host context, and extra infrastructure for every
  operator.

## Trade-offs (review-flagged, documented honestly)

- **(a) Flush is on the job-completion critical path.** `FlushAsync` is awaited in
  `CompleteJobAsync`. It is bounded to a **4 s overall deadline** across all three
  signals (including the retry) so a slow/unreachable collector can delay job
  completion by at most that. It is **best-effort**: it cannot throw and cannot
  fail the job (every failure path is caught and logged via `Trace`). A worker
  crash before flush loses that job's un-flushed telemetry — acceptable, since
  telemetry must never gate or fail real work.
- **(b) Sampling/cardinality is server/collector-controlled.** The runner emits
  **everything** for an enabled job (no client-side sampling); volume is governed
  by the server feature flag + the operator's endpoint opt-in, and any
  sampling/aggregation is the collector's job. This keeps the runner simple and
  puts the cost/volume policy where the operator controls it.
- **(c) The CICD + VCS semantic conventions are experimental.** The `cicd.*` and
  `vcs.*` attributes are **Development/experimental** in semconv and subject to
  breaking change. `schema_url` is **pinned** (`…/schemas/1.34.0`, audited so every emitted attribute exists in that release) on every
  resource/scope so consumers can detect the version and adapt.
- **(d) Flag default + endpoint requirement.** The feature flag defaults **on**
  when unprovisioned, but export still requires an operator-set endpoint, so the
  on-by-default flag never causes unexpected egress on its own.

## Configuration

| Env var | Required | Description |
|---------|----------|-------------|
| `ACTIONS_RUNNER_OTLP_ENDPOINT` | Yes | OTLP/HTTP base URL, e.g. `http://collector:4318`. Signals POST to `{endpoint}/v1/{traces,metrics,logs}`. A URL already ending in `/v1/traces` is accepted. |
| `ACTIONS_RUNNER_OTLP_HEADERS` | No | Comma-separated `key=value` headers for collector auth, e.g. `authorization=Bearer xyz,x-api-key=abc`. Values are registered with the secret masker. |
| `ACTIONS_RUNNER_OTLP_CERTIFICATE` | No | Path to a PEM CA bundle trusted for the collector connection (self-signed/private-CA collectors). Mirrors `OTEL_EXPORTER_OTLP_CERTIFICATE`. |
| `ACTIONS_RUNNER_OTLP_INSECURE` | No | `true` disables **all** TLS certificate validation (not just self-signed) — exposes the connection, including auth headers, to MITM. Logs a warning; prefer `…_OTLP_CERTIFICATE`. |
| `ACTIONS_RUNNER_OTLP_PROPAGATE` | No | `true` to inject `TRACEPARENT` + `OTEL_*` into each step's env so in-job tools nest under the step span. |
| `ACTIONS_RUNNER_PARENT_TRACEPARENT` / `…_TRACESTATE` | No | Inbound W3C context from an upstream scheduler; attached to the job span as a link. |
| `OTEL_RESOURCE_ATTRIBUTES` | No | Standard OTel env var; merged into the Resource (e.g. ARC `k8s.*`). |

These are runner-namespaced (`ACTIONS_RUNNER_OTLP_*`) rather than the standard
`OTEL_*` export variables, so enabling runner export does not clobber the `OTEL_*`
config a workflow's own steps may rely on for their application telemetry.

### Step propagation (`StepPropagationEnv`)

When `ACTIONS_RUNNER_OTLP_PROPAGATE=true`, each step's env is given:

- `TRACEPARENT = 00-{traceId}-{stepSpanId}-01` (matches the exporter's IDs, so
  in-job OTel tools nest under the correct step span),
- `OTEL_EXPORTER_OTLP_ENDPOINT` (the base URL, with any URL userinfo credential
  — `user:token@` — stripped; the userinfo is also registered with the secret
  masker so it can never appear raw in diag logs),
- `OTEL_RESOURCE_ATTRIBUTES` (run/job/repo identity).

The endpoint is propagated from the **runner host's perspective**: for
`container:` jobs and container actions the collector must be reachable from
inside the job container — `http://localhost:4318` on the host does not resolve
there (use a host-gateway or cluster-reachable address instead).

It **deliberately does not** propagate `OTEL_EXPORTER_OTLP_HEADERS`: that header
carries the collector credential, and handing it to user step processes would let
any step read and exfiltrate it. Steps needing collector auth must get a separate,
scoped credential out of band (guarded by `StepPropagationEnv_DoesNotLeakAuthHeaders`).

### Viewing locally

```bash
# Point the runner at any OTLP/HTTP collector (the OpenTelemetry Collector,
# Jaeger :4318, Grafana Alloy, ...):
export ACTIONS_RUNNER_OTLP_ENDPOINT=http://localhost:4318
./run.sh
```

## Consequences

- Operators get real-time, sub-second job/step traces, metrics, and logs with
  runner-host context, no API polling.
- The deterministic ID contract means runner spans merge cleanly with
  API-reconstructed traces, and re-runs need no special handling: attempt _N_
  lands in trace `sha256("{run_id}-{N}")[:16]`, the same trace the API path
  reconstructs for that attempt.
- The exporter is self-contained: if the OTLP wire format must evolve, only
  `OTelTraceExporter` changes — no dependency churn.
- The runner stays dependency-free of the OTel SDK, preserving its trim/AOT and
  supply-chain posture.
