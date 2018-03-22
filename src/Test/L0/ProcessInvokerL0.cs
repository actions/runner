using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ProcessInvokerL0
    {
#if OS_WINDOWS
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Common")]
        // public async Task DefaultsToCurrentSystemOemEncoding()
        // {
        //     // This test verifies that the additional code pages encoding provider is registered.
        //     // By default, only Unicode encodings, ASCII, and code page 28591 are supported. An
        //     // additional provider must be registered to support the full set of encodings that
        //     // were included in Full .NET prior to 4.6.
        //     //
        //     // For example, on an en-US box, this is required for loading the encoding for the
        //     // default console output code page '437'. Without loading the correct encoding for
        //     // code page IBM437, some characters cannot be translated correctly, e.g. write 'ç'
        //     // from powershell.exe.
        //     using (TestHostContext hc = new TestHostContext(this))
        //     {
        //         Tracing trace = hc.GetTrace();
        //         var processInvoker = new ProcessInvoker();
        //         processInvoker.Initialize(hc);
        //         var stdout = new List<string>();
        //         var stderr = new List<string>();
        //         processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
        //         {
        //             stdout.Add(e.Data);
        //         };
        //         processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
        //         {
        //             stderr.Add(e.Data);
        //         };
        //         await processInvoker.ExecuteAsync(
        //             workingDirectory: "",
        //             fileName: "powershell.exe",
        //             arguments: $@"-NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command ""Write-Host 'From STDOUT ''ç''' ; Write-Error 'From STDERR ''ç'''""",
        //             environment: null,
        //             requireExitCodeZero: false,
        //             cancellationToken: CancellationToken.None);
        //         Assert.Equal(1, stdout.Count);
        //         Assert.Equal("From STDOUT 'ç'", stdout[0]);
        //         Assert.True(stderr.Count > 0);
        //         Assert.True(stderr[0].Contains("From STDERR 'ç'"));
        //     }
        // }
#endif

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task SuccessExitsWithCodeZero()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                Int32 exitCode = -1;
                var processInvoker = new ProcessInvoker();
                processInvoker.Initialize(hc);
#if OS_WINDOWS
                exitCode = await processInvoker.ExecuteAsync("", "cmd.exe", "/c \"dir >nul\"", null, CancellationToken.None);
#endif
#if (OS_OSX || OS_LINUX)
                exitCode = await processInvoker.ExecuteAsync("", "bash", "-c echo .", null, CancellationToken.None);
#endif

                trace.Info("Exit Code: {0}", exitCode);
                Assert.Equal(0, exitCode);
            }
        }

#if !OS_WINDOWS
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
                Task execTask = processInvoker.ExecuteAsync("", "bash", $"-c \"sleep {SecondsToRun}s\"", null, tokenSource.Token);
#endif
                await Task.Delay(500);
                tokenSource.Cancel();
                await Task.WhenAny(execTask);
                Assert.True(execTask.IsCompleted);
                Assert.True(!execTask.IsFaulted);
                Assert.True(execTask.IsCanceled);
                watch.Stop();
                var elapsedSeconds = watch.ElapsedMilliseconds / 1000;
                //if cancellation fails, then execution time is more than 10 seconds
                Assert.True(elapsedSeconds < SecondsToRun / 2, $"cancellation failed, because task took too long to run. {elapsedSeconds}");
            }
        }
#endif
    }
}
