using System;
using System.Collections.Generic;
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
    /// Emits OTel trace spans for step execution using deterministic IDs
    /// compatible with otel-explorer's GitHub Actions trace view.
    ///
    /// Enabled by setting ACTIONS_RUNNER_OTLP_ENDPOINT to an OTLP/HTTP base URL.
    /// Spans are exported as OTLP JSON to {endpoint}/v1/traces.
    ///
    /// ID scheme (matches otel-explorer's pkg/githubapi/ids.go):
    ///   TraceID  = MD5("{run_id}-{run_attempt}")
    ///   ParentID = derived from github.job context key
    ///   SpanID   = MD5("runner-step-{run_id}-{step_name}")[:8]
    /// </summary>
    public static class OTelStepTracer
    {
        private static string s_endpoint;
        private static bool s_initialized;
        private static bool s_enabled;
        private static HttpClient s_httpClient;
        private static readonly object s_lock = new();
        private static readonly List<OTelSpan> s_pendingSpans = new();

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
        /// Records a completed step as an OTel span.
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
            string actionRef,
            IExecutionContext context)
        {
            if (!IsEnabled) return;

            try
            {
                var runIdStr = context.GetGitHubContext("run_id");
                var runAttemptStr = context.GetGitHubContext("run_attempt") ?? "1";
                var repository = context.GetGitHubContext("repository") ?? "";
                var workflow = context.GetGitHubContext("workflow") ?? "";
                var eventName = context.GetGitHubContext("event_name") ?? "";
                var serverUrl = context.GetGitHubContext("server_url") ?? "https://github.com";
                var jobName = context.GetGitHubContext("job") ?? "";

                if (string.IsNullOrEmpty(runIdStr) || string.IsNullOrEmpty(stepName)) return;

                long.TryParse(runIdStr, out var runId);
                long.TryParse(runAttemptStr, out var runAttempt);
                if (runAttempt == 0) runAttempt = 1;

                var traceId = NewTraceID(runId, runAttempt);
                var parentSpanId = NewSpanIDFromString(jobName);
                var spanId = NewSpanIDFromString($"runner-step-{runId}-{stepName}");

                var conclusionStr = conclusion?.ToString()?.ToLowerInvariant() ?? "unknown";
                var start = startTime ?? DateTime.UtcNow;
                var end = endTime ?? DateTime.UtcNow;

                var span = new OTelSpan
                {
                    TraceId = traceId,
                    SpanId = spanId,
                    ParentSpanId = parentSpanId,
                    Name = stepName,
                    StartTimeUnixNano = ToUnixNano(start),
                    EndTimeUnixNano = ToUnixNano(end),
                    Attributes = new Dictionary<string, string>
                    {
                        ["type"] = "step",
                        ["source"] = "runner",
                        ["github.step_number"] = (stepNumber ?? 0).ToString(),
                        ["github.conclusion"] = NormalizeConclusion(conclusionStr),
                        ["github.repository"] = repository,
                        ["github.workflow"] = workflow,
                        ["github.event_name"] = eventName,
                        ["github.run_id"] = runIdStr,
                        ["github.run_attempt"] = runAttemptStr,
                        ["github.job"] = jobName,
                        ["cicd.pipeline.task.name"] = stepName,
                        ["cicd.pipeline.task.run.result"] = ConclusionToSemconv(NormalizeConclusion(conclusionStr)),
                        ["cicd.pipeline.run.id"] = runIdStr,
                        ["vcs.repository.url.full"] = $"{serverUrl}/{repository}",
                    }
                };

                if (!string.IsNullOrEmpty(stepType))
                    span.Attributes["github.step_type"] = stepType;
                if (!string.IsNullOrEmpty(actionName))
                    span.Attributes["github.action"] = actionName;
                if (!string.IsNullOrEmpty(actionRef))
                    span.Attributes["github.action_ref"] = actionRef;

                lock (s_lock)
                {
                    s_pendingSpans.Add(span);
                }
            }
            catch
            {
                // OTel export is best-effort; never fail the step
            }
        }

        /// <summary>
        /// Flushes all pending spans to the OTLP endpoint.
        /// Called from JobRunner.CompleteJobAsync().
        /// </summary>
        public static async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled) return;

            List<OTelSpan> toFlush;
            lock (s_lock)
            {
                if (s_pendingSpans.Count == 0) return;
                toFlush = new List<OTelSpan>(s_pendingSpans);
                s_pendingSpans.Clear();
            }

            try
            {
                var json = BuildOTLPJson(toFlush);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{s_endpoint}/v1/traces";
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                await s_httpClient.PostAsync(url, content, cts.Token);
            }
            catch
            {
                // Best-effort export
            }
        }

        internal static string NewTraceID(long runId, long runAttempt)
        {
            if (runAttempt == 0) runAttempt = 1;
            var input = $"{runId}-{runAttempt}";
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return BytesToHex(hash);
        }

        internal static string NewSpanID(long id)
        {
            var bytes = new byte[8];
            bytes[0] = (byte)(id >> 56);
            bytes[1] = (byte)(id >> 48);
            bytes[2] = (byte)(id >> 40);
            bytes[3] = (byte)(id >> 32);
            bytes[4] = (byte)(id >> 24);
            bytes[5] = (byte)(id >> 16);
            bytes[6] = (byte)(id >> 8);
            bytes[7] = (byte)id;
            return BytesToHex(bytes);
        }

        internal static string NewSpanIDFromString(string s)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(s));
            return BytesToHex(hash, 8);
        }

        private static string NormalizeConclusion(string raw)
        {
            return (raw?.ToLowerInvariant()) switch
            {
                "succeeded" or "success" => "success",
                "failed" or "failure" => "failure",
                "cancelled" or "canceled" => "cancelled",
                "skipped" => "skipped",
                "timed_out" or "timedout" => "timed_out",
                _ => raw ?? "unknown"
            };
        }

        private static string ConclusionToSemconv(string conclusion)
        {
            return conclusion switch
            {
                "success" => "success",
                "failure" => "failure",
                "cancelled" => "cancellation",
                "skipped" => "skip",
                "timed_out" => "timeout",
                _ => conclusion
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

        private static string BuildOTLPJson(List<OTelSpan> spans)
        {
            var sb = new StringBuilder();
            sb.Append("{\"resourceSpans\":[{");
            sb.Append("\"resource\":{\"attributes\":[");
            sb.Append("{\"key\":\"service.name\",\"value\":{\"stringValue\":\"github-actions-runner\"}}");
            sb.Append("]},");
            sb.Append("\"scopeSpans\":[{");
            sb.Append("\"scope\":{\"name\":\"actions.runner\"},");
            sb.Append("\"spans\":[");

            for (int i = 0; i < spans.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var s = spans[i];
                sb.Append('{');
                sb.Append($"\"traceId\":\"{s.TraceId}\",");
                sb.Append($"\"spanId\":\"{s.SpanId}\",");
                sb.Append($"\"parentSpanId\":\"{s.ParentSpanId}\",");
                sb.Append($"\"name\":\"{JsonEscape(s.Name)}\",");
                sb.Append("\"kind\":1,");
                sb.Append($"\"startTimeUnixNano\":\"{s.StartTimeUnixNano}\",");
                sb.Append($"\"endTimeUnixNano\":\"{s.EndTimeUnixNano}\",");
                sb.Append("\"attributes\":[");
                int ai = 0;
                foreach (var kv in s.Attributes)
                {
                    if (ai > 0) sb.Append(',');
                    sb.Append($"{{\"key\":\"{JsonEscape(kv.Key)}\",\"value\":{{\"stringValue\":\"{JsonEscape(kv.Value)}\"}}}}");
                    ai++;
                }
                sb.Append("],\"status\":{}}");
            }

            sb.Append("]}]}]}");
            return sb.ToString();
        }

        private static string JsonEscape(string s)
        {
            return s?.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t") ?? "";
        }

        private class OTelSpan
        {
            public string TraceId { get; set; }
            public string SpanId { get; set; }
            public string ParentSpanId { get; set; }
            public string Name { get; set; }
            public long StartTimeUnixNano { get; set; }
            public long EndTimeUnixNano { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
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
            }
        }
    }
}
