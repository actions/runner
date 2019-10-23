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
using GitHub.Services.FileContainer;
using GitHub.Services.Common;

namespace GitHub.Runner.Plugins.Artifact
{
    public class FileContainerServer
    {
        private const int _defaultFileStreamBufferSize = 4096;

        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
        private const int _defaultCopyBufferSize = 81920;

        private readonly ConcurrentQueue<string> _fileUploadQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<DownloadInfo> _fileDownloadQueue = new ConcurrentQueue<DownloadInfo>();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _fileUploadTraceLog = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _fileUploadProgressLog = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        private readonly FileContainerHttpClient _fileContainerHttpClient;

        private CancellationTokenSource _uploadCancellationTokenSource;
        private CancellationTokenSource _downloadCancellationTokenSource;
        private TaskCompletionSource<int> _uploadFinished;
        private TaskCompletionSource<int> _downloadFinished;
        private Guid _projectId;
        private long _containerId;
        private string _containerPath;
        private int _uploadFilesProcessed = 0;
        private int _downloadFilesProcessed = 0;
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

            // default file upload/download request timeout to 600 seconds
            var fileContainerClientConnectionSetting = connection.Settings.Clone();
            if (fileContainerClientConnectionSetting.SendTimeout < TimeSpan.FromSeconds(600))
            {
                fileContainerClientConnectionSetting.SendTimeout = TimeSpan.FromSeconds(600);
            }

            var fileContainerClientConnection = new VssConnection(connection.Uri, connection.Credentials, fileContainerClientConnectionSetting);
            _fileContainerHttpClient = fileContainerClientConnection.GetClient<FileContainerHttpClient>();
        }

        public async Task DownloadFromContainerAsync(
            RunnerActionPluginExecutionContext context,
            String destination,
            CancellationToken cancellationToken)
        {
            // Find out all container items need to be processed
            List<FileContainerItem> containerItems = new List<FileContainerItem>();
            int retryCount = 0;
            while (retryCount < 3)
            {
                try
                {
                    containerItems = await _fileContainerHttpClient.QueryContainerItemsAsync(_containerId,
                                                                                             _projectId,
                                                                                             _containerPath,
                                                                                             cancellationToken: cancellationToken);
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    context.Debug($"Container query has been cancelled.");
                    throw;
                }
                catch (Exception ex) when (retryCount < 2)
                {
                    retryCount++;
                    context.Warning($"Fail to query container items under #/{_containerId}/{_containerPath}, Error: {ex.Message}");
                    context.Debug(ex.ToString());
                }

                var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                context.Warning($"Back off {backOff.TotalSeconds} seconds before retry.");
                await Task.Delay(backOff);
            }

            if (containerItems.Count == 0)
            {
                context.Output($"There is nothing under #/{_containerId}/{_containerPath}");
                return;
            }

            // container items will include both folders, files and even file with zero size
            // Create all required empty folders and emptry files, gather a list of files that we need to download from server.
            int foldersCreated = 0;
            int emptryFilesCreated = 0;
            List<DownloadInfo> downloadFiles = new List<DownloadInfo>();
            foreach (var item in containerItems.OrderBy(x => x.Path))
            {
                if (!item.Path.StartsWith(_containerPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentOutOfRangeException($"Item {item.Path} is not under #/{_containerId}/{_containerPath}");
                }

                var localRelativePath = item.Path.Substring(_containerPath.Length).TrimStart('/');
                var localPath = Path.Combine(destination, localRelativePath);

                if (item.ItemType == ContainerItemType.Folder)
                {
                    context.Debug($"Ensure folder exists: {localPath}");
                    Directory.CreateDirectory(localPath);
                    foldersCreated++;
                }
                else if (item.ItemType == ContainerItemType.File)
                {
                    if (item.FileLength == 0)
                    {
                        context.Debug($"Create empty file at: {localPath}");
                        var parentDirectory = Path.GetDirectoryName(localPath);
                        Directory.CreateDirectory(parentDirectory);
                        IOUtil.DeleteFile(localPath);
                        using (new FileStream(localPath, FileMode.Create))
                        {
                        }
                        emptryFilesCreated++;
                    }
                    else
                    {
                        context.Debug($"Prepare download {item.Path} to {localPath}");
                        downloadFiles.Add(new DownloadInfo(item.Path, localPath));
                    }
                }
                else
                {
                    throw new NotSupportedException(item.ItemType.ToString());
                }
            }

            if (foldersCreated > 0)
            {
                context.Output($"{foldersCreated} folders created.");
            }

            if (emptryFilesCreated > 0)
            {
                context.Output($"{emptryFilesCreated} empty files created.");
            }

            if (downloadFiles.Count == 0)
            {
                context.Output($"There is nothing to download");
                return;
            }

            // Start multi-task to download all files.
            using (_downloadCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // try download all files for the first time.
                DownloadResult downloadResult = await ParallelDownloadAsync(context, downloadFiles.AsReadOnly(), Math.Min(downloadFiles.Count, Environment.ProcessorCount), _downloadCancellationTokenSource.Token);
                if (downloadResult.FailedFiles.Count == 0)
                {
                    // all files have been download succeed.
                    context.Output($"{downloadFiles.Count} files download succeed.");
                    return;
                }
                else
                {
                    context.Output($"{downloadResult.FailedFiles.Count} files failed to download, retry these files after a minute.");
                }

                // Delay 1 min then retry failed files.
                for (int timer = 60; timer > 0; timer -= 5)
                {
                    context.Output($"Retry file download after {timer} seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(5), _uploadCancellationTokenSource.Token);
                }

                // Retry download all failed files.
                context.Output($"Start retry {downloadResult.FailedFiles.Count} failed files upload.");
                DownloadResult retryDownloadResult = await ParallelDownloadAsync(context, downloadResult.FailedFiles.AsReadOnly(), Math.Min(downloadResult.FailedFiles.Count, Environment.ProcessorCount), _downloadCancellationTokenSource.Token);
                if (retryDownloadResult.FailedFiles.Count == 0)
                {
                    // all files have been download succeed after retry.
                    context.Output($"{downloadResult.FailedFiles} files download succeed after retry.");
                    return;
                }
                else
                {
                    throw new Exception($"{retryDownloadResult.FailedFiles.Count} files failed to download even after retry.");
                }
            }
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

                    if (uploadResult.RetryFiles.Count == 0)
                    {
                        // all files have been upload succeed.
                        context.Output("File upload complete.");
                        return uploadResult.TotalFileSizeUploaded;
                    }
                    else
                    {
                        context.Output($"{uploadResult.RetryFiles.Count} files failed to upload, retry these files after a minute.");
                    }

                    // Delay 1 min then retry failed files.
                    for (int timer = 60; timer > 0; timer -= 5)
                    {
                        context.Output($"Retry file upload after {timer} seconds.");
                        await Task.Delay(TimeSpan.FromSeconds(5), _uploadCancellationTokenSource.Token);
                    }

                    // Retry upload all failed files.
                    context.Output($"Start retry {uploadResult.RetryFiles.Count} failed files upload.");
                    UploadResult retryUploadResult = await ParallelUploadAsync(context, uploadResult.RetryFiles, maxConcurrentUploads, _uploadCancellationTokenSource.Token);

                    if (retryUploadResult.RetryFiles.Count == 0)
                    {
                        // all files have been upload succeed after retry.
                        context.Output("File upload complete after retry.");
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

        private async Task<DownloadResult> ParallelDownloadAsync(RunnerActionPluginExecutionContext context, IReadOnlyList<DownloadInfo> files, int concurrentDownloads, CancellationToken token)
        {
            // return files that fail to download
            var downloadResult = new DownloadResult();

            // nothing needs to download
            if (files.Count == 0)
            {
                return downloadResult;
            }

            // ensure the file download queue is empty.
            if (!_fileDownloadQueue.IsEmpty)
            {
                throw new ArgumentOutOfRangeException(nameof(_fileDownloadQueue));
            }

            // enqueue file into download queue.
            foreach (var file in files)
            {
                _fileDownloadQueue.Enqueue(file);
            }

            // Start download monitor task.
            _downloadFilesProcessed = 0;
            _downloadFinished = new TaskCompletionSource<int>();
            Task downloadMonitor = DownloadReportingAsync(context, files.Count(), token);

            // Start parallel download tasks.
            List<Task<DownloadResult>> parallelDownloadingTasks = new List<Task<DownloadResult>>();
            for (int downloader = 0; downloader < concurrentDownloads; downloader++)
            {
                parallelDownloadingTasks.Add(DownloadAsync(context, downloader, token));
            }

            // Wait for parallel download finish.
            await Task.WhenAll(parallelDownloadingTasks);
            foreach (var downloadTask in parallelDownloadingTasks)
            {
                // record all failed files.
                downloadResult.AddDownloadResult(await downloadTask);
            }

            // Stop monitor task;
            _downloadFinished.TrySetResult(0);
            await downloadMonitor;

            return downloadResult;
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
            _uploadFilesProcessed = 0;
            _uploadFinished = new TaskCompletionSource<int>();
            _fileUploadTraceLog.Clear();
            _fileUploadProgressLog.Clear();
            Task uploadMonitor = UploadReportingAsync(context, files.Count(), _uploadCancellationTokenSource.Token);

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

        private async Task<DownloadResult> DownloadAsync(RunnerActionPluginExecutionContext context, int downloaderId, CancellationToken token)
        {
            List<DownloadInfo> failedFiles = new List<DownloadInfo>();
            Stopwatch downloadTimer = new Stopwatch();
            while (_fileDownloadQueue.TryDequeue(out DownloadInfo fileToDownload))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    int retryCount = 0;
                    bool downloadFailed = false;
                    while (true)
                    {
                        try
                        {
                            context.Debug($"Start downloading file: '{fileToDownload.ItemPath}' (Downloader {downloaderId})");
                            downloadTimer.Restart();
                            using (FileStream fs = new FileStream(fileToDownload.LocalPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _defaultFileStreamBufferSize, useAsync: true))
                            using (var downloadStream = await _fileContainerHttpClient.DownloadFileAsync(_containerId, fileToDownload.ItemPath, token, _projectId))
                            {
                                await downloadStream.CopyToAsync(fs, _defaultCopyBufferSize, token);
                                await fs.FlushAsync(token);
                                downloadTimer.Stop();
                                context.Debug($"File: '{fileToDownload.LocalPath}' took {downloadTimer.ElapsedMilliseconds} milliseconds to finish download (Downloader {downloaderId})");
                                break;
                            }
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            context.Debug($"Download has been cancelled while downloading {fileToDownload.ItemPath}. (Downloader {downloaderId})");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            context.Warning($"Fail to download '{fileToDownload.ItemPath}', error: {ex.Message} (Downloader {downloaderId})");
                            context.Debug(ex.ToString());
                        }

                        if (retryCount < 3)
                        {
                            var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                            context.Warning($"Back off {backOff.TotalSeconds} seconds before retry. (Downloader {downloaderId})");
                            await Task.Delay(backOff);
                        }
                        else
                        {
                            // upload still failed after 3 tries.
                            downloadFailed = true;
                            break;
                        }
                    }

                    if (downloadFailed)
                    {
                        // tracking file that failed to download.
                        failedFiles.Add(fileToDownload);
                    }

                    Interlocked.Increment(ref _downloadFilesProcessed);
                }
                catch (Exception ex)
                {
                    // We should never
                    context.Error($"Error '{ex.Message}' when downloading file '{fileToDownload}'. (Downloader {downloaderId})");
                    throw ex;
                }
            }

            return new DownloadResult(failedFiles);
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
                        bool failAndExit = false;
                        try
                        {
                            uploadTimer.Restart();
                            using (HttpResponseMessage response = await _fileContainerHttpClient.UploadFileAsync(_containerId, itemPath, fs, _projectId, cancellationToken: token, chunkSize: 4 * 1024 * 1024))
                            {
                                if (response == null || response.StatusCode != HttpStatusCode.Created)
                                {
                                    context.Output($"Unable to copy file to server StatusCode={response?.StatusCode}: {response?.ReasonPhrase}. Source file path: {fileToUpload}. Target server path: {itemPath}");

                                    if (response?.StatusCode == HttpStatusCode.Conflict)
                                    {
                                        // fail upload task but continue with any other files
                                        context.Error($"Error '{fileToUpload}' has already been uploaded.");
                                    }
                                    else if (_fileContainerHttpClient.IsFastFailResponse(response))
                                    {
                                        // Fast fail: we received an http status code where we should abandon our efforts
                                        context.Output($"Cannot continue uploading files, so draining upload queue of {_fileUploadQueue.Count} items.");
                                        DrainUploadQueue(context);
                                        failedFiles.Clear();
                                        failAndExit = true;
                                        throw new UploadFailedException($"Critical failure uploading '{fileToUpload}'");
                                    }
                                    else
                                    {
                                        context.Debug($"Adding '{fileToUpload}' to retry list.");
                                        failedFiles.Add(fileToUpload);
                                    }
                                    throw new UploadFailedException($"Http failure response '{response?.StatusCode}': '{response?.ReasonPhrase}' while uploading '{fileToUpload}'");
                                }

                                uploadTimer.Stop();
                                context.Debug($"File: '{fileToUpload}' took {uploadTimer.ElapsedMilliseconds} milliseconds to finish upload");
                                uploadedSize += fs.Length;
                                OutputLogForFile(context, fileToUpload, $"Detail upload trace for file: {itemPath}", context.Debug);
                            }
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            context.Output($"File upload has been cancelled during upload file: '{fileToUpload}'.");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            context.Output($"Fail to upload '{fileToUpload}' due to '{ex.Message}'.");
                            context.Output(ex.ToString());

                            OutputLogForFile(context, fileToUpload, $"Detail upload trace for file that fail to upload: {itemPath}", context.Output);

                            if (failAndExit)
                            {
                                context.Debug("Exiting upload.");
                                throw;
                            }
                        }
                    }

                    Interlocked.Increment(ref _uploadFilesProcessed);
                }
                catch (Exception ex)
                {
                    context.Output($"File error '{ex.Message}' when uploading file '{fileToUpload}'.");
                    throw ex;
                }
            }

            return new UploadResult(failedFiles, uploadedSize);
        }

        private async Task UploadReportingAsync(RunnerActionPluginExecutionContext context, int totalFiles, CancellationToken token)
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
                    context.Output($"Total file: {totalFiles} ---- Processed file: {_uploadFilesProcessed} ({(_uploadFilesProcessed * 100) / totalFiles}%)");
                }

                await Task.WhenAny(_uploadFinished.Task, Task.Delay(5000, token));
            }
        }

        private async Task DownloadReportingAsync(RunnerActionPluginExecutionContext context, int totalFiles, CancellationToken token)
        {
            int traceInterval = 0;
            while (!_downloadFinished.Task.IsCompleted && !token.IsCancellationRequested)
            {
                // trace total file progress every 10 seconds when there is no file level detail progress
                if (++traceInterval % 2 == 0)
                {
                    context.Output($"Total file: {totalFiles} ---- Downloaded file: {_downloadFilesProcessed} ({(_downloadFilesProcessed * 100) / totalFiles}%)");
                }

                await Task.WhenAny(_downloadFinished.Task, Task.Delay(5000, token));
            }
        }

        private void DrainUploadQueue(RunnerActionPluginExecutionContext context)
        {
            while (_fileUploadQueue.TryDequeue(out string fileToUpload))
            {
                context.Debug($"Clearing upload queue: '{fileToUpload}'");
                Interlocked.Increment(ref _uploadFilesProcessed);
            }
        }

        private void OutputLogForFile(RunnerActionPluginExecutionContext context, string itemPath, string logDescription, Action<string> log)
        {
            // output detail upload trace for the file.
            ConcurrentQueue<string> logQueue;
            if (_fileUploadTraceLog.TryGetValue(itemPath, out logQueue))
            {
                log(logDescription);
                string message;
                while (logQueue.TryDequeue(out message))
                {
                    log(message);
                }
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
            RetryFiles = new List<string>();
            TotalFileSizeUploaded = 0;
        }

        public UploadResult(List<string> retryFiles, long totalFileSizeUploaded)
        {
            RetryFiles = retryFiles ?? new List<string>();
            TotalFileSizeUploaded = totalFileSizeUploaded;
        }
        public List<string> RetryFiles { get; set; }

        public long TotalFileSizeUploaded { get; set; }

        public void AddUploadResult(UploadResult resultToAdd)
        {
            this.RetryFiles.AddRange(resultToAdd.RetryFiles);
            this.TotalFileSizeUploaded += resultToAdd.TotalFileSizeUploaded;
        }
    }

    public class DownloadInfo
    {
        public DownloadInfo(string itemPath, string localPath)
        {
            this.ItemPath = itemPath;
            this.LocalPath = localPath;
        }

        public string ItemPath { get; set; }
        public string LocalPath { get; set; }
    }

    public class DownloadResult
    {
        public DownloadResult()
        {
            FailedFiles = new List<DownloadInfo>();
        }

        public DownloadResult(List<DownloadInfo> failedFiles)
        {
            FailedFiles = failedFiles;
        }
        public List<DownloadInfo> FailedFiles { get; set; }

        public void AddDownloadResult(DownloadResult resultToAdd)
        {
            this.FailedFiles.AddRange(resultToAdd.FailedFiles);
        }
    }

    public class UploadFailedException : Exception
    {
        public UploadFailedException()
            : base()
        { }

        public UploadFailedException(string message)
            : base(message)
        { }

        public UploadFailedException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}