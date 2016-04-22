using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class WindowsServiceControlManager : ServiceControlManager
    {
        public const string WindowsServiceControllerName = "AgentService.exe";

        private const string ServiceNamePattern = "vstsagent.{0}.{1}";
        private const string ServiceDisplayNamePattern = "VSTS Agent ({0}.{1})";

        private INativeWindowsServiceHelper _windowsServiceHelper;

        private string _logonAccount;
        private string _domainName;
        private string _userName;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _windowsServiceHelper = HostContext.GetService<INativeWindowsServiceHelper>();
        }

        public override bool ConfigureService(AgentSettings settings, CommandSettings command)
        {
            Trace.Entering();
            // TODO: add entering with info level. By default the error leve would be info. Config changes can get lost with this as entering is at Verbose level. For config all the logs should be logged.
            // TODO: Fix bug that exists in the legacy Windows agent where configuration using mirrored credentials causes an error, but the agent is still functional (after restarting). Mirrored credentials is a supported scenario and shouldn't manifest any errors.

            string logonPassword = string.Empty;

            NTAccount defaultServiceAccount = _windowsServiceHelper.GetDefaultServiceAccount();
            _logonAccount = command.GetWindowsLogonAccount(defaultValue: defaultServiceAccount.ToString());
            NativeWindowsServiceHelper.GetAccountSegments(_logonAccount, out _domainName, out _userName);
            if ((string.IsNullOrEmpty(_domainName) || _domainName.Equals(".", StringComparison.CurrentCultureIgnoreCase)) && !_logonAccount.Contains('@'))
            {
                _logonAccount = String.Format("{0}\\{1}", Environment.MachineName, _userName);
            }

            Trace.Info("LogonAccount after transforming: {0}, user: {1}, domain: {2}", _logonAccount, _userName, _domainName);
            if (!defaultServiceAccount.Equals(new NTAccount(_logonAccount)))
            {
                while (true)
                {
                    logonPassword = command.GetWindowsLogonPassword();

                    // TODO: Fix this for unattended (should throw if not valid).
                    // TODO: If account is locked there is no point in retrying, translate error to useful message
                    if (_windowsServiceHelper.IsValidCredential(_domainName, _userName, logonPassword) || command.Unattended)
                    {
                        break;
                    }

                    Trace.Info("Invalid credential entered");
                    _term.WriteLine(StringUtil.Loc("InvalidWindowsCredential"));
                }
            }

            CalculateServiceName(settings, ServiceNamePattern, ServiceDisplayNamePattern);

            if (CheckServiceExists(settings.ServiceName))
            {
                _term.WriteLine(StringUtil.Loc("ServiceAleadyExists"));

                StopService(settings.ServiceName);
                UninstallService(settings.ServiceName);
            }

            Trace.Info("Verifying if the account has LogonAsService permission");
            if (!_windowsServiceHelper.CheckUserHasLogonAsServicePrivilege(_domainName, _userName))
            {
                Trace.Info(StringUtil.Format("Account: {0} already has Logon As Service Privilege.", _logonAccount));
            }
            else
            {
                if (!_windowsServiceHelper.GrantUserLogonAsServicePrivilage(_domainName, _userName))
                {
                    throw new InvalidOperationException(StringUtil.Loc("CanNotGrantPermission", _logonAccount));
                }
            }

            _windowsServiceHelper.InstallService(settings.ServiceName, settings.ServiceDisplayName, _logonAccount, logonPassword);

            // TODO: If its service identity add it to appropriate PoolGroup
            // TODO: Add registry key after installation
            return true;
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

        public override void StopService(string serviceName)
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

        public override bool CheckServiceExists(string serviceName)
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

        public override void StartService(string serviceName)
        {
            Trace.Entering();
            try
            {
                ServiceController service = _windowsServiceHelper.TryGetServiceController(serviceName);
                if (service != null)
                {
                    // TODO Fix this to add permission, this is to make NT Authority\Local Service run as service
                    var windowsSecurityManager = HostContext.GetService<INativeWindowsServiceHelper>();
                    windowsSecurityManager.SetPermissionForAccount(IOUtil.GetRootPath(), _logonAccount);

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
