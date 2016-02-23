using System;
using System.Diagnostics;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class ProcessInvokerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task SuccessExitsWithCodeZero()
        {
            using (TestHostContext thc = new TestHostContext(nameof(ProcessInvokerL0)))
            {
                TraceSource trace = thc.GetTrace();

                Int32 exitCode = -1;
                var processInvoker = new ProcessInvoker();
#if OS_WINDOWS
                processInvoker.Execute(thc, "", "cmd.exe", "/c \"dir >nul\"", null);
                exitCode = await processInvoker.WaitForExit(thc);
#endif

#if (OS_OSX || OS_LINUX)
                processInvoker.Execute(thc, "", "bash", "-c ls > /dev/null", null);
                exitCode = await processInvoker.WaitForExit(thc);
#endif

                trace.Info("Exit Code: {0}", exitCode);
                Assert.Equal(0, exitCode);                
            }
        }
    }
}
