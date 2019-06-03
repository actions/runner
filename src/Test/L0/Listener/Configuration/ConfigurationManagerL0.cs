using GitHub.DistributedTask.WebApi;
using Runner.Common.Listener;
using Runner.Common.Capabilities;
using Runner.Common.Listener.Configuration;
using Runner.Common.Util;
using GitHub.Services.WebApi;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;
using GitHub.Services.Location;
using GitHub.Services.Common;

namespace Runner.Common.Tests.Listener.Configuration
{
    public class ConfigurationManagerL0
    {
        private Mock<IAgentServer> _agentServer;
        private Mock<ILocationServer> _locationServer;
        private Mock<ICredentialManager> _credMgr;
        private Mock<IPromptManager> _promptManager;
        private Mock<IConfigurationStore> _store;
        private Mock<IExtensionManager> _extnMgr;
        // private Mock<IDeploymentGroupServer> _machineGroupServer;
        private Mock<IVstsAgentWebProxy> _vstsAgentWebProxy;
        private Mock<IAgentCertificateManager> _cert;

#if OS_WINDOWS
        private Mock<IWindowsServiceControlManager> _serviceControlManager;
#endif

#if !OS_WINDOWS
        private Mock<ILinuxServiceControlManager> _serviceControlManager;
#endif

        private Mock<IRSAKeyManager> _rsaKeyManager;
        private ICapabilitiesManager _capabilitiesManager;
        // private DeploymentGroupAgentConfigProvider _deploymentGroupAgentConfigProvider;
        private string _expectedToken = "expectedToken";
        private string _expectedServerUrl = "https://localhost";
        private string _expectedVSTSServerUrl = "https://L0ConfigTest.visualstudio.com";
        private string _expectedAgentName = "expectedAgentName";
        private string _expectedPoolName = "poolName";
        private string _expectedCollectionName = "testCollectionName";
        private string _expectedProjectName = "testProjectName";
        private string _expectedProjectId = "edf3f94e-d251-49df-bfce-602d6c967409";
        private string _expectedMachineGroupName = "testMachineGroupName";
        private string _expectedAuthType = "pat";
        private string _expectedWorkFolder = "_work";
        private int _expectedPoolId = 1;
        private int _expectedDeploymentMachineId = 81;
        private RSACryptoServiceProvider rsa = null;
        private AgentSettings _configMgrAgentSettings = new AgentSettings();

        public ConfigurationManagerL0()
        {
            _agentServer = new Mock<IAgentServer>();
            _locationServer = new Mock<ILocationServer>();
            _credMgr = new Mock<ICredentialManager>();
            _promptManager = new Mock<IPromptManager>();
            _store = new Mock<IConfigurationStore>();
            _extnMgr = new Mock<IExtensionManager>();
            _rsaKeyManager = new Mock<IRSAKeyManager>();
            // _machineGroupServer = new Mock<IDeploymentGroupServer>();
            _vstsAgentWebProxy = new Mock<IVstsAgentWebProxy>();
            _cert = new Mock<IAgentCertificateManager>();

#if OS_WINDOWS
            _serviceControlManager = new Mock<IWindowsServiceControlManager>();
#endif

#if !OS_WINDOWS
            _serviceControlManager = new Mock<ILinuxServiceControlManager>();
#endif

            _capabilitiesManager = new CapabilitiesManager();

            var expectedAgent = new TaskAgent(_expectedAgentName) { Id = 1 };
            var expectedDeploymentMachine = new DeploymentMachine() { Agent = expectedAgent, Id = _expectedDeploymentMachineId };
            expectedAgent.Authorization = new TaskAgentAuthorization
            {
                ClientId = Guid.NewGuid(),
                AuthorizationUrl = new Uri("http://localhost:8080/tfs"),
            };

            var connectionData = new ConnectionData()
            {
                InstanceId = Guid.NewGuid(),
                DeploymentType = DeploymentFlags.Hosted,
                DeploymentId = Guid.NewGuid()
            };
            _agentServer.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>())).Returns(Task.FromResult<object>(null));
            _locationServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
            _locationServer.Setup(x => x.GetConnectionDataAsync()).Returns(Task.FromResult<ConnectionData>(connectionData));
            // _machineGroupServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
            // _machineGroupServer.Setup(x => x.UpdateDeploymentTargetsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<DeploymentMachine>>()));
            // _machineGroupServer.Setup(x => x.AddDeploymentTargetAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DeploymentMachine>())).Returns(Task.FromResult(expectedDeploymentMachine));
            // _machineGroupServer.Setup(x => x.ReplaceDeploymentTargetAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DeploymentMachine>())).Returns(Task.FromResult(expectedDeploymentMachine));
            // _machineGroupServer.Setup(x => x.GetDeploymentTargetsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(new List<DeploymentMachine>() { }));
            // _machineGroupServer.Setup(x => x.DeleteDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult<object>(null));

            _store.Setup(x => x.IsConfigured()).Returns(false);
            _store.Setup(x => x.HasCredentials()).Returns(false);
            _store.Setup(x => x.GetSettings()).Returns(() => _configMgrAgentSettings);

            _store.Setup(x => x.SaveSettings(It.IsAny<AgentSettings>()))
                .Callback((AgentSettings settings) =>
                {
                    _configMgrAgentSettings = settings;
                });

            _credMgr.Setup(x => x.GetCredentialProvider(It.IsAny<string>())).Returns(new TestAgentCredential());

#if !OS_WINDOWS
            _serviceControlManager.Setup(x => x.GenerateScripts(It.IsAny<AgentSettings>()));
#endif

            var expectedPools = new List<TaskAgentPool>() { new TaskAgentPool(_expectedPoolName) { Id = _expectedPoolId } };
            _agentServer.Setup(x => x.GetAgentPoolsAsync(It.IsAny<string>(), It.IsAny<TaskAgentPoolType>())).Returns(Task.FromResult(expectedPools));

            var expectedAgents = new List<TaskAgent>();
            _agentServer.Setup(x => x.GetAgentsAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(expectedAgents));

            _agentServer.Setup(x => x.AddAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));
            _agentServer.Setup(x => x.UpdateAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));

            rsa = new RSACryptoServiceProvider(2048);

            _rsaKeyManager.Setup(x => x.CreateKey()).Returns(rsa);
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<ICredentialManager>(_credMgr.Object);
            tc.SetSingleton<IPromptManager>(_promptManager.Object);
            tc.SetSingleton<IConfigurationStore>(_store.Object);
            tc.SetSingleton<IExtensionManager>(_extnMgr.Object);
            tc.SetSingleton<IAgentServer>(_agentServer.Object);
            tc.SetSingleton<ILocationServer>(_locationServer.Object);
            // tc.SetSingleton<IDeploymentGroupServer>(_machineGroupServer.Object);
            tc.SetSingleton<ICapabilitiesManager>(_capabilitiesManager);
            tc.SetSingleton<IVstsAgentWebProxy>(_vstsAgentWebProxy.Object);
            tc.SetSingleton<IAgentCertificateManager>(_cert.Object);

#if OS_WINDOWS
            tc.SetSingleton<IWindowsServiceControlManager>(_serviceControlManager.Object);
#else
            tc.SetSingleton<ILinuxServiceControlManager>(_serviceControlManager.Object);
#endif

            tc.SetSingleton<IRSAKeyManager>(_rsaKeyManager.Object);

            return tc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public async Task CanEnsureConfigure()
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
#if !OS_WINDOWS
                       "--acceptteeeula", 
#endif                       
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

                _extnMgr.Setup(x => x.GetExtensions<IConfigurationProvider>()).Returns(GetConfigurationProviderList(tc));

                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                await configManager.ConfigureAsync(command);

                _store.Setup(x => x.IsConfigured()).Returns(true);

                trace.Info("Configured, verifying all the parameter value");
                var s = configManager.LoadSettings();
                Assert.NotNull(s);
                Assert.True(s.ServerUrl.Equals(_expectedServerUrl));
                Assert.True(s.AgentName.Equals(_expectedAgentName));
                Assert.True(s.PoolId.Equals(_expectedPoolId));
                Assert.True(s.WorkFolder.Equals(_expectedWorkFolder));

                // validate GetAgentPoolsAsync gets called once with automation pool type
                _agentServer.Verify(x => x.GetAgentPoolsAsync(It.IsAny<string>(), It.Is<TaskAgentPoolType>(p => p == TaskAgentPoolType.Automation)), Times.Once);

                // validate GetAgentPoolsAsync not called with deployment pool type
                _agentServer.Verify(x => x.GetAgentPoolsAsync(It.IsAny<string>(), It.Is<TaskAgentPoolType>(p => p == TaskAgentPoolType.Deployment)), Times.Never);

                // For build and release agent / deployment pool, tags logic should not get trigger;
                // _machineGroupServer.Verify(x =>
                //      x.UpdateDeploymentTargetsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<DeploymentMachine>>()), Times.Never);
            }
        }


        // Init the Agent Config Provider
        private List<IConfigurationProvider> GetConfigurationProviderList(TestHostContext tc)
        {
            IConfigurationProvider buildReleasesAgentConfigProvider = new BuildReleasesAgentConfigProvider();
            buildReleasesAgentConfigProvider.Initialize(tc);

            return new List<IConfigurationProvider> { buildReleasesAgentConfigProvider };
        }
        // TODO Unit Test for IsConfigured - Rename config file and make sure it returns false
    }
}
