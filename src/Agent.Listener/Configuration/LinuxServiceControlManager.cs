using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio.Services.Agent.Util;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class LinuxServiceControlManager : ServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep vsts`
        private const string ServiceNamePattern = "vsts.agent.{0}.{1}.service";
        private const string ServiceDisplayNamePattern = "VSTS Agent ({0}.{1})";

        private const int MaxUserNameLength = 32;
        private const string VstsAgentServiceTemplate = "vsts.agent.service.template";

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

            if (CheckServiceExists(settings.ServiceName))
            {
                Trace.Info("Service already exists");
                _term.WriteLine(StringUtil.Loc("ServiceAleadyExists"));
                StopService(settings.ServiceName);
            }

            var unitFile = _linuxServiceHelper.GetUnitFile(settings.ServiceName);

            try
            {
                var unitContent = File.ReadAllText(Path.Combine(IOUtil.GetBinPath(), VstsAgentServiceTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{Description}", settings.ServiceDisplayName },
                                              { "{BinDirectory}", IOUtil.GetBinPath() },
                                              { "{User}", GetCurrentLoginName() }
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
            InstallService(settings.ServiceName);

            _term.WriteLine(StringUtil.Loc("ServiceConfigured", settings.ServiceName));
            return true;
        }

        public override void StartService(string serviceName)
        {
            Trace.Entering();

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
                ExecuteSystemdCommand("stop " + serviceName);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                _term.WriteError(StringUtil.Loc("CanNotStopService", serviceName));

                // We dont want to throw here. We can still replace the systemd unit file and call daemon-reload
            }
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

        private void ChangeOwnershipToLoginUser(Dictionary<string, int> filesToChange)
        {
            // Before starting the service chown/chmod the _diag and settings files to the current user.
            // Since we started with sudo, the _diag will be owned by root. Change this to current login user

            Trace.Entering();

            try
            {
                string uidValue = Environment.GetEnvironmentVariable("SUDO_UID");
                string gidValue = Environment.GetEnvironmentVariable("SUDO_GID");
                uint uid = 0, gid = 0;

                if (string.IsNullOrEmpty(uidValue) || string.IsNullOrEmpty(gidValue)
                    || !uint.TryParse(uidValue, out uid) || !uint.TryParse(gidValue, out gid))
                {
                    Trace.Info("SUDO_UID and SUDO_GID environment variables are not found, calling getpwnam to find the uid,gid of the user");
                    string userName = GetCurrentLoginName();

                    IntPtr statPtr = getpwnam(userName);

                    var stat = Marshal.PtrToStructure<LoginStat>(statPtr);
                    uid = stat.pw_uid;
                    gid = stat.pw_gid;
                }

                Trace.Info(StringUtil.Format("Found uid {0} gid {1} of the logged in user", uid, gid));

                foreach (var file in filesToChange)
                {
                    Trace.Info("Changing ownership of {0} to logged in user", file.Key);
                    chown(file.Key, uid, gid);

                    Trace.Info("Changing permission of {0} to {1}", file.Key, file.Value);
                    chmod(file.Key, file.Value);
                }
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                // TODO: Localize this error message.
                _term.WriteError("CannotChangeOwnership");

                throw;
            }
        }

        private void InstallService(string serviceName)
        {
            Trace.Entering();
            try
            {
                ExecuteSystemdCommand("enable " + serviceName);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                _term.WriteError(StringUtil.Loc("CanNotInstallService"));

                throw;
            }
        }

        private void ReloadSystemd()
        {
            Trace.Entering();
            try
            {
                // TODO: systemd prints any pending info message to the TTY, hide this if possible
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
            Trace.Entering();
            try
            {
                var processInvoker = HostContext.CreateService<IProcessInvoker>();

                // TODO: can systemd installed in non default directory?
                using (var cs = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
                {
                    processInvoker.ExecuteAsync("/usr/bin", "systemctl", command, null, cs.Token).Wait();
                }
            }
            catch (Exception ex)
            {
                Trace.Warning(ex.ToString());
                throw;
            }
        }

        private string GetCurrentLoginName()
        {
            Trace.Entering();

            string userName = Environment.GetEnvironmentVariable("SUDO_USER");
            Trace.Info(StringUtil.Format("Found login username as {0}", userName));

            if (string.IsNullOrEmpty(userName))
            {
                // This should never be null, however the env variables can be controller or modified
                // in such cases try getting the username using getlogin
                Trace.Info("Trying to get login username using getlogin_r");
                StringBuilder loginUser = new StringBuilder();
                int result = getlogin_r(loginUser, MaxUserNameLength);

                Trace.Verbose(StringUtil.Format("Result of getlogin_r {0}", result));

                if (result == 0)
                {
                    userName = loginUser.ToString();
                    Trace.Info(StringUtil.Format("Found login username as {0}", userName));
                }
                else
                {
                    // TODO: Should we set it to root instead of failing?
                    throw new InvalidOperationException(StringUtil.Loc("CanNotFindLoginUserName"));
                }
            }

            return userName;
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