using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.Launch.Client;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(LaunchServer))]
    public interface ILaunchServer : IRunnerService
    {
        void InitializeLaunchClient(Uri uri, string token);

        Task<ActionDownloadInfoCollection> ResolveActionsDownloadInfoAsync(Guid planId, Guid jobId, ActionReferenceList actionReferenceList, CancellationToken cancellationToken, bool displayHelpfulActionsDownloadErrors);
    }

    public sealed class LaunchServer : RunnerService, ILaunchServer
    {
        private LaunchHttpClient _launchClient;

        public void InitializeLaunchClient(Uri uri, string token)
        {
            // Using default 100 timeout
            RawClientHttpRequestSettings settings = VssUtil.GetHttpRequestSettings(null);

            // Create retry handler
            IEnumerable<DelegatingHandler> delegatingHandlers = new List<DelegatingHandler>();
            if (settings.MaxRetryRequest > 0)
            {
                delegatingHandlers = new DelegatingHandler[] { new VssHttpRetryMessageHandler(settings.MaxRetryRequest) };
            }

            // Setup RawHttpMessageHandler without credentials
            var httpMessageHandler = new RawHttpMessageHandler(new NoOpCredentials(null), settings);
            var pipeline = HttpClientFactory.CreatePipeline(httpMessageHandler, delegatingHandlers);

            this._launchClient = new LaunchHttpClient(uri, pipeline, token, disposeHandler: true);
        }

        public Task<ActionDownloadInfoCollection> ResolveActionsDownloadInfoAsync(Guid planId, Guid jobId, ActionReferenceList actionReferenceList,
            CancellationToken cancellationToken, bool displayHelpfulActionsDownloadErrors)
        {
            if (_launchClient != null)
            {
                if (!displayHelpfulActionsDownloadErrors)
                {
                    return _launchClient.GetResolveActionsDownloadInfoAsync(planId, jobId, actionReferenceList,
                        cancellationToken: cancellationToken);
                }
                return _launchClient.GetResolveActionsDownloadInfoAsyncV2(planId, jobId, actionReferenceList, cancellationToken);
            }

            throw new InvalidOperationException("Launch client is not initialized.");
        }
    }
}
