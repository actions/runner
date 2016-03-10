using System.Diagnostics;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class HostContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetServiceReturnsSingleton()
        {
            // Arrange.
            using(TestHostContext tc = new TestHostContext(this))
            {
                TraceSource trace = tc.GetTrace();
                using (var hostContext = new HostContext($"L0Test_{nameof(GetServiceReturnsSingleton)}"))
                {
                    // Act.
                    var reference1 = hostContext.GetService<IAgentServer>();
                    var reference2 = hostContext.GetService<IAgentServer>();

                    // Assert.
                    Assert.NotNull(reference1);
                    Assert.IsType<AgentServer>(reference1);
                    Assert.NotNull(reference2);
                    Assert.True(object.ReferenceEquals(reference1, reference2));
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CreateServiceReturnsNewInstance()
        {
            // Arrange.
            using(TestHostContext tc = new TestHostContext(this))
            {
                TraceSource trace = tc.GetTrace();
                using(var hostContext = new HostContext($"L0Test_{nameof(CreateServiceReturnsNewInstance)}"))
                {
                    // Act.
                    var reference1 = hostContext.CreateService<IAgentServer>();
                    var reference2 = hostContext.CreateService<IAgentServer>();

                    // Assert.
                    Assert.NotNull(reference1);
                    Assert.IsType<AgentServer>(reference1);
                    Assert.NotNull(reference2);
                    Assert.IsType<AgentServer>(reference2);
                    Assert.False(object.ReferenceEquals(reference1, reference2));
                }
            }
        }
    }
}
