using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent
{
    //
    // Settings are persisted in this structure
    //
    public sealed class AgentSettings
    {
        public bool AcceptTeeEula { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; }
        public int PoolId { get; set; }
        public string PoolName { get; set; }
        public string ServerUrl { get; set; }
        public string WorkFolder { get; set; }
        public bool RunAsService { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDisplayName { get; set; }
    }

    [ServiceLocator(Default = typeof(ConfigurationStore))]
    public interface IConfigurationStore : IAgentService
    {
        string RootFolder { get; }
        bool IsConfigured();
        bool HasCredentials();
        CredentialData GetCredentials();
        AgentSettings GetSettings();
        void SaveCredential(CredentialData credential);
        void SaveSettings(AgentSettings settings);
    }

    public sealed class ConfigurationStore : AgentService, IConfigurationStore
    {
        private string _binPath;
        private string _configFilePath;
        private string _credFilePath;
        private CredentialData _creds;
        private AgentSettings _settings;

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

        public void SaveCredential(CredentialData credential)
        {   
            Trace.Info("Saving {0} credential @ {1}", credential.Scheme, _credFilePath);
            IOUtil.SaveObject(credential, _credFilePath);
            Trace.Info("Credentials Saved.");
        }

        public void SaveSettings(AgentSettings settings)
        {
            IOUtil.SaveObject(settings, _configFilePath);
            Trace.Info("Settings Saved.");
        }
    }
}