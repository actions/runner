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

Add an opt-in, best-effort OTLP/HTTP exporter (`OTelTracer`) that emits, for each
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
step  = md5("step-{run_id}-{run_attempt}-{job_name}-{step_name}")[:8] parent -> job
```

### Lifecycle

1. `JobRunner.RunAsync` → `OTelTracer.SetResource(...)` captures runner identity.
2. `ExecutionContext.InitializeJob` → `OTelTracer.SetJobInfo(...)` captures the run/job
   identifiers once, so every span's ID and parent link stays consistent.
3. `ExecutionContext.Complete()` → `RecordStepCompletion` / `RecordJobCompletion` build
   pending spans from the timeline record.
4. `JobRunner.CompleteJobAsync` → `OTelTracer.FlushAsync()` POSTs the resource and all
   spans as a single OTLP/JSON request to `{endpoint}/v1/traces`.

Export is **best-effort**: any failure (unreachable collector, timeout) is swallowed
and never affects job execution. No new NuGet dependencies are introduced — the
exporter uses raw HTTP and hand-built OTLP/JSON.

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
server-side **kill switch** layered on top: it defaults to on so the opt-in keeps
working on self-hosted / GHES where the flag isn't provisioned, but lets GitHub
disable export fleet-wide without a runner redeploy.

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
  `OTelTracer` changes.
- Cross-trace correlation across jobs/runners relies on the deterministic IDs rather
  than W3C `traceparent` propagation; a backend-rooted run span (covering server-side
  orchestration) would require the job message to carry trace context, which is out of
  scope here.
