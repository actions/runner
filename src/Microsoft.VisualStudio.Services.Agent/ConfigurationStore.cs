using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent
{
    //
    // Settings are persisted in this structure
    //
    [DataContract]
    public sealed class AgentSettings
    {
        [DataMember(EmitDefaultValue = false)]
        public bool AcceptTeeEula { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int AgentId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AgentName { get; set; }

        [IgnoreDataMember]
        public bool IsHosted => !string.IsNullOrEmpty(NotificationPipeName) || !string.IsNullOrEmpty(NotificationSocketAddress);

        [DataMember(EmitDefaultValue = false)]
        public string NotificationPipeName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string NotificationSocketAddress { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool SkipCapabilitiesScan { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool SkipSessionRecover { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int PoolId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string PoolName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ServerUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string WorkFolder { get; set; }

        // Do not use Project Name any more to save in agent settings file. Ensure to use ProjectId. 
        // Deployment Group scenario will not work for project rename scenario if we work with projectName
        [DataMember(EmitDefaultValue = false)]
        public string ProjectName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int MachineGroupId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int DeploymentGroupId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ProjectId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string CollectionName { get; set; }
    }

    [DataContract]
    public sealed class AutoLogonSettings
    {
        [DataMember(EmitDefaultValue = false)]
        public string UserDomainName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string UserName { get; set; }
    }

    [DataContract]
    public sealed class AgentRuntimeOptions
    {
#if OS_WINDOWS
        [DataMember(EmitDefaultValue = false)]
        public bool GitUseSecureChannel { get; set; }
#endif
    }

    [ServiceLocator(Default = typeof(ConfigurationStore))]
    public interface IConfigurationStore : IAgentService
    {
        string RootFolder { get; }
        bool IsConfigured();
        bool IsServiceConfigured();
        bool IsAutoLogonConfigured();
        bool HasCredentials();
        CredentialData GetCredentials();
        AgentSettings GetSettings();
        void SaveCredential(CredentialData credential);
        void SaveSettings(AgentSettings settings);
        void DeleteCredential();
        void DeleteSettings();
        void DeleteAutoLogonSettings();
        void SaveAutoLogonSettings(AutoLogonSettings settings);
        AutoLogonSettings GetAutoLogonSettings();
        AgentRuntimeOptions GetAgentRuntimeOptions();
        void SaveAgentRuntimeOptions(AgentRuntimeOptions options);
        void DeleteAgentRuntimeOptions();
    }

    public sealed class ConfigurationStore : AgentService, IConfigurationStore
    {
        private string _binPath;
        private string _configFilePath;
        private string _credFilePath;
        private string _serviceConfigFilePath;
        private string _autoLogonSettingsFilePath;
        private string _runtimeOptionsFilePath;

        private CredentialData _creds;
        private AgentSettings _settings;
        private AutoLogonSettings _autoLogonSettings;
        private AgentRuntimeOptions _runtimeOptions;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            Trace.Info("currentAssemblyLocation: {0}", currentAssemblyLocation);

            _binPath = HostContext.GetDirectory(WellKnownDirectory.Bin);
            Trace.Info("binPath: {0}", _binPath);

            RootFolder = HostContext.GetDirectory(WellKnownDirectory.Root);
            Trace.Info("RootFolder: {0}", RootFolder);

            _configFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Agent);
            Trace.Info("ConfigFilePath: {0}", _configFilePath);

            _credFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Credentials);
            Trace.Info("CredFilePath: {0}", _credFilePath);

            _serviceConfigFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Service);
            Trace.Info("ServiceConfigFilePath: {0}", _serviceConfigFilePath);

            _autoLogonSettingsFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Autologon);
            Trace.Info("AutoLogonSettingsFilePath: {0}", _autoLogonSettingsFilePath);

            _runtimeOptionsFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Options);
            Trace.Info("RuntimeOptionsFilePath: {0}", _runtimeOptionsFilePath);
        }

        public string RootFolder { get; private set; }

        public bool HasCredentials()
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            Trace.Info("HasCredentials()");
            bool credsStored = (new FileInfo(_credFilePath)).Exists;
            Trace.Info("stored {0}", credsStored);
            return credsStored;
        }

        public bool IsConfigured()
        {
            Trace.Info("IsConfigured()");
            bool configured = HostContext.RunMode == RunMode.Local || (new FileInfo(_configFilePath)).Exists;
            Trace.Info("IsConfigured: {0}", configured);
            return configured;
        }

        public bool IsServiceConfigured()
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            Trace.Info("IsServiceConfigured()");
            bool serviceConfigured = (new FileInfo(_serviceConfigFilePath)).Exists;
            Trace.Info($"IsServiceConfigured: {serviceConfigured}");
            return serviceConfigured;
        }

        public bool IsAutoLogonConfigured()
        {
            Trace.Entering();
            bool autoLogonConfigured = (new FileInfo(_autoLogonSettingsFilePath)).Exists;
            Trace.Info($"IsAutoLogonConfigured: {autoLogonConfigured}");
            return autoLogonConfigured;
        }

        public CredentialData GetCredentials()
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            if (_creds == null)
            {
                _creds = IOUtil.LoadObject<CredentialData>(_credFilePath);
            }

            return _creds;
        }

        public AgentSettings GetSettings()
        {
            if (_settings == null)
            {
                AgentSettings configuredSettings = null;
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath, Encoding.UTF8);
                    Trace.Info($"Read setting file: {json.Length} chars");
                    configuredSettings = StringUtil.ConvertFromJson<AgentSettings>(json);
                }

                if (HostContext.RunMode == RunMode.Local)
                {
                    _settings = new AgentSettings()
                    {
                        AcceptTeeEula = configuredSettings?.AcceptTeeEula ?? false,
                        AgentId = 1,
                        AgentName = "local-runner-agent",
                        PoolId = 1,
                        PoolName = "local-runner-pool",
                        ServerUrl = "http://127.0.0.1/vsts-agent-local-runner",
                        WorkFolder = configuredSettings?.WorkFolder ?? Constants.Path.WorkDirectory
                    };
                }
                else
                {
                    ArgUtil.NotNull(configuredSettings, nameof(configuredSettings));
                    _settings = configuredSettings;
                }
            }

            return _settings;
        }

        public AutoLogonSettings GetAutoLogonSettings()
        {
            if (_autoLogonSettings == null)
            {
                _autoLogonSettings = IOUtil.LoadObject<AutoLogonSettings>(_autoLogonSettingsFilePath);
            }

            return _autoLogonSettings;
        }

        public void SaveCredential(CredentialData credential)
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            Trace.Info("Saving {0} credential @ {1}", credential.Scheme, _credFilePath);
            if (File.Exists(_credFilePath))
            {
                // Delete existing credential file first, since the file is hidden and not able to overwrite.
                Trace.Info("Delete exist agent credential file.");
                IOUtil.DeleteFile(_credFilePath);
            }

            IOUtil.SaveObject(credential, _credFilePath);
            Trace.Info("Credentials Saved.");
            File.SetAttributes(_credFilePath, File.GetAttributes(_credFilePath) | FileAttributes.Hidden);
        }

        public void SaveSettings(AgentSettings settings)
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            Trace.Info("Saving agent settings.");
            if (File.Exists(_configFilePath))
            {
                // Delete existing agent settings file first, since the file is hidden and not able to overwrite.
                Trace.Info("Delete exist agent settings file.");
                IOUtil.DeleteFile(_configFilePath);
            }

            IOUtil.SaveObject(settings, _configFilePath);
            Trace.Info("Settings Saved.");
            File.SetAttributes(_configFilePath, File.GetAttributes(_configFilePath) | FileAttributes.Hidden);
        }

        public void SaveAutoLogonSettings(AutoLogonSettings autoLogonSettings)
        {
            Trace.Info("Saving autologon settings.");
            if (File.Exists(_autoLogonSettingsFilePath))
            {
                // Delete existing autologon settings file first, since the file is hidden and not able to overwrite.
                Trace.Info("Delete existing autologon settings file.");
                IOUtil.DeleteFile(_autoLogonSettingsFilePath);
            }

            IOUtil.SaveObject(autoLogonSettings, _autoLogonSettingsFilePath);
            Trace.Info("AutoLogon settings Saved.");
            File.SetAttributes(_autoLogonSettingsFilePath, File.GetAttributes(_autoLogonSettingsFilePath) | FileAttributes.Hidden);
        }

        public void DeleteCredential()
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            IOUtil.Delete(_credFilePath, default(CancellationToken));
        }

        public void DeleteSettings()
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            IOUtil.Delete(_configFilePath, default(CancellationToken));
        }

        public void DeleteAutoLogonSettings()
        {
            IOUtil.Delete(_autoLogonSettingsFilePath, default(CancellationToken));
        }

        public AgentRuntimeOptions GetAgentRuntimeOptions()
        {
            if (_runtimeOptions == null && File.Exists(_runtimeOptionsFilePath))
            {
                _runtimeOptions = IOUtil.LoadObject<AgentRuntimeOptions>(_runtimeOptionsFilePath);
            }

            return _runtimeOptions;
        }

        public void SaveAgentRuntimeOptions(AgentRuntimeOptions options)
        {
            Trace.Info("Saving runtime options.");
            if (File.Exists(_runtimeOptionsFilePath))
            {
                // Delete existing runtime options file first, since the file is hidden and not able to overwrite.
                Trace.Info("Delete exist runtime options file.");
                IOUtil.DeleteFile(_runtimeOptionsFilePath);
            }

            IOUtil.SaveObject(options, _runtimeOptionsFilePath);
            Trace.Info("Options Saved.");
            File.SetAttributes(_runtimeOptionsFilePath, File.GetAttributes(_runtimeOptionsFilePath) | FileAttributes.Hidden);
        }

        public void DeleteAgentRuntimeOptions()
        {
            IOUtil.Delete(_runtimeOptionsFilePath, default(CancellationToken));
        }
    }
}