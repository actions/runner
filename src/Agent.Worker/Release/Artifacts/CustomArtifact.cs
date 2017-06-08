using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Common.ServiceEndpoints;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    public class CustomArtifact : AgentService, IArtifactExtension
    {
        public Type ExtensionType => typeof(IArtifactExtension);
        public AgentArtifactType ArtifactType => AgentArtifactType.Custom;

        public async Task DownloadAsync(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string downloadFolderPath)
        {
            EnsureVersionBelongsToLinkedDefinition(artifactDefinition);

            var customArtifactDetails = artifactDefinition.Details as CustomArtifactDetails;
            if (customArtifactDetails != null)
            {
                IEnumerable<string> artifactDetails = new EndpointProxy().QueryEndpoint(
                    customArtifactDetails.Endpoint,
                    customArtifactDetails.ArtifactsUrl,
                    customArtifactDetails.ResultSelector,
                    customArtifactDetails.ResultTemplate,
                    customArtifactDetails.AuthorizationHeaders,
                    customArtifactDetails.ArtifactVariables);

                var artifactDownloadDetailList = new List<CustomArtifactDownloadDetails>();
                artifactDetails.ToList().ForEach(x => artifactDownloadDetailList.Add(JToken.Parse(x).ToObject<CustomArtifactDownloadDetails>()));
                if (artifactDownloadDetailList.Count <= 0)
                {
                    executionContext.Warning(StringUtil.Loc("NoArtifactsFound", artifactDefinition.Version));
                    return;
                }

                foreach (CustomArtifactDownloadDetails artifactDownloadDetails in artifactDownloadDetailList)
                {
                    executionContext.Output(StringUtil.Loc("StartingArtifactDownload", artifactDownloadDetails.DownloadUrl));
                    await DownloadArtifact(executionContext, HostContext, downloadFolderPath, customArtifactDetails, artifactDownloadDetails);
                }
            }
        }

        public IArtifactDetails GetArtifactDetails(IExecutionContext context, AgentArtifactDefinition agentArtifactDefinition)
        {
            var artifactDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(agentArtifactDefinition.Details);

            string connectionName;
            string relativePath = string.Empty;
            string customArtifactDetails = string.Empty;

            if (!(artifactDetails.TryGetValue("ConnectionName", out connectionName)
                  && artifactDetails.TryGetValue("RelativePath", out relativePath)
                  && artifactDetails.TryGetValue("ArtifactDetails", out customArtifactDetails)))
            {
                throw new InvalidOperationException(StringUtil.Loc("RMArtifactDetailsIncomplete"));
            }

            var customEndpoint = context.Endpoints.FirstOrDefault((e => string.Equals(e.Name, connectionName, StringComparison.OrdinalIgnoreCase)));

            if (customEndpoint == null)
            {
                throw new InvalidOperationException(StringUtil.Loc("RMCustomEndpointNotFound", agentArtifactDefinition.Name));
            }

            var details = JToken.Parse(customArtifactDetails).ToObject<CustomArtifactDetails>();
            details.RelativePath = relativePath;
            details.Endpoint = new ServiceEndpoint
            {
                Url = customEndpoint.Url,
                Authorization = customEndpoint.Authorization
            };

            return details;
        }


        private async Task DownloadArtifact(
            IExecutionContext executionContext,
            IHostContext hostContext,
            string localFolderPath,
            CustomArtifactDetails customArtifactDetails,
            CustomArtifactDownloadDetails artifact)
        {
            IDictionary<string, string> artifactTypeStreamMapping = customArtifactDetails.ArtifactTypeStreamMapping;
            string streamType = GetArtifactStreamType(artifact, artifactTypeStreamMapping);

            if (string.Equals(streamType, WellKnownStreamTypes.FileShare, StringComparison.OrdinalIgnoreCase))
            {
#if !OS_WINDOWS
                throw new NotSupportedException(StringUtil.Loc("RMFileShareArtifactErrorOnNonWindowsAgent"));
#else
                var fileShareArtifact = new FileShareArtifact();
                customArtifactDetails.RelativePath = artifact.RelativePath ?? string.Empty;
                var location = artifact.FileShareLocation ?? artifact.DownloadUrl;
                await fileShareArtifact.DownloadArtifactAsync(executionContext, hostContext, new ArtifactDefinition { Details = customArtifactDetails }, new Uri(location).LocalPath, localFolderPath);
#endif
            }
            else if (string.Equals(streamType, WellKnownStreamTypes.Zip, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    IEndpointAuthorizer authorizer = SchemeBasedAuthorizerFactory.GetEndpointAuthorizer(
                        customArtifactDetails.Endpoint,
                        customArtifactDetails.AuthorizationHeaders);

                    using (HttpWebResponse webResponse = GetWebResponse(executionContext, artifact.DownloadUrl, authorizer))
                    {
                        var zipStreamDownloader = HostContext.GetService<IZipStreamDownloader>();
                        await zipStreamDownloader.DownloadFromStream(
                            executionContext,
                            webResponse.GetResponseStream(),
                            string.Empty,
                            artifact.RelativePath ?? string.Empty,
                            localFolderPath);
                    }
                }
                catch (WebException)
                {
                    executionContext.Output(StringUtil.Loc("ArtifactDownloadFailed", artifact.DownloadUrl));
                    throw;
                }
            }
            else
            {
                string resourceType = streamType;
                var warningMessage = StringUtil.Loc("RMStreamTypeNotSupported", resourceType);
                executionContext.Warning(warningMessage);
            }
        }

        private static string GetArtifactStreamType(CustomArtifactDownloadDetails artifact, IDictionary<string, string> artifactTypeStreamMapping)
        {
            string streamType = artifact.StreamType;
            if (artifactTypeStreamMapping == null)
            {
                return streamType;
            }

            var artifactTypeStreamMappings = new Dictionary<string, string>(artifactTypeStreamMapping, StringComparer.OrdinalIgnoreCase);
            if (artifactTypeStreamMappings.ContainsKey(artifact.StreamType))
            {
                streamType = artifactTypeStreamMappings[artifact.StreamType];
            }

            return streamType;
        }

        private static HttpWebResponse GetWebResponse(IExecutionContext executionContext, string url, IEndpointAuthorizer authorizer)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null)
            {
                string errorMessage = StringUtil.Loc("RMArtifactDownloadRequestCreationFailed", url);
                executionContext.Output(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            authorizer.AuthorizeRequest(request);
            var webResponse = request.GetResponseAsync().Result as HttpWebResponse;
            return webResponse;
        }

        private void EnsureVersionBelongsToLinkedDefinition(ArtifactDefinition artifactDefinition)
        {
            var customArtifactDetails = artifactDefinition.Details as CustomArtifactDetails;
            if (customArtifactDetails != null && !string.IsNullOrEmpty(customArtifactDetails.VersionsUrl))
            {
                // Query for all artifact versions for given artifact source id, these parameters are contained in customArtifactDetails.ArtifactVariables
                var versionBelongsToDefinition = false;
                IEnumerable<string> versions = new EndpointProxy().QueryEndpoint(
                    customArtifactDetails.Endpoint,
                    customArtifactDetails.VersionsUrl,
                    customArtifactDetails.VersionsResultSelector,
                    customArtifactDetails.VersionsResultTemplate,
                    customArtifactDetails.AuthorizationHeaders,
                    customArtifactDetails.ArtifactVariables);

                foreach (var version in versions)
                {
                    var versionDetails = JToken.Parse(version).ToObject<CustomArtifactVersionDetails>();
                    if (versionDetails != null && versionDetails.Value.Equals(artifactDefinition.Version, StringComparison.OrdinalIgnoreCase))
                    {
                        versionBelongsToDefinition = true;
                        break;
                    }
                }

                if (!versionBelongsToDefinition)
                {
                    throw new ArtifactDownloadException(
                        StringUtil.Loc("RMArtifactVersionNotBelongToArtifactSource", artifactDefinition.Version, customArtifactDetails.ArtifactVariables["definition"]));
                }
            }
        }
    }
}