using System;
using Xunit;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class ProcessInvokerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void InvokeProcess()
        {
            IHostContext hc = new TestHostContext();

            Int32 exitCode = -1;
#if OS_WINDOWS            
            exitCode = ProcessInvoker.RunExe(hc, "cmd.exe", "/c \"dir >nul\"");
#endif

#if OS_OSX            
            exitCode = ProcessInvoker.RunExe(hc, "bash", "-c ls");
#endif

#if OS_LINUX            
            exitCode = ProcessInvoker.RunExe(hc, "bash", "-c ls");
#endif

            Assert.Equal(0, exitCode);
        }
    }
}
