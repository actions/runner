using System;
using Xunit;
using Microsoft.VisualStudio.Services.Agent.Worker.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class JobRunnerL0
    {
        public JobRunnerL0()
        {
            m_context = new MockHostContext();
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]        
        public void CanCreateWorker()
        {
            JobRunner runner = new JobRunner(m_context);
            Assert.NotNull(runner);
        }
        
        private MockHostContext m_context;
    }
}
