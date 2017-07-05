using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using System.Runtime.Serialization;
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
        public int PoolId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string PoolName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ServerUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string WorkFolder { get; set; }

        // Do not use Project Name any more to save in agent settings file. Ensure to use ProjectId. 
        // Deployment Group scenario will not work for project rename scneario if we work with projectName
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
    }

    public sealed class ConfigurationStore : AgentService, IConfigurationStore
    {
        private string _binPath;
        private string _configFilePath;
        private string _credFilePath;
        private string _serviceConfigFilePath;
        private string _autoLogonSettingsFilePath;
        private CredentialData _creds;
        private AgentSettings _settings;
        private AutoLogonSettings _autoLogonSettings;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            Trace.Info("currentAssemblyLocation: {0}", currentAssemblyLocation);

            _binPath = IOUtil.GetBinPath();
            Trace.Info("binPath: {0}", _binPath);

            RootFolder = IOUtil.GetRootPath();
            Trace.Info("RootFolder: {0}", RootFolder);

            _configFilePath = IOUtil.GetConfigFilePath();
            Trace.Info("ConfigFilePath: {0}", _configFilePath);

            _credFilePath = IOUtil.GetCredFilePath();
            Trace.Info("CredFilePath: {0}", _credFilePath);

            _serviceConfigFilePath = IOUtil.GetServiceConfigFilePath();
            Trace.Info("ServiceConfigFilePath: {0}", _serviceConfigFilePath);

            _autoLogonSettingsFilePath = IOUtil.GetAutoLogonSettingsFilePath();
            Trace.Info("AutoLogonSettingsFilePath: {0}", _autoLogonSettingsFilePath);
        }

        public string RootFolder { get; private set; }

        public bool HasCredentials()
        {
            Trace.Info("HasCredentials()");
            bool credsStored = (new FileInfo(_credFilePath)).Exists;
            Trace.Info("stored {0}", credsStored);
            return credsStored;
        }

        public bool IsConfigured()
        {
            Trace.Info("IsConfigured()");
            bool configured = (new FileInfo(_configFilePath)).Exists;
            Trace.Info("IsConfigured: {0}", configured);
            return configured;
        }

        public bool IsServiceConfigured()
        {
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
                _settings = IOUtil.LoadObject<AgentSettings>(_configFilePath);
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
            IOUtil.Delete(_credFilePath, default(CancellationToken));
        }

        public void DeleteSettings()
        {
            IOUtil.Delete(_configFilePath, default(CancellationToken));
        }

        public void DeleteAutoLogonSettings()
        {
            IOUtil.Delete(_autoLogonSettingsFilePath, default(CancellationToken));
        }
    }
}