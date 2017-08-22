#if OS_WINDOWS
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(AutoLogonRegistryManager))]
    public interface IAutoLogonRegistryManager : IAgentService
    {
        void GetAutoLogonUserDetails(out string domainName, out string userName); 
        void UpdateRegistrySettings(CommandSettings command, string domainName, string userName, string logonPassword);
        void ResetRegistrySettings(string domainName, string userName);
        //used to log all the autologon related registry settings when agent is running
        void DumpAutoLogonRegistrySettings();
    }

    public class AutoLogonRegistryManager : AgentService, IAutoLogonRegistryManager
    {
        private IWindowsRegistryManager _registryManager;
        private INativeWindowsServiceHelper _windowsServiceHelper;
        private ITerminal _terminal;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _registryManager = hostContext.GetService<IWindowsRegistryManager>();
            _windowsServiceHelper = hostContext.GetService<INativeWindowsServiceHelper>();
            _terminal = HostContext.GetService<ITerminal>();
        }

        public void GetAutoLogonUserDetails(out string domainName, out string userName)
        {
            userName = null;
            domainName = null;

            var regValue = _registryManager.GetValue(RegistryHive.LocalMachine, 
                                                        RegistryConstants.MachineSettings.SubKeys.AutoLogon, 
                                                        RegistryConstants.MachineSettings.ValueNames.AutoLogon);
            if (int.TryParse(regValue, out int autoLogonEnabled)
                    && autoLogonEnabled == 1)
            {
                userName = _registryManager.GetValue(RegistryHive.LocalMachine,
                                                        RegistryConstants.MachineSettings.SubKeys.AutoLogon,
                                                        RegistryConstants.MachineSettings.ValueNames.AutoLogonUserName);
                domainName = _registryManager.GetValue(RegistryHive.LocalMachine, 
                                                        RegistryConstants.MachineSettings.SubKeys.AutoLogon,
                                                        RegistryConstants.MachineSettings.ValueNames.AutoLogonDomainName);
            }
        }

        public void UpdateRegistrySettings(CommandSettings command, string domainName, string userName, string logonPassword)
        {
            IntPtr userHandler = IntPtr.Zero;
            PROFILEINFO userProfile = new PROFILEINFO();
            try
            {
                string securityId = _windowsServiceHelper.GetSecurityId(domainName, userName);
                if(string.IsNullOrEmpty(securityId))
                {
                    Trace.Error($"Could not find the Security ID for the user '{domainName}\\{userName}'. AutoLogon will not be configured.");                    
                    throw new Exception(StringUtil.Loc("InvalidSIDForUser", domainName, userName));
                }

                //check if the registry exists for the user, if not load the user profile
                if(!_registryManager.SubKeyExists(RegistryHive.Users, securityId))
                {
                    userProfile.dwSize = Marshal.SizeOf(typeof(PROFILEINFO));
                    userProfile.lpUserName = userName;

                    _windowsServiceHelper.LoadUserProfile(domainName, userName, logonPassword, out userHandler, out userProfile);
                }

                if(!_registryManager.SubKeyExists(RegistryHive.Users, securityId))
                {
                    throw new InvalidOperationException(StringUtil.Loc("ProfileLoadFailure", domainName, userName));
                }

                //machine specific settings, i.e., autologon
                UpdateMachineSpecificRegistrySettings(domainName, userName);

                //user specific, i.e., screensaver and startup process
                UpdateUserSpecificRegistrySettings(command, securityId);
            }
            finally
            {
                if(userHandler != IntPtr.Zero)
                {
                    _windowsServiceHelper.UnloadUserProfile(userHandler, userProfile);
                }
            }
        }

        public void ResetRegistrySettings(string domainName, string userName)
        {
            string securityId = _windowsServiceHelper.GetSecurityId(domainName, userName);
            if(string.IsNullOrEmpty(securityId))
            {
                Trace.Error($"Could not find the Security ID for the user '{domainName}\\{userName}'. Unconfiguration of AutoLogon is not possible.");
                throw new Exception(StringUtil.Loc("InvalidSIDForUser", domainName, userName));
            }
            
            //machine specific            
            ResetAutoLogon(domainName, userName);

            //user specific
            RemoveStartupCommand(RegistryHive.Users, securityId);
        }

        public void DumpAutoLogonRegistrySettings()
        {
            Trace.Info("Dump from the registry for autologon related settings");
            Trace.Info("****Machine specific policies/settings****");
            if (_registryManager.SubKeyExists(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.ShutdownReasonDomainPolicy))
            {
                var shutDownReasonSubKey = RegistryConstants.MachineSettings.SubKeys.ShutdownReasonDomainPolicy;
                var shutDownReasonValueName = RegistryConstants.MachineSettings.ValueNames.ShutdownReason;                
                var shutdownReasonValue = _registryManager.GetValue(RegistryHive.LocalMachine, shutDownReasonSubKey, shutDownReasonValueName);
                Trace.Info($"Shutdown reason domain policy. Subkey - {shutDownReasonSubKey} ValueName - {shutDownReasonValueName} : {shutdownReasonValue}");
            }
            else
            {
                Trace.Info($"Shutdown reason domain policy not found.");
            }
            
            if (_registryManager.SubKeyExists(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.LegalNotice))
            {
                var legalNoticeSubKey = RegistryConstants.MachineSettings.SubKeys.LegalNotice;
                var captionValueName = RegistryConstants.MachineSettings.ValueNames.LegalNoticeCaption;
                //legal caption/text                
                var legalNoticeCaption = _registryManager.GetValue(RegistryHive.LocalMachine, legalNoticeSubKey, captionValueName);
                //we must avoid printing the text/caption in the logs as it is user data
                var isLegalNoticeCaptionDefined = !string.IsNullOrEmpty(legalNoticeCaption);
                Trace.Info($"Legal notice caption - Subkey - {legalNoticeSubKey} ValueName - {captionValueName}. Is defined - {isLegalNoticeCaptionDefined}");
                
                var textValueName = RegistryConstants.MachineSettings.ValueNames.LegalNoticeText;
                var legalNoticeText =  _registryManager.GetValue(RegistryHive.LocalMachine, legalNoticeSubKey, textValueName);
                var isLegalNoticeTextDefined = !string.IsNullOrEmpty(legalNoticeCaption);
                Trace.Info($"Legal notice text - Subkey - {legalNoticeSubKey} ValueName - {textValueName}. Is defined - {isLegalNoticeTextDefined}");
            }
            else
            {
                Trace.Info($"LegalNotice caption/text not defined");
            }

            var autoLogonSubKey = RegistryConstants.MachineSettings.SubKeys.AutoLogon;
            var valueName = RegistryConstants.MachineSettings.ValueNames.AutoLogon;
            var isAutoLogonEnabled = _registryManager.GetValue(RegistryHive.LocalMachine, autoLogonSubKey, valueName);
            Trace.Info($"AutoLogon. Subkey -  {autoLogonSubKey}. ValueName - {valueName} : {isAutoLogonEnabled} (0-disabled, 1-enabled)");

            var userValueName = RegistryConstants.MachineSettings.ValueNames.AutoLogonUserName;
            var domainValueName = RegistryConstants.MachineSettings.ValueNames.AutoLogonDomainName;
            var userName = _registryManager.GetValue(RegistryHive.LocalMachine, autoLogonSubKey, userValueName);
            var domainName = _registryManager.GetValue(RegistryHive.LocalMachine, autoLogonSubKey, domainValueName);
            Trace.Info($"AutoLogonUser. Subkey -  {autoLogonSubKey}. ValueName - {userValueName} : {userName}");
            Trace.Info($"AutoLogonUser. Subkey -  {autoLogonSubKey}. ValueName - {domainValueName} : {domainName}");

            Trace.Info("****User specific policies/settings****");
            var screenSaverPolicySubKeyName = RegistryConstants.UserSettings.SubKeys.ScreenSaverDomainPolicy;
            var screenSaverValueName = RegistryConstants.UserSettings.ValueNames.ScreenSaver;
            if(_registryManager.SubKeyExists(RegistryHive.CurrentUser, screenSaverPolicySubKeyName))
            {                
                var screenSaverSettingValue = _registryManager.GetValue(RegistryHive.CurrentUser, screenSaverPolicySubKeyName, screenSaverValueName);
                Trace.Info($"Screensaver policy.  SubKey - {screenSaverPolicySubKeyName} ValueName - {screenSaverValueName} : {screenSaverSettingValue} (1- enabled)");
            }
            else
            {
                Trace.Info($"Screen saver domain policy doesnt exist");
            }

            Trace.Info("****User specific settings****");
            
            var  screenSaverSettingSubKeyName = RegistryConstants.UserSettings.SubKeys.ScreenSaver;
            var screenSaverSettingValueName = RegistryConstants.UserSettings.ValueNames.ScreenSaver;            
            var screenSaverValue = _registryManager.GetValue(RegistryHive.CurrentUser, screenSaverSettingSubKeyName, screenSaverSettingValueName);
            Trace.Info($"Screensaver - SubKey - {screenSaverSettingSubKeyName}, ValueName - {screenSaverSettingValueName} : {screenSaverValue} (0-disabled, 1-enabled)");

            var startupSubKeyName = RegistryConstants.UserSettings.SubKeys.StartupProcess;
            var startupValueName = RegistryConstants.UserSettings.ValueNames.StartupProcess;
            var startupProcessPath = _registryManager.GetValue(RegistryHive.CurrentUser, startupSubKeyName, startupValueName);
            Trace.Info($"Startup process SubKey - {startupSubKeyName} ValueName - {startupValueName} : {startupProcessPath}");

            Trace.Info("");
        }

        private void ResetAutoLogon(string domainName, string userName)
        {
            var actualDomainNameForAutoLogon = _registryManager.GetValue(RegistryHive.LocalMachine, 
                                                                            RegistryConstants.MachineSettings.SubKeys.AutoLogon,
                                                                            RegistryConstants.MachineSettings.ValueNames.AutoLogonDomainName);

            var actualUserNameForAutoLogon = _registryManager.GetValue(RegistryHive.LocalMachine, 
                                                                            RegistryConstants.MachineSettings.SubKeys.AutoLogon, 
                                                                            RegistryConstants.MachineSettings.ValueNames.AutoLogonUserName);

            if (string.Equals(actualDomainNameForAutoLogon, domainName, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(actualUserNameForAutoLogon, userName, StringComparison.CurrentCultureIgnoreCase))
            {
                _registryManager.SetValue(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonUserName, "");
                _registryManager.SetValue(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonDomainName, "");
                _registryManager.SetValue(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogon, "0");
            }
            else
            {
                Trace.Info("AutoLogon user and/or domain name is not same as expected after autologon configuration.");
                Trace.Info($"Actual values: Domain - {actualDomainNameForAutoLogon}, user - {actualUserNameForAutoLogon}");
                Trace.Info($"Expected values: Domain - {domainName}, user - {userName}");
                Trace.Info("Skipping the revert of autologon settings.");
            }
        }

        private void UpdateMachineSpecificRegistrySettings(string domainName, string userName)
        {
            var hive = RegistryHive.LocalMachine;
            //before enabling autologon, inspect the policies that may affect it and log the warning
            InspectAutoLogonRelatedPolicies();

            _registryManager.SetValue(hive, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonUserName, userName);
            _registryManager.SetValue(hive, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonDomainName, domainName);

            _registryManager.DeleteValue(hive, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonPassword);
            _registryManager.DeleteValue(hive, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonCount);

            _registryManager.SetValue(hive, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogon, "1");
        }

        private void InspectAutoLogonRelatedPolicies()
        {
            Trace.Info("Checking for policies that may prevent autologon from working correctly.");
            _terminal.WriteLine(StringUtil.Loc("AutoLogonPoliciesInspection"));

            var warningReasons = new List<string>();
            if (_registryManager.SubKeyExists(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.ShutdownReasonDomainPolicy))
            {
                //shutdown reason
                var shutdownReasonValue = _registryManager.GetValue(RegistryHive.LocalMachine, 
                                                                        RegistryConstants.MachineSettings.SubKeys.ShutdownReasonDomainPolicy, 
                                                                        RegistryConstants.MachineSettings.ValueNames.ShutdownReason);
                if (int.TryParse(shutdownReasonValue, out int shutdownReasonOn) 
                        && shutdownReasonOn == 1)
                {
                    warningReasons.Add(StringUtil.Loc("AutoLogonPolicies_ShutdownReason"));
                }
            }

            
            if (_registryManager.SubKeyExists(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.LegalNotice))
            {
                //legal caption/text
                var legalNoticeCaption = _registryManager.GetValue(RegistryHive.LocalMachine, 
                                                                    RegistryConstants.MachineSettings.SubKeys.LegalNotice,
                                                                    RegistryConstants.MachineSettings.ValueNames.LegalNoticeCaption);
                var legalNoticeText =  _registryManager.GetValue(RegistryHive.LocalMachine, 
                                                                    RegistryConstants.MachineSettings.SubKeys.LegalNotice, 
                                                                    RegistryConstants.MachineSettings.ValueNames.LegalNoticeText);
                if (!string.IsNullOrEmpty(legalNoticeCaption) || !string.IsNullOrEmpty(legalNoticeText))
                {
                    warningReasons.Add(StringUtil.Loc("AutoLogonPolicies_LegalNotice"));
                }
            }
            
            if (warningReasons.Count > 0)
            {
                Trace.Warning("Following policies may affect the autologon:");
                _terminal.WriteError(StringUtil.Loc("AutoLogonPoliciesWarningsHeader"));
                for (int i=0; i < warningReasons.Count; i++)
                {
                    var msg = String.Format("{0} - {1}", i + 1, warningReasons[i]);
                    Trace.Warning(msg);
                    _terminal.WriteError(msg);
                }
                _terminal.WriteLine();
            }
        }

        private void UpdateUserSpecificRegistrySettings(CommandSettings command, string securityId)
        {
            //User specific
            UpdateScreenSaverSettings(command, securityId);
            
            //User specific
            string subKeyName = $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.StartupProcess}";
            _registryManager.SetValue(RegistryHive.Users, subKeyName, RegistryConstants.UserSettings.ValueNames.StartupProcess, GetStartupCommand());
        }

        private void UpdateScreenSaverSettings(CommandSettings command, string securityId)
        {
            Trace.Info("Checking for policies that may prevent screensaver from being disabled.");
            _terminal.WriteLine(StringUtil.Loc("ScreenSaverPoliciesInspection"));

            string subKeyName = $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.ScreenSaverDomainPolicy}";
            if(_registryManager.SubKeyExists(RegistryHive.Users, subKeyName))
            {            
                var screenSaverValue = _registryManager.GetValue(RegistryHive.Users, subKeyName, RegistryConstants.UserSettings.ValueNames.ScreenSaver);
                if (int.TryParse(screenSaverValue, out int isScreenSaverDomainPolicySet)
                        && isScreenSaverDomainPolicySet == 1)
                {
                    Trace.Warning("Screensaver policy is defined on the machine. Screensaver may not remain disabled always.");
                    _terminal.WriteError(StringUtil.Loc("ScreenSaverPolicyWarning"));
                }
            }

            string screenSaverSubKeyName = $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.ScreenSaver}";
            _registryManager.SetValue(RegistryHive.Users, screenSaverSubKeyName, RegistryConstants.UserSettings.ValueNames.ScreenSaver, "0");
        }

        private string GetStartupCommand()
        {
            //startup process            
            string cmdExePath = Environment.GetEnvironmentVariable("comspec");
            if (string.IsNullOrEmpty(cmdExePath))
            {
                Trace.Error("Unable to get the path for cmd.exe.");
                throw new Exception(StringUtil.Loc("FilePathNotFound", "cmd.exe"));
            }

            //file to run in cmd.exe
            var filePath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), "run.cmd");

            //extra " are to handle the spaces in the file path (if any)
            var startupCommand = $@"{cmdExePath} /D /S /C start ""Agent with AutoLogon"" ""{filePath}"" --startuptype autostartup";            
            Trace.Info($"Agent auto logon startup command: '{startupCommand}'");

            return startupCommand;
        }

        private void RemoveStartupCommand(RegistryHive targetHive, string securityId)
        {
            var startupProcessSubKeyName = $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.StartupProcess}";
            var expectedStartupCmd = GetStartupCommand();
            var actualStartupCmd = _registryManager.GetValue(targetHive, startupProcessSubKeyName, RegistryConstants.UserSettings.ValueNames.StartupProcess);

            if(string.Equals(actualStartupCmd, expectedStartupCmd, StringComparison.CurrentCultureIgnoreCase))
            {
                _registryManager.DeleteValue(RegistryHive.Users, startupProcessSubKeyName, RegistryConstants.UserSettings.ValueNames.StartupProcess);
            }
            else
            {
                Trace.Info($"Startup process command is not same as expected after autologon configuration. Skipping the revert of it.");
                Trace.Info($"Actual - {actualStartupCmd}, Expected - {expectedStartupCmd}.");
            }
        }
    }

    public class RegistryConstants
    {
        public class MachineSettings
        {
            public class SubKeys
            {
                public const string AutoLogon = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
                public const string ShutdownReasonDomainPolicy = @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability";
                public const string LegalNotice = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
            }

            public class ValueNames
            {
                public const string AutoLogon = "AutoAdminLogon";
                public const string AutoLogonUserName = "DefaultUserName";
                public const string AutoLogonDomainName = "DefaultDomainName";
                public const string AutoLogonCount = "AutoLogonCount";
                public const string AutoLogonPassword = "DefaultPassword";

                public const string ShutdownReason = "ShutdownReasonOn";                
                public const string LegalNoticeCaption = "LegalNoticeCaption";
                public const string LegalNoticeText = "LegalNoticeText";
            }
        }

        public class UserSettings
        {
            public class SubKeys
            {
                public const string ScreenSaver = @"Control Panel\Desktop";
                public const string ScreenSaverDomainPolicy = @"Software\Policies\Microsoft\Windows\Control Panel\Desktop";
                public const string StartupProcess = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            }

            public class ValueNames
            {
                public const string ScreenSaver = "ScreenSaveActive";
                //Value name in the startup tasks list. Every startup task has a name and the command to run.
                //the command gets filled up during AutoLogon configuration
                public const string StartupProcess = "VSTSAgent";
            }
        }
    }
}
#endif