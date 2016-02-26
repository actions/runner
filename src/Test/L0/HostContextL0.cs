using System.Diagnostics;
using Xunit;


namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class HostContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CanLocateDefaultImplementation()
        {
            using(TestHostContext tc = new TestHostContext(nameof(HostContextL0)))
            {
                TraceSource trace = tc.GetTrace();
                Assert.Equal(typeof(TaskServer), new HostContext("L0Test").GetService<ITaskServer>().GetType());
            }
        }
    }
}
