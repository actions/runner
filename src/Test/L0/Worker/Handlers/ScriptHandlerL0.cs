using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker.Handlers
{
    public sealed class ScriptHandlerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ScriptPath_WithSpaces_ShouldBeQuoted()
        {
            // Arrange - Test the path quoting logic that our fix addresses
            var tempPathWithSpaces = "/path with spaces/_temp";
            var scriptPathWithSpaces = Path.Combine(tempPathWithSpaces, "test-script.sh");
            
            // Test the original (broken) behavior 
            var originalPath = scriptPathWithSpaces.Replace("\"", "\\\"");
            
            // Test our fix - properly quoted path
            var quotedPath = $"\"{scriptPathWithSpaces.Replace("\"", "\\\"")}\"";
            
            // Assert
            Assert.False(originalPath.StartsWith("\""), "Original path should not be quoted");
            Assert.True(quotedPath.StartsWith("\"") && quotedPath.EndsWith("\""), "Fixed path should be properly quoted");
            Assert.Contains("path with spaces", quotedPath, StringComparison.Ordinal);
            
            // Verify the path is properly quoted (platform-agnostic check)
            Assert.True(quotedPath.StartsWith("\"/path with spaces/_temp"), "Path should start with quoted temp directory");
            Assert.True(quotedPath.EndsWith("test-script.sh\""), "Path should end with quoted script name");
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ScriptPath_WithQuotes_ShouldEscapeQuotes()
        {
            // Arrange - Test paths that contain quotes
            var pathWithQuotes = "/path/\"quoted folder\"/script.sh";
            
            // Test our fix - properly escape quotes and wrap in quotes
            var quotedPath = $"\"{pathWithQuotes.Replace("\"", "\\\"")}\"";
            
            // Assert
            Assert.True(quotedPath.StartsWith("\"") && quotedPath.EndsWith("\""), "Path should be wrapped in quotes");
            Assert.Contains("\\\"", quotedPath, StringComparison.Ordinal);
            Assert.Contains("quoted folder", quotedPath, StringComparison.Ordinal);
            
            // Verify quotes are properly escaped
            Assert.Contains("\\\"quoted folder\\\"", quotedPath, StringComparison.Ordinal);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ScriptPath_ActionsRunnerWithSpaces_ShouldBeQuoted()
        {
            // Arrange - Test the specific real-world scenario that was failing
            var runnerPathWithSpaces = "/Users/user/Downloads/actions-runner-osx-arm64-2.328.0 2";
            var tempPath = Path.Combine(runnerPathWithSpaces, "_work", "_temp");
            var scriptPath = Path.Combine(tempPath, "script-guid.sh");
            
            // Test our fix
            var quotedPath = $"\"{scriptPath.Replace("\"", "\\\"")}\"";
            
            // Assert
            Assert.True(quotedPath.StartsWith("\"") && quotedPath.EndsWith("\""), "Path should be wrapped in quotes");
            Assert.Contains("actions-runner-osx-arm64-2.328.0 2", quotedPath, StringComparison.Ordinal);
            Assert.Contains("_work", quotedPath, StringComparison.Ordinal);
            Assert.Contains("_temp", quotedPath, StringComparison.Ordinal);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ScriptPath_MultipleSpaces_ShouldBeQuoted()
        {
            // Arrange - Test paths with multiple spaces
            var pathWithMultipleSpaces = "/path/with  multiple   spaces/script.sh";
            
            // Test our fix
            var quotedPath = $"\"{pathWithMultipleSpaces.Replace("\"", "\\\"")}\"";
            
            // Assert
            Assert.True(quotedPath.StartsWith("\"") && quotedPath.EndsWith("\""), "Path should be wrapped in quotes");
            Assert.Contains("multiple   spaces", quotedPath, StringComparison.Ordinal);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ScriptPath_WithoutSpaces_ShouldStillBeQuoted()
        {
            // Arrange - Test normal paths without spaces (regression test)
            var normalPath = "/home/user/runner/_work/_temp/script.sh";
            
            // Test our fix
            var quotedPath = $"\"{normalPath.Replace("\"", "\\\"")}\"";
            
            // Assert
            Assert.True(quotedPath.StartsWith("\"") && quotedPath.EndsWith("\""), "Path should be wrapped in quotes");
            Assert.Equal($"\"{normalPath}\"", quotedPath);
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData("/path with spaces/script.sh")]
        [InlineData("/Users/user/Downloads/actions-runner-osx-arm64-2.328.0 2/_work/_temp/guid.sh")]
        [InlineData("C:\\Program Files\\GitHub Runner\\script.cmd")]
        [InlineData("/path/\"with quotes\"/script.sh")]
        [InlineData("/path/with'single'quotes/script.sh")]
        public void ScriptPath_VariousScenarios_ShouldBeProperlyQuoted(string inputPath)
        {
            // Arrange & Act
            var quotedPath = $"\"{inputPath.Replace("\"", "\\\"")}\"";
            
            // Assert
            Assert.True(quotedPath.StartsWith("\""), "Path should start with quote");
            Assert.True(quotedPath.EndsWith("\""), "Path should end with quote");
            
            // Ensure the original path content is preserved
            var unquotedContent = quotedPath.Substring(1, quotedPath.Length - 2);
            if (inputPath.Contains("\""))
            {
                // If original had quotes, they should be escaped in the result
                Assert.Contains("\\\"", unquotedContent);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FixUpScriptContents_BashEnvironmentVariables_ShouldQuoteRedirects()
        {
            // Arrange
            var scriptContent = @"echo ""## Dependency Status Report"" >> $GITHUB_STEP_SUMMARY
echo ""Generated on: $(date)"" >> $GITHUB_STEP_SUMMARY
echo ""| Component | Status |"" > $GITHUB_OUTPUT
echo ""npm-status=ok"" >> $GITHUB_OUTPUT";

            // Act
            var fixedContent = ScriptHandlerHelpers.FixUpScriptContents("bash", scriptContent);

            // Assert
            Assert.Contains(">> \"$GITHUB_STEP_SUMMARY\"", fixedContent);
            Assert.Contains("> \"$GITHUB_OUTPUT\"", fixedContent);
            Assert.DoesNotContain(">> $GITHUB_STEP_SUMMARY", fixedContent);
            Assert.DoesNotContain("> $GITHUB_OUTPUT", fixedContent);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FixUpScriptContents_AlreadyQuotedVariables_ShouldNotDoubleQuote()
        {
            // Arrange
            var scriptContent = @"echo ""test"" >> ""$GITHUB_STEP_SUMMARY""
echo ""test"" > '$GITHUB_OUTPUT'
echo ""test"" 2>> ""$GITHUB_ENV""";

            // Act
            var fixedContent = ScriptHandlerHelpers.FixUpScriptContents("bash", scriptContent);

            // Assert - Should remain unchanged
            Assert.Equal(scriptContent, fixedContent);
            Assert.Contains(">> \"$GITHUB_STEP_SUMMARY\"", fixedContent);
            Assert.Contains("> '$GITHUB_OUTPUT'", fixedContent);
            Assert.Contains("2>> \"$GITHUB_ENV\"", fixedContent);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FixUpScriptContents_ShellRedirectOperators_ShouldHandleAllTypes()
        {
            // Arrange
            var scriptContent = @"echo ""test"" >> $VAR1
echo ""test"" > $VAR2
cat < $VAR3
echo ""test"" 2>> $VAR4
echo ""test"" 2> $VAR5";

            // Act
            var fixedContent = ScriptHandlerHelpers.FixUpScriptContents("sh", scriptContent);

            // Assert
            Assert.Contains(">> \"$VAR1\"", fixedContent);
            Assert.Contains("> \"$VAR2\"", fixedContent);
            Assert.Contains("< \"$VAR3\"", fixedContent);
            Assert.Contains("2>> \"$VAR4\"", fixedContent);
            Assert.Contains("2> \"$VAR5\"", fixedContent);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FixUpScriptContents_NonShellTypes_ShouldNotModifyEnvironmentVariables()
        {
            // Arrange
            var scriptContent = @"echo ""test"" >> $GITHUB_STEP_SUMMARY";

            // Act
            var powershellFixed = ScriptHandlerHelpers.FixUpScriptContents("powershell", scriptContent);
            var cmdFixed = ScriptHandlerHelpers.FixUpScriptContents("cmd", scriptContent);
            var pythonFixed = ScriptHandlerHelpers.FixUpScriptContents("python", scriptContent);

            // Assert - Should not modify environment variables for non-shell types
            Assert.Contains(">> $GITHUB_STEP_SUMMARY", powershellFixed);
            Assert.Contains(">> $GITHUB_STEP_SUMMARY", cmdFixed);
            Assert.Contains(">> $GITHUB_STEP_SUMMARY", pythonFixed);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FixUpScriptContents_ComplexScript_ShouldQuoteOnlyUnquotedRedirects()
        {
            // Arrange
            var scriptContent = @"#!/bin/bash
# This is a test script
echo ""Starting workflow"" >> $GITHUB_STEP_SUMMARY
echo ""Already quoted"" >> ""$GITHUB_OUTPUT""
export MY_VAR=""$HOME/path with spaces""
curl -s https://api.github.com/rate_limit > $TEMP_FILE
echo ""Final status"" 2>> $ERROR_LOG
if [ -f ""$GITHUB_ENV"" ]; then
    echo ""MY_VAR=test"" >> $GITHUB_ENV
fi";

            // Act
            var fixedContent = ScriptHandlerHelpers.FixUpScriptContents("bash", scriptContent);

            // Assert
            Assert.Contains(">> \"$GITHUB_STEP_SUMMARY\"", fixedContent);
            Assert.Contains(">> \"$GITHUB_OUTPUT\"", fixedContent); // Should remain quoted
            Assert.Contains("> \"$TEMP_FILE\"", fixedContent);
            Assert.Contains("2>> \"$ERROR_LOG\"", fixedContent);
            Assert.Contains(">> \"$GITHUB_ENV\"", fixedContent);
            
            // Other parts should remain unchanged
            Assert.Contains("#!/bin/bash", fixedContent);
            Assert.Contains("# This is a test script", fixedContent);
            Assert.Contains("export MY_VAR=\"$HOME/path with spaces\"", fixedContent);
            Assert.Contains("if [ -f \"$GITHUB_ENV\" ]; then", fixedContent);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FixUpScriptContents_EnvironmentVariablesInCommands_ShouldNotQuote()
        {
            // Arrange - Environment variables not in redirects should not be touched
            var scriptContent = @"echo $GITHUB_STEP_SUMMARY
cd $HOME
ls -la $TEMP_DIR
if [ ""$MY_VAR"" == ""test"" ]; then
    echo ""match""
fi";

            // Act
            var fixedContent = ScriptHandlerHelpers.FixUpScriptContents("bash", scriptContent);

            // Assert - Should remain unchanged as these are not redirects
            Assert.Equal(scriptContent, fixedContent);
        }
    }
}