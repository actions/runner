using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    using Microsoft.VisualStudio.Services.Agent.Configuration;

    public class ConfigurationManagerL0
    {
        private string expectedToken = "expectedToken";
        private string expectedServerUrl = "expectedServerUrl";
        private string expectedAgentName = "expectedAgentName";
        private string expectedPoolName = "poolName";
        private string expectedAuthType = "pat";
        private string expectedWorkFolder = "_work";

        public ConfigurationManagerL0()
        {
            this.agentCredentialManager = new Mock<IAgentCredentialManager>();
            this.configReader = new Mock<IConsoleWizard>();

            this.agentCredentialManager.Setup(x => x.Create(It.IsAny<AuthScheme>())).Returns(new TestAgentCredential());

            this.configReader.Setup(
                x =>
                x.GetConfigurationValue(
                    It.IsAny<IHostContext>(),
                    "WorkFolder",
                    It.IsAny<Dictionary<String, ArgumentMetaData>>(),
                    It.IsAny<Dictionary<String, String>>(),
                    false)).Returns(expectedWorkFolder);

            this.configReader.Setup(
                x =>
                x.GetConfigurationValue(
                    It.IsAny<IHostContext>(),
                    "AuthType",
                    It.IsAny<Dictionary<String, ArgumentMetaData>>(),
                    It.IsAny<Dictionary<String, String>>(),
                    false)).Returns(expectedAuthType);

            this.configReader.Setup(
                x =>
                x.GetConfigurationValue(
                    It.IsAny<IHostContext>(),
                    "ServerUrl",
                    It.IsAny<Dictionary<String, ArgumentMetaData>>(),
                    It.IsAny<Dictionary<String, String>>(),
                    false)).Returns(expectedServerUrl);

            this.configReader.Setup(
                x =>
                x.GetConfigurationValue(
                    It.IsAny<IHostContext>(),
                    "AgentName",
                    It.IsAny<Dictionary<String, ArgumentMetaData>>(),
                    It.IsAny<Dictionary<String, String>>(),
                    false)).Returns(expectedAgentName);

            this.configReader.Setup(
                x =>
                x.GetConfigurationValue(
                    It.IsAny<IHostContext>(),
                    "PoolName",
                    It.IsAny<Dictionary<String, ArgumentMetaData>>(),
                    It.IsAny<Dictionary<String, String>>(),
                    false)).Returns(expectedPoolName);
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext thc = new TestHostContext(nameof(ConfigurationManagerL0), testName);
            thc.RegisterService<IAgentCredentialManager>(this.agentCredentialManager.Object);
            thc.RegisterService<IConsoleWizard>(this.configReader.Object);

            return thc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void CanEnsureConfigure()
        {
            using (TestHostContext thc = CreateTestContext())
            {
                TraceSource trace = thc.GetTrace();

                IConfigurationManager configManager = new ConfigurationManager();
                CommandLineParser clp = new CommandLineParser(thc);

                trace.Info("Preparing command line arguments");
                clp.Parse(
                    new[]
                        {
                            "configure", "--Token", expectedToken, "--ServerUrl", expectedServerUrl, "--AgentName",
                            expectedAgentName, "--PoolName", expectedPoolName, "--authtype", expectedAuthType,
                            "--WorkFolder", expectedWorkFolder
                        });
                trace.Info("Constructed.");

                trace.Info("Ensuring all the required parameters are available in the command line parameter");
                var ensureConfigured = configManager.Configure(thc, clp.Args, false);
                Assert.True(ensureConfigured);

                trace.Info("Configured, verifying all the parameter value");
                var configuration = configManager.GetConfiguration();
                Assert.True(configuration.Setting.ServerUrl.Equals(expectedServerUrl));
                Assert.True(configuration.Setting.AgentName.Equals(expectedAgentName));
                Assert.True(configuration.Setting.PoolName.Equals(expectedPoolName));
                Assert.True(configuration.Setting.WorkFolder.Equals(expectedWorkFolder));
            }
        }

        // TODO Unit Test for IsConfigured - Rename config file and make sure it returns false

        private Mock<IAgentCredentialManager> agentCredentialManager;

        private Mock<IConsoleWizard> configReader;
    }
}