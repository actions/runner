using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager : IAgentService
    {
        bool IsConfigured();
        bool IsServiceConfigured();
        Task EnsureConfiguredAsync(CommandSettings command);
        Task ConfigureAsync(CommandSettings command);
        Task UnconfigureAsync(CommandSettings command);
        AgentSettings LoadSettings();
    }

    public sealed class ConfigurationManager : AgentService, IConfigurationManager
    {
        private IConfigurationStore _store;
        private IAgentServer _agentServer;
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            Trace.Verbose("Creating _store");
            _store = hostContext.GetService<IConfigurationStore>();
            Trace.Verbose("store created");
            _term = hostContext.GetService<ITerminal>();
        }

        public bool IsServiceConfigured()
        {
            bool result = _store.IsServiceConfigured();
            Trace.Info($"Is service configured: {result}");
            return result;
        }

        public bool IsConfigured()
        {
            bool result = _store.IsConfigured();
            Trace.Info($"Is configured: {result}");
            return result;
        }

        public async Task EnsureConfiguredAsync(CommandSettings command)
        {
            Trace.Info(nameof(EnsureConfiguredAsync));
            if (!IsConfigured())
            {
                await ConfigureAsync(command);
            }
        }

        public AgentSettings LoadSettings()
        {
            Trace.Info(nameof(LoadSettings));
            if (!IsConfigured())
            {
                throw new InvalidOperationException("Not configured");
            }

            AgentSettings settings = _store.GetSettings();
            Trace.Info("Settings Loaded");

            return settings;
        }

        public async Task ConfigureAsync(CommandSettings command)
        {
            Trace.Info(nameof(ConfigureAsync));
            if (IsConfigured())
            {
                throw new InvalidOperationException(StringUtil.Loc("AlreadyConfiguredError"));
            }

            // TEE EULA
            bool acceptTeeEula = false;
            switch (Constants.Agent.Platform)
            {
                case Constants.OSPlatform.OSX:
                case Constants.OSPlatform.Linux:
                    // Write the section header.
                    WriteSection(StringUtil.Loc("EulasSectionHeader"));

                    // Verify the EULA exists on disk in the expected location.
                    string eulaFile = Path.Combine(IOUtil.GetExternalsPath(), Constants.Path.TeeDirectory, "license.html");
                    ArgUtil.File(eulaFile, nameof(eulaFile));

                    // Write elaborate verbiage about the TEE EULA.
                    _term.WriteLine(StringUtil.Loc("TeeEula", eulaFile));
                    _term.WriteLine();

                    // Prompt to acccept the TEE EULA.
                    acceptTeeEula = command.GetAcceptTeeEula();
                    break;
                case Constants.OSPlatform.Windows:
                    break;
                default:
                    throw new NotSupportedException();
            }

            // TODO: Check if its running with elevated permission and stop early if its not

            // Loop getting url and creds until you can connect
            string serverUrl = null;
            ICredentialProvider credProvider = null;
            WriteSection(StringUtil.Loc("ConnectSectionHeader"));
            while (true)
            {
                // Get the URL
                serverUrl = command.GetUrl();

                // Get the credentials
                credProvider = GetCredentialProvider(command, serverUrl);
                VssCredentials creds = credProvider.GetVssCredentials(HostContext);
                Trace.Info("cred retrieved");
                try
                {
                    // Validate can connect.
                    await TestConnectAsync(serverUrl, creds);
                    Trace.Info("Connect complete.");
                    break;
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                    _term.WriteError(StringUtil.Loc("FailedToConnect"));
                    // TODO: If the connection fails, shouldn't the URL/creds be cleared from the command line parser? Otherwise retry may be immediately attempted using the same values without prompting the user for new values. The same general problem applies to every retry loop during configure.
                }
            }

            // We want to use the native CSP of the platform for storage, so we use the RSACSP directly
            RSAParameters publicKey;
            var keyManager = HostContext.GetService<IRSAKeyManager>();
            using (var rsa = keyManager.CreateKey())
            {
                publicKey = rsa.ExportParameters(false);
            }

            // Loop getting agent name and pool
            string poolName = null;
            int poolId = 0;
            string agentName = null;
            WriteSection(StringUtil.Loc("RegisterAgentSectionHeader"));
            while (true)
            {
                poolName = command.GetPool();
                try
                {
                    poolId = await GetPoolId(poolName);
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                }

                if (poolId > 0)
                {
                    break;
                }

                _term.WriteError(StringUtil.Loc("FailedToFindPool"));
            }

            TaskAgent agent;
            while (true)
            {
                agentName = command.GetAgent();

                // Get the system capabilities.
                // TODO: Hook up to ctrl+c cancellation token.
                // TODO: LOC
                _term.WriteLine("Scanning for tool capabilities.");
                Dictionary<string, string> systemCapabilities = await HostContext.GetService<ICapabilitiesManager>().GetCapabilitiesAsync(
                    new AgentSettings { AgentName = agentName }, CancellationToken.None);

                // TODO: LOC
                _term.WriteLine("Connecting to the server.");
                agent = await GetAgent(agentName, poolId);
                if (agent != null)
                {
                    if (command.GetReplace())
                    {
                        agent.Authorization = new TaskAgentAuthorization
                        {
                            PublicKey = new TaskAgentPublicKey(publicKey.Exponent, publicKey.Modulus),
                        };

                        // update - update instead of delete so we don't lose user capabilities etc...
                        agent.Version = Constants.Agent.Version;

                        foreach (KeyValuePair<string, string> capability in systemCapabilities)
                        {
                            agent.SystemCapabilities[capability.Key] = capability.Value ?? string.Empty;
                        }

                        try
                        {
                            agent = await _agentServer.UpdateAgentAsync(poolId, agent);
                            _term.WriteLine(StringUtil.Loc("AgentReplaced"));
                            break;
                        }
                        catch (Exception e) when (!command.Unattended)
                        {
                            _term.WriteError(e);
                            _term.WriteError(StringUtil.Loc("FailedToReplaceAgent"));
                        }
                    }
                    else
                    {
                        // TODO: ?
                    }
                }
                else
                {
                    agent = new TaskAgent(agentName)
                    {
                        Authorization = new TaskAgentAuthorization
                        {
                            PublicKey = new TaskAgentPublicKey(publicKey.Exponent, publicKey.Modulus),
                        },
                        MaxParallelism = 1,
                        Version = Constants.Agent.Version
                    };

                    foreach (KeyValuePair<string, string> capability in systemCapabilities)
                    {
                        agent.SystemCapabilities[capability.Key] = capability.Value ?? string.Empty;
                    }

                    try
                    {
                        agent = await _agentServer.AddAgentAsync(poolId, agent);
                        _term.WriteLine(StringUtil.Loc("AgentAddedSuccessfully"));
                        break;
                    }
                    catch (Exception e) when (!command.Unattended)
                    {
                        _term.WriteError(e);
                        _term.WriteError(StringUtil.Loc("AddAgentFailed"));
                    }
                }
            }

            // See if the server supports our OAuth key exchange for credentials
            if (agent.Authorization != null &&
                agent.Authorization.ClientId != Guid.Empty &&
                agent.Authorization.AuthorizationUrl != null)
            {
                var credentialData = new CredentialData
                {
                    Scheme = Constants.Configuration.OAuth,
                    Data =
                    {
                        { "clientId", agent.Authorization.ClientId.ToString("D") },
                        { "authorizationUrl", agent.Authorization.AuthorizationUrl.AbsoluteUri },
                    },
                };

                // Save the negotiated OAuth credential data
                _store.SaveCredential(credentialData);
            }
            else
            {
                // Save the provided admin credential data for compat with existing agent
                _store.SaveCredential(credProvider.CredentialData);
            }

            // We will Combine() what's stored with root.  Defaults to string a relative path
            string workFolder = command.GetWork();
            string notificationPipeName = command.GetNotificationPipeName();

            // Get Agent settings
            var settings = new AgentSettings
            {
                AcceptTeeEula = acceptTeeEula,
                AgentId = agent.Id,
                AgentName = agentName,
                NotificationPipeName = notificationPipeName,
                PoolId = poolId,
                PoolName = poolName,
                ServerUrl = serverUrl,
                WorkFolder = workFolder,
            };

            _store.SaveSettings(settings);
            _term.WriteLine(StringUtil.Loc("SavedSettings", DateTime.UtcNow));

            // chown/chmod the _diag and settings files to the current user, if we started with sudo.
            // Also if we started with sudo, the _diag will be owned by root. Change this to current login user
            if (Constants.Agent.Platform == Constants.OSPlatform.Linux ||
               Constants.Agent.Platform == Constants.OSPlatform.OSX)
            {
                string uidValue = Environment.GetEnvironmentVariable("SUDO_UID");
                string gidValue = Environment.GetEnvironmentVariable("SUDO_GID");

                if (!string.IsNullOrEmpty(uidValue) && !string.IsNullOrEmpty(gidValue))
                {
                    var filesToChange = new Dictionary<string, string>
                                                         {
                                                             { IOUtil.GetDiagPath(), "775" },
                                                             { IOUtil.GetConfigFilePath(), "770"},
                                                             { IOUtil.GetCredFilePath(), "770" },
                                                             { IOUtil.GetRSACredFilePath(), "770" },
                                                         };
                    var unixUtil = HostContext.CreateService<IUnixUtil>();
                    foreach (var file in filesToChange)
                    {
                        await unixUtil.ChownAsync(uidValue, gidValue, file.Key);
                        await unixUtil.ChmodAsync(file.Value, file.Key);
                    }
                }
            }

            bool runAsService = command.GetRunAsService();
            var serviceControlManager = HostContext.GetService<IServiceControlManager>();
            bool successfullyConfigured = false;
            if (runAsService)
            {
                Trace.Info("Configuring to run the agent as service");
                successfullyConfigured = serviceControlManager.ConfigureService(settings, command);
            }

            if (runAsService && successfullyConfigured)
            {
                Trace.Info("Configuration was successful, trying to start the service");
                serviceControlManager.StartService();
            }
        }

        public async Task UnconfigureAsync(CommandSettings command)
        {
            string currentAction = StringUtil.Loc("UninstallingService");
            try
            {
                //stop, uninstall service and remove service config file
                _term.WriteLine(currentAction);
                if (_store.IsServiceConfigured())
                {
                    var serviceControlManager = HostContext.GetService<IServiceControlManager>();
                    serviceControlManager.UnconfigureService();
                    _term.WriteLine(StringUtil.Loc("Success") + currentAction);
                }
                else
                {
                    _term.WriteLine(StringUtil.Loc("Skipping") + currentAction);
                }

                //delete agent from the server
                currentAction = StringUtil.Loc("UnregisteringAgent");
                _term.WriteLine(currentAction);
                bool isConfigured = _store.IsConfigured();
                bool hasCredentials = _store.HasCredentials();
                if (isConfigured && hasCredentials)
                {
                    AgentSettings settings = _store.GetSettings();
                    var credentialManager = HostContext.GetService<ICredentialManager>();

                    // Get the credentials
                    var credProvider = GetCredentialProvider(command, settings.ServerUrl);
                    VssCredentials creds = credProvider.GetVssCredentials(HostContext);
                    Trace.Info("cred retrieved");

                    Uri uri = new Uri(settings.ServerUrl);
                    VssConnection conn = ApiUtil.CreateConnection(uri, creds);
                    var agentSvr = HostContext.GetService<IAgentServer>();
                    await agentSvr.ConnectAsync(conn);
                    Trace.Info("Connect complete.");

                    List<TaskAgent> agents = await agentSvr.GetAgentsAsync(settings.PoolId, settings.AgentName);
                    if (agents.Count == 0)
                    {
                        _term.WriteLine(StringUtil.Loc("Skipping") + currentAction);
                    }
                    else
                    {
                        await agentSvr.DeleteAgentAsync(settings.PoolId, settings.AgentId);
                        _term.WriteLine(StringUtil.Loc("Success") + currentAction);
                    }
                }
                else
                {
                    _term.WriteLine(StringUtil.Loc("MissingConfig"));
                }

                //delete credential config files               
                currentAction = StringUtil.Loc("DeletingCredentials");
                _term.WriteLine(currentAction);
                if (hasCredentials)
                {
                    _store.DeleteCredential();
                    var keyManager = HostContext.GetService<IRSAKeyManager>();
                    keyManager.DeleteKey();                    
                    _term.WriteLine(StringUtil.Loc("Success") + currentAction);
                }
                else
                {
                    _term.WriteLine(StringUtil.Loc("Skipping") + currentAction);
                }

                //delete settings config file                
                currentAction = StringUtil.Loc("DeletingSettings");
                _term.WriteLine(currentAction);
                if (isConfigured)
                {
                    _store.DeleteSettings();
                    _term.WriteLine(StringUtil.Loc("Success") + currentAction);
                }
                else
                {
                    _term.WriteLine(StringUtil.Loc("Skipping") + currentAction);
                }
            }
            catch (Exception)
            {
                _term.WriteLine(StringUtil.Loc("Failed") + currentAction);
                throw;
            }
        }

        private ICredentialProvider GetCredentialProvider(CommandSettings command, string serverUrl)
        {
            Trace.Info(nameof(GetCredentialProvider));

            var credentialManager = HostContext.GetService<ICredentialManager>();
            // Get the auth type. On premise defaults to negotiate (Kerberos with fallback to NTLM).
            // Hosted defaults to PAT authentication.
            bool isHosted = serverUrl.IndexOf("visualstudio.com", StringComparison.OrdinalIgnoreCase) != -1
                || serverUrl.IndexOf("tfsallin.net", StringComparison.OrdinalIgnoreCase) != -1;
            string defaultAuth = isHosted ? Constants.Configuration.PAT :
                (Constants.Agent.Platform == Constants.OSPlatform.Windows ? Constants.Configuration.Integrated : Constants.Configuration.Negotiate);
            string authType = command.GetAuth(defaultValue: defaultAuth);

            // Create the credential.
            Trace.Info("Creating credential for auth: {0}", authType);
            var provider = credentialManager.GetCredentialProvider(authType);
            provider.EnsureCredential(HostContext, command, serverUrl);
            return provider;
        }

        private async Task TestConnectAsync(string url, VssCredentials creds)
        {
            _term.WriteLine(StringUtil.Loc("ConnectingToServer"));
            VssConnection connection = ApiUtil.CreateConnection(new Uri(url), creds);

            _agentServer = HostContext.CreateService<IAgentServer>();
            await _agentServer.ConnectAsync(connection);
        }

        private async Task<int> GetPoolId(string poolName)
        {
            int id = 0;
            List<TaskAgentPool> pools = await _agentServer.GetAgentPoolsAsync(poolName);
            Trace.Verbose("Returned {0} pools", pools.Count);

            if (pools.Count == 1)
            {
                id = pools[0].Id;
                Trace.Info("Found pool {0} with id {1}", poolName, id);
            }

            return id;
        }

        private async Task<TaskAgent> GetAgent(string name, int poolId)
        {
            List<TaskAgent> agents = await _agentServer.GetAgentsAsync(poolId, name);
            Trace.Verbose("Returns {0} agents", agents.Count);
            TaskAgent agent = agents.FirstOrDefault();

            return agent;
        }

        private void WriteSection(string message)
        {
            _term.WriteLine();
            _term.WriteLine($">> {message}:");
            _term.WriteLine();
        }
    }
}
