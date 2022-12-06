namespace GitHub.Services.Results.Contracts
{
    public class GetSignedStepSummaryURLRequest
    {
        public string workflow_job_run_backend_id;
        public string workflow_run_backend_id;
        public string step_backend_id;
    }


    public class GetSignedStepSummaryURLResponse
    {
        public string summary_url;
        public long soft_size_limit;
        public string blob_storage_type;
    }

    public class StepSummaryMetadataCreate
    {
        public string step_backend_id;
        public string workflow_run_backend_id;
        public string workflow_job_run_backend_id;
        public long size;
        public string uploaded_at;
    }

    public class CreateStepSummaryMetadataResponse
    {
        public bool ok;
    }

    public static class BlobStorageTypes {
        public static readonly string AzureBlobStorage = "BLOB_STORAGE_TYPE_AZURE";
        public static readonly string Unspecified = "BLOB_STORAGE_TYPE_UNSPECIFIED";
    }
}
