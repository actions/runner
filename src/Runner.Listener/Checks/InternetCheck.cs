

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Check
{
    public sealed class InternetCheck : RunnerService, ICheckExtension
    {
        private string _logFile = null;

        public int Order => 10;

        public string CheckName => "Internet Connection";

        public string CheckDescription => "Make sure the actions runner have access to public internet.";

        public string CheckLog => _logFile;

        public string HelpLink => "https://github.com/actions/runner/docs/checks/internet.md";

        public Type ExtensionType => typeof(ICheckExtension);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _logFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), StringUtil.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", nameof(InternetCheck), DateTime.UtcNow));
        }

        // check runner access to api.github.com
        public async Task<bool> RunCheck(string url, string pat)
        {
            var result = true;
            await File.AppendAllLinesAsync(_logFile, HostContext.CheckProxy());

            var checkTasks = new List<Task<CheckResult>>();
            checkTasks.Add(CheckUtil.CheckDns("https://api.github.com"));
            checkTasks.Add(CheckUtil.CheckPing("https://api.github.com"));
            checkTasks.Add(HostContext.CheckHttpsRequests("https://api.github.com", "X-GitHub-Request-Id"));

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