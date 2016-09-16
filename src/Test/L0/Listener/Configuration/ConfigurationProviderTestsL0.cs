using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
    using WebApi;

    public class ConfigurationProviderTestsL0
    {
        private Mock<IAgentServer> _agentServer;
        private Mock<IMachineGroupServer> _machineGroupServer;
        private Mock<IPromptManager> _promptManager;
        private string _collectionName = "testCollectionName";
        private int _expectedPoolId = 7;
        private string _projectName = "testProjectName";
        private string _machineGroupName = "testMachineGroup";

        public ConfigurationProviderTestsL0()
        {
            _agentServer = new Mock<IAgentServer>();
            _promptManager = new Mock<IPromptManager>();
            _machineGroupServer = new Mock<IMachineGroupServer>();

            _agentServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
            _machineGroupServer.Setup(x => x.ConnectAsync(It.IsAny<VssConnection>())).Returns(Task.FromResult<object>(null));
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<IAgentServer>(_agentServer.Object);
            tc.EnqueueInstance<IAgentServer>(_agentServer.Object);
            tc.SetSingleton<IMachineGroupServer>(_machineGroupServer.Object);
            tc.EnqueueInstance<IMachineGroupServer>(_machineGroupServer.Object);
            tc.SetSingleton<IPromptManager>(_promptManager.Object);

            return tc;
        }

        /*
         * This test case ensures the flow for Deployment Agent Configuration for on-prem tfs, where collection name is required
        */
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void EnsureDeploymentConfigProviderWorksFineForOnPrem()
        {
            using (TestHostContext tc = CreateTestContext())
            {
                Tracing trace = tc.GetTrace();

                trace.Info("Creating Deployment Config Provide");
                IConfigurationProvider machineGroupAgentConfigProvider = new MachineGroupAgentConfigProvider();

                trace.Info("Init the deployment provider");
                machineGroupAgentConfigProvider.Initialize(tc);

                trace.Info("Preparing command line arguments");
                string expectedBaseUrl = "https://localhost:8080/tfs";
                string serverUrl = string.Format("https://localhost:8080/tfs/{0}/{1}", _collectionName, _projectName);

                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                       "configure",
                       "--url", serverUrl,
                       "--machinegroupname",_machineGroupName
                    });

                string expectedProjectName = null;
                string expectedMachineGroup = null;

                var expectedMachineGroups = new List<DeploymentMachineGroup>() { new DeploymentMachineGroup() { Pool = new TaskAgentPoolReference(new Guid(), _expectedPoolId), Name = "Test-MachineGroup" } };
               _machineGroupServer.Setup(x => x.GetDeploymentMachineGroupsAsync(It.IsAny<string>(), It.IsAny<string>())).Callback<string, string>(
                    (proj, machineGrp) =>
                    {
                        expectedProjectName = proj ;
                        expectedMachineGroup = machineGrp;
                    }).Returns(Task.FromResult(expectedMachineGroups));

                string baseUrl = machineGroupAgentConfigProvider.GetServerUrl(command);
                trace.Info("Verify base url");
                Assert.True(expectedBaseUrl.Equals(baseUrl));

                machineGroupAgentConfigProvider.TestConnectionAsync(serverUrl,
                    new TestAgentCredential().GetVssCredentials(tc));
                int poolId = machineGroupAgentConfigProvider.GetPoolId(command).Result;

                trace.Info("Verifying poolId returned by deployment provider");
                Assert.True(poolId.Equals(_expectedPoolId));

                trace.Info("Verifying GetAgentQueuesAsync get called with correct project name");
                Assert.True(_projectName.Equals(expectedProjectName));

                trace.Info("Verifying GetAgentQueuesAsync get called with correct machineGroup name");
                Assert.True(_machineGroupName.Equals(expectedMachineGroup));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void ShouldThrowForVSTSUrlWithoutProjectForDeploymentAgentScenario()
        {
            string vstsUrlWithoutProject = "https://L0ConfigTest.visualstudio.com";
            using (TestHostContext tc = CreateTestContext())
            {
                Tracing trace = tc.GetTrace();

                trace.Info("Creating Deployment Config Provide");
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                       "configure",
                       "--url", vstsUrlWithoutProject
                    });

                IConfigurationProvider machineGroupAgentConfigProvider = new MachineGroupAgentConfigProvider();
                machineGroupAgentConfigProvider.Initialize(tc);
                try
                {
                    machineGroupAgentConfigProvider.GetServerUrl(command);
                    Assert.True(false,
                        string.Format("Url validation should throw, not project provided with {0}",
                            vstsUrlWithoutProject));
                }
                catch (Exception)
                {
                    // Exceptions are expected here
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void ShouldThrowForOnPremUrlWithoutCollectionAndProjectForDeploymentAgentScenario()
        {
            string tfsUrlWithoutCollectionAndProject = "https://localhost:8080/tfs";
            using (TestHostContext tc = CreateTestContext())
            {
                Tracing trace = tc.GetTrace();

                trace.Info("Creating Deployment Config Provide");
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                       "configure",
                       "--url", tfsUrlWithoutCollectionAndProject
                    });

                IConfigurationProvider machineGroupAgentConfigProvider = new MachineGroupAgentConfigProvider();
                machineGroupAgentConfigProvider.Initialize(tc);
                try
                {
                    machineGroupAgentConfigProvider.GetServerUrl(command);
                    Assert.True(false,
                        string.Format("Url validation should throw, not project provided with {0}",
                            tfsUrlWithoutCollectionAndProject));
                }
                catch (Exception)
                {
                    // Exceptions are expected here
                }
            }
        }

    }
}
