using System;
using System.Diagnostics;
using Xunit;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class HostContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetServiceReturnsSingleton()
        {
            // Arrange.
            using(TestHostContext tc = new TestHostContext(nameof(HostContextL0)))
            {
                TraceSource trace = tc.GetTrace();
                using (var hostContext = new HostContext($"L0Test_{nameof(GetServiceReturnsSingleton)}"))
                {
                    // Act.
                    var reference1 = hostContext.GetService<ITaskServer>();
                    var reference2 = hostContext.GetService<ITaskServer>();

                    // Assert.
                    Assert.NotNull(reference1);
                    Assert.IsType<TaskServer>(reference1);
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
            using(TestHostContext tc = new TestHostContext(nameof(HostContextL0)))
            {
                TraceSource trace = tc.GetTrace();
                using(var hostContext = new HostContext($"L0Test_{nameof(CreateServiceReturnsNewInstance)}"))
                {
                    // Act.
                    var reference1 = hostContext.CreateService<ITaskServer>();
                    var reference2 = hostContext.CreateService<ITaskServer>();

                    // Assert.
                    Assert.NotNull(reference1);
                    Assert.IsType<TaskServer>(reference1);
                    Assert.NotNull(reference2);
                    Assert.IsType<TaskServer>(reference2);
                    Assert.False(object.ReferenceEquals(reference1, reference2));
                }
            }
        }
    }
}
