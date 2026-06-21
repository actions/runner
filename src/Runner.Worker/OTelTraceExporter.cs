using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
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
        void RecordStepCompletion(string stepName, int? stepNumber, DateTime? startTime, DateTime? endTime, TaskResult? conclusion, string stepType, string actionName, string actionRef, bool isEmbedded = false, string errorMessage = null);
        void RecordJobCompletion(DateTime? startTime, DateTime? endTime, TaskResult? conclusion, long throttlingDelayMs = 0, string errorMessage = null);
        // Generic child span (parented to the job) for finer-grained timing, e.g. action download.
        void RecordSpan(string name, string spanType, DateTime startTime, DateTime endTime, IDictionary<string, string> attributes = null);
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
    /// Shared ID contract (mirrored in otel-explorer pkg/githubapi/ids.go):
    ///   traceID  = sha256("{run_id}-{run_attempt}")                                  (16 bytes)
    ///   workflow = bigendian(run_id)                                              (8 bytes)
    ///   job      = sha256("job-{run_id}-{run_attempt}-{job_name}")[:8]               (8 bytes)
    ///   step     = sha256("step-{run_id}-{run_attempt}-{job_name}-{step_name}")[:8]  (8 bytes)
    /// Parent links: step -> job -> workflow.
    /// </summary>
    public sealed class OTelTraceExporter : RunnerService, IOTelTraceExporter
    {
        private static readonly JsonWriterOptions s_jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        // Declares which semantic-convention version this telemetry conforms to.
        private const string SchemaUrl = "https://opentelemetry.io/schemas/1.29.0";
        private string _serviceVersion = "";

        private readonly object _lock = new();
        private readonly List<OTelSpan> _pendingSpans = new();
        private readonly List<OTelLog> _pendingLogs = new();
        private string _endpoint;
        private string _baseUrl;
        private string _tracesUrl;
        private string _logsUrl;
        private string _metricsUrl;
        private string _rawHeaders;
        private JobMetrics _jobMetrics;
        private bool _enabled;
        // Inject W3C TRACEPARENT + OTEL_* into each step's env so in-job tools/actions
        // emit spans parented to the step. Opt-in (ACTIONS_RUNNER_OTLP_PROPAGATE).
        private bool _propagate;
        // Server-side kill switch, captured at job init. Defaults to true so the
        // operator's endpoint opt-in works on self-hosted/GHES where the flag isn't
        // provisioned; GitHub can send false to disable export fleet-wide.
        private bool _featureEnabled = true;
        private HttpClient _httpClient;
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
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            // Optional collector auth headers, e.g.
            //   ACTIONS_RUNNER_OTLP_HEADERS="authorization=Bearer xyz,x-api-key=abc"
            _rawHeaders = Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpHeaders);
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
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(pair.Substring(0, eq).Trim(), value);
                }
            }
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
            // semconv cicd.worker.* identifies the executor; keep github.runner.* too.
            Add("cicd.worker.name", runnerName);
            Add("cicd.worker.id", runnerId);
            Add("service.instance.id", runnerId);
            Add("github.runner.name", runnerName);
            Add("github.runner.id", runnerId);
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

        public void RecordStepCompletion(string stepName, int? stepNumber, DateTime? startTime, DateTime? endTime, TaskResult? conclusion, string stepType, string actionName, string actionRef, bool isEmbedded = false, string errorMessage = null)
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

                span.Set("type", "step");
                span.Set("source", "runner");
                AddCommonContext(span, job);
                span.Set("github.step_number", (long)(stepNumber ?? 0));
                span.Set("github.conclusion", ghConclusion);
                if (!string.IsNullOrEmpty(stepType)) span.Set("github.step_type", stepType);
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

                lock (_lock) { _pendingSpans.Add(span); }
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
                    ParentSpanId = NewSpanID(job.RunId), // workflow span
                    Name = job.JobName,
                    Kind = 2, // SERVER
                    StartTimeUnixNano = ToUnixNano(startTime ?? DateTime.UtcNow),
                    EndTimeUnixNano = ToUnixNano(endTime ?? DateTime.UtcNow),
                };

                span.Set("type", "job");
                span.Set("source", "runner");
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

                var metrics = new JobMetrics
                {
                    StartNano = span.StartTimeUnixNano,
                    EndNano = span.EndTimeUnixNano,
                    DurationSeconds = Math.Max(0, (span.EndTimeUnixNano - span.StartTimeUnixNano) / 1_000_000_000.0),
                    PipelineName = job.Workflow,
                    Result = ToSemconvResult(ghConclusion),
                    Errors = ghConclusion == "failure" ? 1 : 0,
                };

                lock (_lock)
                {
                    _pendingSpans.Add(span);
                    _jobMetrics = metrics;
                }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel job span (best-effort): {ex.Message}");
            }
        }

        public void RecordSpan(string name, string spanType, DateTime startTime, DateTime endTime, IDictionary<string, string> attributes = null)
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

                var startNano = ToUnixNano(startTime);
                var span = new OTelSpan
                {
                    TraceId = NewTraceID(job.RunId, job.RunAttempt),
                    // Include start time so repeated same-named operations don't collide.
                    SpanId = NewSpanIDFromString($"{spanType}-{job.RunId}-{job.RunAttempt}-{job.JobName}-{name}-{startNano}"),
                    ParentSpanId = NewJobSpanID(job.RunId, job.RunAttempt, job.JobName),
                    Name = name,
                    Kind = 1,
                    StartTimeUnixNano = startNano,
                    EndTimeUnixNano = ToUnixNano(endTime),
                };
                span.Set("type", spanType);
                span.Set("source", "runner");
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
                lock (_lock) { _pendingSpans.Add(span); }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel span '{name}' (best-effort): {ex.Message}");
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
                lock (_lock) { _pendingLogs.Add(log); }
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
            if (!string.IsNullOrEmpty(_rawHeaders))
            {
                env["OTEL_EXPORTER_OTLP_HEADERS"] = _rawHeaders;
            }
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

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return;
            }

            List<OTelSpan> spans;
            List<OTelLog> logs;
            JobMetrics metrics;
            List<KeyValuePair<string, object>> resource;
            lock (_lock)
            {
                spans = new List<OTelSpan>(_pendingSpans);
                logs = new List<OTelLog>(_pendingLogs);
                metrics = _jobMetrics;
                _pendingSpans.Clear();
                _pendingLogs.Clear();
                _jobMetrics = null;
                resource = _resource;
            }

            if (spans.Count > 0)
            {
                await PostAsync(_tracesUrl, BuildOTLPSpansJson(spans, resource), $"{spans.Count} span(s)", cancellationToken);
            }
            if (logs.Count > 0)
            {
                await PostAsync(_logsUrl, BuildOTLPLogsJson(logs, resource), $"{logs.Count} log(s)", cancellationToken);
            }
            if (metrics != null)
            {
                await PostAsync(_metricsUrl, BuildOTLPMetricsJson(metrics, resource), "metrics", cancellationToken);
            }
        }

        private async Task PostAsync(string url, string json, string what, CancellationToken cancellationToken)
        {
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                // Keep job completion snappy even when the collector is slow/unreachable.
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                using var response = await _httpClient.PostAsync(url, content, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    Trace.Info($"Exported {what} to {url}");
                }
                else
                {
                    Trace.Info($"OTel export rejected by collector (best-effort, ignored): HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Trace.Info($"OTel export failed (best-effort, ignored): {ex.Message}");
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

        // OTLP/JSON, serialized with Utf8JsonWriter for correct escaping (a control
        // character in a step name must not be able to invalidate the whole batch).
        private string BuildOTLPSpansJson(List<OTelSpan> spans, List<KeyValuePair<string, object>> resource)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, s_jsonOptions))
            {
                w.WriteStartObject();
                w.WriteStartArray("resourceSpans");
                w.WriteStartObject();

                w.WriteStartObject("resource");
                w.WriteStartArray("attributes");
                WriteAttributes(w, resource);
                w.WriteEndArray();
                w.WriteEndObject();

                w.WriteStartArray("scopeSpans");
                w.WriteStartObject();
                w.WriteStartObject("scope");
                w.WriteString("name", "github.actions.runner");
                if (!string.IsNullOrEmpty(_serviceVersion)) w.WriteString("version", _serviceVersion);
                w.WriteEndObject();

                w.WriteStartArray("spans");
                foreach (var s in spans)
                {
                    w.WriteStartObject();
                    w.WriteString("traceId", s.TraceId);
                    w.WriteString("spanId", s.SpanId);
                    if (!string.IsNullOrEmpty(s.ParentSpanId))
                    {
                        w.WriteString("parentSpanId", s.ParentSpanId);
                    }
                    w.WriteString("name", Mask(s.Name) ?? "");
                    w.WriteNumber("kind", s.Kind);
                    w.WriteString("startTimeUnixNano", s.StartTimeUnixNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteString("endTimeUnixNano", s.EndTimeUnixNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteStartArray("attributes");
                    WriteAttributes(w, s.Attributes);
                    w.WriteEndArray();
                    if (s.Events.Count > 0)
                    {
                        w.WriteStartArray("events");
                        foreach (var ev in s.Events)
                        {
                            w.WriteStartObject();
                            w.WriteString("name", ev.Name);
                            w.WriteString("timeUnixNano", ev.TimeUnixNano.ToString(CultureInfo.InvariantCulture));
                            w.WriteStartArray("attributes");
                            WriteAttributes(w, ev.Attributes);
                            w.WriteEndArray();
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                    }
                    w.WriteStartObject("status");
                    if (s.StatusCode != 0)
                    {
                        w.WriteNumber("code", s.StatusCode);
                    }
                    w.WriteEndObject();
                    w.WriteEndObject();
                }
                w.WriteEndArray(); // spans
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // scopeSpans[0]
                w.WriteEndArray();  // scopeSpans
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // resourceSpans[0]
                w.WriteEndArray();  // resourceSpans
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private void WriteAttributes(Utf8JsonWriter w, List<KeyValuePair<string, object>> attrs)
        {
            foreach (var kv in attrs)
            {
                w.WriteStartObject();
                w.WriteString("key", kv.Key);
                w.WriteStartObject("value");
                switch (kv.Value)
                {
                    case bool b:
                        w.WriteBoolean("boolValue", b);
                        break;
                    case long l:
                        w.WriteString("intValue", l.ToString(CultureInfo.InvariantCulture));
                        break;
                    case int n:
                        w.WriteString("intValue", n.ToString(CultureInfo.InvariantCulture));
                        break;
                    default:
                        w.WriteString("stringValue", Mask(kv.Value?.ToString()) ?? "");
                        break;
                }
                w.WriteEndObject();
                w.WriteEndObject();
            }
        }

        private string BuildOTLPLogsJson(List<OTelLog> logs, List<KeyValuePair<string, object>> resource)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, s_jsonOptions))
            {
                w.WriteStartObject();
                w.WriteStartArray("resourceLogs");
                w.WriteStartObject();
                w.WriteStartObject("resource");
                w.WriteStartArray("attributes");
                WriteAttributes(w, resource);
                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteStartArray("scopeLogs");
                w.WriteStartObject();
                w.WriteStartObject("scope");
                w.WriteString("name", "github.actions.runner");
                w.WriteEndObject();
                w.WriteStartArray("logRecords");
                foreach (var l in logs)
                {
                    w.WriteStartObject();
                    w.WriteString("timeUnixNano", l.TimeUnixNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteNumber("severityNumber", l.SeverityNumber);
                    w.WriteString("severityText", l.SeverityText);
                    w.WriteStartObject("body");
                    w.WriteString("stringValue", Mask(l.Body) ?? "");
                    w.WriteEndObject();
                    w.WriteString("traceId", l.TraceId);
                    w.WriteString("spanId", l.SpanId);
                    w.WriteStartArray("attributes");
                    WriteAttributes(w, l.Attributes);
                    w.WriteEndArray();
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private sealed class OTelLog
        {
            public long TimeUnixNano;
            public int SeverityNumber;
            public string SeverityText;
            public string Body;
            public string TraceId;
            public string SpanId;
            public readonly List<KeyValuePair<string, object>> Attributes = new();
        }

        private sealed class JobMetrics
        {
            public long StartNano;
            public long EndNano;
            public double DurationSeconds;
            public string PipelineName;
            public string Result;
            public int Errors;
        }

        // OTLP/JSON metrics: cicd.pipeline.run.duration (histogram) + cicd.pipeline.run.errors (counter).
        private string BuildOTLPMetricsJson(JobMetrics m, List<KeyValuePair<string, object>> resource)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, s_jsonOptions))
            {
                w.WriteStartObject();
                w.WriteStartArray("resourceMetrics");
                w.WriteStartObject();
                w.WriteStartObject("resource");
                w.WriteStartArray("attributes");
                WriteAttributes(w, resource);
                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteStartArray("scopeMetrics");
                w.WriteStartObject();
                w.WriteStartObject("scope");
                w.WriteString("name", "github.actions.runner");
                if (!string.IsNullOrEmpty(_serviceVersion)) w.WriteString("version", _serviceVersion);
                w.WriteEndObject();
                w.WriteStartArray("metrics");

                // cicd.pipeline.run.duration — histogram with a single observation.
                w.WriteStartObject();
                w.WriteString("name", "cicd.pipeline.run.duration");
                w.WriteString("unit", "s");
                w.WriteStartObject("histogram");
                w.WriteNumber("aggregationTemporality", 2); // CUMULATIVE
                w.WriteStartArray("dataPoints");
                w.WriteStartObject();
                w.WriteStartArray("attributes");
                WriteAttributes(w, new List<KeyValuePair<string, object>>
                {
                    new("cicd.pipeline.name", m.PipelineName),
                    new("cicd.pipeline.result", m.Result),
                });
                w.WriteEndArray();
                w.WriteString("startTimeUnixNano", m.StartNano.ToString(CultureInfo.InvariantCulture));
                w.WriteString("timeUnixNano", m.EndNano.ToString(CultureInfo.InvariantCulture));
                w.WriteString("count", "1");
                w.WriteNumber("sum", m.DurationSeconds);
                w.WriteStartArray("bucketCounts");
                w.WriteStringValue("1");
                w.WriteEndArray();
                w.WriteStartArray("explicitBounds");
                w.WriteEndArray();
                w.WriteNumber("min", m.DurationSeconds);
                w.WriteNumber("max", m.DurationSeconds);
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject(); // histogram
                w.WriteEndObject(); // metric

                if (m.Errors > 0)
                {
                    w.WriteStartObject();
                    w.WriteString("name", "cicd.pipeline.run.errors");
                    w.WriteString("unit", "{error}");
                    w.WriteStartObject("sum");
                    w.WriteNumber("aggregationTemporality", 2);
                    w.WriteBoolean("isMonotonic", true);
                    w.WriteStartArray("dataPoints");
                    w.WriteStartObject();
                    w.WriteStartArray("attributes");
                    WriteAttributes(w, new List<KeyValuePair<string, object>>
                    {
                        new("cicd.pipeline.name", m.PipelineName),
                        new("error.type", "failure"),
                    });
                    w.WriteEndArray();
                    w.WriteString("startTimeUnixNano", m.StartNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteString("timeUnixNano", m.EndNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteString("asInt", m.Errors.ToString(CultureInfo.InvariantCulture));
                    w.WriteEndObject();
                    w.WriteEndArray();
                    w.WriteEndObject();
                    w.WriteEndObject();
                }

                w.WriteEndArray(); // metrics
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // scopeMetrics[0]
                w.WriteEndArray();
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // resourceMetrics[0]
                w.WriteEndArray();
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private sealed class OTelEvent
        {
            public string Name;
            public long TimeUnixNano;
            public readonly List<KeyValuePair<string, object>> Attributes = new();
        }

        private sealed class OTelSpan
        {
            public string TraceId;
            public string SpanId;
            public string ParentSpanId;
            public string Name;
            public int Kind = 1;
            public long StartTimeUnixNano;
            public long EndTimeUnixNano;
            public int StatusCode; // 0 unset, 1 ok, 2 error
            public readonly List<KeyValuePair<string, object>> Attributes = new();
            public readonly List<OTelEvent> Events = new();

            public void Set(string key, object value)
            {
                Attributes.Add(new KeyValuePair<string, object>(key, value));
            }

            // semconv exception event.
            public void AddException(string type, string message, long timeNano)
            {
                var ev = new OTelEvent { Name = "exception", TimeUnixNano = timeNano };
                ev.Attributes.Add(new("exception.type", type));
                if (!string.IsNullOrEmpty(message))
                {
                    ev.Attributes.Add(new("exception.message", message));
                }
                Events.Add(ev);
            }
        }

        internal int PendingSpanCountForTest
        {
            get { lock (_lock) { return _pendingSpans.Count; } }
        }

        internal int PendingLogCountForTest
        {
            get { lock (_lock) { return _pendingLogs.Count; } }
        }

        internal string BuildPendingOtlpJsonForTest()
        {
            lock (_lock) { return BuildOTLPSpansJson(new List<OTelSpan>(_pendingSpans), _resource); }
        }

        internal string BuildPendingOtlpLogsJsonForTest()
        {
            lock (_lock) { return BuildOTLPLogsJson(new List<OTelLog>(_pendingLogs), _resource); }
        }

        internal string BuildPendingOtlpMetricsJsonForTest()
        {
            lock (_lock) { return _jobMetrics == null ? null : BuildOTLPMetricsJson(_jobMetrics, _resource); }
        }
    }
}
