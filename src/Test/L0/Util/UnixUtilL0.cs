using System;
using System.IO;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    // Verifies UnixUtil disposes its per-call process invoker.
    public sealed class UnixUtilL0
    {
#if !OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task ExecAsync_DisposesIProcessInvoker_OnSuccess()
        {
            using (TestHostContext hc = new(this))
            {
                var tracker = new DisposalTrackingProcessInvoker();
                hc.EnqueueInstance<IProcessInvoker>(tracker);

                var unix = new UnixUtil();
                unix.Initialize(hc);

                // Use full path to bypass PATH lookup variance.
                string toolPath = File.Exists("/bin/true") ? "/bin/true" : "/usr/bin/true";
                Assert.True(File.Exists(toolPath), $"expected a no-op tool at {toolPath}");

                await unix.ExecAsync(workingDirectory: ".", toolName: toolPath, argLine: "");

                Assert.Equal(1, tracker.DisposeCount);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task ExecAsync_DisposesIProcessInvoker_EvenWhenProcessThrows()
        {
            using (TestHostContext hc = new(this))
            {
                var tracker = new DisposalTrackingProcessInvoker { ThrowOnExecute = true };
                hc.EnqueueInstance<IProcessInvoker>(tracker);

                var unix = new UnixUtil();
                unix.Initialize(hc);

                string toolPath = File.Exists("/bin/true") ? "/bin/true" : "/usr/bin/true";

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    unix.ExecAsync(workingDirectory: ".", toolName: toolPath, argLine: ""));

                Assert.Equal(1, tracker.DisposeCount);
            }
        }
#endif
    }
}
