using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.Services.Agent.Util;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class SystemDControlManager : ServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep vsts`
        private const string ServiceNamePattern = "vsts.agent.{0}.{1}.service";
        private const string ServiceDisplayNamePattern = "VSTS Agent ({0}.{1})";

        private const int MaxUserNameLength = 32;
        private const string VstsAgentServiceTemplate = "vsts.agent.service.template";
        private const string _shTemplate = "systemd.svc.sh.template";
        private const string _shName = "svc.sh";

        public override bool ConfigureService(
            AgentSettings settings,
            CommandSettings command)
        {
            Trace.Entering();

            var _linuxServiceHelper = HostContext.GetService<INativeLinuxServiceHelper>();
            CalculateServiceName(settings, ServiceNamePattern, ServiceDisplayNamePattern);

            if (!_linuxServiceHelper.CheckIfSystemdExists())
            {
                Trace.Info("Systemd does not exists, returning");
                _term.WriteLine(StringUtil.Loc("SystemdDoesNotExists"));

                return false;
            }

            var unitFile = _linuxServiceHelper.GetUnitFile(settings.ServiceName);

            if (CheckServiceExists(settings.ServiceName))
            {
                _term.WriteError(StringUtil.Loc("ServiceAlreadyExists", unitFile));
                throw new InvalidOperationException(StringUtil.Loc("CanNotInstallService"));
            }

            try
            {
                string svcShPath = Path.Combine(IOUtil.GetRootPath(), _shName);

                string svcShContent = File.ReadAllText(Path.Combine(IOUtil.GetBinPath(), _shTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{{SvcDescription}}", settings.ServiceDisplayName },
                                              { "{{SvcNameVar}}", settings.ServiceName }
                                          };

                svcShContent = tokensToReplace.Aggregate(
                    svcShContent,
                    (current, item) => current.Replace(item.Key, item.Value));

                File.WriteAllText(svcShPath, svcShContent, Encoding.UTF8);

                var unixUtil = HostContext.CreateService<IUnixUtil>();
                unixUtil.Chmod("755", svcShPath).GetAwaiter().GetResult();

                SvcSh("install");
                _term.WriteLine(StringUtil.Loc("ServiceConfigured", settings.ServiceName));
            }
            catch (UnauthorizedAccessException ex)
            {
                Trace.Error(ex);
                _term.WriteError(StringUtil.Loc("UnauthorizedAccess", unitFile));
                throw;
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                throw;
            }

            return true;
        }

        public override void StartService(string serviceName)
        {
            Trace.Entering();
            try
            {
                SvcSh("start");
                _term.WriteLine(StringUtil.Loc("ServiceStartedSuccessfully", serviceName));
            }
            catch (Exception)
            {
                _term.WriteError(StringUtil.Loc("CanNotStartService"));
                throw;
            }
        }

        public override void StopService(string serviceName)
        {
            Trace.Entering();
            try
            {
                SvcSh("stop");
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                _term.WriteError(StringUtil.Loc("CanNotStopService", serviceName));
                // We dont want to throw here. We can still replace the systemd unit file and call daemon-reload
            }
        }

        private void SvcSh(string command)
        {
            Trace.Entering();

            string argLine = StringUtil.Format("{0} {1}", _shName, command);
            var unixUtil = HostContext.CreateService<IUnixUtil>();
            unixUtil.Exec(IOUtil.GetRootPath(), "bash", argLine).GetAwaiter().GetResult();
        }

        public override bool CheckServiceExists(string serviceName)
        {
            Trace.Entering();
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException("serviceName");
            }

            try
            {
                var unitFile = new FileInfo(Path.Combine(NativeLinuxServiceHelper.SystemdPathPrefix, serviceName));
                return unitFile.Exists;
            }
            catch (Exception ex)
            {
                Trace.Error(ex);

                // If we can't check if the service exists we can't configure either. We can't ignore this error.
                throw;
            }
        }
    }
}