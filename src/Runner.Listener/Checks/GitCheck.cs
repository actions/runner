using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Check
{
    public sealed class GitCheck : RunnerService, ICheckExtension
    {
        private string _logFile = null;
        private string _gitPath = null;

        public int Order => 3;

        public string CheckName => "Git Certificate/Proxy Validation";

        public string CheckDescription => "Check if the Git CLI can access GitHub.com or GitHub Enterprise Server.";

        public string CheckLog => _logFile;

        public string HelpLink => "https://github.com/actions/runner/blob/main/docs/checks/git.md";

        public Type ExtensionType => typeof(ICheckExtension);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _logFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), StringUtil.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", nameof(GitCheck), DateTime.UtcNow));
            _gitPath = WhichUtil.Which("git");
        }

        // git access to ghes/gh 
        public async Task<bool> RunCheck(string url, string pat)
        {
            await File.AppendAllLinesAsync(_logFile, HostContext.WarnLog());
            await File.AppendAllLinesAsync(_logFile, HostContext.CheckProxy());

            if (string.IsNullOrEmpty(_gitPath))
            {
                await File.AppendAllLinesAsync(_logFile, new[] { $"{DateTime.UtcNow.ToString("O")} Can't verify git with GitHub.com or GitHub Enterprise Server since git is not installed." });
                return false;
            }

            var checkGit = await CheckGit(url, pat);
            var result = checkGit.Pass;
            await File.AppendAllLinesAsync(_logFile, checkGit.Logs);

            // try fix SSL error by providing extra CA certificate.
            if (checkGit.SslError)
            {
                await File.AppendAllLinesAsync(_logFile, new[] { $"{DateTime.UtcNow.ToString("O")} Try fix SSL error by providing extra CA certificate." });
                var downloadCert = await HostContext.DownloadExtraCA(url, pat);
                await File.AppendAllLinesAsync(_logFile, downloadCert.Logs);

                if (downloadCert.Pass)
                {
                    var recheckGit = await CheckGit(url, pat, extraCA: true);
                    await File.AppendAllLinesAsync(_logFile, recheckGit.Logs);
                    if (recheckGit.Pass)
                    {
                        await File.AppendAllLinesAsync(_logFile, new[] { $"{DateTime.UtcNow.ToString("O")} Fixed SSL error by providing extra CA certs." });
                    }
                }
            }

            return result;
        }

        private async Task<CheckResult> CheckGit(string url, string pat, bool extraCA = false)
        {
            var result = new CheckResult();
            try
            {
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     Validate server cert and proxy configuration with Git ");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");
                var repoUrlBuilder = new UriBuilder(url);
                repoUrlBuilder.Path = "actions/checkout";
                repoUrlBuilder.UserName = "gh";
                repoUrlBuilder.Password = pat;

                var gitProxy = "";
                var proxy = HostContext.WebProxy.GetProxy(repoUrlBuilder.Uri);
                if (proxy != null)
                {
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Runner is behind http proxy '{proxy.AbsoluteUri}'");
                    if (HostContext.WebProxy.HttpProxyUsername != null ||
                        HostContext.WebProxy.HttpsProxyUsername != null)
                    {
                        var proxyUrlWithCred = UrlUtil.GetCredentialEmbeddedUrl(
                            proxy,
                            HostContext.WebProxy.HttpProxyUsername ?? HostContext.WebProxy.HttpsProxyUsername,
                            HostContext.WebProxy.HttpProxyPassword ?? HostContext.WebProxy.HttpsProxyPassword);
                        gitProxy = $"-c http.proxy={proxyUrlWithCred}";
                    }
                    else
                    {
                        string proxyUrlWithPort = UrlUtil.GetAbsoluteUrlWithPort(proxy);
                        gitProxy = $"-c http.proxy={proxyUrlWithPort}";
                    }
                }

                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                {
                    processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} {args.Data}");
                        }
                    });

                    processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            result.Logs.Add($"{DateTime.UtcNow.ToString("O")} {args.Data}");
                        }
                    });

                    var gitArgs = $"{gitProxy} ls-remote --exit-code {repoUrlBuilder.Uri.AbsoluteUri} HEAD";
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} Run 'git {gitArgs}' ");

                    var env = new Dictionary<string, string>
                    {
                        { "GIT_TRACE", "1" },
                        { "GIT_CURL_VERBOSE", "1" }
                    };

                    if (extraCA)
                    {
                        env["GIT_SSL_CAINFO"] = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), "download_ca_cert.pem");
                    }

                    await processInvoker.ExecuteAsync(
                        HostContext.GetDirectory(WellKnownDirectory.Root),
                        _gitPath,
                        gitArgs,
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
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     git ls-remote failed with error: {ex}");
                if (result.Logs.Any(x => x.Contains("SSL Certificate problem", StringComparison.OrdinalIgnoreCase)))
                {
                    result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****     git ls-remote failed due to SSL cert issue.");
                    result.SslError = true;
                }
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ****                                                                                                       ****");
                result.Logs.Add($"{DateTime.UtcNow.ToString("O")} ***************************************************************************************************************");

            }

            return result;
        }
    }
}
