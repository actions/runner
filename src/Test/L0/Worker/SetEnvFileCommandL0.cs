using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class SetEnvFileCommandL0
    {
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
                WriteContent(envFile, content);
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
                WriteContent(envFile, content);
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
                WriteContent(envFile, content);
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
                WriteContent(envFile, content);
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
                WriteContent(envFile, content);
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
                WriteContent(envFile, content);
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
                WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"line one{Environment.NewLine}line two{Environment.NewLine}line three", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
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
                WriteContent(envFile, content);
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
                WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"hello{Environment.NewLine}world", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
                Assert.Equal($"HELLO{Environment.NewLine}AGAIN", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetEnvFileCommand_Heredoc_SpecialCharacters()
        {
            using (var hostContext = Setup())
            {
                var envFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_ENV<<=EOF",
                    "hello",
                    "one",
                    "=EOF",
                    "MY_ENV_2<<<EOF",
                    "hello",
                    "two",
                    "<EOF",
                    "MY_ENV_3<<EOF",
                    "hello",
                    string.Empty,
                    "three",
                    string.Empty,
                    "EOF",
                    "MY_ENV_4<<EOF",
                    "hello=four",
                    "EOF",
                    "MY_ENV_5<<EOF",
                    " EOF",
                    "EOF",
                };
                WriteContent(envFile, content);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(5, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"hello{Environment.NewLine}one", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
                Assert.Equal($"hello{Environment.NewLine}two", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_2"]);
                Assert.Equal($"hello{Environment.NewLine}{Environment.NewLine}three{Environment.NewLine}", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_3"]);
                Assert.Equal($"hello=four", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_4"]);
                Assert.Equal($" EOF", _executionContext.Object.Global.EnvironmentVariables["MY_ENV_5"]);
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
                WriteContent(envFile, content, newline: newline);
                _setEnvFileCommand.ProcessCommand(_executionContext.Object, envFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _executionContext.Object.Global.EnvironmentVariables.Count);
                Assert.Equal($"hello{newline}world", _executionContext.Object.Global.EnvironmentVariables["MY_ENV"]);
            }
        }
#endif

        private void WriteContent(
            string path,
            List<string> content,
            string newline = null)
        {
            if (string.IsNullOrEmpty(newline))
            {
                newline = Environment.NewLine;
            }

            var encoding = new UTF8Encoding(true); // Emit BOM
            var contentStr = string.Join(newline, content);
            File.WriteAllText(path, contentStr, encoding);
        }

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
            _executionContext.Setup(x => x.AddIssue(It.IsAny<DTWebApi.Issue>(), It.IsAny<string>()))
                .Callback((DTWebApi.Issue issue, string logMessage) =>
                {
                    _issues.Add(new Tuple<DTWebApi.Issue, string>(issue, logMessage));
                    var message = !string.IsNullOrEmpty(logMessage) ? logMessage : issue.Message;
                    _trace.Info($"Issue '{issue.Type}': {message}");
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
