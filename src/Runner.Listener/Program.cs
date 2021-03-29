using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Runner.Listener
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Add environment variables from .env file
            LoadAndSetEnv();

            using (HostContext context = new HostContext("Runner"))
            {
                return MainAsync(context, args).GetAwaiter().GetResult();
            }
        }

        // Return code definition: (this will be used by service host to determine whether it will re-launch Runner.Listener)
        // 0: Runner exit
        // 1: Terminate failure
        // 2: Retriable failure
        // 3: Exit for self update
        private static async Task<int> MainAsync(IHostContext context, string[] args)
        {
            var trace = context.GetTrace(nameof(GitHub.Runner.Listener));
            trace.Info($"Runner is built for {Constants.Runner.Platform} ({Constants.Runner.PlatformArchitecture}) - {BuildConstants.RunnerPackage.PackageName}.");
            trace.Info($"RuntimeInformation: {RuntimeInformation.OSDescription}.");
            context.WritePerfCounter("RunnerProcessStarted");
            var terminal = context.GetService<ITerminal>();

            var validation = PlatformValidation.Validate(Constants.Runner.Platform);
            if (!validation.IsValid)
            {
                terminal.WriteLine(validation.Message);
                return Constants.Runner.ReturnCode.TerminatedError;
            }
            
            try
            {
                trace.Info($"Version: {BuildConstants.RunnerPackage.Version}");
                trace.Info($"Commit: {BuildConstants.Source.CommitHash}");
                trace.Info($"Culture: {CultureInfo.CurrentCulture.Name}");
                trace.Info($"UI Culture: {CultureInfo.CurrentUICulture.Name}");

                // Validate directory permissions.
                string runnerDirectory = context.GetDirectory(WellKnownDirectory.Root);
                trace.Info($"Validating directory permissions for: '{runnerDirectory}'");
                try
                {
                    IOUtil.ValidateExecutePermission(runnerDirectory);
                }
                catch (Exception e)
                {
                    terminal.WriteError($"An error occurred: {e.Message}");
                    trace.Error(e);
                    return Constants.Runner.ReturnCode.TerminatedError;
                }

                // Parse the command line args.
                var command = new CommandSettings(context, args);
                trace.Info("Arguments parsed");

                // Up front validation, warn for unrecognized commandline args.
                var unknownCommandLines = command.Validate();
                if (unknownCommandLines.Count > 0)
                {
                    terminal.WriteError($"Unrecognized command-line input arguments: '{string.Join(", ", unknownCommandLines)}'. For usage refer to: .\\config.cmd --help or ./config.sh --help");
                }

                // Defer to the Runner class to execute the command.
                IRunner runner = context.GetService<IRunner>();
                try
                {
                    var returnCode = await runner.ExecuteCommand(command);
                    trace.Info($"Runner execution has finished with return code {returnCode}");
                    return returnCode;
                }
                catch (OperationCanceledException) when (context.RunnerShutdownToken.IsCancellationRequested)
                {
                    trace.Info("Runner execution been cancelled.");
                    return Constants.Runner.ReturnCode.Success;
                }
                catch (NonRetryableException e)
                {
                    terminal.WriteError($"An error occurred: {e.Message}");
                    trace.Error(e);
                    return Constants.Runner.ReturnCode.TerminatedError;
                }

            }
            catch (Exception e)
            {
                terminal.WriteError($"An error occurred: {e.Message}");
                trace.Error(e);
                return Constants.Runner.ReturnCode.RetryableError;
            }
        }

        private static void LoadAndSetEnv()
        {
            var binDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var rootDir = new DirectoryInfo(binDir).Parent.FullName;
            string envFile = Path.Combine(rootDir, ".env");
            if (File.Exists(envFile))
            {
                var envContents = File.ReadAllLines(envFile);
                foreach (var env in envContents)
                {
                    if (!string.IsNullOrEmpty(env))
                    {
                        var separatorIndex = env.IndexOf('=');
                        if (separatorIndex > 0)
                        {
                            string envKey = env.Substring(0, separatorIndex);
                            string envValue = null;
                            if (env.Length > separatorIndex + 1)
                            {
                                envValue = env.Substring(separatorIndex + 1);
                            }

                            Environment.SetEnvironmentVariable(envKey, envValue);
                        }
                    }
                }
            }
        }
    }
}
