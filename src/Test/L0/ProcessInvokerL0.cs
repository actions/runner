using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.Runner.Common.Util;
using System.Threading.Channels;
using GitHub.Runner.Sdk;
using System.Linq;

namespace GitHub.Runner.Common.Tests
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
                Assert.Contains("From STDERR 'ç'", stderr[0]);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task SetCIEnv()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                var existingCI = Environment.GetEnvironmentVariable("CI");
                try
                {
                    // Clear out CI and make sure process invoker sets it.
                    Environment.SetEnvironmentVariable("CI", null);

                    Tracing trace = hc.GetTrace();

                    Int32 exitCode = -1;
                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    var stdout = new List<string>();
                    var stderr = new List<string>();
                    processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                    {
                        trace.Info(e.Data);
                        stdout.Add(e.Data);
                    };
                    processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                    {
                        trace.Info(e.Data);
                        stderr.Add(e.Data);
                    };
#if OS_WINDOWS
                    exitCode = await processInvoker.ExecuteAsync("", "cmd.exe", "/c \"echo %CI%\"", null, CancellationToken.None);
#else
                    exitCode = await processInvoker.ExecuteAsync("", "bash", "-c \"echo $CI\"", null, CancellationToken.None);
#endif

                    trace.Info("Exit Code: {0}", exitCode);
                    Assert.Equal(0, exitCode);

                    Assert.Equal("true", stdout.First(x => !string.IsNullOrWhiteSpace(x)));
                }
                finally
                {
                    Environment.SetEnvironmentVariable("CI", existingCI);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task KeepExistingCIEnv()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                var existingCI = Environment.GetEnvironmentVariable("CI");
                try
                {
                    // Clear out CI and make sure process invoker sets it.
                    Environment.SetEnvironmentVariable("CI", null);

                    Tracing trace = hc.GetTrace();

                    Int32 exitCode = -1;
                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    var stdout = new List<string>();
                    var stderr = new List<string>();
                    processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                    {
                        trace.Info(e.Data);
                        stdout.Add(e.Data);
                    };
                    processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                    {
                        trace.Info(e.Data);
                        stderr.Add(e.Data);
                    };
#if OS_WINDOWS
                    exitCode = await processInvoker.ExecuteAsync("", "cmd.exe", "/c \"echo %CI%\"", new Dictionary<string, string>() { { "CI", "false" } }, CancellationToken.None);
#else
                    exitCode = await processInvoker.ExecuteAsync("", "bash", "-c \"echo $CI\"", new Dictionary<string, string>() { { "CI", "false" } }, CancellationToken.None);
#endif

                    trace.Info("Exit Code: {0}", exitCode);
                    Assert.Equal(0, exitCode);

                    Assert.Equal("false", stdout.First(x => !string.IsNullOrWhiteSpace(x)));
                }
                finally
                {
                    Environment.SetEnvironmentVariable("CI", existingCI);
                }
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
                Channel<string> redirectSTDIN = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
                List<string> stdout = new List<string>();
                redirectSTDIN.Writer.TryWrite("Single line of STDIN");

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
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
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
                Channel<string> redirectSTDIN = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
                List<string> stdout = new List<string>();
                redirectSTDIN.Writer.TryWrite("Single line of STDIN");

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
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
                await Task.Delay(100);
                redirectSTDIN.Writer.TryWrite("More line of STDIN");
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

#if OS_LINUX
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task OomScoreAdjIsWriten_Default()
        {
            // We are on a system that supports oom_score_adj in procfs as assumed by ProcessInvoker
            string testProcPath = $"/proc/{Process.GetCurrentProcess().Id}/oom_score_adj";
            if (File.Exists(testProcPath))
            {
                using (TestHostContext hc = new TestHostContext(this))
                using (var tokenSource = new CancellationTokenSource())
                {
                    Tracing trace = hc.GetTrace();
                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    int oomScoreAdj = -9999;
                    processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                    {
                        oomScoreAdj = int.Parse(e.Data);
                        tokenSource.Cancel();
                    };
                    try
                    {
                        var proc = await processInvoker.ExecuteAsync("", "bash", "-c \"cat /proc/$$/oom_score_adj\"", null, false, null, false, null, false, false,
                                                            highPriorityProcess: false,
                                                            cancellationToken: tokenSource.Token);
                        Assert.Equal(500, oomScoreAdj);
                    }
                    catch (OperationCanceledException)
                    {
                        trace.Info("Caught expected OperationCanceledException");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task OomScoreAdjIsWriten_FromEnv()
        {
            // We are on a system that supports oom_score_adj in procfs as assumed by ProcessInvoker
            string testProcPath = $"/proc/{Process.GetCurrentProcess().Id}/oom_score_adj";
            if (File.Exists(testProcPath))
            {
                using (TestHostContext hc = new TestHostContext(this))
                using (var tokenSource = new CancellationTokenSource())
                {
                    Tracing trace = hc.GetTrace();
                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    int oomScoreAdj = -9999;
                    processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                    {
                        oomScoreAdj = int.Parse(e.Data);
                        tokenSource.Cancel();
                    };
                    try
                    {
                        var proc = await processInvoker.ExecuteAsync("", "bash", "-c \"cat /proc/$$/oom_score_adj\"",
                                                                new Dictionary<string, string> { {"PIPELINE_JOB_OOMSCOREADJ", "1234"} },
                                                                false, null, false, null, false, false,
                                                                highPriorityProcess: false,
                                                                cancellationToken: tokenSource.Token);
                        Assert.Equal(1234, oomScoreAdj);
                    }
                    catch (OperationCanceledException)
                    {
                        trace.Info("Caught expected OperationCanceledException");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task OomScoreAdjIsInherited()
        {
            // We are on a system that supports oom_score_adj in procfs as assumed by ProcessInvoker
            string testProcPath = $"/proc/{Process.GetCurrentProcess().Id}/oom_score_adj";
            if (File.Exists(testProcPath))
            {
                int testProcOomScoreAdj = 123;
                File.WriteAllText(testProcPath, testProcOomScoreAdj.ToString());
                using (TestHostContext hc = new TestHostContext(this))
                using (var tokenSource = new CancellationTokenSource())
                {
                    Tracing trace = hc.GetTrace();
                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    int oomScoreAdj = -9999;
                    processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
                    {
                        oomScoreAdj = int.Parse(e.Data);
                        tokenSource.Cancel();
                    };
                    try
                    {
                        var proc = await processInvoker.ExecuteAsync("", "bash", "-c \"cat /proc/$$/oom_score_adj\"", null, false, null, false, null, false, false,
                                                            highPriorityProcess: true,
                                                            cancellationToken: tokenSource.Token);
                        Assert.Equal(123, oomScoreAdj);
                    }
                    catch (OperationCanceledException)
                    {
                        trace.Info("Caught expected OperationCanceledException");
                    }
                }
            }
        }
#endif
    }
}
