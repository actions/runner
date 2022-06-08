using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
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
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                string envName = Guid.NewGuid().ToString();
                string envValue = Guid.NewGuid().ToString();

                Process sleep = null;
                try
                {
#if OS_WINDOWS
                    string node = Path.Combine(TestUtil.GetSrcPath(), @"..\_layout\externals\node16\bin\node");
#else
                    string node = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/externals/node16/bin/node");
                    hc.EnqueueInstance<IProcessInvoker>(new ProcessInvokerWrapper());
#endif
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

                    Assert.True(false, "Fail to retrive process environment variable.");
                }
                finally
                {
                    sleep?.Kill();
                }
            }
        }
    }
}
