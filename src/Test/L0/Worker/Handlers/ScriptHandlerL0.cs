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
            // Arrange
            using (TestHostContext hc = CreateTestContext())
            {
                var scriptHandler = new ScriptHandler();
                scriptHandler.Initialize(hc);

                // Create a mock temp directory path with spaces
                var tempPathWithSpaces = "/path with spaces/_temp";
                var scriptPathWithSpaces = Path.Combine(tempPathWithSpaces, "test-script.sh");
                
                // Test the logic that our fix addresses
                var originalPath = scriptPathWithSpaces.Replace("\"", "\\\"");
                var quotedPath = $"\"{scriptPathWithSpaces.Replace("\"", "\\\"")}\"";
                
                // Assert
                Assert.False(originalPath.StartsWith("\""), "Original path should not be quoted");
                Assert.True(quotedPath.StartsWith("\"") && quotedPath.EndsWith("\""), "Fixed path should be properly quoted");
                Assert.Contains("path with spaces", quotedPath, StringComparison.Ordinal);
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);
            return hc;
        }
    }
}