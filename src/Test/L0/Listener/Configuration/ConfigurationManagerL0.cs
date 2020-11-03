using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Common.Util;
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

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class ConfigurationManagerL0
    {
        private Mock<IRunnerServer> _runnerServer;
        private Mock<ILocationServer> _locationServer;
        private Mock<ICredentialManager> _credMgr;
        private Mock<IPromptManager> _promptManager;
        private Mock<IConfigurationStore> _store;
        private Mock<IExtensionManager> _extnMgr;

#if OS_WINDOWS
        private Mock<IWindowsServiceControlManager> _serviceControlManager;
#endif

#if !OS_WINDOWS
        private Mock<ILinuxServiceControlManager> _serviceControlManager;
#endif

        private Mock<IRSAKeyManager> _rsaKeyManager;
        private string _expectedToken = "expectedToken";
        private string _expectedServerUrl = "https://codedev.ms";
        private string _expectedAgentName = "expectedAgentName";
        private string _defaultRunnerGroupName = "defaultRunnerGroup";
        private string _secondRunnerGroupName = "secondRunnerGroup";
        private string _expectedAuthType = "pat";
        private string _expectedWorkFolder = "_work";
        private int _defaultRunnerGroupId = 1;
        private int _secondRunnerGroupId = 2;
        private RSACryptoServiceProvider rsa = null;
        private RunnerSettings _configMgrAgentSettings = new RunnerSettings();

        public ConfigurationManagerL0()
        {
            _runnerServer = new Mock<IRunnerServer>();
            _locationServer = new Mock<ILocationServer>();
            _credMgr = new Mock<ICredentialManager>();
            _promptManager = new Mock<IPromptManager>();
            _store = new Mock<IConfigurationStore>();
            _extnMgr = new Mock<IExtensionManager>();
            _rsaKeyManager = new Mock<IRSAKeyManager>();

#if OS_WINDOWS
            _serviceControlManager = new Mock<IWindowsServiceControlManager>();
#endif

#if !OS_WINDOWS
            _serviceControlManager = new Mock<ILinuxServiceControlManager>();
#endif

            var expectedAgent = new TaskAgent(_expectedAgentName) { Id = 1 };
            expectedAgent.Authorization = new TaskAgentAuthorization
            {
                ClientId = Guid.NewGuid(),
                AuthorizationUrl = new Uri("http://localhost:8080/pipelines"),
            };

            var connectionData = new ConnectionData()
            {
                InstanceId = Guid.NewGuid(),
                DeploymentType = DeploymentFlags.Hosted,
                DeploymentId = Guid.NewGuid()
            };
            _runnerServer.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<VssCredentials>())).Returns(Task.FromResult<object>(null));
            _locationServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
            _locationServer.Setup(x => x.GetConnectionDataAsync()).Returns(Task.FromResult<ConnectionData>(connectionData));

            _store.Setup(x => x.IsConfigured()).Returns(false);
            _store.Setup(x => x.HasCredentials()).Returns(false);
            _store.Setup(x => x.GetSettings()).Returns(() => _configMgrAgentSettings);

            _store.Setup(x => x.SaveSettings(It.IsAny<RunnerSettings>()))
                .Callback((RunnerSettings settings) =>
                {
                    _configMgrAgentSettings = settings;
                });

            _credMgr.Setup(x => x.GetCredentialProvider(It.IsAny<string>())).Returns(new TestRunnerCredential());

#if !OS_WINDOWS
            _serviceControlManager.Setup(x => x.GenerateScripts(It.IsAny<RunnerSettings>()));
#endif

            var expectedPools = new List<TaskAgentPool>() { new TaskAgentPool(_defaultRunnerGroupName) { Id = _defaultRunnerGroupId, IsInternal = true }, new TaskAgentPool(_secondRunnerGroupName) { Id = _secondRunnerGroupId } };
            _runnerServer.Setup(x => x.GetAgentPoolsAsync(It.IsAny<string>(), It.IsAny<TaskAgentPoolType>())).Returns(Task.FromResult(expectedPools));

            var expectedAgents = new List<TaskAgent>();
            _runnerServer.Setup(x => x.GetAgentsAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(expectedAgents));

            _runnerServer.Setup(x => x.AddAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));
            _runnerServer.Setup(x => x.ReplaceAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));

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
            tc.SetSingleton<IRunnerServer>(_runnerServer.Object);
            tc.SetSingleton<ILocationServer>(_locationServer.Object);

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

                var userLabels = "userlabel1,userlabel2";

                trace.Info("Preparing command line arguments");
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                       "configure",                
                       "--url", _expectedServerUrl,
                       "--name", _expectedAgentName,
                       "--runnergroup", _secondRunnerGroupName,
                       "--work", _expectedWorkFolder,
                       "--auth", _expectedAuthType,
                       "--token", _expectedToken,
                       "--labels", userLabels
                    });
                trace.Info("Constructed.");
                _store.Setup(x => x.IsConfigured()).Returns(false);
                _configMgrAgentSettings = null;

                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                await configManager.ConfigureAsync(command);

                _store.Setup(x => x.IsConfigured()).Returns(true);

                trace.Info("Configured, verifying all the parameter value");
                var s = configManager.LoadSettings();
                Assert.NotNull(s);
                Assert.True(s.ServerUrl.Equals(_expectedServerUrl));
                Assert.True(s.AgentName.Equals(_expectedAgentName));
                Assert.True(s.PoolId.Equals(_secondRunnerGroupId));
                Assert.True(s.WorkFolder.Equals(_expectedWorkFolder));

                // validate GetAgentPoolsAsync gets called twice with automation pool type
                _runnerServer.Verify(x => x.GetAgentPoolsAsync(It.IsAny<string>(), It.Is<TaskAgentPoolType>(p => p == TaskAgentPoolType.Automation)), Times.Exactly(2));

                var expectedLabels = new List<string>() { "self-hosted", VarUtil.OS, VarUtil.OSArchitecture};
                expectedLabels.AddRange(userLabels.Split(",").ToList());

                _runnerServer.Verify(x => x.AddAgentAsync(It.IsAny<int>(), It.Is<TaskAgent>(a => a.Labels.Select(x => x.Name).ToHashSet().SetEquals(expectedLabels))), Times.Once);
            }
        }
    }
}
