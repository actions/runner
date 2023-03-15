using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GitHub.Services.Results.Contracts
{
    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class GetSignedStepSummaryURLRequest
    {
        [DataMember]
        public string WorkflowJobRunBackendId;
        [DataMember]
        public string WorkflowRunBackendId;
        [DataMember]
        public string StepBackendId;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class GetSignedStepSummaryURLResponse
    {
        [DataMember]
        public string SummaryUrl;
        [DataMember]
        public long SoftSizeLimit;
        [DataMember]
        public string BlobStorageType;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class StepSummaryMetadataCreate
    {
        [DataMember]
        public string StepBackendId;
        [DataMember]
        public string WorkflowRunBackendId;
        [DataMember]
        public string WorkflowJobRunBackendId;
        [DataMember]
        public long Size;
        [DataMember]
        public string UploadedAt;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class GetSignedJobLogsURLRequest
    {
        [DataMember]
        public string WorkflowJobRunBackendId;
        [DataMember]
        public string WorkflowRunBackendId;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class GetSignedJobLogsURLResponse
    {
        [DataMember]
        public string LogsUrl;
        [DataMember]
        public string BlobStorageType;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class GetSignedStepLogsURLRequest
    {
        [DataMember]
        public string WorkflowJobRunBackendId;
        [DataMember]
        public string WorkflowRunBackendId;
        [DataMember]
        public string StepBackendId;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class GetSignedStepLogsURLResponse
    {
        [DataMember]
        public string LogsUrl;
        [DataMember]
        public string BlobStorageType;
        [DataMember]
        public long SoftSizeLimit;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class JobLogsMetadataCreate
    {
        [DataMember]
        public string WorkflowRunBackendId;
        [DataMember]
        public string WorkflowJobRunBackendId;
        [DataMember]
        public string UploadedAt;
        [DataMember]
        public long LineCount;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class StepLogsMetadataCreate
    {
        [DataMember]
        public string WorkflowRunBackendId;
        [DataMember]
        public string WorkflowJobRunBackendId;
        [DataMember]
        public string StepBackendId;
        [DataMember]
        public string UploadedAt;
        [DataMember]
        public long LineCount;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class CreateMetadataResponse
    {
        [DataMember]
        public bool Ok;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class StepsUpdateRequest
    {
        [DataMember]
        public IEnumerable<Step> Steps;
        [DataMember]
        public long ChangeOrder;
        [DataMember]
        public string WorkflowJobRunBackendId;
        [DataMember]
        public string WorkflowRunBackendId;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class Step
    {
        [DataMember]
        public string ExternalId;
        [DataMember]
        public int Number;
        [DataMember]
        public string Name;
        [DataMember]
        public Status Status;
        [DataMember]
        public string StartedAt;
        [DataMember]
        public string CompletedAt;
    }

    public enum Status
    {
        StatusUnknown = 0,
        StatusInProgress = 3,
        StatusPending = 5,
        StatusCompleted = 6
    }

    public static class BlobStorageTypes
    {
        public static readonly string AzureBlobStorage = "BLOB_STORAGE_TYPE_AZURE";
        public static readonly string Unspecified = "BLOB_STORAGE_TYPE_UNSPECIFIED";
    }
}
