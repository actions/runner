using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

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

            Trace.Info(nameof(ConfigureAsync));
            if (IsConfigured())
            {
                throw new InvalidOperationException("Cannot configure the runner because it is already configured. To reconfigure the runner, run 'config.cmd remove' or './config.sh remove' first.");
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
                if (!inputUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase) &&
                    !inputUrl.Contains("github.localhost", StringComparison.OrdinalIgnoreCase))
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
                runnerSettings.AgentName = command.GetRunnerName();

                _term.WriteLine();

                var agents = await _runnerServer.GetAgentsAsync(runnerSettings.PoolId, runnerSettings.AgentName);
                Trace.Verbose("Returns {0} agents", agents.Count);
                agent = agents.FirstOrDefault();
                if (agent != null)
                {
                    _term.WriteLine("A runner exists with the same name", ConsoleColor.Yellow);
                    if (command.GetReplace())
                    {
                        // Update existing agent with new PublicKey, agent version.
                        agent = UpdateExistingAgent(agent, publicKey);

                        try
                        {
                            agent = await _runnerServer.ReplaceAgentAsync(runnerSettings.PoolId, agent);
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
                    agent = CreateNewAgent(runnerSettings.AgentName, publicKey);

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

            // Testing agent connection, detect any potential connection issue, like local clock skew that cause OAuth token expired.
            var credMgr = HostContext.GetService<ICredentialManager>();
            VssCredentials credential = credMgr.LoadCredentials();
            try
            {
                await _runnerServer.ConnectAsync(new Uri(runnerSettings.ServerUrl), credential);
                // ConnectAsync() hits _apis/connectionData which is an anonymous endpoint
                // Need to hit an authenticate endpoint to trigger OAuth token exchange.
                await _runnerServer.GetAgentPoolsAsync();
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

            runnerSettings.MonitorSocketAddress = command.GetMonitorSocketAddress();

            _store.SaveSettings(runnerSettings);

            _term.WriteLine();
            _term.WriteSuccessMessage("Settings Saved.");
            _term.WriteLine();

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
                        var githubToken = command.GetRunnerDeletionToken();
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
            string authType = command.GetAuth(defaultValue: Constants.Configuration.OAuthAccessToken);

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


        private TaskAgent UpdateExistingAgent(TaskAgent agent, RSAParameters publicKey)
        {
            ArgUtil.NotNull(agent, nameof(agent));
            agent.Authorization = new TaskAgentAuthorization
            {
                PublicKey = new TaskAgentPublicKey(publicKey.Exponent, publicKey.Modulus),
            };

            // update - update instead of delete so we don't lose labels etc...
            agent.Version = BuildConstants.RunnerPackage.Version;
            agent.OSDescription = RuntimeInformation.OSDescription;

            agent.Labels.Add("self-hosted");
            agent.Labels.Add(VarUtil.OS);
            agent.Labels.Add(VarUtil.OSArchitecture);

            return agent;
        }

        private TaskAgent CreateNewAgent(string agentName, RSAParameters publicKey)
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

            agent.Labels.Add("self-hosted");
            agent.Labels.Add(VarUtil.OS);
            agent.Labels.Add(VarUtil.OSArchitecture);

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
            var gitHubUrlBuilder = new UriBuilder(githubUrl);
            var githubApiUrl = $"https://api.{gitHubUrlBuilder.Host}/actions/runner-registration";
           using (var httpClientHandler = HostContext.CreateHttpClientHandler())
            using (var httpClient = new HttpClient(httpClientHandler))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("RemoteAuth", githubToken);
                httpClient.DefaultRequestHeaders.UserAgent.Add(HostContext.UserAgent);

                var bodyObject = new Dictionary<string, string>()
                {
                    {"url", githubUrl}
                };

                var response = await httpClient.PostAsync(githubApiUrl, new StringContent(StringUtil.ConvertToJson(bodyObject), null, "application/json"));

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
