using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Runner.Worker
{
    /// <summary>
    /// OTLP/HTTP transport for the native OTel exporter: gzip + one bounded retry on
    /// transient failures + OTLP partial-success detection. Separated from the recorder
    /// and constructor-injectable (HttpMessageHandler) so the wire path is unit-testable
    /// in isolation. Best-effort: never throws; logs via the supplied callback.
    /// </summary>
    internal sealed class OTelHttpTransport : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Action<string> _log;

        public OTelHttpTransport(HttpMessageHandler handler, IReadOnlyList<KeyValuePair<string, string>> headers = null, Action<string> log = null)
        {
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            _log = log ?? (_ => { });
            if (headers != null)
            {
                foreach (var h in headers)
                {
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);
                }
            }
        }

        // gzip the OTLP/JSON body. A job's spans/logs are highly repetitive, so this
        // typically shrinks the payload ~10x — less bandwidth and faster posts.
        internal static byte[] GzipUtf8(string s)
        {
            var raw = Encoding.UTF8.GetBytes(s);
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            {
                gz.Write(raw, 0, raw.Length);
            }
            return ms.ToArray();
        }

        // Exactly the OTLP/HTTP failures table: 429, 502, 503, 504 SHOULD be retried;
        // "All other 4xx or 5xx response status codes MUST NOT be retried" (so no 408/500).
        private static bool IsTransientStatus(int status) =>
            status == 429 || status == 502 || status == 503 || status == 504;

        // OTLP partial success: a 200 response may carry
        // {"partialSuccess":{"rejectedSpans":"N",...,"errorMessage":"..."}}. Returns a
        // short description when items were rejected, else null. Best-effort; never throws.
        internal static string DescribePartialSuccess(string responseBody)
        {
            try
            {
                if (string.IsNullOrEmpty(responseBody) || !responseBody.Contains("partialSuccess")) return null;
                using var doc = JsonDocument.Parse(responseBody);
                if (!doc.RootElement.TryGetProperty("partialSuccess", out var ps)) return null;
                long rejected = 0;
                foreach (var key in new[] { "rejectedSpans", "rejectedDataPoints", "rejectedLogRecords" })
                {
                    if (ps.TryGetProperty(key, out var v))
                    {
                        if (v.ValueKind == JsonValueKind.Number) rejected += v.GetInt64();
                        else if (v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), out var n)) rejected += n;
                    }
                }
                var msg = ps.TryGetProperty("errorMessage", out var em) ? em.GetString() : null;
                if (rejected > 0 || !string.IsNullOrEmpty(msg)) return $"{rejected} rejected: {msg}";
                return null;
            }
            catch { return null; }
        }

        private static async Task<string> ReadPartialSuccessAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try { return DescribePartialSuccess(await response.Content.ReadAsStringAsync(ct)); }
            catch { return null; }
        }

        // POST one OTLP/JSON payload, gzipped, with a single transient retry bounded by the
        // caller's deadline. Returns true if delivered (incl. partial success), false otherwise.
        public async Task<bool> PostAsync(string url, string json, string what, CancellationToken cancellationToken)
        {
            try
            {
                var body = GzipUtf8(json);
                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        using var content = new ByteArrayContent(body);
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        content.Headers.ContentEncoding.Add("gzip");
                        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            var partial = await ReadPartialSuccessAsync(response, cancellationToken);
                            _log(string.IsNullOrEmpty(partial)
                                ? $"Exported {what} to {url} ({body.Length} B gzip)"
                                : $"OTel export partially rejected (best-effort, ignored): {partial}");
                            return true;
                        }
                        if (attempt < 2 && IsTransientStatus((int)response.StatusCode) && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                            continue;
                        }
                        _log($"OTel export rejected by collector (best-effort, ignored): HTTP {(int)response.StatusCode}");
                        return false;
                    }
                    catch (Exception ex) when (attempt < 2 && ex is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
                    {
                        _log($"OTel export attempt {attempt} failed, retrying: {ex.Message}");
                        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _log($"OTel export failed (best-effort, ignored): {ex.Message}");
            }
            return false;
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}
