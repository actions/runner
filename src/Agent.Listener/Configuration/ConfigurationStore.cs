using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    //
    // Settings are persisted in this structure
    //
    public sealed class AgentSettings
    {
        public Int32 AgentId { get; set; }
        public String AgentName { get; set; }
        public Int32 PoolId { get; set; }
        public String PoolName { get; set; }
        public string ServerUrl { get; set; }
        public String WorkFolder { get; set; }
    }

    [ServiceLocator(Default = typeof(ConfigurationStore))]
    public interface IConfigurationStore: IAgentService
    {
        string RootFolder { get; }
        bool IsConfigured();
        bool HasCredentials();
        CredentialData GetCredentials();
        AgentSettings GetSettings();
        void SaveCredential(CredentialData credential);
        void SaveSettings(AgentSettings settings);
    }

    public class ConfigurationStore : AgentService, IConfigurationStore
    {
        private string _binPath;
        private string _configFilePath;
        private string _credFilePath;
        private CredentialData _creds;
        private AgentSettings _settings;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            Trace.Info("Initialize()");

            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            Trace.Info("currentAssemblyLocation: {0}", currentAssemblyLocation);

            var _binPath = new DirectoryInfo(currentAssemblyLocation).Parent.FullName.ToString();
            Trace.Info("binPath: {0}", _binPath);

            RootFolder = new DirectoryInfo(_binPath).Parent.FullName.ToString();
            Trace.Info("RootFolder: {0}", RootFolder);

            _configFilePath = Path.Combine(RootFolder, ".Agent");
            Trace.Info("ConfigFilePath: {0}", _configFilePath);

            _credFilePath = Path.Combine(RootFolder, ".Credentials");
            Trace.Info("CredFilePath: {0}", _credFilePath);
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

        public CredentialData GetCredentials()
        {
            if (_creds == null)
            {
                _creds = Load<CredentialData>(_credFilePath);
            }

            return _creds;
        }

        public AgentSettings GetSettings()
        {
            if (_settings == null)
            {
                _settings = Load<AgentSettings>(_configFilePath);    
            }
            
            return _settings;
        }

        public void SaveCredential(CredentialData credential)
        {   
            Trace.Info("Saving {0} credential @ {1}", credential.Scheme, _credFilePath);
            Save(credential, _credFilePath);
            Trace.Info("Saved.");
        }

        public void SaveSettings(AgentSettings settings)
        {
            Save(settings, _configFilePath);
        }

        private void Save(Object obj, string path) 
        {
            Trace.Info("Saving to {0}", path);

            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText (path, json);
            Trace.Info("Written.");
        }

        private T Load<T>(string path)
        {
            // TODO: convert many of these Info statements to Verbose

            Trace.Info("Loading config from {0}", path);

            string json = File.ReadAllText(path);
            Trace.Info("Loaded. Length: {0}", json.Length);
            T config = JsonConvert.DeserializeObject<T>(json);
            Trace.Info("Loaded.");
            return config;
        }
    }
}