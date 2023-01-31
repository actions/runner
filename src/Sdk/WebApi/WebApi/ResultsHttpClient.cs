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

        public async Task<GetSignedStepSummaryURLResponse> GetStepSummaryUploadUrlAsync(string planId, string jobId, string stepId, CancellationToken cancellationToken)
        {
            var request = new GetSignedStepSummaryURLRequest()
            {
                WorkflowJobRunBackendId= jobId,
                WorkflowRunBackendId= planId,
                StepBackendId= stepId
            };

            var stepSummaryUploadRequest = new Uri(m_resultsServiceUrl, "twirp/results.services.receiver.Receiver/GetStepSummarySignedBlobURL");

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepSummaryUploadRequest))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<GetSignedStepSummaryURLRequest>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        return await ReadJsonContentAsync<GetSignedStepSummaryURLResponse>(response, cancellationToken);
                    }
                }
            }
        }

        public async Task<GetSignedStepLogsURLResponse> GetStepLogUploadUrlAsync(string planId, string jobId, string stepId, CancellationToken cancellationToken)
        {
            var request = new GetSignedStepLogsURLRequest()
            {
                WorkflowJobRunBackendId= jobId,
                WorkflowRunBackendId= planId,
                StepBackendId= stepId
            };

            var stepLogsUploadRequest = new Uri(m_resultsServiceUrl, "twirp/results.services.receiver.Receiver/GetStepStepLogsSignedBlobURL");

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepLogsUploadRequest))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<GetSignedStepLogsURLRequest>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        return await ReadJsonContentAsync<GetSignedStepLogsURLResponse>(response, cancellationToken);
                    }
                }
            }
        }

        private async Task StepSummaryUploadCompleteAsync(string planId, string jobId, string stepId, long size, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            var request = new StepSummaryMetadataCreate()
            {
                WorkflowJobRunBackendId= jobId,
                WorkflowRunBackendId= planId,
                StepBackendId = stepId,
                Size = size,
                UploadedAt = timestamp
            };

            var stepSummaryUploadCompleteRequest = new Uri(m_resultsServiceUrl, "twirp/results.services.receiver.Receiver/CreateStepSummaryMetadata");

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepSummaryUploadCompleteRequest))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<StepSummaryMetadataCreate>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        var jsonResponse = await ReadJsonContentAsync<CreateStepSummaryMetadataResponse>(response, cancellationToken);
                        if (!jsonResponse.Ok)
                        {
                            throw new Exception($"Failed to mark step summary upload as complete, status code: {response.StatusCode}, ok: {jsonResponse.Ok}, size: {size}, timestamp: {timestamp}");
                        }
                    }
                }
            }
        }

        private async Task StepLogUploadCompleteAsync(string planId, string jobId, string stepId, long size, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            var request = new StepLogsMetadataCreate()
            {
                WorkflowJobRunBackendId= jobId,
                WorkflowRunBackendId= planId,
                StepBackendId = stepId,
                Size = size,
                UploadedAt = timestamp
            };

            var stepLogsUploadCompleteRequest = new Uri(m_resultsServiceUrl, "twirp/results.services.receiver.Receiver/CreateStepLogsMetadata");

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepLogsUploadCompleteRequest))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<StepLogsMetadataCreate>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        var jsonResponse = await ReadJsonContentAsync<CreateStepSummaryMetadataResponse>(response, cancellationToken);
                        if (!jsonResponse.Ok)
                        {
                            throw new Exception($"Failed to mark step log upload as complete, status code: {response.StatusCode}, ok: {jsonResponse.Ok}, size: {size}, timestamp: {timestamp}");
                        }
                    }
                }
            }
        }

        private async Task<HttpResponseMessage> UploadFileAsync(string url, string blobStorageType, FileStream file, CancellationToken cancellationToken)
        {
            // Upload the file to the url
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StreamContent(file)
            };

            if (blobStorageType == BlobStorageTypes.AzureBlobStorage)
            {
                request.Content.Headers.Add("x-ms-blob-type", "BlockBlob");
            }

            using (var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, userState: null, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to upload file, status code: {response.StatusCode}, reason: {response.ReasonPhrase}");
                }
                return response;
            }
        }

        private async Task<HttpResponseMessage> CreateAppendFileAsync(string url, string blobStorageType, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent("")
            };
            if (blobStorageType == BlobStorageTypes.AzureBlobStorage)
            {
                request.Content.Headers.Add("x-ms-blob-type", "AppendBlob");
                request.Content.Headers.Add("Content-Length", "0");
            }

            using (var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, userState: null, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to create append file, status code: {response.StatusCode}, reason: {response.ReasonPhrase}");
                }
                return response;
            }
        }
        
        private async Task<HttpResponseMessage> UploadAppendFileAsync(string url, string blobStorageType, FileStream file, bool finalize, long fileSize, CancellationToken cancellationToken)
        {
            var comp = finalize ? "&comp=appendblock&seal=true" : "&comp=appendblock";
            // Upload the file to the url
            var request = new HttpRequestMessage(HttpMethod.Put, url + comp)
            {
                Content = new StreamContent(file)
            };

            if (blobStorageType == BlobStorageTypes.AzureBlobStorage)
            {
                // request.Content.Headers.Add("x-ms-blob-type", "AppendBlock");
                request.Content.Headers.Add("Content-Length", fileSize.ToString());
                request.Content.Headers.Add("x-ms-blob-sealed", finalize.ToString());
            }

            using (var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, userState: null, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to upload append file, status code: {response.StatusCode}, reason: {response.ReasonPhrase}, object: {response}, fileSize: {fileSize}");
                }
                return response;
            }
        }

        // Handle file upload for step summary
        public async Task UploadStepSummaryAsync(string planId, string jobId, string stepId, string file, CancellationToken cancellationToken)
        {
            // Get the upload url
            var uploadUrlResponse = await GetStepSummaryUploadUrlAsync(planId, jobId, stepId, cancellationToken);
            if (uploadUrlResponse == null)
            {
                throw new Exception("Failed to get step summary upload url");
            }

            // Do we want to throw an exception here or should we just be uploading/truncating the data
            var fileSize = new FileInfo(file).Length;
            if (fileSize > uploadUrlResponse.SoftSizeLimit)
            {
                throw new Exception($"File size is larger than the upload url allows, file size: {fileSize}, upload url size: {uploadUrlResponse.SoftSizeLimit}");
            }

            // Upload the file
            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                var response = await UploadFileAsync(uploadUrlResponse.SummaryUrl, uploadUrlResponse.BlobStorageType, fileStream, cancellationToken);
            }

            // Send step summary upload complete message
            await StepSummaryUploadCompleteAsync(planId, jobId, stepId, fileSize, cancellationToken);
        }

        // Handle file upload for step log 
        public async Task UploadResultsLogAsync(string planId, string jobId, string stepId, string file, bool finalize, bool firstBlock, CancellationToken cancellationToken)
        {
            // Get the upload url
            var uploadUrlResponse = await GetStepLogUploadUrlAsync(planId, jobId, stepId, cancellationToken);
            if (uploadUrlResponse == null)
            {
                throw new Exception("Failed to get step log upload url");
            }

            // Do we want to throw an exception here or should we just be uploading/truncating the data
            var fileSize = new FileInfo(file).Length;

            // Upload the file
            if (firstBlock)
            {
                await CreateAppendFileAsync(uploadUrlResponse.SummaryUrl, uploadUrlResponse.BlobStorageType, cancellationToken);
            }

            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                var response = await UploadAppendFileAsync(uploadUrlResponse.SummaryUrl, uploadUrlResponse.BlobStorageType, fileStream, finalize, fileSize, cancellationToken);
            }

            // Send step summary upload complete message
            await StepLogUploadCompleteAsync(planId, jobId, stepId, fileSize, cancellationToken);
        }

        private MediaTypeFormatter m_formatter;
        private Uri m_resultsServiceUrl;
        private string m_token;
    }
}
