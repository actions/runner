using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Listener
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) && System.Environment.GetEnvironmentVariable("GHARUN_CHANGE_PROCESS_GROUP") == "1") {
                try {
                    if (Mono.Unix.Native.Syscall.setpgid(0, 0) != 0) {
                        Console.WriteLine($"Failed to change Process Group");
                    }
                } catch {
                    Console.WriteLine($"Failed to change Process Group exception");
                }
            }
            // Delete the environment variable, otherwise it is defined inside the job
            System.Environment.SetEnvironmentVariable("GHARUN_CHANGE_PROCESS_GROUP", null);

            // Add environment variables from .env file
            LoadAndSetEnv();

            using (HostContext context = new("Runner"))
            {
                return MainAsync(context, args).GetAwaiter().GetResult();
            }
        }

        // Return code definition: (this will be used by service host to determine whether it will re-launch Runner.Listener)
        // 0: Runner exit
        // 1: Terminate failure
        // 2: Retriable failure
        // 3: Exit for self update
        private async static Task<int> MainAsync(IHostContext context, string[] args)
        {
            Tracing trace = context.GetTrace(nameof(GitHub.Runner.Listener));
            trace.Info($"Runner is running on {Constants.Runner.Platform} ({Constants.Runner.PlatformArchitecture}) - {BuildConstants.RunnerPackage.PackageName}.");
            trace.Info($"RuntimeInformation: {RuntimeInformation.OSDescription}.");
            context.WritePerfCounter("RunnerProcessStarted");
            var terminal = context.GetService<ITerminal>();

#if OS_WINDOWS || OS_LINUX || OS_OSX || X86 || X64 || ARM || ARM64
            // Validate the binaries intended for one OS are not running on a different OS.
            switch (Constants.Runner.Platform)
            {
                case Constants.OSPlatform.Linux:
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        terminal.WriteLine("This runner version is built for Linux. Please install a correct build for your OS.");
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }
                    break;
                case Constants.OSPlatform.OSX:
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        terminal.WriteLine("This runner version is built for OSX. Please install a correct build for your OS.");
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }
                    break;
                case Constants.OSPlatform.Windows:
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        terminal.WriteLine("This runner version is built for Windows. Please install a correct build for your OS.");
                        return Constants.Runner.ReturnCode.TerminatedError;
                    }
#if ARM64
                        // A little hacky, but windows gives no way to differentiate between windows 10 and 11.
                        // By default only 11 supports native x64 app emulation on arm, so we only want to support windows 11
                        // https://docs.microsoft.com/en-us/windows/arm/overview#build-windows-apps-that-run-on-arm
                        // Windows 10 and 11 share a MajorVersion, so we also check the build version. Minor for both is 0, so doing < 0 doesn't really make a lot of sense.
                        if (Environment.OSVersion.Version.Major < Constants.OperatingSystem.Windows11MajorVersion || 
                            Environment.OSVersion.Version.Build < Constants.OperatingSystem.Windows11BuildVersion)
                        {
                            terminal.WriteLine("Win-arm64 runners require windows 11 or later. Please upgrade your operating system.");
                            return Constants.Runner.ReturnCode.TerminatedError;
                        }
#endif
                    break;
                default:
                    terminal.WriteLine($"Running the runner on this platform is not supported. The current platform is {RuntimeInformation.OSDescription} and it was built for {Constants.Runner.Platform.ToString()}.");
                    return Constants.Runner.ReturnCode.TerminatedError;
            }
#endif

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
                var unknownCommandlines = command.Validate();
                if (unknownCommandlines.Count > 0)
                {
                    string commandName = command.GetCommandName();
                    if (string.IsNullOrEmpty(commandName))
                    {
                        terminal.WriteError($"This command does not recognize the command-line input arguments: '{string.Join(", ", unknownCommandlines)}'. For usage refer to: .\\config.cmd --help or ./config.sh --help");
                    }
                    else
                    {
                        terminal.WriteError($"Unrecognized command-line input arguments for command {commandName}: '{string.Join(", ", unknownCommandlines)}'. For usage refer to: .\\config.cmd --help or ./config.sh --help");
                    }
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
            catch (AccessDeniedException e) when (e.ErrorCode == 1)
            {
                terminal.WriteError($"An error occured: {e.Message}");
                trace.Error(e);
                return Constants.Runner.ReturnCode.TerminatedError;
            }
            catch (RunnerNotFoundException e)
            {
                terminal.WriteError($"An error occurred: {e.Message}");
                trace.Error(e);
                return Constants.Runner.ReturnCode.TerminatedError;
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
            var rootDir = string.IsNullOrWhiteSpace(binDir) ? AppContext.BaseDirectory : new DirectoryInfo(binDir).Parent.FullName;
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
