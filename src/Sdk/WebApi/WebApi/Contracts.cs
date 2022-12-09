using System.Runtime.Serialization;

namespace GitHub.Services.Results.Contracts
{
    [DataContract]
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
    public class CreateStepSummaryMetadataResponse
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
