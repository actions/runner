using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(OTelTraceExporter))]
    public interface IOTelTraceExporter : IRunnerService
    {
        void SetResource(string runnerName, string runnerId, string runnerGroup, string runnerVersion, string osType, string arch, string machineName, bool ephemeral);
        void SetJobInfo(string runId, string runAttempt, string jobName, string jobKey, string repository, string workflow, string eventName, string serverUrl, bool featureEnabled = true, string sha = null, string refName = null, string actor = null, string baseRef = null, string changeId = null);
        void RecordStepCompletion(string stepName, int? stepNumber, DateTime? startTime, DateTime? endTime, TaskResult? conclusion, string stepType, string actionName, string actionRef, bool isEmbedded = false, string errorMessage = null, string stepStage = null);
        // OTel log record correlated to the JOB span (job-level issues/annotations).
        void RecordJobLog(string severityText, string message);
        void RecordJobCompletion(DateTime? startTime, DateTime? endTime, TaskResult? conclusion, long throttlingDelayMs = 0, string errorMessage = null);
        // Generic child span for finer-grained timing, e.g. action download. Parents to the
        // given step (when the operation runs inside one, e.g. "Set up job"), else the job.
        void RecordSpan(string name, string spanType, DateTime startTime, DateTime endTime, IDictionary<string, string> attributes = null, string parentStepName = null, int parentStepNumber = 0, int spanKind = 1);
        // OTel log record correlated to a step span (step issues/annotations).
        void RecordStepLog(string stepName, int? stepNumber, string severityText, string message);
        // W3C trace context + OTEL_* to inject into a step's env so in-job tools nest under the step span.
        IDictionary<string, string> StepPropagationEnv(string stepName, int? stepNumber);
        Task FlushAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Emits native OpenTelemetry trace spans for a job and its steps, plus runner
    /// identity as an OTLP Resource. Spans use deterministic IDs derived only from
    /// identifiers the runner always has (run id, run attempt, job display name,
    /// step name) so they merge with a GitHub Actions trace reconstruction.
    ///
    /// Opt-in: set ACTIONS_RUNNER_OTLP_ENDPOINT to an OTLP/HTTP base URL; spans are
    /// POSTed to {endpoint}/v1/traces. Export is best-effort — failures are logged
    /// and swallowed, never affecting job execution.
    ///
    /// The job span is the trace root; steps are its children. The deterministic-ID
    /// contract (SHA-256 of run/job/step identifiers, truncated to 16/8 bytes) is
    /// specified normatively in docs/otel-id-contract.md so consumers can reconstruct
    /// and de-dupe the same trace; otel-explorer is one example consumer.
    /// </summary>
    public sealed partial class OTelTraceExporter : RunnerService, IOTelTraceExporter
    {
        private static readonly JsonWriterOptions s_jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        // Declares which semantic-convention version this telemetry conforms to.
        private const string SchemaUrl = "https://opentelemetry.io/schemas/1.29.0";
        private string _serviceVersion = "";

        private readonly object _lock = new();
        private readonly List<OTelSpan> _pendingSpans = new();
        private readonly List<OTelLog> _pendingLogs = new();
        // Hard caps so a pathological job (e.g. unbounded ::warning:: annotations) can't
        // grow these buffers without limit and OOM the worker — the export must never
        // affect the job. ~4 KB/span retained until flush (see docs/otel-benchmarks.md);
        // 10k keeps worst-case retention bounded to tens of MB. Excess is dropped + counted.
        internal const int MaxBufferedSpans = 10000;
        internal const int MaxBufferedLogs = 10000;
        internal const int MaxBufferedTaskMetrics = 10000;
        // Max spans/logs per OTLP POST. At ~4 KB/span (docs/otel-benchmarks.md) each
        // request stays ~4 MB uncompressed — well under the OTel Collector's 20 MiB
        // default decompressed-body limit — so one 413 can never drop a whole signal,
        // and per-POST serialization memory stays bounded. Mirrors OTel SDK batch
        // sizing (BatchSpanProcessor exports 512-item batches; we trade a few larger
        // requests for staying inside the single 4 s flush deadline).
        internal const int MaxItemsPerPost = 1000;
        private long _droppedSpans;
        private long _droppedLogs;
        private long _droppedTaskMetrics;
        private string _endpoint;
        private string _baseUrl;
        private string _tracesUrl;
        private string _logsUrl;
        private string _metricsUrl;
        private string _rawHeaders;
        private JobMetrics _jobMetrics;
        private readonly List<TaskMetric> _taskMetrics = new();
        private bool _enabled;
        // Inject W3C TRACEPARENT + OTEL_* into each step's env so in-job tools/actions
        // emit spans parented to the step. Opt-in (ACTIONS_RUNNER_OTLP_PROPAGATE).
        private bool _propagate;
        // Server-side kill switch, captured at job init. Defaults to true so the
        // operator's endpoint opt-in works on self-hosted/GHES where the flag isn't
        // provisioned; GitHub can send false to disable export fleet-wide.
        private bool _featureEnabled = true;
        private OTelHttpTransport _transport;
        private JobInfo _jobInfo;
        private List<KeyValuePair<string, object>> _resource = DefaultResource();

        private sealed class JobInfo
        {
            public long RunId;
            public long RunAttempt;
            public string RunIdRaw;
            public string RunAttemptRaw;
            public string JobName;     // job display name, matches GitHub API job.name
            public string JobKey;      // github.job context key (e.g. "build")
            public string Repository;
            public string Workflow;
            public string EventName;
            public string ServerUrl;
            public string Sha;
            public string RefName;   // branch/tag (vcs.ref.head.name)
            public string Actor;
            public string BaseRef;   // base branch for PRs (vcs.ref.base.name)
            public string ChangeId;  // PR number (vcs.change.id)
        }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _endpoint = Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpEndpoint)?.TrimEnd('/');
            _enabled = !string.IsNullOrEmpty(_endpoint);
            if (!_enabled)
            {
                return;
            }

            // Allow either a base URL or one that already includes the signal path.
            _tracesUrl = _endpoint.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase)
                ? _endpoint
                : $"{_endpoint}/v1/traces";
            _baseUrl = _endpoint.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase)
                ? _endpoint.Substring(0, _endpoint.Length - "/v1/traces".Length)
                : _endpoint;
            _logsUrl = $"{_baseUrl}/v1/logs";
            _metricsUrl = $"{_baseUrl}/v1/metrics";
            _propagate = StringUtil.ConvertToBoolean(
                Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpPropagate));

            var insecure = StringUtil.ConvertToBoolean(
                Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpInsecure));
            // Proxy-aware handler so export honors the runner's proxy configuration.
            var handler = HostContext.CreateHttpClientHandler();
            if (insecure)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            // Optional collector auth headers, e.g.
            //   ACTIONS_RUNNER_OTLP_HEADERS="authorization=Bearer xyz,x-api-key=abc"
            _rawHeaders = Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpHeaders);
            var headers = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(_rawHeaders))
            {
                foreach (var pair in _rawHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }
                    var value = pair.Substring(eq + 1).Trim();
                    // Header values are commonly auth tokens; register so they're masked if ever logged.
                    HostContext.SecretMasker.AddValue(value);
                    headers.Add(new KeyValuePair<string, string>(pair.Substring(0, eq).Trim(), value));
                }
            }
            _transport = new OTelHttpTransport(handler, headers, msg => Trace.Info(msg),
                userAgent: $"GitHubActionsRunner-OTLP-Exporter/{BuildConstants.RunnerPackage.Version}");
            Trace.Info($"Native OTel export enabled, endpoint: {_endpoint}");
        }

        public bool IsEnabled => _enabled && _featureEnabled;

        public void SetResource(string runnerName, string runnerId, string runnerGroup, string runnerVersion, string osType, string arch, string machineName, bool ephemeral)
        {
            var attrs = DefaultResource();
            void Add(string k, object v)
            {
                if (v is string s && string.IsNullOrEmpty(s))
                {
                    return;
                }
                attrs.Add(new KeyValuePair<string, object>(k, v));
            }
            _serviceVersion = runnerVersion ?? "";
            Add("service.version", runnerVersion);
            Add("host.name", machineName);
            Add("host.arch", arch);
            Add("os.type", osType);
            // semconv cicd.worker.* identifies the executor (no bespoke github.runner.name/id
            // dupes); group/ephemeral have no semconv equivalent so they stay github.runner.*.
            Add("cicd.worker.name", runnerName);
            Add("cicd.worker.id", runnerId);
            Add("service.instance.id", runnerId);
            Add("github.runner.group", runnerGroup);
            Add("github.runner.ephemeral", ephemeral);
            // The runner is the CI/CD agent executing tasks (semconv cicd.system.component).
            Add("cicd.system.component", "agent");

            // Honor the standard OTEL_RESOURCE_ATTRIBUTES env var (OTel spec): a
            // comma-separated list of key=value pairs with percent-encoded values. This is
            // the spec-native way for a deployment (e.g. ARC via the Kubernetes Downward
            // API) to attach k8s.pod.name / k8s.namespace.name / k8s.node.name and any other
            // resource attributes — no runner-specific env names required. Keys set
            // explicitly above take precedence over the env var.
            var present = new HashSet<string>();
            foreach (var kv in attrs)
            {
                present.Add(kv.Key);
            }
            foreach (var kv in ParseOtelResourceAttributes(Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES")))
            {
                if (present.Add(kv.Key))
                {
                    attrs.Add(new KeyValuePair<string, object>(kv.Key, kv.Value));
                }
            }
            lock (_lock)
            {
                _resource = attrs;
            }
        }

        // ParseOtelResourceAttributes parses the W3C-Baggage-style value of the standard
        // OTEL_RESOURCE_ATTRIBUTES env var: comma-separated key=value pairs, values
        // percent-encoded. Malformed entries are skipped.
        internal static IEnumerable<KeyValuePair<string, string>> ParseOtelResourceAttributes(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                yield break;
            }
            foreach (var pair in raw.Split(','))
            {
                var trimmed = pair.Trim();
                var eq = trimmed.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }
                var key = trimmed.Substring(0, eq).Trim();
                if (key.Length == 0)
                {
                    continue;
                }
                var value = trimmed.Substring(eq + 1).Trim();
                yield return new KeyValuePair<string, string>(key, Uri.UnescapeDataString(value));
            }
        }

        public void SetJobInfo(string runId, string runAttempt, string jobName, string jobKey, string repository, string workflow, string eventName, string serverUrl, bool featureEnabled = true, string sha = null, string refName = null, string actor = null, string baseRef = null, string changeId = null)
        {
            if (!_enabled)
            {
                lock (_lock) { _featureEnabled = featureEnabled; }
                return;
            }

            long.TryParse(runId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var runIdNum);
            long.TryParse(runAttempt, NumberStyles.Integer, CultureInfo.InvariantCulture, out var runAttemptNum);
            if (runAttemptNum == 0)
            {
                runAttemptNum = 1;
            }

            lock (_lock)
            {
                _featureEnabled = featureEnabled;
                _jobInfo = new JobInfo
                {
                    RunId = runIdNum,
                    RunAttempt = runAttemptNum,
                    RunIdRaw = runId ?? "",
                    RunAttemptRaw = string.IsNullOrEmpty(runAttempt) ? "1" : runAttempt,
                    JobName = jobName ?? "",
                    JobKey = jobKey ?? "",
                    Repository = repository ?? "",
                    Workflow = workflow ?? "",
                    EventName = eventName ?? "",
                    ServerUrl = string.IsNullOrEmpty(serverUrl) ? "https://github.com" : serverUrl,
                    Sha = sha ?? "",
                    RefName = refName ?? "",
                    Actor = actor ?? "",
                    BaseRef = baseRef ?? "",
                    ChangeId = changeId ?? "",
                };
            }
        }

        // Common GitHub/VCS context shared by job and step spans.
        private static void AddCommonContext(OTelSpan span, JobInfo job)
        {
            span.Set("github.repository", job.Repository);
            span.Set("github.workflow", job.Workflow);
            span.Set("github.event_name", job.EventName);
            span.Set("github.run_id", job.RunIdRaw);
            span.Set("github.run_attempt", job.RunAttemptRaw);
            span.Set("github.job", job.JobKey);
            span.Set("cicd.pipeline.name", job.Workflow);
            span.Set("cicd.pipeline.run.id", job.RunIdRaw);
            span.Set("cicd.pipeline.run.url.full", $"{job.ServerUrl}/{job.Repository}/actions/runs/{job.RunIdRaw}/attempts/{job.RunAttemptRaw}");
            span.Set("vcs.repository.url.full", $"{job.ServerUrl}/{job.Repository}");
            span.Set("vcs.provider.name", "github");
            if (!string.IsNullOrEmpty(job.Sha)) span.Set("vcs.ref.head.revision", job.Sha);
            if (!string.IsNullOrEmpty(job.RefName)) span.Set("vcs.ref.head.name", job.RefName);
            if (!string.IsNullOrEmpty(job.BaseRef)) span.Set("vcs.ref.base.name", job.BaseRef);
            if (!string.IsNullOrEmpty(job.ChangeId)) span.Set("vcs.change.id", job.ChangeId);
            if (!string.IsNullOrEmpty(job.Repository))
            {
                var slash = job.Repository.IndexOf('/');
                if (slash > 0)
                {
                    span.Set("vcs.owner.name", job.Repository.Substring(0, slash));
                    span.Set("vcs.repository.name", job.Repository.Substring(slash + 1));
                }
            }
            if (!string.IsNullOrEmpty(job.Actor)) span.Set("github.actor", job.Actor);
        }

        public void RecordStepCompletion(string stepName, int? stepNumber, DateTime? startTime, DateTime? endTime, TaskResult? conclusion, string stepType, string actionName, string actionRef, bool isEmbedded = false, string errorMessage = null, string stepStage = null)
        {
            // Embedded/composite sub-steps are not top-level timeline steps: their
            // display names aren't unique (would collide as span IDs) and they have no
            // counterpart in the API-reconstructed trace. Only emit top-level steps.
            if (!IsEnabled || isEmbedded)
            {
                return;
            }

            try
            {
                JobInfo job;
                lock (_lock) { job = _jobInfo; }
                if (job == null || job.RunId == 0 || string.IsNullOrEmpty(stepName))
                {
                    return;
                }

                var ghConclusion = NormalizeConclusion(conclusion);
                var span = new OTelSpan
                {
                    TraceId = NewTraceID(job.RunId, job.RunAttempt),
                    SpanId = NewStepSpanID(job.RunId, job.RunAttempt, job.JobName, stepNumber ?? 0, stepName),
                    ParentSpanId = NewJobSpanID(job.RunId, job.RunAttempt, job.JobName),
                    Name = stepName,
                    Kind = 1, // INTERNAL
                    StartTimeUnixNano = ToUnixNano(startTime ?? DateTime.UtcNow),
                    EndTimeUnixNano = ToUnixNano(endTime ?? DateTime.UtcNow),
                };

                var runUrl = $"{job.ServerUrl}/{job.Repository}/actions/runs/{job.RunIdRaw}";
                var stepUrl = $"{runUrl}/attempts/{job.RunAttemptRaw}#step:{stepNumber ?? 0}:1";

                span.Set("github.record_type", "step");
                AddCommonContext(span, job);
                span.Set("github.step_number", (long)(stepNumber ?? 0));
                span.Set("github.conclusion", ghConclusion);
                if (!string.IsNullOrEmpty(stepType)) span.Set("github.step_type", stepType);
                if (!string.IsNullOrEmpty(stepStage)) span.Set("github.action_stage", stepStage);
                if (!string.IsNullOrEmpty(actionName)) span.Set("github.action", actionName);
                if (!string.IsNullOrEmpty(actionRef)) span.Set("github.action_ref", actionRef);
                span.Set("cicd.pipeline.task.name", stepName);
                span.Set("cicd.pipeline.task.run.id", span.SpanId);
                span.Set("cicd.pipeline.task.run.result", ToSemconvResult(ghConclusion));
                span.Set("cicd.pipeline.task.run.url.full", stepUrl);
                var taskType = InferTaskType(stepName, actionName);
                if (taskType != null) span.Set("cicd.pipeline.task.type", taskType);

                ApplyStatus(span, ghConclusion);
                if (ghConclusion == "failure")
                {
                    span.AddException("failure", errorMessage, span.EndTimeUnixNano);
                }

                var stepMetric = new TaskMetric
                {
                    Name = stepName,
                    Type = "step",
                    Result = ToSemconvResult(ghConclusion),
                    Attempt = job.RunAttemptRaw,
                    StartNano = span.StartTimeUnixNano,
                    EndNano = span.EndTimeUnixNano,
                    DurationSeconds = Math.Max(0, (span.EndTimeUnixNano - span.StartTimeUnixNano) / 1_000_000_000.0),
                };

                lock (_lock)
                {
                    AddBounded(_pendingSpans, span, MaxBufferedSpans, ref _droppedSpans);
                    AddBounded(_taskMetrics, stepMetric, MaxBufferedTaskMetrics, ref _droppedTaskMetrics);
                }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel step span (best-effort): {ex.Message}");
            }
        }

        // Best-effort cicd.pipeline.task.type (semconv enum: build|test|deploy); omit when unknown.
        internal static string InferTaskType(string name, string actionName)
        {
            var s = $"{name} {actionName}".ToLowerInvariant();
            if (s.Contains("test") || s.Contains("lint") || s.Contains("spec")) return "test";
            if (s.Contains("deploy") || s.Contains("release") || s.Contains("publish")) return "deploy";
            if (s.Contains("build") || s.Contains("compile") || s.Contains("package")) return "build";
            return null;
        }

        public void RecordJobCompletion(DateTime? startTime, DateTime? endTime, TaskResult? conclusion, long throttlingDelayMs = 0, string errorMessage = null)
        {
            if (!IsEnabled)
            {
                return;
            }

            try
            {
                JobInfo job;
                lock (_lock) { job = _jobInfo; }
                if (job == null || job.RunId == 0)
                {
                    return;
                }

                var ghConclusion = NormalizeConclusion(conclusion);
                var jobSpanId = NewJobSpanID(job.RunId, job.RunAttempt, job.JobName);
                var span = new OTelSpan
                {
                    TraceId = NewTraceID(job.RunId, job.RunAttempt),
                    SpanId = jobSpanId,
                    // Root of the runner's trace. The runner owns the job, not the
                    // workflow run, so we do NOT parent to a run span we never emit
                    // (that left a dangling parentSpanId). The authoritative run span
                    // comes from the GitHub API path and is merged downstream; an
                    // upstream scheduler, when present, is attached as a span link below.
                    ParentSpanId = null,
                    Name = job.JobName,
                    Kind = 2, // SERVER
                    StartTimeUnixNano = ToUnixNano(startTime ?? DateTime.UtcNow),
                    EndTimeUnixNano = ToUnixNano(endTime ?? DateTime.UtcNow),
                };

                span.Set("github.record_type", "job");
                AddInboundParentLink(span);
                AddCommonContext(span, job);
                span.Set("github.conclusion", ghConclusion);
                span.Set("cicd.pipeline.task.name", job.JobName);
                span.Set("cicd.pipeline.task.run.id", span.SpanId);
                span.Set("cicd.pipeline.task.run.result", ToSemconvResult(ghConclusion));
                span.Set("cicd.pipeline.task.run.url.full", $"{job.ServerUrl}/{job.Repository}/actions/runs/{job.RunIdRaw}/attempts/{job.RunAttemptRaw}");
                if (throttlingDelayMs > 0)
                {
                    // Time the runner spent blocked on server throttling during this job.
                    span.Set("github.throttling_delay_ms", throttlingDelayMs);
                }

                ApplyStatus(span, ghConclusion);
                if (ghConclusion == "failure")
                {
                    span.AddException("failure", errorMessage, span.EndTimeUnixNano);
                }

                var durationSeconds = Math.Max(0, (span.EndTimeUnixNano - span.StartTimeUnixNano) / 1_000_000_000.0);
                var metrics = new JobMetrics
                {
                    StartNano = span.StartTimeUnixNano,
                    EndNano = span.EndTimeUnixNano,
                    DurationSeconds = durationSeconds,
                    PipelineName = job.Workflow,
                    Attempt = job.RunAttemptRaw,
                    Result = ToSemconvResult(ghConclusion),
                    Errors = ghConclusion == "failure" ? 1 : 0,
                };
                var jobMetric = new TaskMetric
                {
                    Name = job.JobName,
                    Type = "job",
                    Result = ToSemconvResult(ghConclusion),
                    Attempt = job.RunAttemptRaw,
                    StartNano = span.StartTimeUnixNano,
                    EndNano = span.EndTimeUnixNano,
                    DurationSeconds = durationSeconds,
                };

                lock (_lock)
                {
                    AddBounded(_pendingSpans, span, MaxBufferedSpans, ref _droppedSpans);
                    _jobMetrics = metrics;
                    AddBounded(_taskMetrics, jobMetric, MaxBufferedTaskMetrics, ref _droppedTaskMetrics);
                }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel job span (best-effort): {ex.Message}");
            }
        }

        public void RecordSpan(string name, string spanType, DateTime startTime, DateTime endTime, IDictionary<string, string> attributes = null, string parentStepName = null, int parentStepNumber = 0, int spanKind = 1)
        {
            if (!IsEnabled || string.IsNullOrEmpty(name))
            {
                return;
            }

            try
            {
                JobInfo job;
                lock (_lock) { job = _jobInfo; }
                if (job == null || job.RunId == 0)
                {
                    return;
                }

                // Nest under the owning step (e.g. action resolution runs inside "Set up job"),
                // falling back to the job span when no step is supplied.
                var parentSpanId = string.IsNullOrEmpty(parentStepName)
                    ? NewJobSpanID(job.RunId, job.RunAttempt, job.JobName)
                    : NewStepSpanID(job.RunId, job.RunAttempt, job.JobName, parentStepNumber, parentStepName);

                var startNano = ToUnixNano(startTime);
                var span = new OTelSpan
                {
                    TraceId = NewTraceID(job.RunId, job.RunAttempt),
                    // Include start time so repeated same-named operations don't collide.
                    SpanId = NewSpanIDFromString($"{spanType}-{job.RunId}-{job.RunAttempt}-{job.JobName}-{name}-{startNano}"),
                    ParentSpanId = parentSpanId,
                    Name = name,
                    Kind = spanKind,
                    StartTimeUnixNano = startNano,
                    EndTimeUnixNano = ToUnixNano(endTime),
                };
                span.Set("github.record_type", spanType);
                AddCommonContext(span, job);
                // task.name marks this as a child task (not a root pipeline) for enrichers.
                span.Set("cicd.pipeline.task.name", name);
                if (attributes != null)
                {
                    foreach (var kv in attributes)
                    {
                        span.Set(kv.Key, kv.Value);
                    }
                }
                lock (_lock) { AddBounded(_pendingSpans, span, MaxBufferedSpans, ref _droppedSpans); }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel span '{name}' (best-effort): {ex.Message}");
            }
        }

        public void RecordJobLog(string severityText, string message)
        {
            if (!IsEnabled || string.IsNullOrEmpty(message))
            {
                return;
            }
            try
            {
                JobInfo job;
                lock (_lock) { job = _jobInfo; }
                if (job == null || job.RunId == 0)
                {
                    return;
                }
                var log = new OTelLog
                {
                    TimeUnixNano = ToUnixNano(DateTime.UtcNow),
                    SeverityText = string.IsNullOrEmpty(severityText) ? "INFO" : severityText.ToUpperInvariant(),
                    SeverityNumber = SeverityNumber(severityText),
                    Body = message,
                    TraceId = NewTraceID(job.RunId, job.RunAttempt),
                    SpanId = NewJobSpanID(job.RunId, job.RunAttempt, job.JobName),
                };
                log.Attributes.Add(new("cicd.pipeline.task.name", job.JobName));
                lock (_lock) { AddBounded(_pendingLogs, log, MaxBufferedLogs, ref _droppedLogs); }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel job log (best-effort): {ex.Message}");
            }
        }

        public void RecordStepLog(string stepName, int? stepNumber, string severityText, string message)
        {
            if (!IsEnabled || string.IsNullOrEmpty(message))
            {
                return;
            }
            try
            {
                JobInfo job;
                lock (_lock) { job = _jobInfo; }
                if (job == null || job.RunId == 0 || string.IsNullOrEmpty(stepName))
                {
                    return;
                }
                var log = new OTelLog
                {
                    TimeUnixNano = ToUnixNano(DateTime.UtcNow),
                    SeverityText = string.IsNullOrEmpty(severityText) ? "INFO" : severityText.ToUpperInvariant(),
                    SeverityNumber = SeverityNumber(severityText),
                    Body = message,
                    TraceId = NewTraceID(job.RunId, job.RunAttempt),
                    SpanId = NewStepSpanID(job.RunId, job.RunAttempt, job.JobName, stepNumber ?? 0, stepName),
                };
                log.Attributes.Add(new("cicd.pipeline.task.name", stepName));
                lock (_lock) { AddBounded(_pendingLogs, log, MaxBufferedLogs, ref _droppedLogs); }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel log (best-effort): {ex.Message}");
            }
        }

        public IDictionary<string, string> StepPropagationEnv(string stepName, int? stepNumber)
        {
            var env = new Dictionary<string, string>();
            if (!IsEnabled || !_propagate)
            {
                return env;
            }
            JobInfo job;
            lock (_lock) { job = _jobInfo; }
            if (job == null || job.RunId == 0 || string.IsNullOrEmpty(stepName))
            {
                return env;
            }
            var traceId = NewTraceID(job.RunId, job.RunAttempt);
            var spanId = NewStepSpanID(job.RunId, job.RunAttempt, job.JobName, stepNumber ?? 0, stepName);
            env["TRACEPARENT"] = $"00-{traceId}-{spanId}-01";
            // Base endpoint (the OTel SDK appends /v1/traces, /v1/logs itself).
            env["OTEL_EXPORTER_OTLP_ENDPOINT"] = _baseUrl;
            // NOTE: deliberately do NOT propagate OTEL_EXPORTER_OTLP_HEADERS. It carries the
            // collector credential; handing it to user step processes lets any step read and
            // exfiltrate it. Steps that need to authenticate to the collector must be given a
            // separate, scoped credential out of band.
            env["OTEL_RESOURCE_ATTRIBUTES"] =
                $"service.name=github-actions-job,github.run_id={job.RunIdRaw},github.run_attempt={job.RunAttemptRaw},github.job={job.JobKey},github.repository={job.Repository}";
            return env;
        }

        private static int SeverityNumber(string text)
        {
            return (text?.ToUpperInvariant()) switch
            {
                "ERROR" => 17,
                "WARN" or "WARNING" => 13,
                "NOTICE" => 10,
                "DEBUG" => 5,
                _ => 9, // INFO
            };
        }

        // Append under a buffer cap; excess is dropped and counted (call under _lock).
        private static void AddBounded<T>(List<T> list, T item, int cap, ref long dropped)
        {
            if (list.Count >= cap)
            {
                dropped++;
                return;
            }
            list.Add(item);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return;
            }

            // Export is best-effort and must NEVER affect the job: serialization or any
            // other failure here is contained so it can't escape into job completion.
            try
            {
                List<OTelSpan> spans;
                List<OTelLog> logs;
                JobMetrics metrics;
                List<TaskMetric> tasks;
                List<KeyValuePair<string, object>> resource;
                long droppedSpans, droppedLogs, droppedTasks;
                lock (_lock)
                {
                    spans = new List<OTelSpan>(_pendingSpans);
                    logs = new List<OTelLog>(_pendingLogs);
                    metrics = _jobMetrics;
                    tasks = new List<TaskMetric>(_taskMetrics);
                    droppedSpans = _droppedSpans;
                    droppedLogs = _droppedLogs;
                    droppedTasks = _droppedTaskMetrics;
                    _pendingSpans.Clear();
                    _pendingLogs.Clear();
                    _jobMetrics = null;
                    _taskMetrics.Clear();
                    _droppedSpans = _droppedLogs = _droppedTaskMetrics = 0;
                    resource = _resource;
                }

                if (droppedSpans + droppedLogs + droppedTasks > 0)
                {
                    Trace.Info($"OTel dropped over buffer cap (best-effort): {droppedSpans} span(s), {droppedLogs} log(s), {droppedTasks} task-metric(s)");
                }

                // One overall deadline across all signals (incl. retries) so flush never
                // delays job completion by more than this, even if the collector is slow.
                using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                flushCts.CancelAfter(TimeSpan.FromSeconds(4));
                var flushToken = flushCts.Token;

                // Chunked POSTs: a whole job in one request can exceed a collector's
                // body limit (413 is non-retryable per OTLP — the entire signal would
                // be dropped), and serializing per chunk bounds peak memory at flush.
                for (var i = 0; i < spans.Count; i += MaxItemsPerPost)
                {
                    var batch = spans.GetRange(i, Math.Min(MaxItemsPerPost, spans.Count - i));
                    await _transport.PostAsync(_tracesUrl, BuildOTLPSpansJson(batch, resource), $"{batch.Count} span(s)", flushToken);
                }
                for (var i = 0; i < logs.Count; i += MaxItemsPerPost)
                {
                    var batch = logs.GetRange(i, Math.Min(MaxItemsPerPost, logs.Count - i));
                    await _transport.PostAsync(_logsUrl, BuildOTLPLogsJson(batch, resource), $"{batch.Count} log(s)", flushToken);
                }
                if (metrics != null || tasks.Count > 0)
                {
                    await _transport.PostAsync(_metricsUrl, BuildOTLPMetricsJson(metrics, tasks, resource), "metrics", flushToken);
                }
            }
            catch (Exception ex)
            {
                Trace.Info($"OTel flush failed (best-effort, ignored): {ex.Message}");
            }
        }

        // ---- shared deterministic ID contract (pure, mirrored in otel-explorer) ----
        //
        // SHA-256 (truncated) — NOT MD5: MD5 is disallowed under FIPS, so a FIPS-enabled
        // host would throw. SHA-256 gives the same deterministic-from-run-id behavior;
        // we take the leading 16 bytes for a trace ID and 8 for a span ID. These IDs are
        // intentionally deterministic (not random), so the W3C Level-2 randomness flag is
        // never set on the trace.

        internal static string NewTraceID(long runId, long runAttempt)
        {
            if (runAttempt == 0) runAttempt = 1;
            return BytesToHex(SHA256.HashData(Encoding.UTF8.GetBytes($"{runId}-{runAttempt}")), 16);
        }

        internal static string NewSpanID(long id)
        {
            var bytes = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                bytes[i] = (byte)(id >> (56 - 8 * i));
            }
            return BytesToHex(bytes);
        }

        internal static string NewSpanIDFromString(string s)
        {
            return BytesToHex(SHA256.HashData(Encoding.UTF8.GetBytes(s)), 8);
        }

        internal static string NewJobSpanID(long runId, long runAttempt, string jobName)
        {
            if (runAttempt == 0) runAttempt = 1;
            return NewSpanIDFromString($"job-{runId}-{runAttempt}-{jobName}");
        }

        internal static string NewStepSpanID(long runId, long runAttempt, string jobName, int stepNumber, string stepName)
        {
            if (runAttempt == 0) runAttempt = 1;
            // Step number disambiguates two top-level steps that share a display name;
            // it matches the GitHub API's 1-based step.number so the IDs still merge.
            return NewSpanIDFromString($"step-{runId}-{runAttempt}-{jobName}-{stepNumber}-{stepName}");
        }

        // ---- helpers ----

        private static void ApplyStatus(OTelSpan span, string ghConclusion)
        {
            if (ghConclusion == "failure")
            {
                span.StatusCode = 2; // ERROR
                span.Set("error.type", "failure");
            }
        }

        internal static string NormalizeConclusion(TaskResult? result)
        {
            return result switch
            {
                TaskResult.Succeeded => "success",
                TaskResult.SucceededWithIssues => "success",
                TaskResult.Failed => "failure",
                TaskResult.Abandoned => "failure",
                TaskResult.Canceled => "cancelled",
                TaskResult.Skipped => "skipped",
                _ => "unknown",
            };
        }

        internal static string ToSemconvResult(string ghConclusion)
        {
            // semconv cicd.pipeline.task.run.result enum: success|failure|cancellation|skip|timeout|error
            return ghConclusion switch
            {
                "success" => "success",
                "failure" => "failure",
                "cancelled" => "cancellation",
                "skipped" => "skip",
                _ => "error",
            };
        }

        private static List<KeyValuePair<string, object>> DefaultResource()
        {
            return new List<KeyValuePair<string, object>>
            {
                new("service.name", "github-actions-runner"),
            };
        }

        // If an upstream scheduler injected a W3C traceparent, attach it to the job span
        // as a span LINK (cross-trace causality) — we do NOT re-root the job's own
        // deterministic trace. tracestate is preserved and the inbound sampled flag is
        // carried on the link rather than overridden.
        private void AddInboundParentLink(OTelSpan span)
        {
            var traceparent = Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpParentTraceparent);
            if (!TryParseTraceparent(traceparent, out var traceId, out var spanId, out var flags))
            {
                return;
            }
            var link = new OTelLink { TraceId = traceId, SpanId = spanId, Flags = flags };
            var tracestate = Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpParentTracestate);
            if (!string.IsNullOrEmpty(tracestate))
            {
                link.TraceState = tracestate.Trim();
            }
            // The linked span belongs to the controller that scheduled this job (semconv).
            link.Attributes.Add(new KeyValuePair<string, object>("cicd.system.component", "controller"));
            span.Links.Add(link);
        }

        // TryParseTraceparent validates a W3C traceparent ("version-traceid-spanid-flags").
        // Rejects malformed input and the forbidden "ff" version; tolerates unknown future
        // versions by reading only the first four fields.
        internal static bool TryParseTraceparent(string traceparent, out string traceId, out string spanId, out byte flags)
        {
            traceId = null;
            spanId = null;
            flags = 0;
            if (string.IsNullOrEmpty(traceparent))
            {
                return false;
            }
            var parts = traceparent.Trim().Split('-');
            if (parts.Length < 4)
            {
                return false;
            }
            var version = parts[0];
            if (version.Length != 2 || !IsHex(version) || version == "ff")
            {
                return false;
            }
            // Version 00 is exactly four fields; unknown versions may carry more.
            if (version == "00" && parts.Length != 4)
            {
                return false;
            }
            var tid = parts[1];
            var sid = parts[2];
            var flg = parts[3];
            if (tid.Length != 32 || !IsHex(tid) || IsAllZeroHex(tid))
            {
                return false;
            }
            if (sid.Length != 16 || !IsHex(sid) || IsAllZeroHex(sid))
            {
                return false;
            }
            if (flg.Length != 2 || !IsHex(flg))
            {
                return false;
            }
            traceId = tid.ToLowerInvariant();
            spanId = sid.ToLowerInvariant();
            flags = Convert.ToByte(flg, 16);
            return true;
        }

        private static bool IsHex(string s)
        {
            foreach (var c in s)
            {
                var hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsAllZeroHex(string s)
        {
            foreach (var c in s)
            {
                if (c != '0')
                {
                    return false;
                }
            }
            return true;
        }

        private static string BytesToHex(byte[] bytes, int length = 0)
        {
            if (length <= 0) length = bytes.Length;
            var sb = new StringBuilder(length * 2);
            for (int i = 0; i < length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }

        private static long ToUnixNano(DateTime dt)
        {
            // Timeline timestamps are UTC; treat an Unspecified kind as UTC rather than
            // letting ToUniversalTime() shift it by the local offset.
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return (dt.ToUniversalTime().Ticks - 621355968000000000L) * 100;
        }

        private string Mask(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            return HostContext?.SecretMasker?.MaskSecrets(s) ?? s;
        }

        internal int PendingSpanCountForTest
        {
            get { lock (_lock) { return _pendingSpans.Count; } }
        }

        internal long DroppedSpanCountForTest
        {
            get { lock (_lock) { return _droppedSpans; } }
        }

        internal int PendingLogCountForTest
        {
            get { lock (_lock) { return _pendingLogs.Count; } }
        }

        internal string BuildPendingOtlpJsonForTest()
        {
            lock (_lock) { return Encoding.UTF8.GetString(BuildOTLPSpansJson(new List<OTelSpan>(_pendingSpans), _resource)); }
        }

        internal string BuildPendingOtlpLogsJsonForTest()
        {
            lock (_lock) { return Encoding.UTF8.GetString(BuildOTLPLogsJson(new List<OTelLog>(_pendingLogs), _resource)); }
        }

        internal string BuildPendingOtlpMetricsJsonForTest()
        {
            lock (_lock)
            {
                return _jobMetrics == null && _taskMetrics.Count == 0
                    ? null
                    : Encoding.UTF8.GetString(BuildOTLPMetricsJson(_jobMetrics, _taskMetrics, _resource));
            }
        }
    }
}
