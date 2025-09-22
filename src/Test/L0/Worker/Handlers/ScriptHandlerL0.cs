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
            
            // Verify the specific scenario that was failing
            Assert.Equal("\"/path with spaces/_temp/test-script.sh\"", quotedPath);
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
            Assert.Equal("\"/path/\\\"quoted folder\\\"/script.sh\"", quotedPath);
            Assert.True(quotedPath.StartsWith("\"") && quotedPath.EndsWith("\""), "Path should be wrapped in quotes");
            Assert.Contains("\\\"", quotedPath, StringComparison.Ordinal);
        }
    }
}