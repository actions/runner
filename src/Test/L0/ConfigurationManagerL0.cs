using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    using Microsoft.VisualStudio.Services.Agent.Configuration;

    public class ConfigurationManagerL0
    {
        private Mock<IAgentServer> _agentServer;
        private Mock<ICredentialManager> _credMgr;
        private Mock<IConsoleWizard> _reader;
        private Mock<IConfigurationStore> _store;
        private string _expectedToken = "expectedToken";
        private string _expectedServerUrl = "https://localhost";
        private string _expectedAgentName = "expectedAgentName";
        private string _expectedPoolName = "poolName";
        private string _expectedAuthType = "pat";
        private string _expectedWorkFolder = "_work";

        public ConfigurationManagerL0()
        {
            _agentServer = new Mock<IAgentServer>();
            _credMgr = new Mock<ICredentialManager>();
            _reader = new Mock<IConsoleWizard>();
            _store = new Mock<IConfigurationStore>();

            _agentServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
            
            _store.Setup(x => x.IsConfigured()).Returns(false);
            _store.Setup(x => x.HasCredentials()).Returns(false);
            _store.Setup(x => x.GetSettings()).Returns(
                new AgentSettings { 
                    ServerUrl = _expectedServerUrl,
                    AgentName = _expectedAgentName,
                    PoolName = _expectedPoolName,
                    WorkFolder = _expectedWorkFolder
                });

            _credMgr.Setup(x => x.GetCredentialProvider(It.IsAny<string>())).Returns(new TestAgentCredential());

            _reader.Setup(x => x.ReadValue(
                    "work",
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(),   // secret
                    It.IsAny<string>(), // defaultValue
                    It.IsAny<Func<string, bool>>(), // validator
                    It.IsAny<Dictionary<string, string>>(), //validator
                    false // unattended
                )).Returns(_expectedWorkFolder);

            _reader.Setup(x => x.ReadValue(
                    "auth",
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(),   // secret
                    It.IsAny<string>(), // defaultValue
                    It.IsAny<Func<string, bool>>(), // validator
                    It.IsAny<Dictionary<String, String>>(), //validator
                    false // unattended
                )).Returns(_expectedAuthType);

            _reader.Setup(x => x.ReadValue(
                    "url",
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(),   // secret
                    It.IsAny<string>(), // defaultValue
                    It.IsAny<Func<string, bool>>(), // validator
                    It.IsAny<Dictionary<String, String>>(), //validator
                    false // unattended
                )).Returns(_expectedServerUrl);

            _reader.Setup(x => x.ReadValue(
                    "agent",
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(),   // secret
                    It.IsAny<string>(), // defaultValue
                    It.IsAny<Func<string, bool>>(), // validator
                    It.IsAny<Dictionary<String, String>>(), //validator
                    false // unattended
                )).Returns(_expectedAgentName);

            _reader.Setup(x => x.ReadValue(
                    "pool",
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(),   // secret
                    It.IsAny<string>(), // defaultValue
                    It.IsAny<Func<string, bool>>(), // validator
                    It.IsAny<Dictionary<String, String>>(), //validator
                    false // unattended
                )).Returns(_expectedPoolName);
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(nameof(ConfigurationManagerL0), testName);
            tc.SetSingleton<ICredentialManager>(this._credMgr.Object);
            tc.SetSingleton<IConsoleWizard>(_reader.Object);
            tc.SetSingleton<IConfigurationStore>(_store.Object);
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
                TraceSource trace = tc.GetTrace();

                trace.Info("Creating config manager");
                IConfigurationManager configManager = new ConfigurationManager();
                configManager.Initialize(tc);

                trace.Info("Creating command line parser");
                CommandLineParser clp = new CommandLineParser(tc);

                trace.Info("Preparing command line arguments");
                clp.Parse(
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
                
                var expectedPools = new List<TaskAgentPool>() { new TaskAgentPool(_expectedPoolName) { Id = 1 } };
                _agentServer.Setup(x => x.GetAgentPoolsAsync(It.IsAny<string>())).Returns(Task.FromResult(expectedPools));
                
                var expectedAgents = new List<TaskAgent>();
                _agentServer.Setup(x => x.GetAgentsAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(expectedAgents));
                
                var expectedAgent = new TaskAgent(_expectedAgentName) { Id = 1 };
                _agentServer.Setup(x => x.AddAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));
                _agentServer.Setup(x => x.UpdateAgentAsync(It.IsAny<int>(), It.IsAny<TaskAgent>())).Returns(Task.FromResult(expectedAgent));
                
                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                configManager.ConfigureAsync(clp.Args, false);

                _store.Setup(x => x.IsConfigured()).Returns(true);
                
                trace.Info("Configured, verifying all the parameter value");
                var s = configManager.LoadSettings();
                Assert.True(s.ServerUrl.Equals(_expectedServerUrl));
                Assert.True(s.AgentName.Equals(_expectedAgentName));
                Assert.True(s.PoolName.Equals(_expectedPoolName));
                Assert.True(s.WorkFolder.Equals(_expectedWorkFolder));
            }
        }

        // TODO Unit Test for IsConfigured - Rename config file and make sure it returns false

    }
}