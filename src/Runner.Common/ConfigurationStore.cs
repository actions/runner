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
    }

    [DataContract]
    public sealed class RunnerRuntimeOptions
    {
#if OS_WINDOWS
        [DataMember(EmitDefaultValue = false)]
        public bool GitUseSecureChannel { get; set; }
#endif
    }

    [ServiceLocator(Default = typeof(ConfigurationStore))]
    public interface IConfigurationStore : IRunnerService
    {
        bool IsConfigured();
        bool IsServiceConfigured();
        bool HasCredentials();
        CredentialData GetCredentials();
        RunnerSettings GetSettings();
        void SaveCredential(CredentialData credential);
        void SaveSettings(RunnerSettings settings);
        void DeleteCredential();
        void DeleteSettings();
        RunnerRuntimeOptions GetRunnerRuntimeOptions();
        void SaveRunnerRuntimeOptions(RunnerRuntimeOptions options);
        void DeleteRunnerRuntimeOptions();
    }

    public sealed class ConfigurationStore : RunnerService, IConfigurationStore
    {
        private string _binPath;
        private string _configFilePath;
        private string _credFilePath;
        private string _serviceConfigFilePath;
        private string _runtimeOptionsFilePath;

        private CredentialData _creds;
        private RunnerSettings _settings;
        private RunnerRuntimeOptions _runtimeOptions;

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

            _serviceConfigFilePath = hostContext.GetConfigFile(WellKnownConfigFile.Service);
            Trace.Info("ServiceConfigFilePath: {0}", _serviceConfigFilePath);

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

        public CredentialData GetCredentials()
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            if (_creds == null)
            {
                _creds = IOUtil.LoadObject<CredentialData>(_credFilePath);
            }

            return _creds;
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
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
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
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
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
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            IOUtil.Delete(_credFilePath, default(CancellationToken));
        }

        public void DeleteSettings()
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            IOUtil.Delete(_configFilePath, default(CancellationToken));
        }

        public RunnerRuntimeOptions GetRunnerRuntimeOptions()
        {
            if (_runtimeOptions == null && File.Exists(_runtimeOptionsFilePath))
            {
                _runtimeOptions = IOUtil.LoadObject<RunnerRuntimeOptions>(_runtimeOptionsFilePath);
            }

            return _runtimeOptions;
        }

        public void SaveRunnerRuntimeOptions(RunnerRuntimeOptions options)
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

        public void DeleteRunnerRuntimeOptions()
        {
            IOUtil.Delete(_runtimeOptionsFilePath, default(CancellationToken));
        }
    }
}
