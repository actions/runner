using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants(alternateName: "Build2ResourceIds")]
    public static class BuildResourceIds
    {
        // keep these sorted

        public const String AreaId = "5D6898BB-45EC-463F-95F9-54D49C71752E";
        public const String AreaName = "build";

        public static readonly Guid Artifacts = new Guid("{1DB06C96-014E-44E1-AC91-90B2D4B3E984}");
        public const String ArtifactsResource = "artifacts";

        public const String AttachmentLocation = "AF5122D3-3438-485E-A25A-2DBBFDE84EE6";
        public static readonly Guid Attachment = new Guid(AttachmentLocation);

        public const String AttachmentsLocation = "F2192269-89FA-4F94-BAF6-8FB128C55159";
        public static readonly Guid Attachments = new Guid(AttachmentsLocation);
        public const String AttachmentsResource = "attachments";

        public static readonly Guid BuildBadge = new Guid("21b3b9ce-fad5-4567-9ad0-80679794e003");
        public const String BuildBadgeResource = "buildbadge";

        public const String BuildChangesLocationId = "54572C7B-BBD3-45D4-80DC-28BE08941620";
        public static readonly Guid BuildChangesBetweenBuilds = new Guid("{F10F0EA5-18A1-43EC-A8FB-2042C7BE9B43}");
        public static readonly Guid BuildChanges = new Guid(BuildChangesLocationId);
        public const String BuildChangesResource = "changes";

        public static readonly Guid BuildDefinitionBadge = new Guid("de6a4df8-22cd-44ee-af2d-39f6aa7a4261");
        public const String BuildDefinitionBadgeResource = "badge";

        public static readonly Guid BuildDeployments = new Guid("{F275BE9A-556A-4EE9-B72F-F9C8370CCAEE}");
        public const String BuildDeploymentsResource = "deployments";

        public static readonly Guid BuildLogs = new Guid("{35A80DAF-7F30-45FC-86E8-6B813D9C90DF}");
        public const String BuildLogsResource = "logs";

        public const String BuildPropertiesLocationString = "0A6312E9-0627-49B7-8083-7D74A64849C9";
        public static readonly Guid BuildProperties = new Guid(BuildPropertiesLocationString);

        public static readonly Guid BuildReport = new Guid("{45BCAA88-67E1-4042-A035-56D3B4A7D44C}");
        public const String BuildReportResource = "report";

        public static readonly Guid Builds = new Guid("{0CD358E1-9217-4D94-8269-1C1EE6F93DCF}");
        public const String BuildsResource = "builds";

        public const String BuildTagsLocationIdString = "6E6114B2-8161-44C8-8F6C-C5505782427F";
        public static readonly Guid BuildTags = new Guid(BuildTagsLocationIdString);
        public const String BuildTagsResource = "tags";

        public const String BuildWorkItemsLocationId = "5A21F5D2-5642-47E4-A0BD-1356E6731BEE";
        public static readonly Guid BuildWorkItemsBetweenBuilds = new Guid("{52BA8915-5518-42E3-A4BB-B0182D159E2D}");
        public static readonly Guid BuildWorkItems = new Guid(BuildWorkItemsLocationId);
        public const String BuildWorkItemsResource = "workitems";

        public const String ControllersLocationString = "{FCAC1932-2EE1-437F-9B6F-7F696BE858F6}";
        public static readonly Guid Controllers = new Guid(ControllersLocationString);
        public const String ControllersResource = "Controllers";

        public const String DefinitionMetricsLocationString = "D973B939-0CE0-4FEC-91D8-DA3940FA1827";
        public static readonly Guid DefinitionMetrics = new Guid(DefinitionMetricsLocationString);
        public const String DefinitionMetricsResource = "metrics";

        public const String DefinitionPropertiesLocationString = "D9826AD7-2A68-46A9-A6E9-677698777895";
        public static readonly Guid DefinitionProperties = new Guid(DefinitionPropertiesLocationString);

        public static readonly Guid DefinitionResources = new Guid("ea623316-1967-45eb-89ab-e9e6110cf2d6");
        public const String DefinitionResourcesResource = "resources";

        public static readonly Guid DefinitionRevisions = new Guid("{7C116775-52E5-453E-8C5D-914D9762D8C4}");
        public const String DefinitionRevisionsResource = "revisions";

        public static readonly Guid Definitions = new Guid("{DBEAF647-6167-421A-BDA9-C9327B25E2E6}");
        public const String DefinitionsResource = "definitions";

        public const String DefinitionTagsLocationIdString = "CB894432-134A-4D31-A839-83BECEAACE4B";
        public static readonly Guid DefinitionTags = new Guid(DefinitionTagsLocationIdString);

        public static readonly Guid Folders = new Guid("{A906531B-D2DA-4F55-BDA7-F3E676CC50D9}");
        public const String FoldersResource = "folders";

        // information nodes for XAML builds
        public static readonly Guid InformationNodes = new Guid("9F094D42-B41C-4920-95AA-597581A79821");

        public static readonly Guid InputValuesQuery = new Guid("{2182A7F0-B363-4B2D-B89E-ED0A0B721E95}");
        public const String InputValuesQueryResource = "InputValuesQuery";

        public static readonly Guid LatestBuildLocationId = new Guid("54481611-01F4-47F3-998F-160DA0F0C229");
        public const String LatestBuildResource = "latest";

        public static readonly Guid Metrics = new Guid("104AD424-B758-4699-97B7-7E7DA427F9C2");
        public const String MetricsResource = "Metrics";

        public static readonly Guid Options = new Guid("{591CB5A4-2D46-4F3A-A697-5CD42B6BD332}");
        public const String OptionsResource = "options";

        public const String ProjectMetricsLocationString = "7433FAE7-A6BC-41DC-A6E2-EEF9005CE41A";
        public static readonly Guid ProjectMetrics = new Guid(ProjectMetricsLocationString);

        public static readonly Guid ProjectAuthorizedResources = new Guid("398c85bc-81aa-4822-947c-a194a05f0fef");
        public const String ProjectAuthorizedResourcesResource = "authorizedresources";

        public const String PropertiesResource = "properties";

        public static readonly Guid Queues = new Guid("{09F2A4B8-08C9-4991-85C3-D698937568BE}");
        public const String QueuesResource = "queues";

        public static readonly Guid Settings = new Guid("{AA8C1C9C-EF8B-474A-B8C4-785C7B191D0D}");
        public const String SettingsResource = "settings";

        public const String SourceProviderBranchesResource = "branches";
        public const String SourceProviderBranchesLocationIdString = "E05D4403-9B81-4244-8763-20FDE28D1976";
        public static readonly Guid SourceProviderBranchesLocationId = new Guid(SourceProviderBranchesLocationIdString);

        public const String SourceProviderFileContentsResource = "fileContents";
        public const String SourceProviderFileContentsLocationIdString = "29D12225-B1D9-425F-B668-6C594A981313";
        public static readonly Guid SourceProviderFileContentsLocationId = new Guid(SourceProviderFileContentsLocationIdString);

        public const String SourceProviderPathContentsResource = "pathContents";
        public const String SourceProviderPathContentsLocationIdString = "7944D6FB-DF01-4709-920A-7A189AA34037";
        public static readonly Guid SourceProviderPathContentsLocationId = new Guid(SourceProviderPathContentsLocationIdString);

        public const String SourceProviderPullRequestsResource = "pullRequests";
        public const String SourceProviderPullRequestsLocationIdString = "D8763EC7-9FF0-4FB4-B2B2-9D757906FF14";
        public static readonly Guid SourceProviderPullRequestsLocationId = new Guid(SourceProviderPullRequestsLocationIdString);

        public const String SourceProviderRepositoriesResource = "repositories";
        public const String SourceProviderRepositoriesLocationIdString = "D44D1680-F978-4834-9B93-8C6E132329C9";
        public static readonly Guid SourceProviderRepositoriesLocationId = new Guid(SourceProviderRepositoriesLocationIdString);

        public const String SourceProviderRestoreWebhooksLocationIdString = "793BCEB8-9736-4030-BD2F-FB3CE6D6B478";
        public static readonly Guid SourceProviderRestoreWebhooksLocationId = new Guid(SourceProviderRestoreWebhooksLocationIdString);

        public const String SourceProvidersResource = "sourceProviders";
        public const String SourceProvidersLocationIdString = "3CE81729-954F-423D-A581-9FEA01D25186";
        public static readonly Guid SourceProviders = new Guid(SourceProvidersLocationIdString);

        public const String SourceProviderWebhooksResource = "webhooks";
        public const String SourceProviderWebhooksLocationIdString = "8F20FF82-9498-4812-9F6E-9C01BDC50E99";
        public static readonly Guid SourceProviderWebhooksLocationId = new Guid(SourceProviderWebhooksLocationIdString);

        public const String SourcesLocationId = "56EFDCDC-CF90-4028-9D2F-D41000682202";
        public static readonly Guid Sources = new Guid(SourcesLocationId);
        public const String SourcesResource = "sources";

        public const String StatusBadgeLocationIdString = "07ACFDCE-4757-4439-B422-DDD13A2FCC10";
        public static readonly Guid StatusBadgeLocationId = new Guid(StatusBadgeLocationIdString);
        public const String StatusBadgeResource = "status";

        public const String TagsLocationIdString = "D84AC5C6-EDC7-43D5-ADC9-1B34BE5DEA09";
        public static readonly Guid Tags = new Guid(TagsLocationIdString);

        public static readonly Guid Templates = new Guid("{E884571E-7F92-4D6A-9274-3F5649900835}");
        public const String TemplatesResource = "templates";

        public static readonly Guid Timelines = new Guid("8baac422-4c6e-4de5-8532-db96d92acffa");
        public const String TimelinesResource = "Timeline";

        public static readonly Guid Usage = new Guid("3813d06c-9e36-4ea1-aac3-61a485d60e3d");
        public const String UsageResource = "ResourceUsage";
    }
}
