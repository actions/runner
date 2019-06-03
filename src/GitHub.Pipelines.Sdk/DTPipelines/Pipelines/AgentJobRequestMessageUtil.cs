using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AgentJobRequestMessageUtil
    {
        // Legacy JobRequestMessage -> Pipeline JobRequestMessage
        // Used by the agent when the latest version agent connect to old version TFS
        // Used by the server when common method only take the new Message contact, like, telemetry logging
        public static AgentJobRequestMessage Convert(WebApi.AgentJobRequestMessage message)
        {
            // construct steps
            List<JobStep> jobSteps = new List<JobStep>();
            foreach (var task in message.Tasks)
            {
                TaskStep taskStep = new TaskStep(task);
                jobSteps.Add(taskStep);
            }

            Dictionary<String, VariableValue> variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            HashSet<MaskHint> maskHints = new HashSet<MaskHint>();
            JobResources jobResources = new JobResources();
            WorkspaceOptions workspace = new WorkspaceOptions();
            message.Environment.Extract(variables, maskHints, jobResources);

            // convert repository endpoint into checkout task for Build
            if (string.Equals(message.Plan.PlanType, "Build", StringComparison.OrdinalIgnoreCase))
            {
                // repositoryId was added sometime after TFS2015, so we need to fall back to find endpoint using endpoint type.
                var legacyRepoEndpoint = jobResources.Endpoints.FirstOrDefault(x => x.Data.ContainsKey("repositoryId"));
                if (legacyRepoEndpoint == null)
                {
                    legacyRepoEndpoint = jobResources.Endpoints.FirstOrDefault(x => x.Type == LegacyRepositoryTypes.Bitbucket || x.Type == LegacyRepositoryTypes.Git || x.Type == LegacyRepositoryTypes.TfsGit || x.Type == LegacyRepositoryTypes.GitHub || x.Type == LegacyRepositoryTypes.GitHubEnterprise || x.Type == LegacyRepositoryTypes.TfsVersionControl);
                }

                // build retention job will not have a repo endpoint.
                if (legacyRepoEndpoint != null)
                {
                    // construct checkout task
                    var checkoutStep = new TaskStep();
                    checkoutStep.Id = Guid.NewGuid();
                    checkoutStep.DisplayName = PipelineConstants.CheckoutTask.FriendlyName;
                    checkoutStep.Name = "__system_checkout";
                    checkoutStep.Reference = new TaskStepDefinitionReference()
                    {
                        Id = PipelineConstants.CheckoutTask.Id,
                        Name = PipelineConstants.CheckoutTask.Name,
                        Version = PipelineConstants.CheckoutTask.Version,
                    };
                    checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.Repository] = "__legacy_repo_endpoint";

                    // construct self repository resource
                    var defaultRepo = new RepositoryResource();
                    defaultRepo.Alias = "__legacy_repo_endpoint";
                    defaultRepo.Properties.Set<String>(RepositoryPropertyNames.Name, legacyRepoEndpoint.Name);
                    legacyRepoEndpoint.Data.TryGetValue("repositoryId", out string repositoryId);
                    if (!string.IsNullOrEmpty(repositoryId))
                    {
                        defaultRepo.Id = repositoryId;
                    }
                    else
                    {
                        defaultRepo.Id = "__legacy_repo_endpoint";
                    }

                    defaultRepo.Endpoint = new ServiceEndpointReference()
                    {
                        Id = Guid.Empty,
                        Name = legacyRepoEndpoint.Name
                    };
                    defaultRepo.Type = ConvertLegacySourceType(legacyRepoEndpoint.Type);
                    defaultRepo.Url = legacyRepoEndpoint.Url;
                    if (variables.TryGetValue("build.sourceVersion", out VariableValue sourceVersion) && !string.IsNullOrEmpty(sourceVersion?.Value))
                    {
                        defaultRepo.Version = sourceVersion.Value;
                    }
                    if (variables.TryGetValue("build.sourceBranch", out VariableValue sourceBranch) && !string.IsNullOrEmpty(sourceBranch?.Value))
                    {
                        defaultRepo.Properties.Set<string>(RepositoryPropertyNames.Ref, sourceBranch.Value);
                    }

                    VersionInfo versionInfo = null;
                    if (variables.TryGetValue("build.sourceVersionAuthor", out VariableValue sourceAuthor) && !string.IsNullOrEmpty(sourceAuthor?.Value))
                    {
                        versionInfo = new VersionInfo();
                        versionInfo.Author = sourceAuthor.Value;
                    }
                    if (variables.TryGetValue("build.sourceVersionMessage", out VariableValue sourceMessage) && !string.IsNullOrEmpty(sourceMessage?.Value))
                    {
                        if (versionInfo == null)
                        {
                            versionInfo = new VersionInfo();
                        }
                        versionInfo.Message = sourceMessage.Value;
                    }
                    if (versionInfo != null)
                    {
                        defaultRepo.Properties.Set<VersionInfo>(RepositoryPropertyNames.VersionInfo, versionInfo);
                    }

                    if (defaultRepo.Type == RepositoryTypes.Tfvc)
                    {
                        if (variables.TryGetValue("build.sourceTfvcShelveset", out VariableValue shelveset) && !string.IsNullOrEmpty(shelveset?.Value))
                        {
                            defaultRepo.Properties.Set<string>(RepositoryPropertyNames.Shelveset, shelveset.Value);
                        }

                        var legacyTfvcMappingJson = legacyRepoEndpoint.Data["tfvcWorkspaceMapping"];
                        var legacyTfvcMapping = JsonUtility.FromString<LegacyBuildWorkspace>(legacyTfvcMappingJson);
                        if (legacyTfvcMapping != null)
                        {
                            IList<WorkspaceMapping> tfvcMapping = new List<WorkspaceMapping>();
                            foreach (var mapping in legacyTfvcMapping.Mappings)
                            {
                                tfvcMapping.Add(new WorkspaceMapping() { ServerPath = mapping.ServerPath, LocalPath = mapping.LocalPath, Exclude = String.Equals(mapping.MappingType, "cloak", StringComparison.OrdinalIgnoreCase) });
                            }

                            defaultRepo.Properties.Set<IList<WorkspaceMapping>>(RepositoryPropertyNames.Mappings, tfvcMapping);
                        }
                    }
                    else if (defaultRepo.Type == RepositoryTypes.Svn)
                    {
                        var legacySvnMappingJson = legacyRepoEndpoint.Data["svnWorkspaceMapping"];
                        var legacySvnMapping = JsonUtility.FromString<LegacySvnWorkspace>(legacySvnMappingJson);
                        if (legacySvnMapping != null)
                        {
                            IList<WorkspaceMapping> svnMapping = new List<WorkspaceMapping>();
                            foreach (var mapping in legacySvnMapping.Mappings)
                            {
                                svnMapping.Add(new WorkspaceMapping() { ServerPath = mapping.ServerPath, LocalPath = mapping.LocalPath, Depth = mapping.Depth, IgnoreExternals = mapping.IgnoreExternals, Revision = mapping.Revision });
                            }

                            defaultRepo.Properties.Set<IList<WorkspaceMapping>>(RepositoryPropertyNames.Mappings, svnMapping);
                        }
                    }

                    legacyRepoEndpoint.Data.TryGetValue("clean", out string cleanString);
                    if (!string.IsNullOrEmpty(cleanString))
                    {
                        checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.Clean] = cleanString;
                    }
                    else
                    {
                        // Checkout task has clean set tp false as default.
                        checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.Clean] = Boolean.FalseString;
                    }

                    if (legacyRepoEndpoint.Data.TryGetValue("checkoutSubmodules", out string checkoutSubmodulesString) &&
                        Boolean.TryParse(checkoutSubmodulesString, out Boolean checkoutSubmodules) &&
                        checkoutSubmodules)
                    {
                        if (legacyRepoEndpoint.Data.TryGetValue("checkoutNestedSubmodules", out string nestedSubmodulesString) &&
                            Boolean.TryParse(nestedSubmodulesString, out Boolean nestedSubmodules) &&
                            nestedSubmodules)
                        {
                            checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.Submodules] = PipelineConstants.CheckoutTaskInputs.SubmodulesOptions.Recursive;
                        }
                        else
                        {
                            checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.Submodules] = PipelineConstants.CheckoutTaskInputs.SubmodulesOptions.True;
                        }
                    }

                    if (legacyRepoEndpoint.Data.ContainsKey("fetchDepth"))
                    {
                        checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.FetchDepth] = legacyRepoEndpoint.Data["fetchDepth"];
                    }

                    if (legacyRepoEndpoint.Data.ContainsKey("gitLfsSupport"))
                    {
                        checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.Lfs] = legacyRepoEndpoint.Data["gitLfsSupport"];
                    }

                    if (VariableUtility.GetEnableAccessTokenType(variables) == EnableAccessTokenType.Variable)
                    {
                        checkoutStep.Inputs[PipelineConstants.CheckoutTaskInputs.PersistCredentials] = Boolean.TrueString;
                    }

                    // construct worksapce option
                    if (Boolean.TryParse(cleanString, out Boolean clean) && clean)
                    {
                        if (legacyRepoEndpoint.Data.TryGetValue("cleanOptions", out string cleanOptionsString) && !string.IsNullOrEmpty(cleanOptionsString))
                        {
                            if (string.Equals(cleanOptionsString, "1", StringComparison.OrdinalIgnoreCase)) //RepositoryCleanOptions.SourceAndOutputDir
                            {
                                workspace.Clean = PipelineConstants.WorkspaceCleanOptions.Outputs;
                            }
                            else if (string.Equals(cleanOptionsString, "2", StringComparison.OrdinalIgnoreCase)) //RepositoryCleanOptions.SourceDir
                            {
                                workspace.Clean = PipelineConstants.WorkspaceCleanOptions.Resources;
                            }
                            else if (string.Equals(cleanOptionsString, "3", StringComparison.OrdinalIgnoreCase)) //RepositoryCleanOptions.AllBuildDir
                            {
                                workspace.Clean = PipelineConstants.WorkspaceCleanOptions.All;
                            }
                        }
                    }

                    // add checkout task when build.syncsources and skipSyncSource not set
                    variables.TryGetValue("build.syncSources", out VariableValue syncSourcesVariable);
                    legacyRepoEndpoint.Data.TryGetValue("skipSyncSource", out string skipSyncSource);
                    if (!string.IsNullOrEmpty(syncSourcesVariable?.Value) && Boolean.TryParse(syncSourcesVariable?.Value, out bool syncSource) && !syncSource)
                    {
                        checkoutStep.Condition = bool.FalseString;
                    }
                    else if (Boolean.TryParse(skipSyncSource, out bool skipSource) && skipSource)
                    {
                        checkoutStep.Condition = bool.FalseString;
                    }

                    jobSteps.Insert(0, checkoutStep);

                    // always add self repository to job resource
                    jobResources.Repositories.Add(defaultRepo);
                }
            }

            AgentJobRequestMessage agentRequestMessage = new AgentJobRequestMessage(message.Plan, message.Timeline, message.JobId, message.JobName, message.JobRefName, null, null, variables, maskHints.ToList(), jobResources, null, workspace, jobSteps)
            {
                RequestId = message.RequestId
            };

            return agentRequestMessage;
        }

        // Pipeline JobRequestMessage -> Legacy JobRequestMessage
        // Used by the server when the connected agent is old version and doesn't support new contract yet.
        public static WebApi.AgentJobRequestMessage Convert(AgentJobRequestMessage message)
        {
            // Old agent can't handle container(s)
            if (!String.IsNullOrEmpty(message.JobContainer))
            {
                throw new NotSupportedException(message.JobContainer);
            }
            if (message.JobSidecarContainers?.Count > 0)
            {
                throw new NotSupportedException(String.Join(", ", message.JobSidecarContainers.Keys));
            }

            // Old agent can't handle more than 1 repository
            if (message.Resources.Repositories.Count > 1)
            {
                throw new NotSupportedException(string.Join(", ", message.Resources.Repositories.Select(x => x.Alias)));
            }

            // Old agent can't handle more than 1 checkout task
            if (message.Steps.Where(x => x.IsCheckoutTask()).Count() > 1)
            {
                throw new NotSupportedException(PipelineConstants.CheckoutTask.Id.ToString("D"));
            }

            // construct tasks
            List<TaskInstance> tasks = new List<TaskInstance>();
            foreach (var step in message.Steps)
            {
                // Pipeline builder should add min agent demand when steps contains group
                if (step.Type != StepType.Task)
                {
                    throw new NotSupportedException(step.Type.ToString());
                }

                // don't add checkout task, we need to convert the checkout task into endpoint
                if (!step.IsCheckoutTask())
                {
                    TaskInstance task = (step as TaskStep).ToLegacyTaskInstance();
                    tasks.Add(task);
                }
            }

            if (message.Resources != null)
            {
                foreach (var endpoint in message.Resources.Endpoints)
                {
                    // Legacy message require all endpoint's name equals to endpoint's id
                    // Guid.Empty is for repository endpoints
                    if (!String.Equals(endpoint.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase) &&
                        endpoint.Id != Guid.Empty)
                    {
                        endpoint.Name = endpoint.Id.ToString("D");
                    }
                }

                // Make sure we propagate download ticket into the mask hints
                foreach (var secureFile in message.Resources.SecureFiles)
                {
                    if (!String.IsNullOrEmpty(secureFile.Ticket))
                    {
                        message.MaskHints.Add(new MaskHint() { Type = MaskType.Regex, Value = Regex.Escape(secureFile.Ticket) });
                    }
                }
            }

            if (String.Equals(message.Plan.PlanType, "Build", StringComparison.OrdinalIgnoreCase))
            {
                // create repository endpoint base on checkout task + repository resource + repository endpoint
                // repoResource might be null when environment verion is still on 1
                var repoResource = message.Resources?.Repositories.SingleOrDefault();
                if (repoResource != null)
                {
                    var legacyRepoEndpoint = new ServiceEndpoint();
                    legacyRepoEndpoint.Name = repoResource.Properties.Get<string>(RepositoryPropertyNames.Name);
                    legacyRepoEndpoint.Type = ConvertToLegacySourceType(repoResource.Type);
                    legacyRepoEndpoint.Url = repoResource.Url;
                    if (repoResource.Endpoint != null)
                    {
                        var referencedEndpoint = message.Resources.Endpoints.First(x => (x.Id == repoResource.Endpoint.Id && x.Id != Guid.Empty) || (String.Equals(x.Name, repoResource.Endpoint.Name?.Literal, StringComparison.OrdinalIgnoreCase) && x.Id == Guid.Empty && repoResource.Endpoint.Id == Guid.Empty));
                        var endpointAuthCopy = referencedEndpoint.Authorization?.Clone();
                        if (endpointAuthCopy != null)
                        {
                            if (endpointAuthCopy.Scheme == EndpointAuthorizationSchemes.Token) //InstallationToken (Tabby) or ApiToken (GithubEnterprise)
                            {
                                if (referencedEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out string accessToken))  //Tabby
                                {
                                    legacyRepoEndpoint.Authorization = new EndpointAuthorization()
                                    {
                                        Scheme = EndpointAuthorizationSchemes.UsernamePassword,
                                        Parameters =
                                        {
                                            { EndpointAuthorizationParameters.Username, "x-access-token" },
                                            { EndpointAuthorizationParameters.Password, accessToken }
                                        }
                                    };
                                }
                                else if (referencedEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.ApiToken, out string apiToken))  //GithubEnterprise
                                {
                                    legacyRepoEndpoint.Authorization = new EndpointAuthorization()
                                    {
                                        Scheme = EndpointAuthorizationSchemes.UsernamePassword,
                                        Parameters =
                                        {
                                            { EndpointAuthorizationParameters.Username, apiToken },
                                            { EndpointAuthorizationParameters.Password, "x-oauth-basic" }
                                        }
                                    };
                                }
                            }
                            else if (endpointAuthCopy.Scheme == EndpointAuthorizationSchemes.PersonalAccessToken) // Github
                            {
                                if (referencedEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out string accessToken))  //Tabby
                                {
                                    legacyRepoEndpoint.Authorization = new EndpointAuthorization()
                                    {
                                        Scheme = EndpointAuthorizationSchemes.UsernamePassword,
                                        Parameters =
                                        {
                                            { EndpointAuthorizationParameters.Username, "pat" },
                                            { EndpointAuthorizationParameters.Password, accessToken }
                                        }
                                    };
                                }
                            }
                            else
                            {
                                legacyRepoEndpoint.Authorization = endpointAuthCopy;
                            }
                        }

                        // there are 2 properties we put into the legacy repo endpoint directly from connect endpoint
                        if (referencedEndpoint.Data.TryGetValue("acceptUntrustedCerts", out String acceptUntrustedCerts))
                        {
                            legacyRepoEndpoint.Data["acceptUntrustedCerts"] = acceptUntrustedCerts;
                        }
                        if (referencedEndpoint.Data.TryGetValue("realmName", out String realmName))
                        {
                            legacyRepoEndpoint.Data["realmName"] = realmName;
                        }
                    }
                    legacyRepoEndpoint.Data["repositoryId"] = repoResource.Id;

                    // default values in the old message format
                    legacyRepoEndpoint.Data["clean"] = Boolean.FalseString;
                    legacyRepoEndpoint.Data["checkoutSubmodules"] = Boolean.FalseString;
                    legacyRepoEndpoint.Data["checkoutNestedSubmodules"] = Boolean.FalseString;
                    legacyRepoEndpoint.Data["fetchDepth"] = "0";
                    legacyRepoEndpoint.Data["gitLfsSupport"] = Boolean.FalseString;
                    legacyRepoEndpoint.Data["skipSyncSource"] = Boolean.FalseString;
                    legacyRepoEndpoint.Data["cleanOptions"] = "0";
                    legacyRepoEndpoint.Data["rootFolder"] = null; // old tfvc repo endpoint has this set to $/foo, but it doesn't seems to be used at all.

                    if (repoResource.Type == RepositoryTypes.Tfvc)
                    {
                        var tfvcMapping = repoResource.Properties.Get<IList<WorkspaceMapping>>(RepositoryPropertyNames.Mappings);
                        if (tfvcMapping != null)
                        {
                            LegacyBuildWorkspace legacyMapping = new LegacyBuildWorkspace();
                            foreach (var mapping in tfvcMapping)
                            {
                                legacyMapping.Mappings.Add(new LegacyMappingDetails() { ServerPath = mapping.ServerPath, LocalPath = mapping.LocalPath, MappingType = mapping.Exclude ? "cloak" : "map" });
                            }

                            legacyRepoEndpoint.Data["tfvcWorkspaceMapping"] = JsonUtility.ToString(legacyMapping);
                        }
                    }
                    else if (repoResource.Type == RepositoryTypes.Svn)
                    {
                        var svnMapping = repoResource.Properties.Get<IList<WorkspaceMapping>>(RepositoryPropertyNames.Mappings);
                        if (svnMapping != null)
                        {
                            LegacySvnWorkspace legacyMapping = new LegacySvnWorkspace();
                            foreach (var mapping in svnMapping)
                            {
                                legacyMapping.Mappings.Add(new LegacySvnMappingDetails() { ServerPath = mapping.ServerPath, LocalPath = mapping.LocalPath, Depth = mapping.Depth, IgnoreExternals = mapping.IgnoreExternals, Revision = mapping.Revision });
                            }

                            legacyRepoEndpoint.Data["svnWorkspaceMapping"] = JsonUtility.ToString(legacyMapping);
                        }
                    }
                    else if (repoResource.Type == RepositoryTypes.Git)
                    {
                        if (message.Variables.TryGetValue(WellKnownDistributedTaskVariables.ServerType, out VariableValue serverType) && String.Equals(serverType?.Value, "Hosted", StringComparison.OrdinalIgnoreCase))
                        {
                            legacyRepoEndpoint.Data["onpremtfsgit"] = Boolean.FalseString;
                        }
                        else
                        {
                            legacyRepoEndpoint.Data["onpremtfsgit"] = Boolean.TrueString;
                        }
                    }

                    if (!message.Variables.ContainsKey("build.repository.id") || String.IsNullOrEmpty(message.Variables["build.repository.id"]?.Value))
                    {
                        message.Variables["build.repository.id"] = repoResource.Id;
                    }
                    if (!message.Variables.ContainsKey("build.repository.name") || String.IsNullOrEmpty(message.Variables["build.repository.name"]?.Value))
                    {
                        message.Variables["build.repository.name"] = repoResource.Properties.Get<String>(RepositoryPropertyNames.Name);
                    }
                    if (!message.Variables.ContainsKey("build.repository.uri") || String.IsNullOrEmpty(message.Variables["build.repository.uri"]?.Value))
                    {
                        message.Variables["build.repository.uri"] = repoResource.Url.AbsoluteUri;
                    }

                    var versionInfo = repoResource.Properties.Get<VersionInfo>(RepositoryPropertyNames.VersionInfo);
                    if (!message.Variables.ContainsKey("build.sourceVersionAuthor") || String.IsNullOrEmpty(message.Variables["build.sourceVersionAuthor"]?.Value))
                    {
                        message.Variables["build.sourceVersionAuthor"] = versionInfo?.Author;
                    }
                    if (!message.Variables.ContainsKey("build.sourceVersionMessage") || String.IsNullOrEmpty(message.Variables["build.sourceVersionMessage"]?.Value))
                    {
                        message.Variables["build.sourceVersionMessage"] = versionInfo?.Message;
                    }
                    if (!message.Variables.ContainsKey("build.sourceVersion") || String.IsNullOrEmpty(message.Variables["build.sourceVersion"]?.Value))
                    {
                        message.Variables["build.sourceVersion"] = repoResource.Version;
                    }
                    if (!message.Variables.ContainsKey("build.sourceBranch") || String.IsNullOrEmpty(message.Variables["build.sourceBranch"]?.Value))
                    {
                        message.Variables["build.sourceBranch"] = repoResource.Properties.Get<String>(RepositoryPropertyNames.Ref);
                    }
                    if (repoResource.Type == RepositoryTypes.Tfvc)
                    {
                        var shelveset = repoResource.Properties.Get<String>(RepositoryPropertyNames.Shelveset);
                        if (!String.IsNullOrEmpty(shelveset) && (!message.Variables.ContainsKey("build.sourceTfvcShelveset") || String.IsNullOrEmpty(message.Variables["build.sourceTfvcShelveset"]?.Value)))
                        {
                            message.Variables["build.sourceTfvcShelveset"] = shelveset;
                        }
                    }

                    TaskStep checkoutTask = message.Steps.FirstOrDefault(x => x.IsCheckoutTask()) as TaskStep;
                    if (checkoutTask != null)
                    {
                        if (checkoutTask.Inputs.TryGetValue(PipelineConstants.CheckoutTaskInputs.Clean, out string taskInputClean) && !string.IsNullOrEmpty(taskInputClean))
                        {
                            legacyRepoEndpoint.Data["clean"] = taskInputClean;
                        }
                        else
                        {
                            legacyRepoEndpoint.Data["clean"] = Boolean.FalseString;
                        }

                        if (checkoutTask.Inputs.TryGetValue(PipelineConstants.CheckoutTaskInputs.Submodules, out string taskInputSubmodules) && !string.IsNullOrEmpty(taskInputSubmodules))
                        {
                            legacyRepoEndpoint.Data["checkoutSubmodules"] = Boolean.TrueString;
                            if (String.Equals(taskInputSubmodules, PipelineConstants.CheckoutTaskInputs.SubmodulesOptions.Recursive, StringComparison.OrdinalIgnoreCase))
                            {
                                legacyRepoEndpoint.Data["checkoutNestedSubmodules"] = Boolean.TrueString;
                            }
                        }

                        if (checkoutTask.Inputs.TryGetValue(PipelineConstants.CheckoutTaskInputs.FetchDepth, out string taskInputFetchDepth) && !string.IsNullOrEmpty(taskInputFetchDepth))
                        {
                            legacyRepoEndpoint.Data["fetchDepth"] = taskInputFetchDepth;
                        }

                        if (checkoutTask.Inputs.TryGetValue(PipelineConstants.CheckoutTaskInputs.Lfs, out string taskInputfs) && !string.IsNullOrEmpty(taskInputfs))
                        {
                            legacyRepoEndpoint.Data["gitLfsSupport"] = taskInputfs;
                        }

                        // Skip sync sources
                        if (String.Equals(checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.Repository], PipelineConstants.NoneAlias, StringComparison.OrdinalIgnoreCase))
                        {
                            legacyRepoEndpoint.Data["skipSyncSource"] = Boolean.TrueString;
                        }
                        else if (String.Equals(checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.Repository], PipelineConstants.DesignerRepo, StringComparison.OrdinalIgnoreCase) && checkoutTask.Condition == Boolean.FalseString)
                        {
                            legacyRepoEndpoint.Data["skipSyncSource"] = Boolean.TrueString;
                        }
                    }

                    // workspace clean options
                    legacyRepoEndpoint.Data["cleanOptions"] = "0"; // RepositoryCleanOptions.Source;
                    if (message.Workspace != null)
                    {
                        if (String.Equals(message.Workspace.Clean, PipelineConstants.WorkspaceCleanOptions.Outputs, StringComparison.OrdinalIgnoreCase))
                        {
                            legacyRepoEndpoint.Data["cleanOptions"] = "1"; // RepositoryCleanOptions.SourceAndOutputDir;
                        }
                        else if (String.Equals(message.Workspace.Clean, PipelineConstants.WorkspaceCleanOptions.Resources, StringComparison.OrdinalIgnoreCase))
                        {
                            legacyRepoEndpoint.Data["cleanOptions"] = "2"; //RepositoryCleanOptions.SourceDir;
                        }
                        else if (String.Equals(message.Workspace.Clean, PipelineConstants.WorkspaceCleanOptions.All, StringComparison.OrdinalIgnoreCase))
                        {
                            legacyRepoEndpoint.Data["cleanOptions"] = "3"; // RepositoryCleanOptions.AllBuildDir;
                        }
                    }

                    // add reposiotry endpoint to environment
                    message.Resources.Endpoints.Add(legacyRepoEndpoint);
                }
            }

            JobEnvironment environment = new JobEnvironment(message.Variables, message.MaskHints, message.Resources);

            WebApi.AgentJobRequestMessage legacyAgentRequestMessage = new WebApi.AgentJobRequestMessage(message.Plan, message.Timeline, message.JobId, message.JobDisplayName, message.JobName, environment, tasks)
            {
                RequestId = message.RequestId
            };

            return legacyAgentRequestMessage;
        }

        private static string ConvertLegacySourceType(string legacySourceType)
        {
            if (String.Equals(legacySourceType, LegacyRepositoryTypes.Bitbucket, StringComparison.OrdinalIgnoreCase))
            {
                return RepositoryTypes.Bitbucket;
            }
            else if (String.Equals(legacySourceType, LegacyRepositoryTypes.Git, StringComparison.OrdinalIgnoreCase))
            {
                return RepositoryTypes.ExternalGit;
            }
            else if (String.Equals(legacySourceType, LegacyRepositoryTypes.TfsGit, StringComparison.OrdinalIgnoreCase))
            {
                return RepositoryTypes.Git;
            }
            else if (String.Equals(legacySourceType, LegacyRepositoryTypes.GitHub, StringComparison.OrdinalIgnoreCase))
            {
                return RepositoryTypes.GitHub;
            }
            else if (String.Equals(legacySourceType, LegacyRepositoryTypes.GitHubEnterprise, StringComparison.OrdinalIgnoreCase))
            {
                return RepositoryTypes.GitHubEnterprise;
            }
            else if (String.Equals(legacySourceType, LegacyRepositoryTypes.Svn, StringComparison.OrdinalIgnoreCase))
            {
                return RepositoryTypes.Svn;
            }
            else if (String.Equals(legacySourceType, LegacyRepositoryTypes.TfsVersionControl, StringComparison.OrdinalIgnoreCase))
            {
                return RepositoryTypes.Tfvc;
            }
            else
            {
                throw new NotSupportedException(legacySourceType);
            }
        }

        private static string ConvertToLegacySourceType(string pipelineSourceType)
        {
            if (String.Equals(pipelineSourceType, RepositoryTypes.Bitbucket, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRepositoryTypes.Bitbucket;
            }
            else if (String.Equals(pipelineSourceType, RepositoryTypes.ExternalGit, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRepositoryTypes.Git;
            }
            else if (String.Equals(pipelineSourceType, RepositoryTypes.Git, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRepositoryTypes.TfsGit;
            }
            else if (String.Equals(pipelineSourceType, RepositoryTypes.GitHub, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRepositoryTypes.GitHub;
            }
            else if (String.Equals(pipelineSourceType, RepositoryTypes.GitHubEnterprise, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRepositoryTypes.GitHubEnterprise;
            }
            else if (String.Equals(pipelineSourceType, RepositoryTypes.Svn, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRepositoryTypes.Svn;
            }
            else if (String.Equals(pipelineSourceType, RepositoryTypes.Tfvc, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRepositoryTypes.TfsVersionControl;
            }
            else
            {
                throw new NotSupportedException(pipelineSourceType);
            }
        }

        private static class LegacyRepositoryTypes // Copy from Build.Webapi
        {
            public const String TfsVersionControl = "TfsVersionControl";
            public const String TfsGit = "TfsGit";
            public const String Git = "Git";
            public const String GitHub = "GitHub";
            public const String GitHubEnterprise = "GitHubEnterprise";
            public const String Bitbucket = "Bitbucket";
            public const String Svn = "Svn";
        }

        /// <summary>
        /// Represents an entry in a workspace mapping.
        /// </summary>
        [DataContract]
        private class LegacyMappingDetails
        {
            /// <summary>
            /// The server path.
            /// </summary>
            [DataMember(Name = "serverPath")]
            public String ServerPath
            {
                get;
                set;
            }

            /// <summary>
            /// The mapping type.
            /// </summary>
            [DataMember(Name = "mappingType")]
            public String MappingType
            {
                get;
                set;
            }

            /// <summary>
            /// The local path.
            /// </summary>
            [DataMember(Name = "localPath")]
            public String LocalPath
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Represents a workspace mapping.
        /// </summary>
        [DataContract]
        private class LegacyBuildWorkspace
        {
            /// <summary>
            /// The list of workspace mapping entries.
            /// </summary>
            public List<LegacyMappingDetails> Mappings
            {
                get
                {
                    if (m_mappings == null)
                    {
                        m_mappings = new List<LegacyMappingDetails>();
                    }
                    return m_mappings;
                }
            }

            [DataMember(Name = "mappings")]
            private List<LegacyMappingDetails> m_mappings;
        }

        /// <summary>
        /// Represents a Subversion mapping entry.
        /// </summary>
        [DataContract]
        private class LegacySvnMappingDetails
        {
            /// <summary>
            /// The server path.
            /// </summary>
            [DataMember(Name = "serverPath")]
            public String ServerPath
            {
                get;
                set;
            }

            /// <summary>
            /// The local path.
            /// </summary>
            [DataMember(Name = "localPath")]
            public String LocalPath
            {
                get;
                set;
            }

            /// <summary>
            /// The revision.
            /// </summary>
            [DataMember(Name = "revision")]
            public String Revision
            {
                get;
                set;
            }

            /// <summary>
            /// The depth.
            /// </summary>
            [DataMember(Name = "depth")]
            public Int32 Depth
            {
                get;
                set;
            }

            /// <summary>
            /// Indicates whether to ignore externals.
            /// </summary>
            [DataMember(Name = "ignoreExternals")]
            public bool IgnoreExternals
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Represents a subversion workspace.
        /// </summary>
        [DataContract]
        private class LegacySvnWorkspace
        {
            /// <summary>
            /// The list of mappings.
            /// </summary>
            public List<LegacySvnMappingDetails> Mappings
            {
                get
                {
                    if (m_Mappings == null)
                    {
                        m_Mappings = new List<LegacySvnMappingDetails>();
                    }
                    return m_Mappings;
                }
            }

            [DataMember(Name = "mappings")]
            private List<LegacySvnMappingDetails> m_Mappings;
        }
    }
}
