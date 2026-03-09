using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class ProcessExtensionL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task SuccessReadProcessEnv()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                string envName = Guid.NewGuid().ToString();
                string envValue = Guid.NewGuid().ToString();

                Process sleep = null;
                try
                {
#if OS_WINDOWS
                    string nodeFallback = Path.Combine(TestUtil.GetSrcPath(), @"..\_layout\externals\node20\bin\node.exe");
#else
                    hc.EnqueueInstance<IProcessInvoker>(new ProcessInvokerWrapper());
                    string nodeFallback = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/externals/node20/bin/node");
#endif
                    string node = FindInPath("node") ?? nodeFallback;
                    var startInfo = new ProcessStartInfo(node, "-e \"setTimeout(function(){{}}, 15 * 1000);\"");
                    startInfo.Environment[envName] = envValue;
                    sleep = Process.Start(startInfo);

                    var timeout = Process.GetProcessById(sleep.Id);
                    while (timeout == null)
                    {
                        await Task.Delay(500);
                        timeout = Process.GetProcessById(sleep.Id);
                    }

                    try
                    {
                        trace.Info($"Read env from {timeout.Id}");
                        var value = timeout.GetEnvironmentVariable(hc, envName);
                        if (string.Equals(value, envValue, StringComparison.OrdinalIgnoreCase))
                        {
                            trace.Info($"Find the env.");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        trace.Error(ex);
                    }

                    Assert.Fail("Failed to retrieve process environment variable.");
                }
                finally
                {
                    sleep?.Kill();
                }
            }
        }
        private static string FindInPath(string executable)
        {
            foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                string full = Path.Combine(dir, executable);
                if (File.Exists(full))
                {
                    return full;
                }
            }
            return null;
        }
    }
}
