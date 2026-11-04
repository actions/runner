using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class StepHostNodeVersionL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DefaultStepHostUsesNode24()
        {
            var executionContext = new Mock<IExecutionContext>(MockBehavior.Strict);
            var stepHost = new DefaultStepHost();

            Assert.Equal("node24", await stepHost.DetermineNodeRuntimeVersion(executionContext.Object, "node24"));
            executionContext.VerifyNoOtherCalls();
        }
    }
}
