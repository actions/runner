using System;
using System.Diagnostics;
using System.Threading;
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
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = hc.GetTrace();

                Int32 exitCode = -1;
                var processInvoker = new ProcessInvoker();
                processInvoker.Initialize(hc);
#if OS_WINDOWS
                exitCode = await processInvoker.ExecuteAsync("", "cmd.exe", "/c \"dir >nul\"", null, tokenSource.Token);
#endif
#if (OS_OSX || OS_LINUX)
                exitCode = await processInvoker.ExecuteAsync("", "bash", "-c echo .", null, tokenSource.Token);
#endif

                trace.Info("Exit Code: {0}", exitCode);
                Assert.Equal(0, exitCode);
            }
        }


        //Run a process that normally takes 20sec to finish and cancel it.        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task TestCancel()
        {
            const int SecondsToRun = 20;
            using (TestHostContext hc = new TestHostContext(this))
            using (var tokenSource = new CancellationTokenSource())
            {
                Tracing trace = hc.GetTrace();
                var processInvoker = new ProcessInvoker();
                processInvoker.Initialize(hc);
                Stopwatch watch = Stopwatch.StartNew();
#if OS_WINDOWS
                Task execTask = processInvoker.ExecuteAsync("", "cmd.exe", $"/c \"choice /T {SecondsToRun} /D y\"", null, tokenSource.Token);
#endif
#if (OS_OSX || OS_LINUX)
                Task execTask = processInvoker.ExecuteAsync("", "bash", $"--init-file <(echo \"sleep {SecondsToRun}s\")", null, tokenSource.Token);
#endif
                tokenSource.Cancel();
                await Task.WhenAny(new Task[] { execTask });
                Assert.True(execTask.IsCompleted);
                Assert.True(!execTask.IsFaulted);
                Assert.True(execTask.IsCanceled);
                watch.Stop();
                var elapsedSeconds = watch.ElapsedMilliseconds / 1000;
                //if cancellation fails, then execution time is more than 10 seconds
                Assert.True(elapsedSeconds < SecondsToRun / 2, "cancellation failed, because task took too long to run");
            }
        }
    }
}
