using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    /// <summary>
    /// Emits native OpenTelemetry trace spans for a job and its steps, plus
    /// runner-identity as an OTLP Resource. Spans use deterministic IDs derived
    /// only from identifiers the runner always has (run id, run attempt, job
    /// display name, step name) so they merge with the GitHub Actions trace
    /// reconstructed by otel-explorer.
    ///
    /// Enabled by setting ACTIONS_RUNNER_OTLP_ENDPOINT to an OTLP/HTTP base URL.
    /// Spans are exported as OTLP/JSON to {endpoint}/v1/traces.
    ///
    /// Shared ID contract (mirrored in otel-explorer pkg/githubapi/ids.go):
    ///   traceID  = md5("{run_id}-{run_attempt}")                                  (16 bytes)
    ///   workflow = bigendian(run_id)                                              (8 bytes)
    ///   job      = md5("job-{run_id}-{run_attempt}-{job_name}")[:8]               (8 bytes)
    ///   step     = md5("step-{run_id}-{run_attempt}-{job_name}-{step_name}")[:8]  (8 bytes)
    /// Parent links: step -> job -> workflow.
    ///
    /// Export is best-effort: failures are swallowed and never affect job execution.
    /// </summary>
    public static class OTelTracer
    {
        private static string s_endpoint;
        private static bool s_initialized;
        private static bool s_enabled;
        private static HttpClient s_httpClient;
        private static readonly object s_lock = new();
        private static readonly List<OTelSpan> s_pendingSpans = new();
        private static JobInfo s_jobInfo;
        private static List<KeyValuePair<string, object>> s_resource = DefaultResource();

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

        private static void EnsureInitialized()
        {
            if (s_initialized) return;
            lock (s_lock)
            {
                if (s_initialized) return;
                s_endpoint = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_OTLP_ENDPOINT")?.TrimEnd('/');
                s_enabled = !string.IsNullOrEmpty(s_endpoint);
                if (s_enabled)
                {
                    var insecure = StringUtil.ConvertToBoolean(
                        Environment.GetEnvironmentVariable("ACTIONS_RUNNER_OTLP_INSECURE"));
                    var handler = new HttpClientHandler();
                    if (insecure)
                    {
                        handler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }
                    s_httpClient = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromSeconds(5)
                    };
                }
                s_initialized = true;
            }
        }

        public static bool IsEnabled
        {
            get
            {
                EnsureInitialized();
                return s_enabled;
            }
        }

        /// <summary>
        /// Records job-level identifiers once at job initialization. Both step
        /// spans and the job span read from this so their IDs and parent links
        /// stay consistent.
        /// </summary>
        public static void SetJobInfo(
            string runId,
            string runAttempt,
            string jobName,
            string jobKey,
            string repository,
            string workflow,
            string eventName,
            string serverUrl)
        {
            if (!IsEnabled) return;

            long.TryParse(runId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var runIdNum);
            long.TryParse(runAttempt, NumberStyles.Integer, CultureInfo.InvariantCulture, out var runAttemptNum);
            if (runAttemptNum == 0) runAttemptNum = 1;

            lock (s_lock)
            {
                s_jobInfo = new JobInfo
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

        /// <summary>
        /// Sets the OTLP Resource attributes that describe the runner itself
        /// (the "runner info"). Attached to every exported batch.
        /// </summary>
        public static void SetResource(
            string runnerName,
            string runnerId,
            string runnerGroup,
            string runnerVersion,
            string osType,
            string arch,
            string machineName,
            bool ephemeral)
        {
            var attrs = DefaultResource();
            void Add(string k, object v)
            {
                if (v is string s && string.IsNullOrEmpty(s)) return;
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
            lock (s_lock)
            {
                s_resource = attrs;
            }
        }

        private static List<KeyValuePair<string, object>> DefaultResource()
        {
            return new List<KeyValuePair<string, object>>
            {
                new("service.name", "github-actions-runner"),
            };
        }

        /// <summary>
        /// Records a completed step as an OTel span (child of the job span).
        /// Called from ExecutionContext.Complete() for Task-type records.
        /// </summary>
        public static void RecordStepCompletion(
            string stepName,
            int? stepNumber,
            DateTime? startTime,
            DateTime? endTime,
            TaskResult? conclusion,
            string stepType,
            string actionName,
            string actionRef)
        {
            if (!IsEnabled) return;

            try
            {
                JobInfo job;
                lock (s_lock) { job = s_jobInfo; }
                if (job == null || job.RunId == 0 || string.IsNullOrEmpty(stepName)) return;

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
                // OTel CI/CD semconv (task run)
                span.Set("cicd.pipeline.name", job.Workflow);
                span.Set("cicd.pipeline.run.id", job.RunIdRaw);
                span.Set("cicd.pipeline.task.name", stepName);
                span.Set("cicd.pipeline.task.run.id", span.SpanId);
                span.Set("cicd.pipeline.task.run.result", ToSemconvResult(ghConclusion));
                span.Set("cicd.pipeline.task.run.url.full", stepUrl);
                span.Set("vcs.repository.url.full", $"{job.ServerUrl}/{job.Repository}");

                ApplyStatus(span, ghConclusion);

                lock (s_lock) { s_pendingSpans.Add(span); }
            }
            catch
            {
                // best-effort; never fail the step
            }
        }

        /// <summary>
        /// Records the job as an OTel span (parent of all step spans, child of
        /// the workflow span). Called from ExecutionContext.Complete() for the
        /// Job-type record, before spans are flushed.
        /// </summary>
        public static void RecordJobCompletion(
            DateTime? startTime,
            DateTime? endTime,
            TaskResult? conclusion)
        {
            if (!IsEnabled) return;

            try
            {
                JobInfo job;
                lock (s_lock) { job = s_jobInfo; }
                if (job == null || job.RunId == 0) return;

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
                // OTel CI/CD semconv (treat the job as a task run within the pipeline)
                span.Set("cicd.pipeline.name", job.Workflow);
                span.Set("cicd.pipeline.run.id", job.RunIdRaw);
                span.Set("cicd.pipeline.task.name", job.JobName);
                span.Set("cicd.pipeline.task.run.id", span.SpanId);
                span.Set("cicd.pipeline.task.run.result", ToSemconvResult(ghConclusion));
                span.Set("cicd.pipeline.task.run.url.full", jobUrl);
                span.Set("vcs.repository.url.full", $"{job.ServerUrl}/{job.Repository}");

                ApplyStatus(span, ghConclusion);

                lock (s_lock) { s_pendingSpans.Add(span); }
            }
            catch
            {
                // best-effort
            }
        }

        /// <summary>Flushes all pending spans to the OTLP endpoint.</summary>
        public static async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled) return;

            List<OTelSpan> toFlush;
            List<KeyValuePair<string, object>> resource;
            lock (s_lock)
            {
                if (s_pendingSpans.Count == 0) return;
                toFlush = new List<OTelSpan>(s_pendingSpans);
                s_pendingSpans.Clear();
                resource = s_resource;
            }

            try
            {
                var json = BuildOTLPJson(toFlush, resource);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{s_endpoint}/v1/traces";
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                await s_httpClient.PostAsync(url, content, cts.Token);
            }
            catch
            {
                // best-effort export
            }
        }

        // ---- shared deterministic ID contract ----

        internal static string NewTraceID(long runId, long runAttempt)
        {
            if (runAttempt == 0) runAttempt = 1;
            var input = $"{runId}-{runAttempt}";
            return BytesToHex(MD5.HashData(Encoding.UTF8.GetBytes(input)));
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

        private static string BuildOTLPJson(List<OTelSpan> spans, List<KeyValuePair<string, object>> resource)
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
                sb.Append($"\"name\":\"{JsonEscape(s.Name)}\",");
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

        private static void AppendAttributes(StringBuilder sb, List<KeyValuePair<string, object>> attrs)
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
                        sb.Append($"\"stringValue\":\"{JsonEscape(kv.Value?.ToString())}\"");
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

        internal static int PendingSpanCountForTest
        {
            get { lock (s_lock) { return s_pendingSpans.Count; } }
        }

        internal static string BuildPendingOtlpJsonForTest()
        {
            lock (s_lock) { return BuildOTLPJson(new List<OTelSpan>(s_pendingSpans), s_resource); }
        }

        internal static void Reset()
        {
            lock (s_lock)
            {
                s_initialized = false;
                s_enabled = false;
                s_endpoint = null;
                s_httpClient?.Dispose();
                s_httpClient = null;
                s_pendingSpans.Clear();
                s_jobInfo = null;
                s_resource = DefaultResource();
            }
        }
    }
}
