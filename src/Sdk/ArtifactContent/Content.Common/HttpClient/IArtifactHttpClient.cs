using GitHub.Services.Content.Common.Tracing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Base interface all ArtifactServices HttpClient classes should implement to enable use 
    /// of shared client telemetry functionality.
    /// </summary>
    /// <remarks>
    /// This will be extended during refactoring of client telemetry, currently supports the 
    /// TelemetryEnvironmentHelper in AppShared module
    /// </remarks>
    public interface IArtifactHttpClient
    {
        /// <remarks>Note BaseAddress is a member of VssHttpClientBase</remarks>
        Uri BaseAddress { get; }

        Task GetOptionsAsync(CancellationToken cancellationToken);

        /// <remarks>May only be called once</remarks>
        void SetTracer(IAppTraceSource tracer);
    }
}
