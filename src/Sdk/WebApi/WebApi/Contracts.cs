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
        public string BlobUrl;
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
    public class CreateStepSummaryMetadataResponse
    {
        [DataMember]
        public bool Ok;
    }

    [DataContract]
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class StepLogsMetadataCreate
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
    public class CreateStepLogsMetadataResponse
    {
        [DataMember]
        public bool Ok;
    }

    public static class BlobStorageTypes
    {
        public static readonly string AzureBlobStorage = "BLOB_STORAGE_TYPE_AZURE";
        public static readonly string Unspecified = "BLOB_STORAGE_TYPE_UNSPECIFIED";
    }
}
