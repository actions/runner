using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public enum AgentStatus
    {
        /// <summary>
        /// Indicates that the build agent cannot be contacted.
        /// </summary>
        [EnumMember]
        Unavailable = 0,

        /// <summary>
        /// Indicates that the build agent is currently available.
        /// </summary>
        [EnumMember]
        Available = 1,

        /// <summary>
        /// Indicates that the build agent has taken itself offline.
        /// </summary>
        [EnumMember]
        Offline = 2,
    }

    [DataContract]
    public enum AuditAction
    {
        [EnumMember]
        Add = 1,
        [EnumMember]
        Update = 2,
        [EnumMember]
        Delete = 3
    }

    /// <summary>
    /// Represents the desired scope of authorization for a build.
    /// </summary>
    [DataContract]
    public enum BuildAuthorizationScope
    {
        /// <summary>
        /// The identity used should have build service account permissions scoped to the project collection. This is 
        /// useful when resources for a single build are spread across multiple projects.
        /// </summary>
        [EnumMember]
        ProjectCollection = 1,

        /// <summary>
        /// The identity used should have build service account permissions scoped to the project in which the build
        /// definition resides. This is useful for isolation of build jobs to a particular team project to avoid any
        /// unintentional escalation of privilege attacks during a build.
        /// </summary>
        [EnumMember]
        Project = 2,
    }

    [DataContract]
    public enum BuildOptionInputType
    {
        [EnumMember]
        String,
        [EnumMember]
        Boolean,
        [EnumMember]
        StringList,
        [EnumMember]
        Radio,
        [EnumMember]
        PickList,
        [EnumMember]
        MultiLine,
        [EnumMember]
        BranchFilter
    }

    [DataContract]
    public enum BuildPhaseStatus
    {
        /// <summary>
        /// The state is not known.
        /// </summary>
        [EnumMember]
        Unknown = 0,

        /// <summary>
        /// The build phase completed unsuccessfully.
        /// </summary>
        [EnumMember]
        Failed = 1,

        /// <summary>
        /// The build phase completed successfully.
        /// </summary>
        [EnumMember]
        Succeeded = 2,
    }

    /// <summary>
    /// Specifies the desired ordering of builds.
    /// </summary>
    [DataContract]
    public enum BuildQueryOrder
    {
        /// <summary>
        /// Order by finish time ascending.
        /// </summary>
        [EnumMember]
        FinishTimeAscending = 2,

        /// <summary>
        /// Order by finish time descending.
        /// </summary>
        [EnumMember]
        FinishTimeDescending = 3,

        /// <summary>
        /// Order by queue time descending.
        /// </summary>
        [EnumMember]
        QueueTimeDescending = 4,

        /// <summary>
        /// Order by queue time ascending.
        /// </summary>
        [EnumMember]
        QueueTimeAscending = 5,

        /// <summary>
        /// Order by start time descending.
        /// </summary>
        [EnumMember]
        StartTimeDescending = 6,

        /// <summary>
        /// Order by start time ascending.
        /// </summary>
        [EnumMember]
        StartTimeAscending = 7
    }

    /// <summary>
    /// Specifies the desired ordering of definitions.
    /// </summary>
    [DataContract]
    public enum DefinitionQueryOrder
    {
        /// <summary>
        /// No order
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Order by created on/last modified time ascending.
        /// </summary>
        [EnumMember]
        LastModifiedAscending = 1,

        /// <summary>
        /// Order by created on/last modified time descending.
        /// </summary>
        [EnumMember]
        LastModifiedDescending = 2,

        /// <summary>
        /// Order by definition name ascending.
        /// </summary>
        [EnumMember]
        DefinitionNameAscending = 3,

        /// <summary>
        /// Order by definition name descending.
        /// </summary>
        [EnumMember]
        DefinitionNameDescending = 4
    }

    /// <summary>
    /// Specifies the desired ordering of folders.
    /// </summary>
    [DataContract]
    public enum FolderQueryOrder
    {
        /// <summary>
        /// No order
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Order by folder name and path ascending.
        /// </summary>
        [EnumMember]
        FolderAscending = 1,

        /// <summary>
        /// Order by folder name and path descending.
        /// </summary>
        [EnumMember]
        FolderDescending = 2
    }

    [DataContract]
    public enum BuildReason
    {
        /// <summary>
        /// No reason. This value should not be used.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// The build was started manually.
        /// </summary>
        [EnumMember]
        Manual = 1,

        /// <summary>
        /// The build was started for the trigger TriggerType.ContinuousIntegration.
        /// </summary>
        [EnumMember]
        IndividualCI = 2,

        /// <summary>
        /// The build was started for the trigger TriggerType.BatchedContinuousIntegration.
        /// </summary>
        [EnumMember]
        BatchedCI = 4,

        /// <summary>
        /// The build was started for the trigger TriggerType.Schedule.
        /// </summary>
        [EnumMember]
        Schedule = 8,

        /// <summary>
        /// The build was started for the trigger TriggerType.ScheduleForced.
        /// </summary>
        [EnumMember]
        ScheduleForced = 16,

        /// <summary>
        /// The build was created by a user.
        /// </summary>
        [EnumMember]
        UserCreated = 32,

        /// <summary>
        /// The build was started manually for private validation.
        /// </summary>
        [EnumMember]
        ValidateShelveset = 64,

        /// <summary>
        /// The build was started for the trigger ContinuousIntegrationType.Gated.
        /// </summary>
        [EnumMember]
        CheckInShelveset = 128,

        /// <summary>
        /// The build was started by a pull request.
        /// Added in resource version 3.
        /// </summary>
        [EnumMember]
        PullRequest = 256,
        
        /// <summary>
        /// The build was started when another build completed.
        /// </summary>
        [EnumMember]
        BuildCompletion = 512,

        /// <summary>
        /// The build was triggered for retention policy purposes.
        /// </summary>
        [EnumMember]
        Triggered = Manual | IndividualCI | BatchedCI | Schedule | UserCreated | CheckInShelveset | PullRequest | BuildCompletion,

        /// <summary>
        /// All reasons.
        /// </summary>
        [EnumMember]
        All = Manual | IndividualCI | BatchedCI | Schedule | UserCreated | ValidateShelveset | CheckInShelveset | PullRequest | BuildCompletion,
    }

    /// <summary>
    /// This is not a Flags enum because we don't want to set multiple statuses on a build.
    /// However, when adding values, please stick to powers of 2 as if it were a Flags enum
    /// This will ensure that things that key off multiple result types (like labelling sources) continue to work
    /// </summary>
    [DataContract]
    public enum BuildResult
    {
        /// <summary>
        /// No result
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// The build completed successfully.
        /// </summary>
        [EnumMember]
        Succeeded = 2,

        /// <summary>
        /// The build completed compilation successfully but had other errors.
        /// </summary>
        [EnumMember]
        PartiallySucceeded = 4,

        /// <summary>
        /// The build completed unsuccessfully.
        /// </summary>
        [EnumMember]
        Failed = 8,

        /// <summary>
        /// The build was canceled before starting.
        /// </summary>
        [EnumMember]
        Canceled = 32
    }

    [DataContract]
    public enum BuildStatus
    {
        /// <summary>
        /// No status.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// The build is currently in progress.
        /// </summary>
        [EnumMember]
        InProgress = 1,

        /// <summary>
        /// The build has completed.
        /// </summary>
        [EnumMember]
        Completed = 2,

        /// <summary>
        /// The build is cancelling
        /// </summary>
        [EnumMember]
        Cancelling = 4,

        /// <summary>
        /// The build is inactive in the queue.
        /// </summary>
        [EnumMember]
        Postponed = 8,

        /// <summary>
        /// The build has not yet started.
        /// </summary>
        [EnumMember]
        NotStarted = 32,

        /// <summary>
        /// All status.
        /// </summary>
        [EnumMember]
        All = 47,
    }

    [DataContract]
    public enum ControllerStatus
    {
        /// <summary>
        /// Indicates that the build controller cannot be contacted.
        /// </summary>
        [EnumMember]
        Unavailable = 0,

        /// <summary>
        /// Indicates that the build controller is currently available.
        /// </summary>
        [EnumMember]
        Available = 1,

        /// <summary>
        /// Indicates that the build controller has taken itself offline.
        /// </summary>
        [EnumMember]
        Offline = 2,
    }

    [DataContract]
    public enum DefinitionType
    {
        [EnumMember]
        Xaml = 1,
        [EnumMember]
        Build = 2

    }

    [DataContract]
    public enum DefinitionQuality
    {
        [EnumMember]
        Definition = 1,
        [EnumMember]
        Draft = 2
    }

    [DataContract]
    public enum GetOption
    {
        /// <summary>
        /// Use the latest changeset at the time the build is queued.
        /// </summary>
        [EnumMember]
        LatestOnQueue = 0,

        /// <summary>
        /// Use the latest changeset at the time the build is started.
        /// </summary>
        [EnumMember]
        LatestOnBuild = 1,

        /// <summary>
        /// A user-specified version has been supplied.
        /// </summary>
        [EnumMember]
        Custom = 2,
    }

    [DataContract]
    public enum IssueType
    {
        [EnumMember]
        Error = 1,

        [EnumMember]
        Warning = 2
    }

    [DataContract]
    public enum QueryDeletedOption
    {
        /// <summary>
        /// Include only non-deleted builds.
        /// </summary>
        [EnumMember]
        ExcludeDeleted = 0,

        /// <summary>
        /// Include deleted and non-deleted builds.
        /// </summary>
        [EnumMember]
        IncludeDeleted = 1,

        /// <summary>
        /// Include only deleted builds.
        /// </summary>
        [EnumMember]
        OnlyDeleted = 2
    }

    [DataContract]
    public enum QueuePriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        [EnumMember]
        Low = 5,

        /// <summary>
        /// Below normal priority.
        /// </summary>
        [EnumMember]
        BelowNormal = 4,

        /// <summary>
        /// Normal priority.
        /// </summary>
        [EnumMember]
        Normal = 3,

        /// <summary>
        /// Above normal priority.
        /// </summary>
        [EnumMember]
        AboveNormal = 2,

        /// <summary>
        /// High priority.
        /// </summary>
        [EnumMember]
        High = 1,
    }

    [DataContract]
    [Flags]
    public enum QueueOptions
    {
        /// <summary>
        /// No queue options 
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Create a plan Id for the build, do not run it
        /// </summary>
        [EnumMember]
        DoNotRun = 1
    }

    [DataContract]
    public enum RepositoryCleanOptions
    {
        /// <summary>
        /// Run git clean -fdx && git reset --hard or Tf /scorch on $(build.sourcesDirectory)
        /// </summary>
        [EnumMember]
        Source,

        /// <summary>
        /// Run git clean -fdx && git reset --hard or Tf /scorch on $(build.sourcesDirectory), also re-create $(build.binariesDirectory)
        /// </summary>
        [EnumMember]
        SourceAndOutputDir,

        /// <summary>
        /// Re-create $(build.sourcesDirectory)
        /// </summary>
        [EnumMember]
        SourceDir,

        /// <summary>
        /// Re-create $(agnet.buildDirectory) which contains $(build.sourcesDirectory), $(build.binariesDirectory) and any folders that left from previous build.
        /// </summary>
        [EnumMember]
        AllBuildDir,
    }

    [DataContract]
    public enum ResultSet
    {
        /// <summary>
        /// Include all repositories
        /// </summary>
        [EnumMember]
        All = 0,

        /// <summary>
        /// Include most relevant repositories for user
        /// </summary>
        [EnumMember]
        Top = 1,
    }

    [DataContract]
    public enum ServiceHostStatus
    {
        /// <summary>
        /// The service host is currently connected and accepting commands.
        /// </summary>
        [EnumMember]
        Online = 1,

        /// <summary>
        /// The service host is currently disconnected and not accepting commands.
        /// </summary>
        [EnumMember]
        Offline = 2,
    }

    [DataContract]
    public enum TaskResult
    {
        [EnumMember]
        Succeeded = 0,

        [EnumMember]
        SucceededWithIssues = 1,

        [EnumMember]
        Failed = 2,

        [EnumMember]
        Canceled = 3,

        [EnumMember]
        Skipped = 4,

        [EnumMember]
        Abandoned = 5,
    }

    [DataContract]
    public enum TimelineRecordState
    {
        [EnumMember]
        Pending,

        [EnumMember]
        InProgress,

        [EnumMember]
        Completed,
    }

    [DataContract]
    public enum DefinitionTriggerType
    {
        /// <summary>
        /// Manual builds only.
        /// </summary>
        [EnumMember]
        None = 1,

        /// <summary>
        /// A build should be started for each changeset.
        /// </summary>
        [EnumMember]
        ContinuousIntegration = 2,

        /// <summary>
        /// A build should be started for multiple changesets at a time at a specified interval.
        /// </summary>
        [EnumMember]
        BatchedContinuousIntegration = 4,

        /// <summary>
        /// A build should be started on a specified schedule whether or not changesets exist.
        /// </summary>
        [EnumMember]
        Schedule = 8,

        /// <summary>
        /// A validation build should be started for each check-in.
        /// </summary>
        [EnumMember]
        GatedCheckIn = 16,

        /// <summary>
        /// A validation build should be started for each batch of check-ins.
        /// </summary>
        [EnumMember]
        BatchedGatedCheckIn = 32,

        /// <summary>
        /// A build should be triggered when a GitHub pull request is created or updated.
        /// Added in resource version 3
        /// </summary>
        [EnumMember]
        PullRequest = 64,

        /// <summary>
        /// A build should be triggered when another build completes.
        /// </summary>
        [EnumMember]
        BuildCompletion = 128,

        /// <summary>
        /// All types.
        /// </summary>
        [EnumMember]
        All = None | ContinuousIntegration | BatchedContinuousIntegration | Schedule | GatedCheckIn | BatchedGatedCheckIn | PullRequest | BuildCompletion,
    }

    [DataContract]
    public enum DefinitionQueueStatus
    {
        /// <summary>
        /// When enabled the definition queue allows builds to be queued by users,
        /// the system will queue scheduled, gated and continuous integration builds,
        /// and the queued builds will be started by the system.
        /// </summary>
        [EnumMember]
        Enabled,

        /// <summary>
        /// When paused the definition queue allows builds to be queued by users
        /// and the system will queue scheduled, gated and continuous integration builds.
        /// Builds in the queue will not be started by the system.
        /// </summary>
        [EnumMember]
        Paused,

        /// <summary>
        /// When disabled the definition queue will not allow builds to be queued by users
        /// and the system will not queue scheduled, gated or continuous integration builds.
        /// Builds already in the queue will not be started by the system.
        /// </summary>
        [EnumMember]
        Disabled
    }

    [DataContract]
    public enum DeleteOptions
    {
        /// <summary>
        /// No data should be deleted. This value should not be used.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// The drop location should be deleted.
        /// </summary>
        [EnumMember]
        DropLocation = 1,

        /// <summary>
        /// The test results should be deleted.
        /// </summary>
        [EnumMember]
        TestResults = 2,

        /// <summary>
        /// The version control label should be deleted.
        /// </summary>
        [EnumMember]
        Label = 4,

        /// <summary>
        /// The build should be deleted.
        /// </summary>
        [EnumMember]
        Details = 8,

        /// <summary>
        /// Published symbols should be deleted.
        /// </summary>
        [EnumMember]
        Symbols = 16,

        /// <summary>
        /// All data should be deleted.
        /// </summary>
        [EnumMember]
        All = 31,
    }

    [DataContract]
    public enum ScheduleDays
    {
        /// <summary>
        /// Do not run.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Run on Monday.
        /// </summary>
        [EnumMember]
        Monday = 1,

        /// <summary>
        /// Run on Tuesday.
        /// </summary>
        [EnumMember]
        Tuesday = 2,

        /// <summary>
        /// Run on Wednesday.
        /// </summary>
        [EnumMember]
        Wednesday = 4,

        /// <summary>
        /// Run on Thursday.
        /// </summary>
        [EnumMember]
        Thursday = 8,

        /// <summary>
        /// Run on Friday.
        /// </summary>
        [EnumMember]
        Friday = 16,

        /// <summary>
        /// Run on Saturday.
        /// </summary>
        [EnumMember]
        Saturday = 32,

        /// <summary>
        /// Run on Sunday.
        /// </summary>
        [EnumMember]
        Sunday = 64,

        /// <summary>
        /// Run on all days of the week.
        /// </summary>
        [EnumMember]
        All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
    }

    [DataContract]
    public enum WorkspaceMappingType
    {
        /// <summary>
        /// The path is mapped in the workspace.
        /// </summary>
        [EnumMember]
        Map = 0,

        /// <summary>
        /// The path is cloaked in the workspace.
        /// </summary>
        [EnumMember]
        Cloak = 1,
    }

    [DataContract]
    public enum ProcessTemplateType
    {
        /// <summary>
        /// Indicates a custom template.
        /// </summary>
        [EnumMember]
        Custom = 0,

        /// <summary>
        /// Indicates a default template.
        /// </summary>
        [EnumMember]
        Default = 1,

        /// <summary>
        /// Indicates an upgrade template.
        /// </summary>
        [EnumMember]
        Upgrade = 2,
    }

    [DataContract]
    public enum ValidationResult
    {
        [EnumMember]
        OK,
        [EnumMember]
        Warning,
        [EnumMember]
        Error
    }
}
