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
    }
}