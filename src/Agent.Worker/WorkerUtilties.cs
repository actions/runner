using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public class WorkerUtilities
    {
        public static VssConnection GetVssConnection(IExecutionContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(context.Endpoints, nameof(context.Endpoints));

            ServiceEndpoint systemConnection = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(systemConnection, nameof(systemConnection));
            ArgUtil.NotNull(systemConnection.Url, nameof(systemConnection.Url));

            VssCredentials credentials = VssUtil.GetVssCredential(systemConnection);
            ArgUtil.NotNull(credentials, nameof(credentials));
            VssConnection connection = VssUtil.CreateConnection(systemConnection.Url, credentials);
            return connection;
        }

        public static Pipelines.AgentJobRequestMessage ScrubPiiData(Pipelines.AgentJobRequestMessage message)
        {
            ArgUtil.NotNull(message, nameof(message));

            var scrubbedVariables = new Dictionary<string, VariableValue>();

            // Scrub the known PII variables
            foreach (var variable in message.Variables)
            {
                if (Variables.PiiVariables.Contains(variable.Key) ||
                    (variable.Key.StartsWith(Variables.PiiArtifactVariablePrefix, StringComparison.OrdinalIgnoreCase)
                    && Variables.PiiArtifactVariableSuffixes.Any(varSuffix => variable.Key.EndsWith(varSuffix, StringComparison.OrdinalIgnoreCase))))
                {
                    scrubbedVariables[variable.Key] = "[PII]";
                }
                else
                {
                    scrubbedVariables[variable.Key] = variable.Value;
                }
            }

            var scrubbedRepositories = new List<Pipelines.RepositoryResource>();

            // Scrub the repository resources
            foreach (var repository in message.Resources.Repositories)
            {
                Pipelines.RepositoryResource scrubbedRepository = repository.Clone();

                var versionInfo = repository.Properties.Get<Pipelines.VersionInfo>(Pipelines.RepositoryPropertyNames.VersionInfo);

                if (versionInfo != null)
                {
                    scrubbedRepository.Properties.Set(
                        Pipelines.RepositoryPropertyNames.VersionInfo,
                        new Pipelines.VersionInfo()
                        {
                            Author = "[PII]",
                            Message = versionInfo.Message
                        });
                }

                scrubbedRepositories.Add(scrubbedRepository);
            }

            var scrubbedJobResources = new Pipelines.JobResources();

            scrubbedJobResources.Containers.AddRange(message.Resources.Containers);
            scrubbedJobResources.Endpoints.AddRange(message.Resources.Endpoints);
            scrubbedJobResources.Repositories.AddRange(scrubbedRepositories);
            scrubbedJobResources.SecureFiles.AddRange(message.Resources.SecureFiles);

            // Reconstitute a new agent job request message from the scrubbed parts
            return new Pipelines.AgentJobRequestMessage(
                plan: message.Plan,
                timeline: message.Timeline,
                jobId: message.JobId,
                jobDisplayName: message.JobDisplayName,
                jobName: message.JobName,
                jobContainer: message.JobContainer,
                jobSidecarContainers: message.JobSidecarContainers,
                variables: scrubbedVariables,
                maskHints: message.MaskHints,
                jobResources: scrubbedJobResources,
                workspaceOptions: message.Workspace,
                steps: message.Steps);
        }
    }
}