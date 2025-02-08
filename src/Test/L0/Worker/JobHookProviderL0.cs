using GitHub.Runner.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class JobHookProviderL0
    {
        private Mock<IExecutionContext> _executionContext;
        private JobHookProvider _jobHookProvider;

        public JobHookProviderL0()
        {
            _executionContext = new Mock<IExecutionContext>();
            _jobHookProvider = new JobHookProvider();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunHook_ShouldAddWarningAnnotation_WhenInterruptedFileExists()
        {
            // Arrange
            var hookData = new JobHookData(ActionRunStage.Main, "/opt/runs-on/hooks/interrupted");
            var interruptedFilePath = "/opt/runs-on/hooks/interrupted";

            // Create the interrupted file
            File.WriteAllText(interruptedFilePath, "test");

            try
            {
                // Act
                await _jobHookProvider.RunHook(_executionContext.Object, hookData);

                // Assert
                _executionContext.Verify(x => x.AddWarningAnnotation(It.Is<string>(msg => msg.Contains(interruptedFilePath))), Times.Once);
            }
            finally
            {
                // Clean up the interrupted file
                File.Delete(interruptedFilePath);
            }
        }
    }
}
