using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class LinuxServiceControlManager : ServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep vsts`
        private const string ServiceNamePattern = "vsts.agent.{0}.{1}.service";
        private const string ServiceDisplayNamePattern = "VSTS Agent ({0}.{1})";

        private const string SystemdPathPrefix = "/etc/systemd/system";
        private const string InitFileCommandLocation = "/proc/1/comm";

        private const int MaxUserNameLength = 32;
        private const string VstsAgentServiceTemplate = "vsts.agent.service.template";

        public override void ConfigureService(
            AgentSettings settings,
            Dictionary<string, string> args,
            bool enforceSupplied)
        {
            Trace.Info(nameof(ConfigureService));

            CalculateServiceName(settings, ServiceNamePattern, ServiceDisplayNamePattern);

            if (!CheckIfSystemdExists())
            {
                Trace.Info("Systemd does not exists, returning");
                _term.WriteLine(StringUtil.Loc("SystemdDoesNotExists"));

                return;
            }

            if (CheckServiceExists(settings.ServiceName))
            {
                Trace.Info("Service already exists");
                _term.WriteLine(StringUtil.Loc("ServiceAleadyExists"));
                StopService(settings.ServiceName);
            }

            var unitFile = GetUnitFile(settings.ServiceName);

            try
            {
                StringBuilder loginUser = new StringBuilder();
                getlogin_r(loginUser, MaxUserNameLength);
                string currentUserName = loginUser.ToString();

                var unitContent = File.ReadAllText(Path.Combine(IOUtil.GetBinPath(), VstsAgentServiceTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{Description}", settings.ServiceDisplayName },
                                              { "{BinDirectory}", IOUtil.GetBinPath() },
                                              { "{User}", currentUserName }
                                          };

                unitContent = tokensToReplace.Aggregate(
                    unitContent,
                    (current, item) => current.Replace(item.Key, item.Value));
                File.WriteAllText(unitFile, unitContent);

                // unit file should not be executable and world writable
                chmod(unitFile, Convert.ToInt32("664", 8));
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

            ReloadSystemd();
            EnableService(settings.ServiceName);

            _term.WriteLine(StringUtil.Loc("LinuxServiceConfigured", settings.ServiceName));
        }

        public override void StartService(string serviceName)
        {
            Trace.Info(nameof(StartService));

            Dictionary<string, int> filesToChange = new Dictionary<string, int>
                                                         {
                                                             { IOUtil.GetDiagPath(), Convert.ToInt32("775", 8) },
                                                             { IOUtil.GetConfigFilePath(), Convert.ToInt32("770", 8) },
                                                             { IOUtil.GetCredFilePath(), Convert.ToInt32("770", 8) },
                                                         };

            try
            {
                ChangeOwnershipToLoginUser(filesToChange);
                ReloadSystemd();
                ExecuteSystemdCommand("start " + serviceName);
            }
            catch (Exception)
            {
                _term.WriteError(StringUtil.Loc("LinuxServiceStartFailed"));
                throw;
            }
        }

        protected virtual string GetUnitFile(string serviceName)
        {
            return Path.Combine(SystemdPathPrefix, serviceName);
        }

        protected virtual bool CheckIfSystemdExists()
        {
            Trace.Info(nameof(CheckIfSystemdExists));
            try
            {
                var commName = File.ReadAllText(InitFileCommandLocation).Trim();
                return commName.Equals("systemd", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Trace.Error(ex.ToString());
                _term.WriteError(StringUtil.Loc("CanNotFindSystemd"));

                return false;
            }
        }

        protected virtual bool CheckServiceExists(string serviceName)
        {
            Trace.Info(nameof(CheckServiceExists));
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException("serviceName");
            }

            try
            {
                var unitFile = new FileInfo(Path.Combine(SystemdPathPrefix, serviceName));
                return unitFile.Exists;
            }
            catch (Exception ex)
            {
                Trace.Error(ex);

                // If we can check if the service exists we can't configure either. We can't ignore this error.
                throw;
            }
        }

        private void ChangeOwnershipToLoginUser(Dictionary<string, int> filesToChange)
        {
            // Before starting the service chown/chmod the _diag and settings files to the current user.
            // Since we started with sudo, the _diag will be owned by root. Change this to current login user

            Trace.Info(nameof(ChangeOwnershipToLoginUser));

            try
            {
                StringBuilder loginUser = new StringBuilder();
                getlogin_r(loginUser, MaxUserNameLength);
                string userName = loginUser.ToString();

                IntPtr statPtr = getpwnam(userName);
                var stat = Marshal.PtrToStructure<LoginStat>(statPtr);
                foreach (var file in filesToChange)
                {
                    Trace.Info("Changing ownership of {0} to current user {1}", file.Key, userName);
                    chown(file.Key, stat.pw_uid, stat.pw_gid);

                    Trace.Info("Changing permission of {0} to {1}", file.Key, file.Value);
                    chmod(file.Key, file.Value);
                }
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                _term.WriteError(StringUtil.Loc("CannotChangeOwnership"));

                throw;
            }
        }

        private void EnableService(string serviceName)
        {
            Trace.Info(nameof(EnableService));
            try
            {
                ExecuteSystemdCommand("enable " + serviceName);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                _term.WriteError(StringUtil.Loc("LinuxServiceStartFailed"));

                throw;
            }
        }

        private void StopService(string serviceName)
        {
            Trace.Info(nameof(StopService));
            try
            {
                ExecuteSystemdCommand("stop " + serviceName);
            }
            catch (Exception)
            {
                _term.WriteError(StringUtil.Loc("LinuxServiceStartFailed"));

                // We dont want to throw here. We can still replace the systemd unit file and call daemon-reload
            }
        }

        private void ReloadSystemd()
        {
            Trace.Info(nameof(ReloadSystemd));
            try
            {
                ExecuteSystemdCommand("daemon-reload");
            }
            catch (Exception)
            {
                _term.WriteError(StringUtil.Loc("SystemdCannotReload"));
                throw;
            }
        }

        private void ExecuteSystemdCommand(string command)
        {
            Trace.Info(nameof(ExecuteSystemdCommand));
            try
            {
                var processInvoker = HostContext.CreateService<IProcessInvoker>();

                // TODO: can systemd installed in non default directory?
                processInvoker.Execute("/usr/bin", "systemctl", command, null);
                processInvoker.WaitForExit(HostContext.CancellationToken);
            }
            catch (Exception ex)
            {
                Trace.Warning(ex.ToString());
                throw;
            }
        }

        // Only the configuration requires sudo permission as systemd commands have to be called. But the agent itself will run with current user premission
        [DllImport("libc.so.6")]
        private static extern int getlogin_r(StringBuilder buf, int bufsize);

        [DllImport("libc.so.6")]
        private static extern IntPtr getpwnam(string name);

        [DllImport("libc.so.6")]
        private static extern int chown(string pathname, uint owner, uint group);

        [DllImport("libc.so.6")]
        private static extern int chmod(string pathname, int mode);

        [StructLayout(LayoutKind.Sequential)]
        public struct LoginStat
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pw_name;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pw_passwd;

            public uint pw_uid;
            public uint pw_gid;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pw_gecos;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pw_dir;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pw_shell;
        }
    }
}