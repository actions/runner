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
        public void CanLocateDefaultImplementation()
        {
            using(TestHostContext thc = new TestHostContext("HostContextL0", "CanLocateDefaultImplementation"))
            {
                TraceSource trace = thc.GetTrace();
                Assert.Equal(typeof(TaskServer), new HostContext("L0Test").GetService<ITaskServer>().GetType());
            }
        }
    }
}
