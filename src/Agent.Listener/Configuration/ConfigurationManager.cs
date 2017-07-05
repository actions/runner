using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager : IAgentService
    {
        bool IsConfigured();
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

        public bool IsConfigured()
        {
            bool result = _store.IsConfigured();
            Trace.Info($"Is configured: {result}");
            return result;
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

            AgentSettings agentSettings = new AgentSettings();
            // TEE EULA
            agentSettings.AcceptTeeEula = false;
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
                    agentSettings.AcceptTeeEula = command.GetAcceptTeeEula();
                    break;
                case Constants.OSPlatform.Windows:
                    // Warn and continue if .NET 4.6 is not installed.
                    var netFrameworkUtil = HostContext.GetService<INetFrameworkUtil>();
                    if (!netFrameworkUtil.Test(new Version(4, 6)))
                    {
                        WriteSection(StringUtil.Loc("PrerequisitesSectionHeader")); // Section header.
                        _term.WriteLine(StringUtil.Loc("MinimumNetFrameworkTfvc")); // Warning.
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }

            // Create the configuration provider as per agent type.
            string agentType = command.DeploymentGroup
                ? Constants.Agent.AgentConfigurationProvider.DeploymentAgentConfiguration
                : Constants.Agent.AgentConfigurationProvider.BuildReleasesAgentConfiguration;
            var extensionManager = HostContext.GetService<IExtensionManager>();
            IConfigurationProvider agentProvider =
                (extensionManager.GetExtensions<IConfigurationProvider>())
                .FirstOrDefault(x => x.ConfigurationProviderType == agentType);
            ArgUtil.NotNull(agentProvider, agentType);

            // TODO: Check if its running with elevated permission and stop early if its not

            // Loop getting url and creds until you can connect
            ICredentialProvider credProvider = null;
            VssCredentials creds = null;
            WriteSection(StringUtil.Loc("ConnectSectionHeader"));
            while (true)
            {
                // Get the URL
                agentProvider.GetServerUrl(agentSettings, command);

                // Get the credentials
                credProvider = GetCredentialProvider(command, agentSettings.ServerUrl);
                creds = credProvider.GetVssCredentials(HostContext);
                Trace.Info("cred retrieved");
                try
                {
                    // Validate can connect.
                    await agentProvider.TestConnectionAsync(agentSettings, creds);
                    Trace.Info("Test Connection complete.");
                    break;
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                    _term.WriteError(StringUtil.Loc("FailedToConnect"));
                }
            }

            _agentServer = HostContext.GetService<IAgentServer>();
            // We want to use the native CSP of the platform for storage, so we use the RSACSP directly
            RSAParameters publicKey;
            var keyManager = HostContext.GetService<IRSAKeyManager>();
            using (var rsa = keyManager.CreateKey())
            {
                publicKey = rsa.ExportParameters(false);
            }

            // Loop getting agent name and pool name
            WriteSection(StringUtil.Loc("RegisterAgentSectionHeader"));

            while (true)
            {
                try
                {
                    await agentProvider.GetPoolId(agentSettings, command);
                    break;
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                    _term.WriteError(agentProvider.GetFailedToFindPoolErrorString());
                }
            }

            TaskAgent agent;
            while (true)
            {
                agentSettings.AgentName = command.GetAgentName();

                // Get the system capabilities.
                // TODO: Hook up to ctrl+c cancellation token.
                _term.WriteLine(StringUtil.Loc("ScanToolCapabilities"));
                Dictionary<string, string> systemCapabilities = await HostContext.GetService<ICapabilitiesManager>().GetCapabilitiesAsync(agentSettings, CancellationToken.None);

                _term.WriteLine(StringUtil.Loc("ConnectToServer"));
                agent = await agentProvider.GetAgentAsync(agentSettings);
                if (agent != null)
                {
                    if (command.GetReplace())
                    {
                        // Update existing agent with new PublicKey, agent version and SystemCapabilities.
                        agent = UpdateExistingAgent(agent, publicKey, systemCapabilities);

                        try
                        {
                            agent = await agentProvider.UpdateAgentAsync(agentSettings, agent, command);
                            _term.WriteLine(StringUtil.Loc("AgentReplaced"));
                            break;
                        }
                        catch (Exception e) when (!command.Unattended)
                        {
                            _term.WriteError(e);
                            _term.WriteError(StringUtil.Loc("FailedToReplaceAgent"));
                        }
                    }
                    else if (command.Unattended)
                    {
                        // if not replace and it is unattended config.
                        agentProvider.ThrowTaskAgentExistException(agentSettings);
                    }
                }
                else
                {
                    // Create a new agent. 
                    agent = CreateNewAgent(agentSettings.AgentName, publicKey, systemCapabilities);

                    try
                    {
                        agent = await agentProvider.AddAgentAsync(agentSettings, agent, command);
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
            // Add Agent Id to settings
            agentSettings.AgentId = agent.Id;

            // respect the serverUrl resolve by server.
            // in case of agent configured using collection url instead of account url.
            string agentServerUrl;
            if (agent.Properties.TryGetValidatedValue<string>("ServerUrl", out agentServerUrl) &&
                !string.IsNullOrEmpty(agentServerUrl))
            {
                Trace.Info($"Agent server url resolve by server: '{agentServerUrl}'.");

                // we need make sure the Host component of the url remain the same.
                UriBuilder inputServerUrl = new UriBuilder(agentSettings.ServerUrl);
                UriBuilder serverReturnedServerUrl = new UriBuilder(agentServerUrl);
                if (Uri.Compare(inputServerUrl.Uri, serverReturnedServerUrl.Uri, UriComponents.Host, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    inputServerUrl.Path = serverReturnedServerUrl.Path;
                    Trace.Info($"Replace server returned url's host component with user input server url's host: '{inputServerUrl.Uri.AbsoluteUri}'.");
                    agentSettings.ServerUrl = inputServerUrl.Uri.AbsoluteUri;
                }
                else
                {
                    agentSettings.ServerUrl = agentServerUrl;
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
                switch (Constants.Agent.Platform)
                {
                    case Constants.OSPlatform.OSX:
                    case Constants.OSPlatform.Linux:
                        // Save the provided admin cred for compat with previous agent.
                        _store.SaveCredential(credProvider.CredentialData);
                        break;
                    case Constants.OSPlatform.Windows:
                        // Not supported against TFS 2015.
                        _term.WriteError(StringUtil.Loc("Tfs2015NotSupported"));
                        return;
                    default:
                        throw new NotSupportedException();
                }
            }

            // Testing agent connection, detect any protential connection issue, like local clock skew that cause OAuth token expired.
            _term.WriteLine(StringUtil.Loc("TestAgentConnection"));
            var credMgr = HostContext.GetService<ICredentialManager>();
            VssCredentials credential = credMgr.LoadCredentials();
            VssConnection conn = ApiUtil.CreateConnection(new Uri(agentSettings.ServerUrl), credential);
            var agentSvr = HostContext.GetService<IAgentServer>();
            try
            {
                await agentSvr.ConnectAsync(conn);
            }
            catch (VssOAuthTokenRequestException ex) when (ex.Message.Contains("Current server time is"))
            {
                // there are two exception messages server send that indicate clock skew.
                // 1. The bearer token expired on {jwt.ValidTo}. Current server time is {DateTime.UtcNow}.
                // 2. The bearer token is not valid until {jwt.ValidFrom}. Current server time is {DateTime.UtcNow}.                
                Trace.Error("Catch exception during test agent connection.");
                Trace.Error(ex);
                throw new Exception(StringUtil.Loc("LocalClockSkewed"));
            }

            // We will Combine() what's stored with root.  Defaults to string a relative path
            agentSettings.WorkFolder = command.GetWork();

            // notificationPipeName for Hosted agent provisioner.
            agentSettings.NotificationPipeName = command.GetNotificationPipeName();

            agentSettings.NotificationSocketAddress = command.GetNotificationSocketAddress();

            _store.SaveSettings(agentSettings);
            _term.WriteLine(StringUtil.Loc("SavedSettings", DateTime.UtcNow));

#if OS_WINDOWS
            // config windows service as part of configuration
            bool runAsService = command.GetRunAsService();
            if (runAsService)
            {
                Trace.Info("Configuring to run the agent as service");
                var serviceControlManager = HostContext.GetService<IWindowsServiceControlManager>();
                serviceControlManager.ConfigureService(agentSettings, command);
            }
            //This will be enabled with AutoLogon code changes are tested
            else if (command.GetEnableAutoLogon())
            {                
                Trace.Info("Agent is going to run as process setting up the 'AutoLogon' capability for the agent.");
                var autoLogonConfigManager = HostContext.GetService<IAutoLogonManager>();
                await autoLogonConfigManager.ConfigureAsync(command);
                //Important: The machine may restart if the autologon user is not same as the current user
                //if you are adding code after this, keep that in mind
            }

#elif OS_LINUX || OS_OSX
            // generate service config script for OSX and Linux, GenerateScripts() will no-opt on windows.
            var serviceControlManager = HostContext.GetService<ILinuxServiceControlManager>();
            serviceControlManager.GenerateScripts(agentSettings);
#endif
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
#if OS_WINDOWS
                    var serviceControlManager = HostContext.GetService<IWindowsServiceControlManager>();
                    serviceControlManager.UnconfigureService();
                    _term.WriteLine(StringUtil.Loc("Success") + currentAction);
#elif OS_LINUX
                    // unconfig system D service first
                    throw new Exception(StringUtil.Loc("UnconfigureServiceDService"));
#elif OS_OSX
                    // unconfig osx service first
                    throw new Exception(StringUtil.Loc("UnconfigureOSXService"));
#endif
                }
                else
                {
#if OS_WINDOWS
                    //running as process, unconfigure autologon if it was configured                    
                    if (_store.IsAutoLogonConfigured())
                    {
                        var autoLogonConfigManager = HostContext.GetService<IAutoLogonManager>();
                        autoLogonConfigManager.Unconfigure();                        
                    }
                    else
                    {
                        Trace.Info("AutoLogon was not configured on the agent.");
                    }
#endif
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

                    bool isDeploymentGroup = (settings.MachineGroupId > 0) || (settings.DeploymentGroupId > 0);

                    Trace.Info("Agent configured for deploymentGroup : {0}", isDeploymentGroup.ToString());

                    string agentType = isDeploymentGroup
                   ? Constants.Agent.AgentConfigurationProvider.DeploymentAgentConfiguration
                   : Constants.Agent.AgentConfigurationProvider.BuildReleasesAgentConfiguration;

                    var extensionManager = HostContext.GetService<IExtensionManager>();
                    IConfigurationProvider agentProvider = (extensionManager.GetExtensions<IConfigurationProvider>()).FirstOrDefault(x => x.ConfigurationProviderType == agentType);
                    ArgUtil.NotNull(agentProvider, agentType);

                    await agentProvider.TestConnectionAsync(settings, creds);

                    TaskAgent agent = await agentProvider.GetAgentAsync(settings);
                    if (agent == null)
                    {
                        _term.WriteLine(StringUtil.Loc("Skipping") + currentAction);
                    }
                    else
                    {
                        await agentProvider.DeleteAgentAsync(settings);
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
            string defaultAuth = UrlUtil.IsHosted(serverUrl) ? Constants.Configuration.PAT :
                (Constants.Agent.Platform == Constants.OSPlatform.Windows ? Constants.Configuration.Integrated : Constants.Configuration.Negotiate);
            string authType = command.GetAuth(defaultValue: defaultAuth);

            // Create the credential.
            Trace.Info("Creating credential for auth: {0}", authType);
            var provider = credentialManager.GetCredentialProvider(authType);
            provider.EnsureCredential(HostContext, command, serverUrl);
            return provider;
        }

        private TaskAgent UpdateExistingAgent(TaskAgent agent, RSAParameters publicKey, Dictionary<string, string> systemCapabilities)
        {
            ArgUtil.NotNull(agent, nameof(agent));
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

            return agent;
        }

        private TaskAgent CreateNewAgent(string agentName, RSAParameters publicKey, Dictionary<string, string> systemCapabilities)
        {
            TaskAgent agent = new TaskAgent(agentName)
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