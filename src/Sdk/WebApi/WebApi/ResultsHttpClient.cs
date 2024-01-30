using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Formatting;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Results.Contracts;
using Sdk.WebApi.WebApi;

namespace GitHub.Services.Results.Client
{
    public class ResultsHttpClient : RawHttpClientBase
    {
        public ResultsHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            string token,
            bool disposeHandler,
            bool useSdk)
            : base(baseUrl, pipeline, disposeHandler)
        {
            m_token = token;
            m_resultsServiceUrl = baseUrl;
            m_formatter = new JsonMediaTypeFormatter();
            m_changeIdCounter = 1;
            m_useSdk = useSdk;
        }

        // Get Sas URL calls
        private async Task<T> GetResultsSignedURLResponse<R, T>(Uri uri, CancellationToken cancellationToken, R request)
        {
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<R>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        return await ReadJsonContentAsync<T>(response, cancellationToken);
                    }
                }
            }
        }

        private async Task<GetSignedStepSummaryURLResponse> GetStepSummaryUploadUrlAsync(string planId, string jobId, Guid stepId, CancellationToken cancellationToken)
        {
            var request = new GetSignedStepSummaryURLRequest()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
                StepBackendId = stepId.ToString()
            };

            var getStepSummarySignedBlobURLEndpoint = new Uri(m_resultsServiceUrl, Constants.GetStepSummarySignedBlobURL);

            return await GetResultsSignedURLResponse<GetSignedStepSummaryURLRequest, GetSignedStepSummaryURLResponse>(getStepSummarySignedBlobURLEndpoint, cancellationToken, request);
        }

        private async Task<GetSignedStepLogsURLResponse> GetStepLogUploadUrlAsync(string planId, string jobId, Guid stepId, CancellationToken cancellationToken)
        {
            var request = new GetSignedStepLogsURLRequest()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
                StepBackendId = stepId.ToString(),
            };

            var getStepLogsSignedBlobURLEndpoint = new Uri(m_resultsServiceUrl, Constants.GetStepLogsSignedBlobURL);

            return await GetResultsSignedURLResponse<GetSignedStepLogsURLRequest, GetSignedStepLogsURLResponse>(getStepLogsSignedBlobURLEndpoint, cancellationToken, request);
        }

        private async Task<GetSignedDiagnosticLogsURLResponse> GetSignedDiagnosticLogsURL(string planid, string jobid, CancellationToken cancellationtoken)
        {
            var request = new GetSignedDiagnosticLogsURLRequest()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
            };

            var getDiagnosticLogsSignedBlobURLEndpoint = new Uri(m_resultsServiceUrl, Constants.GetDiagnosticLogsSignedBlobURL);

            return await GetSignedDiagnosticLogsURLResponse<GetSignedDiagnosticLogsURLRequest, GetSignedDiagnosticLogsURLResponse>(getDiagnosticLogsSignedBlobURLEndpoint, cancellationToken, request);
        }

        private async Task<GetSignedDiagnosticLogsURLResponse>GetDiagnosticLogsUploadUrlAsync(string planId, string jobId, CancellationToken cancellationToken)
        {
            var request = new GetSignedDiagnosticLogsURLRequest()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
            };

            var getDiagnosticLogsSignedBlobURLEndpoint = new Uri(m_resultsServiceUrl, Constants.GetDiagnosticLogsSignedBlobURL);

            return await GetResultsSignedURLResponse<GetSignedDiagnosticLogsURLRequest, GetSignedDiagnosticLogsURLResponse>(getDiagnosticLogsSignedBlobURLEndpoint, cancellationToken, request);
        }

        private async Task<GetSignedJobLogsURLResponse> GetJobLogUploadUrlAsync(string planId, string jobId, CancellationToken cancellationToken)
        {
            var request = new GetSignedJobLogsURLRequest()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
            };

            var getJobLogsSignedBlobURLEndpoint = new Uri(m_resultsServiceUrl, Constants.GetJobLogsSignedBlobURL);

            return await GetResultsSignedURLResponse<GetSignedJobLogsURLRequest, GetSignedJobLogsURLResponse>(getJobLogsSignedBlobURLEndpoint, cancellationToken, request);
        }

        // Create metadata calls
        private async Task SendRequest<R>(Uri uri, CancellationToken cancellationToken, R request, string timestamp)
        {
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<R>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        var jsonResponse = await ReadJsonContentAsync<CreateMetadataResponse>(response, cancellationToken);
                        if (!jsonResponse.Ok)
                        {
                            throw new Exception($"Failed to mark {typeof(R).Name} upload as complete, status code: {response.StatusCode}, ok: {jsonResponse.Ok}, timestamp: {timestamp}");
                        }
                    }
                }
            }
        }

        private async Task StepSummaryUploadCompleteAsync(string planId, string jobId, Guid stepId, long size, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString(Constants.TimestampFormat);
            var request = new StepSummaryMetadataCreate()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
                StepBackendId = stepId.ToString(),
                Size = size,
                UploadedAt = timestamp
            };

            var createStepSummaryMetadataEndpoint = new Uri(m_resultsServiceUrl, Constants.CreateStepSummaryMetadata);
            await SendRequest<StepSummaryMetadataCreate>(createStepSummaryMetadataEndpoint, cancellationToken, request, timestamp);
        }

        private async Task StepLogUploadCompleteAsync(string planId, string jobId, Guid stepId, long lineCount, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString(Constants.TimestampFormat);
            var request = new StepLogsMetadataCreate()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
                StepBackendId = stepId.ToString(),
                UploadedAt = timestamp,
                LineCount = lineCount,
            };

            var createStepLogsMetadataEndpoint = new Uri(m_resultsServiceUrl, Constants.CreateStepLogsMetadata);
            await SendRequest<StepLogsMetadataCreate>(createStepLogsMetadataEndpoint, cancellationToken, request, timestamp);
        }

        private async Task JobLogUploadCompleteAsync(string planId, string jobId, long lineCount, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString(Constants.TimestampFormat);
            var request = new JobLogsMetadataCreate()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
                UploadedAt = timestamp,
                LineCount = lineCount,
            };

            var createJobLogsMetadataEndpoint = new Uri(m_resultsServiceUrl, Constants.CreateJobLogsMetadata);
            await SendRequest<JobLogsMetadataCreate>(createJobLogsMetadataEndpoint, cancellationToken, request, timestamp);
        }

        private (Uri path, string sas) ParseSasToken(string url)
        {
            if (String.IsNullOrEmpty(url))
            {
                throw new Exception($"SAS url is empty");
            }

            var blobUri = new UriBuilder(url);
            var sasUrl = blobUri.Query.Substring(1); //remove starting "?"
            blobUri.Query = null; // remove query params
            return (blobUri.Uri, sasUrl);
        }

        private BlobClient GetBlobClient(string url)
        {
            var blobUri = ParseSasToken(url);

            var opts = new BlobClientOptions
            {
                Retry =
                {
                    MaxRetries = Constants.DefaultBlobUploadRetries,
                    NetworkTimeout = TimeSpan.FromSeconds(Constants.DefaultNetworkTimeoutInSeconds)
                }
            };

            return new BlobClient(blobUri.path, new AzureSasCredential(blobUri.sas), opts);
        }

        private AppendBlobClient GetAppendBlobClient(string url)
        {
            var blobUri = ParseSasToken(url);

            var opts = new BlobClientOptions
            {
                Retry =
                {
                    MaxRetries = Constants.DefaultBlobUploadRetries,
                    NetworkTimeout = TimeSpan.FromSeconds(Constants.DefaultNetworkTimeoutInSeconds)
                }
            };

            return new AppendBlobClient(blobUri.path, new AzureSasCredential(blobUri.sas), opts);
        }

        private async Task UploadBlockFileAsync(string url, string blobStorageType, FileStream file, CancellationToken cancellationToken)
        {
            if (m_useSdk && blobStorageType == BlobStorageTypes.AzureBlobStorage)
            {
                var blobClient = GetBlobClient(url);
                try
                {
                    await blobClient.UploadAsync(file, cancellationToken);
                }
                catch (RequestFailedException e)
                {
                    throw new Exception($"Failed to upload block to Azure blob: {e.Message}");
                }
            }
            else
            {
                // Upload the file to the url
                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StreamContent(file)
                };

                if (blobStorageType == BlobStorageTypes.AzureBlobStorage)
                {
                    request.Content.Headers.Add(Constants.AzureBlobTypeHeader, Constants.AzureBlockBlob);
                }

                using (var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, userState: null, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to upload file, status code: {response.StatusCode}, reason: {response.ReasonPhrase}");
                    }
                }
            }
        }

        private async Task CreateAppendFileAsync(string url, string blobStorageType, CancellationToken cancellationToken)
        {
            if (m_useSdk && blobStorageType == BlobStorageTypes.AzureBlobStorage)
            {
                var appendBlobClient = GetAppendBlobClient(url);
                try
                {
                    await appendBlobClient.CreateAsync(cancellationToken: cancellationToken);
                }
                catch (RequestFailedException e)
                {
                    throw new Exception($"Failed to create append blob in Azure blob: {e.Message}");
                }
            }
            else
            {
                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StringContent("")
                };
                if (blobStorageType == BlobStorageTypes.AzureBlobStorage)
                {
                    request.Content.Headers.Add(Constants.AzureBlobTypeHeader, Constants.AzureAppendBlob);
                    request.Content.Headers.Add("Content-Length", "0");
                }

                using (var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, userState: null, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to create append file, status code: {response.StatusCode}, reason: {response.ReasonPhrase}");
                    }
                }
            }
        }

        private async Task UploadAppendFileAsync(string url, string blobStorageType, FileStream file, bool finalize, long fileSize, CancellationToken cancellationToken)
        {
            if (m_useSdk && blobStorageType == BlobStorageTypes.AzureBlobStorage)
            {
                var appendBlobClient = GetAppendBlobClient(url);
                try
                {
                    await appendBlobClient.AppendBlockAsync(file, cancellationToken: cancellationToken);
                    if (finalize)
                    {
                        await appendBlobClient.SealAsync(cancellationToken: cancellationToken);
                    }
                }
                catch (RequestFailedException e)
                {
                    throw new Exception($"Failed to upload append block in Azure blob: {e.Message}");
                }
            }
            else
            {
                var comp = finalize ? "&comp=appendblock&seal=true" : "&comp=appendblock";
                // Upload the file to the url
                var request = new HttpRequestMessage(HttpMethod.Put, url + comp)
                {
                    Content = new StreamContent(file)
                };

                if (blobStorageType == BlobStorageTypes.AzureBlobStorage)
                {
                    request.Content.Headers.Add("Content-Length", fileSize.ToString());
                    request.Content.Headers.Add(Constants.AzureBlobSealedHeader, finalize.ToString());
                }

                using (var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, userState: null, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to upload append file, status code: {response.StatusCode}, reason: {response.ReasonPhrase}, object: {response}, fileSize: {fileSize}");
                    }
                }
            }
        }

        // Handle file upload for step summary
        public async Task UploadStepSummaryAsync(string planId, string jobId, Guid stepId, string file, CancellationToken cancellationToken)
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
                await UploadBlockFileAsync(uploadUrlResponse.SummaryUrl, uploadUrlResponse.BlobStorageType, fileStream, cancellationToken);
            }

            // Send step summary upload complete message
            await StepSummaryUploadCompleteAsync(planId, jobId, stepId, fileSize, cancellationToken);
        }

        private async Task UploadLogFile(string file, bool finalize, bool firstBlock, string sasUrl, string blobStorageType,
            CancellationToken cancellationToken)
        {
            if (firstBlock && finalize)
            {
                // This is the one and only block, just use a block blob
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    await UploadBlockFileAsync(sasUrl, blobStorageType, fileStream, cancellationToken);
                }
            }
            else
            {
                // This is either not the first block, which means it's using appendBlob; or first block and need to wait for additional blocks.  Using append blob in either case. 
                // Create the Append blob
                if (firstBlock)
                {
                    await CreateAppendFileAsync(sasUrl, blobStorageType, cancellationToken);
                }

                // Upload content
                var fileSize = new FileInfo(file).Length;
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    await UploadAppendFileAsync(sasUrl, blobStorageType, fileStream, finalize, fileSize, cancellationToken);
                }
            }
        }

        // Handle file upload for step log
        public async Task UploadResultsStepLogAsync(string planId, string jobId, Guid stepId, string file, bool finalize, bool firstBlock, long lineCount, CancellationToken cancellationToken)
        {
            // Get the upload url
            var uploadUrlResponse = await GetStepLogUploadUrlAsync(planId, jobId, stepId, cancellationToken);
            if (uploadUrlResponse == null || uploadUrlResponse.LogsUrl == null)
            {
                throw new Exception("Failed to get step log upload url");
            }

            await UploadLogFile(file, finalize, firstBlock, uploadUrlResponse.LogsUrl, uploadUrlResponse.BlobStorageType, cancellationToken);

            // Update metadata
            if (finalize)
            {
                // Send step log upload complete message
                await StepLogUploadCompleteAsync(planId, jobId, stepId, lineCount, cancellationToken);
            }
        }

        // Handle file upload for job log
        public async Task UploadResultsJobLogAsync(string planId, string jobId, string file, bool finalize, bool firstBlock, long lineCount, CancellationToken cancellationToken)
        {
            // Get the upload url
            var uploadUrlResponse = await GetJobLogUploadUrlAsync(planId, jobId, cancellationToken);
            if (uploadUrlResponse == null || uploadUrlResponse.LogsUrl == null)
            {
                throw new Exception("Failed to get job log upload url");
            }

            await UploadLogFile(file, finalize, firstBlock, uploadUrlResponse.LogsUrl, uploadUrlResponse.BlobStorageType, cancellationToken);

            // Update metadata
            if (finalize)
            {
                // Send step log upload complete message
                await JobLogUploadCompleteAsync(planId, jobId, lineCount, cancellationToken);
            }
        }

        public async Task UploadResultsDiagnosticLogsAsync(string planId, string jobId, string file, CancellationToken cancellationToken)
        {
            // Get the upload url
            var uploadUrlResponse = await GetDiagnosticLogsUploadUrlAsync(planId, jobId, cancellationToken);
            if (uploadUrlResponse == null || uploadUrlResponse.LogsUrl == null)
            {
                throw new Exception("Failed to get diagnostic logs upload url");
            }

            await UploadLogFile(file, true, true, uploadUrlResponse.DiagnosticLogsURL, uploadUrlResponse.BlobStorageType, cancellationToken);
        }

        private Step ConvertTimelineRecordToStep(TimelineRecord r)
        {
            return new Step()
            {
                ExternalId = r.Id.ToString(),
                Number = r.Order.GetValueOrDefault(),
                Name = r.Name,
                Status = ConvertStateToStatus(r.State.GetValueOrDefault()),
                StartedAt = r.StartTime?.ToString(Constants.TimestampFormat),
                CompletedAt = r.FinishTime?.ToString(Constants.TimestampFormat),
                Conclusion = ConvertResultToConclusion(r.Result)
            };
        }

        private Status ConvertStateToStatus(TimelineRecordState s)
        {
            switch (s)
            {
                case TimelineRecordState.Completed:
                    return Status.StatusCompleted;
                case TimelineRecordState.Pending:
                    return Status.StatusPending;
                case TimelineRecordState.InProgress:
                    return Status.StatusInProgress;
                default:
                    return Status.StatusUnknown;
            }
        }

        private Conclusion ConvertResultToConclusion(TaskResult? r)
        {
            if (!r.HasValue)
            {
                return Conclusion.ConclusionUnknown;
            }

            switch (r)
            {
                case TaskResult.Succeeded:
                case TaskResult.SucceededWithIssues:
                    return Conclusion.ConclusionSuccess;
                case TaskResult.Canceled:
                    return Conclusion.ConclusionCancelled;
                case TaskResult.Skipped:
                    return Conclusion.ConclusionSkipped;
                case TaskResult.Failed:
                    return Conclusion.ConclusionFailure;
                default:
                    return Conclusion.ConclusionUnknown;
            }
        }

        public async Task UpdateWorkflowStepsAsync(Guid planId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString(Constants.TimestampFormat);
            var stepRecords = records.Where(r => String.Equals(r.RecordType, "Task", StringComparison.Ordinal));
            var stepUpdateRequests = stepRecords.GroupBy(r => r.ParentId).Select(sg => new StepsUpdateRequest()
            {
                WorkflowRunBackendId = planId.ToString(),
                WorkflowJobRunBackendId = sg.Key.ToString(),
                ChangeOrder = m_changeIdCounter++,
                Steps = sg.Select(ConvertTimelineRecordToStep)
            });

            var stepUpdateEndpoint = new Uri(m_resultsServiceUrl, Constants.WorkflowStepsUpdate);
            foreach (var request in stepUpdateRequests)
            {
                await SendRequest<StepsUpdateRequest>(stepUpdateEndpoint, cancellationToken, request, timestamp);
            }
        }

        private MediaTypeFormatter m_formatter;
        private Uri m_resultsServiceUrl;
        private string m_token;
        private int m_changeIdCounter;
        private bool m_useSdk;
    }

    // Constants specific to results
    public static class Constants
    {
        public static readonly string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffK";

        public static readonly string ResultsReceiverTwirpEndpoint = "twirp/results.services.receiver.Receiver/";
        public static readonly string GetStepSummarySignedBlobURL = ResultsReceiverTwirpEndpoint + "GetStepSummarySignedBlobURL";
        public static readonly string CreateStepSummaryMetadata = ResultsReceiverTwirpEndpoint + "CreateStepSummaryMetadata";
        public static readonly string GetStepLogsSignedBlobURL = ResultsReceiverTwirpEndpoint + "GetStepLogsSignedBlobURL";
        public static readonly string CreateStepLogsMetadata = ResultsReceiverTwirpEndpoint + "CreateStepLogsMetadata";
        public static readonly string GetJobLogsSignedBlobURL = ResultsReceiverTwirpEndpoint + "GetJobLogsSignedBlobURL";
        public static readonly string CreateJobLogsMetadata = ResultsReceiverTwirpEndpoint + "CreateJobLogsMetadata";
        public static readonly string ResultsProtoApiV1Endpoint = "twirp/github.actions.results.api.v1.WorkflowStepUpdateService/";
        public static readonly string WorkflowStepsUpdate = ResultsProtoApiV1Endpoint + "WorkflowStepsUpdate";

        public static readonly int DefaultNetworkTimeoutInSeconds = 30;
        public static readonly int DefaultBlobUploadRetries = 3;

        public static readonly string AzureBlobSealedHeader = "x-ms-blob-sealed";
        public static readonly string AzureBlobTypeHeader = "x-ms-blob-type";
        public static readonly string AzureBlockBlob = "BlockBlob";
        public static readonly string AzureAppendBlob = "AppendBlob";
    }

}
