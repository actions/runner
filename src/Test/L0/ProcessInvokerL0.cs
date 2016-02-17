using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.Agent;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class ProcessInvokerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SuccessExitsWithCodeZero()
        {
            using (TestHostContext thc = new TestHostContext(nameof(ProcessInvokerL0)))
            {
                TraceSource trace = thc.GetTrace();

                Int32 exitCode = -1;
    #if OS_WINDOWS            
                exitCode = ProcessInvoker.RunExe(thc, "cmd.exe", "/c \"dir >nul\"");
    #endif

    #if (OS_OSX || OS_LINUX)
                exitCode = ProcessInvoker.RunExe(thc, "bash", "-c ls > /dev/null");
    #endif

                trace.Info("Exit Code: {0}", exitCode);
                Assert.Equal(0, exitCode);                
            }
        }
    }
}
