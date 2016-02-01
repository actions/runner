using System;
using Xunit;
using Microsoft.VisualStudio.Services.Agent.Worker.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class JobRunnerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]        
        public void CanCreateWorker()
        {
            JobRunner runner = new JobRunner();
            Assert.NotNull(runner);
        }
    }
}
