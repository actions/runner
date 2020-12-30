using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private const string _nodejsCertDownloadScript = @"
const https = require('https')
const fs = require('fs')
const http = require('http')
const hostname = '<HOSTNAME>'
const port = '<PORT>'
const path = '<PATH>'
const pat = '<PAT>'
const proxyHost = '<PROXYHOST>'
const proxyPort = '<PROXYPORT>'
const proxyUsername = '<PROXYUSERNAME>'
const proxyPassword = '<PROXYPASSWORD>'

process.env['NODE_TLS_REJECT_UNAUTHORIZED'] = '0'

if (proxyHost === '') {
    const options = {
        hostname: hostname,
        port: port,
        path: path,
        method: 'GET',
        headers: {
            'User-Agent': 'GitHubActionsRunnerCheck/1.0',
            'Authorization': `token ${pat}`
        },
    }
    const req = https.request(options, res => {
        console.log(`statusCode: ${res.statusCode}`)
        console.log(`headers: ${JSON.stringify(res.headers)}`)
        let cert = socket.getPeerCertificate(true)
        let certPEM = ''
        let fingerprints = {}
        while (cert != null && fingerprints[cert.fingerprint] != '1') {
            fingerprints[cert.fingerprint] = '1'
            certPEM = certPEM + '-----BEGIN CERTIFICATE-----\n'
            let certEncoded = cert.raw.toString('base64')
            for (let i = 0; i < certEncoded.length; i++) {
                certPEM = certPEM + certEncoded[i]
                if (i != certEncoded.length - 1 && (i + 1) % 64 == 0) {
                    certPEM = certPEM + '\n'
                }
            }
            certPEM = certPEM + '\n-----END CERTIFICATE-----\n'
            cert = cert.issuerCertificate
        }
        console.log(certPEM)
        fs.writeFileSync('./download_ca_cert.pem', certPEM)
        res.on('data', d => {
            process.stdout.write(d)
        })
    })
    req.on('error', error => {
        console.error(error)
    })
    req.end()
}
else {
    const auth = 'Basic ' + Buffer.from(proxyUsername + ':' + proxyPassword).toString('base64')

    const options = {
        host: proxyHost,
        port: proxyPort,
        method: 'CONNECT',
        path: `${hostname}:${port}`,
    }

    if (proxyUsername != '' || proxyPassword != '') {
        options.headers = {
            'Proxy-Authorization': auth,
        }
    }

    http.request(options).on('connect', (res, socket) => {
        if (res.statusCode != 200) {
            throw new Error(`Proxy returns code: ${res.statusCode}`)
        }

        https.get({
            host: hostname,
            port: port,
            socket: socket,
            agent: false,
            path: '/',
            headers: {
                'User-Agent': 'GitHubActionsRunnerCheck/1.0',
                'Authorization': `token ${pat}`
            }
        }, (res) => {
            let cert = res.socket.getPeerCertificate(true)
            let certPEM = ''
            let fingerprints = {}
            while (cert != null && fingerprints[cert.fingerprint] != '1') {
                fingerprints[cert.fingerprint] = '1'
                certPEM = certPEM + '-----BEGIN CERTIFICATE-----\n'
                let certEncoded = cert.raw.toString('base64')
                for (let i = 0; i < certEncoded.length; i++) {
                    certPEM = certPEM + certEncoded[i]
                    if (i != certEncoded.length - 1 && (i + 1) % 64 == 0) {
                        certPEM = certPEM + '\n'
                    }
                }
                certPEM = certPEM + '\n-----END CERTIFICATE-----\n'
                cert = cert.issuerCertificate
            }
            console.log(certPEM)
            fs.writeFileSync('./download_ca_cert.pem', certPEM)
            console.log(`statusCode: ${res.statusCode}`)
            console.log(`headers: ${JSON.stringify(res.headers)}`)
            res.on('data', d => {
                process.stdout.write(d)
            })
        })
    }).on('error', (err) => {
        console.error('error', err)
    }).end()
}
";

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
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Resolved DNS for {url.Host} failed with error: {ex}");
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
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Ping api.github.com failed with error: {ex}");
            }

            return result;
        }

        public static async Task<CheckResult> CheckHttpsRequests(this IHostContext hostContext, string url, string expectedHeader)
        {
            var result = new CheckResult();
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Send HTTPS Request to {url} ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                using (var _ = new HttpEventSourceListener(result.Logs))
                using (var httpClientHandler = hostContext.CreateHttpClientHandler())
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.AddRange(hostContext.UserAgents);
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
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Http request 'GET' to {url} succeed but doesn't have expected HTTP Header.");
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
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Https request 'GET' to {url} failed with error: {ex}");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ");
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
                var tempScript = _nodejsCertDownloadScript.Replace("<HOSTNAME>", uri.Host)
                                                          .Replace("<PORT>", uri.IsDefaultPort ? (uri.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : uri.Port.ToString())
                                                          .Replace("<PATH>", uri.AbsolutePath)
                                                          .Replace("<PAT>", pat);
                var proxy = hostContext.WebProxy.GetProxy(uri);
                if (proxy != null)
                {
                    tempScript = tempScript.Replace("<PROXYHOST>", proxy.Host)
                                           .Replace("<PROXYPORT>", proxy.IsDefaultPort ? (proxy.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : proxy.Port.ToString());
                    if (hostContext.WebProxy.Credentials is NetworkCredential proxyCred)
                    {
                        tempScript = tempScript.Replace("<PROXYUSERNAME>", proxyCred.UserName)
                                               .Replace("<PROXYPASSWORD>", proxyCred.Password);
                    }
                    else
                    {
                        tempScript = tempScript.Replace("<PROXYUSERNAME>", "")
                                               .Replace("<PROXYPASSWORD>", "");
                    }
                }
                else
                {
                    tempScript = tempScript.Replace("<PROXYHOST>", "")
                                           .Replace("<PROXYPORT>", "")
                                           .Replace("<PROXYUSERNAME>", "")
                                           .Replace("<PROXYPASSWORD>", "");
                }

                var tempJsFile = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Diag), StringUtil.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.js", nameof(DownloadExtraCA), DateTime.UtcNow));
                await File.WriteAllTextAsync(tempJsFile, tempScript);
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Generate temp node.js script: '{tempJsFile}'");

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

                    var node12 = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Externals), "node12", "bin", $"node{IOUtil.ExeExtension}");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Run '{node12} \"{tempJsFile}\"' ");
                    await processInvoker.ExecuteAsync(hostContext.GetDirectory(WellKnownDirectory.Root), node12, $"\"{tempJsFile}\"", null, true, CancellationToken.None);
                }

                result.Pass = true;
            }
            catch (Exception ex)
            {
                result.Pass = false;
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Download SSL Certificate from '{url}' failed with error: {ex}");
            }

            return result;
        }
    }

    public sealed class HttpEventSourceListener : EventListener
    {
        private readonly List<string> _logs;
        private readonly object _lock = new object();
        private readonly Dictionary<string, HashSet<string>> _ignoredEvent = new Dictionary<string, HashSet<string>>
        {
            {
                "Private.InternalDiagnostics.System.Net.Http",
                new HashSet<string>
                {
                    "Info",
                    "Associate"
                }
            },
            {
                "Private.InternalDiagnostics.System.Net.Security",
                new HashSet<string>
                {
                    "Info",
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

            if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http" ||
                eventSource.Name == "Private.InternalDiagnostics.System.Net.Security")
            {
                EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
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