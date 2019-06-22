using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
	/* IMPORTANT NOTE: if you're adding a new build variable that's designed to hold PII data
	   (e.g. names, addresses, phone numbers, IP addresses, emails), please add a corresponding reference to `PiiVariables` at
	   https://github.com/Microsoft/azure-pipelines-agent/blob/master/src/Agent.Worker/Variables.cs
	   This is so the agent can scrub the variable value from the diagnostics log. */
    [GenerateAllConstants]
    public static class BuildVariables
    {
        public const String CollectionId = "system.collectionId";
        public const String DefinitionId = "system.definitionId";
        public const String HostType = "system.hosttype";
        public const String IsFork = "system.pullRequest.isFork";
        public const String ForkSecretsRemoved= "system.pullRequest.forkSecretsRemoved";
        public const String PullRequestId = "system.pullRequest.pullRequestId";
        public const String PullRequestNumber = "system.pullRequest.pullRequestNumber";
        public const String PullRequestIterationId = "system.pullRequest.pullRequestIteration";
        public const String PullRequestSourceBranch = "system.pullRequest.sourceBranch";
        public const String PullRequestTargetBranch = "system.pullRequest.targetBranch";
        public const String PullRequestSourceRepositoryUri = "system.pullRequest.sourceRepositoryUri";
        public const String PullRequestSourceCommitId = "system.pullRequest.sourceCommitId";
        public const String PullRequestMergedAt = "system.pullRequest.mergedAt";
        public const String System = "system";
        public const String TeamProject = "system.teamProject";
        public const String TeamProjectId = "system.teamProjectId";

        public const String BuildId = "build.buildId";
        public const String BuildNumber = "build.buildNumber";
        public const String BuildUri = "build.buildUri";
        public const String ContainerId = "build.containerId";
        public const String DefinitionName = "build.definitionName";
        public const String DefinitionVersion = "build.definitionVersion";
        public const String JobAuthorizeAs = "Job.AuthorizeAs";
        public const String JobAuthorizeAsId = "Job.AuthorizeAsId";
        public const String QueuedBy = "build.queuedBy";
        public const String QueuedById = "build.queuedById";
        public const String Reason = "build.reason";
        public const String RepoUri = "build.repository.uri";
        public const String RequestedFor = "build.requestedFor";
        public const String RequestedForEmail = "build.requestedForEmail";
        public const String RequestedForId = "build.requestedForId";
        public const String SourceBranch = "build.sourceBranch";
        public const String SourceBranchName = "build.sourceBranchName";
        public const String SourceTfvcShelveset = "build.sourceTfvcShelveset";
        public const String SourceVersion = "build.sourceVersion";
        public const String SourceVersionAuthor = "build.sourceVersionAuthor";
        public const String SourceVersionMessage = "build.sourceVersionMessage";
        public const String SyncSources = "build.syncSources";
    }

    [Obsolete("Use BuildVariables instead.")]
    public static class WellKnownBuildVariables
    {
        public const String System = BuildVariables.System;
        public const String CollectionId = BuildVariables.CollectionId;
        public const String TeamProject = BuildVariables.TeamProject;
        public const String TeamProjectId = BuildVariables.TeamProjectId;
        public const String DefinitionId = BuildVariables.DefinitionId;
        public const String HostType = BuildVariables.HostType;
        public const String IsFork = BuildVariables.IsFork;
        public const String DefinitionName = BuildVariables.DefinitionName;
        public const String DefinitionVersion = BuildVariables.DefinitionVersion;
        public const String QueuedBy = BuildVariables.QueuedBy;
        public const String QueuedById = BuildVariables.QueuedById;
        public const String Reason = BuildVariables.Reason;
        public const String RequestedFor = BuildVariables.RequestedFor;
        public const String RequestedForId = BuildVariables.RequestedForId;
        public const String RequestedForEmail = BuildVariables.RequestedForEmail;
        public const String SourceBranch = BuildVariables.SourceBranch;
        public const String SourceBranchName = BuildVariables.SourceBranchName;
        public const String SourceVersion = BuildVariables.SourceVersion;
        public const String SourceVersionAuthor = BuildVariables.SourceVersionAuthor;
        public const String SourceVersionMessage = BuildVariables.SourceVersionMessage;
        public const String SourceTfvcShelveset = BuildVariables.SourceTfvcShelveset;
        public const String BuildId = BuildVariables.BuildId;
        public const String BuildUri = BuildVariables.BuildUri;
        public const String BuildNumber = BuildVariables.BuildNumber;
        public const String ContainerId = BuildVariables.ContainerId;
        public const String SyncSources = BuildVariables.SyncSources;
        public const String JobAuthorizeAs = BuildVariables.JobAuthorizeAs;
        public const String JobAuthorizeAsId = BuildVariables.JobAuthorizeAsId;
        public const String RepoUri = BuildVariables.RepoUri;
    }
}
