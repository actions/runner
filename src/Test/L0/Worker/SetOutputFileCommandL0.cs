using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitHub.Runner.Worker;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class SetOutputFileCommandL0 : FileCommandTestBase<SetOutputFileCommand>
    {

        protected override IDictionary<string, string> PostSetup()
        {
            var outputs = new Dictionary<string, string>();
            var reference = string.Empty;
            _executionContext.Setup(x => x.SetOutput(It.IsAny<string>(), It.IsAny<string>(), out reference))
              .Callback((string name, string value, out string reference) =>
              {
                  reference = value;
                  outputs[name] = value;
              });

            return outputs;

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_DirectoryNotFound()
        {
            base.TestDirectoryNotFound();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_NotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "file-not-found");
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _store.Count);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _store.Count);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal("MY VALUE", _store["MY_OUTPUT"]);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _store.Count);
                Assert.Equal("my value", _store["MY_OUTPUT"]);
                Assert.Equal("my second value", _store["MY_OUTPUT_2"]);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal(string.Empty, _store["MY_OUTPUT"]);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _store.Count);
                Assert.Equal("my value", _store["MY_OUTPUT"]);
                Assert.Equal(string.Empty, _store["MY_OUTPUT_2"]);
                Assert.Equal("my third value", _store["MY_OUTPUT_3"]);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(3, _store.Count);
                Assert.Equal("=abc", _store["MY_OUTPUT"]);
                Assert.Equal("def=ghi", _store["MY_OUTPUT_2"]);
                Assert.Equal("jkl=", _store["MY_OUTPUT_3"]);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal($"line one{Environment.NewLine}line two{Environment.NewLine}line three", _store["MY_OUTPUT"]);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal(string.Empty, _store["MY_OUTPUT"]);
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
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(2, _store.Count);
                Assert.Equal($"hello{Environment.NewLine}world", _store["MY_OUTPUT"]);
                Assert.Equal($"HELLO{Environment.NewLine}AGAIN", _store["MY_OUTPUT_2"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveOutputFileCommand_Heredoc_EdgeCases()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                var content = new List<string>
                {
                    "MY_OUTPUT_1<<EOF",
                    "hello",
                    string.Empty,
                    "three",
                    string.Empty,
                    "EOF",
                    "MY_OUTPUT_2<<EOF",
                    "hello=two",
                    "EOF",
                    "MY_OUTPUT_3<<EOF",
                    " EOF",
                    "EOF",
                    "MY_OUTPUT_4<<EOF",
                    "EOF EOF",
                    "EOF",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(4, _store.Count);
                Assert.Equal($"hello{BREAK}{BREAK}three{BREAK}", _store["MY_OUTPUT_1"]);
                Assert.Equal($"hello=two", _store["MY_OUTPUT_2"]);
                Assert.Equal($" EOF", _store["MY_OUTPUT_3"]);
                Assert.Equal($"EOF EOF", _store["MY_OUTPUT_4"]);
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
        public void SaveOutputFileCommand_Heredoc_EndMarkerVariations(string validEndMarker)
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                string eof = validEndMarker;
                var content = new List<string>
                {
                    $"MY_OUTPUT_1<<{eof}",
                    $"hello",
                    $"one",
                    $"{eof}",
                    $"MY_OUTPUT_2<<{eof}",
                    $"hello=two",
                    $"{eof}",
                    $"MY_OUTPUT_3<<{eof}",
                    $" {eof}",
                    $"{eof}",
                    $"MY_OUTPUT_4<<{eof}",
                    $"{eof} {eof}",
                    $"{eof}",
                };
                TestUtil.WriteContent(stateFile, content);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(4, _store.Count);
                Assert.Equal($"hello{BREAK}one", _store["MY_OUTPUT_1"]);
                Assert.Equal($"hello=two", _store["MY_OUTPUT_2"]);
                Assert.Equal($" {eof}", _store["MY_OUTPUT_3"]);
                Assert.Equal($"{eof} {eof}", _store["MY_OUTPUT_4"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SaveOutputFileCommand_Heredoc_EqualBeforeMultilineIndicator()
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_MissingNewLine()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "heredoc");
                string content = "MY_OUTPUT<<EOF line one line two line three EOF";
                TestUtil.WriteContent(stateFile, content);
                var ex = Assert.Throws<Exception>(() => _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null));
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
                string multilineFragment = @"line one
                                             line two
                                             line three";

                // Note that the final EOF does not appear on it's own line.
                string content = $"MY_OUTPUT<<EOF {multilineFragment} EOF";
                TestUtil.WriteContent(stateFile, content);
                var ex = Assert.Throws<Exception>(() => _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null));
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
                TestUtil.WriteContent(stateFile, content, LineEndingType.Linux);
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _store.Count);
                Assert.Equal($"hello{newline}world", _store["MY_OUTPUT"]);
            }
        }
#endif

    }
}
