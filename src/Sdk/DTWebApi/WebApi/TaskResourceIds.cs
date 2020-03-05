using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public static class TaskResourceIds
    {
        public const String AreaId = "A85B8835-C1A1-4AAC-AE97-1C3D0BA72DBD";
        public const String AreaName = "distributedtask";

        public static readonly Guid Agents = new Guid("{E298EF32-5878-4CAB-993C-043836571F42}");
        public const String AgentsResource = "agents";

        public static readonly Guid AgentMessages = new Guid("{C3A054F6-7A8A-49C0-944E-3A8E5D7ADFD7}");
        public const String AgentMessagesResource = "messages";

        public static readonly Guid AgentSessions = new Guid("{134E239E-2DF3-4794-A6F6-24F1F19EC8DC}");
        public const String AgentSessionsResource = "sessions";

        public static readonly Guid AgentUpdates = new Guid("{8CC1B02B-AE49-4516-B5AD-4F9B29967C30}");
        public const String AgentUpdatesResource = "updates";

        public static readonly Guid UserCapabilities = new Guid("{30BA3ADA-FEDF-4DA8-BBB5-DACF2F82E176}");
        public const String UserCapabilitiesResource = "usercapabilities";

        public static readonly Guid AgentClouds = new Guid("{BFA72B3D-0FC6-43FB-932B-A7F6559F93B9}");
        public const String AgentCloudsResource = "agentclouds";

        public static readonly Guid AgentCloudRequests = new Guid("{20189BD7-5134-49C2-B8E9-F9E856EEA2B2}");
        public const String AgentCloudRequestsResource = "requests";

        public static readonly Guid AgentCloudRequestMessages = new Guid("{BD247656-4D13-49AF-80C1-1891BB057A93}");
        public const String AgentCloudRequestMessagesResource = "agentCloudRequestMessages";

        public static readonly Guid AgentCloudRequestJob = new Guid("{662C9827-FEED-40F0-AE63-B0B8E88A58B8}");
        public const String AgentCloudRequestJobResource = "agentCloudRequestJob";

        public static readonly Guid Packages = new Guid("{8FFCD551-079C-493A-9C02-54346299D144}");
        public const String PackagesResource = "packages";

        public static readonly Guid AgentDownload = new Guid("{314EA24F-8331-4AF1-9FB6-CFC73A4CB5A8}");
        public const String AgentDownloadResource = "downloads";

        public static readonly Guid Pools = new Guid("{A8C47E17-4D56-4A56-92BB-DE7EA7DC65BE}");
        public const String PoolsResource = "pools";

        public static readonly Guid AgentCloudTypes = new Guid("{5932E193-F376-469D-9C3E-E5588CE12CB5}");
        public const String AgentCloudTypesResource = "agentcloudtypes";

        public const String DeploymentPoolsResource = "deploymentPools";

        public static readonly Guid DeploymentPoolsSummary = new Guid("{6525D6C6-258F-40E0-A1A9-8A24A3957625}");
        public const String DeploymentPoolsSummaryResource = "deploymentPoolsSummary";

        public static readonly Guid PoolMaintenanceDefinitions = new Guid("{80572E16-58F0-4419-AC07-D19FDE32195C}");
        public const String PoolMaintenanceDefinitionsResource = "maintenancedefinitions";

        public static readonly Guid PoolMaintenanceJobs = new Guid("{15E7AB6E-ABCE-4601-A6D8-E111FE148F46}");
        public const String PoolMaintenanceJobsResource = "maintenancejobs";

        public static readonly Guid Queues = new Guid("900FA995-C559-4923-AAE7-F8424FE4FBEA");
        public const String QueuesResource = "queues";

        public static readonly Guid DeploymentGroupAccessToken = new Guid("3D197BA2-C3E9-4253-882F-0EE2440F8174");
        public const String DeploymentGroupAccessTokenResource = "deploymentgroupaccesstoken";

        public static readonly Guid DeploymentPoolAccessToken = new Guid("E077EE4A-399B-420B-841F-C43FBC058E0B");
        public const String DeploymentPoolAccessTokenResource = "deploymentpoolaccesstoken";

        public const string DeploymentGroupsMetricsLocationIdString = "281C6308-427A-49E1-B83A-DAC0F4862189";
        public static readonly Guid DeploymentGroupsMetrics = new Guid(DeploymentGroupsMetricsLocationIdString);
        public const String DeploymentGroupsMetricsResource = "deploymentgroupsmetrics";

        public static readonly Guid DeploymentGroups = new Guid("083C4D89-AB35-45AF-AA11-7CF66895C53E");
        public const String DeploymentGroupsResource = "deploymentgroups";

        public static readonly Guid DeploymentMachineGroups = new Guid("D4ADF50F-80C6-4AC8-9CA1-6E4E544286E9");
        public const String DeploymentMachineGroupsResource = "machinegroups";

        public const string DeploymentMachinesLocationIdString = "6F6D406F-CFE6-409C-9327-7009928077E7";
        public static readonly Guid DeploymentMachines = new Guid(DeploymentMachinesLocationIdString);
        public const string DeploymentMachineGroupMachinesLocationIdString = "966C3874-C347-4B18-A90C-D509116717FD";
        public static readonly Guid DeploymentMachineGroupMachines = new Guid(DeploymentMachineGroupMachinesLocationIdString);
        public const String DeploymentMachinesResource = "machines";

        public const string DeploymentTargetsLocationIdString = "2F0AA599-C121-4256-A5FD-BA370E0AE7B6";
        public static readonly Guid DeploymentTargets = new Guid(DeploymentTargetsLocationIdString);
        public const String DeploymentTargetsResource = "targets";

        public static readonly Guid DeploymentMachineGroupAccessToken = new Guid("F8C7C0DE-AC0D-469B-9CB1-C21F72D67693");
        public const String DeploymentMachineGroupAccessTokenResource = "machinegroupaccesstoken";

        public static readonly Guid PoolRolesCompat = new Guid("{9E627AF6-3635-4DDF-A275-DCA904802338}");
        public const String PoolRolesCompatResource = "roles";

        public static readonly Guid QueueRoles = new Guid("{B0C6D64D-C9FA-4946-B8DE-77DE623EE585}");
        public const String QueueRolesResource = "queueroles";

        public static readonly Guid PoolRoles = new Guid("{381DD2BB-35CF-4103-AE8C-3C815B25763C}");
        public const string PoolRolesResource = "poolroles";

        public static readonly Guid PoolMetadata = new Guid("{0D62F887-9F53-48B9-9161-4C35D5735B0F}");
        public const string PoolMetadataResource = "poolmetadata";

        public static readonly Guid JobRequestsDeprecated = new Guid("{FC825784-C92A-4299-9221-998A02D1B54F}");
        public const String JobRequestsDeprecatedResource = "jobrequests";

        public static readonly Guid AgentRequests = new Guid("{F5F81FFB-F396-498D-85B1-5ADA145E648A}");
        public const String AgentRequestsResource = "agentrequests";

        public static readonly Guid DeploymentMachineJobRequests = new Guid("{A3540E5B-F0DC-4668-963B-B752459BE545}");
        public const String DeploymentMachineJobRequestsResource = "deploymentmachinejobrequests";

        public static readonly Guid DeploymentTargetJobRequests = new Guid("{2FAC0BE3-8C8F-4473-AB93-C1389B08A2C9}");
        public const String DeploymentTargetJobRequestsResource = "deploymentTargetJobRequests";

        public static readonly Guid DeploymentMachineMessages = new Guid("{91006AC4-0F68-4D82-A2BC-540676BD73CE}");
        public const String DeploymentMachineMessagesResource = "deploymentmachinemessages";

        public static readonly Guid DeploymentTargetMessages = new Guid("{1C1A817F-F23D-41C6-BF8D-14B638F64152}");
        public const String DeploymentTargetMessagesResource = "deploymentTargetMessages";

        public static readonly Guid Tasks = new Guid("{60AAC929-F0CD-4BC8-9CE4-6B30E8F1B1BD}");
        public const String TasksResource = "tasks";

        public static readonly Guid TaskEndpoint = new Guid("{F223B809-8C33-4B7D-B53F-07232569B5D6}");
        public const String TaskEndpointResource = "endpoint";

        public static readonly Guid TaskIcons = new Guid("{63463108-174D-49D4-B8CB-235EEA42A5E1}");
        public const String TaskIconsResource = "icon";

        public static readonly Guid Logs = new Guid("{46F5667D-263A-4684-91B1-DFF7FDCF64E2}");
        public static readonly Guid Logs_Compat = new Guid("{15344176-9E77-4CF4-A7C3-8BC4D0A3C4EB}");
        public const String LogsResource = "logs";

        public static readonly Guid Plans = new Guid("{5CECD946-D704-471E-A45F-3B4064FCFABA}");
        public static readonly Guid Plans_Compat = new Guid("{F8D10759-6E90-48BC-96B0-D19440116797}");
        public const String PlansResource = "plans";

        public static readonly Guid JobInstances = new Guid("{0A1EFD25-ABDA-43BD-9629-6C7BDD2E0D60}");
        public const String JobInstancesResource = "jobinstances";

        public static readonly Guid PlanEvents = new Guid("{557624AF-B29E-4C20-8AB0-0399D2204F3F}");
        public static readonly Guid PlanEvents_Compat = new Guid("{DFED02FB-DEEE-4039-A04D-AA21D0241995}");
        public const String PlanEventsResource = "events";

        public const String PlanAttachmentsLocationIdString = "EB55E5D6-2F30-4295-B5ED-38DA50B1FC52";
        public static readonly Guid PlanAttachments = new Guid(PlanAttachmentsLocationIdString);
        public const String AttachmentsLocationIdString = "7898F959-9CDF-4096-B29E-7F293031629E";
        public static readonly Guid Attachments = new Guid(AttachmentsLocationIdString);
        public const String AttachmentsResource = "attachments";

        public static readonly Guid Timelines = new Guid("{83597576-CC2C-453C-BEA6-2882AE6A1653}");
        public static readonly Guid Timelines_Compat = new Guid("{FFE38397-3A9D-4CA6-B06D-49303F287BA5}");
        public const String TimelinesResource = "timelines";

        public static readonly Guid TimelineRecords = new Guid("{8893BC5B-35B2-4BE7-83CB-99E683551DB4}");
        public static readonly Guid TimelineRecords_Compat = new Guid("{50170D5D-F122-492F-9816-E2EF9F8D1756}");
        public const String TimelineRecordsResource = "records";

        public static readonly Guid TimelineRecordFeeds = new Guid("{858983E4-19BD-4C5E-864C-507B59B58B12}");
        public static readonly Guid TimelineRecordFeeds_Compat = new Guid("{9AE056F6-D4E4-4D0C-BD26-AEE2A22F01F2}");
        public const String TimelineRecordFeedsResource = "feed";

        public static readonly Guid ServiceEndpoints = new Guid("CA373C13-FEC3-4B30-9525-35A117731384");
        public const String ServiceEndpoints2LocationIdString = "DCA61D2F-3444-410A-B5EC-DB2FC4EFB4C5";
        public static readonly Guid ServiceEndpoints2 = new Guid(ServiceEndpoints2LocationIdString);
        public const String ServiceEndpointsResource = "serviceendpoints";

        public static readonly Guid ServiceEndpointTypes = new Guid("7c74af83-8605-45c1-a30b-7a05d5d7f8c1");
        public const String ServiceEndpointTypesResource = "serviceendpointtypes";

        public static readonly Guid ServiceEndpointProxy = new Guid("e3a44534-7b94-4add-a053-8af449589c62");
        public const String ServiceEndpointProxy2LocationIdString = "F956A7DE-D766-43AF-81B1-E9E349245634";
        public static readonly Guid ServiceEndpointProxy2 = new Guid(ServiceEndpointProxy2LocationIdString);
        public const String ServiceEndpointProxyResource = "serviceendpointproxy";

        public static readonly Guid AzureSubscriptions = new Guid("BCD6189C-0303-471F-A8E1-ACB22B74D700");
        public const String AzureRmSubscriptionsResource = "azurermsubscriptions";

        public static readonly Guid AzureManagementGroups = new Guid("39FE3BF2-7EE0-4198-A469-4A29929AFA9C");
        public const String AzureRmManagementGroupsResource = "azurermmanagementgroups";

        public static readonly Guid TaskGroups = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
        public const string TaskGroupsResource = "taskgroups";

        public static readonly Guid TaskGroupHistory = new Guid("100cc92a-b255-47fa-9ab3-e44a2985a3ac");
        public const string TaskGroupHistoryResource = "revisions";

        public static readonly Guid ExtensionEvents = new Guid("{96c86d26-36fb-4649-9215-36e03a8bbc7d}");
        public const String ExtensionEventsResource = "extensionevents";
        public const String ExtensionPreInstallResource = "preinstall";

        public static readonly Guid TaskHubLicense = new Guid("{F9F0F436-B8A1-4475-9041-1CCDBF8F0128}");
        public const String TaskHubLicenseResource = "hublicense";

        public const String ResourceLimitsLocationIdString = "1F1F0557-C445-42A6-B4A0-0DF605A3A0F8";
        public static readonly Guid ResourceLimits = new Guid(ResourceLimitsLocationIdString);
        public const String ResourceLimitsResource = "resourcelimits";

        public const String ResourceUsageLocationIdString = "EAE1D376-A8B1-4475-9041-1DFDBE8F0143";
        public static readonly Guid ResourceUsage = new Guid(ResourceUsageLocationIdString);
        public const String ResourceUsageResource = "resourceusage";

        public static readonly Guid VariableGroups = new Guid("F5B09DD5-9D54-45A1-8B5A-1C8287D634CC");
        public const String VariableGroupsResource = "variablegroups";

        public static readonly Guid VariableGroupsShare = new Guid("74455598-DEF7-499A-B7A3-A41D1C8225F8");
        public const String VariableGroupsShareResource = "variablegroupshare";

        public static readonly Guid SecureFiles = new Guid("ADCFD8BC-B184-43BA-BD84-7C8C6A2FF421");
        public const String SecureFilesResource = "securefiles";

        public const String PlanGroupsQueueLocationIdString = "0DD73091-3E36-4F43-B443-1B76DD426D84";
        public static readonly Guid PlanGroupsQueue = new Guid(PlanGroupsQueueLocationIdString);
        public const String QueuedPlanGroupLocationIdString = "65FD0708-BC1E-447B-A731-0587C5464E5B";
        public static readonly Guid QueuedPlanGroup = new Guid(QueuedPlanGroupLocationIdString);
        public const String PlanGroupsQueueResource = "plangroupsqueue";

        public const String PlanGroupsQueueMetricsLocationIdString = "038FD4D5-CDA7-44CA-92C0-935843FEE1A7";
        public static readonly Guid PlanGroupsQueueMetrics = new Guid(PlanGroupsQueueMetricsLocationIdString);
        public const String PlanGroupsQueueMetricsResource = "metrics";

        public static readonly Guid VstsAadOAuth = new Guid("9C63205E-3A0F-42A0-AD88-095200F13607");
        public const string VstsAadOAuthResource = "vstsaadoauth";

        public static readonly Guid InputValidation = new Guid("58475b1e-adaf-4155-9bc1-e04bf1fff4c2");
        public const string InputValidationResource = "inputvalidation";

        public const string GetServiceEndpointExecutionHistoryLocationIdString = "3AD71E20-7586-45F9-A6C8-0342E00835AC";
        public static readonly Guid GetServiceEndpointExecutionHistory = new Guid(GetServiceEndpointExecutionHistoryLocationIdString);
        public const string PostServiceEndpointExecutionHistoryLocationIdString = "11A45C69-2CCE-4ADE-A361-C9F5A37239EE";
        public static readonly Guid PostServiceEndpointExecutionHistory = new Guid(PostServiceEndpointExecutionHistoryLocationIdString);

        public const string ServiceEndpointExecutionHistoryResource = "executionhistory";

        public static readonly Guid Environments = new Guid("8572B1FC-2482-47FA-8F74-7E3ED53EE54B");
        public const String EnvironmentsResource = "environments";

        public static readonly Guid EnvironmentDeploymentExecutionHistory = new Guid("51bb5d21-4305-4ea6-9dbb-b7488af73334");
        public const String EnvironmentDeploymentExecutionHistoryResource = "environmentdeploymentRecords";

        public const String KubernetesResourcesLocationIdString = "73FBA52F-15AB-42B3-A538-CE67A9223A04";
        public static readonly Guid KubernetesResourcesLocationId = new Guid(KubernetesResourcesLocationIdString);
        public const String KubernetesResourcesResource = "kubernetes";

        public const String VirtualMachineGroupsLocationIdString = "9e597901-4af7-4cc3-8d92-47d54db8ebfb";
        public static readonly Guid VirtualMachineGroupsLocationId = new Guid(VirtualMachineGroupsLocationIdString);
        public const String VirtualMachineGroupsResource = "virtualmachinegroups";

        public const String VirtualMachinesLocationIdString = "48700676-2BA5-4282-8EC8-083280D169C7";
        public static readonly Guid VirtualMachinesLocationId = new Guid(VirtualMachinesLocationIdString);
        public const String VirtualMachinesResource = "virtualmachines";

        public static readonly Guid YamlSchema = new Guid("{1F9990B9-1DBA-441F-9C2E-6485888C42B6}");
        public const String YamlSchemaResource = "yamlschema";

        public const String CheckpointResourcesLocationIdString = "57835CC4-6FF0-4D62-8C27-4541BA97A094";
        public static readonly Guid CheckpointResourcesLocationId = new Guid(CheckpointResourcesLocationIdString);
        public const String CheckpointResourcesResource = "references";

        public static readonly Guid RunnerAuthUrl = new Guid("{A82A119C-1E46-44B6-8D75-C82A79CF975B}");
        public const string RunnerAuthUrlResource = "authurl";

    }
}
