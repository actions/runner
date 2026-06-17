# ADR 4366: Native OpenTelemetry trace export

**Date**: 2026-06-17

**Status**: Proposed

## Context

The runner already collects the timing and result data that the GitHub Actions
timeline API exposes (job and per-step start/finish/result via `TimelineRecord`).
Operators who want this data in their own observability stack today have to poll
the timeline/REST API after the fact and translate it to OpenTelemetry out of
band — post-hoc, second-granularity, and without runner-host context.

We want the runner to emit this telemetry **natively as OpenTelemetry**, in real
time, with sub-second precision, so it can be sent straight to any OTLP collector
(Honeycomb, Grafana/Tempo, Jaeger, the OpenTelemetry Collector, etc.).

## Decision

Add an opt-in, best-effort OTLP/HTTP exporter — `IOTelTraceExporter` (a
`RunnerService` resolved via `HostContext.GetService<>()`) — that emits, for each
job the runner executes:

1. **Runner info** as the OTLP **Resource** — `service.name=github-actions-runner`,
   `service.version`, `host.name`, `host.arch`, `os.type`, and
   `github.runner.{name,id,group,ephemeral}`.
2. **A job span** — parented to the workflow span.
3. **A step span per step** — parented to the job span — carrying timing, result,
   action ref/type, and CI/CD semantic-convention attributes
   (`cicd.pipeline.task.*`), with `error.type` and an ERROR status on failure.

### Deterministic, name-based span IDs

Span IDs are derived only from identifiers the runner always has — the run id,
run attempt, and job/step **display names** — never the numeric job id (which is
only available behind a separate server feature flag). This lets a consumer that
also reconstructs the trace from the GitHub API (e.g.
[otel-explorer](https://github.com/stefanpenner/otel-explorer)) compute the same
IDs and merge the two sources:

```
trace = md5("{run_id}-{run_attempt}")
job   = md5("job-{run_id}-{run_attempt}-{job_name}")[:8]              parent -> bigendian(run_id)  (workflow)
step  = md5("step-{run_id}-{run_attempt}-{job_name}-{step_number}-{step_name}")[:8] parent -> job
```

### Lifecycle

1. `JobRunner.RunAsync` → `OTelTracer.SetResource(...)` captures runner identity.
2. `ExecutionContext.InitializeJob` → `OTelTracer.SetJobInfo(...)` captures the run/job
   identifiers once, so every span's ID and parent link stays consistent.
3. `ExecutionContext.Complete()` → `RecordStepCompletion` / `RecordJobCompletion` build
   pending spans from the timeline record.
4. `JobRunner.CompleteJobAsync` → `OTelTracer.FlushAsync()` POSTs the resource and all
   spans as a single OTLP/JSON request to `{endpoint}/v1/traces`.

Only **top-level** steps are emitted; composite/embedded sub-steps are skipped (their
display names aren't unique and have no counterpart in the API-reconstructed trace).

Export is **best-effort**: any failure (unreachable collector, non-2xx, timeout) is
logged via `Trace` and swallowed, never affecting job execution. Flush is bounded to a
2s budget so a slow collector can't delay job completion. The payload is OTLP/JSON built
with `System.Text.Json` (no new NuGet dependencies) and sent over the runner's
proxy-aware `HostContext.CreateHttpClientHandler()`.

### Gating

Two independent gates, both must allow export:

| Gate | Controlled by | Default |
|------|---------------|---------|
| `ACTIONS_RUNNER_OTLP_ENDPOINT` set | Runner operator (env) | unset → off |
| `actions_runner_otel_export` feature flag | GitHub (server) | unset → **on** |

```
enabled = endpoint configured && (feature flag ?? true)
```

The endpoint env var is the operator's explicit opt-in. The feature flag is a
server-side **kill switch** layered on top.

**Decision — the flag defaults to on (not the usual `?? false`).** The operator
already opted in by configuring an endpoint; the flag exists only so GitHub can
*disable* a misbehaving export. A `?? false` default would silently break the
opt-in for self-hosted / GHES runners where the flag isn't provisioned — the
primary audience for this feature. On github.com the flag is always provisioned,
so GitHub retains full control there by sending `false`; the default only matters
for servers that don't know the flag, where "on" is the correct behavior.

## Configuration

| Env var | Required | Description |
|---------|----------|-------------|
| `ACTIONS_RUNNER_OTLP_ENDPOINT` | Yes | OTLP/HTTP base URL, e.g. `http://collector:4318`. Traces are POSTed to `{endpoint}/v1/traces`. |
| `ACTIONS_RUNNER_OTLP_HEADERS` | No | Comma-separated `key=value` headers sent with the export, e.g. `authorization=Bearer xyz,x-honeycomb-team=abc`. For collector auth. |
| `ACTIONS_RUNNER_OTLP_INSECURE` | No | `true` to skip TLS verification (self-signed collectors). |

Every exported string — span names and attribute/resource values — is run through
the runner's secret masker before export, the same scrubbing applied to all other
telemetry the runner sends off-box.

These are intentionally runner-namespaced rather than the standard `OTEL_*`
variables, so enabling runner export does not clobber `OTEL_*` configuration that
a workflow's own steps may rely on for their application telemetry.

### Viewing locally

```bash
# Point the runner at any OTLP/HTTP collector, e.g. the OpenTelemetry Collector,
# Jaeger (:4318), or otel-explorer's receiver:
export ACTIONS_RUNNER_OTLP_ENDPOINT=http://localhost:4318
./run.sh
```

## Consequences

- Operators get real-time, sub-second job/step traces with runner-host context, no
  API polling.
- The name-based ID contract means runner spans merge cleanly with API-reconstructed
  traces (server-side queue time from the API + runner-native step precision).
- The exporter is self-contained; if the OTLP wire format needs to evolve, only
  `OTelTraceExporter` changes.
- Cross-trace correlation across jobs/runners relies on the deterministic IDs rather
  than W3C `traceparent` propagation; a backend-rooted run span (covering server-side
  orchestration) would require the job message to carry trace context, which is out of
  scope here.
- Step span IDs include the 1-based step number, so two top-level steps that share a
  display name still get distinct IDs. The number matches the GitHub API's `step.number`,
  so the IDs continue to merge with the API-reconstructed trace.
- Re-runs work without special handling: a job for attempt _N_ lands in trace
  `md5("{run_id}-{N}")` parented to `bigendian(run_id)` — identical to how the API trace
  for attempt _N_ is reconstructed. Prior attempts live in their own (different) traces
  the runner never emits to.
