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
    public static class CliArgs
    {
        public const string AcceptTeeEula = "acceptteeeula";
        public const string Auth = "auth";
        public const string Url = "url";
        public const string Pool = "pool";
        public const string Agent = "agent";
        public const string Replace = "replace";
        public const string Work = "work";
        public const string RunAsService = "runasservice";
    }

    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager : IAgentService
    {
        bool IsConfigured();
        Task EnsureConfiguredAsync();
        Task ConfigureAsync(Dictionary<string, string> args, HashSet<string> flags, bool unattended);
        ICredentialProvider AcquireCredentials(Dictionary<String, String> args, bool unattended);
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
            _term = hostContext.GetService<ITerminal>();
            Trace.Verbose("store created");
        }

        public bool IsConfigured()
        {
            return _store.IsConfigured();
        }

        public async Task EnsureConfiguredAsync()
        {
            Trace.Info(nameof(EnsureConfiguredAsync));

            bool configured = _store.IsConfigured();

            Trace.Info("configured? {0}", configured);

            if (!configured)
            {
                await ConfigureAsync(null, null, false);
            }
        }

        public ICredentialProvider AcquireCredentials(Dictionary<String, String> args, bool unattended)
        {
            Trace.Info(nameof(AcquireCredentials));

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
                var promptManager = HostContext.GetService<IPromptManager>();
                string authType = promptManager.ReadValue(
                    argName: CliArgs.Auth,
                    description: StringUtil.Loc("AuthenticationType"),
                    secret: false,
                    defaultValue: "PAT",
                    validator: Validators.AuthSchemeValidator,
                    args: args,
                    unattended: unattended);
                Trace.Info("AuthType: {0}", authType);

                Trace.Verbose("Creating Credential: {0}", authType);
                cred = credentialManager.GetCredentialProvider(authType);
                cred.ReadCredential(HostContext, args, unattended);
            }

            return cred;
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

        public async Task ConfigureAsync(Dictionary<string, string> args, HashSet<string> flags, bool unattended)
        {
            Trace.Info(nameof(ConfigureAsync));
            if (IsConfigured())
            {
                throw new InvalidOperationException(StringUtil.Loc("AlreadyConfiguredError"));
            }

            Trace.Info("Read agent settings");
            var promptManager = HostContext.GetService<IPromptManager>();

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
                    acceptTeeEula =
                        (flags?.Contains(CliArgs.AcceptTeeEula) ?? false) ||
                        promptManager.ReadBool(
                            argName: CliArgs.AcceptTeeEula,
                            description: StringUtil.Loc("AcceptTeeEula"),
                            defaultValue: false,
                            args: null,
                            unattended: unattended);
                    break;
                case Constants.OSPlatform.Windows:
                    break;
                default:
                    throw new NotSupportedException();
            }

            // TODO: Check if its running with elevated permission and stop early if its not

            // Loop getting url and creds until you can connect
            string serverUrl = null;
            ICredentialProvider credProv = null;
            while (true)
            {
                WriteSection(StringUtil.Loc("ConnectSectionHeader"));
                serverUrl = promptManager.ReadValue(
                    argName: CliArgs.Url,
                    description: StringUtil.Loc("ServerUrl"),
                    secret: false,
                    defaultValue: string.Empty,
                    validator: Validators.ServerUrlValidator,
                    args: args,
                    unattended: unattended);
                Trace.Info("serverUrl: {0}", serverUrl);

                credProv = AcquireCredentials(args, unattended);
                VssCredentials creds = credProv.GetVssCredentials(HostContext);

                Trace.Info("cred retrieved");

                bool connected = true;
                try
                {
                    await TestConnectAsync(serverUrl, creds);
                }
                catch (Exception e)
                {
                    Trace.Error(e);
                    _term.WriteLine(StringUtil.Loc("FailedToConnect"));
                    connected = false;
                }

                // we don't want to loop when unattended
                if (unattended || connected)
                {
                    break;
                }
            }

            // TODO: Create console agent service so we can hide in testing etc... and trace
            _term.WriteLine(StringUtil.Loc("SavingCredential"));
            Trace.Verbose("Saving credential");
            _store.SaveCredential(credProv.CredentialData);

            Trace.Info("Connect Complete.");

            // Loop getting agent name and pool
            string poolName = null;
            int poolId = 0;
            string agentName = null;
            int agentId = 0;

            WriteSection(StringUtil.Loc("RegisterAgentSectionHeader"));

            while (true)
            {
                poolName = promptManager.ReadValue(
                    argName: CliArgs.Pool,
                    description: StringUtil.Loc("AgentMachinePoolNameLabel"),
                    secret: false,
                    defaultValue: "default",
                    // can do better
                    validator: Validators.NonEmptyValidator,
                    args: args,
                    unattended: unattended);

                try
                {
                    poolId = await GetPoolId(poolName);
                }
                catch (Exception e)
                {
                    Trace.Error(e);
                }

                if (unattended || poolId > 0)
                {
                    break;
                }
                else
                {
                    _term.WriteLine(StringUtil.Loc("FailedToFindPool"));
                }
            }

            var capProvider = HostContext.GetService<ICapabilitiesProvider>();
            while (true)
            {
                agentName = promptManager.ReadValue(
                    argName: CliArgs.Agent,
                    description: StringUtil.Loc("AgentName"),
                    secret: false,
                    defaultValue: Environment.MachineName ?? "myagent",
                    // can do better
                    validator: Validators.NonEmptyValidator,
                    args: args,
                    unattended: unattended);

                Dictionary<string, string> capabilities = await capProvider.GetCapabilitiesAsync(agentName, CancellationToken.None);

                TaskAgent agent = await GetAgent(agentName, poolId);
                bool exists = agent != null;
                bool replace = false;
                bool registered = false;
                if (exists)
                {
                    replace = promptManager.ReadBool(
                        argName: CliArgs.Replace,
                        description: StringUtil.Loc("Replace"),
                        defaultValue: false,
                        args: args,
                        unattended: unattended);
                    if (replace)
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
                            registered = true;
                        }
                        catch (Exception e)
                        {
                            Trace.Error(e);
                            _term.WriteLine(StringUtil.Loc("FailedToReplaceAgent"));
                        }
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
                        agent.SystemCapabilities.Add(capability.Key, capability.Value);
                    }

                    try
                    {
                        agent = await _agentServer.AddAgentAsync(poolId, agent);
                        _term.WriteLine(StringUtil.Loc("AgentAddedSuccessfully"));
                        registered = true;
                    }
                    catch (Exception e)
                    {
                        Trace.Error(e);
                        _term.WriteLine(StringUtil.Loc("AddAgentFailed"));
                    }
                }
                agentId = agent.Id;

                if (unattended || registered)
                {
                    break;
                }
            }

            // We will Combine() what's stored with root.  Defaults to string a relative path
            string workFolder = promptManager.ReadValue(
                argName: CliArgs.Work,
                description: StringUtil.Loc("WorkFolderDescription"),
                secret: false,
                defaultValue: "_work",
                // can do better
                validator: Validators.NonEmptyValidator,
                args: args,
                unattended: unattended);

            // Get Agent settings
            var settings = new AgentSettings
            {
                AcceptTeeEula = acceptTeeEula,
                AgentId = agentId,
                ServerUrl = serverUrl,
                AgentName = agentName,
                PoolName = poolName,
                PoolId = poolId,
                WorkFolder = workFolder,
            };

            bool runAsService = false;
            if (flags != null && flags.Contains(CliArgs.RunAsService))
            {
                runAsService = true;
            }
            else
            {
                runAsService = promptManager.ReadBool(
                    argName: CliArgs.RunAsService,
                    description: StringUtil.Loc("RunAgentAsServiceDescription"),
                    defaultValue: false,
                    args: null,
                    unattended: unattended);
            }

            var serviceControlManager = HostContext.GetService<IServiceControlManager>();
            bool successfullyConfigured = false;
            if (runAsService)
            {
                settings.RunAsService = true;
                Trace.Info("Configuring to run the agent as service");
                successfullyConfigured = serviceControlManager.ConfigureService(settings, args, unattended);
            }

            _store.SaveSettings(settings);

            if (runAsService && successfullyConfigured)
            {
                Trace.Info("Configuration was successful, trying to start the service");
                serviceControlManager.StartService(settings.ServiceName);
            }
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