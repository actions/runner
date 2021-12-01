using System;
using System.Globalization;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Diagnostics;
using GitHub.Runner.Worker.Container;

namespace GitHub.Runner.Worker
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            using (HostContext context = new HostContext("Worker"))
            {
                // Enable until kubernetes support is fully enabled
                // configureCommandManager(context);
                return MainAsync(context, args).GetAwaiter().GetResult();
            }
        }

        public static void configureCommandManager(HostContext context) {
            Tracing trace = context.GetTrace(nameof(GitHub.Runner.Worker));
            var containerProvider = Environment.GetEnvironmentVariable("RUNNER_CONTAINER_PROVIDER");
            switch (containerProvider)
            {
                case "docker":
                    trace.Info($"Registering DockerCommandManager as IDockerCommandManager");
                    context.RegisterService(typeof(IContainerCommandManager), typeof(DockerCommandManager));
                    break;
                case "kubernetes":
                    trace.Info($"Registering KubernetesCommandManager as IDockerCommandManager");
                    context.RegisterService(typeof(IContainerCommandManager), typeof(KubernetesCommandManager));
                    break;
            }
            // If no environment variable is set, it will default to whatever it's defaulted in IContainerCommandManager
            // which in this case it defaults to DockerCommandManager
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
    }
}
