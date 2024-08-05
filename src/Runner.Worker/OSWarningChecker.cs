using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(OSWarningChecker))]
    public interface IOSWarningChecker : IRunnerService
    {
        Task CheckOSAsync(IExecutionContext context, IList<OSWarning> osWarnings);
    }

#if OS_WINDOWS || OS_OSX
    public sealed class OSWarningChecker : RunnerService, IOSWarningChecker
    {
        public Task CheckOSAsync(IExecutionContext context, IList<OSWarning> osWarnings)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(osWarnings, nameof(osWarnings));
            return Task.CompletedTask;
        }
    }
#else
    public sealed class OSWarningChecker : RunnerService, IOSWarningChecker
    {
        private static readonly TimeSpan s_matchTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly RegexOptions s_regexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

        public async Task CheckOSAsync(IExecutionContext context, IList<OSWarning> osWarnings)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(osWarnings, nameof(osWarnings));
            foreach (var osWarning in osWarnings)
            {
                if (string.IsNullOrEmpty(osWarning.FilePath))
                {
                    Trace.Error("The file path is not specified in the OS warning check.");
                    continue;
                }

                if (string.IsNullOrEmpty(osWarning.RegularExpression))
                {
                    Trace.Error("The regular expression is not specified in the OS warning check.");
                    continue;
                }

                if (string.IsNullOrEmpty(osWarning.Warning))
                {
                    Trace.Error("The warning message is not specified in the OS warning check.");
                    continue;
                }

                try
                {
                    if (File.Exists(osWarning.FilePath))
                    {
                        var lines = await File.ReadAllLinesAsync(osWarning.FilePath, context.CancellationToken);
                        var regex = new Regex(osWarning.RegularExpression, s_regexOptions, s_matchTimeout);
                        foreach (var line in lines)
                        {
                            if (regex.IsMatch(line))
                            {
                                context.Warning(osWarning.Warning);
                                context.Global.JobTelemetry.Add(new JobTelemetry() { Type = JobTelemetryType.General, Message = $"OS warning: {osWarning.Warning}" });
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.Error("An error occurred while checking OS warnings for file '{0}' and regex '{1}'.", osWarning.FilePath, osWarning.RegularExpression);
                    Trace.Error(ex);
                    context.Global.JobTelemetry.Add(new JobTelemetry() { Type = JobTelemetryType.General, Message = $"An error occurred while checking OS warnings for file '{osWarning.FilePath}' and regex '{osWarning.RegularExpression}': {ex.Message}" });
                }
            }
        }
    }
#endif
}

