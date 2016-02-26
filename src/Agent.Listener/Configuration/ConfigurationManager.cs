using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    public static class CliArgs
    {
        public const string Auth = "auth";
        public const string Url = "url";
        public const string Pool = "pool";
        public const string Agent = "agent";
        public const string Replace = "replace";
        public const string Work = "work";
    }

    // TODO: does initialize make sense for service locator pattern?
    // should it be ensureInitialized?  Singleton?
    //
    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager: IAgentService
    {
        bool IsConfigured();
        void EnsureConfigured();
        void Configure(Dictionary<String, String> args, Boolean unattend);
        ICredentialProvider AcquireCredentials(Dictionary<String, String> args, bool enforceSupplied);
        AgentSettings GetSettings();
    }

    public sealed class ConfigurationManager : AgentService, IConfigurationManager
    {
        private IConfigurationStore _store;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            Trace.Verbose("Creating _store");
            _store = hostContext.GetService<IConfigurationStore>(); 
            Trace.Verbose("store created");
        }

        public bool IsConfigured()
        {
            return _store.IsConfigured();
        }

        public AgentSettings GetSettings()
        {
            return _store.GetSettings();
        }

        public void EnsureConfigured()
        {
            Trace.Info("EnsureConfigured()");

            bool configured = _store.IsConfigured();
            
            Trace.Info("configured? {0}", configured);

            if (!configured) 
            {
                Configure(null, false);
            }
        }

        public ICredentialProvider AcquireCredentials(Dictionary<String, String> args, bool enforceSupplied)
        {
            Trace.Info("AcquireCredentials()");

            var credentialManager = HostContext.GetService<ICredentialManager>();
            ICredentialProvider cred = null;

            if (_store.HasCredentials())
            {
                CredentialData data = _store.GetCredentials();
                cred = credentialManager.GetCredentialProvider(data.Scheme);
                cred.CredentialData = data;
            }
            else
            {
                // get from user
                var consoleWizard = HostContext.GetService<IConsoleWizard>();
                string authType = consoleWizard.ReadValue(CliArgs.Auth,
                                                        "Authentication Type", 
                                                        false,
                                                        "PAT", 
                                                        Validators.AuthSchemeValidator,
                                                        args, 
                                                        enforceSupplied);
                Trace.Info("AuthType: {0}", authType);

                Trace.Verbose("Creating Credential: {0}", authType);
                cred = credentialManager.GetCredentialProvider(authType);
                cred.ReadCredential(HostContext, args, enforceSupplied);

                Trace.Verbose("Saving credential");
                _store.SaveCredential(cred.CredentialData);
            }

            return cred;
        }

        public void Configure(Dictionary<String, String> args, bool enforceSupplied)
        {
            Trace.Info("Configure()");
            if (IsConfigured()) 
            {
                throw new InvalidOperationException("Cannot configure.  Already configured.");
            }

            Trace.Info("Read agent settings");
            var consoleWizard = HostContext.GetService<IConsoleWizard>();

            //
            // Loop getting url and creds until you can connect
            //
            string serverUrl = null;
            ICredentialProvider cred = null;
            while (true)
            {
                WriteSection("Connect");
                serverUrl = consoleWizard.ReadValue(CliArgs.Url,
                                                "Server URL", 
                                                false,
                                                String.Empty,
                                                Validators.ServerUrlValidator,
                                                args, 
                                                enforceSupplied);
                Trace.Info("serverUrl: {0}", serverUrl);

                cred = AcquireCredentials(args, enforceSupplied);
                Trace.Info("cred retrieved");

                // we don't want to loop on unattend
                if (enforceSupplied || TestConnect(serverUrl))
                {
                    break;
                }
            }
            Trace.Info("Connect Complete.");

            //
            // Loop getting agent name and pool
            //
            string poolName = null;
            int poolId = 0;
            string agentName = null;
            while (true)
            {
                WriteSection("Register Agent");

                while(true)
                {
                    poolName = consoleWizard.ReadValue(CliArgs.Pool,
                                                    "Pool Name", 
                                                    false,
                                                    "default",
                                                    // can do better
                                                    Validators.NonEmptyValidator, 
                                                    args, 
                                                    enforceSupplied);

                    poolId = GetPoolId(poolName);
                    if (enforceSupplied || poolId > 0)
                    {
                        break;
                    }
                }

                while(true)
                {
                    agentName = consoleWizard.ReadValue(CliArgs.Agent,
                                                    "Agent Name", 
                                                    false,
                                                    // TODO: coreCLR doesn't expose till very recently (Jan 15)
                                                    // Environment.MachineName,
                                                    "myagent",
                                                    // can do better
                                                    Validators.NonEmptyValidator, 
                                                    args, 
                                                    enforceSupplied);

                    bool exists = AgentExists(agentName, poolId);
                    bool replace = false;
                    if (exists) 
                    {
                        replace = consoleWizard.ReadBool(CliArgs.Replace,
                                                    "Replace? (Y/N)",
                                                    false,
                                                    args, 
                                                    enforceSupplied);
                        if (replace)
                        {
                            // best effort, will fail and registration and loop on failure
                            DeleteAgent(agentName, poolId);
                            exists = false;
                        }
                    }

                    if (enforceSupplied || !exists)
                    {
                        break;
                    }                    
                }

                if (enforceSupplied || RegisterAgent(agentName, poolId))
                {
                    break;
                }                             
            }

            // We will Combine() what's stored with root.  Defaults to string a relative path
            string workFolder = consoleWizard.ReadValue(CliArgs.Work,
                                                    "Work Folder", 
                                                    false,
                                                    "_work",
                                                    // can do better
                                                    Validators.NonEmptyValidator, 
                                                    args, 
                                                    enforceSupplied);
            // Get Agent settings
            var settings = new AgentSettings
                     {
                         ServerUrl = serverUrl,
                         AgentName = agentName,
                         PoolName = poolName,
                         PoolId = poolId,
                         WorkFolder = workFolder
                     };
            
            _store.SaveSettings(settings);

            // TODO connect to server if suceeds save the config
        }

        private bool TestConnect(string url)
        {
            // TODO: test with connect call
            Console.WriteLine("Connecting to server ...");

            // TODO: Communicate failure, hopefully with good actionable error message
            return true;
        }

        private int GetPoolId(string poolName)
        {
            // TODO: Communicate failure and return 0, hopefully with good actionable error message
            return 1;
        }

        private bool AgentExists(string name, int poolId)
        {
            return false;
        }

        private bool DeleteAgent(string name, int poolId)
        {
            // TODO: Communicate failure and return 0, hopefully with good actionable error message
            return true;
        }

        private bool RegisterAgent(string name, int poolId)
        {
            // TODO: Communicate failure and return 0, hopefully with good actionable error message
            return true;
        }

        private void WriteSection(string message)
        {
            Console.WriteLine();
            Console.WriteLine(">> {0}:", message);
            Console.WriteLine();
        }        
    }
}