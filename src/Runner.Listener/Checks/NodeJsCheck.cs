using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Check
{
    public sealed class NodeJsCheck : RunnerService, ICheckExtension
    {
        private string _logFile = null;

        public int Order => 4;

        public string CheckName => "Node.js Certificate/Proxy Validation";

        public string CheckDescription => "Check if Node.js has access to GitHub.com or GitHub Enterprise Server.";

        public string CheckLog => _logFile;

        public string HelpLink => "https://github.com/actions/runner/blob/main/docs/checks/nodejs.md";

        public Type ExtensionType => typeof(ICheckExtension);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _logFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), StringUtil.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", nameof(NodeJsCheck), DateTime.UtcNow));
        }

        // node access to ghes/gh
        public async Task<bool> RunCheck(string url, string pat)
        {
            await File.AppendAllLinesAsync(_logFile, HostContext.WarnLog());
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
                    var recheckNode = await CheckNodeJs(urlBuilder.Uri.AbsoluteUri, pat, extraCA: true);
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
                Uri requestUrl = new(url);
                var env = new Dictionary<string, string>()
                {
                    { "HOSTNAME", requestUrl.Host },
                    { "PORT", requestUrl.IsDefaultPort ? (requestUrl.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : requestUrl.Port.ToString() },
                    { "PATH", requestUrl.AbsolutePath },
                    { "PAT", pat }
                };

                var proxy = HostContext.WebProxy.GetProxy(requestUrl);
                if (proxy != null)
                {
                    env["PROXYHOST"] = proxy.Host;
                    env["PROXYPORT"] = proxy.IsDefaultPort ? (proxy.Scheme.ToLowerInvariant() == "https" ? "443" : "80") : proxy.Port.ToString();
                    if (HostContext.WebProxy.HttpProxyUsername != null ||
                        HostContext.WebProxy.HttpsProxyUsername != null)
                    {
                        env["PROXYUSERNAME"] = HostContext.WebProxy.HttpProxyUsername ?? HostContext.WebProxy.HttpsProxyUsername;
                        env["PROXYPASSWORD"] = HostContext.WebProxy.HttpProxyPassword ?? HostContext.WebProxy.HttpsProxyPassword;
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

                if (extraCA)
                {
                    env["NODE_EXTRA_CA_CERTS"] = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), "download_ca_cert.pem");
                }

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

                    var makeWebRequestScript = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), "checkScripts", "makeWebRequest.js");
                    var node = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), NodeUtil.GetInternalNodeVersion(), "bin", $"node{IOUtil.ExeExtension}");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Run '{node} \"{makeWebRequestScript}\"' ");
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} {StringUtil.ConvertToJson(env)}");
                    await processInvoker.ExecuteAsync(
                        HostContext.GetDirectory(WellKnownDirectory.Root),
                        node,
                        $"\"{makeWebRequestScript}\"",
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
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Make https request to {url} using node.js failed with error: {ex}");
                if (result.Logs.Any(x => x.Contains("UNABLE_TO_VERIFY_LEAF_SIGNATURE") ||
                                         x.Contains("UNABLE_TO_GET_ISSUER_CERT_LOCALLY") ||
                                         x.Contains("SELF_SIGNED_CERT_IN_CHAIN")))
                {
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Https request failed due to SSL cert issue.");
                    result.SslError = true;
                }
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
            }

            return result;
        }
    }
}
