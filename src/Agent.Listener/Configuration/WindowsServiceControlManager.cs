#if OS_WINDOWS
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Security;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class WindowsServiceControlManager : ServiceControlManager, IWindowsServiceControlManager
    {
        public const string WindowsServiceControllerName = "AgentService.exe";

        private const string ServiceNamePattern = "vstsagent.{0}.{1}";
        private const string ServiceDisplayNamePattern = "VSTS Agent ({0}.{1})";

        private INativeWindowsServiceHelper _windowsServiceHelper;
        private ITerminal _term;

        private string _logonAccount;
        private string _domainName;
        private string _userName;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _windowsServiceHelper = HostContext.GetService<INativeWindowsServiceHelper>();
            _term = HostContext.GetService<ITerminal>();
        }

        public void ConfigureService(AgentSettings settings, CommandSettings command)
        {
            Trace.Entering();
            // TODO: Fix bug that exists in the legacy Windows agent where configuration using mirrored credentials causes an error, but the agent is still functional (after restarting). Mirrored credentials is a supported scenario and shouldn't manifest any errors.

            // We use NetworkService as default account.
            NTAccount defaultServiceAccount = _windowsServiceHelper.GetDefaultServiceAccount();

            _logonAccount = command.GetWindowsLogonAccount(defaultValue: defaultServiceAccount.ToString());
            NativeWindowsServiceHelper.GetAccountSegments(_logonAccount, out _domainName, out _userName);
            if ((string.IsNullOrEmpty(_domainName) || _domainName.Equals(".", StringComparison.CurrentCultureIgnoreCase)) && !_logonAccount.Contains('@'))
            {
                _logonAccount = String.Format("{0}\\{1}", Environment.MachineName, _userName);
            }

            Trace.Info("LogonAccount after transforming: {0}, user: {1}, domain: {2}", _logonAccount, _userName, _domainName);

            string logonPassword = string.Empty;
            if (!defaultServiceAccount.Equals(new NTAccount(_logonAccount)) && !NativeWindowsServiceHelper.IsWellKnownIdentity(_logonAccount))
            {
                while (true)
                {
                    logonPassword = command.GetWindowsLogonPassword(_logonAccount);
                    if (_windowsServiceHelper.IsValidCredential(_domainName, _userName, logonPassword))
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
            if (CheckServiceExists(serviceName))
            {
                _term.WriteLine(StringUtil.Loc("ServiceAlreadyExists", serviceName));
                UninstallService(serviceName);
            }

            Trace.Info("Verifying if the account has LogonAsService permission");
            if (!_windowsServiceHelper.CheckUserHasLogonAsServicePrivilege(_domainName, _userName))
            {
                Trace.Info($"Account: {_logonAccount} already has Logon As Service Privilege.");
            }
            else
            {
                if (!_windowsServiceHelper.GrantUserLogonAsServicePrivilage(_domainName, _userName))
                {
                    throw new InvalidOperationException(StringUtil.Loc("CanNotGrantPermission", _logonAccount));
                }
            }

            Trace.Info("Create local group and grant folder permission to service logon account.");
            GrantDirectoryPermissionForAccount();

            // install service.
            _windowsServiceHelper.InstallService(serviceName, serviceDisplayName, _logonAccount, logonPassword);

            // create .service file with service name.
            SaveServiceSettings(serviceName);

            // Add registry key after installation
            _windowsServiceHelper.CreateVstsAgentRegistryKey();

            Trace.Info("Configuration was successful, trying to start the service");
            StartService(serviceName);
        }

        private void GrantDirectoryPermissionForAccount()
        {
            Trace.Entering();
            string groupName = _windowsServiceHelper.GetUniqueBuildGroupName();
            Trace.Info(StringUtil.Format("Calculated unique group name {0}", groupName));

            if (!_windowsServiceHelper.LocalGroupExists(groupName))
            {
                Trace.Info(StringUtil.Format("Trying to create group {0}", groupName));
                _windowsServiceHelper.CreateLocalGroup(groupName);
            }

            Trace.Info(StringUtil.Format("Trying to add userName {0} to the group {0}", _logonAccount, groupName));
            _windowsServiceHelper.AddMemberToLocalGroup(_logonAccount, groupName);

            // grant permssion for agent root folder
            string agentRoot = IOUtil.GetRootPath();
            Trace.Info(StringUtil.Format("Set full access control to group for the folder {0}", agentRoot));
            _windowsServiceHelper.GrantFullControlToGroup(agentRoot, groupName);

            // grant permssion for work folder
            string workFolder = IOUtil.GetWorkPath(HostContext);
            Directory.CreateDirectory(workFolder);
            Trace.Info(StringUtil.Format("Set full access control to group for the folder {0}", workFolder));
            _windowsServiceHelper.GrantFullControlToGroup(workFolder, groupName);
        }

        private void RevokeDirectoryPermissionForAccount()
        {
            Trace.Entering();
            string groupName = _windowsServiceHelper.GetUniqueBuildGroupName();
            Trace.Info(StringUtil.Format("Calculated unique group name {0}", groupName));

            // remove the group from the work folder
            string workFolder = IOUtil.GetWorkPath(HostContext);
            if (Directory.Exists(workFolder))
            {
                Trace.Info(StringUtil.Format($"Remove the group {groupName} for the folder {workFolder}."));
                _windowsServiceHelper.RemoveGroupFromFolderSecuritySetting(workFolder, groupName);
            }

            //remove group from agent root folder
            string agentRoot = IOUtil.GetRootPath();
            if (Directory.Exists(agentRoot))
            {
                Trace.Info(StringUtil.Format($"Remove the group {groupName} for the folder {agentRoot}."));
                _windowsServiceHelper.RemoveGroupFromFolderSecuritySetting(agentRoot, groupName);
            }

            //delete group
            Trace.Info(StringUtil.Format($"Delete the group {groupName}."));
            _windowsServiceHelper.DeleteLocalGroup(groupName);
        }

        public void UnconfigureService()
        {
            string serviceConfigPath = IOUtil.GetServiceConfigFilePath();
            string serviceName = File.ReadAllText(serviceConfigPath);
            if (CheckServiceExists(serviceName))
            {
                StopService(serviceName);
                UninstallService(serviceName);

                // Delete local group we created during confiure.
                RevokeDirectoryPermissionForAccount();

                // Remove registry key only on Windows
                _windowsServiceHelper.DeleteVstsAgentRegistryKey();
            }

            IOUtil.DeleteFile(serviceConfigPath);
        }

        private void SaveServiceSettings(string serviceName)
        {
            string serviceConfigPath = IOUtil.GetServiceConfigFilePath();
            if (File.Exists(serviceConfigPath))
            {
                IOUtil.DeleteFile(serviceConfigPath);
            }

            File.WriteAllText(serviceConfigPath, serviceName, new UTF8Encoding(false));
            File.SetAttributes(serviceConfigPath, File.GetAttributes(serviceConfigPath) | FileAttributes.Hidden);
        }

        private void UninstallService(string serviceName)
        {
            Trace.Entering();
            IntPtr scmHndl = NativeWindowsServiceHelper.OpenSCManager(null, null, NativeWindowsServiceHelper.ServiceManagerRights.Connect);

            if (scmHndl.ToInt64() <= 0)
            {
                throw new Exception(StringUtil.Loc("FailedToOpenSCManager"));
            }

            try
            {
                IntPtr serviceHndl = NativeWindowsServiceHelper.OpenService(
                    scmHndl,
                    serviceName,
                    NativeWindowsServiceHelper.ServiceRights.StandardRightsRequired | NativeWindowsServiceHelper.ServiceRights.Stop | NativeWindowsServiceHelper.ServiceRights.QueryStatus);

                if (serviceHndl == IntPtr.Zero)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError == NativeWindowsServiceHelper.ReturnCode.ERROR_ACCESS_DENIED)
                    {
                        throw new Exception(StringUtil.Loc("ShouldBeAdmin"));
                    }
                    Trace.Info("Service is not installed");
                    return;
                }

                try
                {
                    Trace.Info(StringUtil.Format("Trying to delete service {0}", serviceName));
                    int result = NativeWindowsServiceHelper.DeleteService(serviceHndl);
                    if (result == 0)
                    {
                        result = Marshal.GetLastWin32Error();
                        Trace.Error(StringUtil.Format("Could not delete service, result: {0}", result));
                        throw new InvalidOperationException(StringUtil.Loc("CouldNotRemoveService", serviceName));
                    }

                    Trace.Info("successfully removed the service");
                }
                finally
                {
                    NativeWindowsServiceHelper.CloseServiceHandle(serviceHndl);
                }
            }
            finally
            {
                NativeWindowsServiceHelper.CloseServiceHandle(scmHndl);
            }
        }

        private void StopService(string serviceName)
        {
            Trace.Entering();
            try
            {
                ServiceController service = _windowsServiceHelper.TryGetServiceController(serviceName);
                if (service != null)
                {
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        Trace.Info("Trying to stop the service");
                        service.Stop();

                        try
                        {
                            _term.WriteLine(StringUtil.Loc("WaitForServiceToStop"));
                            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(35));
                        }
                        catch (System.ServiceProcess.TimeoutException)
                        {
                            throw new InvalidOperationException(StringUtil.Loc("CanNotStopService", serviceName));
                        }
                    }

                    Trace.Info("Successfully stopped the service");
                }
                else
                {
                    Trace.Info(StringUtil.Loc("CanNotFindService", serviceName));
                }
            }
            catch (Exception exception)
            {
                Trace.Error(exception);
                _term.WriteError(StringUtil.Loc("CanNotStopService", serviceName));

                // Log the exception but do not report it as error. We can try uninstalling the service and then report it as error if something goes wrong.
            }
        }

        private bool CheckServiceExists(string serviceName)
        {
            Trace.Entering();
            try
            {
                ServiceController service = _windowsServiceHelper.TryGetServiceController(serviceName);
                return service != null;
            }
            catch (Exception exception)
            {
                Trace.Error(exception);

                // If we can't check the status of the service, probably we can't do anything else too. Report it as error.
                throw;
            }
        }

        private void StartService(string serviceName)
        {
            Trace.Entering();
            try
            {
                ServiceController service = _windowsServiceHelper.TryGetServiceController(serviceName);
                if (service != null)
                {
                    service.Start();
                    _term.WriteLine(StringUtil.Loc("ServiceStartedSuccessfully", serviceName));
                }
                else
                {
                    throw new InvalidOperationException(StringUtil.Loc("CanNotFindService", serviceName));
                }
            }
            catch (Exception exception)
            {
                Trace.Error(exception);
                _term.WriteError(StringUtil.Loc("CanNotStartService"));

                // This is the last step in the configuration. Even if the start failed the status of the configuration should be error
                // If its configured through scripts its mandatory we indicate the failure where configuration failed to start the service
                throw;
            }
        }
    }
}
#endif