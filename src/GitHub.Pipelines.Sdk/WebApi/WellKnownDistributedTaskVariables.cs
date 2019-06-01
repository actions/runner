using System;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public static class WellKnownDistributedTaskVariables
    {
        public static readonly String AccessToken = "system.accessToken";
        public static readonly String AccessTokenScope = "system.connection.accessTokenScope";
        public static readonly String AzureUserAgent = "AZURE_HTTP_USER_AGENT";
        public static readonly String CollectionId = "system.collectionId";
        public static readonly String CollectionUrl = "system.collectionUri";
        public static readonly String Culture = "system.culture";
        public static readonly String DefinitionId = "system.definitionId";
        public static readonly String DefinitionName = "system.definitionName";
        public static readonly String EnableAccessToken = "system.enableAccessToken";
        public static readonly String HostType = "system.hosttype";
        public static readonly String HubVersion = "system.hubversion";
        public static readonly String IsScheduled = "system.isScheduled";
        public static readonly String JobAttempt = "system.jobAttempt";
        public static readonly String JobDisplayName = "system.jobDisplayName";
        public static readonly String JobId = "system.jobId";
        public static readonly String JobIdentifier = "system.jobIdentifier";
        public static readonly String JobName = "system.jobName";
        public static readonly String JobParallelismTag = "system.jobParallelismTag";
        public static readonly String JobPositionInPhase = "System.JobPositionInPhase";
        public static readonly String JobStatus = "system.jobStatus";
        public static readonly String MsDeployUserAgent = "MSDEPLOY_HTTP_USER_AGENT";
        public static readonly String ParallelExecutionType = "System.ParallelExecutionType";
        public static readonly String PhaseAttempt = "system.phaseAttempt";
        public static readonly String PhaseDisplayName = "system.phaseDisplayName";
        public static readonly String PhaseId = "system.phaseId";
        public static readonly String PhaseName = "system.phaseName";
        public static readonly String PipelineStartTime = "system.pipelineStartTime";
        public static readonly String PlanId = "system.planId";
        public static readonly String RestrictSecrets = "system.restrictSecrets";
        public static readonly String RetainDefaultEncoding = "agent.retainDefaultEncoding";
        public static readonly String ServerType = "system.servertype";
        public static readonly String StageAttempt = "system.stageAttempt";
        public static readonly String StageDisplayName = "system.stageDisplayName";
        public static readonly String StageId = "system.stageId";
        public static readonly String StageName = "system.stageName";
        public static readonly String System = "system";
        public static readonly String TFCollectionUrl = "system.teamFoundationCollectionUri";
        public static readonly String TaskDefinitionsUrl = "system.taskDefinitionsUri";
        public static readonly String TaskDisplayName = "system.taskDisplayName";
        public static readonly String TaskInstanceId = "system.taskInstanceId";
        public static readonly String TaskInstanceName = "system.taskInstanceName";
        public static readonly String TeamProject = "system.teamProject";
        public static readonly String TeamProjectId = "system.teamProjectId";
        public static readonly String TimelineId = "system.timelineId";
        public static readonly String TotalJobsInPhase = "System.TotalJobsInPhase";
    }
}
