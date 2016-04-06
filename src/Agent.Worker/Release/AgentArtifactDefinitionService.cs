using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(AgentArtifactDefinitionService))]
    public interface IAgentArtifactDefinitionService : IAgentService
    {
        ArtifactDefinition ConvertToArtifactDefinition(
            AgentArtifactDefinition agentArtifactDefinition,
            IExecutionContext executionContext);
    }

    public class AgentArtifactDefinitionService : AgentService, IAgentArtifactDefinitionService
    {
        private readonly IDictionary<ArtifactType, Func<AgentArtifactDefinition, IExecutionContext, IArtifactDetails>>
            ArtifactTypeToDetailsMap =
                new Dictionary<ArtifactType, Func<AgentArtifactDefinition, IExecutionContext, IArtifactDetails>>
                    {
                        {
                            ArtifactType.Build,
                            GetBuildArtifactDetails
                        },
                        {
                            ArtifactType.Jenkins,
                            GetJenkinsArtifactDetails
                        },
                    };

        public ArtifactDefinition ConvertToArtifactDefinition(AgentArtifactDefinition agentArtifactDefinition, IExecutionContext executionContext)
        {
            Trace.Entering();

            ArgUtil.NotNull(agentArtifactDefinition, nameof(agentArtifactDefinition));
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            var artifactDefinition = new ArtifactDefinition
                                         {
                                             ArtifactType = (ArtifactType)agentArtifactDefinition.ArtifactType,
                                             Name = agentArtifactDefinition.Name,
                                             Version = agentArtifactDefinition.Version
                                         };

            artifactDefinition.Details =
                ArtifactTypeToDetailsMap[artifactDefinition.ArtifactType](agentArtifactDefinition, executionContext);

            return artifactDefinition;
        }

        private static BuildArtifactDetails GetBuildArtifactDetails(AgentArtifactDefinition agentArtifactDefinition, IExecutionContext context)
        {
            ServiceEndpoint vssEndpoint = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(vssEndpoint, nameof(vssEndpoint));
            ArgUtil.NotNull(vssEndpoint.Url, nameof(vssEndpoint.Url));

            // TODO Get this value from settings
            string parallelDownloadLimit = "8";

            var artifactDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(agentArtifactDefinition.Details);
            VssCredentials vssCredentials = ApiUtil.GetVssCredential(vssEndpoint);

            Guid projectId = context.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            string relativePath;
            var tfsUrl = context.Variables.Get(WellKnownDistributedTaskVariables.TFCollectionUrl);
            string accessToken;

            vssEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken);
            if (artifactDetails.TryGetValue("RelativePath", out relativePath))
            {
                return new BuildArtifactDetails
                           {
                               Credentials = vssCredentials,
                               RelativePath = artifactDetails["RelativePath"],
                               Project = projectId.ToString(),
                               TfsUrl = new Uri(tfsUrl),
                               AccessToken = accessToken,
                               ParallelDownloadLimit =
                                   Convert.ToInt32(
                                       parallelDownloadLimit,
                                       CultureInfo.InvariantCulture)
                           };
            }
            else
            {
                throw new InvalidOperationException(StringUtil.Loc("RMArtifactDetailsIncomplete"));
            }
        }

        private static IArtifactDetails GetJenkinsArtifactDetails(
            AgentArtifactDefinition agentArtifactDefinition,
            IExecutionContext context)
        {
            var artifactDetails =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(agentArtifactDefinition.Details);

            ServiceEndpoint jenkinsEndpoint = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, artifactDetails["ConnectionName"], StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(jenkinsEndpoint, nameof(jenkinsEndpoint));

            string relativePath;
            var jobName = string.Empty;

            var allFieldsPresents = artifactDetails.TryGetValue("RelativePath", out relativePath)
                                    && artifactDetails.TryGetValue("JobName", out jobName);
            if (allFieldsPresents)
            {
                return new JenkinsArtifactDetails
                {
                    RelativePath = relativePath,
                    AccountName = jenkinsEndpoint.Authorization.Parameters[EndpointAuthorizationParameters.Username],
                    AccountPassword = jenkinsEndpoint.Authorization.Parameters[EndpointAuthorizationParameters.Password],
                    BuildId = Convert.ToInt32(agentArtifactDefinition.Version, CultureInfo.InvariantCulture),
                    JobName = jobName,
                    Url = jenkinsEndpoint.Url
                };
            }
            else
            {
                throw new InvalidOperationException(StringUtil.Loc("RMArtifactDetailsIncomplete"));
            }
        }
    }
}