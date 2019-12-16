using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.FileContainer.Client
{
    // until we figure out the TFS location service story for REST apis, we leave the serviceDefinition attribute off the class
    //[ServiceDefinition(ServiceInterfaces.FileContainerResource, ServiceIdentifiers.FileContainerResource)]
    public class FileContainerHttpClient : VssHttpClientBase
    {
        public event EventHandler<ReportTraceEventArgs> UploadFileReportTrace;
        public event EventHandler<ReportProgressEventArgs> UploadFileReportProgress;

        static FileContainerHttpClient()
        {
            s_translatedExceptions = new Dictionary<String, Type>();
            s_translatedExceptions.Add("ArtifactUriNotSupportedException", typeof(ArtifactUriNotSupportedException));
            s_translatedExceptions.Add("ContainerAlreadyExistsException", typeof(ContainerAlreadyExistsException));
            s_translatedExceptions.Add("ContainerItemCopyDuplicateTargetsException", typeof(ContainerItemCopyDuplicateTargetsException));
            s_translatedExceptions.Add("ContainerItemCopySourcePendingUploadException", typeof(ContainerItemCopySourcePendingUploadException));
            s_translatedExceptions.Add("ContainerItemCopyTargetChildOfSourceException", typeof(ContainerItemCopyTargetChildOfSourceException));
            s_translatedExceptions.Add("ContainerItemExistsException", typeof(ContainerItemExistsException));
            s_translatedExceptions.Add("ContainerItemNotFoundException", typeof(ContainerItemNotFoundException));
            s_translatedExceptions.Add("ContainerNoContentException", typeof(ContainerNoContentException));
            s_translatedExceptions.Add("ContainerNotFoundException", typeof(ContainerNotFoundException));
            s_translatedExceptions.Add("ContainerUnexpectedContentTypeException", typeof(ContainerUnexpectedContentTypeException));
            s_translatedExceptions.Add("ContainerWriteAccessDeniedException", typeof(ContainerWriteAccessDeniedException));
            s_translatedExceptions.Add("PendingUploadNotFoundException", typeof(PendingUploadNotFoundException));

            s_currentApiVersion = new ApiResourceVersion(1.0, 4);
        }

        public FileContainerHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public FileContainerHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public FileContainerHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public FileContainerHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public FileContainerHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// Queries for container items in a container.
        /// </summary>
        /// <param name="containerId">Id of the container to query.</param>
        /// <param name="scopeIdentifier">Id of the scope to query</param>
        /// <param name="itemPath">Path to folder or file. Can be empty or null to query from container root.</param>
        /// <param name="userState">User state</param>
        /// <param name="includeDownloadTickets">Whether to include download ticket(s) for the container item(s) in the result</param>
        /// <param name="cancellationToken">CancellationToken to cancel the task</param>
        /// <returns></returns>
        public Task<List<FileContainerItem>> QueryContainerItemsAsync(Int64 containerId, Guid scopeIdentifier, String itemPath = null, Object userState = null, Boolean includeDownloadTickets = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return QueryContainerItemsAsync(containerId, scopeIdentifier, false, itemPath, userState, includeDownloadTickets, cancellationToken);
        }

        /// <summary>
        /// Queries for container items in a container.
        /// </summary>
        /// <param name="containerId">Id of the container to query.</param>
        /// <param name="scopeIdentifier">Id of the scope to query</param>
        /// <param name="isShallow">Whether to just return immediate children items under the itemPath</param>
        /// <param name="itemPath">Path to folder or file. Can be empty or null to query from container root.</param>
        /// <param name="userState">User state</param>
        /// <param name="includeDownloadTickets">Whether to include download ticket(s) for the container item(s) in the result</param>
        /// <param name="cancellationToken">CancellationToken to cancel the task</param>
        public Task<List<FileContainerItem>> QueryContainerItemsAsync(Int64 containerId, Guid scopeIdentifier, Boolean isShallow, String itemPath = null, Object userState = null, Boolean includeDownloadTickets = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (containerId < 1)
            {
                throw new ArgumentException(WebApiResources.ContainerIdMustBeGreaterThanZero(), "containerId");
            }
            
            List<KeyValuePair<String, String>> query = AppendItemQueryString(itemPath, scopeIdentifier, includeDownloadTickets, isShallow);
            return SendAsync<List<FileContainerItem>>(HttpMethod.Get, FileContainerResourceIds.FileContainer, routeValues: new { containerId = containerId }, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Uploads a file in chunks to the specified uri. 
        /// </summary>
        /// <param name="fileStream">Stream to upload.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the task</param>
        /// <returns>Http response message.</returns>
        public async Task<HttpResponseMessage> UploadFileAsync(
            Int64 containerId,
            String itemPath,
            Stream fileStream,
            Guid scopeIdentifier,
            CancellationToken cancellationToken = default(CancellationToken),
            int chunkSize = c_defaultChunkSize,
            bool uploadFirstChunk = false,
            Object userState = null,
            Boolean compressStream = true)
        {
            if (containerId < 1)
            {
                throw new ArgumentException(WebApiResources.ContainerIdMustBeGreaterThanZero(), "containerId");
            }

            ArgumentUtility.CheckForNull(fileStream, "fileStream");

            if (fileStream.Length == 0)
            {
                HttpRequestMessage requestMessage;
                List<KeyValuePair<String, String>> query = AppendItemQueryString(itemPath, scopeIdentifier);

                // zero byte upload
                requestMessage = await CreateRequestMessageAsync(HttpMethod.Put, FileContainerResourceIds.FileContainer, routeValues: new { containerId = containerId }, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await SendAsync(requestMessage, userState, cancellationToken).ConfigureAwait(false);
            }

            ApiResourceVersion gzipSupportedVersion = new ApiResourceVersion(new Version(1, 0), 2);
            ApiResourceVersion requestVersion = await NegotiateRequestVersionAsync(FileContainerResourceIds.FileContainer, s_currentApiVersion, userState, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (compressStream
                && (requestVersion.ApiVersion < gzipSupportedVersion.ApiVersion
                    || (requestVersion.ApiVersion == gzipSupportedVersion.ApiVersion && requestVersion.ResourceVersion < gzipSupportedVersion.ResourceVersion)))
            {
                compressStream = false;
            }

            Stream streamToUpload = fileStream;
            Boolean gzipped = false;
            long filelength = fileStream.Length;

            try
            {
                if (compressStream)
                {
                    if (filelength > 65535) // if file greater than 64K use a file
                    {
                        String tempFile = Path.GetTempFileName();
                        streamToUpload = File.Create(tempFile, 32768, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
                    }
                    else
                    {
                        streamToUpload = new MemoryStream((int)filelength + 8);
                    }

                    using (GZipStream zippedStream = new GZipStream(streamToUpload, CompressionMode.Compress, true))
                    {
                        await fileStream.CopyToAsync(zippedStream).ConfigureAwait(false);
                    }

                    if (streamToUpload.Length >= filelength)
                    {
                        // compression did not help
                        streamToUpload.Dispose();
                        streamToUpload = fileStream;
                    }
                    else
                    {
                        gzipped = true;
                    }

                    streamToUpload.Seek(0, SeekOrigin.Begin);
                }

                return await UploadFileAsync(containerId, itemPath, streamToUpload, null, filelength, gzipped, scopeIdentifier, cancellationToken, chunkSize, uploadFirstChunk: uploadFirstChunk, userState: userState);
            }
            finally
            {
                if (gzipped && streamToUpload != null)
                {
                    streamToUpload.Dispose();
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<HttpResponseMessage> UploadFileAsync(
            Int64 containerId,
            String itemPath,
            Stream fileStream,
            byte[] contentId,
            Int64 fileLength,
            Boolean isGzipped,
            Guid scopeIdentifier,
            CancellationToken cancellationToken = default(CancellationToken),
            int chunkSize = c_defaultChunkSize,
            int chunkRetryTimes = c_defaultChunkRetryTimes,
            bool uploadFirstChunk = false,
            Object userState = null)
        {
            if (containerId < 1)
            {
                throw new ArgumentException(WebApiResources.ContainerIdMustBeGreaterThanZero(), "containerId");
            }

            if (chunkSize > c_maxChunkSize)
            {
                chunkSize = c_maxChunkSize;
            }

            // if a contentId is specified but the chunk size is not a 2mb multiple error
            if (contentId != null && (chunkSize % c_ContentChunkMultiple) != 0)
            {
                throw new ArgumentException(FileContainerResources.ChunksizeWrongWithContentId(c_ContentChunkMultiple), "chunkSize");
            }

            ArgumentUtility.CheckForNull(fileStream, "fileStream");

            ApiResourceVersion gzipSupportedVersion = new ApiResourceVersion(new Version(1, 0), 2);
            ApiResourceVersion requestVersion = await NegotiateRequestVersionAsync(FileContainerResourceIds.FileContainer, s_currentApiVersion, userState, cancellationToken).ConfigureAwait(false);

            if (isGzipped
                && (requestVersion.ApiVersion < gzipSupportedVersion.ApiVersion
                    || (requestVersion.ApiVersion == gzipSupportedVersion.ApiVersion && requestVersion.ResourceVersion < gzipSupportedVersion.ResourceVersion)))
            {
                throw new ArgumentException(FileContainerResources.GzipNotSupportedOnServer(), "isGzipped");
            }

            if (isGzipped && fileStream.Length >= fileLength)
            {
                throw new ArgumentException(FileContainerResources.BadCompression(), "fileLength");
            }

            HttpRequestMessage requestMessage = null;
            List<KeyValuePair<String, String>> query = AppendItemQueryString(itemPath, scopeIdentifier);

            if (fileStream.Length == 0)
            {
                // zero byte upload
                FileUploadTrace(itemPath, $"Upload zero byte file '{itemPath}'.");
                requestMessage = await CreateRequestMessageAsync(HttpMethod.Put, FileContainerResourceIds.FileContainer, routeValues: new { containerId = containerId }, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await SendAsync(requestMessage, userState, cancellationToken).ConfigureAwait(false);
            }

            bool multiChunk = false;
            int totalChunks = 1;
            if (fileStream.Length > chunkSize)
            {
                totalChunks = (int)Math.Ceiling(fileStream.Length / (double)chunkSize);
                FileUploadTrace(itemPath, $"Begin chunking upload file '{itemPath}', chunk size '{chunkSize} Bytes', total chunks '{totalChunks}'.");
                multiChunk = true;
            }
            else
            {
                FileUploadTrace(itemPath, $"File '{itemPath}' will be uploaded in one chunk.");
                chunkSize = (int)fileStream.Length;
            }

            StreamParser streamParser = new StreamParser(fileStream, chunkSize);
            SubStream currentStream = streamParser.GetNextStream();
            HttpResponseMessage response = null;

            Byte[] dataToSend = new Byte[chunkSize];
            int currentChunk = 0;
            Stopwatch uploadTimer = new Stopwatch();
            while (currentStream.Length > 0 && !cancellationToken.IsCancellationRequested)
            {
                currentChunk++;

                for (int attempt = 1; attempt <= chunkRetryTimes && !cancellationToken.IsCancellationRequested; attempt++)
                {
                    if (attempt > 1)
                    {
                        TimeSpan backoff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
                        FileUploadTrace(itemPath, $"Backoff {backoff.TotalSeconds} seconds before attempt '{attempt}' chunk '{currentChunk}' of file '{itemPath}'.");
                        await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                        currentStream.Seek(0, SeekOrigin.Begin);
                    }

                    FileUploadTrace(itemPath, $"Attempt '{attempt}' for uploading chunk '{currentChunk}' of file '{itemPath}'.");

                    // inorder for the upload to be retryable, we need the content to be re-readable
                    // to ensure this we copy the chunk into a byte array and send that
                    // chunk size ensures we can convert the length to an int
                    int bytesToCopy = (int)currentStream.Length;
                    using (MemoryStream ms = new MemoryStream(dataToSend))
                    {
                        await currentStream.CopyToAsync(ms, bytesToCopy, cancellationToken).ConfigureAwait(false);
                    }

                    // set the content and the Content-Range header
                    HttpContent byteArrayContent = new ByteArrayContent(dataToSend, 0, bytesToCopy);
                    byteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    byteArrayContent.Headers.ContentLength = currentStream.Length;
                    byteArrayContent.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(currentStream.StartingPostionOnOuterStream,
                                                                                                             currentStream.EndingPostionOnOuterStream,
                                                                                                             streamParser.Length);
                    FileUploadTrace(itemPath, $"Generate new HttpRequest for uploading file '{itemPath}', chunk '{currentChunk}' of '{totalChunks}'.");

                    try
                    {
                        if (requestMessage != null)
                        {
                            requestMessage.Dispose();
                            requestMessage = null;
                        }

                        requestMessage = await CreateRequestMessageAsync(
                            HttpMethod.Put,
                            FileContainerResourceIds.FileContainer,
                            routeValues: new { containerId = containerId },
                            version: s_currentApiVersion,
                            content: byteArrayContent,
                            queryParameters: query,
                            userState: userState,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // stop re-try on cancellation.
                        throw;
                    }
                    catch (Exception ex) when (attempt < chunkRetryTimes) // not the last attempt
                    {
                        FileUploadTrace(itemPath, $"Chunk '{currentChunk}' attempt '{attempt}' of file '{itemPath}' fail to create HttpRequest. Error: {ex.ToString()}.");
                        continue;
                    }

                    if (isGzipped)
                    {
                        //add gzip header info
                        byteArrayContent.Headers.ContentEncoding.Add("gzip");
                        byteArrayContent.Headers.Add("x-tfs-filelength", fileLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    if (contentId != null)
                    {
                        byteArrayContent.Headers.Add("x-vso-contentId", Convert.ToBase64String(contentId)); // Base64FormattingOptions.None is default when not supplied
                    }

                    FileUploadTrace(itemPath, $"Start uploading file '{itemPath}' to server, chunk '{currentChunk}'.");
                    uploadTimer.Restart();

                    try
                    {
                        if (response != null)
                        {
                            response.Dispose();
                            response = null;
                        }

                        response = await SendAsync(requestMessage, userState, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // stop re-try on cancellation.
                        throw;
                    }
                    catch (Exception ex) when (attempt < chunkRetryTimes) // not the last attempt
                    {
                        FileUploadTrace(itemPath, $"Chunk '{currentChunk}' attempt '{attempt}' of file '{itemPath}' fail to send request to server. Error: {ex.ToString()}.");
                        continue;
                    }

                    uploadTimer.Stop();
                    FileUploadTrace(itemPath, $"Finished upload chunk '{currentChunk}' of file '{itemPath}', elapsed {uploadTimer.ElapsedMilliseconds} (ms), response code '{response.StatusCode}'.");

                    if (multiChunk)
                    {
                        FileUploadProgress(itemPath, currentChunk, (int)Math.Ceiling(fileStream.Length / (double)chunkSize));
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                    else if (IsFastFailResponse(response))
                    {
                        FileUploadTrace(itemPath, $"Chunk '{currentChunk}' attempt '{attempt}' of file '{itemPath}' received non-success status code {response.StatusCode} for sending request and cannot continue.");
                        break;
                    }
                    else
                    {
                        FileUploadTrace(itemPath, $"Chunk '{currentChunk}' attempt '{attempt}' of file '{itemPath}' received non-success status code {response.StatusCode} for sending request.");
                        continue;
                    }
                }

                // if we don't have success then bail and return the failed response
                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                if (contentId != null && response.StatusCode == HttpStatusCode.Created)
                {
                    // no need to keep uploading since the server said it has all the content
                    FileUploadTrace(itemPath, $"Stop chunking upload the rest of the file '{itemPath}', since server already has all the content.");
                    break;
                }

                currentStream = streamParser.GetNextStream();
                if (uploadFirstChunk)
                {
                    break;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return response;
        }

        /// <summary>
        /// Download a file from the specified container.
        /// </summary>
        /// <param name="containerId"></param>
        /// <param name="itemPath"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="userState"></param>
        /// <returns>A stream of the file content.</returns>
        public Task<Stream> DownloadFileAsync(
            Int64 containerId,
            String itemPath,
            CancellationToken cancellationToken,
            Guid scopeIdentifier,
            Object userState = null)
        {
            return DownloadAsync(containerId, itemPath, "application/octet-stream", cancellationToken, scopeIdentifier, userState);
        }

        public bool IsFastFailResponse(HttpResponseMessage response)
        {
            int statusCode = (int)response?.StatusCode;
            return statusCode >= 400 && statusCode <= 499;
        }
        
        protected override bool ShouldThrowError(HttpResponseMessage response)
        {
            return !response.IsSuccessStatusCode && !IsFastFailResponse(response);
        }

        private async Task<HttpResponseMessage> ContainerGetRequestAsync(
            Int64 containerId,
            String itemPath,
            String contentType,
            CancellationToken cancellationToken,
            Guid scopeIdentifier,
            Object userState = null)
        {
            if (containerId < 1)
            {
                throw new ArgumentException(WebApiResources.ContainerIdMustBeGreaterThanZero(), "containerId");
            }

            List<KeyValuePair<String, String>> query = AppendItemQueryString(itemPath, scopeIdentifier);
            HttpRequestMessage requestMessage = await CreateRequestMessageAsync(HttpMethod.Get, FileContainerResourceIds.FileContainer, routeValues: new { containerId = containerId }, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!String.IsNullOrEmpty(contentType))
            {
                requestMessage.Headers.Accept.Clear();
                var header = new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(contentType);
                header.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue(ApiResourceVersionExtensions.c_apiVersionHeaderKey, "1.0"));
                header.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue(ApiResourceVersionExtensions.c_legacyResourceVersionHeaderKey, "1"));
                requestMessage.Headers.Accept.Add(header);
            }

            return await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
        }

        private List<KeyValuePair<String, String>> AppendItemQueryString(String itemPath, Guid scopeIdentifier, Boolean includeDownloadTickets = false, Boolean isShallow = false)
        {
            List<KeyValuePair<String, String>> collection = new List<KeyValuePair<String, String>>();

            if (!String.IsNullOrEmpty(itemPath))
            {
                itemPath = FileContainerItem.EnsurePathFormat(itemPath);
                collection.Add(QueryParameters.ItemPath, itemPath);
            }

            if (includeDownloadTickets)
            {
                collection.Add(QueryParameters.includeDownloadTickets, "true");
            }

            if (isShallow)
            {
                collection.Add(QueryParameters.isShallow, "true");
            }

            collection.Add(QueryParameters.ScopeIdentifier, scopeIdentifier.ToString());

            return collection;
        }

        private async Task<Stream> DownloadAsync(
            Int64 containerId,
            String itemPath,
            String contentType,
            CancellationToken cancellationToken,
            Guid scopeIdentifier,
            Object userState = null)
        {
            HttpResponseMessage response = await ContainerGetRequestAsync(containerId, itemPath, contentType, cancellationToken, scopeIdentifier, userState).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                throw new ContainerNoContentException();
            }

            if (VssStringComparer.ContentType.Equals(response.Content.Headers.ContentType.MediaType, contentType))
            {
                if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
                {
                    return new GZipStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), CompressionMode.Decompress);
                }
                else
                {
                    return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                }
            }
            else
            {
                throw new ContainerUnexpectedContentTypeException(contentType, response.Content.Headers.ContentType.MediaType);
            }
        }

        private void FileUploadTrace(string file, string message)
        {
            if (UploadFileReportTrace != null)
            {
                UploadFileReportTrace(this, new ReportTraceEventArgs(file, message));
            }
        }

        private void FileUploadProgress(string file, int currentChunk, int totalChunks)
        {
            if (UploadFileReportProgress != null)
            {
                UploadFileReportProgress(this, new ReportProgressEventArgs(file, currentChunk, totalChunks));
            }
        }

        /// <summary>
        /// Exceptions for file container errors
        /// </summary>
        protected override IDictionary<String, Type> TranslatedExceptions
        {
            get
            {
                return s_translatedExceptions;
            }
        }

        private const int c_defaultChunkSize = 8 * 1024 * 1024;
        private const int c_defaultChunkRetryTimes = 3;
        private const int c_maxChunkSize = 24 * 1024 * 1024;
        private const int c_ContentChunkMultiple = 2 * 1024 * 1024;
        private static Dictionary<String, Type> s_translatedExceptions;
        private static readonly ApiResourceVersion s_currentApiVersion;
    }

    public class ReportTraceEventArgs : EventArgs
    {
        public ReportTraceEventArgs(String file, String message)
        {
            File = file;
            Message = message;
        }

        public String File { get; private set; }
        public String Message { get; private set; }
    }

    public class ReportProgressEventArgs : EventArgs
    {
        public ReportProgressEventArgs(String file, int currentChunk, int totalChunks)
        {
            File = file;
            CurrentChunk = currentChunk;
            TotalChunks = totalChunks;
        }

        public String File { get; private set; }
        public int CurrentChunk { get; private set; }
        public int TotalChunks { get; private set; }
    }
}
