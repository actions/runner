using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public class SourceProviderAttributes
    {
        /// <summary>
        /// The name of the source provider.
        /// </summary>
        [DataMember]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The types of triggers supported by this source provider.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IList<SupportedTrigger> SupportedTriggers
        {
            get;
            set;
        }

        /// <summary>
        /// The capabilities supported by this source provider.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<string, bool> SupportedCapabilities
        {
            get;
            set;
        }

        /// <summary>
        /// The environments where this source provider is available.
        /// </summary>
        [IgnoreDataMember]
        public SourceProviderAvailability Availability
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the repository type is external to TFS / VSTS servers
        /// </summary>
        [IgnoreDataMember]
        public bool IsExternal
        {
            get;
            set;
        }

        #region Server-side convenience properties
        [IgnoreDataMember]
        public bool SupportsSourceLinks => SupportedCapabilities != null && SupportedCapabilities.TryGetValue(SourceProviderCapabilities.SourceLinks, out bool supported) && supported;

        [IgnoreDataMember]
        public bool SupportsYamlDefinition => SupportedCapabilities != null && SupportedCapabilities.TryGetValue(SourceProviderCapabilities.YamlDefinition, out bool supported) && supported;

        [IgnoreDataMember]
        public DefinitionTriggerType SupportedTriggerTypes => SupportedTriggers?.Select(t => t.Type).Aggregate(DefinitionTriggerType.None, (x, y) => x | y) ?? DefinitionTriggerType.None;
        #endregion
    }

    [DataContract]
    public class SupportedTrigger
    {
        /// <summary>
        /// The type of trigger.
        /// </summary>
        [DataMember]
        public DefinitionTriggerType Type { get; set; }

        /// <summary>
        /// How the trigger is notified of changes.
        /// </summary>
        /// <remarks>
        /// See <see cref="TriggerNotificationTypes"/> for supported values.
        /// </remarks>
        [DataMember]
        public string NotificationType { get; set; }

        /// <summary>
        /// The default interval to wait between polls (only relevant when <see cref="NotificationType"/> is <see cref="TriggerNotificationTypes.Polling"/>).
        /// </summary>
        [DataMember]
        public int DefaultPollingInterval { get; set; }

        /// <summary>
        /// The capabilities supported by this trigger.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<string, SupportLevel> SupportedCapabilities
        {
            get;
            set;
        }
    }

    [DataContract]
    public enum SupportLevel
    {
        /// <summary>
        /// The functionality is not supported.
        /// </summary>
        [EnumMember]
        Unsupported,

        /// <summary>
        /// The functionality is supported.
        /// </summary>
        [EnumMember]
        Supported,

        /// <summary>
        /// The functionality is required.
        /// </summary>
        [EnumMember]
        Required
    }

    [DataContract]
    public enum SourceProviderAvailability
    {
        /// <summary>
        /// The source provider is available in the hosted environment.
        /// </summary>
        [EnumMember]
        Hosted = 1,

        /// <summary>
        /// The source provider is available in the on-premises environment.
        /// </summary>
        [EnumMember]
        OnPremises = 2,

        /// <summary>
        /// The source provider is available in all environments.
        /// </summary>
        [EnumMember]
        All = Hosted | OnPremises
    }

    public class SourceProviderCapabilities
    {
        public const string CreateLabel = "createLabel";
        public const string QueryBranches = "queryBranches";
        public const string QueryFileContents = "queryFileContents";
        public const string QueryPathContents = "queryPathContents";
        public const string QueryPullRequest = "queryPullRequest";
        public const string QueryRelatedWorkItems = "queryRelatedWorkItems";
        public const string QueryRepositories = "queryRepositories";
        public const string QueryTopRepositories = "queryTopRepositories";
        public const string QueryWebhooks = "queryWebhooks";
        public const string SourceLinks = "sourceLinks";
        public const string YamlDefinition = "yamlDefinition";
    }

    public class TriggerCapabilities
    {
        public const string BranchFilters = "branchFilters";
        public const string PathFilters = "pathFilters";
        public const string BatchChanges = "batchChanges";
        public const string BuildForks = "buildForks";
        public const string Comments = "comments";
    }

    public class TriggerNotificationTypes
    {
        public const string None = "none";
        public const string Polling = "polling";
        public const string Webhook = "webhook";
    }
}
