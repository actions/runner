#if OS_WINDOWS
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.Win32;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener
{
    public sealed class AgentAutoLogonTestL0
    {
        private Mock<INativeWindowsServiceHelper> _windowsServiceHelper;
        private Mock<IPromptManager> _promptManager;
        private Mock<IProcessInvoker> _processInvoker;
        private Mock<IConfigurationStore> _store;
        private MockRegistryManager _mockRegManager;
        private AutoLogonSettings _autoLogonSettings;
        private CommandSettings _command;

        private string _sid = "001";
        private string _sidForDifferentUser = "007";
        private string _userName = "ironMan";
        private string _domainName = "avengers";
        
        private bool _powerCfgCalledForACOption = false;
        private bool _powerCfgCalledForDCOption = false;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void TestAutoLogonConfiguration()
        {
            using (var hc = new TestHostContext(this))
            {
                SetupTestEnv(hc, _sid);

                var iConfigManager = new AutoLogonManager();
                iConfigManager.Initialize(hc);
                await iConfigManager.ConfigureAsync(_command);

                VerifyRegistryChanges(_sid);
                Assert.True(_powerCfgCalledForACOption);
                Assert.True(_powerCfgCalledForDCOption);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void TestAutoLogonConfigurationForDifferentUser()
        {
            using (var hc = new TestHostContext(this))
            {
                SetupTestEnv(hc, _sidForDifferentUser);

                var iConfigManager = new AutoLogonManager();
                iConfigManager.Initialize(hc);
                await iConfigManager.ConfigureAsync(_command);
                
                VerifyRegistryChanges(_sidForDifferentUser);
                Assert.True(_powerCfgCalledForACOption);
                Assert.True(_powerCfgCalledForDCOption);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void TestAutoLogonUnConfigure()
        {
            //strategy-
            //1. fill some existing values in the registry
            //2. run configure
            //3. make sure the old values are there in the backup
            //4. unconfigure
            //5. make sure original values are reverted back

            using (var hc = new TestHostContext(this))
            {
                SetupTestEnv(hc, _sid);
                SetupRegistrySettings(_sid);

                var iConfigManager = new AutoLogonManager();
                iConfigManager.Initialize(hc);
                await iConfigManager.ConfigureAsync(_command);

                //make sure the backup was taken for the keys
                RegistryVerificationForBackup(_sid);

                // Debugger.Launch();
                iConfigManager.Unconfigure();
                
                //original values were reverted
                RegistryVerificationForUnConfigure(_sid);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void TestAutoLogonUnConfigureForDifferentUser()
        {
            //strategy-
            //1. fill some existing values in the registry
            //2. run configure
            //3. make sure the old values are there in the backup
            //4. unconfigure
            //5. make sure original values are reverted back

            using (var hc = new TestHostContext(this))
            {
                SetupTestEnv(hc, _sidForDifferentUser);

                SetupRegistrySettings(_sidForDifferentUser);

                var iConfigManager = new AutoLogonManager();
                iConfigManager.Initialize(hc);
                await iConfigManager.ConfigureAsync(_command);

                //make sure the backup was taken for the keys
                RegistryVerificationForBackup(_sidForDifferentUser);
                
                iConfigManager.Unconfigure();

                //original values were reverted
                RegistryVerificationForUnConfigure(_sidForDifferentUser);
            }
        }

        private void RegistryVerificationForBackup(string securityId)
        {
            //screen saver (user specific)            
            ValidateRegistryValue(RegistryHive.Users, 
                                    $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.ScreenSaver}", 
                                    GetBackupValueName(RegistryConstants.UserSettings.ValueNames.ScreenSaver),
                                    "1");
            
            //HKLM setting
            ValidateRegistryValue(RegistryHive.LocalMachine, 
                                    RegistryConstants.MachineSettings.SubKeys.AutoLogon, 
                                    GetBackupValueName(RegistryConstants.MachineSettings.ValueNames.AutoLogon),
                                    "0");

            //autologon password (delete key)
            ValidateRegistryValue(RegistryHive.LocalMachine, 
                                    RegistryConstants.MachineSettings.SubKeys.AutoLogon,
                                    GetBackupValueName(RegistryConstants.MachineSettings.ValueNames.AutoLogonPassword),
                                    "xyz");
        }

        private string GetBackupValueName(string valueName)
        {
            return string.Concat(RegistryConstants.BackupKeyPrefix, valueName);
        }

        private void RegistryVerificationForUnConfigure(string securityId)
        {
            //screen saver (user specific)
            ValidateRegistryValue(RegistryHive.Users, $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.ScreenSaver}", RegistryConstants.UserSettings.ValueNames.ScreenSaver, "1");

            //HKLM setting
            ValidateRegistryValue(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogon, "0");

            //autologon password (delete key)
            ValidateRegistryValue(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonPassword, "xyz");

            //when done with reverting back the original settings we need to make sure we dont leave behind any extra setting            
            //user specific
            ValidateRegistryValue(RegistryHive.Users,
                                    $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.StartupProcess}",
                                    RegistryConstants.UserSettings.ValueNames.StartupProcess,
                                    null);
        }

        private void SetupRegistrySettings(string securityId)
        {
            //HKLM setting
            _mockRegManager.SetValue(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogon, "0");

            //setting that we delete
            _mockRegManager.SetValue(RegistryHive.LocalMachine, RegistryConstants.MachineSettings.SubKeys.AutoLogon, RegistryConstants.MachineSettings.ValueNames.AutoLogonPassword, "xyz");
            
            //screen saver (user specific)
            _mockRegManager.SetValue(RegistryHive.Users, $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.ScreenSaver}", RegistryConstants.UserSettings.ValueNames.ScreenSaver, "1");
        }

        private void SetupTestEnv(TestHostContext hc, string securityId)
        {
            _powerCfgCalledForACOption = _powerCfgCalledForDCOption = false;
            _autoLogonSettings = null;

            _windowsServiceHelper = new Mock<INativeWindowsServiceHelper>();
            hc.SetSingleton<INativeWindowsServiceHelper>(_windowsServiceHelper.Object);

            _promptManager = new Mock<IPromptManager>();
            hc.SetSingleton<IPromptManager>(_promptManager.Object);

            hc.SetSingleton<IWhichUtil>(new WhichUtil());

            _promptManager
                .Setup(x => x.ReadValue(
                    Constants.Agent.CommandLine.Args.WindowsLogonAccount, // argName
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(), // secret
                    It.IsAny<string>(), // defaultValue
                    Validators.NTAccountValidator, // validator
                    It.IsAny<bool>())) // unattended
                .Returns(string.Format(@"{0}\{1}", _domainName, _userName));

            _windowsServiceHelper.Setup(x => x.IsValidAutoLogonCredential(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _windowsServiceHelper.Setup(x => x.SetAutoLogonPassword(It.IsAny<string>()));
            _windowsServiceHelper.Setup(x => x.GetSecurityId(It.IsAny<string>(), It.IsAny<string>())).Returns(() => securityId);
            _windowsServiceHelper.Setup(x => x.IsRunningInElevatedMode()).Returns(true);

            _processInvoker = new Mock<IProcessInvoker>();
            hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
            hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

            _processInvoker.Setup(x => x.ExecuteAsync(
                                                It.IsAny<String>(), 
                                                "powercfg.exe", 
                                                "/Change monitor-timeout-ac 0",
                                                null,
                                                It.IsAny<CancellationToken>())).Returns(Task.FromResult<int>(SetPowerCfgFlags(true)));

            _processInvoker.Setup(x => x.ExecuteAsync(
                                                It.IsAny<String>(), 
                                                "powercfg.exe", 
                                                "/Change monitor-timeout-dc 0", 
                                                null,
                                                It.IsAny<CancellationToken>())).Returns(Task.FromResult<int>(SetPowerCfgFlags(false)));

            _mockRegManager = new MockRegistryManager();
            hc.SetSingleton<IWindowsRegistryManager>(_mockRegManager);

            _command = new CommandSettings(
                hc,
                new[]
                {
                    "--windowslogonaccount", "wont be honored",
                    "--windowslogonpassword", "sssh",
                    "--DisableScreenSaver"
                });
            
            _store = new Mock<IConfigurationStore>();
            _store.Setup(x => x.SaveAutoLogonSettings(It.IsAny<AutoLogonSettings>()))
                .Callback((AutoLogonSettings settings) =>
                {
                    _autoLogonSettings = settings;
                });
            
            _store.Setup(x => x.IsAutoLogonConfigured()).Returns(() => _autoLogonSettings != null);
            _store.Setup(x => x.GetAutoLogonSettings()).Returns(() => _autoLogonSettings);

            

            hc.SetSingleton<IConfigurationStore>(_store.Object);

            hc.SetSingleton<IAutoLogonRegistryManager>(new AutoLogonRegistryManager());
        }

        private int SetPowerCfgFlags(bool isForACOption)
        {
            if (isForACOption)
            {
                _powerCfgCalledForACOption = true;
            }
            else
            {
                _powerCfgCalledForDCOption = true;
            }
            return 0;
        }

        public void VerifyRegistryChanges(string securityId)
        {
            ValidateRegistryValue(RegistryHive.LocalMachine,
                                    RegistryConstants.MachineSettings.SubKeys.AutoLogon,
                                    RegistryConstants.MachineSettings.ValueNames.AutoLogon,
                                    "1");

            ValidateRegistryValue(RegistryHive.LocalMachine,
                                    RegistryConstants.MachineSettings.SubKeys.AutoLogon,
                                    RegistryConstants.MachineSettings.ValueNames.AutoLogonUserName,
                                    _userName);

            ValidateRegistryValue(RegistryHive.LocalMachine,
                                    RegistryConstants.MachineSettings.SubKeys.AutoLogon,
                                    RegistryConstants.MachineSettings.ValueNames.AutoLogonPassword,
                                    null);
            
            ValidateRegistryValue(RegistryHive.Users,
                                    $"{securityId}\\{RegistryConstants.UserSettings.SubKeys.ScreenSaver}",
                                    RegistryConstants.UserSettings.ValueNames.ScreenSaver,
                                    "0");
        }

        public void ValidateRegistryValue(RegistryHive hive, string subKeyName, string name, string expectedValue)
        {
            var actualValue = _mockRegManager.GetValue(hive, subKeyName, name);

            var validationPassed = string.Equals(expectedValue, actualValue, StringComparison.OrdinalIgnoreCase);
            Assert.True(validationPassed, $"Validation failed for '{subKeyName}\\{name}'. Expected - {expectedValue} Actual - {actualValue}");
        }
    }

    public class MockRegistryManager : AgentService, IWindowsRegistryManager
    {
        private Dictionary<string, string> _regStore;

        public MockRegistryManager()
        {
            _regStore = new Dictionary<string, string>();
        }

        public string GetValue(RegistryHive hive, string subKeyName, string name)
        {
            var key = string.Concat(hive.ToString(), subKeyName, name);
            return _regStore.ContainsKey(key) ? _regStore[key] : null;
        }

        public void SetValue(RegistryHive hive, string subKeyName, string name, string value)
        {
            var key = string.Concat(hive.ToString(), subKeyName, name);
            if (_regStore.ContainsKey(key))
            {
                _regStore[key] = value;
            }
            else
            {
                _regStore.Add(key, value);
            }            
        }

        public void DeleteValue(RegistryHive hive, string subKeyName, string name)
        {
            var key = string.Concat(hive.ToString(), subKeyName, name);
            _regStore.Remove(key);
        }

        public bool SubKeyExists(RegistryHive hive, string subKeyName)
        {
            return true;
        }
    }
}
#endif