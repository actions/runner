using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Diagnostics;

namespace GitHub.Runner.Worker
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            using (HostContext context = new("Worker"))
            {
                return MainAsync(context, args).GetAwaiter().GetResult();
            }
        }

        public static async Task<int> MainAsync(IHostContext context, string[] args)
        {
            Tracing trace = context.GetTrace(nameof(GitHub.Runner.Worker));
            if (StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_ATTACH_DEBUGGER")))
            {
                await WaitForDebugger(trace);
            }

            // We may want to consider registering this handler in Worker.cs, similiar to the unloading/SIGTERM handler
            //ITerminal registers a CTRL-C handler, which keeps the Runner.Worker process running
            //and lets the Runner.Listener handle gracefully the exit.
            var term = context.GetService<ITerminal>();
            try
            {
#if !OS_WINDOWS
                MoveToOwnProcessGroup(trace);
#endif
                trace.Info($"Version: {BuildConstants.RunnerPackage.Version}");
                trace.Info($"Commit: {BuildConstants.Source.CommitHash}");
                trace.Info($"Culture: {CultureInfo.CurrentCulture.Name}");
                trace.Info($"UI Culture: {CultureInfo.CurrentUICulture.Name}");
                context.WritePerfCounter("WorkerProcessStarted");

                // Validate args.
                ArgUtil.NotNull(args, nameof(args));
                ArgUtil.Equal(3, args.Length, nameof(args.Length));
                ArgUtil.NotNullOrEmpty(args[0], $"{nameof(args)}[0]");
                ArgUtil.Equal("spawnclient", args[0].ToLowerInvariant(), $"{nameof(args)}[0]");
                ArgUtil.NotNullOrEmpty(args[1], $"{nameof(args)}[1]");
                ArgUtil.NotNullOrEmpty(args[2], $"{nameof(args)}[2]");
                var worker = context.GetService<IWorker>();

                // Run the worker.
                return await worker.RunAsync(
                    pipeIn: args[1],
                    pipeOut: args[2]);
            }
            catch (Exception ex)
            {
                // Populate any exception that cause worker failure back to runner.
                Console.WriteLine(ex.ToString());
                try
                {
                    trace.Error(ex);
                }
                catch (Exception e)
                {
                    // make sure we don't crash the app on trace error.
                    // since IOException will throw when we run out of disk space.
                    Console.WriteLine(e.ToString());
                }
            }

            return 1;
        }



        /// <summary>
        /// Runner.Worker is started by Runner.Listener in a separate process,
        /// so the two can't be debugged in the same session.
        /// This method halts the Runner.Worker process until a debugger is attached,
        /// allowing a developer to debug Runner.Worker from start to finish.
        /// </summary>
        private static async Task WaitForDebugger(Tracing trace)
        {
            trace.Info($"Waiting for a debugger to be attached. Edit the 'GITHUB_ACTIONS_RUNNER_ATTACH_DEBUGGER' environment variable to toggle this feature.");
            int waitInSeconds = 20;
            while (!Debugger.IsAttached && waitInSeconds-- > 0)
            {
                trace.Info($"Waiting for a debugger to be attached. {waitInSeconds} seconds left.");
                await Task.Delay(1000);
            }
            Debugger.Break();
        }

#if !OS_WINDOWS
        /// <summary>
        /// Without a controlling terminal, Runner.Worker inherits the process group of whatever launched the runner,
        /// so job steps share a process group with Runner.Listener and its supervisor. That group is orphaned, which means
        /// a step that leaves a stopped process behind when another process in the group exits (for example .NET's
        /// Process.Kill(entireProcessTree: true), which SIGSTOPs before it kills) makes the kernel send SIGHUP and SIGCONT
        /// to the whole group, killing the runner. Moving the worker into its own group keeps those signals away from
        /// Runner.Listener, and the new group is not orphaned while the listener (same session, other group) is its parent.
        /// With a controlling terminal, Ctrl+C and job control already behave, so the process group is left as it is.
        /// With RUNNER_MANUALLY_TRAP_SIG, run.sh starts the runner in a job-control group that is not orphaned and
        /// forwards SIGINT to that whole group, so the process group is left as it is there too.
        /// https://github.com/actions/runner-images/issues/14833
        /// </summary>
        private static void MoveToOwnProcessGroup(Tracing trace)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUNNER_MANUALLY_TRAP_SIG")))
            {
                trace.Info("RUNNER_MANUALLY_TRAP_SIG is set, keep the current process group.");
                return;
            }

            try
            {
                using (new FileStream("/dev/tty", FileMode.Open, FileAccess.Read))
                {
                    trace.Info("Controlling terminal found, keep the current process group.");
                    return;
                }
            }
            catch (IOException)
            {
                // No controlling terminal (ENXIO) or no /dev/tty at all.
            }
            catch (Exception ex)
            {
                trace.Info($"Unable to check for a controlling terminal, keep the current process group: {ex.Message}");
                return;
            }

            try
            {
                if (setpgid(0, 0) == 0)
                {
                    trace.Info($"Moved into own process group {getpgrp()}.");
                }
                else
                {
                    trace.Warning($"Failed to move into own process group, errno {Marshal.GetLastPInvokeError()}.");
                }
            }
            catch (Exception ex)
            {
                // Best effort, the job can still run in the inherited process group.
                trace.Warning($"Failed to move into own process group: {ex.Message}");
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int setpgid(int pid, int pgid);

        [DllImport("libc")]
        private static extern int getpgrp();
#endif
    }
}
