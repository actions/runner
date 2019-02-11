using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.TeamFoundation.Framework.Common;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ProcessInvokerL0
    {
#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DefaultsToCurrentSystemOemEncoding()
        {
            // This test verifies that the additional code pages encoding provider is registered.
            // By default, only Unicode encodings, ASCII, and code page 28591 are supported. An
            // additional provider must be registered to support the full set of encodings that
            // were included in Full .NET prior to 4.6.
            //
            // For example, on an en-US box, this is required for loading the encoding for the
            // default console output code page '437'. Without loading the correct encoding for
            // code page IBM437, some characters cannot be translated correctly, e.g. write 'ç'
            // from powershell.exe.
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                var processInvoker = new ProcessInvokerWrapper();
                processInvoker.Initialize(hc);
                var stdout = new List<string>();
                var stderr = new List<string>();
                processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                {
                    stdout.Add(e.Data);
                };
                processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                {
                    stderr.Add(e.Data);
                };
                await processInvoker.ExecuteAsync(
                    workingDirectory: "",
                    fileName: "powershell.exe",
                    arguments: $@"-NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command ""Write-Host 'From STDOUT ''ç''' ; Write-Error 'From STDERR ''ç'''""",
                    environment: null,
                    requireExitCodeZero: false,
                    cancellationToken: CancellationToken.None);
                Assert.Equal(1, stdout.Count);
                Assert.Equal("From STDOUT 'ç'", stdout[0]);
                Assert.True(stderr.Count > 0);
                Assert.True(stderr[0].Contains("From STDERR 'ç'"));
            }
        }
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
                var processInvoker = new ProcessInvokerWrapper();
                processInvoker.Initialize(hc);
#if OS_WINDOWS
                exitCode = await processInvoker.ExecuteAsync("", "cmd.exe", "/c \"dir >nul\"", null, CancellationToken.None);
#else
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
                var processInvoker = new ProcessInvokerWrapper();
                processInvoker.Initialize(hc);
                Stopwatch watch = Stopwatch.StartNew();
                Task execTask;
#if OS_WINDOWS
                execTask = processInvoker.ExecuteAsync("", "cmd.exe", $"/c \"choice /T {SecondsToRun} /D y\"", null, tokenSource.Token);
#else
                execTask = processInvoker.ExecuteAsync("", "bash", $"-c \"sleep {SecondsToRun}s\"", null, tokenSource.Token);
#endif
                await Task.Delay(500);
                tokenSource.Cancel();
                try
                {
                    await execTask;
                }
                catch (OperationCanceledException)
                {
                    trace.Info("Get expected OperationCanceledException.");
                }

                Assert.True(execTask.IsCompleted);
                Assert.True(!execTask.IsFaulted);
                Assert.True(execTask.IsCanceled);
                watch.Stop();
                long elapsedSeconds = watch.ElapsedMilliseconds / 1000;

#if ARM
                // if cancellation fails, then execution time is more than 15 seconds
                // longer time to compensate for a slower ARM environment (e.g. Raspberry Pi)
                long expectedSeconds = (SecondsToRun * 3) / 4;
#else
                // if cancellation fails, then execution time is more than 10 seconds
                long expectedSeconds = SecondsToRun / 2;
#endif

                Assert.True(elapsedSeconds <= expectedSeconds, $"cancellation failed, because task took too long to run. {elapsedSeconds}");
            }
        }
#endif

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RedirectSTDINCloseStream()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                Int32 exitCode = -1;
                InputQueue<string> redirectSTDIN = new InputQueue<string>();
                List<string> stdout = new List<string>();
                redirectSTDIN.Enqueue("Single line of STDIN");

                var processInvoker = new ProcessInvokerWrapper();
                processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                 {
                     stdout.Add(e.Data);
                 };

                processInvoker.Initialize(hc);
#if OS_WINDOWS
                var proc = processInvoker.ExecuteAsync("", "cmd.exe", "/c more", null, false, null, false, redirectSTDIN, false, false, cancellationTokenSource.Token);
#else
                var proc = processInvoker.ExecuteAsync("", "bash", "-c \"read input; echo $input; read input; echo $input; read input; echo $input;\"", null, false, null, false, redirectSTDIN, false, false, cancellationTokenSource.Token);
#endif
                redirectSTDIN.Enqueue("More line of STDIN");
                redirectSTDIN.Enqueue("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Enqueue("More line of STDIN");
                redirectSTDIN.Enqueue("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Enqueue("More line of STDIN");
                cancellationTokenSource.CancelAfter(100);

                try
                {
                    exitCode = await proc;
                    trace.Info("Exit Code: {0}", exitCode);
                }
                catch (Exception ex)
                {
                    trace.Error(ex);
                }

                trace.Info("STDOUT: {0}", string.Join(Environment.NewLine, stdout));
                Assert.False(stdout.Contains("More line of STDIN"), "STDIN should be closed after first input line.");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RedirectSTDINKeepStreamOpen()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                Int32 exitCode = -1;
                InputQueue<string> redirectSTDIN = new InputQueue<string>();
                List<string> stdout = new List<string>();
                redirectSTDIN.Enqueue("Single line of STDIN");

                var processInvoker = new ProcessInvokerWrapper();
                processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                 {
                     stdout.Add(e.Data);
                 };

                processInvoker.Initialize(hc);
#if OS_WINDOWS
                var proc = processInvoker.ExecuteAsync("", "cmd.exe", "/c more", null, false, null, false, redirectSTDIN, false, true, cancellationTokenSource.Token);
#else
                var proc = processInvoker.ExecuteAsync("", "bash", "-c \"read input; echo $input; read input; echo $input; read input; echo $input;\"", null, false, null, false, redirectSTDIN, false, true, cancellationTokenSource.Token);
#endif
                redirectSTDIN.Enqueue("More line of STDIN");
                redirectSTDIN.Enqueue("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Enqueue("More line of STDIN");
                redirectSTDIN.Enqueue("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Enqueue("More line of STDIN");
                cancellationTokenSource.CancelAfter(100);

                try
                {
                    exitCode = await proc;
                    trace.Info("Exit Code: {0}", exitCode);
                }
                catch (Exception ex)
                {
                    trace.Error(ex);
                }

                trace.Info("STDOUT: {0}", string.Join(Environment.NewLine, stdout));
                Assert.True(stdout.Contains("More line of STDIN"), "STDIN should keep open and accept more inputs after first input line.");
            }
        }
    }
}
