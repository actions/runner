using System;
using Xunit;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class ContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CanCreateHostContext()
        {
            HostContext hc = new HostContext();
            Assert.NotNull(hc);
        }
    }
}
