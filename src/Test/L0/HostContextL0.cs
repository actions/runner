using System;
using Xunit;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class HostContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CanCreateHostContext()
        {
            HostContext hc = new HostContext("L0Test");
            Assert.NotNull(hc);
        }
    }
}
