using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace GitHub.Runner.Common
{
    //
    // Settings are persisted in this structure
    //
    [DataContract]
    public sealed class RunnerSettings
    {
        [DataMember(Name = "IsHostedServer", EmitDefaultValue = false)]
        private bool? _isHostedServer;

        [DataMember(EmitDefaultValue = false)]
        public int AgentId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AgentName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool SkipSessionRecover { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int PoolId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string PoolName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ServerUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string GitHubUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string WorkFolder { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string MonitorSocketAddress { get; set; }

        [IgnoreDataMember]
        public bool IsHostedServer
        {
            get
            {
                // Old runners do not have this property. Hosted runners likely don't have this property either.
                return _isHostedServer ?? true;
            }

            set
            {
                _isHostedServer = value;
            }
        }

        /// <summary>
        // Computed property for convenience. Can either return:
        // 1. If runner was configured at the repo level, returns something like: "myorg/myrepo"
        // 2. If runner was configured at the org level, returns something like: "myorg"
        /// </summary>
        public string RepoOrOrgName
        {
            get
            {
                Uri accountUri = new Uri(this.ServerUrl);
                string repoOrOrgName = string.Empty;

                if (accountUri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase))
                {
                    Uri gitHubUrl = new Uri(this.GitHubUrl);

                    // Use the "NWO part" from the GitHub URL path
                    repoOrOrgName = gitHubUrl.AbsolutePath.Trim('/');
                }
                else
                {
                    repoOrOrgName = accountUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                }

                return repoOrOrgName;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (_isHostedServer.HasValue && _isHostedServer.Value)
            {
                _isHostedServer = null;
            }
        }
    }

    [ServiceLocator(Default = typeof(ConfigurationStore))]
    public interface IConfigurationStore : IRunnerService
    {
        bool IsConfigured();
        bool IsServiceConfigured();
        bool HasCredentials();
        CredentialData GetCredentials();
        CredentialData GetMigratedCredentials();
        RunnerSettings GetSettings();
        void SaveCredential(CredentialData credential);
        void SaveSettings(RunnerSettings settings);
        void DeleteCredential();
        void DeleteMigratedCredential();
        void DeleteSettings();
    }

    public sealed class ConfigurationStore : RunnerService, IConfigurationStore
    {
        private string _binPath;
        private string _configFilePath;
        private string _credFilePath;
        private string _migratedCredFilePath;
        private string _serviceConfigFilePath;

        private CredentialData _creds;
        private CredentialData _migratedCreds;
        private RunnerSettings _settings;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            Trace.Info("currentAssemblyLocation: {0}", currentAssemblyLocation);

            _binPath = HostContext.GetDirectory(WellKnownDirectory.Bin);
            Trace.Info("binPath: {0}", _binPath);

            RootFolder = HostContext.GetDirectory(WellKnownDirectory.Root);
            Trace.Info("RootFolder: {0}", RootFolder);

            _configFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Runner);
            Trace.Info("ConfigFilePath: {0}", _configFilePath);

            _credFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Credentials);
            Trace.Info("CredFilePath: {0}", _credFilePath);

            _migratedCredFilePath = hostContext.GetConfigFile(WellKnownConfigFile.MigratedCredentials);
            Trace.Info("MigratedCredFilePath: {0}", _migratedCredFilePath);

            _serviceConfigFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Service);
            Trace.Info("ServiceConfigFilePath: {0}", _serviceConfigFilePath);
        }

        public string RootFolder { get; private set; }

        public bool HasCredentials()
        {
            Trace.Info("HasCredentials()");
            bool credsStored = (new FileInfo(_credFilePath)).Exists || (new FileInfo(_migratedCredFilePath)).Exists;
            Trace.Info("stored {0}", credsStored);
            return credsStored;
        }

        public bool IsConfigured()
        {
            Trace.Info("IsConfigured()");
            bool configured = new FileInfo(_configFilePath).Exists;
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

        public CredentialData GetCredentials()
        {
            if (_creds == null)
            {
                _creds = IOUtil.LoadObject<CredentialData>(_credFilePath);
            }

            return _creds;
        }

        public CredentialData GetMigratedCredentials()
        {
            if (_migratedCreds == null && File.Exists(_migratedCredFilePath))
            {
                _migratedCreds = IOUtil.LoadObject<CredentialData>(_migratedCredFilePath);
            }

            return _migratedCreds;
        }

        public RunnerSettings GetSettings()
        {
            if (_settings == null)
            {
                RunnerSettings configuredSettings = null;
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath, Encoding.UTF8);
                    Trace.Info($"Read setting file: {json.Length} chars");
                    configuredSettings = StringUtil.ConvertFromJson<RunnerSettings>(json);
                }

                ArgUtil.NotNull(configuredSettings, nameof(configuredSettings));
                _settings = configuredSettings;
            }

            return _settings;
        }

        public void SaveCredential(CredentialData credential)
        {
            Trace.Info("Saving {0} credential @ {1}", credential.Scheme, _credFilePath);
            if (File.Exists(_credFilePath))
            {
                // Delete existing credential file first, since the file is hidden and not able to overwrite.
                Trace.Info("Delete exist runner credential file.");
                IOUtil.DeleteFile(_credFilePath);
            }

            IOUtil.SaveObject(credential, _credFilePath);
            Trace.Info("Credentials Saved.");
            File.SetAttributes(_credFilePath, File.GetAttributes(_credFilePath) | FileAttributes.Hidden);
        }

        public void SaveSettings(RunnerSettings settings)
        {
            Trace.Info("Saving runner settings.");
            if (File.Exists(_configFilePath))
            {
                // Delete existing runner settings file first, since the file is hidden and not able to overwrite.
                Trace.Info("Delete exist runner settings file.");
                IOUtil.DeleteFile(_configFilePath);
            }

            IOUtil.SaveObject(settings, _configFilePath);
            Trace.Info("Settings Saved.");
            File.SetAttributes(_configFilePath, File.GetAttributes(_configFilePath) | FileAttributes.Hidden);
        }

        public void DeleteCredential()
        {
            IOUtil.Delete(_credFilePath, default(CancellationToken));
            IOUtil.Delete(_migratedCredFilePath, default(CancellationToken));
        }

        public void DeleteMigratedCredential()
        {
            IOUtil.Delete(_migratedCredFilePath, default(CancellationToken));
        }

        public void DeleteSettings()
        {
            IOUtil.Delete(_configFilePath, default(CancellationToken));
        }
    }
}
