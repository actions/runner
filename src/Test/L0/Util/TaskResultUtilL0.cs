using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{
    public class TaskResultUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void TaskResultReturnCodeTranslate()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Act.
                TaskResult abandon = TaskResultUtil.TranslateFromReturnCode(TaskResultUtil.TranslateToReturnCode(TaskResult.Abandoned));
                // Actual
                Assert.Equal(TaskResult.Abandoned, abandon);

                // Act.
                TaskResult canceled = TaskResultUtil.TranslateFromReturnCode(TaskResultUtil.TranslateToReturnCode(TaskResult.Canceled));
                // Actual
                Assert.Equal(TaskResult.Canceled, canceled);

                // Act.
                TaskResult failed = TaskResultUtil.TranslateFromReturnCode(TaskResultUtil.TranslateToReturnCode(TaskResult.Failed));
                // Actual
                Assert.Equal(TaskResult.Failed, failed);

                // Act.
                TaskResult skipped = TaskResultUtil.TranslateFromReturnCode(TaskResultUtil.TranslateToReturnCode(TaskResult.Skipped));
                // Actual
                Assert.Equal(TaskResult.Skipped, skipped);

                // Act.
                TaskResult succeeded = TaskResultUtil.TranslateFromReturnCode(TaskResultUtil.TranslateToReturnCode(TaskResult.Succeeded));
                // Actual
                Assert.Equal(TaskResult.Succeeded, succeeded);

                // Act.
                TaskResult succeededWithIssues = TaskResultUtil.TranslateFromReturnCode(TaskResultUtil.TranslateToReturnCode(TaskResult.SucceededWithIssues));
                // Actual
                Assert.Equal(TaskResult.SucceededWithIssues, succeededWithIssues);

                // Act.
                TaskResult unknowReturnCode1 = TaskResultUtil.TranslateFromReturnCode(0);
                // Actual
                Assert.Equal(TaskResult.Failed, unknowReturnCode1);

                // Act.
                TaskResult unknowReturnCode2 = TaskResultUtil.TranslateFromReturnCode(1);
                // Actual
                Assert.Equal(TaskResult.Failed, unknowReturnCode2);
            }
        }
    }
}
