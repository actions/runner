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
        private const string WindowsServiceControllerName = "AgentService.exe";
        private const string WindowsLogonAccount = "windowslogonaccount";
        private const string WindowsLogonPassword = "windowslogonpassword";
        private const string ServiceNamePattern = "vstsagent.{0}.{1}";
        private const string ServiceDisplayNamePattern = "VSTS Agent ({0}.{1})";

        private string _logonAccount;
        private string _domainName;
        private string _userName;

        public override bool ConfigureService(AgentSettings settings, Dictionary<string, string> args, bool enforceSupplied)
        {
            Trace.Entering();
            // TODO: add entering with info level. By default the error leve would be info. Config changes can get lost with this as entering is at Verbose level. For config all the logs should be logged.
            // TODO: Fix bug that exists in the legacy Windows agent where configuration using mirrored credentials causes an error, but the agent is still functional (after restarting). Mirrored credentials is a supported scenario and shouldn't manifest any errors.

            var windowsSecurityManager = HostContext.GetService<IWindowsSecurityManager>();
            var consoleWizard = HostContext.GetService<IConsoleWizard>();
            string logonPassword = string.Empty;

            NTAccount defaultServiceAccount = windowsSecurityManager.GetDefaultServiceAccount();
            _logonAccount = consoleWizard.ReadValue(WindowsLogonAccount,
                                                StringUtil.Loc("WindowsLogonAccountNameDescription"),
                                                false,
                                                defaultServiceAccount.ToString(),
                                                Validators.NTAccountValidator,
                                                args,
                                                enforceSupplied);
            Trace.Info("Received LogonAccount: {0}", _logonAccount);

            WindowsSecurityManager.GetAccountSegments(_logonAccount, out _domainName, out _userName);
            if ((string.IsNullOrEmpty(_domainName) || _domainName.Equals(".", StringComparison.CurrentCultureIgnoreCase)) && !_logonAccount.Contains('@'))
            {
                _logonAccount = String.Format("{0}\\{1}", Environment.MachineName, _userName);
            }

            Trace.Info("LogonAccount after transforming: {0}, user: {1}, domain: {2}", _logonAccount, _userName, _domainName);
            if (!defaultServiceAccount.Equals(new NTAccount(_logonAccount)))
            {
                while (true)
                {
                    Trace.Info("Acquiring logon account password");
                    logonPassword = consoleWizard.ReadValue(WindowsLogonPassword,
                                                        StringUtil.Loc("WindowsLogonPasswordDescription", _logonAccount),
                                                        true,
                                                        string.Empty,
                                                        Validators.NonEmptyValidator,
                                                        args,
                                                        enforceSupplied);

                    // TODO: If account is locked there is no point in retrying, translate error to useful message
                    if (windowsSecurityManager.IsValidCredential(_domainName, _userName, logonPassword) || enforceSupplied)
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
            if (!windowsSecurityManager.CheckUserHasLogonAsServicePrivilege(_domainName, _userName))
            {
                Trace.Info(StringUtil.Format("Account: {0} already has Logon As Service Privilege.", _logonAccount));
            }
            else
            {
                if (!windowsSecurityManager.GrantUserLogonAsServicePrivilage(_domainName, _userName))
                {
                    throw new InvalidOperationException(StringUtil.Loc("CanNotGrantPermission", _logonAccount));
                }
            }

            InstallService(settings.ServiceName, settings.ServiceDisplayName, _logonAccount, logonPassword);

            // TODO: If its service identity add it to appropriate PoolGroup
            // TODO: Add registry key after installation
            return true;
        }

        private void UninstallService(string serviceName)
        {
            Trace.Entering();
            IntPtr scmHndl = OpenSCManager(null, null, ServiceManagerRights.Connect);

            if (scmHndl.ToInt64() <= 0)
            {
                throw new Exception(StringUtil.Loc("FailedToOpenSCManager"));
            }

            try
            {
                IntPtr serviceHndl = OpenService(
                    scmHndl,
                    serviceName,
                    ServiceRights.StandardRightsRequired | ServiceRights.Stop | ServiceRights.QueryStatus);

                if (serviceHndl == IntPtr.Zero)
                {
                    Trace.Info("Service is not installed");
                    return;
                }

                try
                {
                    Trace.Info(StringUtil.Format("Trying to delete service {0}", serviceName));
                    int result = DeleteService(serviceHndl);
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
                    CloseServiceHandle(serviceHndl);
                }
            }
            finally
            {
                CloseServiceHandle(scmHndl);
            }
        }

        protected virtual void InstallService(string serviceName, string serviceDisplayName, string logonAccount, string logonPassword)
        {
            Trace.Entering();

            string agentServiceExecutable = Path.Combine(IOUtil.GetBinPath(), WindowsServiceControllerName);
            IntPtr scmHndl = OpenSCManager(null, null, ServiceManagerRights.AllAccess);

            if (scmHndl.ToInt64() <= 0)
            {
                throw new Exception("Failed to Open Service Control Manager");
            }

            try
            {
                Trace.Verbose(StringUtil.Format("Opened SCManager. Trying to create service {0}", serviceName));

                IntPtr serviceHndl = CreateService(
                                        scmHndl,
                                        serviceName,
                                        serviceDisplayName,
                                        ServiceRights.QueryStatus | ServiceRights.Start,
                                        SERVICE_WIN32_OWN_PROCESS,
                                        ServiceBootFlag.AutoStart,
                                        ServiceError.Normal,
                                        agentServiceExecutable,
                                        null,
                                        IntPtr.Zero,
                                        null,
                                        logonAccount,
                                        logonPassword);

                if (serviceHndl.ToInt64() <= 0)
                {
                    throw new InvalidOperationException(StringUtil.Loc("OperationFailed", nameof(CreateService), GetLastError()));
                }

                _term.WriteLine(StringUtil.Loc("ServiceConfigured", serviceName));
                CloseServiceHandle(serviceHndl);
            }
            finally
            {
                CloseServiceHandle(scmHndl);
            }
        }

        public override void StopService(string serviceName)
        {
            Trace.Entering();
            try
            {
                var service = TryGetServiceController(serviceName);
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
                _term.WriteError(StringUtil.Loc("CanNotStopService"));

                // Log the exception but do not report it as error. We can try uninstalling the service and then report it as error if something goes wrong.
            }
        }

        protected virtual ServiceController TryGetServiceController(string serviceName)
        {
            Trace.Entering();

            return
                ServiceController.GetServices()
                    .FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        public override bool CheckServiceExists(string serviceName)
        {
            Trace.Entering();
            try
            {
                var service = TryGetServiceController(serviceName);
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
                var service = TryGetServiceController(serviceName);
                if (service != null)
                {
                    // TODO Fix this to add permission, this is to make NT Authority\Local Service run as service
                    var windowsSecurityManager = HostContext.GetService<IWindowsSecurityManager>();
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

        // Interop declarations section for windows SCM operations

        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        public const int SERVICE_NO_CHANGE = -1;

        public enum ServiceError
        {
            Ignore = 0x00000000,
            Normal = 0x00000001,
            Severe = 0x00000002,
            Critical = 0x00000003
        }

        public enum ServiceBootFlag
        {
            Start = 0x00000000,
            SystemStart = 0x00000001,
            AutoStart = 0x00000002,
            DemandStart = 0x00000003,
            Disabled = 0x00000004
        }

        [Flags]
        public enum ServiceRights
        {
            QueryConfig = 0x1,
            ChangeConfig = 0x2,
            QueryStatus = 0x4,
            EnumerateDependants = 0x8,
            Start = 0x10,
            Stop = 0x20,
            PauseContinue = 0x40,
            Interrogate = 0x80,
            UserDefinedControl = 0x100,
            Delete = 0x00010000,
            StandardRightsRequired = 0xF0000,
            AllAccess =
                (StandardRightsRequired | QueryConfig | ChangeConfig | QueryStatus | EnumerateDependants | Start | Stop
                 | PauseContinue | Interrogate | UserDefinedControl)
        }

        [Flags]
        public enum ServiceManagerRights
        {
            Connect = 0x0001,
            CreateService = 0x0002,
            EnumerateService = 0x0004,
            Lock = 0x0008,
            QueryLockStatus = 0x0010,
            ModifyBootConfig = 0x0020,
            StandardRightsRequired = 0xF0000,
            AllAccess =
                (StandardRightsRequired | Connect | CreateService | EnumerateService | Lock | QueryLockStatus
                 | ModifyBootConfig)
        }

        [DllImport("advapi32.dll")]
        public static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, ServiceManagerRights dwDesiredAccess);

        [DllImport("advapi32.dll")]
        public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceRights dwDesiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "CreateServiceA")]
        private static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            ServiceRights dwDesiredAccess,
            int dwServiceType,
            ServiceBootFlag dwStartType,
            ServiceError dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lp,
            string lpPassword);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int DeleteService(IntPtr hService);

        [DllImport("advapi32.dll")]
        private static extern int CloseServiceHandle(IntPtr hSCObject);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();
    }
}
