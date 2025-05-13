#nullable enable

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using GitHub.DistributedTask.WebApi;
using GitHub.Services.Launch.Contracts;

using Sdk.WebApi.WebApi;

namespace GitHub.Services.Launch.Client
{
    public class LaunchHttpClient : RawHttpClientBase
    {
        public LaunchHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            string token,
            bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            m_token = token;
            m_launchServiceUrl = baseUrl;
            m_formatter = new JsonMediaTypeFormatter();
        }

        public async Task<ActionDownloadInfoCollection> GetResolveActionsDownloadInfoAsync(Guid planId, Guid jobId, ActionReferenceList actionReferenceList, CancellationToken cancellationToken)
        {
            var GetResolveActionsDownloadInfoURLEndpoint = new Uri(m_launchServiceUrl, $"/actions/build/{planId.ToString()}/jobs/{jobId.ToString()}/runnerresolve/actions");
            return ToServerData(await GetLaunchSignedURLResponse<ActionReferenceRequestList, ActionDownloadInfoResponseCollection>(GetResolveActionsDownloadInfoURLEndpoint, ToGitHubData(actionReferenceList), cancellationToken));
        }

        // Resolve Actions
        private async Task<T> GetLaunchSignedURLResponse<R, T>(Uri uri, R request, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<R>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        return await ReadJsonContentAsync<T>(response, cancellationToken);
                    }
                }
            }
        }

        private static ActionReferenceRequestList ToGitHubData(ActionReferenceList actionReferenceList)
        {
            return new ActionReferenceRequestList
            {
                Actions = actionReferenceList.Actions?.Select(ToGitHubData).ToList()
            };
        }

        private static ActionReferenceRequest ToGitHubData(ActionReference actionReference)
        {
            return new ActionReferenceRequest
            {
                Action = actionReference.NameWithOwner,
                Version = actionReference.Ref,
                Path = actionReference.Path
            };
        }

        private static ActionDownloadInfoCollection ToServerData(ActionDownloadInfoResponseCollection actionDownloadInfoResponseCollection)
        {
            return new ActionDownloadInfoCollection
            {
                Actions = actionDownloadInfoResponseCollection.Actions?.ToDictionary(kvp => kvp.Key, kvp => ToServerData(kvp.Value))
            };
        }

        private static ActionDownloadInfo ToServerData(ActionDownloadInfoResponse actionDownloadInfoResponse)
        {
            return new ActionDownloadInfo
            {
                Authentication = ToServerData(actionDownloadInfoResponse.Authentication),
                NameWithOwner = actionDownloadInfoResponse.Name,
                ResolvedNameWithOwner = actionDownloadInfoResponse.ResolvedName,
                ResolvedSha = actionDownloadInfoResponse.ResolvedSha,
                TarballUrl = actionDownloadInfoResponse.TarUrl,
                Ref = actionDownloadInfoResponse.Version,
                ZipballUrl = actionDownloadInfoResponse.ZipUrl,
                PackageDetails = ToServerData(actionDownloadInfoResponse.PackageDetails)
            };
        }

        private static ActionDownloadAuthentication? ToServerData(ActionDownloadAuthenticationResponse? actionDownloadAuthenticationResponse)
        {
            if (actionDownloadAuthenticationResponse == null)
            {
                return null;
            }

            return new ActionDownloadAuthentication
            {
                ExpiresAt = actionDownloadAuthenticationResponse.ExpiresAt,
                Token = actionDownloadAuthenticationResponse.Token
            };
        }


        private static ActionDownloadPackageDetails? ToServerData(ActionDownloadPackageDetailsResponse? actionDownloadPackageDetails)
        {
            if (actionDownloadPackageDetails == null)
            {
                return null;
            }

            return new ActionDownloadPackageDetails
            {
                Version = actionDownloadPackageDetails.Version,
                ManifestDigest = actionDownloadPackageDetails.ManifestDigest
            };
        }

        private MediaTypeFormatter m_formatter;
        private Uri m_launchServiceUrl;
        private string m_token;
    }
}
