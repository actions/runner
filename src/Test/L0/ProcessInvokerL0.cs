using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

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
                processInvoker.Initialize(thc);
#if OS_WINDOWS
                processInvoker.Execute("", "cmd.exe", "/c \"dir >nul\"", null);
#endif
#if (OS_OSX || OS_LINUX)
                processInvoker.Execute("", "bash", "-c ls > /dev/null", null);
#endif
                exitCode = await processInvoker.WaitForExit(thc.CancellationToken);

                trace.Info("Exit Code: {0}", exitCode);
                Assert.Equal(0, exitCode);                
            }
        }
    }
}
