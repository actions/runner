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
            using (TestHostContext hc = new TestHostContext(this))
            {
                TraceSource trace = hc.GetTrace();

                Int32 exitCode = -1;
                var processInvoker = new ProcessInvoker();
                processInvoker.Initialize(hc);
#if OS_WINDOWS
                processInvoker.Execute("", "cmd.exe", "/c \"dir >nul\"", null);
#endif
#if (OS_OSX || OS_LINUX)
                processInvoker.Execute("", "bash", "-c echo .", null);
#endif
                exitCode = await processInvoker.WaitForExit(hc.CancellationToken);

                trace.Info("Exit Code: {0}", exitCode);
                Assert.Equal(0, exitCode);                
            }
        }
    }
}
