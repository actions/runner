using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Check
{
    public sealed class ActionsCheck : RunnerService, ICheckExtension
    {
        private string _logFile = null;

        public int Order => 2;

        public string CheckName => "GitHub Actions Connection";

        public string CheckDescription => "Check if the Actions runner has access to the GitHub Actions service.";

        public string CheckLog => _logFile;

        public string HelpLink => "https://github.com/actions/runner/blob/main/docs/checks/actions.md";

        public Type ExtensionType => typeof(ICheckExtension);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _logFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), StringUtil.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", nameof(ActionsCheck), DateTime.UtcNow));
        }

        // runner access to actions service
        public async Task<bool> RunCheck(string url, string pat)
        {
            await File.AppendAllLinesAsync(_logFile, HostContext.WarnLog());
            await File.AppendAllLinesAsync(_logFile, HostContext.CheckProxy());

            var checkTasks = new List<Task<CheckResult>>();
            string githubApiUrl = null;
            string actionsTokenServiceUrl = null;
            string actionsPipelinesServiceUrl = null;
            var urlBuilder = new UriBuilder(url);
            if (UrlUtil.IsHostedServer(urlBuilder))
            {
                urlBuilder.Host = $"api.{urlBuilder.Host}";
                urlBuilder.Path = "";
                githubApiUrl = urlBuilder.Uri.AbsoluteUri;
                actionsTokenServiceUrl = "https://vstoken.actions.githubusercontent.com/_apis/health";
                actionsPipelinesServiceUrl = "https://pipelines.actions.githubusercontent.com/_apis/health";
            }
            else
            {
                urlBuilder.Path = "api/v3";
                githubApiUrl = urlBuilder.Uri.AbsoluteUri;
                urlBuilder.Path = "_services/vstoken/_apis/health";
                actionsTokenServiceUrl = urlBuilder.Uri.AbsoluteUri;
                urlBuilder.Path = "_services/pipelines/_apis/health";
                actionsPipelinesServiceUrl = urlBuilder.Uri.AbsoluteUri;
            }

            // check github api
            checkTasks.Add(CheckUtil.CheckDns(githubApiUrl));
            checkTasks.Add(CheckUtil.CheckPing(githubApiUrl));
            checkTasks.Add(HostContext.CheckHttpsGetRequests(githubApiUrl, pat, expectedHeader: "X-GitHub-Request-Id"));

            // check actions token service
            checkTasks.Add(CheckUtil.CheckDns(actionsTokenServiceUrl));
            checkTasks.Add(CheckUtil.CheckPing(actionsTokenServiceUrl));
            checkTasks.Add(HostContext.CheckHttpsGetRequests(actionsTokenServiceUrl, pat, expectedHeader: "x-vss-e2eid"));

            // check actions pipelines service
            checkTasks.Add(CheckUtil.CheckDns(actionsPipelinesServiceUrl));
            checkTasks.Add(CheckUtil.CheckPing(actionsPipelinesServiceUrl));
            checkTasks.Add(HostContext.CheckHttpsGetRequests(actionsPipelinesServiceUrl, pat, expectedHeader: "x-vss-e2eid"));

            // check HTTP POST to actions pipelines service
            checkTasks.Add(HostContext.CheckHttpsPostRequests(actionsPipelinesServiceUrl, pat, expectedHeader: "x-vss-e2eid"));

            var result = true;
            while (checkTasks.Count > 0)
            {
                var finishedCheckTask = await Task.WhenAny<CheckResult>(checkTasks);
                var finishedCheck = await finishedCheckTask;
                result = result && finishedCheck.Pass;
                await File.AppendAllLinesAsync(_logFile, finishedCheck.Logs);
                checkTasks.Remove(finishedCheckTask);
            }

            await Task.WhenAll(checkTasks);
            return result;
        }
    }
}
