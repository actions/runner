using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.FileContainer.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net.Http;
using System.Net;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
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

        public async Task CopyToContainerAsync(
            IAsyncCommandContext context,
            String source,
            CancellationToken cancellationToken)
        {
            //set maxConcurrentUploads up to 2 untill figure out how to use WinHttpHandler.MaxConnectionsPerServer modify DefaultConnectionLimit
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

            context.Output(StringUtil.Loc("TotalUploadFiles", files.Count()));
            using (_uploadCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // hook up reporting event from file container client.
                _fileContainerHttpClient.UploadFileReportTrace += UploadFileTraceReportReceived;
                _fileContainerHttpClient.UploadFileReportProgress += UploadFileProgressReportReceived;

                try
                {
                    // try upload all files for the first time.
                    List<string> failedFiles = await ParallelUploadAsync(context, files, maxConcurrentUploads, _uploadCancellationTokenSource.Token);

                    if (failedFiles.Count == 0)
                    {
                        // all files have been upload suceed.
                        context.Output(StringUtil.Loc("FileUploadSucceed"));
                        return;
                    }
                    else
                    {
                        context.Output(StringUtil.Loc("FileUploadFailedRetryLater", failedFiles.Count));
                    }

                    // Delay 1 min then retry failed files.
                    for (int timer = 60; timer > 0; timer -= 5)
                    {
                        context.Output(StringUtil.Loc("FileUploadRetryInSecond", timer));
                        await Task.Delay(TimeSpan.FromSeconds(5), _uploadCancellationTokenSource.Token);
                    }

                    // Retry upload all failed files.
                    context.Output(StringUtil.Loc("FileUploadRetry", failedFiles.Count));
                    failedFiles = await ParallelUploadAsync(context, failedFiles, maxConcurrentUploads, _uploadCancellationTokenSource.Token);

                    if (failedFiles.Count == 0)
                    {
                        // all files have been upload suceed after retry.
                        context.Output(StringUtil.Loc("FileUploadRetrySucceed"));
                        return;
                    }
                    else
                    {
                        throw new Exception(StringUtil.Loc("FileUploadFailedAfterRetry"));
                    }
                }
                finally
                {
                    _fileContainerHttpClient.UploadFileReportTrace -= UploadFileTraceReportReceived;
                    _fileContainerHttpClient.UploadFileReportProgress -= UploadFileProgressReportReceived;
                }
            }
        }

        private async Task<List<string>> ParallelUploadAsync(IAsyncCommandContext context, List<string> files, int concurrentUploads, CancellationToken token)
        {
            // return files that fail to upload
            List<string> failedFiles = new List<string>();

            // nothing needs to upload
            if (files.Count == 0)
            {
                return failedFiles;
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
            List<Task<List<string>>> parallelUploadingTasks = new List<Task<List<string>>>();
            for (int uploader = 0; uploader < concurrentUploads; uploader++)
            {
                parallelUploadingTasks.Add(UploadAsync(context, uploader, _uploadCancellationTokenSource.Token));
            }

            // Wait for parallel upload finish.
            await Task.WhenAll(parallelUploadingTasks);
            foreach (var uploadTask in parallelUploadingTasks)
            {
                // record all failed files.
                failedFiles.AddRange(await uploadTask);
            }

            // Stop monitor task;
            _uploadFinished.TrySetResult(0);
            await uploadMonitor;

            return failedFiles;
        }

        private async Task<List<string>> UploadAsync(IAsyncCommandContext context, int uploaderId, CancellationToken token)
        {
            List<string> failedFiles = new List<string>();
            string fileToUpload;
            Stopwatch uploadTimer = new Stopwatch();
            while (_fileUploadQueue.TryDequeue(out fileToUpload))
            {
                token.ThrowIfCancellationRequested();
                using (FileStream fs = File.Open(fileToUpload, FileMode.Open, FileAccess.Read))
                {
                    string itemPath = (_containerPath.TrimEnd('/') + "/" + fileToUpload.Remove(0, _sourceParentDirectory.Length + 1)).Replace('\\', '/');
                    uploadTimer.Restart();
                    bool catchExceptionDuringUpload = false;
                    HttpResponseMessage response = null;
                    try
                    {
                        response = await _fileContainerHttpClient.UploadFileAsync(_containerId, itemPath, fs, _projectId, token, chunkSize: 4 * 1024 * 1024);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        context.Output(StringUtil.Loc("FileUploadCancelled", fileToUpload));
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
                        context.Output(StringUtil.Loc("FileUploadFailed", fileToUpload, ex.Message));
                        context.Output(ex.ToString());
                    }

                    uploadTimer.Stop();
                    if (catchExceptionDuringUpload || (response != null && response.StatusCode != HttpStatusCode.Created))
                    {
                        if (response != null)
                        {
                            context.Output(StringUtil.Loc("FileContainerUploadFailed", response.StatusCode, response.ReasonPhrase, fileToUpload, itemPath));
                        }

                        // output detail upload trace for the file.
                        ConcurrentQueue<string> logQueue;
                        if (_fileUploadTraceLog.TryGetValue(itemPath, out logQueue))
                        {
                            context.Output(StringUtil.Loc("FileUploadDetailTrace", itemPath));
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
                        context.Debug(StringUtil.Loc("FileUploadFinish", fileToUpload, uploadTimer.ElapsedMilliseconds));

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

            return failedFiles;
        }

        private async Task ReportingAsync(IAsyncCommandContext context, int totalFiles, CancellationToken token)
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
                    context.Output(StringUtil.Loc("FileUploadProgress", totalFiles, _filesProcessed, (_filesProcessed * 100) / totalFiles));
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
            progressQueue.Enqueue(StringUtil.Loc("FileUploadProgressDetail", e.File, (e.CurrentChunk * 100) / e.TotalChunks));
        }
    }
}