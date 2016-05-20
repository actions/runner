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

            string plistPath = GetPlistPath(ServiceName);
            if (this.CheckServiceExists(ServiceName))
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
                                              { "{{SvcDescription}}", ServiceDisplayName },
                                              { "{{SvcNameVar}}", ServiceName }
                                          };

                svcShContent = tokensToReplace.Aggregate(
                    svcShContent,
                    (current, item) => current.Replace(item.Key, item.Value));

                //TODO: encoding?
                File.WriteAllText(svcShPath, svcShContent);

                var unixUtil = HostContext.CreateService<IUnixUtil>();
                unixUtil.Chmod("755", svcShPath).GetAwaiter().GetResult();

                SvcSh("install");
                _term.WriteLine(StringUtil.Loc("ServiceConfigured", ServiceName));
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

        public override void UnconfigureService()
        {
            SvcSh("uninstall");
        }

        public override void StartService()
        {
            SvcSh("start");
        }

        public override void StopService()
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
            var unixUtil = HostContext.CreateService<IUnixUtil>();
            unixUtil.Exec(IOUtil.GetRootPath(), "bash", argLine).GetAwaiter().GetResult();
        }
    }
}