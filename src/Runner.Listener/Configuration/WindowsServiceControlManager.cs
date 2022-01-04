#if OS_WINDOWS
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Configuration
{
    public class WindowsServiceControlManager : ServiceControlManager, IWindowsServiceControlManager
    {
        public const string WindowsServiceControllerName = "RunnerService.exe";

        private const string ServiceNamePattern = "actions.runner.{0}.{1}";
        private const string ServiceDisplayNamePattern = "GitHub Actions Runner ({0}.{1})";

        private INativeWindowsServiceHelper _windowsServiceHelper;
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _windowsServiceHelper = HostContext.GetService<INativeWindowsServiceHelper>();
            _term = HostContext.GetService<ITerminal>();
        }

        public void ConfigureService(RunnerSettings settings, CommandSettings command)
        {
            Trace.Entering();

            if (!_windowsServiceHelper.IsRunningInElevatedMode())
            {
                Trace.Error("Needs Administrator privileges for configure runner as windows service.");
                throw new SecurityException("Needs Administrator privileges for configuring runner as windows service.");
            }

            // We use NetworkService as default account for actions runner
            NTAccount defaultServiceAccount = _windowsServiceHelper.GetDefaultServiceAccount();
            string logonAccount = command.GetWindowsLogonAccount(defaultValue: defaultServiceAccount.ToString(), descriptionMsg: "User account to use for the service");

            string domainName;
            string userName;
            GetAccountSegments(logonAccount, out domainName, out userName);

            if ((string.IsNullOrEmpty(domainName) || domainName.Equals(".", StringComparison.CurrentCultureIgnoreCase)) && !logonAccount.Contains('@'))
            {
                logonAccount = String.Format("{0}\\{1}", Environment.MachineName, userName);
                domainName = Environment.MachineName;
            }

            Trace.Info("LogonAccount after transforming: {0}, user: {1}, domain: {2}", logonAccount, userName, domainName);

            string logonPassword = string.Empty;
            if (!defaultServiceAccount.Equals(new NTAccount(logonAccount)) && !NativeWindowsServiceHelper.IsWellKnownIdentity(logonAccount))
            {
                while (true)
                {
                    logonPassword = command.GetWindowsLogonPassword(logonAccount);
                    if (_windowsServiceHelper.IsValidCredential(domainName, userName, logonPassword))
                    {
                        Trace.Info("Credential validation succeed");
                        break;
                    }
                    else
                    {
                        if (!command.Unattended)
                        {
                            Trace.Info("Invalid credential entered");
                            _term.WriteLine("Invalid windows credentials entered. Try again or ctrl-c to quit");
                        }
                        else
                        {
                            throw new SecurityException("Invalid windows credentials entered. Try again or ctrl-c to quit");
                        }
                    }
                }
            }

            string serviceName;
            string serviceDisplayName;
            CalculateServiceName(settings, ServiceNamePattern, ServiceDisplayNamePattern, out serviceName, out serviceDisplayName);
            if (_windowsServiceHelper.IsServiceExists(serviceName))
            {
                _term.WriteLine($"The service already exists: {serviceName}, it will be replaced");
                _windowsServiceHelper.UninstallService(serviceName);
            }

            Trace.Info("Verifying if the account has LogonAsService permission");
            if (_windowsServiceHelper.IsUserHasLogonAsServicePrivilege(domainName, userName))
            {
                Trace.Info($"Account: {logonAccount} already has Logon As Service Privilege.");
            }
            else
            {
                if (!_windowsServiceHelper.GrantUserLogonAsServicePrivilege(domainName, userName))
                {
                    throw new InvalidOperationException($"Cannot grant LogonAsService permission to the user {logonAccount}");
                }
            }

            // grant permission for runner root folder and work folder
            Trace.Info("Create local group and grant folder permission to service logon account.");
            string runnerRoot = HostContext.GetDirectory(WellKnownDirectory.Root);
            string workFolder = HostContext.GetDirectory(WellKnownDirectory.Work);
            Directory.CreateDirectory(workFolder);
            _windowsServiceHelper.GrantDirectoryPermissionForAccount(logonAccount, new[] { runnerRoot, workFolder });
            _term.WriteLine($"Granting file permissions to '{logonAccount}'.");

            // install service.
            _windowsServiceHelper.InstallService(serviceName, serviceDisplayName, logonAccount, logonPassword);

            // create .service file with service name.
            SaveServiceSettings(serviceName);

            Trace.Info("Configuration was successful, trying to start the service");
            _windowsServiceHelper.StartService(serviceName);
        }

        public void UnconfigureService()
        {
            if (!_windowsServiceHelper.IsRunningInElevatedMode())
            {
                Trace.Error("Needs Administrator privileges for unconfigure windows service runner.");
                throw new SecurityException("Needs Administrator privileges for unconfiguring runner that running as windows service.");
            }

            string serviceConfigPath = HostContext.GetConfigFile(WellKnownConfigFile.Service);
            string serviceName = File.ReadAllText(serviceConfigPath);
            if (_windowsServiceHelper.IsServiceExists(serviceName))
            {
                _windowsServiceHelper.StopService(serviceName);
                _windowsServiceHelper.UninstallService(serviceName);

                // Delete local group we created during configure.
                string runnerRoot = HostContext.GetDirectory(WellKnownDirectory.Root);
                string workFolder = HostContext.GetDirectory(WellKnownDirectory.Work);
                _windowsServiceHelper.RevokeDirectoryPermissionForAccount(new[] { runnerRoot, workFolder });
            }

            IOUtil.DeleteFile(serviceConfigPath);
        }

        private void SaveServiceSettings(string serviceName)
        {
            string serviceConfigPath = HostContext.GetConfigFile(WellKnownConfigFile.Service);
            if (File.Exists(serviceConfigPath))
            {
                IOUtil.DeleteFile(serviceConfigPath);
            }

            File.WriteAllText(serviceConfigPath, serviceName, new UTF8Encoding(false));
            File.SetAttributes(serviceConfigPath, File.GetAttributes(serviceConfigPath) | FileAttributes.Hidden);
        }

        private void GetAccountSegments(string account, out string domain, out string user)
        {
            string[] segments = account.Split('\\');
            domain = string.Empty;
            user = account;
            if (segments.Length == 2)
            {
                domain = segments[0];
                user = segments[1];
            }
        }
    }
}
#endif
