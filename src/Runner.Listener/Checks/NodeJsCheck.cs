

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Check
{
    public sealed class NodeJsCheck : RunnerService, ICheckExtension
    {
        private const string _nodejsTestScript = @"
const https = require('https')
const http = require('http')
const hostname = '<HOSTNAME>'
const port = '<PORT>'
const path = '<PATH>'
const pat = '<PAT>'
const proxyHost = '<PROXYHOST>'
const proxyPort = '<PROXYPORT>'
const proxyUsername = '<PROXYUSERNAME>'
const proxyPassword = '<PROXYPASSWORD>'

if (proxyHost === '') {
    const options = {
        hostname: hostname,
        port: port,
        path: path,
        method: 'GET',
        headers: {
            'User-Agent': 'GitHubActionsRunnerCheck/1.0',
            'Authorization': `token ${pat}`,
        }
    }
    const req = https.request(options, res => {
        console.log(`statusCode: ${res.statusCode}`)
        console.log(`headers: ${JSON.stringify(res.headers)}`)

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
    const proxyAuth = 'Basic ' + Buffer.from(proxyUsername + ':' + proxyPassword).toString('base64')
    const options = {
        hostname: proxyHost,
        port: proxyPort,
        method: 'CONNECT',
        path: `${hostname}:${port}`
    }

    if (proxyUsername != '' || proxyPassword != '') {
        options.headers = {
            'Proxy-Authorization': proxyAuth,
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
            path: path,
            headers: {
                'User-Agent': 'GitHubActionsRunnerCheck/1.0',
                'Authorization': `token ${pat}`,
            }
        }, (res) => {
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

        private string _logFile = null;

        public int Order => 50;

        public string CheckName => "Node.js Certificate/Proxy Validation";

        public string CheckDescription => "Make sure the node.js have access to the GitHub Enterprise Server.";

        public string CheckLog => _logFile;

        public string HelpLink => "https://github.com/actions/runner/docs/checks/nodejs.md";

        public Type ExtensionType => typeof(ICheckExtension);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _logFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), StringUtil.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", nameof(NodeJsCheck), DateTime.UtcNow));
        }

        // node access to ghes/gh
        public async Task<bool> RunCheck(string url, string pat)
        {
            await File.AppendAllLinesAsync(_logFile, HostContext.CheckProxy());

            // Request to github.com or ghes server
            var urlBuilder = new UriBuilder(url);
            if (UrlUtil.IsHostedServer(urlBuilder))
            {
                urlBuilder.Host = $"api.{urlBuilder.Host}";
                urlBuilder.Path = "";
            }
            else
            {
                urlBuilder.Path = "api/v3";
            }

            var checkNode = await CheckNodeJs(urlBuilder.Uri.AbsoluteUri, pat);
            var result = checkNode.Pass;
            await File.AppendAllLinesAsync(_logFile, checkNode.Logs);

            // try fix SSL error by providing extra CA certificate.
            if (checkNode.SslError)
            {
                var downloadCert = await HostContext.DownloadExtraCA(urlBuilder.Uri.AbsoluteUri, pat);
                await File.AppendAllLinesAsync(_logFile, downloadCert.Logs);

                if (downloadCert.Pass)
                {
                    var recheckNode = await CheckNodeJs(urlBuilder.Uri.AbsoluteUri, pat, true);
                    await File.AppendAllLinesAsync(_logFile, recheckNode.Logs);
                    if (recheckNode.Pass)
                    {
                        await File.AppendAllLinesAsync(_logFile, new[] { $"{DateTime.UtcNow.ToString("O")} Fixed SSL error by providing extra CA certs." });
                    }
                }
            }

            return result;
        }

        private async Task<CheckResult> CheckNodeJs(string url, string pat, bool extraCA = false)
        {
            var result = new CheckResult();
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Make Http request to {url} using node.js ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");

                // Request to github.com or ghes server
                Uri requestUrl = null;
                var urlBuilder = new UriBuilder(url);
                if (UrlUtil.IsHostedServer(urlBuilder))
                {
                    urlBuilder.Host = $"api.{urlBuilder.Host}";
                }
                else
                {
                    urlBuilder.Path = "api/v3";
                }
                requestUrl = urlBuilder.Uri;

                var tempScript = _nodejsTestScript.Replace("<HOSTNAME>", requestUrl.Host)
                                                  .Replace("<PORT>", requestUrl.IsDefaultPort ? (requestUrl.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : requestUrl.Port.ToString())
                                                  .Replace("<PATH>", requestUrl.AbsolutePath)
                                                  .Replace("<PAT>", pat);

                var proxy = HostContext.WebProxy.GetProxy(requestUrl);
                if (proxy != null)
                {
                    tempScript = tempScript.Replace("<PROXYHOST>", proxy.Host)
                                           .Replace("<PROXYPORT>", proxy.IsDefaultPort ? (proxy.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : proxy.Port.ToString());
                    if (HostContext.WebProxy.Credentials is NetworkCredential proxyCred)
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

                var tempJsFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), StringUtil.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.js", nameof(NodeJsCheck), DateTime.UtcNow));
                await File.WriteAllTextAsync(tempJsFile, tempScript);
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Generate temp node.js script: '{tempJsFile}'");

                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
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

                    var node12 = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "node12", "bin", $"node{IOUtil.ExeExtension}");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Run '{node12} \"{tempJsFile}\"' ");

                    var env = new Dictionary<string, string>();
                    if (extraCA)
                    {
                        env["NODE_EXTRA_CA_CERTS"] = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), "download_ca_cert.pem");
                    }
                    await processInvoker.ExecuteAsync(
                        HostContext.GetDirectory(WellKnownDirectory.Root),
                        node12,
                        $"\"{tempJsFile}\"",
                        env,
                        true,
                        CancellationToken.None);
                }

                result.Pass = true;
            }
            catch (Exception ex)
            {
                result.Pass = false;
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Make https request to {url} using node.js failed with error: {ex}");
                if (result.Logs.Any(x => x.Contains("UNABLE_TO_VERIFY_LEAF_SIGNATURE") ||
                                         x.Contains("UNABLE_TO_GET_ISSUER_CERT_LOCALLY") ||
                                         x.Contains("SELF_SIGNED_CERT_IN_CHAIN")))
                {
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Https request failed due to SSL cert issue.");
                    result.SslError = true;
                }
            }

            return result;
        }
    }
}