using System;
using System.ComponentModel;

namespace GitHub.Services.WebApi
{
    public static class DataImportPropertyConstants
    {
        // Used on servicing
        public const string Prefix = "DataImport.";
        public const string InternalPrefix = Prefix + "Internal.";  // All properties that use this prefix require advanced permissions to queue.
        public const string ComputedPrefix = Prefix + "Computed.";  // All properties that use this prefix require advanced permissions to queue, and cannot be written to the DB, they are computed
        public const string AccountOwner = Prefix + "AccountOwner";
        public const string AADTenantName = Prefix + "AADTenantName";
        public const string AccountName = Prefix + "AccountName";
        public const string AccountRegion = Prefix + "AccountRegion";
        public const string AccountScaleUnit = Prefix + "AccountScaleUnit";
        public const string SourceDacpacLocation = Prefix + "SourceDacpacLocation";
        public const string HostToMovePostImport = Prefix + "HostToMovePostImport";
        public const string TargetDatabaseDowngradeSize = Prefix + "TargetDatabaseDowngradeSize";
        public const string UseDevOpsDomainUrls = InternalPrefix + "UseCodexDomainUrls";

        [EditorBrowsable(EditorBrowsableState.Never)]
        public const string IdentityImportMapping = Prefix + "IdentityImportMapping";

        public const string SkipDataImportFileContent = Prefix + "SkipFileContent";
        public const string SkipDataImportValidation = Prefix + "SkipValidation";
        public const string SkipDataImportWIT = Prefix + "SkipDataImportWIT";
        public const string SkipValidationTfvcInFileService = InternalPrefix + nameof(SkipValidationTfvcInFileService);
        public const string TryMapADGroupsToAADGroupsAutomatically = Prefix + "TryMapADGroupsToAADGroupsAutomatically";
        public const string SourceDatabase = Prefix + "SourceDatabase";
        public const string NeighborHostId = Prefix + "NeighborHostId";
        public const string CollectionCreationJobId = Prefix + "CollectionCreationJobId";
        public const string DatabaseImportJobId = Prefix + "DatabaseImportJobId";
        public const string ActivateImportJobId = Prefix + "ActivateImportJobId";
        public const string ObtainDatabaHoldJobId = Prefix + "ObtainDatabaHoldJobId";
        public const string HostMoveCollectionJobId = Prefix + "HostMoveCollectionJobId";
        public const string OnlinePostHostUpgradeJobId = Prefix + "OnlinePostHostUpgradeJobId";
        public const string StopHostAfterUpgradeJobId = Prefix + "StopHostAfterUpgradeJobId";
        public const string RemoveImportJobId = Prefix + "RemoveImportJobId";
        public const string DehydrateJobId = Prefix + "DehydrateJobId";
        public const string OverrideServiceInstanceIds = Prefix + "OverrideServiceInstanceIds";
        public const string IgnoreImportStatus = Prefix + "IgnoreImportStatus";
        public const string KeepRegistryData = Prefix + "KeepRegistryData";
        public const string RunType = Prefix + "RunType";
        public const string ImportOwner = Prefix + "ImportOwner";
        public const string OrganizationName = Prefix + "OrganizationName";
        public const string CollectionName = Prefix + "CollectionName";
        public const string SourceCollectionId = Prefix + "SourceCollectionId";
        public const string GlobalCollectionId = Prefix + "GlobalCollectionId";
        public const string PreferredRegion = Prefix + "PreferredRegion";
        public const string OwnerId = Prefix + "OwnerId";
        public const string ProcessMode = Prefix + "ProcessMode";
        public const string IdentitySidsToImport = Prefix + "IdentitySidsToImport";
        public const string PermitForNotRules = Prefix + "PermitForNotRules";
        public const string SoftWarningVersionMissmatchInTfsMigrator = InternalPrefix + nameof(SoftWarningVersionMissmatchInTfsMigrator);
        public const string EnableCommerce = InternalPrefix + "EnableCommerce";
        public const string DisableIdentityDualWriteToDeployment = InternalPrefix + nameof(DisableIdentityDualWriteToDeployment);
        public const string EnableLicensingDataImportStepPerformer = InternalPrefix + nameof(EnableLicensingDataImportStepPerformer);
        public const string DisableLicensingIdentityStepPerformer = InternalPrefix + nameof(DisableLicensingIdentityStepPerformer);

        // Prefix for known service errors
        public const string KnownErrorPrefix = InternalPrefix + "KnownError.";

        // This flag controls whether we want to clear processId project property from projects before dataimport.
        public const string EnableClearProcessIdFromProject = InternalPrefix + nameof(EnableClearProcessIdFromProject);

        // Note this will only respected when in services where /Configuration/DataImport/AllowCustomTargetServiceObjective is set
        public const string TargetDatabaseServiceObjective = InternalPrefix + nameof(TargetDatabaseServiceObjective);

        // Set of IP address that we should add to the Firewall rules.
        public const string ExtraVIPs = InternalPrefix + nameof(ExtraVIPs);
        public const string FirewallWaitTime = InternalPrefix + nameof(FirewallWaitTime);

        // Validation Data
        public const string ImportPackage = Prefix + "ImportPackage";
        public const string SqlPackageVersion = Prefix + "SqlPackageVersion";
        public const string DatabaseTotalSize = Prefix + "DatabaseTotalSize";
        public const string DatabaseBlobSize = Prefix + "DatabaseBlobSize";
        public const string DatabaseTableSize = Prefix + "DatabaseTableSize";
        public const string ActiveUserCount = Prefix + "ActiveUserCount";
        public const string TfsVersion = Prefix + "TfsVersion";
        public const string Collation = Prefix + "Collation";
        public const string CommandExecutionTime = Prefix + "CommandExecutionTime";
        public const string CommandExecutionCount = Prefix + "CommandExecutionCount";
        public const string Force = Prefix + "Force";
        public const string TenantId = Prefix + "TenantId";
        public const string ServicesToInclude = Prefix + nameof(ServicesToInclude);
        public const string ValidationChecksum = Prefix + "ValidationChecksum";
        public const string FieldMetaDataValidation = InternalPrefix + nameof(FieldMetaDataValidation);

        // wit configuration properties
        public const string MaxWorkItemTypesPerProcess = InternalPrefix + nameof(MaxWorkItemTypesPerProcess);
        public const string MaxCustomWorkItemTypesPerProcess = InternalPrefix + nameof(MaxCustomWorkItemTypesPerProcess);
        public const string MaxFieldsPerCollection = InternalPrefix + nameof(MaxFieldsPerCollection);
        public const string MaxFieldsPerProcessTemplate = InternalPrefix + nameof(MaxFieldsPerProcessTemplate);
        public const string MaxCategoriesPerProcess = InternalPrefix + nameof(MaxCategoriesPerProcess);
        public const string MaxGlobalListCountPerProcess = InternalPrefix + nameof(MaxGlobalListCountPerProcess);
        public const string MaxGlobalListItemCountPerProcess = InternalPrefix + nameof(MaxGlobalListItemCountPerProcess);
        public const string MaxCustomLinkTypes = InternalPrefix + nameof(MaxCustomLinkTypes);

        public const string MaxStatesPerWorkItemType = InternalPrefix + nameof(MaxStatesPerWorkItemType);
        public const string MaxValuesInSingleRuleValuesList = InternalPrefix + nameof(MaxValuesInSingleRuleValuesList);
        public const string MaxSyncNameChangesFieldsPerType = InternalPrefix + nameof(MaxSyncNameChangesFieldsPerType);
        public const string MaxFieldsInWorkItemType = InternalPrefix + nameof(MaxFieldsInWorkItemType);
        public const string MaxCustomFieldsPerWorkItemType = InternalPrefix + nameof(MaxCustomFieldsPerWorkItemType);
        public const string MaxRulesPerWorkItemType = InternalPrefix + nameof(MaxRulesPerWorkItemType);

        public const string MaxPickListItemsPerList = InternalPrefix + nameof(MaxPickListItemsPerList);
        public const string MaxPickListItemLength = InternalPrefix + nameof(MaxPickListItemLength);
        public const string MaxCustomStatesPerWorkItemType = InternalPrefix + nameof(MaxCustomStatesPerWorkItemType);
        public const string MaxPortfolioBacklogLevels = InternalPrefix + nameof(MaxPortfolioBacklogLevels);
        public const string MaxPickListsPerCollection = InternalPrefix + nameof(MaxPickListsPerCollection);


        // Collection property keys
        public const string RestrictedMilestones = InternalPrefix + nameof(RestrictedMilestones);
        public const string BlockDataImportWithRunningImportEnabled = InternalPrefix + nameof(BlockDataImportWithRunningImportEnabled);
        public const string BlockDataImportWithExistingProductionEnabled = InternalPrefix + nameof(BlockDataImportWithExistingProductionEnabled);
        public const string BlockDataImportWithRecentlyCompletedEnabled = InternalPrefix + nameof(BlockDataImportWithRecentlyCompletedEnabled);
        public const string RequirePreviousProductionHardDelete = InternalPrefix + nameof(RequirePreviousProductionHardDelete);
        public const string TfsMigratorVersion = Prefix + nameof(TfsMigratorVersion);              // Version used during Prepare/Validate
        public const string TfsMigratorImportVersion = Prefix + nameof(TfsMigratorImportVersion);  // Version used during Import
        public const string SkipValidationTfsMigratorVersion = InternalPrefix + nameof(SkipValidationTfsMigratorVersion);
        public const string SkipValidationSourceCollectionId = InternalPrefix + nameof(SkipValidationSourceCollectionId);
        public const string ServicesToImport = InternalPrefix + nameof(ServicesToImport);
        public const string PreviewServices = InternalPrefix + nameof(PreviewServices);
        public const string ServiceMapPrefix = InternalPrefix + "ServiceMap.";
        public const string DryRunAccountLifeSpan = InternalPrefix + nameof(DryRunAccountLifeSpan);
        public const string FailedImportLifeSpan = InternalPrefix + nameof(FailedImportLifeSpan);

        // Used to communicate the warning confirmation to the service
        public const string CollectionBlockedWarningConfirmed = Prefix + nameof(CollectionBlockedWarningConfirmed);
        public const string OrchestratingDataImportHostId = InternalPrefix + nameof(OrchestratingDataImportHostId);

        // Validation Properties
        public const string TfsMigratorBanner = InternalPrefix + nameof(TfsMigratorBanner);
        public const string NewTfsMigratorVersionMessageAlreadyShown = InternalPrefix + nameof(NewTfsMigratorVersionMessageAlreadyShown);
        public const string SkipValidationDataImportHistory = InternalPrefix + nameof(SkipValidationDataImportHistory);
        public const string SkipValidationBlockImportReason = InternalPrefix + nameof(SkipValidationBlockImportReason);
        public const string CollectionBlockedWarning = InternalPrefix + nameof(CollectionBlockedWarning);
        public const string BlockImportReason = InternalPrefix + nameof(BlockImportReason);
        public const string PublicImportsEnabled = InternalPrefix + nameof(PublicImportsEnabled);
        public const string MinTfsMigratorVersionPrefix = InternalPrefix + nameof(MinTfsMigratorVersionPrefix);
        public const string MinTfsMigratorImportVersionPrefix = InternalPrefix + nameof(MinTfsMigratorImportVersionPrefix);
        public const string LatestTfsMigratorVersionPrefix = InternalPrefix + nameof(LatestTfsMigratorVersionPrefix);
        public const string UnsupportedCollations = InternalPrefix + nameof(UnsupportedCollations);
        public const string BlockDataImportTenantLimit = InternalPrefix + nameof(BlockDataImportTenantLimit);                   //Default 5
        public const string DisableAutoFix = InternalPrefix + "DisableAutoFix";
        public const string DisabeWorkItemColorsAutoFix = InternalPrefix + nameof(DisabeWorkItemColorsAutoFix);
        public const string AllowCustomTeamField = InternalPrefix + "AllowCustomTeamField"; //Default is false
        public const string SupportedRegions = InternalPrefix + nameof(SupportedRegions);
        public const string DeleteActiveDryRunAccounts = InternalPrefix + nameof(DeleteActiveDryRunAccounts);
        public const string MinSqlPackageVersion = InternalPrefix + nameof(MinSqlPackageVersion);
        public const string AllowParallelImports = InternalPrefix + nameof(AllowParallelImports);
        public const string BlockLargeDacpacs = InternalPrefix + nameof(BlockLargeDacpacs);

        public const string ServicesThatUseParallelCopy = InternalPrefix + nameof(ServicesThatUseParallelCopy);

        // SPS Instance Allocation Selection Properties
        public const string SpsInstanceId = InternalPrefix + nameof(SpsInstanceId);         // When set this Guid is used, no other checks are done
        public const string UseStaticSpsInstance = InternalPrefix + nameof(UseStaticSpsInstance);
        public const string AllowedSpsInstanceRegionStatuses = InternalPrefix + nameof(AllowedSpsInstanceRegionStatuses);
        public const string SpsRegionCacheLimit = InternalPrefix + nameof(SpsRegionCacheLimit);  // TimeSpan
        public const string SkipOrganizationCatalogService = InternalPrefix + nameof(SkipOrganizationCatalogService);

        public const string AllowedSourceMilestones = ComputedPrefix + nameof(AllowedSourceMilestones);
        
        //Obsolete properties, these are being kept for release compatibility
        [Obsolete("Import Code are no longer supported"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string ImportCode = Prefix + "ImportCode";
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string CreateImportCodeInput = InternalPrefix + "CreateImportCodeInput";
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string HostMoveAccountJobId = InternalPrefix + "HostMoveAccountJobId";        
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string LegacyServicesToImport = InternalPrefix + "LegacyServicesToImport";
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string AdditionalMilestone = InternalPrefix + "AdditionalMilestone";
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string NeighborHostName = Prefix + "NeighborHostName";
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string AddSidsToImportFile = InternalPrefix + "AddSidsToImportFile";
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string StopWritingMappingFile = Prefix + "StopWritingMappingFile";
        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string ShouldImportFromSidsList = Prefix + "ShouldImportFromSidsList";
    }

    public static class DataImportPropertyDelimiter
    {
        public const char Underscore = '_';
    }

    public static class DataImportCiConstants
    {
        // Ci Data
        public const string Prefix = "DataImportCi.";
        public const string ImportId = Prefix + "ImportId";
        public const string DatabaseImportJobId = Prefix + "DatabaseImportJobId";
        public const string ApplicationHostId = Prefix + "ApplicationHost";
        public const string CollectionHostId = Prefix + "CollectionHost";
        public const string ImportResult = Prefix + "ImportResult";
        public const string FailureArea = Prefix + "FailureArea";
        public const string StartTime = Prefix + "StartTime";
        public const string FinishTime = Prefix + "FinishTime";
        public const string RunTime = Prefix + "RunTime";
        public const string NumberOfMappedUsers = Prefix + "NumberOfMappedUsers";
        public const string ValidationData = Prefix + "ValidationData";

        public const string RunType = Prefix + "RunType";
        public const string WitData = Prefix + "WorkitemTrackingData";
        public const string LicenseCount = Prefix + "LicenseCount";
        public const string RequestedFor = Prefix + "RequestedFor";
        public const string AADTenantName = Prefix + "AADTenantName";
        public const string UserErrorMessage = Prefix + "UserErrorMessage";

        [Obsolete("Import Code are no longer supported")]
        public const string ImportCode = Prefix + "ImportCode";
    }

    public static class DataImportResponseConstants
    {
        public const string Prefix = "DataImportResponse.";
        public const string ImportServiceInstances = Prefix + nameof(ImportServiceInstances);
        public const string DryRunAccountLifeSpan = Prefix + nameof(DryRunAccountLifeSpan);
    }

    public static class DataImportPackageConstants
    {
        //Source Properties
        public const string SourceLocation = "Location";

        [Obsolete("No longer used in Data Import pipeline"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string SourceIdentityMapping = "IdentityMapping";

        public const string SourceDacpac = "Dacpac";
        public const string SourceConnectionString = "ConnectionString";

        // Target properties
        public const string AccountOwner = "Owner";
        public const string AccountRegion = "Region";
        public const string AccountScaleUnit = "ScaleUnit";

        // Internal DataImport Options
        public const string SkipValidation = "SkipValidation";
        public const string SkipWITImport = "SkipWITImport";
        public const string SkipFileImport = "SkipFileImport";

        public const string SourceDacpacLocation = "SourceDacpacLocation";
        public const string HostToMovePostImport = "HostToMovePostImport";
        public const string NeighborHostName = "NeighborHostName";
        public const string TargetDatabaseDowngradeSize = "TargetDatabaseDowngradeSize";
        public const string TryMapADGroupsToAAD = "TryMapADGroupsToAAD";

        // Public properties
        public const string ImportType = "ImportType";
        public const string TfsMigratorImportVersion = "TfsMigratorImportVersion";  // Version used during Import

        // Validation Data
        public const string ImportPackage = "ImportPackage";
        public const string DatabaseTotalSize = "DatabaseTotalSize";
        public const string DatabaseBlobSize = "DatabaseBlobSize";
        public const string DatabaseTableSize = "DatabaseTableSize";
        public const string ActiveUserCount = "ActiveUserCount";
        public const string TfsVersion = "TfsVersion";
        public const string TfsMigratorVersion = "TfsMigratorVersion";
        public const string Collation = "Collation";
        public const string CommandExecutionTime = "CommandExecutionTime";
        public const string CommandExecutionCount = "CommandExecutionCount";
        public const string Force = "Force";
        public const string ServicesToInclude = nameof(ServicesToInclude);
        public const string ServicesToExclude = nameof(ServicesToExclude);
        public const string ValidationChecksum = "ValidationChecksum";
        public const string ValidationChecksumVersion = "ValidationChecksumVersion";

        [Obsolete("Import Code are no longer supported")]
        public const string ImportCode = "ImportCode";
    }

    [Obsolete("No longer used in Data Import pipeline"), EditorBrowsable(EditorBrowsableState.Never)]
    public static class DataImportFileSizeLimit
    {
        // 64MBs. Per discussion with Rogan and Octavio, a reasonable identity mapping file should be well below 50MBs
        public const Int64 IdentityMappingSizeLimitInBytes = 67108864;
    }

    public static class DataImportTestMilestones
    {
        /// <summary>
        /// Used to mark the oldest milestone for RestrictedMilestone tests
        /// </summary>
        public const string OldestServiceLevel = "Dev16.M131.11";  // These two a little bit of line.  2018 QU2 is oldest, but leaving it set this way to ensure we cover all milestones (2018.2, 2018.3[.0-.2], 2019.0)
        public const string OldestImportMilestone = SmallMostRecentImportMilestoneMinusTwo;

        /// <summary>
        /// Used for Stretch Tests
        /// </summary>
        public const string StretchServiceLevel = "Dev17.M143.6";
        public const string StretchMilestone = "smalldev17-m143-5-stretch";
        public const string OldStretchServiceLevel = "Dev17.M143.5";
        public const string OldStretchMilestone = "smalldev17-m143-4-stretch";
        
        public const string SmallMostRecentImportMilestone = "smalldev17-m143-5";
        public const string MostRecentImportMilestone = "dev17-m143-5";
        
        public const string SmallMostRecentImportMilestoneMinusOne = "smalldev17-m143-4";
        public const string MostRecentImportMilestoneMinusOne = "dev17-m143-4";

        public const string SmallMostRecentImportMilestoneMinusTwo = "smalldev16-m131-11";

        public const string TurkishCollation = "Dev16QU2Turkish";

        [Obsolete("This property has been deprecated"), EditorBrowsable(EditorBrowsableState.Never)]
        public const string MostRecent = "Obsolete";
    }

    public static class DataImportDelimiters
    {
        [Obsolete("DataImportDelimiters.BetweenServiceInstanceIds should not be used, instead use OverrideServiceInstanceIds")]
        public const char BetweenServiceInstanceIds = OverrideServiceInstanceIds;
        public const char BetweenIdentitiesToImport = ',';
        public const char OverrideServiceInstanceIds = ',';

        public const char ServiceInstanceTypeIds = ';';
        public static readonly string ServiceInstanceTypeIdsString = ServiceInstanceTypeIds.ToString();
    }

    public class DataImportEventTypes
    {
        public const string ImportStatusChangedEvent = "ms.vss-dataimport.dataimport-importstatuschanged-event";
    }

    public class DataImportSemaphore
    {
        /// <summary>
        /// Used as property names while locking cleanup for DataImport during Parallel Imports
        /// </summary>
        public const string RegistryBase = "DataImportSemaphore.RegistryBase";
        public const string LockKeyName = "DataImportSemaphore.LockKeyName";
    }
}
