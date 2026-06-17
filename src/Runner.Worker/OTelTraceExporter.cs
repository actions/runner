using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
        void SetJobInfo(string runId, string runAttempt, string jobName, string jobKey, string repository, string workflow, string eventName, string serverUrl, bool featureEnabled = true);
        void RecordStepCompletion(string stepName, int? stepNumber, DateTime? startTime, DateTime? endTime, TaskResult? conclusion, string stepType, string actionName, string actionRef);
        void RecordJobCompletion(DateTime? startTime, DateTime? endTime, TaskResult? conclusion);
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
    ///   traceID  = md5("{run_id}-{run_attempt}")                                  (16 bytes)
    ///   workflow = bigendian(run_id)                                              (8 bytes)
    ///   job      = md5("job-{run_id}-{run_attempt}-{job_name}")[:8]               (8 bytes)
    ///   step     = md5("step-{run_id}-{run_attempt}-{job_name}-{step_name}")[:8]  (8 bytes)
    /// Parent links: step -> job -> workflow.
    /// </summary>
    public sealed class OTelTraceExporter : RunnerService, IOTelTraceExporter
    {
        private readonly object _lock = new();
        private readonly List<OTelSpan> _pendingSpans = new();
        private string _endpoint;
        private bool _enabled;
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
            var rawHeaders = Environment.GetEnvironmentVariable(Constants.Variables.Agent.OtlpHeaders);
            if (!string.IsNullOrEmpty(rawHeaders))
            {
                foreach (var pair in rawHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(pair.Substring(0, eq).Trim(), pair.Substring(eq + 1).Trim());
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
            Add("service.version", runnerVersion);
            Add("host.name", machineName);
            Add("host.arch", arch);
            Add("os.type", osType);
            Add("github.runner.name", runnerName);
            Add("github.runner.id", runnerId);
            Add("github.runner.group", runnerGroup);
            Add("github.runner.ephemeral", ephemeral);
            lock (_lock)
            {
                _resource = attrs;
            }
        }

        public void SetJobInfo(string runId, string runAttempt, string jobName, string jobKey, string repository, string workflow, string eventName, string serverUrl, bool featureEnabled = true)
        {
            _featureEnabled = featureEnabled;
            if (!_enabled)
            {
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
                };
            }
        }

        public void RecordStepCompletion(string stepName, int? stepNumber, DateTime? startTime, DateTime? endTime, TaskResult? conclusion, string stepType, string actionName, string actionRef)
        {
            if (!IsEnabled)
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
                    SpanId = NewStepSpanID(job.RunId, job.RunAttempt, job.JobName, stepName),
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
                span.Set("github.step_number", (long)(stepNumber ?? 0));
                span.Set("github.conclusion", ghConclusion);
                span.Set("github.repository", job.Repository);
                span.Set("github.workflow", job.Workflow);
                span.Set("github.event_name", job.EventName);
                span.Set("github.run_id", job.RunIdRaw);
                span.Set("github.run_attempt", job.RunAttemptRaw);
                span.Set("github.job", job.JobKey);
                if (!string.IsNullOrEmpty(stepType)) span.Set("github.step_type", stepType);
                if (!string.IsNullOrEmpty(actionName)) span.Set("github.action", actionName);
                if (!string.IsNullOrEmpty(actionRef)) span.Set("github.action_ref", actionRef);
                span.Set("cicd.pipeline.name", job.Workflow);
                span.Set("cicd.pipeline.run.id", job.RunIdRaw);
                span.Set("cicd.pipeline.task.name", stepName);
                span.Set("cicd.pipeline.task.run.id", span.SpanId);
                span.Set("cicd.pipeline.task.run.result", ToSemconvResult(ghConclusion));
                span.Set("cicd.pipeline.task.run.url.full", stepUrl);
                span.Set("vcs.repository.url.full", $"{job.ServerUrl}/{job.Repository}");

                ApplyStatus(span, ghConclusion);

                lock (_lock) { _pendingSpans.Add(span); }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel step span (best-effort): {ex.Message}");
            }
        }

        public void RecordJobCompletion(DateTime? startTime, DateTime? endTime, TaskResult? conclusion)
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
                var span = new OTelSpan
                {
                    TraceId = NewTraceID(job.RunId, job.RunAttempt),
                    SpanId = NewJobSpanID(job.RunId, job.RunAttempt, job.JobName),
                    ParentSpanId = NewSpanID(job.RunId), // workflow span
                    Name = job.JobName,
                    Kind = 2, // SERVER
                    StartTimeUnixNano = ToUnixNano(startTime ?? DateTime.UtcNow),
                    EndTimeUnixNano = ToUnixNano(endTime ?? DateTime.UtcNow),
                };

                var jobUrl = $"{job.ServerUrl}/{job.Repository}/actions/runs/{job.RunIdRaw}/attempts/{job.RunAttemptRaw}";

                span.Set("type", "job");
                span.Set("source", "runner");
                span.Set("github.conclusion", ghConclusion);
                span.Set("github.repository", job.Repository);
                span.Set("github.workflow", job.Workflow);
                span.Set("github.event_name", job.EventName);
                span.Set("github.run_id", job.RunIdRaw);
                span.Set("github.run_attempt", job.RunAttemptRaw);
                span.Set("github.job", job.JobKey);
                span.Set("cicd.pipeline.name", job.Workflow);
                span.Set("cicd.pipeline.run.id", job.RunIdRaw);
                span.Set("cicd.pipeline.task.name", job.JobName);
                span.Set("cicd.pipeline.task.run.id", span.SpanId);
                span.Set("cicd.pipeline.task.run.result", ToSemconvResult(ghConclusion));
                span.Set("cicd.pipeline.task.run.url.full", jobUrl);
                span.Set("vcs.repository.url.full", $"{job.ServerUrl}/{job.Repository}");

                ApplyStatus(span, ghConclusion);

                lock (_lock) { _pendingSpans.Add(span); }
            }
            catch (Exception ex)
            {
                Trace.Info($"Skipping OTel job span (best-effort): {ex.Message}");
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return;
            }

            List<OTelSpan> toFlush;
            List<KeyValuePair<string, object>> resource;
            lock (_lock)
            {
                if (_pendingSpans.Count == 0)
                {
                    return;
                }
                toFlush = new List<OTelSpan>(_pendingSpans);
                _pendingSpans.Clear();
                resource = _resource;
            }

            try
            {
                var json = BuildOTLPJson(toFlush, resource);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                await _httpClient.PostAsync($"{_endpoint}/v1/traces", content, cts.Token);
                Trace.Info($"Exported {toFlush.Count} OTel span(s) to {_endpoint}");
            }
            catch (Exception ex)
            {
                Trace.Info($"OTel export failed (best-effort, ignored): {ex.Message}");
            }
        }

        // ---- shared deterministic ID contract (pure, mirrored in otel-explorer) ----

        internal static string NewTraceID(long runId, long runAttempt)
        {
            if (runAttempt == 0) runAttempt = 1;
            return BytesToHex(MD5.HashData(Encoding.UTF8.GetBytes($"{runId}-{runAttempt}")));
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
            return BytesToHex(MD5.HashData(Encoding.UTF8.GetBytes(s)), 8);
        }

        internal static string NewJobSpanID(long runId, long runAttempt, string jobName)
        {
            if (runAttempt == 0) runAttempt = 1;
            return NewSpanIDFromString($"job-{runId}-{runAttempt}-{jobName}");
        }

        internal static string NewStepSpanID(long runId, long runAttempt, string jobName, string stepName)
        {
            if (runAttempt == 0) runAttempt = 1;
            return NewSpanIDFromString($"step-{runId}-{runAttempt}-{jobName}-{stepName}");
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
            return ghConclusion switch
            {
                "success" => "success",
                "failure" => "failure",
                "cancelled" => "cancellation",
                "skipped" => "skipped",
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

        private string BuildOTLPJson(List<OTelSpan> spans, List<KeyValuePair<string, object>> resource)
        {
            var sb = new StringBuilder();
            sb.Append("{\"resourceSpans\":[{");
            sb.Append("\"resource\":{\"attributes\":[");
            AppendAttributes(sb, resource);
            sb.Append("]},");
            sb.Append("\"scopeSpans\":[{");
            sb.Append("\"scope\":{\"name\":\"github.actions.runner\"},");
            sb.Append("\"spans\":[");

            for (int i = 0; i < spans.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var s = spans[i];
                sb.Append('{');
                sb.Append($"\"traceId\":\"{s.TraceId}\",");
                sb.Append($"\"spanId\":\"{s.SpanId}\",");
                if (!string.IsNullOrEmpty(s.ParentSpanId))
                {
                    sb.Append($"\"parentSpanId\":\"{s.ParentSpanId}\",");
                }
                sb.Append($"\"name\":\"{JsonEscape(Mask(s.Name))}\",");
                sb.Append($"\"kind\":{s.Kind},");
                sb.Append($"\"startTimeUnixNano\":\"{s.StartTimeUnixNano}\",");
                sb.Append($"\"endTimeUnixNano\":\"{s.EndTimeUnixNano}\",");
                sb.Append("\"attributes\":[");
                AppendAttributes(sb, s.Attributes);
                sb.Append(']');
                sb.Append($",\"status\":{{{(s.StatusCode != 0 ? $"\"code\":{s.StatusCode}" : "")}}}");
                sb.Append('}');
            }

            sb.Append("]}]}]}");
            return sb.ToString();
        }

        private void AppendAttributes(StringBuilder sb, List<KeyValuePair<string, object>> attrs)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var kv = attrs[i];
                sb.Append($"{{\"key\":\"{JsonEscape(kv.Key)}\",\"value\":{{");
                switch (kv.Value)
                {
                    case bool b:
                        sb.Append($"\"boolValue\":{(b ? "true" : "false")}");
                        break;
                    case long l:
                        sb.Append($"\"intValue\":\"{l}\"");
                        break;
                    case int n:
                        sb.Append($"\"intValue\":\"{n}\"");
                        break;
                    default:
                        sb.Append($"\"stringValue\":\"{JsonEscape(Mask(kv.Value?.ToString()))}\"");
                        break;
                }
                sb.Append("}}");
            }
        }

        private static string JsonEscape(string s)
        {
            return s?.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t") ?? "";
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

            public void Set(string key, object value)
            {
                Attributes.Add(new KeyValuePair<string, object>(key, value));
            }
        }

        internal int PendingSpanCountForTest
        {
            get { lock (_lock) { return _pendingSpans.Count; } }
        }

        internal string BuildPendingOtlpJsonForTest()
        {
            lock (_lock) { return BuildOTLPJson(new List<OTelSpan>(_pendingSpans), _resource); }
        }
    }
}
