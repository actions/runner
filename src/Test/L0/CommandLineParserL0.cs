using Moq;
using System.Runtime.CompilerServices;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class CommandLineParserL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CanConstruct()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                Tracing trace = hc.GetTrace();

                CommandLineParser clp = new CommandLineParser(hc, secretArgNames: new string[0]);
                trace.Info("Constructed");

                Assert.NotNull(clp);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void MasksSecretArgs()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                CommandLineParser clp = new CommandLineParser(
                    hc,
                    secretArgNames: new[] { "SecretArg1", "SecretArg2" });

                // Assert.
                clp.Parse(new string[]
                {
                    "cmd",
                    "--secretarg1",
                    "secret value 1",
                    "--publicarg",
                    "public arg value",
                    "--secretarg2",
                    "secret value 2",
                });

                // Assert.
                Assert.Equal("***", hc.SecretMasker.MaskSecrets("secret value 1"));
                Assert.Equal("***", hc.SecretMasker.MaskSecrets("secret value 2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ParsesCommands()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                Tracing trace = hc.GetTrace();

                CommandLineParser clp = new CommandLineParser(hc, secretArgNames: new string[0]);
                trace.Info("Constructed.");

                clp.Parse(new string[] { "cmd1", "cmd2", "--arg1", "arg1val", "badcmd" });
                trace.Info("Parsed");

                trace.Info("Commands: {0}", clp.Commands.Count);
                Assert.True(clp.Commands.Count == 2);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ParsesArgs()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                Tracing trace = hc.GetTrace();

                CommandLineParser clp = new CommandLineParser(hc, secretArgNames: new string[0]);
                trace.Info("Constructed.");

                clp.Parse(new string[] { "cmd1", "--arg1", "arg1val", "--arg2", "arg2val" });
                trace.Info("Parsed");

                trace.Info("Args: {0}", clp.Args.Count);
                Assert.True(clp.Args.Count == 2);
                Assert.True(clp.Args.ContainsKey("arg1"));
                Assert.Equal("arg1val", clp.Args["arg1"]);
                Assert.True(clp.Args.ContainsKey("arg2"));
                Assert.Equal("arg2val", clp.Args["arg2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ParsesFlags()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                Tracing trace = hc.GetTrace();

                CommandLineParser clp = new CommandLineParser(hc, secretArgNames: new string[0]);
                trace.Info("Constructed.");

                clp.Parse(new string[] { "cmd1", "--flag1", "--arg1", "arg1val", "--flag2" });
                trace.Info("Parsed");

                trace.Info("Args: {0}", clp.Flags.Count);
                Assert.True(clp.Flags.Count == 2);
                Assert.Contains("flag1", clp.Flags);
                Assert.Contains("flag2", clp.Flags);
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            TestHostContext hc = new TestHostContext(this, testName);
            return hc;
        }
    }
}
