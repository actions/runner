using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Capabilities;
using GitHub.Runner.Common.Util;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace GitHub.Runner.Listener.Configuration
{
    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager : IRunnerService
    {
        bool IsConfigured();
        Task ConfigureAsync(CommandSettings command);
        Task UnconfigureAsync(CommandSettings command);
        RunnerSettings LoadSettings();
    }

    public sealed class ConfigurationManager : RunnerService, IConfigurationManager
    {
        private IConfigurationStore _store;
        private IRunnerServer _runnerServer;
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _runnerServer = HostContext.GetService<IRunnerServer>();
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

        public RunnerSettings LoadSettings()
        {
            Trace.Info(nameof(LoadSettings));
            if (!IsConfigured())
            {
                throw new InvalidOperationException("Not configured");
            }

            RunnerSettings settings = _store.GetSettings();
            Trace.Info("Settings Loaded");

            return settings;
        }

        public async Task ConfigureAsync(CommandSettings command)
        {
            _term.WriteLine();
            _term.WriteLine("--------------------------------------------------------------------------------", ConsoleColor.White);
            _term.WriteLine("|        ____ _ _   _   _       _          _        _   _                      |", ConsoleColor.White);
            _term.WriteLine("|       / ___(_) |_| | | |_   _| |__      / \\   ___| |_(_) ___  _ __  ___      |", ConsoleColor.White);
            _term.WriteLine("|      | |  _| | __| |_| | | | | '_ \\    / _ \\ / __| __| |/ _ \\| '_ \\/ __|     |", ConsoleColor.White);
            _term.WriteLine("|      | |_| | | |_|  _  | |_| | |_) |  / ___ \\ (__| |_| | (_) | | | \\__ \\     |", ConsoleColor.White);
            _term.WriteLine("|       \\____|_|\\__|_| |_|\\__,_|_.__/  /_/   \\_\\___|\\__|_|\\___/|_| |_|___/     |", ConsoleColor.White);
            _term.WriteLine("|                                                                              |", ConsoleColor.White);
            _term.Write("|                       ", ConsoleColor.White);
            _term.Write("Self-hosted runner registration", ConsoleColor.Cyan);
            _term.WriteLine("                        |", ConsoleColor.White);
            _term.WriteLine("|                                                                              |", ConsoleColor.White);
            _term.WriteLine("--------------------------------------------------------------------------------", ConsoleColor.White);

            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            Trace.Info(nameof(ConfigureAsync));
            if (IsConfigured())
            {
                throw new InvalidOperationException("Cannot configure the runner because it is already configured. To reconfigure the runner, run 'config.cmd remove' or './config.sh remove' first.");
            }

            // Populate proxy setting from commandline args
            var runnerProxy = HostContext.GetService<IRunnerWebProxy>();
            bool saveProxySetting = false;
            string proxyUrl = command.GetProxyUrl();
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                if (!Uri.IsWellFormedUriString(proxyUrl, UriKind.Absolute))
                {
                    throw new ArgumentOutOfRangeException(nameof(proxyUrl));
                }

                Trace.Info("Reset proxy base on commandline args.");
                string proxyUserName = command.GetProxyUserName();
                string proxyPassword = command.GetProxyPassword();
                (runnerProxy as RunnerWebProxy).SetupProxy(proxyUrl, proxyUserName, proxyPassword);
                saveProxySetting = true;
            }

            // Populate cert setting from commandline args
            var runnerCertManager = HostContext.GetService<IRunnerCertificateManager>();
            bool saveCertSetting = false;
            bool skipCertValidation = command.GetSkipCertificateValidation();
            string caCert = command.GetCACertificate();
            string clientCert = command.GetClientCertificate();
            string clientCertKey = command.GetClientCertificatePrivateKey();
            string clientCertArchive = command.GetClientCertificateArchrive();
            string clientCertPassword = command.GetClientCertificatePassword();

            // We require all Certificate files are under agent root.
            // So we can set ACL correctly when configure as service
            if (!string.IsNullOrEmpty(caCert))
            {
                caCert = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), caCert);
                ArgUtil.File(caCert, nameof(caCert));
            }

            if (!string.IsNullOrEmpty(clientCert) &&
                !string.IsNullOrEmpty(clientCertKey) &&
                !string.IsNullOrEmpty(clientCertArchive))
            {
                // Ensure all client cert pieces are there.
                clientCert = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), clientCert);
                clientCertKey = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), clientCertKey);
                clientCertArchive = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), clientCertArchive);

                ArgUtil.File(clientCert, nameof(clientCert));
                ArgUtil.File(clientCertKey, nameof(clientCertKey));
                ArgUtil.File(clientCertArchive, nameof(clientCertArchive));
            }
            else if (!string.IsNullOrEmpty(clientCert) ||
                     !string.IsNullOrEmpty(clientCertKey) ||
                     !string.IsNullOrEmpty(clientCertArchive))
            {
                // Print out which args are missing.
                ArgUtil.NotNullOrEmpty(Constants.Runner.CommandLine.Args.SslClientCert, Constants.Runner.CommandLine.Args.SslClientCert);
                ArgUtil.NotNullOrEmpty(Constants.Runner.CommandLine.Args.SslClientCertKey, Constants.Runner.CommandLine.Args.SslClientCertKey);
                ArgUtil.NotNullOrEmpty(Constants.Runner.CommandLine.Args.SslClientCertArchive, Constants.Runner.CommandLine.Args.SslClientCertArchive);
            }

            if (skipCertValidation || !string.IsNullOrEmpty(caCert) || !string.IsNullOrEmpty(clientCert))
            {
                Trace.Info("Reset runner cert setting base on commandline args.");
                (runnerCertManager as RunnerCertificateManager).SetupCertificate(skipCertValidation, caCert, clientCert, clientCertKey, clientCertArchive, clientCertPassword);
                saveCertSetting = true;
            }

            RunnerSettings runnerSettings = new RunnerSettings();

            bool isHostedServer = false;
            // Loop getting url and creds until you can connect
            ICredentialProvider credProvider = null;
            VssCredentials creds = null;
            _term.WriteSection("Authentication");
            while (true)
            {
                // Get the URL
                var inputUrl = command.GetUrl();
                if (!inputUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                {
                    runnerSettings.ServerUrl = inputUrl;
                    // Get the credentials
                    credProvider = GetCredentialProvider(command, runnerSettings.ServerUrl);
                    creds = credProvider.GetVssCredentials(HostContext);
                    Trace.Info("legacy vss cred retrieved");
                }
                else
                {
                    runnerSettings.GitHubUrl = inputUrl;
                    var githubToken = command.GetRunnerRegisterToken();
                    GitHubAuthResult authResult = await GetTenantCredential(inputUrl, githubToken);
                    runnerSettings.ServerUrl = authResult.TenantUrl;
                    creds = authResult.ToVssCredentials();
                    Trace.Info("cred retrieved via GitHub auth");
                }

                try
                {
                    // Determine the service deployment type based on connection data. (Hosted/OnPremises)
                    isHostedServer = await IsHostedServer(runnerSettings.ServerUrl, creds);

                    // Validate can connect.
                    await _runnerServer.ConnectAsync(new Uri(runnerSettings.ServerUrl), creds);

                    _term.WriteLine();
                    _term.WriteSuccessMessage("Connected to GitHub");

                    Trace.Info("Test Connection complete.");
                    break;
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                    _term.WriteError("Failed to connect.  Try again or ctrl-c to quit");
                    _term.WriteLine();
                }
            }

            // We want to use the native CSP of the platform for storage, so we use the RSACSP directly
            RSAParameters publicKey;
            var keyManager = HostContext.GetService<IRSAKeyManager>();
            using (var rsa = keyManager.CreateKey())
            {
                publicKey = rsa.ExportParameters(false);
            }

            _term.WriteSection("Runner Registration");

            //Get all the agent pools, and select the first private pool
            List<TaskAgentPool> agentPools = await _runnerServer.GetAgentPoolsAsync();
            TaskAgentPool agentPool = agentPools?.Where(x => x.IsHosted == false).FirstOrDefault();

            if (agentPool == null)
            {
                throw new TaskAgentPoolNotFoundException($"Could not find any private pool. Contact support.");
            }
            else
            {
                Trace.Info("Found a private pool with id {1} and name {2}", agentPool.Id, agentPool.Name);
                runnerSettings.PoolId = agentPool.Id;
                runnerSettings.PoolName = agentPool.Name;
            }

            TaskAgent agent;
            while (true)
            {
                runnerSettings.AgentName = command.GetAgentName();

                // Get the system capabilities.
                Dictionary<string, string> systemCapabilities = await HostContext.GetService<ICapabilitiesManager>().GetCapabilitiesAsync(runnerSettings, CancellationToken.None);

                _term.WriteLine();
                
                var agents = await _runnerServer.GetAgentsAsync(runnerSettings.PoolId, runnerSettings.AgentName);
                Trace.Verbose("Returns {0} agents", agents.Count);
                agent = agents.FirstOrDefault();
                if (agent != null)
                {
                    _term.WriteLine("A runner exists with the same name", ConsoleColor.Yellow);
                    if (command.GetReplace())
                    {
                        // Update existing agent with new PublicKey, agent version and SystemCapabilities.
                        agent = UpdateExistingAgent(agent, publicKey, systemCapabilities);

                        try
                        {
                            agent = await _runnerServer.UpdateAgentAsync(runnerSettings.PoolId, agent);
                            _term.WriteSuccessMessage("Successfully replaced the runner");
                            break;
                        }
                        catch (Exception e) when (!command.Unattended)
                        {
                            _term.WriteError(e);
                            _term.WriteError("Failed to replace the runner.  Try again or ctrl-c to quit");
                        }
                    }
                    else if (command.Unattended)
                    {
                        // if not replace and it is unattended config.
                        throw new TaskAgentExistsException($"Pool {runnerSettings.PoolId} already contains a runner with name {runnerSettings.AgentName}.");
                    }
                }
                else
                {
                    // Create a new agent. 
                    agent = CreateNewAgent(runnerSettings.AgentName, publicKey, systemCapabilities);

                    try
                    {
                        agent = await _runnerServer.AddAgentAsync(runnerSettings.PoolId, agent);
                        _term.WriteSuccessMessage("Runner successfully added");
                        break;
                    }
                    catch (Exception e) when (!command.Unattended)
                    {
                        _term.WriteError(e);
                        _term.WriteError("Failed to add the runner. Try again or ctrl-c to quit");
                    }
                }
            }
            // Add Agent Id to settings
            runnerSettings.AgentId = agent.Id;

            // respect the serverUrl resolve by server.
            // in case of agent configured using collection url instead of account url.
            string agentServerUrl;
            if (agent.Properties.TryGetValidatedValue<string>("ServerUrl", out agentServerUrl) &&
                !string.IsNullOrEmpty(agentServerUrl))
            {
                Trace.Info($"Agent server url resolve by server: '{agentServerUrl}'.");

                // we need make sure the Schema/Host/Port component of the url remain the same.
                UriBuilder inputServerUrl = new UriBuilder(runnerSettings.ServerUrl);
                UriBuilder serverReturnedServerUrl = new UriBuilder(agentServerUrl);
                if (Uri.Compare(inputServerUrl.Uri, serverReturnedServerUrl.Uri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    inputServerUrl.Path = serverReturnedServerUrl.Path;
                    Trace.Info($"Replace server returned url's scheme://host:port component with user input server url's scheme://host:port: '{inputServerUrl.Uri.AbsoluteUri}'.");
                    runnerSettings.ServerUrl = inputServerUrl.Uri.AbsoluteUri;
                }
                else
                {
                    runnerSettings.ServerUrl = agentServerUrl;
                }
            }

            // See if the server supports our OAuth key exchange for credentials
            if (agent.Authorization != null &&
                agent.Authorization.ClientId != Guid.Empty &&
                agent.Authorization.AuthorizationUrl != null)
            {
                UriBuilder configServerUrl = new UriBuilder(runnerSettings.ServerUrl);
                UriBuilder oauthEndpointUrlBuilder = new UriBuilder(agent.Authorization.AuthorizationUrl);
                if (!isHostedServer && Uri.Compare(configServerUrl.Uri, oauthEndpointUrlBuilder.Uri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    oauthEndpointUrlBuilder.Scheme = configServerUrl.Scheme;
                    oauthEndpointUrlBuilder.Host = configServerUrl.Host;
                    oauthEndpointUrlBuilder.Port = configServerUrl.Port;
                    Trace.Info($"Set oauth endpoint url's scheme://host:port component to match runner configure url's scheme://host:port: '{oauthEndpointUrlBuilder.Uri.AbsoluteUri}'.");
                }

                var credentialData = new CredentialData
                {
                    Scheme = Constants.Configuration.OAuth,
                    Data =
                    {
                        { "clientId", agent.Authorization.ClientId.ToString("D") },
                        { "authorizationUrl", agent.Authorization.AuthorizationUrl.AbsoluteUri },
                        { "oauthEndpointUrl", oauthEndpointUrlBuilder.Uri.AbsoluteUri },
                    },
                };

                // Save the negotiated OAuth credential data
                _store.SaveCredential(credentialData);
            }
            else
            {

                throw new NotSupportedException("Message queue listen OAuth token.");
            }

            // Testing agent connection, detect any protential connection issue, like local clock skew that cause OAuth token expired.
            var credMgr = HostContext.GetService<ICredentialManager>();
            VssCredentials credential = credMgr.LoadCredentials();
            try
            {
                await _runnerServer.ConnectAsync(new Uri(runnerSettings.ServerUrl), credential);
                _term.WriteSuccessMessage("Runner connection is good");
            }
            catch (VssOAuthTokenRequestException ex) when (ex.Message.Contains("Current server time is"))
            {
                // there are two exception messages server send that indicate clock skew.
                // 1. The bearer token expired on {jwt.ValidTo}. Current server time is {DateTime.UtcNow}.
                // 2. The bearer token is not valid until {jwt.ValidFrom}. Current server time is {DateTime.UtcNow}.                
                Trace.Error("Catch exception during test agent connection.");
                Trace.Error(ex);
                throw new Exception("The local machine's clock may be out of sync with the server time by more than five minutes. Please sync your clock with your domain or internet time and try again.");
            }

            _term.WriteSection("Runner settings");

            // We will Combine() what's stored with root.  Defaults to string a relative path
            runnerSettings.WorkFolder = command.GetWork();

            // notificationPipeName for Hosted agent provisioner.
            runnerSettings.NotificationPipeName = command.GetNotificationPipeName();

            runnerSettings.MonitorSocketAddress = command.GetMonitorSocketAddress();

            runnerSettings.NotificationSocketAddress = command.GetNotificationSocketAddress();

            _store.SaveSettings(runnerSettings);

            if (saveProxySetting)
            {
                Trace.Info("Save proxy setting to disk.");
                (runnerProxy as RunnerWebProxy).SaveProxySetting();
            }

            if (saveCertSetting)
            {
                Trace.Info("Save agent cert setting to disk.");
                (runnerCertManager as RunnerCertificateManager).SaveCertificateSetting();
            }

            _term.WriteLine();
            _term.WriteSuccessMessage("Settings Saved.");
            _term.WriteLine();

            bool saveRuntimeOptions = false;
            var runtimeOptions = new RunnerRuntimeOptions();
#if OS_WINDOWS
            if (command.GitUseSChannel)
            {
                saveRuntimeOptions = true;
                runtimeOptions.GitUseSecureChannel = true;
            }
#endif
            if (saveRuntimeOptions)
            {
                Trace.Info("Save agent runtime options to disk.");
                _store.SaveRunnerRuntimeOptions(runtimeOptions);
            }

#if OS_WINDOWS
            // config windows service
            bool runAsService = command.GetRunAsService();
            if (runAsService)
            {
                Trace.Info("Configuring to run the agent as service");
                var serviceControlManager = HostContext.GetService<IWindowsServiceControlManager>();
                serviceControlManager.ConfigureService(runnerSettings, command);
            }

#elif OS_LINUX || OS_OSX
            // generate service config script for OSX and Linux, GenerateScripts() will no-opt on windows.
            var serviceControlManager = HostContext.GetService<ILinuxServiceControlManager>();
            serviceControlManager.GenerateScripts(runnerSettings);
#endif
        }

        public async Task UnconfigureAsync(CommandSettings command)
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            string currentAction = string.Empty;
            
            _term.WriteSection("Runner removal");

            try
            {
                //stop, uninstall service and remove service config file
                if (_store.IsServiceConfigured())
                {
                    currentAction = "Removing service";
                    _term.WriteLine(currentAction);
#if OS_WINDOWS
                    var serviceControlManager = HostContext.GetService<IWindowsServiceControlManager>();
                    serviceControlManager.UnconfigureService();

                    _term.WriteLine();
                    _term.WriteSuccessMessage("Runner service removed");
#elif OS_LINUX
                    // unconfig system D service first
                    throw new Exception("Unconfigure service first");
#elif OS_OSX
                    // unconfig osx service first
                    throw new Exception("Unconfigure service first");
#endif
                }

                //delete agent from the server
                currentAction = "Removing runner from the server";
                bool isConfigured = _store.IsConfigured();
                bool hasCredentials = _store.HasCredentials();
                if (isConfigured && hasCredentials)
                {
                    RunnerSettings settings = _store.GetSettings();
                    var credentialManager = HostContext.GetService<ICredentialManager>();

                    // Get the credentials
                    VssCredentials creds = null;
                    if (string.IsNullOrEmpty(settings.GitHubUrl))
                    {
                        var credProvider = GetCredentialProvider(command, settings.ServerUrl);
                        creds = credProvider.GetVssCredentials(HostContext);
                        Trace.Info("legacy vss cred retrieved");
                    }
                    else
                    {
                        var githubToken = command.GetToken();
                        GitHubAuthResult authResult = await GetTenantCredential(settings.GitHubUrl, githubToken);
                        creds = authResult.ToVssCredentials();
                        Trace.Info("cred retrieved via GitHub auth");
                    }

                    // Determine the service deployment type based on connection data. (Hosted/OnPremises)
                    bool isHostedServer = await IsHostedServer(settings.ServerUrl, creds);
                    await _runnerServer.ConnectAsync(new Uri(settings.ServerUrl), creds);

                    var agents = await _runnerServer.GetAgentsAsync(settings.PoolId, settings.AgentName);
                    Trace.Verbose("Returns {0} agents", agents.Count);
                    TaskAgent agent = agents.FirstOrDefault();
                    if (agent == null)
                    {
                        _term.WriteLine("Does not exist. Skipping " + currentAction);
                    }
                    else
                    {
                        await _runnerServer.DeleteAgentAsync(settings.PoolId, settings.AgentId);
                        
                        _term.WriteLine();
                        _term.WriteSuccessMessage("Runner removed successfully");
                    }
                }
                else
                {
                    _term.WriteLine("Cannot connect to server, because config files are missing. Skipping removing runner from the server.");
                }

                //delete credential config files               
                currentAction = "Removing .credentials";
                if (hasCredentials)
                {
                    _store.DeleteCredential();
                    var keyManager = HostContext.GetService<IRSAKeyManager>();
                    keyManager.DeleteKey();
                    _term.WriteSuccessMessage("Removed .credentials");
                }
                else
                {
                    _term.WriteLine("Does not exist. Skipping " + currentAction);
                }

                //delete settings config file                
                currentAction = "Removing .runner";
                if (isConfigured)
                {
                    // delete proxy setting
                    (HostContext.GetService<IRunnerWebProxy>() as RunnerWebProxy).DeleteProxySetting();

                    // delete agent cert setting
                    (HostContext.GetService<IRunnerCertificateManager>() as RunnerCertificateManager).DeleteCertificateSetting();

                    // delete agent runtime option
                    _store.DeleteRunnerRuntimeOptions();

                    _store.DeleteSettings();
                    _term.WriteSuccessMessage("Removed .runner");
                }
                else
                {
                    _term.WriteLine("Does not exist. Skipping " + currentAction);
                }
            }
            catch (Exception)
            {
                _term.WriteError("Failed: " + currentAction);
                throw;
            }

            _term.WriteLine();
        }

        private ICredentialProvider GetCredentialProvider(CommandSettings command, string serverUrl)
        {
            Trace.Info(nameof(GetCredentialProvider));

            var credentialManager = HostContext.GetService<ICredentialManager>();
            // Get the default auth type. 
            // Use PAT as long as the server uri scheme is Https and looks like a FQDN
            // Otherwise windows use Integrated, linux/mac use negotiate.
            string defaultAuth = string.Empty;
            Uri server = new Uri(serverUrl);
            if (server.Scheme == Uri.UriSchemeHttps && server.Host.Contains('.'))
            {
                defaultAuth = Constants.Configuration.PAT;
            }
            else
            {
                defaultAuth = Constants.Runner.Platform == Constants.OSPlatform.Windows ? Constants.Configuration.Integrated : Constants.Configuration.Negotiate;
            }

            string authType = command.GetAuth(defaultValue: defaultAuth);

            // Create the credential.
            Trace.Info("Creating credential for auth: {0}", authType);
            var provider = credentialManager.GetCredentialProvider(authType);
            if (provider.RequireInteractive && command.Unattended)
            {
                throw new NotSupportedException($"Authentication type '{authType}' is not supported for unattended configuration.");
            }

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
            agent.Version = BuildConstants.RunnerPackage.Version;
            agent.OSDescription = RuntimeInformation.OSDescription;

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
                Version = BuildConstants.RunnerPackage.Version,
                OSDescription = RuntimeInformation.OSDescription,
            };

            foreach (KeyValuePair<string, string> capability in systemCapabilities)
            {
                agent.SystemCapabilities[capability.Key] = capability.Value ?? string.Empty;
            }

            return agent;
        }

        private async Task<bool> IsHostedServer(string serverUrl, VssCredentials credentials)
        {
            // Determine the service deployment type based on connection data. (Hosted/OnPremises)
            var locationServer = HostContext.GetService<ILocationServer>();
            VssConnection connection = VssUtil.CreateConnection(new Uri(serverUrl), credentials);
            await locationServer.ConnectAsync(connection);
            try
            {
                var connectionData = await locationServer.GetConnectionDataAsync();
                Trace.Info($"Server deployment type: {connectionData.DeploymentType}");
                return connectionData.DeploymentType.HasFlag(DeploymentFlags.Hosted);
            }
            catch (Exception ex)
            {
                // Since the DeploymentType is Enum, deserialization exception means there is a new Enum member been added.
                // It's more likely to be Hosted since OnPremises is always behind and customer can update their agent if are on-prem
                Trace.Error(ex);
                return true;
            }
        }

        private async Task<GitHubAuthResult> GetTenantCredential(string githubUrl, string githubToken)
        {
            var gitHubUrl = new UriBuilder(githubUrl);
            var githubApiUrl = $"https://api.github.com/repos/{gitHubUrl.Path.Trim('/')}/actions-runners/registration";
            using (var httpClientHandler = HostContext.CreateHttpClientHandler())
            using (var httpClient = new HttpClient(httpClientHandler))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("RemoteAuth", githubToken);
                httpClient.DefaultRequestHeaders.UserAgent.Add(HostContext.UserAgent);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.shuri-preview+json"));
                var response = await httpClient.PostAsync(githubApiUrl, new StringContent("", null, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    Trace.Info($"Http response code: {response.StatusCode} from 'POST {githubApiUrl}'");
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return StringUtil.ConvertFromJson<GitHubAuthResult>(jsonResponse);
                }
                else
                {
                    _term.WriteError($"Http response code: {response.StatusCode} from 'POST {githubApiUrl}'");
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _term.WriteError(errorResponse);
                    response.EnsureSuccessStatusCode();
                    return null;
                }
            }
        }
    }
}
