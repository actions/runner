using System;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    public class ContainerFetchEngineOptions
    {
        public int RetryLimit { get; set; }
        public TimeSpan RetryInterval { get; set; }
        public TimeSpan GetFileAsyncTimeout { get; set; }
        public int ParallelDownloadLimit { get; set; }
        public int DownloadBufferSize { get; set; }

        public ContainerFetchEngineOptions()
        {
            RetryLimit = ContainerFetchEngineDefaultOptions.RetryLimit;
            ParallelDownloadLimit = ContainerFetchEngineDefaultOptions.ParallelDownloadLimit;
            RetryInterval = ContainerFetchEngineDefaultOptions.RetryInterval;
            GetFileAsyncTimeout = ContainerFetchEngineDefaultOptions.GetFileAsyncTimeout;
            DownloadBufferSize = ContainerFetchEngineDefaultOptions.DownloadBufferSize;
        }
    }
}