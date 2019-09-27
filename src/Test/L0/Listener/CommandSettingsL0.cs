using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Common.Util;
using Moq;
using System;
using System.Runtime.CompilerServices;
using Xunit;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Tests
{
    public sealed class CommandSettingsL0
    {
        private readonly Mock<IPromptManager> _promptManager = new Mock<IPromptManager>();

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
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_AGENT", "some agent");
                    var command = new CommandSettings(hc, args: new string[0]);

                    // Act.
                    string actual = command.GetAgentName();

                    // Assert.
                    Assert.Equal("some agent", actual);
                    Assert.Equal(string.Empty, Environment.GetEnvironmentVariable("ACTIONS_RUNNER_INPUT_AGENT") ?? string.Empty); // Should remove.
                    Assert.Equal(hc.SecretMasker.MaskSecrets("some agent"), "some agent");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_AGENT", null);
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
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_TOKEN", "some secret token value");
                    var command = new CommandSettings(hc, args: new string[0]);

                    // Act.
                    string actual = command.GetToken();

                    // Assert.
                    Assert.Equal("some secret token value", actual);
                    Assert.Equal(string.Empty, Environment.GetEnvironmentVariable("ACTIONS_RUNNER_INPUT_TOKEN") ?? string.Empty); // Should remove.
                    Assert.Equal(hc.SecretMasker.MaskSecrets("some secret token value"), "***");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_TOKEN", null);
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
                bool actual = command.Remove;

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
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_UNATTENDED", "true");
                    var command = new CommandSettings(hc, args: new string[0]);

                    // Act.
                    bool actual = command.Unattended;

                    // Assert.
                    Assert.Equal(true, actual);
                    Assert.Equal(string.Empty, Environment.GetEnvironmentVariable("ACTIONS_RUNNER_INPUT_UNATTENDED") ?? string.Empty); // Should remove.
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_UNATTENDED", null);
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
                        Constants.Runner.CommandLine.Flags.Replace, // argName
                        "Do you like to replace existing runner? (Y/N)", // description
                        false, // defaultValue
                        true)) // unattended
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
        public void PassesUnattendedToReadValue()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--unattended" });
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Runner.CommandLine.Args.Agent, // argName
                        "Enter the name of runner:", // description
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
        public void PromptsForAgent()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Runner.CommandLine.Args.Agent, // argName
                        "Enter the name of runner:", // description
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
                        Constants.Runner.CommandLine.Args.Auth, // argName
                        "How would you like to authenticate?", // description
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
                        Constants.Runner.CommandLine.Args.Password, // argName
                        "What is your Github password?", // description
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
                        Constants.Runner.CommandLine.Args.Pool, // argName
                        "Enter the name of runner pool:", // description
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
                        Constants.Runner.CommandLine.Flags.Replace, // argName
                        "Do you like to replace existing runner? (Y/N)", // description
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
                        Constants.Runner.CommandLine.Flags.RunAsService, // argName
                        "Do you like to run runner as service? (Y/N)", // description
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
                        Constants.Runner.CommandLine.Args.Token, // argName
                        "Enter your personal access token:", // description
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
                        Constants.Runner.CommandLine.Args.Url, // argName
                        "What is the URL of your repository?", // description
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
                        Constants.Runner.CommandLine.Args.UserName, // argName
                        "What is your Github username?", // description
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
                        Constants.Runner.CommandLine.Args.WindowsLogonAccount, // argName
                        "User account to use for the service", // description
                        false, // secret
                        "some default account", // defaultValue
                        Validators.NTAccountValidator, // validator
                        false)) // unattended
                    .Returns("some windows logon account");

                // Act.
                string actual = command.GetWindowsLogonAccount("some default account", "User account to use for the service");

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
                        Constants.Runner.CommandLine.Args.WindowsLogonPassword, // argName
                        string.Format("Password for the account {0}", accountName), // description
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
                        Constants.Runner.CommandLine.Args.Work, // argName
                        "Enter name of work folder:", // description
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
                        Constants.Runner.CommandLine.Args.Url, // argName
                        "What is the URL of your repository?", // description
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
                        Constants.Runner.CommandLine.Args.Url, // argName
                        "What is the URL of your repository?", // description
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
            return hc;
        }
    }
}
