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
    public sealed class SetOutputFileCommandL0
    {
        private Mock<IExecutionContext> _executionContext;
        private List<Tuple<DTWebApi.Issue, string>> _issues;
        private Dictionary<string, string> _outputs;
        private string _rootDirectory;
        private SetOutputFileCommand _setOutputFileCommand;
        private ITraceWriter _trace;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_DirectoryNotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "directory-not-found", "env");
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _outputs.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_NotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "file-not-found");
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _outputs.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_EmptyFile()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "empty-file");
                var content = new List<string>();
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _outputs.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_OUTPUT=MY VALUE",
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _outputs.Count);
                Assert.Equal("MY VALUE", _outputs["MY_OUTPUT"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_OUTPUT=my value",
                    string.Empty,
                    "MY_OUTPUT_2=my second value",
                    string.Empty,
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _outputs.Count);
                Assert.Equal("my value", _outputs["MY_OUTPUT"]);
                Assert.Equal("my second value", _outputs["MY_OUTPUT_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple-empty-value");
                var content = new List<string>
                {
                    "MY_OUTPUT=",
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _outputs.Count);
                Assert.Equal(string.Empty, _outputs["MY_OUTPUT"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_MultipleValues()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_OUTPUT=my value",
                    "MY_OUTPUT_2=",
                    "MY_OUTPUT_3=my third value",
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _outputs.Count);
                Assert.Equal("my value", _outputs["MY_OUTPUT"]);
                Assert.Equal(string.Empty, _outputs["MY_OUTPUT_2"]);
                Assert.Equal("my third value", _outputs["MY_OUTPUT_3"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_SpecialCharacters()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_OUTPUT==abc",
                    "MY_OUTPUT_2=def=ghi",
                    "MY_OUTPUT_3=jkl=",
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _outputs.Count);
                Assert.Equal("=abc", _outputs["MY_OUTPUT"]);
                Assert.Equal("def=ghi", _outputs["MY_OUTPUT_2"]);
                Assert.Equal("jkl=", _outputs["MY_OUTPUT_3"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_OUTPUT<<EOF",
                    "line one",
                    "line two",
                    "line three",
                    "EOF",
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _outputs.Count);
                Assert.Equal($"line one{Environment.NewLine}line two{Environment.NewLine}line three", _outputs["MY_OUTPUT"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_OUTPUT<<EOF",
                    "EOF",
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _outputs.Count);
                Assert.Equal(string.Empty, _outputs["MY_OUTPUT"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_OUTPUT<<EOF",
                    "hello",
                    "world",
                    "EOF",
                    string.Empty,
                    "MY_OUTPUT_2<<EOF",
                    "HELLO",
                    "AGAIN",
                    "EOF",
                    string.Empty,
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _outputs.Count);
                Assert.Equal($"hello{Environment.NewLine}world", _outputs["MY_OUTPUT"]);
                Assert.Equal($"HELLO{Environment.NewLine}AGAIN", _outputs["MY_OUTPUT_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_SpecialCharacters()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_OUTPUT<<=EOF",
                    "hello",
                    "one",
                    "=EOF",
                    "MY_OUTPUT_2<<<EOF",
                    "hello",
                    "two",
                    "<EOF",
                    "MY_OUTPUT_3<<EOF",
                    "hello",
                    string.Empty,
                    "three",
                    string.Empty,
                    "EOF",
                    "MY_OUTPUT_4<<EOF",
                    "hello=four",
                    "EOF",
                    "MY_OUTPUT_5<<EOF",
                    " EOF",
                    "EOF",
                };
                WriteContent(stateFile, content);
                _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(5, _outputs.Count);
                Assert.Equal($"hello{Environment.NewLine}one", _outputs["MY_OUTPUT"]);
                Assert.Equal($"hello{Environment.NewLine}two", _outputs["MY_OUTPUT_2"]);
                Assert.Equal($"hello{Environment.NewLine}{Environment.NewLine}three{Environment.NewLine}", _outputs["MY_OUTPUT_3"]);
                Assert.Equal($"hello=four", _outputs["MY_OUTPUT_4"]);
                Assert.Equal($" EOF", _outputs["MY_OUTPUT_5"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_MissingNewLine()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_OUTPUT<<EOF",
                    "line one",
                    "line two",
                    "line three",
                    "EOF",
                };
                WriteContent(stateFile, content, " ");
                var ex = Assert.Throws<Exception>(() => _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null));
                Assert.Contains("Matching delimiter not found", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_MissingNewLineMultipleLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_OUTPUT<<EOF",
                    @"line one
                    line two
                    line three",
                    "EOF",
                };
                WriteContent(stateFile, content, " ");
                var ex = Assert.Throws<Exception>(() => _setOutputFileCommand.ProcessCommand(_executionContext.Object, stateFile, null));
                Assert.Contains("EOF marker missing new line", ex.Message);
            }
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_PreservesNewline()
        {
            using (var hostContext = Setup())
            {
                var newline = "\n";
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_OUTPUT<<EOF",
                    "hello",
                    "world",
                    "EOF",
                };
                WriteContent(stateFile, content, newline: newline);
                _saveStateFileCommand.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _outputs.Count);
                Assert.Equal($"hello{newline}world", _outputs["MY_OUTPUT"]);
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
            _outputs = new Dictionary<string, string>();

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

            var reference = string.Empty;
            _executionContext.Setup(x => x.SetOutput(It.IsAny<string>(), It.IsAny<string>(), out reference))
              .Callback((string name, string value, out string reference) =>
              {
                reference = value;
                _outputs[name] = value;
              });

            // SaveStateFileCommand
            _setOutputFileCommand = new SetOutputFileCommand();
            _setOutputFileCommand.Initialize(hostContext);

            return hostContext;
        }
    }
}
