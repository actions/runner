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
    public sealed class SaveStateFileCommandL0
    {
        private Mock<IExecutionContext> _executionContext;
        private List<Tuple<DTWebApi.Issue, string>> _issues;
        private string _rootDirectory;
        private SaveStateFileCommand _saveStateFileCommand;
        private Dictionary<string, string> _intraActionState;
        private ITraceWriter _trace;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_DirectoryNotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "directory-not-found", "env");
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _intraActionState.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_NotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "file-not-found");
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _intraActionState.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_EmptyFile()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "empty-file");
                var content = new List<string>();
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _intraActionState.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Simple()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_STATE=MY VALUE",
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _intraActionState.Count);
                Assert.Equal("MY VALUE", _intraActionState["MY_STATE"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Simple_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_STATE=my value",
                    string.Empty,
                    "MY_STATE_2=my second value",
                    string.Empty,
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _intraActionState.Count);
                Assert.Equal("my value", _intraActionState["MY_STATE"]);
                Assert.Equal("my second value", _intraActionState["MY_STATE_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Simple_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple-empty-value");
                var content = new List<string>
                {
                    "MY_STATE=",
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _intraActionState.Count);
                Assert.Equal(string.Empty, _intraActionState["MY_STATE"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Simple_MultipleValues()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_STATE=my value",
                    "MY_STATE_2=",
                    "MY_STATE_3=my third value",
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _intraActionState.Count);
                Assert.Equal("my value", _intraActionState["MY_STATE"]);
                Assert.Equal(string.Empty, _intraActionState["MY_STATE_2"]);
                Assert.Equal("my third value", _intraActionState["MY_STATE_3"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Simple_SpecialCharacters()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_STATE==abc",
                    "MY_STATE_2=def=ghi",
                    "MY_STATE_3=jkl=",
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _intraActionState.Count);
                Assert.Equal("=abc", _intraActionState["MY_STATE"]);
                Assert.Equal("def=ghi", _intraActionState["MY_STATE_2"]);
                Assert.Equal("jkl=", _intraActionState["MY_STATE_3"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Heredoc()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_STATE<<EOF",
                    "line one",
                    "line two",
                    "line three",
                    "EOF",
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _intraActionState.Count);
                Assert.Equal($"line one{Environment.NewLine}line two{Environment.NewLine}line three", _intraActionState["MY_STATE"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Heredoc_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_STATE<<EOF",
                    "EOF",
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _intraActionState.Count);
                Assert.Equal(string.Empty, _intraActionState["MY_STATE"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Heredoc_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_STATE<<EOF",
                    "hello",
                    "world",
                    "EOF",
                    string.Empty,
                    "MY_STATE_2<<EOF",
                    "HELLO",
                    "AGAIN",
                    "EOF",
                    string.Empty,
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _intraActionState.Count);
                Assert.Equal($"hello{Environment.NewLine}world", _intraActionState["MY_STATE"]);
                Assert.Equal($"HELLO{Environment.NewLine}AGAIN", _intraActionState["MY_STATE_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Heredoc_SpecialCharacters()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_STATE<<=EOF",
                    "hello",
                    "one",
                    "=EOF",
                    "MY_STATE_2<<<EOF",
                    "hello",
                    "two",
                    "<EOF",
                    "MY_STATE_3<<EOF",
                    "hello",
                    string.Empty,
                    "three",
                    string.Empty,
                    "EOF",
                    "MY_STATE_4<<EOF",
                    "hello=four",
                    "EOF",
                    "MY_STATE_5<<EOF",
                    " EOF",
                    "EOF",
                };
                WriteContent(stateFile, content);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(5, _intraActionState.Count);
                Assert.Equal($"hello{Environment.NewLine}one", _intraActionState["MY_STATE"]);
                Assert.Equal($"hello{Environment.NewLine}two", _intraActionState["MY_STATE_2"]);
                Assert.Equal($"hello{Environment.NewLine}{Environment.NewLine}three{Environment.NewLine}", _intraActionState["MY_STATE_3"]);
                Assert.Equal($"hello=four", _intraActionState["MY_STATE_4"]);
                Assert.Equal($" EOF", _intraActionState["MY_STATE_5"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Heredoc_MissingNewLine()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_STATE<<EOF",
                    "line one",
                    "line two",
                    "line three",
                    "EOF",
                };
                WriteContent(stateFile, content, " ");
                var ex = Assert.Throws<Exception>(() => _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null));
                Assert.Contains("Matching delimiter not found", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Heredoc_MissingNewLineMultipleLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_STATE<<EOF",
                    @"line one
                    line two
                    line three",
                    "EOF",
                };
                WriteContent(stateFile, content, " ");
                var ex = Assert.Throws<Exception>(() => _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null));
                Assert.Contains("EOF marker missing new line", ex.Message);
            }
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveStateFileCommand_Heredoc_PreservesNewline()
        {
            using (var hostContext = Setup())
            {
                var newline = "\n";
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_STATE<<EOF",
                    "hello",
                    "world",
                    "EOF",
                };
                WriteContent(stateFile, content, newline: newline);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _intraActionState.Count);
                Assert.Equal($"hello{newline}world", _intraActionState["MY_STATE"]);
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
            _intraActionState = new Dictionary<string, string>();

            var hostContext = new TestHostContext(this, name);

            // Trace
            _trace = hostContext.GetTrace();

            // Directory for test data
            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(SaveStateFileCommandL0));
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
            _executionContext.Setup(x => x.IntraActionState)
              .Returns(_intraActionState);

            // SaveStateFileCommand
            _saveStateFileCommand = new SaveStateFileCommand();
            _saveStateFileCommand.Initialize(hostContext);

            return hostContext;
        }
    }
}
