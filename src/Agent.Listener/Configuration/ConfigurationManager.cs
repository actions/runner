using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager : IAgentService
    {
        bool IsConfigured();
        Task EnsureConfiguredAsync(CommandSettings command);
        Task ConfigureAsync(CommandSettings command);
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
                    IOUtil.AssertFile(eulaFile);

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

            _term.WriteLine(StringUtil.Loc("SavingCredential"));
            Trace.Verbose("Saving credential");
            _store.SaveCredential(credProvider.CredentialData);

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

            var capProvider = HostContext.GetService<ICapabilitiesProvider>();
            TaskAgent agent;
            while (true)
            {
                agentName = command.GetAgent();
                Dictionary<string, string> capabilities = await capProvider.GetCapabilitiesAsync(agentName, CancellationToken.None);
                agent = await GetAgent(agentName, poolId);
                if (agent != null)
                {
                    if (command.GetReplace())
                    {
                        // update - update instead of delete so we don't lose user capabilities etc...
                        agent.Version = Constants.Agent.Version;

                        foreach (var capability in capabilities)
                        {
                            agent.SystemCapabilities.Add(capability.Key, capability.Value);
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
                        Version = Constants.Agent.Version
                    };

                    foreach (var capability in capabilities)
                    {
                        agent.SystemCapabilities[capability.Key] = capability.Value;
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

            // We will Combine() what's stored with root.  Defaults to string a relative path
            string workFolder = command.GetWork();

            // Get Agent settings
            var settings = new AgentSettings
            {
                AcceptTeeEula = acceptTeeEula,
                AgentId = agent.Id,
                ServerUrl = serverUrl,
                AgentName = agentName,
                PoolName = poolName,
                PoolId = poolId,
                WorkFolder = workFolder,
            };

            bool runAsService = command.GetRunAsService();
            var serviceControlManager = HostContext.GetService<IServiceControlManager>();
            bool successfullyConfigured = false;
            if (runAsService)
            {
                settings.RunAsService = true;
                Trace.Info("Configuring to run the agent as service");
                successfullyConfigured = serviceControlManager.ConfigureService(settings, command);
            }

            _store.SaveSettings(settings);

            if (runAsService && successfullyConfigured)
            {
                Trace.Info("Configuration was successful, trying to start the service");
                serviceControlManager.StartService(settings.ServiceName);
            }
        }

        private ICredentialProvider GetCredentialProvider(CommandSettings command, string serverUrl)
        {
            Trace.Info(nameof(GetCredentialProvider));

            var credentialManager = HostContext.GetService<ICredentialManager>();
            ICredentialProvider provider = null;
            if (_store.HasCredentials())
            {
                CredentialData data = _store.GetCredentials();
                provider = credentialManager.GetCredentialProvider(data.Scheme);
                provider.CredentialData = data;
            }
            else
            {
                // Get the auth type. On premise defaults to negotiate (Kerberos with fallback to NTLM).
                // Hosted defaults to PAT authentication.
                bool isHosted = serverUrl.IndexOf("visualstudio.com", StringComparison.OrdinalIgnoreCase) != -1
                    || serverUrl.IndexOf("tfsallin.net", StringComparison.OrdinalIgnoreCase) != -1;
                string defaultAuth = isHosted ? Constants.Configuration.PAT : 
                    (Constants.Agent.Platform == Constants.OSPlatform.Windows ? Constants.Configuration.Integrated : Constants.Configuration.Negotiate);
                string authType = command.GetAuth(defaultValue: defaultAuth);

                // Create the credential.
                Trace.Info("Creating credential for auth: {0}", authType);
                provider = credentialManager.GetCredentialProvider(authType);
                provider.EnsureCredential(HostContext, command, serverUrl);
            }

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