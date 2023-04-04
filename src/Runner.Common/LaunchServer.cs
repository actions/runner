using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Launch.Client;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(LaunchServer))]
    public interface ILaunchServer : IRunnerService
    {
        void InitializeLaunchClient(Uri uri, string token);

        Task ResolveActionsDownloadInfoAsync(string planId, string jobId, WebApi.ActionReferenceList actionReferenceList, CancellationToken cancellationToken);
    }

    public sealed class LaunchServer : RunnerService, ILaunchServer
    {
        private LaunchHttpClient _launchClient;

        public void InitializeLaunchClient(Uri uri, string token)
        {
            var httpMessageHandler = HostContext.CreateHttpClientHandler();
            this._launchClient = new LaunchHttpClient(uri, httpMessageHandler, token, disposeHandler: true);
        }
        
        public Task ResolveActionsDownloadInfoAsync(string planId, string jobId, WebApi.ActionReferenceList actionReferenceList,
            CancellationToken cancellationToken)
        {
            if (_launchClient != null)
            {
                return _launchClient.GetResolveActionsDownloadInfoAsync(planId, jobId, actionReferenceList,
                    cancellationToken: cancellationToken);
            }

            throw new InvalidOperationException("Launch client is not initialized.");
        }
    }
}