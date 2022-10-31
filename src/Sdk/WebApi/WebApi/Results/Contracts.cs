
namespace GitHub.Services.Results.Contracts
{
    public class StepSummaryUploadUrlRequest
    {
        public string WorkflowJobRunBackendId;
        public string WorkflowRunBackendId;
        public string StepBackendId;
    }

    public class StepSummaryUploadUrlResponse
    {
        public string SummaryUrl;
        public long Size;
    }

    public class StepSummaryUploadCompleteRequest
    {
        public string WorkflowJobRunBackendId;
        public string WorkflowRunBackendId;
        public string StepBackendId;
        public long Size;
        public string UploadedAt;
    }

    public class StepSummaryUploadCompleteResponse
    {
        public bool Ok;
    }
}
