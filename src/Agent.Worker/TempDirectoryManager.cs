using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TempDirectoryManager))]
    public interface ITempDirectoryManager : IAgentService
    {
        void InitializeTempDirectory(IExecutionContext jobContext);
        void CleanupTempDirectory();
    }

    public sealed class TempDirectoryManager : AgentService, ITempDirectoryManager
    {
        private bool _overwriteTemp;
        private string _tempDirectory;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            if (!bool.TryParse(Environment.GetEnvironmentVariable("VSTS_OVERWRITE_TEMP") ?? "true", out _overwriteTemp))
            {
                _overwriteTemp = true;
            }

            _tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.TempDirectory);
        }

        public void InitializeTempDirectory(IExecutionContext jobContext)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNullOrEmpty(_tempDirectory, nameof(_tempDirectory));
            jobContext.Variables.Set(Constants.Variables.Agent.TempDirectory, _tempDirectory);
            jobContext.Debug($"Cleaning agent temp folder: {_tempDirectory}");
            try
            {
                IOUtil.DeleteDirectory(_tempDirectory, contentsOnly: true, continueOnContentDeleteError: true, cancellationToken: jobContext.CancellationToken);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
            }
            finally
            {
                // make sure folder exists
                Directory.CreateDirectory(_tempDirectory);
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
                jobContext.Debug($"SET TMP={_tempDirectory}");
                jobContext.Debug($"SET TEMP={_tempDirectory}");                
                Environment.SetEnvironmentVariable("TMP", _tempDirectory);
                Environment.SetEnvironmentVariable("TEMP", _tempDirectory);
#else
                jobContext.Debug($"SET TMPDIR={_tempDirectory}");
                Environment.SetEnvironmentVariable("TMPDIR", _tempDirectory);
#endif
            }
        }

        public void CleanupTempDirectory()
        {
            ArgUtil.NotNullOrEmpty(_tempDirectory, nameof(_tempDirectory));
            Trace.Info($"Cleaning agent temp folder: {_tempDirectory}");
            try
            {
                IOUtil.DeleteDirectory(_tempDirectory, contentsOnly: true, continueOnContentDeleteError: true, cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
            }
        }
    }
}
