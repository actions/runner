using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using Moq;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class CommandSettingsL0
    {
        private readonly Mock<IPromptManager> _promptManager = new();

        // It is sufficient to test one arg only. All individual args are tested by the PromptsFor___ methods.
        // The PromptsFor___ methods suffice to cover the interesting differences between each of the args.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsNameArg()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { "--name", "some runner" });

                // Act.
                string actual = command.GetRunnerName();

                // Assert.
                Assert.Equal("some runner", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void GetsNameArgFromEnvVar()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                try
                {
                    // Arrange.
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_NAME", "some runner");
                    var command = new CommandSettings(hc, args: new string[0]);

                    // Act.
                    string actual = command.GetRunnerName();

                    // Assert.
                    Assert.Equal("some runner", actual);
                    Assert.Equal(string.Empty, Environment.GetEnvironmentVariable("ACTIONS_RUNNER_INPUT_NAME") ?? string.Empty); // Should remove.
                    Assert.Equal("some runner", hc.SecretMasker.MaskSecrets("some runner"));
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_INPUT_NAME", null);
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
                    Assert.Equal("***", hc.SecretMasker.MaskSecrets("some secret token value"));
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
                    Assert.True(actual);
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
                        "Would you like to replace the existing runner? (Y/N)", // description
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
                        Constants.Runner.CommandLine.Args.Name, // argName
                        "Enter the name of runner:", // description
                        false, // secret
                        Environment.MachineName, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        true, // unattended
                        false)) // isOptional
                    .Returns("some runner");

                // Act.
                string actual = command.GetRunnerName();

                // Assert.
                Assert.Equal("some runner", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void PromptsForRunnerName()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Runner.CommandLine.Args.Name, // argName
                        "Enter the name of runner:", // description
                        false, // secret
                        Environment.MachineName, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false, // unattended
                        false)) // isOptional
                    .Returns("some runner");

                // Act.
                string actual = command.GetRunnerName();

                // Assert.
                Assert.Equal("some runner", actual);
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
                        false, // unattended
                        false)) // isOptional
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
        public void PromptsForRunnerRegisterToken()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Runner.CommandLine.Args.Token, // argName
                        "What is your runner register token?", // description
                        true, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false, // unattended
                        false)) // isOptional
                    .Returns("some token");

                // Act.
                string actual = command.GetRunnerRegisterToken();

                // Assert.
                Assert.Equal("some token", actual);
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
                        "Would you like to replace the existing runner? (Y/N)", // description
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
                        "Would you like to run the runner as service? (Y/N)", // description
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
                        "What is your pool admin oauth access token?", // description
                        true, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false, // unattended
                        false)) // isOptional
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
        public void PromptsForRunnerDeletionToken()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[0]);
                _promptManager
                    .Setup(x => x.ReadValue(
                        Constants.Runner.CommandLine.Args.Token, // argName
                        "Enter runner remove token:", // description
                        true, // secret
                        string.Empty, // defaultValue
                        Validators.NonEmptyValidator, // validator
                        false, // unattended
                        false)) // isOptional
                    .Returns("some token");

                // Act.
                string actual = command.GetRunnerDeletionToken();

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
                        false, // unattended
                        false)) // isOptional
                    .Returns("some url");

                // Act.
                string actual = command.GetUrl();

                // Assert.
                Assert.Equal("some url", actual);
            }
        }

#if OS_WINDOWS
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
                        false, // unattended
                        false)) // isOptional
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
                        false, // unattended
                        false)) // isOptional
                    .Returns("some windows logon password");

                // Act.
                string actual = command.GetWindowsLogonPassword(accountName);

                // Assert.
                Assert.Equal("some windows logon password", actual);
            }
        }
#endif
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
                        false, // unattended
                        false)) // isOptional
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
                        false, // unattended
                        false)) // isOptional
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
                        false, // unattended
                        false)) // isOptional
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
                Assert.Contains("badcommand", command.Validate());
            }
        }

        [Theory]
        [InlineData("configure", "once")]
        [InlineData("remove", "disableupdate")]
        [InlineData("remove", "ephemeral")]
        [InlineData("remove", "once")]
        [InlineData("remove", "replace")]
        [InlineData("remove", "runasservice")]
        [InlineData("remove", "unattended")]
        [InlineData("run", "disableupdate")]
        [InlineData("run", "ephemeral")]
        [InlineData("run", "replace")]
        [InlineData("run", "runasservice")]
        [InlineData("run", "unattended")]
        [InlineData("warmup", "disableupdate")]      
        [InlineData("warmup", "ephemeral")]     
        [InlineData("warmup", "once")]   
        [InlineData("warmup", "replace")]      
        [InlineData("warmup", "runasservice")]   
        [InlineData("warmup", "unattended")]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateInvalidFlagCommandCombination(string validCommand, string flag)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { validCommand, $"--{flag}" });

                // Assert.
                Assert.Contains(flag, command.Validate());
            }
        }

        [Theory]
        [InlineData("remove", "auth", "bar arg value")]
        [InlineData("remove", "labels", "bar arg value")]
        [InlineData("remove", "monitorsocketaddress", "bar arg value")]
        [InlineData("remove", "name", "bar arg value")]
        [InlineData("remove", "runnergroup", "bar arg value")]
        [InlineData("remove", "url", "bar arg value")]
        [InlineData("remove", "username", "bar arg value")]
        [InlineData("remove", "windowslogonaccount", "bar arg value")]
        [InlineData("remove", "windowslogonpassword", "bar arg value")]
        [InlineData("remove", "work", "bar arg value")]
        [InlineData("run", "auth", "bad arg value")]
        [InlineData("run", "labels", "bad arg value")]
        [InlineData("run", "monitorsocketaddress", "bad arg value")]
        [InlineData("run", "name", "bad arg value")]
        [InlineData("run", "pat", "bad arg value")]
        [InlineData("run", "runnergroup", "bad arg value")]
        [InlineData("run", "token", "bad arg value")]
        [InlineData("run", "url", "bad arg value")]
        [InlineData("run", "username", "bad arg value")]
        [InlineData("run", "windowslogonaccount", "bad arg value")]
        [InlineData("run", "windowslogonpassword", "bad arg value")]
        [InlineData("run", "work", "bad arg value")]
        [InlineData("warmup", "auth", "bad arg value")]
        [InlineData("warmup", "labels", "bad arg value")]
        [InlineData("warmup", "monitorsocketaddress", "bad arg value")]
        [InlineData("warmup", "name", "bad arg value")]
        [InlineData("warmup", "pat", "bad arg value")]
        [InlineData("warmup", "runnergroup", "bad arg value")]
        [InlineData("warmup", "token", "bad arg value")]
        [InlineData("warmup", "url", "bad arg value")]
        [InlineData("warmup", "username", "bad arg value")]
        [InlineData("warmup", "windowslogonaccount", "bad arg value")]
        [InlineData("warmup", "windowslogonpassword", "bad arg value")]
        [InlineData("warmup", "work", "bad arg value")]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateInvalidArgCommandCombination(string validCommand, string arg, string argValue)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { validCommand, $"--{arg}", argValue });

                // Assert.
                Assert.Contains(arg, command.Validate());
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
                        "--name",
                        "test runner" });

                // Assert.
                Assert.True(command.Validate().Count == 0);
            }
        }

        [Theory]
        [InlineData("configure", "help")]
        [InlineData("configure", "version")]
        [InlineData("configure", "commit")]
        [InlineData("configure", "check")]
        [InlineData("configure", "disableupdate")]
        [InlineData("configure", "ephemeral")]
        [InlineData("configure", "replace")]
        [InlineData("configure", "runasservice")]
        [InlineData("configure", "unattended")]
        [InlineData("remove", "help")]
        [InlineData("remove", "version")]
        [InlineData("remove", "commit")]
        [InlineData("remove", "check")]
        [InlineData("run", "help")]
        [InlineData("run", "version")]
        [InlineData("run", "commit")]
        [InlineData("run", "check")]
        [InlineData("run", "once")]
        [InlineData("warmup", "help")]
        [InlineData("warmup", "version")]
        [InlineData("warmup", "commit")]
        [InlineData("warmup", "check")]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateGoodFlagCommandCombination(string validCommand, string flag)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { validCommand, $"--{flag}" });

                // Assert.
                Assert.True(command.Validate().Count == 0);
            }
        }

        [Theory]
        [InlineData("configure", "auth", "good arg value")]
        [InlineData("configure", "labels", "good arg value")]
        [InlineData("configure", "monitorsocketaddress", "good arg value")]
        [InlineData("configure", "name", "good arg value")]
        [InlineData("configure", "pat", "good arg value")]
        [InlineData("configure", "runnergroup", "good arg value")]
        [InlineData("configure", "token", "good arg value")]
        [InlineData("configure", "url", "good arg value")]
        [InlineData("configure", "username", "good arg value")]
        [InlineData("configure", "windowslogonaccount", "good arg value")]
        [InlineData("configure", "windowslogonpassword", "good arg value")]
        [InlineData("configure", "work", "good arg value")]
        [InlineData("remove", "token", "good arg value")]
        [InlineData("remove", "pat", "good arg value")]
        [InlineData("run", "startuptype", "good arg value")]
        [Trait("Level", "L0")]
        [Trait("Category", nameof(CommandSettings))]
        public void ValidateGoodArgCommandCombination(string validCommand, string arg, string argValue)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var command = new CommandSettings(hc, args: new string[] { validCommand, $"--{arg}", argValue });

                // Assert.
                Assert.True(command.Validate().Count == 0);
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            TestHostContext hc = new(this, testName);
            hc.SetSingleton<IPromptManager>(_promptManager.Object);
            return hc;
        }
    }
}
