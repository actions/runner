using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    public static class ContainerFetchEngineDefaultOptions
    {
        public static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);
        public const int ParallelDownloadLimit = 4;
        public const int RetryLimit = 5;
        public const int DownloadBufferSize = 8192;
    }
}