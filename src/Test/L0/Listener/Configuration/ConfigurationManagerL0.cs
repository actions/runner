using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Xunit;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public class ConfigurationManagerL0
    {
        private Mock<IAgentServer> _agentServer;
        private Mock<ICredentialManager> _credMgr;
        private Mock<IPromptManager> _promptManager;
        private Mock<IConfigurationStore> _store;
        private Mock<IExtensionManager> _extnMgr;
        private Mock<IServiceControlManager> _serviceControlManager;
        private IRSAKeyManager _rsaKeyManager;
        private ICapabilitiesManager _capabilitiesManager;
        private string _expectedToken = "expectedToken";
        private string _expectedServerUrl = "https://localhost";
        private string _expectedAgentName = "expectedAgentName";
        private string _expectedPoolName = "poolName";
        private string _expectedAuthType = "pat";
        private string _expectedWorkFolder = "_work";
        private int _expectedPoolId = 1;
        private AgentSettings _configMgrAgentSettings = new AgentSettings();


        public ConfigurationManagerL0()
        {
            _agentServer = new Mock<IAgentServer>();
            _credMgr = new Mock<ICredentialManager>();
            _promptManager = new Mock<IPromptManager>();
            _store = new Mock<IConfigurationStore>();
            _extnMgr = new Mock<IExtensionManager>();
            _serviceControlManager = new Mock<IServiceControlManager>();

#if OS_WINDOWS
            _rsaKeyManager = new RSAEncryptedFileKeyManager();
#else
            _rsaKeyManager = new RSAFileKeyManager();
#endif

            _capabilitiesManager = new CapabilitiesManager();

            _agentServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));

            _store.Setup(x => x.IsConfigured()).Returns(false);
            _store.Setup(x => x.HasCredentials()).Returns(false);

            _store.Setup(x => x.GetSettings()).Returns(
                () => _configMgrAgentSettings
                );

            _store.Setup(x => x.SaveSettings(It.IsAny<AgentSettings>())).Callback((AgentSettings settings) =>
            {
                _configMgrAgentSettings = settings;
            });

            _credMgr.Setup(x => x.GetCredentialProvider(It.IsAny<string>())).Returns(new TestAgentCredential());

            _serviceControlManager.Setup(x => x.GenerateScripts(It.IsAny<AgentSettings>()));

            var expectedPools = new List<TaskAgentPool>() { new TaskAgentPool(_expectedPoolName) { Id = _expectedPoolId } };
            _agentServer.Setup(x => x.GetAgentPoolsAsync(It.IsAny<string>())).Returns(Task.FromResult(expectedPools));

            var expectedAgents = new List<TaskAgent>();
            _agentServer.Setup(x => x.GetAgentsAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(expectedAgents));

            var expectedAgent = new TaskAgent(_expectedAgentName) { Id = 1 };
            _agentServer.Setup(x => x.AddAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));
            _agentServer.Setup(x => x.UpdateAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<ICredentialManager>(_credMgr.Object);
            tc.SetSingleton<IPromptManager>(_promptManager.Object);
            tc.SetSingleton<IConfigurationStore>(_store.Object);
            tc.SetSingleton<IExtensionManager>(_extnMgr.Object);
            tc.SetSingleton<IAgentServer>(_agentServer.Object);
            tc.SetSingleton<ICapabilitiesManager>(_capabilitiesManager);
            tc.SetSingleton<IServiceControlManager>(_serviceControlManager.Object);

            tc.SetSingleton<IRSAKeyManager>(_rsaKeyManager);
            tc.EnqueueInstance<IAgentServer>(_agentServer.Object);

            return tc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void CanEnsureConfigure()
        {
            using (TestHostContext tc = CreateTestContext())
            {
                Tracing trace = tc.GetTrace();

                trace.Info("Creating config manager");
                IConfigurationManager configManager = new ConfigurationManager();
                configManager.Initialize(tc);

                trace.Info("Preparing command line arguments");
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                       "configure",
                       "--url", _expectedServerUrl,
                       "--agent", _expectedAgentName,
                       "--pool", _expectedPoolName,
                       "--work", _expectedWorkFolder,
                       "--auth", _expectedAuthType,
                       "--token", _expectedToken
                    });
                trace.Info("Constructed.");
                _store.Setup(x => x.IsConfigured()).Returns(false);
                _configMgrAgentSettings = null;

                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                configManager.ConfigureAsync(command);

                _store.Setup(x => x.IsConfigured()).Returns(true);

                trace.Info("Configured, verifying all the parameter value");
                var s = configManager.LoadSettings();
                Assert.True(s.ServerUrl.Equals(_expectedServerUrl));
                Assert.True(s.AgentName.Equals(_expectedAgentName));
                Assert.True(s.PoolId.Equals(_expectedPoolId));
                Assert.True(s.WorkFolder.Equals(_expectedWorkFolder));
            }
        }

        // TODO Unit Test for IsConfigured - Rename config file and make sure it returns false

    }
}