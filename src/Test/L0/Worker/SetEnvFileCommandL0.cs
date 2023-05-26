using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class SetEnvFileCommandL0
    {

        private static readonly string BREAK = Environment.NewLine;

        private Mock<IExecutionContext> _executionContext;
        private List<Tuple<DTWebApi.Issue, string>> _issues;
        private string _rootDirectory;
        private SetEnvFileCommand _setEnvFileCommand;
        private ITraceWriter _trace;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_DirectoryNotFound()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "directory-not-found", "env");
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _executionContext.Object.Global.EnvironmentVariables.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_NotFound()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "file-not-found");
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _executionContext.Object.Global.EnvironmentVariables.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_EmptyFile()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "empty-file");
                var content = new List<string>();
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _executionContext.Object.Global.EnvironmentVariables.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Simple()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_ENV=MY VALUE",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal("MY VALUE", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Simple_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_ENV=my value",
                    string.Empty,
                    "MY_ENV_2=my second value",
                    string.Empty,
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal("my value", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
                Assert.Equal("my second value", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Simple_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "simple-empty-value");
                var content = new List<string>
                {
                    "MY_ENV=",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal(string.Empty, _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Simple_MultipleValues()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_ENV=my value",
                    "MY_ENV_2=",
                    "MY_ENV_3=my third value",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal("my value", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
                Assert.Equal(string.Empty, _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
                Assert.Equal("my third value", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_3"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Simple_SpecialCharacters()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_ENV==abc",
                    "MY_ENV_2=def=ghi",
                    "MY_ENV_3=jkl=",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal("=abc", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
                Assert.Equal("def=ghi", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
                Assert.Equal("jkl=", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_3"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_ENV<<EOF",
                    "line one",
                    "line two",
                    "line three",
                    "EOF",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"line one{BREAK}line two{BREAK}line three", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_ENV<<EOF",
                    "EOF",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal(string.Empty, _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_ENV<<EOF",
                    "hello",
                    "world",
                    "EOF",
                    string.Empty,
                    "MY_ENV_2<<EOF",
                    "HELLO",
                    "AGAIN",
                    "EOF",
                    string.Empty,
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"hello{BREAK}world", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
                Assert.Equal($"HELLO{BREAK}AGAIN", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_EdgeCases()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_ENV_1<<EOF",
                    "hello",
                    string.Empty,
                    "three",
                    string.Empty,
                    "EOF",
                    "MY_ENV_2<<EOF",
                    "hello=two",
                    "EOF",
                    "MY_ENV_3<<EOF",
                    " EOF",
                    "EOF",
                    "MY_ENV_4<<EOF",
                    "EOF EOF",
                    "EOF",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(4, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"hello{BREAK}{BREAK}three{BREAK}", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_1"]);
                Assert.Equal($"hello=two", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
                Assert.Equal($" EOF", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_3"]);
                Assert.Equal($"EOF EOF", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_4"]);
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        // All of the following are not only valid, but quite plausible end markers.
        // Most are derived straight from the example at https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#multiline-strings
#pragma warning disable format
        [InlineData("=EOF")][InlineData("==EOF")][InlineData("EO=F")][InlineData("EO==F")][InlineData("EOF=")][InlineData("EOF==")]
        [InlineData("<EOF")][InlineData("<<EOF")][InlineData("EO<F")][InlineData("EO<<F")][InlineData("EOF<")][InlineData("EOF<<")]
        [InlineData("+EOF")][InlineData("++EOF")][InlineData("EO+F")][InlineData("EO++F")][InlineData("EOF+")][InlineData("EOF++")]
        [InlineData("/EOF")][InlineData("//EOF")][InlineData("EO/F")][InlineData("EO//F")][InlineData("EOF/")][InlineData("EOF//")]
#pragma warning restore format
        [InlineData("<<//++==")]
        [InlineData("contrivedBase64==")]
        [InlineData("khkIhPxsVA==")]
        [InlineData("D+Y8zE/EOw==")]
        [InlineData("wuOWG4S6FQ==")]
        [InlineData("7wigCJ//iw==")]
        [InlineData("uifTuYTs8K4=")]
        [InlineData("M7N2ITg/04c=")]
        [InlineData("Xhh+qp+Y6iM=")]
        [InlineData("5tdblQajc/b+EGBZXo0w")]
        [InlineData("jk/UMjIx/N0eVcQYOUfw")]
        [InlineData("/n5lsw73Cwl35Hfuscdz")]
        [InlineData("ZvnAEW+9O0tXp3Fmb3Oh")]
        public void SetEnvFileCommand_Heredoc_EndMarkerVariations(string validEndMarker)
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                string eof = validEndMarker;
                var content = new List<string>
                {
                    $"MY_ENV_1<<{eof}",
                    $"hello",
                    $"one",
                    $"{eof}",
                    $"MY_ENV_2<<{eof}",
                    $"hello=two",
                    $"{eof}",
                    $"MY_ENV_3<<{eof}",
                    $" {eof}",
                    $"{eof}",
                    $"MY_ENV_4<<{eof}",
                    $"{eof} {eof}",
                    $"{eof}",
                };
                TestUtil.WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(4, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"hello{BREAK}one", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_1"]);
                Assert.Equal($"hello=two", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
                Assert.Equal($" {eof}", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_3"]);
                Assert.Equal($"{eof} {eof}", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_4"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_EqualBeforeMultilineIndicator()
        {
            using var hostContext = Setup();
            var envFile = Path.Combine(_rootDirectory, "heredoc");

            // Define a hypothetical injectable payload that just happens to contain the '=' character.
            string contrivedGitHubIssueTitle = "Issue 999:  Better handling for the `=` character";

            // The docs recommend using randomly-generated EOF markers.
            // Here's a randomly-generated base64 EOF marker that just happens to contain an '=' character.  ('=' is a padding character in base64.)
            // see https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#multiline-strings
            string randomizedEOF = "khkIhPxsVA==";
            var content = new List<string>
            {
                // In a real world scenario, "%INJECT%" might instead be something like "${{ github.event.issue.title }}"
                $"PREFIX_%INJECT%<<{randomizedEOF}".Replace("%INJECT%", contrivedGitHubIssueTitle),
                "RandomDataThatJustHappensToContainAnEquals=Character",
                randomizedEOF,
            };
            TestUtil.WriteContent(envFile, content);
            var ex = Assert.Throws<Exception>(() => _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null));
            Assert.StartsWith("Invalid format", ex.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_MissingNewLine()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                string content = "MY_OUTPUT<<EOF line one line two line three EOF";
                TestUtil.WriteContent(envFile, content);
                var ex = Assert.Throws<Exception>(() => _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null));
                Assert.Contains("Matching delimiter not found", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_MissingNewLineMultipleLinesEnv()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                string multilineFragment = @"line one
                                             line two
                                             line three";

                // Note that the final EOF does not appear on it's own line.
                string content = $"MY_OUTPUT<<EOF {multilineFragment} EOF";
                TestUtil.WriteContent(envFile, content);
                var ex = Assert.Throws<Exception>(() => _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null));
                Assert.Contains("EOF marker missing new line", ex.Message);
            }
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_PreservesNewline()
        {
            using (var hostContext = Setup())
            {
                var newline = "\n";
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_ENV<<EOF",
                    "hello",
                    "world",
                    "EOF",
                };
                TestUtil.WriteContent(stateFile, content, LineEndingType.Linux);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"hello{newline}world", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
            }
        }
#endif

        private TestHostContext Setup([CallerMemberName] string name = "")
        {
            _issues = new List<Tuple<DTWebApi.Issue, string>>();

            var hostContext = new TestHostContext(this, name);

            // Trace
            _trace = hostContext.GetTrace();

            // Directory for test data
            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(SetEnvFileCommandL0));
            Directory.CreateDirectory(_rootDirectory);

            // Execution context
            _executionContext = new Mock<IExecutionContext>();
            _executionContext.Setup(x => x.Global)
                .Returns(new GlobalContext
                {
                    EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                    WriteDebug = true,
                });
            _executionContext.Setup(x => x.AddIssue(It.IsAny<DTWebApi.Issue>(), It.IsAny<ExecutionContextLogOptions>()))
                .Callback((DTWebApi.Issue issue, ExecutionContextLogOptions logOptions) =>
                {
                    var resolvedMessage = issue.Message;
                    if (logOptions.WriteToLog && !string.IsNullOrEmpty(logOptions.LogMessageOverride))
                    {
                        resolvedMessage = logOptions.LogMessageOverride;
                    }
                    _issues.Add(new(issue, resolvedMessage));
                    _trace.Info($"Issue '{issue.Type}': {resolvedMessage}");
                });
            _executionContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) =>
                {
                    _trace.Info($"{tag}{message}");
                });

            // SetEnvFileCommand
            _setEnvFileCommand = new SetEnvFileCommand();
            _setEnvFileCommand.Initialize(hostContext);

            return hostContext;
        }
    }
}
