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
        private string _tempDirectory;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);
        }

        public void InitializeTempDirectory(IExecutionContext jobContext)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNullOrEmpty(_tempDirectory, nameof(_tempDirectory));
            jobContext.SetVariable(Constants.Variables.Agent.TempDirectory, _tempDirectory, isFilePath: true);
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

            bool overwriteTemp = jobContext.Variables.GetBoolean("VSTS_OVERWRITE_TEMP") ?? StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("VSTS_OVERWRITE_TEMP"));

            // TEMP and TMP on Windows
            // TMPDIR on Linux
            if (!overwriteTemp)
            {
                jobContext.Debug($"Skipping overwrite %TEMP% environment variable");
            }
            else
            {
#if OS_WINDOWS
                jobContext.Debug($"SET TMP={_tempDirectory}");
                jobContext.Debug($"SET TEMP={_tempDirectory}");
                jobContext.SetVariable("TMP", _tempDirectory, isFilePath: true);
                jobContext.SetVariable("TEMP", _tempDirectory, isFilePath: true);
#else
                jobContext.Debug($"SET TMPDIR={_tempDirectory}");
                jobContext.SetVariable("TMPDIR", _tempDirectory, isFilePath:true);
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
