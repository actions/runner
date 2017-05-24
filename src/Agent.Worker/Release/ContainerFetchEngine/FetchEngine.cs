using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//TODO(omeshp) remove these dependencies on agent.
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    /// <summary>
    /// A base class for fetch engines. Handles purging extra files in the destination directory.
    /// </summary>
    public abstract class FetchEngine : IDisposable
    {
        public IReleaseFileSystemManager FileSystemManager { private get; set; }
        protected IContainerProvider Provider { get; }

        protected FetchEngine(
            IContainerProvider containerProvider,
            string rootItemPath,
            string rootDestinationDir)
        {
            RootItemPath = rootItemPath;
            RootDestinationDir = rootDestinationDir;
            Provider = containerProvider;

            ContainerFetchEngineOptions = new ContainerFetchEngineOptions();
            FileSystemManager = new ReleaseFileSystemManager();
            ExecutionLogger = new NullExecutionLogger();
        }

        public void Dispose()
        {
            if (_downloadedFiles > 0)
            {
                ExecutionLogger.Output(StringUtil.Loc("RMDownloadComplete"));
            }

            LogStatistics();
        }

        protected async Task FetchItemsAsync(IEnumerable<ContainerItem> containerItems, CancellationToken token)
        {
            var itemsToDownload = new List<ContainerItem>();

            foreach (ContainerItem item in containerItems)
            {
                if (item.ItemType == ItemType.Folder)
                {
                    string localDirectory = ConvertToLocalPath(item);
                    FileSystemManager.EnsureDirectoryExists(localDirectory);
                }
                else if (item.ItemType == ItemType.File)
                {
                    string localPath = ConvertToLocalPath(item);

                    ExecutionLogger.Debug(StringUtil.Format("[File] {0} => {1}", item.Path, localPath));

                    _totalFiles++;

                    if (item.FileLength == 0)
                    {
                        CreateEmptyFile(localPath);
                        _newEmptyFiles++;
                    }
                    else
                    {
                        itemsToDownload.Add(item);
                    }
                }
                else
                {
                    throw new NotSupportedException(StringUtil.Loc("RMContainerItemNotSupported", item.ItemType));
                }
            }

            if (_totalFiles == 0)
            {
                ExecutionLogger.Warning(StringUtil.Loc("RMArtifactEmpty"));
            }

            if (itemsToDownload.Count > 0)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken =
                    CancellationTokenSource.CreateLinkedTokenSource(token, cancellationTokenSource.Token).Token;

                // Used to limit the number of concurrent downloads.
                SemaphoreSlim downloadThrottle = new SemaphoreSlim(ContainerFetchEngineOptions.ParallelDownloadLimit);
                Stopwatch watch = Stopwatch.StartNew();
                LinkedList<Task> remainingDownloads = new LinkedList<Task>();

                foreach (ContainerItem ticketedItem in itemsToDownload)
                {
                    _bytesDownloaded += ticketedItem.FileLength;
                    Task downloadTask = DownloadItemAsync(downloadThrottle, ticketedItem, cancellationToken);

                    if (downloadTask.IsCompleted)
                    {
                        // don't wait to throw for faulted tasks.
                        await downloadTask.ConfigureAwait(false);
                    }
                    else
                    {
                        remainingDownloads.AddLast(downloadTask);
                    }
                }

                try
                {
                    // Monitor and log the progress of the download tasks if they take over a few seconds.
                    await LogProgressAsync(remainingDownloads).ConfigureAwait(false);
                    cancellationTokenSource.Dispose();
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    await Task.WhenAll(remainingDownloads);
                    cancellationTokenSource.Dispose();
                    throw;
                }

                _elapsedDownloadTime += watch.Elapsed;
            }

            _downloadedFiles += itemsToDownload.Count;
        }

        private void LogStatistics()
        {
            ExecutionLogger.Output(StringUtil.Loc("RMDownloadProgress", _totalFiles, _downloadedFiles, _newEmptyFiles));

            if (_downloadedFiles > 0)
            {
                string message = StringUtil.Loc("RMDownloadProgressDetails", Math.Ceiling(_bytesDownloaded/(1024.0*1024.0)), Math.Floor(_bytesDownloaded/(1024.0*_elapsedDownloadTime.TotalSeconds)), _elapsedDownloadTime);
                ExecutionLogger.Output(message);
            }
        }

        private async Task LogProgressAsync(LinkedList<Task> remainingTasks)
        {
            Stopwatch watch = Stopwatch.StartNew();

            // Log progress until all downloads complete
            while (remainingTasks.Any())
            {
                Task delayTask = Task.Delay(ProgressInterval);

                if (remainingTasks.Count < 20)
                {
                    // temporarily add the delay task.
                    remainingTasks.AddLast(delayTask);

                    // wait for the delay task or a download to complete.
                    // Task.WhenAny is probably an O(n) operation, so we only do this if there's not many downloads remaining.
                    await Task.WhenAny(remainingTasks).ConfigureAwait(false);

                    // remove the delay task.
                    remainingTasks.RemoveLast();
                }
                else
                {
                    // go do something else for 5 seconds.
                    await delayTask.ConfigureAwait(false);
                }

                // remove any download tasks that completed.
                LinkedListNode<Task> task = remainingTasks.First;
                while (task != null)
                {
                    LinkedListNode<Task> nextTask = task.Next;

                    if (task.Value.IsCompleted)
                    {
                        // don't wait to throw for faulted tasks.
                        await task.Value.ConfigureAwait(false);

                        remainingTasks.Remove(task);
                    }

                    task = nextTask;
                }

                // check how many downloads remain.
                if (remainingTasks.Count > 0)
                {
                    if (watch.Elapsed > ProgressInterval)
                    {
                        ExecutionLogger.Output(StringUtil.Loc("RMRemainingDownloads", remainingTasks.Count));
                        watch.Restart();
                    }

                    if (remainingTasks.Count != _previousRemainingTaskCount)
                    {
                        _lastTaskCompletionTime = DateTime.UtcNow;
                        _previousRemainingTaskCount = remainingTasks.Count;
                    }

                    TimeSpan timeSinceLastTaskCompletion = DateTime.UtcNow - _lastTaskCompletionTime;
                    TimeSpan timeSinceLastDiag = DateTime.UtcNow - _lastTaskDiagTime;

                    if (timeSinceLastTaskCompletion > TaskDiagThreshold
                        && timeSinceLastDiag > TaskDiagThreshold)
                    {
                        var taskStates = remainingTasks.GroupBy(dt => dt.Status);

                        lock (_lock)
                        {
                            ExecutionLogger.Warning(StringUtil.Loc("RMDownloadTaskCompletedStatus", (int) timeSinceLastTaskCompletion.TotalMinutes));
                            foreach (IGrouping<TaskStatus, Task> group in taskStates)
                            {
                                ExecutionLogger.Warning(StringUtil.Loc("RMDownloadTaskStates", group.Key, group.Count()));
                            }
                        }

                        _lastTaskDiagTime = DateTime.UtcNow;
                    }
                }
            }
        }

        private Task DownloadItemAsync(
            SemaphoreSlim downloadThrottle,
            ContainerItem ticketedItem,
            CancellationToken cancellationToken)
        {
            string downloadPath = ConvertToLocalPath(ticketedItem);

            return DownloadItemImplAsync(downloadThrottle, ticketedItem, downloadPath, cancellationToken);
        }

        private async Task DownloadItemImplAsync(
            SemaphoreSlim downloadThrottle,
            ContainerItem ticketedItem,
            string downloadPath,
            CancellationToken cancellationToken)
        {
            try
            {
                ExecutionLogger.Debug(StringUtil.Format("Acquiring semaphore to download file {0}", downloadPath));

                await downloadThrottle.WaitAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Download the file content to a temp file on the same drive.
                // Assumption: the build won't produce files ending in .download.
                string tmpDownloadPath = downloadPath + ".download";

                FileSystemManager.EnsureParentDirectory(tmpDownloadPath);
                FileSystemManager.DeleteFile(downloadPath);

                ExecutionLogger.Output(StringUtil.Loc("RMDownloadStartDownloadOfFile", downloadPath));

                await GetFileAsync(ticketedItem, tmpDownloadPath, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // With the content successfully downloaded, move the tmp file to its permanent location.
                FileSystemManager.MoveFile(tmpDownloadPath, downloadPath);
            }
            finally
            {
                downloadThrottle.Release();
            }
        }

        // Wraps our use of the proxy client library
        private async Task GetFileAsync(ContainerItem ticketedItem, string tmpDownloadPath, CancellationToken cancellationToken)
        {
            // Will get doubled on each attempt.
            TimeSpan timeBetweenRetries = ContainerFetchEngineOptions.RetryInterval;

            for (int triesRemaining = ContainerFetchEngineOptions.RetryLimit; ; triesRemaining--)
            {
                bool lastAttempt = (triesRemaining == 0);
                timeBetweenRetries += timeBetweenRetries;

                // Delete the tmp file inbetween attempts
                FileSystemManager.DeleteFile(tmpDownloadPath);

                try
                {
                    Task<Stream> getFileTask = Provider.GetFileTask(ticketedItem, cancellationToken);
                    Task timeoutTask = Task.Delay(ContainerFetchEngineOptions.GetFileAsyncTimeout, cancellationToken);

                    ExecutionLogger.Debug(StringUtil.Format("Fetching contents of file {0}", tmpDownloadPath));

                    // Wait for GetFileAsync or the timeout to elapse.
                    await Task.WhenAny(getFileTask, timeoutTask).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!getFileTask.IsCompleted)
                    {
                        throw new TimeoutException(StringUtil.Loc("RMGetFileAsyncTimedOut", GetFileAsyncTimeoutMinutes));
                    }

                    ExecutionLogger.Debug(StringUtil.Format("Writing contents of file {0} to disk", tmpDownloadPath));

                    using (Stream stream = await getFileTask.ConfigureAwait(false))
                    {
                        await FileSystemManager.WriteStreamToFile(stream, tmpDownloadPath, ContainerFetchEngineOptions.DownloadBufferSize, cancellationToken);
                    }

                    ExecutionLogger.Debug(StringUtil.Format("Finished writing contents of file {0} to disk", tmpDownloadPath));

                    break;
                }
                catch (Exception exception)
                {
                    if (exception is AggregateException)
                    {
                        exception = ((AggregateException)exception).Flatten().InnerException;
                    }

                    if (lastAttempt)
                    {
                        throw new Exception(StringUtil.Loc("RMErrorDownloadingContainerItem", tmpDownloadPath, exception));
                    }

                    lock (_lock)
                    {
                        ExecutionLogger.Warning(StringUtil.Loc("RMReAttemptingDownloadOfContainerItem", tmpDownloadPath, exception));
                    }
                }

                // "Sleep" in between attempts. (Can't await inside a catch clause.)
                await Task.Delay(timeBetweenRetries);
            }
        }

        private void CreateEmptyFile(string downloadPath)
        {
            FileSystemManager.EnsureParentDirectory(downloadPath);
            FileSystemManager.DeleteFile(downloadPath);
            FileSystemManager.CreateEmptyFile(downloadPath);
        }

        private string ConvertToLocalPath(ContainerItem item)
        {
            string localRelativePath;
            if (item.Path.StartsWith(RootItemPath, StringComparison.OrdinalIgnoreCase))
            {
                localRelativePath = item.Path.Substring(RootItemPath.Length).TrimStart('/');
            }
            else
            {
                Debug.Fail(StringUtil.Loc("RMContainerItemPathDoesnotExist", RootItemPath, item.Path));
                localRelativePath = item.Path;
            }

            localRelativePath = localRelativePath.Replace('/', Path.DirectorySeparatorChar);

            if (string.IsNullOrEmpty(localRelativePath) && item.ItemType == ItemType.File)
            {
                //
                // This will only happen when item path matches the RootItemPath.  For directory that is fine (it happens for the root directly) but 
                // for a file it is a little misleading.  When the RootItemPath is a directory we want everything under it (but not the directory itself),
                // but when it is a file, we want the file.
                localRelativePath = FileSystemManager.GetFileName(item.Path);
            }

            return FileSystemManager.JoinPath(RootDestinationDir, localRelativePath);
        }

        public IConatinerFetchEngineLogger ExecutionLogger { get; set; }
        private string RootDestinationDir { get; }
        private string RootItemPath { get; }

        private int _previousRemainingTaskCount;
        private DateTime _lastTaskCompletionTime;
        private DateTime _lastTaskDiagTime;
        private int _totalFiles;
        private int _newEmptyFiles;
        private int _downloadedFiles;
        private TimeSpan _elapsedDownloadTime;
        private long _bytesDownloaded;
        private readonly object _lock = new object();

        private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TaskDiagThreshold = TimeSpan.FromMinutes(1);

        private const int GetFileAsyncTimeoutMinutes = 5;
        public ContainerFetchEngineOptions ContainerFetchEngineOptions { get; set; }
    }
}
