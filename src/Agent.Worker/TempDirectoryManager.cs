using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TempDirectoryManager))]
    public interface ITempDirectoryManager : IAgentService
    {
        void InitializeTempDirectory(IExecutionContext jobContext);
        void CleanupTempDirectory(IExecutionContext jobContext);
    }

    public sealed class TempDirectoryManager : AgentService, ITempDirectoryManager
    {
        private bool _overwriteTemp;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            if (!bool.TryParse(Environment.GetEnvironmentVariable("VSTS_OVERWRITE_TEMP") ?? "true", out _overwriteTemp))
            {
                _overwriteTemp = true;
            }
        }

        public void InitializeTempDirectory(IExecutionContext jobContext)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));

            // TEMP and TMP on Windows
            // TMPDIR on Linux
            if (!_overwriteTemp)
            {
                jobContext.Debug($"Skipping overwrite %TEMP% environment variable");
            }
            else
            {
                string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.TempDirectory);
                jobContext.Debug($"Cleaning temp folder: {tempDirectory}");
                try
                {
                    IOUtil.DeleteDirectory(tempDirectory, contentsOnly: true, continueOnContentDeleteError: true, cancellationToken: jobContext.CancellationToken);
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                }
                finally
                {
                    // make sure folder exists
                    Directory.CreateDirectory(tempDirectory);
                }

#if OS_WINDOWS
                jobContext.Debug($"SET TMP={tempDirectory}");
                jobContext.Debug($"SET TEMP={tempDirectory}");                
                Environment.SetEnvironmentVariable("TMP", tempDirectory);
                Environment.SetEnvironmentVariable("TEMP", tempDirectory);
#else
                jobContext.Debug($"SET TMPDIR={tempDirectory}");
                Environment.SetEnvironmentVariable("TMPDIR", tempDirectory);
#endif
            }
        }

        public void CleanupTempDirectory(IExecutionContext jobContext)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));

            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.TempDirectory);
            if (!_overwriteTemp)
            {
                jobContext.Debug($"Skipping cleanup temp folder: {tempDirectory}");
            }
            else
            {
                jobContext.Debug($"Cleaning temp folder: {tempDirectory}");
                try
                {
                    IOUtil.DeleteDirectory(tempDirectory, contentsOnly: true, continueOnContentDeleteError: true, cancellationToken: jobContext.CancellationToken);
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                }
            }
        }

        private bool OverwriteTemp()
        {
            bool overwriteTemp = true;
            if (!bool.TryParse(Environment.GetEnvironmentVariable("VSTS_OVERWRITE_TEMP") ?? "true", out overwriteTemp))
            {
                overwriteTemp = true;
            }

            return overwriteTemp;
        }
    }
}
