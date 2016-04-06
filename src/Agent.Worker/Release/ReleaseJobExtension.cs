using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Agent.Worker.Release;
using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseJobExtension : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);

        public string HostType => "release";

        public IStep PrepareStep { get; private set; }

        public IStep FinallyStep { get; private set; }

        private IAgentServer _agentServer;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _agentServer = hostContext.GetService<IAgentServer>();
        }

        public void GetRootedPath(IExecutionContext context, string path, out string rootedPath)
        {
            throw new NotImplementedException();
        }

        public void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath)
        {
            throw new NotImplementedException();
        }

        public ReleaseJobExtension()
        {
            PrepareStep = new JobExtensionRunner(
                runAsync: PrepareAsync,
                alwaysRun: false,
                continueOnError: false,
                critical: true,
                displayName: StringUtil.Loc("DownloadArtifacts"),
                enabled: true,
                @finally: false);

            //this.FinallyStep = new JobExtensionRunner(
            //    runAsync: FinallyAsync,
            //    alwaysRun: false,
            //    continueOnError: false,
            //    critical: false,
            //    displayName: StringUtil.Loc("Cleanup"),
            //    enabled: true,
            //    @finally: true);
        }

        public async Task PrepareAsync()
        {
            Trace.Entering();

            ArgUtil.NotNull(PrepareStep, nameof(PrepareStep));
            ArgUtil.NotNull(PrepareStep.ExecutionContext, nameof(PrepareStep.ExecutionContext));

            int releaseId;
            Guid teamProjectId;
            string artifactsWorkingFolder;
            bool skipArtifactsDownload;

            IExecutionContext executionContext = PrepareStep.ExecutionContext;
            InitializeAgent(executionContext, out skipArtifactsDownload, out teamProjectId, out artifactsWorkingFolder, out releaseId);

            if (!skipArtifactsDownload)
            {
                // TODO: Create this as new task. Old agent does this. We have two task. First is initialize which does the above and download task will be added based on skipDownloadArtifact option
                executionContext.Debug("Downloading artifact");

                await DownloadArtifacts(executionContext, teamProjectId, artifactsWorkingFolder, releaseId);
            }

            await Task.Delay(100);
        }

        private async Task DownloadArtifacts(IExecutionContext executionContext, Guid teamProjectId, string artifactsWorkingFolder, int releaseId)
        {
            Trace.Entering();
            // Get artifacts first
            // TODO: send correct cancellation token
            try
            {
                List<AgentArtifactDefinition> releaseArtifacts =
                    _agentServer.GetReleaseArtifactsFromService(teamProjectId, releaseId).ToList();

                releaseArtifacts.ForEach(x => Trace.Info($"Found Artifact = {x.Alias}"));

                CleanUpArtifactsFolder(executionContext, artifactsWorkingFolder);
                await DownloadArtifacts(executionContext, releaseArtifacts, artifactsWorkingFolder, teamProjectId);
            }
            catch (Exception ex)
            {
                executionContext.Error(ex);
                throw;
            }
        }

        private async Task DownloadArtifacts(IExecutionContext executionContext, List<AgentArtifactDefinition> agentArtifactDefinitions, string artifactsWorkingFolder, Guid teamProjectId)
        {
            Trace.Entering();

            var agentArtifactDefinitionService = HostContext.GetService<IAgentArtifactDefinitionService>();
            foreach (AgentArtifactDefinition agentArtifactDefinition in agentArtifactDefinitions)
            {
                executionContext.Output(StringUtil.Loc("RMStartArtifactsDownload"));
                ArtifactDefinition artifactDefinition = agentArtifactDefinitionService.ConvertToArtifactDefinition(agentArtifactDefinition, executionContext);
                executionContext.Output(StringUtil.Loc("RMArtifactDownloadBegin", agentArtifactDefinition.Alias));
                executionContext.Output(StringUtil.Loc("RMDownloadArtifactType", agentArtifactDefinition.ArtifactType));

                // Get the local path where this artifact should be downloaded. 
                string downloadFolderPath = GetLocalDownloadFolderPath(agentArtifactDefinition, artifactsWorkingFolder);

                // Create the directory if it does not exist. 
                if (!Directory.Exists(downloadFolderPath))
                {
                    Directory.CreateDirectory(downloadFolderPath);
                    executionContext.Output(StringUtil.Loc("RMArtifactFolderCreated", downloadFolderPath));
                }

                // download the artifact to this path. 
                RetryExecutor retryExecutor = new RetryExecutor();
                await retryExecutor.ExecuteAsync(
                    async () =>
                        {
                            if (Directory.Exists(downloadFolderPath))
                            {
                                //TODO:SetAttributesToNormal
                                Directory.Delete(downloadFolderPath, true);
                            }

                            if (agentArtifactDefinition.ArtifactType == AgentArtifactType.GitHub
                                || agentArtifactDefinition.ArtifactType == AgentArtifactType.TFGit
                                || agentArtifactDefinition.ArtifactType == AgentArtifactType.Tfvc)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                var buildArtifactProvider = HostContext.GetService<IBuildArtifactProvider>();
                                await buildArtifactProvider.Download(executionContext, artifactDefinition, downloadFolderPath);
                            }
                        });

                executionContext.Output(StringUtil.Loc("RMArtifactDownloadFinished", agentArtifactDefinition.Alias));
            }

            executionContext.Output(StringUtil.Loc("RMArtifactsDownloadFinished"));
        }

        private static string GetLocalDownloadFolderPath(AgentArtifactDefinition artifactDefinition, string artifactsWorkingFolder)
        {
            return  Path.Combine(artifactsWorkingFolder, artifactDefinition.Alias ?? string.Empty);
        }

        private void CleanUpArtifactsFolder(IExecutionContext executionContext, string artifactsWorkingFolder)
        {
            Trace.Entering();
            executionContext.Output(StringUtil.Loc("RMCleaningArtifactsDirectory", artifactsWorkingFolder));
            try
            {
                if (Directory.Exists(artifactsWorkingFolder))
                {
                    Directory.Delete(artifactsWorkingFolder);
                }
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                // Do not throw here
            }
            finally
            {
                executionContext.Output(StringUtil.Loc("RMCleanedUpArtifactsDirectory", artifactsWorkingFolder));
            }
        }

        private void InitializeAgent(IExecutionContext executionContext, out bool skipArtifactsDownload, out Guid teamProjectId, out string artifactsWorkingFolder, out int releaseId)
        {
            Trace.Entering();

            releaseId = executionContext.Variables.GetInt(Constants.Variables.Release.ReleaseId) ?? 0;
            teamProjectId = executionContext.Variables.GetGuid(Constants.Variables.System.TeamProjectId) ?? Guid.Empty;
            skipArtifactsDownload = executionContext.Variables.GetBoolean(Constants.Variables.Release.SkipArtifactsDownload) ?? false;
            string releaseDefinitionName = executionContext.Variables.Get(Constants.Variables.Release.ReleaseDefinitionName);

            // TODO: Should we also write to log in executionContext.Output methods? so that we don't have to repeat writing into logs?
            // Log these values here to debug scenarios where downloading the artifact fails.
            executionContext.Output(
                $"ReleaseId={releaseId}, TeamProjectId={teamProjectId}, ReleaseDefinitionName={releaseDefinitionName}");

            var configuration = HostContext.GetService<IConfigurationStore>();
            var shortHash = ReleaseFolderHelper.CreateShortHash(
                configuration.GetSettings().AgentName,
                teamProjectId.ToString(),
                releaseDefinitionName);
            executionContext.Output($"Release folder: {shortHash}");

            artifactsWorkingFolder = Path.Combine(IOUtil.GetWorkPath(this.HostContext), shortHash);
            this.SetLocalVariables(executionContext, artifactsWorkingFolder);

            // Log the environment variables available after populating the variable service with our variables
            LogEnvironmentVariables(executionContext);

            if (skipArtifactsDownload)
            {
                // If this is the first time the agent is executing a task, we need to create the artifactsFolder
                // otherwise Process.StartWithCreateProcess() will fail with the error "The directory name is invalid"
                // because the working folder doesn't exist
                CreateWorkingFolderIfRequired(executionContext, artifactsWorkingFolder);

                // log the message that the user chose to skip artifact download and move on
                executionContext.Output(StringUtil.Loc("RMUserChoseToSkipArtifactDownload"));
                Trace.Info("Skipping artifact download based on the setting specified.");
            }
        }

        //private static IEnumerable<AgentArtifactDefinition> GetReleaseArtifactsFromService(
        //    IJobRequest job,
        //    CancellationToken cancellationToken,
        //    string teamProject,
        //    int releaseId)
        //{
        //    var connection = job.GetVssConnection();
        //    var releaseManagementHttpClient = connection.GetClient<ReleaseHttpClient>();
        //    var artifacts = releaseManagementHttpClient.GetAgentArtifactDefinitionsAsync(teamProject, releaseId, userState: null, cancellationToken: cancellationToken).Result;
        //    return artifacts;
        //}

        private void SetLocalVariables(IExecutionContext executionContext, string artifactsDirectoryPath)
        {
            Trace.Entering();

            // Always set the AgentReleaseDirectory because this is set as the WorkingDirectory of the task.
            executionContext.Variables.Set(WellKnownReleaseVariables.AgentReleaseDirectory, artifactsDirectoryPath);

            // Set the ArtifactsDirectory even when artifacts downloaded is skipped.  Reason: The task might want to access the old artifact.
            executionContext.Variables.Set(WellKnownReleaseVariables.ArtifactsDirectory, artifactsDirectoryPath);
            executionContext.Variables.Set(Constants.Variables.System.DefaultWorkingDirectory, artifactsDirectoryPath);
        }

        private void LogEnvironmentVariables(IExecutionContext executionContext)
        {
            Trace.Entering();
            string stringifiedEnvironmentVariables = AgentUtilities.GetEnvironmentVariables(executionContext.Variables.Public);

            // Use LogMessage to ensure that the logs reach the TWA UI, but don't spam the console cmd window
            executionContext.Output(StringUtil.Loc("RMEnvironmentVariablesAvailable", stringifiedEnvironmentVariables));
        }

        private void CreateWorkingFolderIfRequired(IExecutionContext executionContext, string artifactsFolderPath)
        {
            Trace.Entering();
            if (!Directory.Exists(artifactsFolderPath))
            {
                Trace.Info($"Creating artifacts folder: {artifactsFolderPath}");
                executionContext.Output($"Creating artifacts folder: {artifactsFolderPath}");
                Directory.CreateDirectory(artifactsFolderPath);
            }
        }
    }
}