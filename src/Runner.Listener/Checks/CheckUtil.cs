using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;

namespace GitHub.Runner.Listener.Check
{
    public static class CheckUtil
    {
        public static List<string> WarnLog(this IHostContext hostContext)
        {
            var logs = new List<string>();
            logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
            logs.Add($"{DateTime.UtcNow.ToString("O")} ****     !!! WARNING !!! ");
            logs.Add($"{DateTime.UtcNow.ToString("O")} ****     DO NOT share the log in public place! The log may contains secrets in plain text. ");
            logs.Add($"{DateTime.UtcNow.ToString("O")} ****     !!! WARNING !!! ");
            logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
            logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            return logs;
        }

        public static List<string> CheckProxy(this IHostContext hostContext)
        {
            var logs = new List<string>();
            if (!string.IsNullOrEmpty(hostContext.WebProxy.HttpProxyAddress) ||
                !string.IsNullOrEmpty(hostContext.WebProxy.HttpsProxyAddress))
            {
                logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Runner is behind web proxy {hostContext.WebProxy.HttpsProxyAddress ?? hostContext.WebProxy.HttpProxyAddress} ");
                logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            }

            return logs;
        }

        public static async Task<CheckResult> CheckDns(string targetUrl)
        {
            var result = new CheckResult();
            var url = new Uri(targetUrl);
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Try DNS lookup for {url.Host} ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                IPHostEntry host = await Dns.GetHostEntryAsync(url.Host);
                foreach (var address in host.AddressList)
                {
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Resolved DNS for {url.Host} to '{address}'");
                }

                result.Pass = true;
            }
            catch (Exception ex)
            {
                result.Pass = false;
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Resolved DNS for {url.Host} failed with error: {ex}");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            }

            return result;
        }

        public static async Task<CheckResult> CheckPing(string targetUrl)
        {
            var result = new CheckResult();
            var url = new Uri(targetUrl);
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Try ping {url.Host} ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(url.Host);
                    if (reply.Status == IPStatus.Success)
                    {
                        result.Pass = true;
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Ping {url.Host} ({reply.Address}) succeed within to '{reply.RoundtripTime} ms'");
                    }
                    else
                    {
                        result.Pass = false;
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Ping {url.Host} ({reply.Address}) failed with '{reply.Status}'");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Pass = false;
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Ping api.github.com failed with error: {ex}");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            }

            return result;
        }

        public static async Task<CheckResult> CheckHttpsGetRequests(this IHostContext hostContext, string url, string pat, string expectedHeader)
        {
            var result = new CheckResult();
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Send HTTPS Request (GET) to {url} ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                using (var _ = new HttpEventSourceListener(result.Logs))
                using (var httpClientHandler = hostContext.CreateHttpClientHandler())
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.AddRange(hostContext.UserAgents);
                    if (!string.IsNullOrEmpty(pat))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", pat);
                    }

                    var response = await httpClient.GetAsync(url);

                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http status code: {response.StatusCode}");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http response headers: {response.Headers}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http response body: {responseContent}");
                    if (response.IsSuccessStatusCode)
                    {
                        if (response.Headers.Contains(expectedHeader))
                        {
                            result.Pass = true;
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http request 'GET' to {url} succeed");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                        }
                        else
                        {
                            result.Pass = false;
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http request 'GET' to {url} succeed but doesn't have expected HTTP response Header '{expectedHeader}'.");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                        }
                    }
                    else
                    {
                        result.Pass = false;
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http request 'GET' to {url} failed with {response.StatusCode}");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Pass = false;
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Https request 'GET' to {url} failed with error: {ex}");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            }

            return result;
        }

        public static async Task<CheckResult> CheckHttpsPostRequests(this IHostContext hostContext, string url, string pat, string expectedHeader)
        {
            var result = new CheckResult();
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Send HTTPS Request (POST) to {url} ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                using (var _ = new HttpEventSourceListener(result.Logs))
                using (var httpClientHandler = hostContext.CreateHttpClientHandler())
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.AddRange(hostContext.UserAgents);
                    if (!string.IsNullOrEmpty(pat))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", pat);
                    }

                    // Send empty JSON '{}' to service
                    var response = await httpClient.PostAsJsonAsync<Dictionary<string, string>>(url, new Dictionary<string, string>());

                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http status code: {response.StatusCode}");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http response headers: {response.Headers}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http response body: {responseContent}");
                    if (response.Headers.Contains(expectedHeader))
                    {
                        result.Pass = true;
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http request 'POST' to {url} has expected HTTP response header");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                    }
                    else
                    {
                        result.Pass = false;
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http request 'POST' to {url} doesn't have expected HTTP response Header '{expectedHeader}'.");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                        result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Pass = false;
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Https request 'POST' to {url} failed with error: {ex}");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            }

            return result;
        }

        public static async Task<CheckResult> DownloadExtraCA(this IHostContext hostContext, string url, string pat)
        {
            var result = new CheckResult();
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Download SSL Certificate from {url} ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");

                var uri = new Uri(url);
                var env = new Dictionary<string, string>()
                {
                    { "HOSTNAME", uri.Host },
                    { "PORT", uri.IsDefaultPort ? (uri.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : uri.Port.ToString() },
                    { "PATH", uri.AbsolutePath },
                    { "PAT", pat }
                };

                var proxy = hostContext.WebProxy.GetProxy(uri);
                if (proxy != null)
                {
                    env["PROXYHOST"] = proxy.Host;
                    env["PROXYPORT"] = proxy.IsDefaultPort ? (proxy.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : proxy.Port.ToString();
                    if (hostContext.WebProxy.HttpProxyUsername != null ||
                        hostContext.WebProxy.HttpsProxyUsername != null)
                    {
                        env["PROXYUSERNAME"] = hostContext.WebProxy.HttpProxyUsername ?? hostContext.WebProxy.HttpsProxyUsername;
                        env["PROXYPASSWORD"] = hostContext.WebProxy.HttpProxyPassword ?? hostContext.WebProxy.HttpsProxyPassword;
                    }
                    else
                    {
                        env["PROXYUSERNAME"] = "";
                        env["PROXYPASSWORD"] = "";
                    }
                }
                else
                {
                    env["PROXYHOST"] = "";
                    env["PROXYPORT"] = "";
                    env["PROXYUSERNAME"] = "";
                    env["PROXYPASSWORD"] = "";
                }

                using (var processInvoker = hostContext.CreateService<IProcessInvoker>())
                {
                    processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} [STDOUT] {args.Data}");
                        }
                    });

                    processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} [STDERR] {args.Data}");
                        }
                    });

                    var downloadCertScript = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Bin), "checkScripts", "downloadCert");
                    var node16 = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Externals), "node16", "bin", $"node{IOUtil.ExeExtension}");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Run '{node16} \"{downloadCertScript}\"' ");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} {StringUtil.ConvertToJson(env)}");
                    await processInvoker.ExecuteAsync(
                        hostContext.GetDirectory(WellKnownDirectory.Root),
                        node16,
                        $"\"{downloadCertScript}\"",
                        env,
                        true,
                        CancellationToken.None);
                }

                result.Pass = true;
            }
            catch (Exception ex)
            {
                result.Pass = false;
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Download SSL Certificate from '{url}' failed with error: {ex}");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            }

            return result;
        }
    }

    // EventSource listener for dotnet debug trace for HTTP and SSL
    public sealed class HttpEventSourceListener : EventListener
    {
        private readonly List<string> _logs;
        private readonly object _lock = new object();
        private readonly Dictionary<string, HashSet<string>> _ignoredEvent = new Dictionary<string, HashSet<string>>
        {
            {
                "Microsoft-System-Net-Http",
                new HashSet<string>
                {
                    "Info",
                    "Associate",
                    "Enter",
                    "Exit"
                }
            },
            {
                "Microsoft-System-Net-Security",
                new HashSet<string>
                {
                    "Enter",
                    "Exit",
                    "Info",
                    "DumpBuffer",
                    "SslStreamCtor",
                    "SecureChannelCtor",
                    "NoDelegateNoClientCert",
                    "CertsAfterFiltering",
                    "UsingCachedCredential",
                    "SspiSelectedCipherSuite"
                }
            }
        };

        public HttpEventSourceListener(List<string> logs)
        {
            _logs = logs;
            if (Environment.GetEnvironmentVariable("ACTIONS_RUNNER_TRACE_ALL_HTTP_EVENT") == "1")
            {
                _ignoredEvent.Clear();
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);

            if (eventSource.Name == "Microsoft-System-Net-Http" ||
                eventSource.Name == "Microsoft-System-Net-Security")
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            base.OnEventWritten(eventData);
            lock (_lock)
            {
                if (_ignoredEvent.TryGetValue(eventData.EventSource.Name, out var ignored) &&
                    ignored.Contains(eventData.EventName))
                {
                    return;
                }

                _logs.Add($"{DateTime.UtcNow.ToString("O")} [START {eventData.EventSource.Name} - {eventData.EventName}]");
                _logs.AddRange(eventData.Payload.Select(x => string.Join(Environment.NewLine, x.ToString().Split(Environment.NewLine).Select(y => $"{DateTime.UtcNow.ToString("O")} {y}"))));
                _logs.Add($"{DateTime.UtcNow.ToString("O")} [END {eventData.EventSource.Name} - {eventData.EventName}]");
            }
        }
    }
}