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
    public abstract class FileCommandTestBase<T>
        where T : IFileCommandExtension, new()
    {

        protected void TestDirectoryNotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "directory-not-found", "env");
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _store.Count);
            }
        }

        protected void TestNotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "file-not-found");
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _store.Count);
            }
        }

        protected void TestEmptyFile()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "empty-file");
                var content = new List<string>();
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _store.Count);
            }
        }

        protected void TestSimple()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_KEY=MY VALUE",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal("MY VALUE", _store["MY_KEY"]);
            }
        }

        protected void TestSimple_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_KEY=my value",
                    string.Empty,
                    "MY_KEY_2=my second value",
                    string.Empty,
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _store.Count);
                Assert.Equal("my value", _store["MY_KEY"]);
                Assert.Equal("my second value", _store["MY_KEY_2"]);
            }
        }

        protected void TestSimple_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple-empty-value");
                var content = new List<string>
                {
                    "MY_KEY=",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal(string.Empty, _store["MY_KEY"]);
            }
        }

        protected void TestSimple_MultipleValues()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_KEY=my value",
                    "MY_KEY_2=",
                    "MY_KEY_3=my third value",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _store.Count);
                Assert.Equal("my value", _store["MY_KEY"]);
                Assert.Equal(string.Empty, _store["MY_KEY_2"]);
                Assert.Equal("my third value", _store["MY_KEY_3"]);
            }
        }

        protected void TestSimple_SpecialCharacters()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "MY_KEY==abc",
                    "MY_KEY_2=def=ghi",
                    "MY_KEY_3=jkl=",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _store.Count);
                Assert.Equal("=abc", _store["MY_KEY"]);
                Assert.Equal("def=ghi", _store["MY_KEY_2"]);
                Assert.Equal("jkl=", _store["MY_KEY_3"]);
            }
        }

        protected void TestHeredoc()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_KEY<<EOF",
                    "line one",
                    "line two",
                    "line three",
                    "EOF",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal($"line one{BREAK}line two{BREAK}line three", _store["MY_KEY"]);
            }
        }

        protected void TestHeredoc_EmptyValue()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_KEY<<EOF",
                    "EOF",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal(string.Empty, _store["MY_KEY"]);
            }
        }

        protected void TestHeredoc_SkipEmptyLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    string.Empty,
                    "MY_KEY<<EOF",
                    "hello",
                    "world",
                    "EOF",
                    string.Empty,
                    "MY_KEY_2<<EOF",
                    "HELLO",
                    "AGAIN",
                    "EOF",
                    string.Empty,
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _store.Count);
                Assert.Equal($"hello{BREAK}world", _store["MY_KEY"]);
                Assert.Equal($"HELLO{BREAK}AGAIN", _store["MY_KEY_2"]);
            }
        }

        protected void TestHeredoc_EdgeCases()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_KEY_1<<EOF",
                    "hello",
                    string.Empty,
                    "three",
                    string.Empty,
                    "EOF",
                    "MY_KEY_2<<EOF",
                    "hello=two",
                    "EOF",
                    "MY_KEY_3<<EOF",
                    " EOF",
                    "EOF",
                    "MY_KEY_4<<EOF",
                    "EOF EOF",
                    "EOF",
                    "MY_KEY_5=abc << def",
                    "MY_KEY_6=    <<EOF",
                    "white space test",
                    "EOF",
                    "MY_KEY_7 <<=EOF=",
                    "abc",
                    "=EOF=",
                    string.Empty
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(4, _store.Count);
                Assert.Equal($"hello{BREAK}{BREAK}three{BREAK}", _store["MY_KEY_1"]);
                Assert.Equal($"hello=two", _store["MY_KEY_2"]);
                Assert.Equal($" EOF", _store["MY_KEY_3"]);
                Assert.Equal($"EOF EOF", _store["MY_KEY_4"]);
                Assert.Equal($"abc << def", _store["MY_KEY_5"]);
                Assert.Equal($"white space test", _store["MY_KEY_6="]);
                Assert.Equal($"abc", _store["MY_KEY_7"]);
            }
        }

        protected void TestHeredoc_EndMarkerVariations(string validEndMarker)
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                string eof = validEndMarker;
                var content = new List<string>
                {
                    $"MY_KEY_1<<{eof}",
                    $"hello",
                    $"one",
                    $"{eof}",
                    $"MY_KEY_2<<{eof}",
                    $"hello=two",
                    $"{eof}",
                    $"MY_KEY_3<<{eof}",
                    $" {eof}",
                    $"{eof}",
                    $"MY_KEY_4<<{eof}",
                    $"{eof} {eof}",
                    $"{eof}",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(4, _store.Count);
                Assert.Equal($"hello{BREAK}one", _store["MY_KEY_1"]);
                Assert.Equal($"hello=two", _store["MY_KEY_2"]);
                Assert.Equal($" {eof}", _store["MY_KEY_3"]);
                Assert.Equal($"{eof} {eof}", _store["MY_KEY_4"]);
            }
        }

        protected void TestHeredoc_EqualBeforeMultilineIndicator()
        {
            using var hostContext = Setup();
            var stateFile = Path.Combine(_rootDirectory, "heredoc");

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
            TestUtil.WriteContent(stateFile, content);
            var ex = Assert.Throws<Exception>(() => _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null));
            Assert.StartsWith("Invalid format", ex.Message);
        }

        protected void TestHeredoc_MissingNewLine()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                string content = "MY_KEY<<EOF line one line two line three EOF";
                TestUtil.WriteContent(stateFile, content);
                var ex = Assert.Throws<Exception>(() => _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null));
                Assert.Contains("Matching delimiter not found", ex.Message);
            }
        }

        protected void TestHeredoc_MissingNewLineMultipleLines()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                string multilineFragment = @"line one
                                             line two
                                             line three";

                // Note that the final EOF does not appear on it's own line.
                string content = $"MY_KEY<<EOF {multilineFragment} EOF";
                TestUtil.WriteContent(stateFile, content);
                var ex = Assert.Throws<Exception>(() => _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null));
                Assert.Contains("EOF marker missing new line", ex.Message);
            }
        }

        protected void TestHeredoc_PreservesNewline()
        {
            using (var hostContext = Setup())
            {
                var newline = "\n";
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_KEY<<EOF",
                    "hello",
                    "world",
                    "EOF",
                };
                TestUtil.WriteContent(stateFile, content, LineEndingType.Linux);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal($"hello{newline}world", _store["MY_KEY"]);
            }
        }

        protected TestHostContext Setup([CallerMemberName] string name = "")
        {
            _issues = new List<Tuple<DTWebApi.Issue, string>>();

            var hostContext = new TestHostContext(this, name);

            // Trace
            _trace = hostContext.GetTrace();

            // Directory for test data
            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(T));
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

            _store = PostSetup();

            _fileCmdExtension = new T();
            _fileCmdExtension.Initialize(hostContext);

            return hostContext;
        }

        protected abstract IDictionary<string, string> PostSetup();

        protected static readonly string BREAK = Environment.NewLine;

        protected IFileCommandExtension _fileCmdExtension { get; private set; }
        protected Mock<IExecutionContext> _executionContext { get; private set; }
        protected List<Tuple<DTWebApi.Issue, string>> _issues { get; private set; }
        protected IDictionary<string, string> _store { get; private set; }
        protected string _rootDirectory { get; private set; }
        protected ITraceWriter _trace { get; private set; }
    }
}
