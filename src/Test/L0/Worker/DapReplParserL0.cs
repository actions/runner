using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Worker.Dap;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapReplParserL0
    {
        #region help command

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_HelpReturnsHelpCommand()
        {
            var cmd = DapReplParser.TryParse("help", out var error);

            Assert.Null(error);
            var help = Assert.IsType<HelpCommand>(cmd);
            Assert.Null(help.Topic);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_HelpCaseInsensitive()
        {
            var cmd = DapReplParser.TryParse("Help", out var error);
            Assert.Null(error);
            Assert.IsType<HelpCommand>(cmd);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_HelpWithTopic()
        {
            var cmd = DapReplParser.TryParse("help(\"run\")", out var error);

            Assert.Null(error);
            var help = Assert.IsType<HelpCommand>(cmd);
            Assert.Equal("run", help.Topic);
        }

        #endregion

        #region run command — basic

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunSimpleScript()
        {
            var cmd = DapReplParser.TryParse("run(\"echo hello\")", out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal("echo hello", run.Script);
            Assert.Null(run.Shell);
            Assert.Null(run.Env);
            Assert.Null(run.WorkingDirectory);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunWithShell()
        {
            var cmd = DapReplParser.TryParse("run(\"echo hello\", shell: \"bash\")", out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal("echo hello", run.Script);
            Assert.Equal("bash", run.Shell);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunWithWorkingDirectory()
        {
            var cmd = DapReplParser.TryParse("run(\"ls\", working_directory: \"/tmp\")", out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal("ls", run.Script);
            Assert.Equal("/tmp", run.WorkingDirectory);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunWithEnv()
        {
            var cmd = DapReplParser.TryParse("run(\"echo $FOO\", env: { FOO: \"bar\" })", out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal("echo $FOO", run.Script);
            Assert.NotNull(run.Env);
            Assert.Equal("bar", run.Env["FOO"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunWithMultipleEnvVars()
        {
            var cmd = DapReplParser.TryParse("run(\"echo\", env: { A: \"1\", B: \"2\" })", out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal(2, run.Env.Count);
            Assert.Equal("1", run.Env["A"]);
            Assert.Equal("2", run.Env["B"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunWithAllOptions()
        {
            var input = "run(\"echo $X\", shell: \"zsh\", env: { X: \"1\" }, working_directory: \"/tmp\")";
            var cmd = DapReplParser.TryParse(input, out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal("echo $X", run.Script);
            Assert.Equal("zsh", run.Shell);
            Assert.Equal("1", run.Env["X"]);
            Assert.Equal("/tmp", run.WorkingDirectory);
        }

        #endregion

        #region run command — edge cases

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunWithEscapedQuotes()
        {
            var cmd = DapReplParser.TryParse("run(\"echo \\\"hello\\\"\")", out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal("echo \"hello\"", run.Script);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunWithCommaInEnvValue()
        {
            var cmd = DapReplParser.TryParse("run(\"echo\", env: { CSV: \"a,b,c\" })", out var error);

            Assert.Null(error);
            var run = Assert.IsType<RunCommand>(cmd);
            Assert.Equal("a,b,c", run.Env["CSV"]);
        }

        #endregion

        #region error cases

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunEmptyArgsReturnsError()
        {
            var cmd = DapReplParser.TryParse("run()", out var error);

            Assert.NotNull(error);
            Assert.Null(cmd);
            Assert.Contains("requires a script argument", error);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunUnquotedArgReturnsError()
        {
            var cmd = DapReplParser.TryParse("run(echo hello)", out var error);

            Assert.NotNull(error);
            Assert.Null(cmd);
            Assert.Contains("quoted string", error);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunUnknownOptionReturnsError()
        {
            var cmd = DapReplParser.TryParse("run(\"echo\", timeout: \"10\")", out var error);

            Assert.NotNull(error);
            Assert.Null(cmd);
            Assert.Contains("Unknown option", error);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_RunMissingClosingParenReturnsError()
        {
            var cmd = DapReplParser.TryParse("run(\"echo\"", out var error);

            Assert.NotNull(error);
            Assert.Null(cmd);
        }

        #endregion

        #region non-DSL input falls through

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_ExpressionReturnsNull()
        {
            var cmd = DapReplParser.TryParse("github.repository", out var error);

            Assert.Null(error);
            Assert.Null(cmd);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_WrappedExpressionReturnsNull()
        {
            var cmd = DapReplParser.TryParse("${{ github.event_name }}", out var error);

            Assert.Null(error);
            Assert.Null(cmd);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Parse_EmptyInputReturnsNull()
        {
            var cmd = DapReplParser.TryParse("", out var error);
            Assert.Null(error);
            Assert.Null(cmd);

            cmd = DapReplParser.TryParse(null, out error);
            Assert.Null(error);
            Assert.Null(cmd);
        }

        #endregion

        #region help text

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetGeneralHelp_ContainsCommands()
        {
            var help = DapReplParser.GetGeneralHelp();

            Assert.Contains("help", help);
            Assert.Contains("run", help);
            Assert.Contains("expression", help, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetRunHelp_ContainsOptions()
        {
            var help = DapReplParser.GetRunHelp();

            Assert.Contains("shell", help);
            Assert.Contains("env", help);
            Assert.Contains("working_directory", help);
        }

        #endregion

        #region internal parser helpers

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SplitArguments_HandlesNestedBraces()
        {
            var args = DapReplParser.SplitArguments("\"hello\", env: { A: \"1\", B: \"2\" }", out var error);

            Assert.Null(error);
            Assert.Equal(2, args.Count);
            Assert.Equal("\"hello\"", args[0].Trim());
            Assert.Contains("A:", args[1]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ParseEnvBlock_HandlesEmptyBlock()
        {
            var result = DapReplParser.ParseEnvBlock("{ }", out var error);

            Assert.Null(error);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}
