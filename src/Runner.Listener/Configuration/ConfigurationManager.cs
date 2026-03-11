using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.Common.Internal;
using GitHub.Services.OAuth;

namespace GitHub.Runner.Listener.Configuration
{
    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager : IRunnerService
    {
        bool IsConfigured();
        Task ConfigureAsync(CommandSettings command);
        Task UnconfigureAsync(CommandSettings command);
        void DeleteLocalRunnerConfig();
        RunnerSettings LoadSettings();
        RunnerSettings LoadMigratedSettings();
    }

    public sealed class ConfigurationManager : RunnerService, IConfigurationManager
    {
        private IConfigurationStore _store;
        private IRunnerServer _runnerServer;
        private IRunnerDotcomServer _dotcomServer;
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _runnerServer = HostContext.GetService<IRunnerServer>();
            _dotcomServer = HostContext.GetService<IRunnerDotcomServer>();
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
                throw new NonRetryableException("Not configured. Run config.(sh/cmd) to configure the runner.");
            }

            RunnerSettings settings = _store.GetSettings();
            Trace.Info("Settings Loaded");

            return settings;
        }

        public RunnerSettings LoadMigratedSettings()
        {
            Trace.Info(nameof(LoadMigratedSettings));

            // Check if migrated settings file exists
            if (!_store.IsMigratedConfigured())
            {
                throw new NonRetryableException("No migrated configuration found.");
            }

            RunnerSettings settings = _store.GetMigratedSettings();
            Trace.Info("Migrated Settings Loaded");

            return settings;
        }

        public async Task ConfigureAsync(CommandSettings command)
        {
            _term.WriteLine();
            _term.WriteLine("--------------------------------------------------------------------------------");
            _term.WriteLine("|        ____ _ _   _   _       _          _        _   _                      |");
            _term.WriteLine("|       / ___(_) |_| | | |_   _| |__      / \\   ___| |_(_) ___  _ __  ___      |");
            _term.WriteLine("|      | |  _| | __| |_| | | | | '_ \\    / _ \\ / __| __| |/ _ \\| '_ \\/ __|     |");
            _term.WriteLine("|      | |_| | | |_|  _  | |_| | |_) |  / ___ \\ (__| |_| | (_) | | | \\__ \\     |");
            _term.WriteLine("|       \\____|_|\\__|_| |_|\\__,_|_.__/  /_/   \\_\\___|\\__|_|\\___/|_| |_|___/     |");
            _term.WriteLine("|                                                                              |");
            _term.Write("|                       ");
            _term.Write("Self-hosted runner registration", ConsoleColor.Cyan);
            _term.WriteLine("                        |");
            _term.WriteLine("|                                                                              |");
            _term.WriteLine("--------------------------------------------------------------------------------");

            Trace.Info(nameof(ConfigureAsync));

            if (command.GenerateServiceConfig)
            {
#if OS_LINUX
                if (!IsConfigured())
                {
                    throw new InvalidOperationException("--generateServiceConfig requires that the runner is already configured. For configuring a new runner as a service, run './config.sh'.");
                }

                RunnerSettings settings = _store.GetSettings();

                Trace.Info($"generate service config for runner: {settings.AgentId}");
                var controlManager = HostContext.GetService<ILinuxServiceControlManager>();
                controlManager.GenerateScripts(settings);

                return;
#else
                throw new NotSupportedException("--generateServiceConfig is only supported on Linux.");
#endif
            }

            if (IsConfigured())
            {
                throw new InvalidOperationException("Cannot configure the runner because it is already configured. To reconfigure the runner, run 'config.cmd remove' or './config.sh remove' first.");
            }

            RunnerSettings runnerSettings = new();

            // Loop getting url and creds until you can connect
            ICredentialProvider credProvider = null;
            VssCredentials creds = null;
            _term.WriteSection("Authentication");
            string registerToken = string.Empty;
            while (true)
            {
                // When testing against a dev deployment of Actions Service, set this environment variable
                var useDevActionsServiceUrl = Environment.GetEnvironmentVariable("USE_DEV_ACTIONS_SERVICE_URL");
                var inputUrl = command.GetUrl();
                if (inputUrl.Contains("codedev.ms", StringComparison.OrdinalIgnoreCase)
                    || useDevActionsServiceUrl != null)
                {
                    runnerSettings.ServerUrl = inputUrl;
                    // Get the credentials
                    credProvider = GetCredentialProvider(command, runnerSettings.ServerUrl);
                    creds = credProvider.GetVssCredentials(HostContext, allowAuthUrlV2: false);
                    Trace.Info("legacy vss cred retrieved");
                }
                else
                {
                    runnerSettings.GitHubUrl = inputUrl;
                    registerToken = await GetRunnerTokenAsync(command, inputUrl, "registration");
                    GitHubAuthResult authResult = await GetTenantCredential(inputUrl, registerToken, Constants.RunnerEvent.Register);
                    runnerSettings.ServerUrl = authResult.TenantUrl;
                    runnerSettings.UseRunnerAdminFlow = authResult.UseRunnerAdminFlow;
                    Trace.Info($"Using runner-admin flow: {runnerSettings.UseRunnerAdminFlow}");
                    creds = authResult.ToVssCredentials();
                    Trace.Info("cred retrieved via GitHub auth");
                }

                try
                {
                    // Determine the service deployment type based on connection data. (Hosted/OnPremises)
                    // Hosted usually means github.com or localhost, while OnPremises means GHES or GHAE
                    runnerSettings.IsHostedServer = runnerSettings.GitHubUrl == null || UrlUtil.IsHostedServer(new UriBuilder(runnerSettings.GitHubUrl));

                    // Warn if the Actions server url and GHES server url has different Host
                    if (!runnerSettings.IsHostedServer)
                    {
                        // Example actionsServerUrl is https://my-ghes/_services/pipelines/[...]
                        // Example githubServerUrl is https://my-ghes
                        var actionsServerUrl = new Uri(runnerSettings.ServerUrl);
                        var githubServerUrl = new Uri(runnerSettings.GitHubUrl);
                        if (!UriUtility.IsSubdomainOf(actionsServerUrl.Authority, githubServerUrl.Authority))
                        {
                            throw new InvalidOperationException($"GitHub Actions is not properly configured in GHES. GHES url: {runnerSettings.GitHubUrl}, Actions url: {runnerSettings.ServerUrl}.");
                        }
                    }

                    // Validate can connect using the obtained vss credentials.
                    // In Runner Admin flow there's nothing new to test connection to at this point as registerToken is already validated via GetTenantCredential.
                    if (!runnerSettings.UseRunnerAdminFlow)
                    {
                        await _runnerServer.ConnectAsync(new Uri(runnerSettings.ServerUrl), creds);
                    }

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
            string publicKeyXML;
            using (var rsa = keyManager.CreateKey())
            {
                publicKey = rsa.ExportParameters(false);
                publicKeyXML = rsa.ToXmlString(includePrivateParameters: false);
            }

            _term.WriteSection("Runner Registration");

            // If we have more than one runner group available, allow the user to specify which one to be added into
            string poolName = null;
            TaskAgentPool agentPool = null;
            List<TaskAgentPool> agentPools;
            if (runnerSettings.UseRunnerAdminFlow)
            {
                agentPools = await _dotcomServer.GetRunnerGroupsAsync(runnerSettings.GitHubUrl, registerToken);
            }
            else
            {
                agentPools = await _runnerServer.GetAgentPoolsAsync();
            }

            TaskAgentPool defaultPool = agentPools?.Where(x => x.IsInternal).FirstOrDefault();
            if (agentPools?.Where(x => !x.IsHosted).Count() > 0)
            {
                poolName = command.GetRunnerGroupName(defaultPool?.Name);
                _term.WriteLine();
                agentPool = agentPools.Where(x => string.Equals(poolName, x.Name, StringComparison.OrdinalIgnoreCase) && !x.IsHosted).FirstOrDefault();
            }
            else
            {
                agentPool = defaultPool;
            }

            if (agentPool == null && poolName == null)
            {
                throw new TaskAgentPoolNotFoundException($"Could not find any self-hosted runner groups. Contact support.");
            }
            else if (agentPool == null && poolName != null)
            {
                throw new TaskAgentPoolNotFoundException($"Could not find any self-hosted runner group named \"{poolName}\".");
            }
            else
            {
                Trace.Info($"Found a self-hosted runner group with id {agentPool.Id} and name {agentPool.Name}");
                runnerSettings.PoolId = agentPool.Id;
                runnerSettings.PoolName = agentPool.Name;
            }

            TaskAgent agent;
            while (true)
            {
                runnerSettings.DisableUpdate = command.DisableUpdate;
                runnerSettings.Ephemeral = command.Ephemeral;
                runnerSettings.AgentName = command.GetRunnerName();

                _term.WriteLine();

                var userLabels = command.GetLabels();
                _term.WriteLine();
                List<TaskAgent> agents;
                if (runnerSettings.UseRunnerAdminFlow)
                {
                    agents = await _dotcomServer.GetRunnerByNameAsync(runnerSettings.GitHubUrl, registerToken, runnerSettings.AgentName);
                }
                else
                {
                    agents = await _runnerServer.GetAgentsAsync(runnerSettings.AgentName);
                }

                Trace.Verbose("Returns {0} agents", agents.Count);
                agent = agents.FirstOrDefault();
                if (agent != null)
                {
                    _term.WriteLine("A runner exists with the same name", ConsoleColor.Yellow);
                    if (command.GetReplace())
                    {
                        // Update existing agent with new PublicKey, agent version.
                        agent = UpdateExistingAgent(agent, publicKey, userLabels, runnerSettings.Ephemeral, command.DisableUpdate, command.NoDefaultLabels);

                        try
                        {
                            if (runnerSettings.UseRunnerAdminFlow)
                            {
                                var runner = await _dotcomServer.ReplaceRunnerAsync(runnerSettings.PoolId, agent, runnerSettings.GitHubUrl, registerToken, publicKeyXML);
                                runnerSettings.ServerUrlV2 = runner.RunnerAuthorization.ServerUrl;
                                runnerSettings.UseV2Flow = true; // if we are using runner admin, we also need to hit broker

                                agent.Id = runner.Id;
                                agent.Authorization = new TaskAgentAuthorization()
                                {
                                    AuthorizationUrl = runner.RunnerAuthorization.AuthorizationUrl,
                                    ClientId = new Guid(runner.RunnerAuthorization.ClientId)
                                };

                                if (!string.IsNullOrEmpty(runner.RunnerAuthorization.LegacyAuthorizationUrl?.AbsoluteUri))
                                {
                                    agent.Authorization.AuthorizationUrl = runner.RunnerAuthorization.LegacyAuthorizationUrl;
                                    agent.Properties["EnableAuthMigrationByDefault"] = true;
                                    agent.Properties["AuthorizationUrlV2"] = runner.RunnerAuthorization.AuthorizationUrl.AbsoluteUri;
                                }
                            }
                            else
                            {
                                agent = await _runnerServer.ReplaceAgentAsync(runnerSettings.PoolId, agent);
                            }

                            if (command.DisableUpdate &&
                                command.DisableUpdate != agent.DisableUpdate)
                            {
                                throw new NotSupportedException("The GitHub server does not support configuring a self-hosted runner with 'DisableUpdate' flag.");
                            }
                            if (command.Ephemeral &&
                                command.Ephemeral != agent.Ephemeral)
                            {
                                throw new NotSupportedException("The GitHub server does not support configuring a self-hosted runner with 'Ephemeral' flag.");
                            }

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
                        throw new TaskAgentExistsException($"A runner exists with the same name {runnerSettings.AgentName}.");
                    }
                }
                else
                {
                    // Create a new agent.
                    agent = CreateNewAgent(runnerSettings.AgentName, publicKey, userLabels, runnerSettings.Ephemeral, command.DisableUpdate, command.NoDefaultLabels);

                    try
                    {
                        if (runnerSettings.UseRunnerAdminFlow)
                        {
                            var runner = await _dotcomServer.AddRunnerAsync(runnerSettings.PoolId, agent, runnerSettings.GitHubUrl, registerToken, publicKeyXML);
                            runnerSettings.ServerUrlV2 = runner.RunnerAuthorization.ServerUrl;
                            runnerSettings.UseV2Flow = true; // if we are using runner admin, we also need to hit broker

                            agent.Id = runner.Id;
                            agent.Authorization = new TaskAgentAuthorization()
                            {
                                AuthorizationUrl = runner.RunnerAuthorization.AuthorizationUrl,
                                ClientId = new Guid(runner.RunnerAuthorization.ClientId)
                            };

                            if (!string.IsNullOrEmpty(runner.RunnerAuthorization.LegacyAuthorizationUrl?.AbsoluteUri))
                            {
                                agent.Authorization.AuthorizationUrl = runner.RunnerAuthorization.LegacyAuthorizationUrl;
                                agent.Properties["EnableAuthMigrationByDefault"] = true;
                                agent.Properties["AuthorizationUrlV2"] = runner.RunnerAuthorization.AuthorizationUrl.AbsoluteUri;
                            }
                        }
                        else
                        {
                            agent = await _runnerServer.AddAgentAsync(runnerSettings.PoolId, agent);
                        }

                        if (command.DisableUpdate &&
                            command.DisableUpdate != agent.DisableUpdate)
                        {
                            throw new NotSupportedException("The GitHub server does not support configuring a self-hosted runner with 'DisableUpdate' flag.");
                        }
                        if (command.Ephemeral &&
                            command.Ephemeral != agent.Ephemeral)
                        {
                            throw new NotSupportedException("The GitHub server does not support configuring a self-hosted runner with 'Ephemeral' flag.");
                        }

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
                        { "requireFipsCryptography", agent.Properties.GetValue("RequireFipsCryptography", true).ToString() }
                    },
                };

                if (agent.Properties.GetValue("EnableAuthMigrationByDefault", false) &&
                    agent.Properties.TryGetValue<string>("AuthorizationUrlV2", out var authUrlV2) &&
                    !string.IsNullOrEmpty(authUrlV2))
                {
                    credentialData.Data["enableAuthMigrationByDefault"] = "true";
                    credentialData.Data["authorizationUrlV2"] = authUrlV2;
                }

                // Save the negotiated OAuth credential data
                _store.SaveCredential(credentialData);
            }
            else
            {
                throw new NotSupportedException("Message queue listen OAuth token.");
            }

            // allow the server to override the serverUrlV2 and useV2Flow
            if (agent.Properties.TryGetValue("ServerUrlV2", out string serverUrlV2) &&
                !string.IsNullOrEmpty(serverUrlV2))
            {
                Trace.Info($"Service enforced serverUrlV2: {serverUrlV2}");
                runnerSettings.ServerUrlV2 = serverUrlV2;
            }

            if (agent.Properties.TryGetValue("UseV2Flow", out bool useV2Flow) && useV2Flow)
            {
                Trace.Info($"Service enforced useV2Flow: {useV2Flow}");
                runnerSettings.UseV2Flow = useV2Flow;
            }

            // Testing agent connection, detect any potential connection issue, like local clock skew that cause OAuth token expired.

            if (!runnerSettings.UseV2Flow && !runnerSettings.UseRunnerAdminFlow)
            {
                var credMgr = HostContext.GetService<ICredentialManager>();
                VssCredentials credential = credMgr.LoadCredentials(allowAuthUrlV2: false);
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

        // Delete .runner and .credentials files
        public void DeleteLocalRunnerConfig()
        {
            bool isConfigured = _store.IsConfigured();
            bool hasCredentials = _store.HasCredentials();
            // delete credential config files
            var currentAction = "Removing .credentials";
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

            // delete settings config file
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

        public async Task UnconfigureAsync(CommandSettings command)
        {
            string currentAction = string.Empty;

            _term.WriteSection("Runner removal");

            try
            {
                // stop, uninstall service and remove service config file
                if (_store.IsServiceConfigured())
                {
                    currentAction = "Removing service";
                    _term.WriteLine(currentAction);
#if OS_WINDOWS
                    var serviceControlManager = HostContext.GetService<IWindowsServiceControlManager>();
                    serviceControlManager.UnconfigureService();

                    _term.WriteLine();
                    _term.WriteSuccessMessage("Runner service removed");
#else
                    // uninstall systemd or macOS service first
                    throw new Exception("Uninstall service first");
#endif
                }

                // delete agent from the server
                currentAction = "Removing runner from the server";
                bool isConfigured = _store.IsConfigured();
                bool hasCredentials = _store.HasCredentials();
                if (isConfigured && hasCredentials)
                {
                    RunnerSettings settings = _store.GetSettings();

                    if (settings.UseRunnerAdminFlow)
                    {
                        var deletionToken = await GetRunnerTokenAsync(command, settings.GitHubUrl, "remove");
                        await _dotcomServer.DeleteRunnerAsync(settings.GitHubUrl, deletionToken, settings.AgentId);
                    }
                    else
                    {
                        var credentialManager = HostContext.GetService<ICredentialManager>();

                        // Get the credentials
                        VssCredentials creds = null;
                        if (string.IsNullOrEmpty(settings.GitHubUrl))
                        {
                            var credProvider = GetCredentialProvider(command, settings.ServerUrl);
                            creds = credProvider.GetVssCredentials(HostContext, allowAuthUrlV2: false);
                            Trace.Info("legacy vss cred retrieved");
                        }
                        else
                        {
                            var deletionToken = await GetRunnerTokenAsync(command, settings.GitHubUrl, "remove");
                            GitHubAuthResult authResult = await GetTenantCredential(settings.GitHubUrl, deletionToken, Constants.RunnerEvent.Remove);
                            creds = authResult.ToVssCredentials();
                            Trace.Info("cred retrieved via GitHub auth");
                        }

                        // Determine the service deployment type based on connection data. (Hosted/OnPremises)
                        await _runnerServer.ConnectAsync(new Uri(settings.ServerUrl), creds);

                        var agents = await _runnerServer.GetAgentsAsync(settings.AgentName);
                        Trace.Verbose("Returns {0} agents", agents.Count);
                        TaskAgent agent = agents.FirstOrDefault();
                        if (agent == null)
                        {
                            _term.WriteLine("Does not exist. Skipping " + currentAction);
                        }
                        else
                        {
                            await _runnerServer.DeleteAgentAsync(settings.AgentId);
                        }
                    }

                    _term.WriteLine();
                    _term.WriteSuccessMessage("Runner removed successfully");
                }
                else
                {
                    _term.WriteLine("Cannot connect to server, because config files are missing. Skipping removing runner from the server.");
                }

                DeleteLocalRunnerConfig();
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


        private TaskAgent UpdateExistingAgent(TaskAgent agent, RSAParameters publicKey, ISet<string> userLabels, bool ephemeral, bool disableUpdate, bool noDefaultLabels)
        {
            ArgUtil.NotNull(agent, nameof(agent));
            agent.Authorization = new TaskAgentAuthorization
            {
                PublicKey = new TaskAgentPublicKey(publicKey.Exponent, publicKey.Modulus),
            };

            // update should replace the existing labels
            agent.Version = BuildConstants.RunnerPackage.Version;
            agent.OSDescription = RuntimeInformation.OSDescription;
            agent.Ephemeral = ephemeral;
            agent.DisableUpdate = disableUpdate;
            agent.MaxParallelism = 1;

            agent.Labels.Clear();

            if (!noDefaultLabels)
            {
                agent.Labels.Add(new AgentLabel("self-hosted", LabelType.System));
                agent.Labels.Add(new AgentLabel(VarUtil.OS, LabelType.System));
                agent.Labels.Add(new AgentLabel(VarUtil.OSArchitecture, LabelType.System));
            }
            else if (userLabels.Count == 0)
            {
                throw new NotSupportedException("Disabling default labels via --no-default-labels without specifying --labels is not supported");
            }

            foreach (var userLabel in userLabels)
            {
                agent.Labels.Add(new AgentLabel(userLabel, LabelType.User));
            }

            return agent;
        }

        private TaskAgent CreateNewAgent(string agentName, RSAParameters publicKey, ISet<string> userLabels, bool ephemeral, bool disableUpdate, bool noDefaultLabels)
        {
            TaskAgent agent = new(agentName)
            {
                Authorization = new TaskAgentAuthorization
                {
                    PublicKey = new TaskAgentPublicKey(publicKey.Exponent, publicKey.Modulus),
                },
                MaxParallelism = 1,
                Version = BuildConstants.RunnerPackage.Version,
                OSDescription = RuntimeInformation.OSDescription,
                Ephemeral = ephemeral,
                DisableUpdate = disableUpdate
            };

            if (!noDefaultLabels)
            {
                agent.Labels.Add(new AgentLabel("self-hosted", LabelType.System));
                agent.Labels.Add(new AgentLabel(VarUtil.OS, LabelType.System));
                agent.Labels.Add(new AgentLabel(VarUtil.OSArchitecture, LabelType.System));
            }
            else if (userLabels.Count == 0)
            {
                throw new NotSupportedException("Disabling default labels via --no-default-labels without specifying --labels is not supported");
            }

            foreach (var userLabel in userLabels)
            {
                agent.Labels.Add(new AgentLabel(userLabel, LabelType.User));
            }

            return agent;
        }

        private async Task<string> GetRunnerTokenAsync(CommandSettings command, string githubUrl, string tokenType)
        {
            var githubPAT = command.GetGitHubPersonalAccessToken();
            var runnerToken = string.Empty;
            if (!string.IsNullOrEmpty(githubPAT))
            {
                Trace.Info($"Retrieving runner {tokenType} token using GitHub PAT.");
                var jitToken = await GetJITRunnerTokenAsync(githubUrl, githubPAT, tokenType);
                Trace.Info($"Retrieved runner {tokenType} token is good to {jitToken.ExpiresAt}.");
                HostContext.SecretMasker.AddValue(jitToken.Token);
                runnerToken = jitToken.Token;
            }

            if (string.IsNullOrEmpty(runnerToken))
            {
                if (string.Equals("registration", tokenType, StringComparison.OrdinalIgnoreCase))
                {
                    runnerToken = command.GetRunnerRegisterToken();
                }
                else
                {
                    runnerToken = command.GetRunnerDeletionToken();
                }
            }

            return runnerToken;
        }

        private async Task<GitHubRunnerRegisterToken> GetJITRunnerTokenAsync(string githubUrl, string githubToken, string tokenType)
        {
            var githubApiUrl = "";
            var gitHubUrlBuilder = new UriBuilder(githubUrl);
            var path = gitHubUrlBuilder.Path.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
            if (path.Length == 1)
            {
                // org runner
                if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/orgs/{path[0]}/actions/runners/{tokenType}-token";
                }
                else
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/orgs/{path[0]}/actions/runners/{tokenType}-token";
                }
            }
            else if (path.Length == 2)
            {
                // repo or enterprise runner.
                var repoScope = "repos/";
                if (string.Equals(path[0], "enterprises", StringComparison.OrdinalIgnoreCase))
                {
                    repoScope = "";
                }

                if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/{repoScope}{path[0]}/{path[1]}/actions/runners/{tokenType}-token";
                }
                else
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/{repoScope}{path[0]}/{path[1]}/actions/runners/{tokenType}-token";
                }
            }
            else
            {
                throw new ArgumentException($"'{githubUrl}' should point to an org or repository.");
            }

            int retryCount = 0;
            while (retryCount < 3)
            {
                using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"github:{githubToken}"));
                    HostContext.SecretMasker.AddValue(base64EncodingToken);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("basic", base64EncodingToken);
                    httpClient.DefaultRequestHeaders.UserAgent.AddRange(HostContext.UserAgents);
                    httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

                    var responseStatus = System.Net.HttpStatusCode.OK;
                    try
                    {
                        var response = await httpClient.PostAsync(githubApiUrl, new StringContent(string.Empty));
                        responseStatus = response.StatusCode;
                        var githubRequestId = UrlUtil.GetGitHubRequestId(response.Headers);

                        if (response.IsSuccessStatusCode)
                        {
                            Trace.Info($"Http response code: {response.StatusCode} from 'POST {githubApiUrl}' ({githubRequestId})");
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            return StringUtil.ConvertFromJson<GitHubRunnerRegisterToken>(jsonResponse);
                        }
                        else
                        {
                            _term.WriteError($"Http response code: {response.StatusCode} from 'POST {githubApiUrl}' (Request Id: {githubRequestId})");
                            var errorResponse = await response.Content.ReadAsStringAsync();
                            _term.WriteError(errorResponse);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    catch (Exception ex) when (retryCount < 2 && responseStatus != System.Net.HttpStatusCode.NotFound)
                    {
                        retryCount++;
                        Trace.Error($"Failed to get JIT runner token -- Attempt: {retryCount}");
                        Trace.Error(ex);
                    }
                }
                var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
                Trace.Info($"Retrying in {backOff.Seconds} seconds");
                await Task.Delay(backOff);
            }
            return null;
        }

        private async Task<GitHubAuthResult> GetTenantCredential(string githubUrl, string githubToken, string runnerEvent)
        {
            var githubApiUrl = "";
            var gitHubUrlBuilder = new UriBuilder(githubUrl);
            if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
            {
                githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/actions/runner-registration";
            }
            else
            {
                githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/actions/runner-registration";
            }

            int retryCount = 0;
            while (retryCount < 3)
            {
                using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("RemoteAuth", githubToken);
                    httpClient.DefaultRequestHeaders.UserAgent.AddRange(HostContext.UserAgents);

                    var bodyObject = new Dictionary<string, string>()
                    {
                        {"url", githubUrl},
                        {"runner_event", runnerEvent}
                    };

                    var responseStatus = System.Net.HttpStatusCode.OK;
                    try
                    {
                        var response = await httpClient.PostAsync(githubApiUrl, new StringContent(StringUtil.ConvertToJson(bodyObject), null, "application/json"));
                        responseStatus = response.StatusCode;
                        var githubRequestId = UrlUtil.GetGitHubRequestId(response.Headers);

                        if (response.IsSuccessStatusCode)
                        {
                            Trace.Info($"Http response code: {response.StatusCode} from 'POST {githubApiUrl}' ({githubRequestId})");
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            return StringUtil.ConvertFromJson<GitHubAuthResult>(jsonResponse);
                        }
                        else
                        {
                            _term.WriteError($"Http response code: {response.StatusCode} from 'POST {githubApiUrl}' (Request Id: {githubRequestId})");
                            var errorResponse = await response.Content.ReadAsStringAsync();
                            _term.WriteError(errorResponse);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    catch (Exception ex) when (retryCount < 2 && responseStatus != System.Net.HttpStatusCode.NotFound)
                    {
                        retryCount++;
                        Trace.Error($"Failed to get tenant credentials -- Attempt: {retryCount}");
                        Trace.Error(ex);
                    }
                }
                var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
                Trace.Info($"Retrying in {backOff.Seconds} seconds");
                await Task.Delay(backOff);
            }
            return null;
        }
    }
}
