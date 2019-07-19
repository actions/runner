using GitHub.Services.FileContainer.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using GitHub.Services.WebApi;
using System.Net.Http;
using System.Net;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    public class FileContainerServer
    {
        private readonly ConcurrentQueue<string> _fileUploadQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _fileUploadTraceLog = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _fileUploadProgressLog = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        private readonly FileContainerHttpClient _fileContainerHttpClient;

        private CancellationTokenSource _uploadCancellationTokenSource;
        private TaskCompletionSource<int> _uploadFinished;
        private Guid _projectId;
        private long _containerId;
        private string _containerPath;
        private int _filesProcessed = 0;
        private string _sourceParentDirectory;

        public FileContainerServer(
            VssConnection connection,
            Guid projectId,
            long containerId,
            string containerPath)
        {
            _projectId = projectId;
            _containerId = containerId;
            _containerPath = containerPath;

            // default file upload request timeout to 600 seconds
            var fileContainerClientConnectionSetting = connection.Settings.Clone();
            if (fileContainerClientConnectionSetting.SendTimeout < TimeSpan.FromSeconds(600))
            {
                fileContainerClientConnectionSetting.SendTimeout = TimeSpan.FromSeconds(600);
            }

            var fileContainerClientConnection = new VssConnection(connection.Uri, connection.Credentials, fileContainerClientConnectionSetting);
            _fileContainerHttpClient = fileContainerClientConnection.GetClient<FileContainerHttpClient>();
        }

        public async Task<long> CopyToContainerAsync(
            RunnerActionPluginExecutionContext context,
            String source,
            CancellationToken cancellationToken)
        {
            //set maxConcurrentUploads up to 2 until figure out how to use WinHttpHandler.MaxConnectionsPerServer modify DefaultConnectionLimit
            int maxConcurrentUploads = Math.Min(Environment.ProcessorCount, 2);
            //context.Output($"Max Concurrent Uploads {maxConcurrentUploads}");

            List<String> files;
            if (File.Exists(source))
            {
                files = new List<String>() { source };
                _sourceParentDirectory = Path.GetDirectoryName(source);
            }
            else
            {
                files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).ToList();
                _sourceParentDirectory = source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            context.Output($"Uploading {files.Count()} files");
            using (_uploadCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // hook up reporting event from file container client.
                _fileContainerHttpClient.UploadFileReportTrace += UploadFileTraceReportReceived;
                _fileContainerHttpClient.UploadFileReportProgress += UploadFileProgressReportReceived;

                try
                {
                    // try upload all files for the first time.
                    UploadResult uploadResult = await ParallelUploadAsync(context, files, maxConcurrentUploads, _uploadCancellationTokenSource.Token);

                    if (uploadResult.FailedFiles.Count == 0)
                    {
                        // all files have been upload succeed.
                        context.Output("File upload succeed.");
                        return uploadResult.TotalFileSizeUploaded;
                    }
                    else
                    {
                        context.Output($"{uploadResult.FailedFiles.Count} files failed to upload, retry these files after a minute.");
                    }

                    // Delay 1 min then retry failed files.
                    for (int timer = 60; timer > 0; timer -= 5)
                    {
                        context.Output($"Retry file upload after {timer} seconds.");
                        await Task.Delay(TimeSpan.FromSeconds(5), _uploadCancellationTokenSource.Token);
                    }

                    // Retry upload all failed files.
                    context.Output($"Start retry {uploadResult.FailedFiles.Count} failed files upload.");
                    UploadResult retryUploadResult = await ParallelUploadAsync(context, uploadResult.FailedFiles, maxConcurrentUploads, _uploadCancellationTokenSource.Token);

                    if (retryUploadResult.FailedFiles.Count == 0)
                    {
                        // all files have been upload succeed after retry.
                        context.Output("File upload succeed after retry.");
                        return uploadResult.TotalFileSizeUploaded + retryUploadResult.TotalFileSizeUploaded;
                    }
                    else
                    {
                        throw new Exception("File upload failed even after retry.");
                    }
                }
                finally
                {
                    _fileContainerHttpClient.UploadFileReportTrace -= UploadFileTraceReportReceived;
                    _fileContainerHttpClient.UploadFileReportProgress -= UploadFileProgressReportReceived;
                }
            }
        }

        private async Task<UploadResult> ParallelUploadAsync(RunnerActionPluginExecutionContext context, IReadOnlyList<string> files, int concurrentUploads, CancellationToken token)
        {
            // return files that fail to upload and total artifact size
            var uploadResult = new UploadResult();

            // nothing needs to upload
            if (files.Count == 0)
            {
                return uploadResult;
            }

            // ensure the file upload queue is empty.
            if (!_fileUploadQueue.IsEmpty)
            {
                throw new ArgumentOutOfRangeException(nameof(_fileUploadQueue));
            }

            // enqueue file into upload queue.
            foreach (var file in files)
            {
                _fileUploadQueue.Enqueue(file);
            }

            // Start upload monitor task.
            _filesProcessed = 0;
            _uploadFinished = new TaskCompletionSource<int>();
            _fileUploadTraceLog.Clear();
            _fileUploadProgressLog.Clear();
            Task uploadMonitor = ReportingAsync(context, files.Count(), _uploadCancellationTokenSource.Token);

            // Start parallel upload tasks.
            List<Task<UploadResult>> parallelUploadingTasks = new List<Task<UploadResult>>();
            for (int uploader = 0; uploader < concurrentUploads; uploader++)
            {
                parallelUploadingTasks.Add(UploadAsync(context, uploader, _uploadCancellationTokenSource.Token));
            }

            // Wait for parallel upload finish.
            await Task.WhenAll(parallelUploadingTasks);
            foreach (var uploadTask in parallelUploadingTasks)
            {
                // record all failed files.
                uploadResult.AddUploadResult(await uploadTask);
            }

            // Stop monitor task;
            _uploadFinished.TrySetResult(0);
            await uploadMonitor;

            return uploadResult;
        }

        private async Task<UploadResult> UploadAsync(RunnerActionPluginExecutionContext context, int uploaderId, CancellationToken token)
        {
            List<string> failedFiles = new List<string>();
            long uploadedSize = 0;
            string fileToUpload;
            Stopwatch uploadTimer = new Stopwatch();
            while (_fileUploadQueue.TryDequeue(out fileToUpload))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    using (FileStream fs = File.Open(fileToUpload, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        string itemPath = (_containerPath.TrimEnd('/') + "/" + fileToUpload.Remove(0, _sourceParentDirectory.Length + 1)).Replace('\\', '/');
                        uploadTimer.Restart();
                        bool catchExceptionDuringUpload = false;
                        HttpResponseMessage response = null;
                        try
                        {
                            response = await _fileContainerHttpClient.UploadFileAsync(_containerId, itemPath, fs, _projectId, cancellationToken: token, chunkSize: 4 * 1024 * 1024);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            context.Output($"File upload has been cancelled during upload file: '{fileToUpload}'.");
                            if (response != null)
                            {
                                response.Dispose();
                                response = null;
                            }

                            throw;
                        }
                        catch (Exception ex)
                        {
                            catchExceptionDuringUpload = true;
                            context.Output($"Fail to upload '{fileToUpload}' due to '{ex.Message}'.");
                            context.Output(ex.ToString());
                        }

                        uploadTimer.Stop();
                        if (catchExceptionDuringUpload || (response != null && response.StatusCode != HttpStatusCode.Created))
                        {
                            if (response != null)
                            {
                                context.Output($"Unable to copy file to server StatusCode={response.StatusCode}: {response.ReasonPhrase}. Source file path: {fileToUpload}. Target server path: {itemPath}");
                            }

                            // output detail upload trace for the file.
                            ConcurrentQueue<string> logQueue;
                            if (_fileUploadTraceLog.TryGetValue(itemPath, out logQueue))
                            {
                                context.Output($"Detail upload trace for file that fail to upload: {itemPath}");
                                string message;
                                while (logQueue.TryDequeue(out message))
                                {
                                    context.Output(message);
                                }
                            }

                            // tracking file that failed to upload.
                            failedFiles.Add(fileToUpload);
                        }
                        else
                        {
                            context.Debug($"File: '{fileToUpload}' took {uploadTimer.ElapsedMilliseconds} milliseconds to finish upload");
                            uploadedSize += fs.Length;
                            // debug detail upload trace for the file.
                            ConcurrentQueue<string> logQueue;
                            if (_fileUploadTraceLog.TryGetValue(itemPath, out logQueue))
                            {
                                context.Debug($"Detail upload trace for file: {itemPath}");
                                string message;
                                while (logQueue.TryDequeue(out message))
                                {
                                    context.Debug(message);
                                }
                            }
                        }

                        if (response != null)
                        {
                            response.Dispose();
                            response = null;
                        }
                    }

                    Interlocked.Increment(ref _filesProcessed);
                }
                catch (Exception ex)
                {
                    context.Output($"File error '{ex.Message}' when uploading file '{fileToUpload}'.");
                    throw ex;
                }
            }

            return new UploadResult(failedFiles, uploadedSize);
        }

        private async Task ReportingAsync(RunnerActionPluginExecutionContext context, int totalFiles, CancellationToken token)
        {
            int traceInterval = 0;
            while (!_uploadFinished.Task.IsCompleted && !token.IsCancellationRequested)
            {
                bool hasDetailProgress = false;
                foreach (var file in _fileUploadProgressLog)
                {
                    string message;
                    while (file.Value.TryDequeue(out message))
                    {
                        hasDetailProgress = true;
                        context.Output(message);
                    }
                }

                // trace total file progress every 25 seconds when there is no file level detail progress
                if (++traceInterval % 2 == 0 && !hasDetailProgress)
                {
                    context.Output($"Total file: {totalFiles} ---- Processed file: {_filesProcessed} ({(_filesProcessed * 100) / totalFiles}%)");
                }

                await Task.WhenAny(_uploadFinished.Task, Task.Delay(5000, token));
            }
        }

        private void UploadFileTraceReportReceived(object sender, ReportTraceEventArgs e)
        {
            ConcurrentQueue<string> logQueue = _fileUploadTraceLog.GetOrAdd(e.File, new ConcurrentQueue<string>());
            logQueue.Enqueue(e.Message);
        }

        private void UploadFileProgressReportReceived(object sender, ReportProgressEventArgs e)
        {
            ConcurrentQueue<string> progressQueue = _fileUploadProgressLog.GetOrAdd(e.File, new ConcurrentQueue<string>());
            progressQueue.Enqueue($"Uploading '{e.File}' ({(e.CurrentChunk * 100) / e.TotalChunks}%)");
        }
    }

    public class UploadResult
    {
        public UploadResult()
        {
            FailedFiles = new List<string>();
            TotalFileSizeUploaded = 0;
        }

        public UploadResult(List<string> failedFiles, long totalFileSizeUploaded)
        {
            FailedFiles = failedFiles;
            TotalFileSizeUploaded = totalFileSizeUploaded;
        }
        public List<string> FailedFiles { get; set; }

        public long TotalFileSizeUploaded { get; set; }

        public void AddUploadResult(UploadResult resultToAdd)
        {
            this.FailedFiles.AddRange(resultToAdd.FailedFiles);
            this.TotalFileSizeUploaded += resultToAdd.TotalFileSizeUploaded;
        }
    }
}