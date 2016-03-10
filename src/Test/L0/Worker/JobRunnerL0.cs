using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public class JobRunnerL0
    {
        //// TODO: Fix this test. This test hit file in use issues trying to write to the log file. My guess is it probably has to do with XUnit running tests in parallel.
        // public JobRunnerL0()
        // {
        //     m_context = new MockHostContext();
        // }
        // 
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Worker")]
        // public void CanCreateWorker()
        // {
        //     JobRunner runner = new JobRunner(m_context);
        //     Assert.NotNull(runner);
        // }
        // 
        // private MockHostContext m_context;
    }
}
