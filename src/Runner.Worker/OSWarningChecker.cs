using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(OSWarningChecker))]
    public interface IOSWarningChecker : IRunnerService
    {
        Task CheckOSAsync(IExecutionContext context);
    }

    public sealed class OSWarningChecker : RunnerService, IOSWarningChecker
    {
        public async Task CheckOSAsync(IExecutionContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            if (!context.Global.Variables.System_TestDotNet8Compatibility)
            {
                return;
            }

            context.Output("Testing runner upgrade compatibility");
            List<string> output = new();
            object outputLock = new();
            try
            {
                using (var process = HostContext.CreateService<IProcessInvoker>())
                {
                    process.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stdout)
                    {
                        if (!string.IsNullOrEmpty(stdout.Data))
                        {
                            lock (outputLock)
                            {
                                output.Add(stdout.Data);
                                Trace.Info(stdout.Data);
                            }
                        }
                    };

                    process.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stderr)
                    {
                        if (!string.IsNullOrEmpty(stderr.Data))
                        {
                            lock (outputLock)
                            {
                                output.Add(stderr.Data);
                                Trace.Error(stderr.Data);
                            }
                        }
                    };

                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        int exitCode = await process.ExecuteAsync(
                            workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Root),
                            fileName: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), "testDotNet8Compatibility", $"TestDotNet8Compatibility{IOUtil.ExeExtension}"),
                            arguments: string.Empty,
                            environment: null,
                            cancellationToken: cancellationTokenSource.Token);

                        var outputStr = string.Join("\n", output).Trim();
                        if (exitCode != 0 || !string.Equals(outputStr, "Hello from .NET 8!", StringComparison.Ordinal))
                        {
                            var warningMessage = context.Global.Variables.System_DotNet8CompatibilityWarning;
                            if (!string.IsNullOrEmpty(warningMessage))
                            {
                                context.Warning(warningMessage);
                            }

                            context.Global.JobTelemetry.Add(new JobTelemetry() { Type = JobTelemetryType.General, Message = $".NET 8 OS compatibility test failed with exit code '{exitCode}' and output: {GetShortOutput(output)}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Error("An error occurred while testing .NET 8 compatibility'");
                Trace.Error(ex);
                context.Global.JobTelemetry.Add(new JobTelemetry() { Type = JobTelemetryType.General, Message = $".NET 8 OS compatibility test encountered exception type '{ex.GetType().FullName}', message: '{ex.Message}', process output: '{GetShortOutput(output)}'" });
            }
        }

        private static string GetShortOutput(List<string> output)
        {
            var outputStr = string.Join("\n", output).Trim();
            return outputStr.Length > 200 ? string.Concat(outputStr.Substring(0, 200), "[...]") : outputStr;
        }
    }
}
