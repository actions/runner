using GitHub.Runner.Common.Util;
using System;
using System.IO;
using System.Threading;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(TempDirectoryManager))]
    public interface ITempDirectoryManager : IRunnerService
    {
        void InitializeTempDirectory(IExecutionContext jobContext);
        void CleanupTempDirectory();
    }

    public sealed class TempDirectoryManager : RunnerService, ITempDirectoryManager
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
            jobContext.SetRunnerContext("temp", _tempDirectory);
            jobContext.Debug($"Cleaning runner temp folder: {_tempDirectory}");
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
        }

        public void CleanupTempDirectory()
        {
            ArgUtil.NotNullOrEmpty(_tempDirectory, nameof(_tempDirectory));
            Trace.Info($"Cleaning runner temp folder: {_tempDirectory}");
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
