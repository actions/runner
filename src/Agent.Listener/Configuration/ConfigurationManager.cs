using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        Task EnsureConfiguredAsync();
        Task ConfigureAsync(Dictionary<String, String> args, Boolean unattend);
        ICredentialProvider AcquireCredentials(Dictionary<String, String> args, bool enforceSupplied);
        AgentSettings LoadSettings();
    }

    public sealed class ConfigurationManager : AgentService, IConfigurationManager
    {
        private IConfigurationStore _store;
        private ITaskServer _server;

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

        public async Task EnsureConfiguredAsync()
        {
            Trace.Info("EnsureConfigured()");

            bool configured = _store.IsConfigured();
            
            Trace.Info("configured? {0}", configured);

            if (!configured) 
            {
                await ConfigureAsync(null, false);
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
            }

            return cred;
        }

        public AgentSettings LoadSettings()
        {
            Trace.Info("LoadSettings()");
            if (!IsConfigured()) 
            {
                throw new InvalidOperationException("Not configured");
            }
            
            AgentSettings settings = _store.GetSettings();
            Trace.Info("Settings Loaded");
            
            return settings;
        }
        
        public async Task ConfigureAsync(Dictionary<String, String> args, bool enforceSupplied)
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
            ICredentialProvider credProv = null;
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

                credProv = AcquireCredentials(args, enforceSupplied);
                VssCredentials creds = credProv.GetVssCredentials(HostContext);
                
                Trace.Info("cred retrieved");

                bool connected = true;
                try
                {
                    await TestConnectAsync(serverUrl, creds);
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.WriteLine("Failed to connect.  Try again or ctrl-c to quit");
                    connected = false;
                }

                // we don't want to loop on unattend
                if (enforceSupplied || connected)
                {
                    break;
                }
            }
            
            // TODO: Create console agent service so we can hide in testing etc... and trace
            Console.WriteLine("Saving credentials...");
            Trace.Verbose("Saving credential");
            _store.SaveCredential(credProv.CredentialData);
            
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

        private async Task TestConnectAsync(string url, VssCredentials creds)
        {
            Console.WriteLine("Connecting to server ...");
            VssConnection connection = ApiUtil.CreateConnection(new Uri(url), creds);
            
            //connection.GetClient<TaskAgentHttpClient>();
            
            //await connection.ConnectAsync();
            _server = HostContext.CreateService<ITaskServer>();
            _server.SetConnection(connection);
            await _server.ConnectAsync();
            
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