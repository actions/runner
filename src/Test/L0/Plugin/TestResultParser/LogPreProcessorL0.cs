using Agent.Plugins.Log.TestResultParser.Plugin;
using Xunit;

namespace Test.L0.Plugin.TestResultParser
{
    public class LogPreProcessorL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void LogPreProcessorRemovesDebugLines()
        {
            var logLine = "##[debug]some log line";
            Assert.Null(new LogPreProcessor().ProcessData(logLine));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void LogPreProcessorRemovesWarningLines()
        {
            var logLine = "##[warning]some log line";
            Assert.Null(new LogPreProcessor().ProcessData(logLine));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void LogPreProcessorRemovesCommandLines()
        {
            var logLine = "##[command]some log line";
            Assert.Null(new LogPreProcessor().ProcessData(logLine));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void LogPreProcessorRemovesSectionLines()
        {
            var logLine = "##[section]some log line";
            Assert.Null(new LogPreProcessor().ProcessData(logLine));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void LogPreProcessorRemovesErrorPrefixFromErrorLog()
        {
            var logLine = "##[error]some log line";
            Assert.Equal("some log line", new LogPreProcessor().ProcessData(logLine));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void LogPreProcessorShouldLeaveInfoLinesIntact()
        {
            var logLine = "some log line";
            Assert.Equal("some log line", new LogPreProcessor().ProcessData(logLine));
        }
    }
}
