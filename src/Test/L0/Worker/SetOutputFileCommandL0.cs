using System;
using System.Collections.Generic;
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
            base.TestNotFound();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_EmptyFile()
        {
            base.TestEmptyFile();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple()
        {
            base.TestSimple();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_SkipEmptyLines()
        {
            base.TestSimple_SkipEmptyLines();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_EmptyValue()
        {
            base.TestSimple_EmptyValue();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_MultipleValues()
        {
            base.TestSimple_MultipleValues();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Simple_SpecialCharacters()
        {
            base.TestSimple_SpecialCharacters();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc()
        {
            base.TestHeredoc();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_EmptyValue()
        {
            base.TestHeredoc_EmptyValue();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_SkipEmptyLines()
        {
            base.TestHeredoc_SkipEmptyLines();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_EdgeCases()
        {
            base.TestHeredoc_EdgeCases();
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
        public void SetOutputFileCommand_Heredoc_EndMarkerVariations(string validEndMarker)
        {
            base.TestHeredoc_EndMarkerVariations(validEndMarker);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_EqualBeforeMultilineIndicator()
        {
            base.TestHeredoc_EqualBeforeMultilineIndicator();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_MissingNewLine()
        {
            base.TestHeredoc_MissingNewLine();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_MissingNewLineMultipleLines()
        {
            base.TestHeredoc_MissingNewLineMultipleLines();
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetOutputFileCommand_Heredoc_PreservesNewline()
        {
            base.TestHeredoc_PreservesNewline();
        }
#endif

    }
}
