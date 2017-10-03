using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
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

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public class ConfigurationManagerL0
    {
        private Mock<IAgentServer> _agentServer;
        private Mock<ICredentialManager> _credMgr;
        private Mock<IPromptManager> _promptManager;
        private Mock<IConfigurationStore> _store;
        private Mock<IExtensionManager> _extnMgr;
        private Mock<IDeploymentGroupServer> _machineGroupServer;
        private Mock<INetFrameworkUtil> _netFrameworkUtil;
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
        private DeploymentGroupAgentConfigProvider _deploymentGroupAgentConfigProvider;
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
        private RSA rsa = null;
        private AgentSettings _configMgrAgentSettings = new AgentSettings();

        public ConfigurationManagerL0()
        {
            _agentServer = new Mock<IAgentServer>();
            _credMgr = new Mock<ICredentialManager>();
            _promptManager = new Mock<IPromptManager>();
            _store = new Mock<IConfigurationStore>();
            _extnMgr = new Mock<IExtensionManager>();
            _rsaKeyManager = new Mock<IRSAKeyManager>();
            _machineGroupServer = new Mock<IDeploymentGroupServer>();
            _netFrameworkUtil = new Mock<INetFrameworkUtil>();
            _vstsAgentWebProxy = new Mock<IVstsAgentWebProxy>();
            _cert = new Mock<IAgentCertificateManager>();

#if OS_WINDOWS
            _serviceControlManager = new Mock<IWindowsServiceControlManager>();
#endif

#if !OS_WINDOWS
            _serviceControlManager = new Mock<ILinuxServiceControlManager>();
#endif

#if !OS_WINDOWS
            string eulaFile = Path.Combine(IOUtil.GetExternalsPath(), Constants.Path.TeeDirectory, "license.html");
            Directory.CreateDirectory(IOUtil.GetExternalsPath());
            Directory.CreateDirectory(Path.Combine(IOUtil.GetExternalsPath(), Constants.Path.TeeDirectory));
            File.WriteAllText(eulaFile, "testeulafile");
#endif

            _capabilitiesManager = new CapabilitiesManager();

            var expectedAgent = new TaskAgent(_expectedAgentName) { Id = 1 };
            var expectedDeploymentMachine = new DeploymentMachine() { Agent = expectedAgent, Id = _expectedDeploymentMachineId };
            expectedAgent.Authorization = new TaskAgentAuthorization
            {
                ClientId = Guid.NewGuid(),
                AuthorizationUrl = new Uri("http://localhost:8080/tfs"),
            };

            _agentServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
            _machineGroupServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
            _machineGroupServer.Setup(x => x.UpdateDeploymentMachinesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<DeploymentMachine>>()));
            _machineGroupServer.Setup(x => x.AddDeploymentMachineAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DeploymentMachine>())).Returns(Task.FromResult(expectedDeploymentMachine));
            _machineGroupServer.Setup(x => x.ReplaceDeploymentMachineAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DeploymentMachine>())).Returns(Task.FromResult(expectedDeploymentMachine));
            _machineGroupServer.Setup(x => x.GetDeploymentMachinesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(new List<DeploymentMachine>() { }));
            _machineGroupServer.Setup(x => x.DeleteDeploymentMachineAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult<object>(null));
            _netFrameworkUtil.Setup(x => x.Test(It.IsAny<Version>())).Returns(true);

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
            _agentServer.Setup(x => x.GetAgentPoolsAsync(It.IsAny<string>())).Returns(Task.FromResult(expectedPools));

            var expectedAgents = new List<TaskAgent>();
            _agentServer.Setup(x => x.GetAgentsAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(expectedAgents));

            _agentServer.Setup(x => x.AddAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));
            _agentServer.Setup(x => x.UpdateAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));

            rsa = RSA.Create();
            rsa.KeySize = 2048;

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
            tc.SetSingleton<IDeploymentGroupServer>(_machineGroupServer.Object);
            tc.SetSingleton<INetFrameworkUtil>(_netFrameworkUtil.Object);
            tc.SetSingleton<ICapabilitiesManager>(_capabilitiesManager);
            tc.SetSingleton<IVstsAgentWebProxy>(_vstsAgentWebProxy.Object);
            tc.SetSingleton<IAgentCertificateManager>(_cert.Object);

#if OS_WINDOWS
            tc.SetSingleton<IWindowsServiceControlManager>(_serviceControlManager.Object);
#else
            tc.SetSingleton<ILinuxServiceControlManager>(_serviceControlManager.Object);
#endif

            tc.SetSingleton<IRSAKeyManager>(_rsaKeyManager.Object);
            tc.EnqueueInstance<IAgentServer>(_agentServer.Object);
            tc.EnqueueInstance<IDeploymentGroupServer>(_machineGroupServer.Object);

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

                // For build and release agent, tags logic should not get trigger
                _machineGroupServer.Verify(x =>
                     x.UpdateDeploymentMachinesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<DeploymentMachine>>()), Times.Never);
            }
        }


        /*
         * Agent configuartion as deployment agent against VSTS account
         * Collectioion name is not required
         */
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public async Task CanEnsureMachineGroupAgentConfigureVSTSScenario()
        {
            using (TestHostContext tc = CreateTestContext())
            {
                Tracing trace = tc.GetTrace();

                trace.Info("Creating config manager");
                IConfigurationManager configManager = new ConfigurationManager();
                configManager.Initialize(tc);

                trace.Info("Preparing command line arguments for vsts scenario");
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                        "configure",
#if !OS_WINDOWS
                       "--acceptteeeula",
#endif
                        "--machinegroup",
                        "--url", _expectedVSTSServerUrl,
                        "--agent", _expectedAgentName,
                        "--projectname", _expectedProjectName,
                        "--machinegroupname", _expectedMachineGroupName,
                        "--work", _expectedWorkFolder,
                        "--auth", _expectedAuthType,
                        "--token", _expectedToken
                    });
                trace.Info("Constructed.");

                _store.Setup(x => x.IsConfigured()).Returns(false);
                _configMgrAgentSettings = null;

                _extnMgr.Setup(x => x.GetExtensions<IConfigurationProvider>()).Returns(GetConfigurationProviderList(tc));
                _machineGroupServer.Setup(x => x.GetDeploymentGroupsAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(GetDeploymentGroups(18, 27)));

                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                await configManager.ConfigureAsync(command);

                _store.Setup(x => x.IsConfigured()).Returns(true);

                trace.Info("Configured, verifying all the parameter value");
                var s = configManager.LoadSettings();
                Assert.NotNull(s);
                Assert.True(s.ServerUrl.Equals(_expectedVSTSServerUrl, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(s.AgentName.Equals(_expectedAgentName));
                Assert.True(s.PoolId.Equals(27));
                Assert.True(s.WorkFolder.Equals(_expectedWorkFolder));
                Assert.True(s.MachineGroupId.Equals(0));
                Assert.True(s.DeploymentGroupId.Equals(18));
                Assert.Null(s.ProjectName);
                Assert.True(s.ProjectId.Equals(_expectedProjectId));

                // Tags logic should not get trigger
                _machineGroupServer.Verify(x =>
                    x.UpdateDeploymentMachinesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<DeploymentMachine>>()), Times.Never);
            }
        }

        /*
        * Agent configuartion as deployment agent against on prem tfs
        * Collectioion name is required
        */
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public async Task CanEnsureMachineGroupAgentConfigureOnPremScenario()
        {
            using (TestHostContext tc = CreateTestContext())
            {
                Tracing trace = tc.GetTrace();

                trace.Info("Creating config manager");
                IConfigurationManager configManager = new ConfigurationManager();
                configManager.Initialize(tc);

                var onPremTfsUrl = "http://localtfs:8080/tfs";

                trace.Info("Preparing command line arguments for vsts scenario");
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                        "configure",
#if !OS_WINDOWS
                       "--acceptteeeula",
#endif
                        "--deploymentgroup",
                        "--url", onPremTfsUrl,
                        "--agent", _expectedAgentName,
                        "--collectionname", _expectedCollectionName,
                        "--projectname", _expectedProjectName,
                        "--deploymentgroupname", _expectedMachineGroupName,
                        "--work", _expectedWorkFolder,
                        "--auth", _expectedAuthType,
                        "--token", _expectedToken
                    });
                trace.Info("Constructed.");

                _store.Setup(x => x.IsConfigured()).Returns(false);
                _configMgrAgentSettings = null;

                _extnMgr.Setup(x => x.GetExtensions<IConfigurationProvider>()).Returns(GetConfigurationProviderList(tc));

                _machineGroupServer.Setup(x => x.GetDeploymentGroupsAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(GetDeploymentGroups(3, 7)));

                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                await configManager.ConfigureAsync(command);

                _store.Setup(x => x.IsConfigured()).Returns(true);

                trace.Info("Configured, verifying all the parameter value");
                var s = configManager.LoadSettings();
                Assert.NotNull(s);
                Assert.True(s.ServerUrl.Equals(onPremTfsUrl));
                Assert.True(s.AgentName.Equals(_expectedAgentName));
                Assert.True(s.PoolId.Equals(7));
                Assert.True(s.WorkFolder.Equals(_expectedWorkFolder));
                Assert.True(s.MachineGroupId.Equals(0));
                Assert.True(s.DeploymentGroupId.Equals(3));
                Assert.Null(s.ProjectName);
                Assert.True(s.ProjectId.Equals(_expectedProjectId));

                // Tags logic should not get trigger
                _machineGroupServer.Verify(x =>
                    x.UpdateDeploymentMachinesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<DeploymentMachine>>()), Times.Never);
            }
        }

        /*
        * Agent configuartion as deployment agent against VSTS account
        * Collectioion name is not required
        */
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public async Task CanEnsureMachineGroupAgentConfigureVSTSScenarioWithTags()
        {
            Guid receivedProjectId;
            string expectedProcessedTags = string.Empty;
            string tags = "Tag3, ,, Tag4  , , ,  Tag1,  , tag3 ";
            string expectedTags = "Tag3,Tag4,Tag1";
            int receivedMachineId = -1;
            int expectedDeploymentGroupId = 7;
            int receivedDeploymentGroupId = -1;

            _machineGroupServer.Setup(x =>
                x.UpdateDeploymentMachinesAsync(It.IsAny<Guid>(), It.IsAny<int>(),
                    It.IsAny<List<DeploymentMachine>>())).Callback((Guid project, int deploymentGroupId, List<DeploymentMachine> deploymentMachine) =>
                    {
                        receivedProjectId = project;
                        expectedProcessedTags = string.Join(",", deploymentMachine.FirstOrDefault().Tags.ToArray());
                        receivedMachineId = deploymentMachine.FirstOrDefault().Id;
                        receivedDeploymentGroupId = deploymentGroupId;
                    }
                );

            using (TestHostContext tc = CreateTestContext())
            {
                Tracing trace = tc.GetTrace();

                trace.Info("Creating config manager");
                IConfigurationManager configManager = new ConfigurationManager();
                configManager.Initialize(tc);

                trace.Info("Preparing command line arguments for vsts scenario");
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                        "configure",
#if !OS_WINDOWS
                       "--acceptteeeula",
#endif
                        "--machinegroup",
                        "--adddeploymentgrouptags",
                        "--url", _expectedVSTSServerUrl,
                        "--agent", _expectedAgentName,
                        "--projectname", _expectedProjectName,
                        "--deploymentgroupname", _expectedMachineGroupName,
                        "--work", _expectedWorkFolder,
                        "--auth", _expectedAuthType,
                        "--token", _expectedToken,
                        "--deploymentgrouptags", tags
                    });
                trace.Info("Constructed.");

                _store.Setup(x => x.IsConfigured()).Returns(false);
                _configMgrAgentSettings = null;

                _extnMgr.Setup(x => x.GetExtensions<IConfigurationProvider>()).Returns(GetConfigurationProviderList(tc));


                _machineGroupServer.Setup(x => x.GetDeploymentGroupsAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(GetDeploymentGroups(expectedDeploymentGroupId, 3)));

                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                await configManager.ConfigureAsync(command);

                _store.Setup(x => x.IsConfigured()).Returns(true);

                trace.Info("Configured, verifying all the parameter value");
                var s = configManager.LoadSettings();
                Assert.NotNull(s);
                Assert.True(s.ServerUrl.Equals(_expectedVSTSServerUrl, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(s.AgentName.Equals(_expectedAgentName));
                Assert.True(s.PoolId.Equals(3));
                Assert.True(s.DeploymentGroupId.Equals(7));
                Assert.True(s.WorkFolder.Equals(_expectedWorkFolder));
                Assert.True(s.MachineGroupId.Equals(0));
                Assert.Null(s.ProjectName);
                Assert.True(s.ProjectId.Equals(_expectedProjectId));

                Assert.True(receivedProjectId.Equals(new Guid(_expectedProjectId)), "UpdateDeploymentMachinesGroupAsync should get call with correct project name");
                Assert.True(expectedTags.Equals(expectedProcessedTags), "Before applying the tags, should get processed ( Trim, Remove duplicate)");
                Assert.True(receivedMachineId.Equals(_expectedDeploymentMachineId), "UpdateDeploymentMachinesGroupAsync should get call with correct machine id");
                Assert.True(receivedDeploymentGroupId.Equals(expectedDeploymentGroupId), "UpdateDeploymentMachinesGroupAsync should get call with correct deployment group id");
                // Tags logic should get trigger
                _machineGroupServer.Verify(x =>
                    x.UpdateDeploymentMachinesAsync(It.IsAny<Guid>(), It.IsAny<int>(),
                        It.IsAny<List<DeploymentMachine>>()), Times.Once);
            }
        }

        private List<DeploymentGroup> GetDeploymentGroups(int dgId, int poolId)
        {
            var dgJson = "{'id':" + dgId.ToString() + ",'project':{'id':'" + _expectedProjectId + "','name':'Test-Project1'},'name':'ch-test','pool':{'id':" + poolId.ToString() + ",'scope':'0efb4611-d565-4cd1-9a64-7d6cb6d7d5f0'}}";
            var deploymentGroup = JsonConvert.DeserializeObject<DeploymentGroup>(dgJson);
            return new List<DeploymentGroup>() { deploymentGroup };
        }

        // Init the Agent Config Provider
        private List<IConfigurationProvider> GetConfigurationProviderList(TestHostContext tc)
        {
            IConfigurationProvider buildReleasesAgentConfigProvider = new BuildReleasesAgentConfigProvider();
            buildReleasesAgentConfigProvider.Initialize(tc);

            _deploymentGroupAgentConfigProvider = new DeploymentGroupAgentConfigProvider();
            _deploymentGroupAgentConfigProvider.Initialize(tc);

            return new List<IConfigurationProvider> { buildReleasesAgentConfigProvider, _deploymentGroupAgentConfigProvider };
        }
        // TODO Unit Test for IsConfigured - Rename config file and make sure it returns false

    }
}