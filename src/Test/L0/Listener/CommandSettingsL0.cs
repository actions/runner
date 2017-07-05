using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Moq;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class CommandSettingsL0
    {
        private readonly Mock<IPromptManager> _promptManager = new Mock<IPromptManager>();
        private readonly Mock<ISecretMasker> _secretMasker = new Mock<ISecretMasker>();

        // It is sufficient to test one arg only. All individual args are tested by the PromptsFor___ methods.
        // The PromptsFor___ methods suffice to cover the interesting differences between each of the args.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsArg()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--agent", "some agent" });

                // Act.
                string actual = command.GetAgentName();

                // Assert.
                Assert.Equal("some agent", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsArgFromEnvVar()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                try
                {
                    // Arrange.
                    Environment.SetEnvironmentVariable("VSTS_AGENT_INPUT_AGENT", "some agent");
                    var command = new CommandSettings(hc, args: new string[0]);

                    // Act.
                    string actual = command.GetAgentName();

                    // Assert.
                    Assert.Equal("some agent", actual);
                    Assert.Equal(string.Empty, Environment.GetEnvironmentVariable("VSTS_AGENT_INPUT_AGENT") ?? string.Empty); // Should remove.
                    _secretMasker.Verify(x => x.AddValue(It.IsAny<string>()), Times.Never);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("VSTS_AGENT_INPUT_AGENT", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsArgSecretFromEnvVar()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                try
                {
                    // Arrange.
                    Environment.SetEnvironmentVariable("VSTS_AGENT_INPUT_TOKEN", "some secret token value");
                    var command = new CommandSettings(hc, args: new string[0]);

                    // Act.
                    string actual = command.GetToken();

                    // Assert.
                    Assert.Equal("some secret token value", actual);
                    Assert.Equal(string.Empty, Environment.GetEnvironmentVariable("VSTS_AGENT_INPUT_TOKEN") ?? string.Empty); // Should remove.
                    _secretMasker.Verify(x => x.AddValue("some secret token value"));
                }
                finally
                {
                    Environment.SetEnvironmentVariable("VSTS_AGENT_INPUT_TOKEN", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsCommandConfigure()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "configure" });

                // Act.
                bool actual = command.Configure;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsCommandRun()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "run" });

                // Act.
                bool actual = command.Run;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsCommandUnconfigure()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "remove" });

                // Act.
                bool actual = command.Unconfigure;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagAcceptTeeEula()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--acceptteeeula" });

                // Act.
                bool actual = command.GetAcceptTeeEula();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagCommit()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--commit" });

                // Act.
                bool actual = command.Commit;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagHelp()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--help" });

                // Act.
                bool actual = command.Help;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagReplace()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--replace" });

                // Act.
                bool actual = command.GetReplace();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagRunAsService()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--runasservice" });

                // Act.
                bool actual = command.GetRunAsService();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagUnattended()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--unattended" });

                // Act.
                bool actual = command.Unattended;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagUnattendedFromEnvVar()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                try
                {
                    // Arrange.
                    Environment.SetEnvironmentVariable("VSTS_AGENT_INPUT_UNATTENDED", "true");
                    var command = new CommandSettings(hc, args: new string[0]);

                    // Act.
                    bool actual = command.Unattended;

                    // Assert.
                    Assert.Equal(true, actual);
                    Assert.Equal(string.Empty, Environment.GetEnvironmentVariable("VSTS_AGENT_INPUT_UNATTENDED") ?? string.Empty); // Should remove.
                    _secretMasker.Verify(x => x.AddValue(It.IsAny<string>()), Times.Never);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("VSTS_AGENT_INPUT_UNATTENDED", null);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagVersion()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--version" });

                // Act.
                bool actual = command.Version;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PassesUnattendedToReadBool()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--unattended" });
                _promptManager
                    .Setup(x => x.ReadBool(
                        Constants.Agent.CommandLine.Flags.AcceptTeeEula, // argName
                        StringUtil.Loc("AcceptTeeEula"), // description
                        false, // defaultValue
                        true)) // unattended
                    .Returns(true);

                // Act.
                bool actual = command.GetAcceptTeeEula();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PassesUnattendedToReadValue()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--unattended" });
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Agent, // argName
                        StringUtil.Loc("AgentName"), // description
                        false, // secret
                        Environment.MachineName, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        true)) // unattended
                    .Returns("some agent");

                // Act.
                string actual = command.GetAgentName();

                // Assert.
                Assert.Equal("some agent", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForAcceptTeeEula()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadBool(
                        Constants.Agent.CommandLine.Flags.AcceptTeeEula, // argName
                        StringUtil.Loc("AcceptTeeEula"), // description
                        false, // defaultValue
                        false)) // unattended
                    .Returns(true);

                // Act.
                bool actual = command.GetAcceptTeeEula();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForAgent()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Agent, // argName
                        StringUtil.Loc("AgentName"), // description
                        false, // secret
                        Environment.MachineName, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("some agent");

                // Act.
                string actual = command.GetAgentName();

                // Assert.
                Assert.Equal("some agent", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForAuth()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Auth, // argName
                        StringUtil.Loc("AuthenticationType"), // description
                        false, // secret
                        "some default auth", // defaultValue
                        Validators.AuthSchemeValidator, // validator
                        false)) // unattended
                    .Returns("some auth");

                // Act.
                string actual = command.GetAuth("some default auth");

                // Assert.
                Assert.Equal("some auth", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForPassword()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Password, // argName
                        StringUtil.Loc("Password"), // description
                        true, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("some password");

                // Act.
                string actual = command.GetPassword();

                // Assert.
                Assert.Equal("some password", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForPool()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Pool, // argName
                        StringUtil.Loc("AgentMachinePoolNameLabel"), // description
                        false, // secret
                        "default", // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("some pool");

                // Act.
                string actual = command.GetPool();

                // Assert.
                Assert.Equal("some pool", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForReplace()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadBool(
                        Constants.Agent.CommandLine.Flags.Replace, // argName
                        StringUtil.Loc("Replace"), // description
                        false, // defaultValue
                        false)) // unattended
                    .Returns(true);

                // Act.
                bool actual = command.GetReplace();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForRunAsService()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadBool(
                        Constants.Agent.CommandLine.Flags.RunAsService, // argName
                        StringUtil.Loc("RunAgentAsServiceDescription"), // description
                        false, // defaultValue
                        false)) // unattended
                    .Returns(true);

                // Act.
                bool actual = command.GetRunAsService();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForToken()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Token, // argName
                        StringUtil.Loc("PersonalAccessToken"), // description
                        true, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("some token");

                // Act.
                string actual = command.GetToken();

                // Assert.
                Assert.Equal("some token", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForUrl()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Url, // argName
                        StringUtil.Loc("ServerUrl"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.ServerUrlValidator, // validator
                        false)) // unattended
                    .Returns("some url");

                // Act.
                string actual = command.GetUrl();

                // Assert.
                Assert.Equal("some url", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForUserName()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.UserName, // argName
                        StringUtil.Loc("UserName"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("some user name");

                // Act.
                string actual = command.GetUserName();

                // Assert.
                Assert.Equal("some user name", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForWindowsLogonAccount()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.WindowsLogonAccount, // argName
                        StringUtil.Loc("WindowsLogonAccountNameDescription"), // description
                        false, // secret
                        "some default account", // defaultValue
                        Validators.NTAccountValidator, // validator
                        false)) // unattended
                    .Returns("some windows logon account");

                // Act.
                string actual = command.GetWindowsLogonAccount("some default account", StringUtil.Loc("WindowsLogonAccountNameDescription"));

                // Assert.
                Assert.Equal("some windows logon account", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForWindowsLogonPassword()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                string accountName = "somewindowsaccount";
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.WindowsLogonPassword, // argName
                        StringUtil.Loc("WindowsLogonPasswordDescription", accountName), // description
                        true, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("some windows logon password");

                // Act.
                string actual = command.GetWindowsLogonPassword(accountName);

                // Assert.
                Assert.Equal("some windows logon password", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForWork()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Work, // argName
                        StringUtil.Loc("WorkFolderDescription"), // description
                        false, // secret
                        "_work", // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("some work");

                // Act.
                string actual = command.GetWork();

                // Assert.
                Assert.Equal("some work", actual);
            }
        }

        // It is sufficient to test one arg only. All individual args are tested by the PromptsFor___ methods.
        // The PromptsFor___ methods suffice to cover the interesting differences between each of the args.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsWhenEmpty()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--url", "" });
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Url, // argName
                        StringUtil.Loc("ServerUrl"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.ServerUrlValidator, // validator
                        false)) // unattended
                    .Returns("some url");

                // Act.
                string actual = command.GetUrl();

                // Assert.
                Assert.Equal("some url", actual);
            }
        }

        // It is sufficient to test one arg only. All individual args are tested by the PromptsFor___ methods.
        // The PromptsFor___ methods suffice to cover the interesting differences between each of the args.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsWhenInvalid()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--url", "notValid" });
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.Url, // argName
                        StringUtil.Loc("ServerUrl"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.ServerUrlValidator, // validator
                        false)) // unattended
                    .Returns("some url");

                // Act.
                string actual = command.GetUrl();

                // Assert.
                Assert.Equal("some url", actual);
            }
        }

        /*
         * Deployment Agent Tests
        */

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagDeploymentAgentWithBackCompat()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--machinegroup" });

                // Act.
                bool actual = command.DeploymentGroup;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagDeploymentAgent()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--deploymentgroup" });

                // Act.
                bool actual = command.DeploymentGroup;

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagAddDeploymentGroupTagsBackCompat()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--addmachinegrouptags" });

                // Act.
                bool actual = command.GetDeploymentGroupTagsRequired();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsFlagAddDeploymentGroupTags()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--adddeploymentgrouptags" });

                // Act.
                bool actual = command.GetDeploymentGroupTagsRequired();

                // Assert.
                Assert.True(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForProjectName()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.ProjectName, // argName
                        StringUtil.Loc("ProjectName"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("TestProject");

                // Act.
                string actual = command.GetProjectName(string.Empty);

                // Assert.
                Assert.Equal("TestProject", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForCollectionName()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.CollectionName, // argName
                        StringUtil.Loc("CollectionName"), // description
                        false, // secret
                        "DefaultCollection", // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("TestCollection");

                // Act.
                string actual = command.GetCollectionName();

                // Assert.
                Assert.Equal("TestCollection", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForDeploymentGroupName()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.DeploymentGroupName, // argName
                        StringUtil.Loc("DeploymentGroupName"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("Test Deployment Group");

                // Act.
                string actual = command.GetDeploymentGroupName();

                // Assert.
                Assert.Equal("Test Deployment Group", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void DeploymentGroupNameBackCompat()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(
                              hc,
                              new[]
                              {
                                  "--machinegroupname", "Test-MachineGroupName",
                                  "--deploymentgroupname", "Test-DeploymentGroupName"
                              });
                _promptManager.Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.DeploymentGroupName, // argName
                        StringUtil.Loc("DeploymentGroupName"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("This Method should not get called!");

                // Act.
                string actual = command.GetDeploymentGroupName();

                // Validate if --machinegroupname parameter is working
                Assert.Equal("Test-MachineGroupName", actual);
                
                // Validate Read Value should not get invoked.
                _promptManager.Verify(x =>
                    x.ReadValue(It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<Func<string, bool>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForDeploymentGroupTags()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.DeploymentGroupTags, // argName
                        StringUtil.Loc("DeploymentGroupTags"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("Test-Tag1,Test-Tg2");

                // Act.
                string actual = command.GetDeploymentGroupTags();

                // Assert.
                Assert.Equal("Test-Tag1,Test-Tg2", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void DeploymentGroupTagsBackCompat()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(
                              hc,
                              new[]
                              {
                                  "--machinegrouptags", "Test-MachineGrouptag1,Test-MachineGrouptag2",
                                  "--deploymentgrouptags", "Test-DeploymentGrouptag1,Test-DeploymentGrouptag2"
                              });
                _promptManager.Setup(x => x.ReadValue(
                        Constants.Agent.CommandLine.Args.DeploymentGroupTags, // argName
                        StringUtil.Loc("DeploymentGroupTags"), // description
                        false, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false)) // unattended
                    .Returns("This Method should not get called!");

                // Act.
                string actual = command.GetDeploymentGroupTags();

                // Validate if --machinegrouptags parameter is working fine
                Assert.Equal("Test-MachineGrouptag1,Test-MachineGrouptag2", actual);
                
                // Validate Read Value should not get invoked.
                _promptManager.Verify(x =>
                    x.ReadValue(It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<bool>(),It.IsAny<string>(), It.IsAny<Func<string,bool>>(),It.IsAny<bool>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateCommands()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "badcommand" });

                // Assert.
                Assert.True(command.Validate().Contains("badcommand"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateFlags()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--badflag" });

                // Assert.
                Assert.True(command.Validate().Contains("badflag"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateArgs()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--badargname", "bad arg value" });

                // Assert.
                Assert.True(command.Validate().Contains("badargname"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateGoodCommandline()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc,
                    args: new string[] {
                        "configure",
                        "--unattended",
                        "--agent",
                        "test agent" });

                // Assert.
                Assert.True(command.Validate().Count == 0);
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            TestHostContext hc = new TestHostContext(this, testName);
            hc.SetSingleton<IPromptManager>(_promptManager.Object);
            hc.SetSingleton<ISecretMasker>(_secretMasker.Object);
            return hc;
        }
    }
}
