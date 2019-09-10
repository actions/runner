﻿using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    public class WorkerUtilities
    {
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

            var contextData = new DictionaryContextData();
            if (message.ContextData?.Count > 0)
            {
                foreach (var pair in message.ContextData)
                {
                    contextData[pair.Key] = pair.Value;
                }
            }

            // Reconstitute a new agent job request message from the scrubbed parts
            return new Pipelines.AgentJobRequestMessage(
                plan: message.Plan,
                timeline: message.Timeline,
                jobId: message.JobId,
                jobDisplayName: message.JobDisplayName,
                jobName: message.JobName,
                jobContainer: message.JobContainer,
                jobServiceContainers: message.JobServiceContainers,
                environmentVariables: message.EnvironmentVariables,
                variables: scrubbedVariables,
                maskHints: message.MaskHints,
                jobResources: scrubbedJobResources,
                contextData: contextData,
                workspaceOptions: message.Workspace,
                steps: message.Steps,
                scopes: message.Scopes);
        }
    }
}
