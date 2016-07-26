using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    public static class ContainerFetchEngineDefaultOptions
    {
        public static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(5);
        public const int DefaultParallelDownloadLimit = 4;
        public const int DefaultRetryLimit = 5;
        public static readonly TimeSpan GetFileAsyncTimeout = TimeSpan.FromMinutes(5);
    }
}