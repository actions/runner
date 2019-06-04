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
        public const string WindowsServiceControllerName = "AgentService.exe";

        private const string ServiceNamePattern = "vstsagent.{0}.{1}.{2}";
        private const string ServiceDisplayNamePattern = "Azure Pipelines Agent ({0}.{1}.{2})";

        private INativeWindowsServiceHelper _windowsServiceHelper;
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _windowsServiceHelper = HostContext.GetService<INativeWindowsServiceHelper>();
            _term = HostContext.GetService<ITerminal>();
        }

        public void ConfigureService(AgentSettings settings, CommandSettings command)
        {
            Trace.Entering();

            if (!_windowsServiceHelper.IsRunningInElevatedMode())
            {
                Trace.Error("Needs Administrator privileges for configure agent as windows service.");
                throw new SecurityException(StringUtil.Loc("NeedAdminForConfigAgentWinService"));
            }

            // TODO: Fix bug that exists in the legacy Windows agent where configuration using mirrored credentials causes an error, but the agent is still functional (after restarting). Mirrored credentials is a supported scenario and shouldn't manifest any errors.

            // We use NetworkService as default account for build and release agent
            // We use Local System as default account for deployment agent, deployment pool agent
            bool isDeploymentGroupScenario = command.DeploymentGroup || command.DeploymentPool;
            NTAccount defaultServiceAccount = isDeploymentGroupScenario ? _windowsServiceHelper.GetDefaultAdminServiceAccount() : _windowsServiceHelper.GetDefaultServiceAccount();
            string logonAccount = command.GetWindowsLogonAccount(defaultValue: defaultServiceAccount.ToString(), descriptionMsg: StringUtil.Loc("WindowsLogonAccountNameDescription"));

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
                            _term.WriteLine(StringUtil.Loc("InvalidWindowsCredential"));
                        }
                        else
                        {
                            throw new SecurityException(StringUtil.Loc("InvalidWindowsCredential"));
                        }
                    }
                }
            }

            string serviceName;
            string serviceDisplayName;
            CalculateServiceName(settings, ServiceNamePattern, ServiceDisplayNamePattern, out serviceName, out serviceDisplayName);
            if (_windowsServiceHelper.IsServiceExists(serviceName))
            {
                _term.WriteLine(StringUtil.Loc("ServiceAlreadyExists", serviceName));
                _windowsServiceHelper.UninstallService(serviceName);
            }

            Trace.Info("Verifying if the account has LogonAsService permission");
            if (_windowsServiceHelper.IsUserHasLogonAsServicePrivilege(domainName, userName))
            {
                Trace.Info($"Account: {logonAccount} already has Logon As Service Privilege.");
            }
            else
            {
                if (!_windowsServiceHelper.GrantUserLogonAsServicePrivilage(domainName, userName))
                {
                    throw new InvalidOperationException(StringUtil.Loc("CanNotGrantPermission", logonAccount));
                }
            }

            // grant permission for agent root folder and work folder
            Trace.Info("Create local group and grant folder permission to service logon account.");
            string agentRoot = HostContext.GetDirectory(WellKnownDirectory.Root);
            string workFolder = HostContext.GetDirectory(WellKnownDirectory.Work);
            Directory.CreateDirectory(workFolder);
            _windowsServiceHelper.GrantDirectoryPermissionForAccount(logonAccount, new[] { agentRoot, workFolder });
            _term.WriteLine(StringUtil.Loc("GrantingFilePermissions", logonAccount));

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
                Trace.Error("Needs Administrator privileges for unconfigure windows service agent.");
                throw new SecurityException(StringUtil.Loc("NeedAdminForUnconfigWinServiceAgent"));
            }

            string serviceConfigPath = HostContext.GetConfigFile(WellKnownConfigFile.Service);
            string serviceName = File.ReadAllText(serviceConfigPath);
            if (_windowsServiceHelper.IsServiceExists(serviceName))
            {
                _windowsServiceHelper.StopService(serviceName);
                _windowsServiceHelper.UninstallService(serviceName);

                // Delete local group we created during configure.
                string agentRoot = HostContext.GetDirectory(WellKnownDirectory.Root);
                string workFolder = HostContext.GetDirectory(WellKnownDirectory.Work);
                _windowsServiceHelper.RevokeDirectoryPermissionForAccount(new[] { agentRoot, workFolder });

                // Remove registry key only on Windows
                _windowsServiceHelper.DeleteVstsAgentRegistryKey();
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
