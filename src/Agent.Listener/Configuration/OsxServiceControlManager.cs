using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class OsxServiceControlManager : ServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep vsts`
        private const string _svcNamePattern = "vsts.agent.{0}.{1}";
        private const string _svcDisplayPattern = "VSTS Agent ({0}.{1})";
        private const string _plistTemplate = "vsts.agent.plist.template";
        private const string _shTemplate = "darwin.svc.sh.template";
        private const string _shName = "svc.sh";

        public override bool ConfigureService(AgentSettings settings, CommandSettings command)
        {
            Trace.Entering();

            CalculateServiceName(settings, _svcNamePattern, _svcDisplayPattern);

            string plistPath = GetPlistPath(settings.ServiceName);
            if (this.CheckServiceExists(settings.ServiceName))
            {
                _term.WriteError(StringUtil.Loc("ServiceAlreadyExists", plistPath));
                throw new InvalidOperationException(StringUtil.Loc("CanNotInstallService"));
            }

            try
            {
                string svcShPath = Path.Combine(IOUtil.GetRootPath(), _shName);

                // TODO: encoding?
                // TODO: Loc strings formatted into MSG_xxx vars in shellscript
                string svcShContent = File.ReadAllText(Path.Combine(IOUtil.GetBinPath(), _shTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{{SvcNameVar}}", settings.ServiceName }
                                          };

                svcShContent = tokensToReplace.Aggregate(
                    svcShContent,
                    (current, item) => current.Replace(item.Key, item.Value));

                //TODO: encoding?
                File.WriteAllText(svcShPath, svcShContent);

                Chmod("755", svcShPath);

                SvcSh("install");
                _term.WriteLine(StringUtil.Loc("ServiceConfigured", settings.ServiceName));
            }
            catch (Exception e)
            {
                // if cfg as service fails, we fail cfg and cleanup 
                Trace.Error(e);
                _term.WriteError(StringUtil.Loc("CanNotStartService"));

                if (File.Exists(plistPath))
                {
                    Trace.Info($"Cleaning up plist file from failed config: {plistPath}");
                    IOUtil.DeleteFile(plistPath);
                }                
                throw;
            }
            
            return true;            
        }

        public override void StartService(string serviceName)
        {
            SvcSh("start");
        }

        public override void StopService(string serviceName)
        {
            SvcSh("stop");
        }

        public override bool CheckServiceExists(string serviceName)
        {
            return File.Exists(GetPlistPath(serviceName));
        }

        private string GetPlistPath(string svcName)
        {
            string homeDir = Environment.GetEnvironmentVariable("HOME");
            ArgUtil.NotNullOrEmpty(homeDir, "HOME");

            string plistPath = Path.Combine(homeDir, "Library/LaunchAgents", svcName + ".plist");
            Trace.Info($"plistPath: {plistPath}");
            return plistPath;
        }

        private void SvcSh(string command)
        {
            Trace.Entering();

            string argLine = StringUtil.Format("{0} {1}", _shName, command);
            Exec(IOUtil.GetRootPath(), "bash", argLine);
        }

        // TODO: move to a common nix util after I close with Kalyan on not using pinvoke
        private void Chmod(string mode, string file)
        {
            Trace.Entering();

            string argLine = StringUtil.Format("{0} {1}", mode, file);
            Exec(IOUtil.GetRootPath(), "chmod", argLine);
        }

        private void Exec(string workingDirectory, string toolName, string argLine)
        {
            Trace.Entering();

            var whichUtil = HostContext.GetService<IWhichUtil>();
            string toolPath = whichUtil.Which(toolName);
            Trace.Info($"Running {toolPath} {argLine}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += OnOutputDataReceived;
            processInvoker.ErrorDataReceived += OnErrorDataReceived;

            using (var cs = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
            {
                // TODO: the service classes here are not async
                processInvoker.ExecuteAsync(workingDirectory, toolPath, argLine, null, true, cs.Token).GetAwaiter().GetResult();
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _term.WriteLine(e.Data);
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _term.WriteLine(e.Data);
            }
        }
    }
}