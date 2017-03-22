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

            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.TempDirectory);
            jobContext.Variables.Set(Constants.Variables.Agent.TempDirectory, tempDirectory);
            jobContext.Debug($"Cleaning agent temp folder: {tempDirectory}");
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

            // TEMP and TMP on Windows
            // TMPDIR on Linux
            if (!_overwriteTemp)
            {
                jobContext.Debug($"Skipping overwrite %TEMP% environment variable");
            }
            else
            {
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
            ArgUtil.NotNullOrEmpty(jobContext.Variables.Agent_TempDirectory, nameof(jobContext.Variables.Agent_TempDirectory));

            string tempDirectory = jobContext.Variables.Agent_TempDirectory;
            jobContext.Debug($"Cleaning agent temp folder: {tempDirectory}");
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
}
