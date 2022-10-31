using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Results.Contracts;
using System.Net.Http.Formatting;
using Sdk.WebApi.WebApi;

namespace GitHub.Services.Results.Client
{
    public class ResultsHttpClient : RawHttpClientBase
    {
        public ResultsHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            string token,
            bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            m_token = token;
            m_resultsServiceUrl = baseUrl;
            m_formatter = new JsonMediaTypeFormatter();
        }

        public async Task<GetSignedStepSummaryURLResponse> GetStepSummaryUploadUrlAsync(string jobId, string planId, string stepId, CancellationToken cancellationToken)
        {
            var request = new GetSignedStepSummaryURLRequest()
            {
                workflow_job_run_backend_id = jobId,
                workflow_run_backend_id = planId,
                step_backend_id = stepId
            };

            var stepSummaryUploadRequest = m_resultsServiceUrl+"twirp/results.services.receiver.Receiver/GetStepSummarySignedBlobURL";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepSummaryUploadRequest);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            HttpContent content = new ObjectContent<GetSignedStepSummaryURLRequest>(request, m_formatter);
            requestMessage.Content = content;

            var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken);
            return await ReadJsonContentAsync<GetSignedStepSummaryURLResponse>(response, cancellationToken);
        }

        public async Task StepSummaryUploadCompleteAsync(string jobId, string planId, string stepId, long size, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            var request = new StepSummaryMetadataCreate()
            {
                workflow_job_run_backend_id = jobId,
                workflow_run_backend_id = planId,
                step_backend_id = stepId,
                size = size,
                uploaded_at = timestamp
            };

            var stepSummaryUploadCompleteRequest = m_resultsServiceUrl+"twirp/results.services.receiver.Receiver/CreateStepSummaryMetadata";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepSummaryUploadCompleteRequest);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            HttpContent content = new ObjectContent<StepSummaryMetadataCreate>(request, m_formatter);
            requestMessage.Content = content;

            var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken);
            var jsonResponse = await ReadJsonContentAsync<CreateStepSummaryMetadataResponse>(response, cancellationToken);
            if (!jsonResponse.ok)
            {
                throw new Exception($"Failed to mark step summary upload as complete, status code: {response.StatusCode}, ok: {jsonResponse.ok}, jobId: {jobId}, planId: {planId}, stepId: {stepId}, size: {size}, timestamp: {timestamp}");
            }
        }

        public static async Task<HttpResponseMessage> UploadFileAsync(string url, string blobStorageType, FileStream file, CancellationToken cancellationToken) {
            // Upload the file to the url
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StreamContent(file)
            };

            if (blobStorageType == BlobStorageTypes.AzureBlobStorage) {
                request.Content.Headers.Add("x-ms-blob-type", "BlockBlob");
            }

            using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
                if (!response.IsSuccessStatusCode) {
                    throw new Exception($"Failed to upload file, status code: {response.StatusCode}, reason: {response.ReasonPhrase}, blobStorageType: {blobStorageType}");
                }
                return response;
            }
        }

        // Handle file upload for step summary
        public async Task UploadStepSummaryAsync(string jobId, string planId, string stepId, string file, CancellationToken cancellationToken)
        {
            try {
                // Get the upload url
                var uploadUrlResponse = await GetStepSummaryUploadUrlAsync(jobId, planId, stepId, cancellationToken);

                // Do we want to throw an exception here or should we just be uploading/truncating the data
                var fileSize = new FileInfo(file).Length;
                if (fileSize > uploadUrlResponse.soft_size_limit)
                {
                    throw new Exception($"File size is larger than the upload url allows, file size: {fileSize}, upload url size: {uploadUrlResponse.soft_size_limit}, blobType: {uploadUrlResponse.blob_storage_type}");
                }

                // Upload the file
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    var response = await UploadFileAsync(uploadUrlResponse.summary_url, uploadUrlResponse.blob_storage_type, fileStream, cancellationToken);
                }

                // Send step summary upload complete message
                await StepSummaryUploadCompleteAsync(jobId, planId, stepId, fileSize, cancellationToken);
            }
            catch (Exception ex) {
                throw new Exception("Failed to upload step summary to results", ex);
            }
        }

        private MediaTypeFormatter m_formatter;
        private Uri m_resultsServiceUrl;
        private string m_token;
    }
}
