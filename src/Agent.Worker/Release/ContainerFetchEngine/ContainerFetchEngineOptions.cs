using System;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    public class ContainerFetchEngineOptions
    {
        public CancellationToken CancellationToken { get; set; }
        public int RetryLimit { get; set; }
        public TimeSpan RetryInterval { get; set; }
        public TimeSpan GetFileAsyncTimeout { get; set; }
        public int ParallelDownloadLimit { get; set; }

        public ContainerFetchEngineOptions()
        {
            RetryLimit = ContainerFetchEngineDefaultOptions.DefaultRetryLimit;
            ParallelDownloadLimit = ContainerFetchEngineDefaultOptions.DefaultParallelDownloadLimit;
            RetryInterval = ContainerFetchEngineDefaultOptions.DefaultRetryInterval;
            GetFileAsyncTimeout = ContainerFetchEngineDefaultOptions.GetFileAsyncTimeout;
            CancellationToken = new CancellationToken();
        }
    }
}