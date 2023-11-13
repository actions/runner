using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Listener
{
    // This class is a fork of SelfUpdater.cs and is intended to only be used for the
    // new self-update flow where the PackageMetadata is sent in the message directly. 
    // Forking the class prevents us from accidentally breaking the old flow while it's still in production

    [ServiceLocator(Default = typeof(SelfUpdaterV2))]
    public interface ISelfUpdaterV2 : IRunnerService
    {
        bool Busy { get; }
        Task<bool> SelfUpdate(RunnerRefreshMessage updateMessage, IJobDispatcher jobDispatcher, bool restartInteractiveRunner, CancellationToken token);
    }
    public class SelfUpdaterV2 : RunnerService, ISelfUpdaterV2
    {
        private static string _platform = BuildConstants.RunnerPackage.PackageName;
        private ITerminal _terminal;
        private IRunnerServer _runnerServer;
        private int _poolId;
        private ulong _agentId;
        private readonly ConcurrentQueue<string> _updateTrace = new();
        public bool Busy { get; private set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _terminal = hostContext.GetService<ITerminal>();
            _runnerServer = HostContext.GetService<IRunnerServer>();
            var configStore = HostContext.GetService<IConfigurationStore>();
            var settings = configStore.GetSettings();
            _poolId = settings.PoolId;
            _agentId = settings.AgentId;
        }

        public async Task<bool> SelfUpdate(RunnerRefreshMessage updateMessage, IJobDispatcher jobDispatcher, bool restartInteractiveRunner, CancellationToken token)
        {
            Busy = true;
            try
            {
                var totalUpdateTime = Stopwatch.StartNew();

                Trace.Info($"An update is available.");
                _updateTrace.Enqueue($"RunnerPlatform: {updateMessage.OS}");

                // Print console line that warn user not shutdown runner.
                _terminal.WriteLine("Runner update in progress, do not shutdown runner.");
                _terminal.WriteLine($"Downloading {updateMessage.TargetVersion} runner");

                await DownloadLatestRunner(token, updateMessage.TargetVersion, updateMessage.DownloadUrl, updateMessage.SHA256Checksum, updateMessage.OS);
                Trace.Info($"Download latest runner and unzip into runner root.");

                // wait till all running job finish
                _terminal.WriteLine("Waiting for current job finish running.");

                await jobDispatcher.WaitAsync(token);
                Trace.Info($"All running job has exited.");

                // We need to keep runner backup around for macOS until we fixed https://github.com/actions/runner/issues/743
                // delete runner backup
                var stopWatch = Stopwatch.StartNew();
                DeletePreviousVersionRunnerBackup(token, updateMessage.TargetVersion);
                Trace.Info($"Delete old version runner backup.");
                stopWatch.Stop();
                // generate update script from template
                _updateTrace.Enqueue($"DeleteRunnerBackupTime: {stopWatch.ElapsedMilliseconds}ms");
                _terminal.WriteLine("Generate and execute update script.");

                string updateScript = GenerateUpdateScript(restartInteractiveRunner, updateMessage.TargetVersion);
                Trace.Info($"Generate update script into: {updateScript}");


                // For L0, we will skip execute update script.
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GITHUB_ACTION_EXECUTE_UPDATE_SCRIPT")))
                {
                    string flagFile = "update.finished";
                    IOUtil.DeleteFile(flagFile);
                    // kick off update script
                    Process invokeScript = new();
#if OS_WINDOWS
                    invokeScript.StartInfo.FileName = WhichUtil.Which("cmd.exe", trace: Trace);
                    invokeScript.StartInfo.Arguments = $"/c \"{updateScript}\"";
#elif (OS_OSX || OS_LINUX)
                    invokeScript.StartInfo.FileName = WhichUtil.Which("bash", trace: Trace);
                    invokeScript.StartInfo.Arguments = $"\"{updateScript}\"";
#endif
                    invokeScript.Start();
                    Trace.Info($"Update script start running");
                }

                totalUpdateTime.Stop();

                _updateTrace.Enqueue($"TotalUpdateTime: {totalUpdateTime.ElapsedMilliseconds}ms");
                _terminal.WriteLine("Runner will exit shortly for update, should be back online within 10 seconds.");

                return true;
            }
            catch (Exception ex)
            {
                _updateTrace.Enqueue(ex.ToString());
                throw;
            }
            finally
            {
                _terminal.WriteLine("Runner update process finished.");
                Busy = false;
            }
        }

        /// <summary>
        /// _work
        ///     \_update
        ///            \bin
        ///            \externals
        ///            \run.sh
        ///            \run.cmd
        ///            \package.zip //temp download .zip/.tar.gz
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task DownloadLatestRunner(CancellationToken token, string targetVersion, string packageDownloadUrl, string packageHashValue, string targetPlatform)
        {
            string latestRunnerDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.UpdateDirectory);
            IOUtil.DeleteDirectory(latestRunnerDirectory, token);
            Directory.CreateDirectory(latestRunnerDirectory);

            string archiveFile = null;

            // Only try trimmed package if sever sends them and we have calculated hash value of the current runtime/externals.
            _updateTrace.Enqueue($"DownloadUrl: {packageDownloadUrl}");

            try
            {
#if DEBUG
                // Much of the update process (targetVersion, archive) is server-side, this is a way to control it from here for testing specific update scenarios
                // Add files like 'runner2.281.2.tar.gz' or 'runner2.283.0.zip' (depending on your platform) to your runner root folder
                // Note that runners still need to be older than the server's runner version in order to receive an 'AgentRefreshMessage' and trigger this update
                // Wrapped in #if DEBUG as this should not be in the RELEASE build
                if (StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_IS_MOCK_UPDATE")))
                {
                    var waitForDebugger = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_IS_MOCK_UPDATE_WAIT_FOR_DEBUGGER"));
                    if (waitForDebugger)
                    {
                        int waitInSeconds = 20;
                        while (!Debugger.IsAttached && waitInSeconds-- > 0)
                        {
                            await Task.Delay(1000);
                        }
                        Debugger.Break();
                    }

                    if (targetPlatform.StartsWith("win"))
                    {
                        archiveFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"runner{targetVersion}.zip");
                    }
                    else
                    {
                        archiveFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"runner{targetVersion}.tar.gz");
                    }

                    if (File.Exists(archiveFile))
                    {
                        _updateTrace.Enqueue($"Mocking update with file: '{archiveFile}' and targetVersion: '{targetVersion}', nothing is downloaded");
                        _terminal.WriteLine($"Mocking update with file: '{archiveFile}' and targetVersion: '{targetVersion}', nothing is downloaded");
                    }
                    else
                    {
                        archiveFile = null;
                        _terminal.WriteLine($"Mock runner archive not found at {archiveFile} for target version {targetVersion}, proceeding with download instead");
                        _updateTrace.Enqueue($"Mock runner archive not found at {archiveFile} for target version {targetVersion}, proceeding with download instead");
                    }
                }
#endif
                // archiveFile is not null only if we mocked it above
                if (string.IsNullOrEmpty(archiveFile))
                {
                    archiveFile = await DownLoadRunner(latestRunnerDirectory, packageDownloadUrl, packageHashValue, targetPlatform, token);

                    if (string.IsNullOrEmpty(archiveFile))
                    {
                        throw new TaskCanceledException($"Runner package '{packageDownloadUrl}' failed after {Constants.RunnerDownloadRetryMaxAttempts} download attempts");
                    }
                    await ValidateRunnerHash(archiveFile, packageHashValue);
                }

                await ExtractRunnerPackage(archiveFile, latestRunnerDirectory, token);
            }
            finally
            {
                try
                {
                    // delete .zip file
                    if (!string.IsNullOrEmpty(archiveFile) && File.Exists(archiveFile))
                    {
                        Trace.Verbose("Deleting latest runner package zip: {0}", archiveFile);
                        IOUtil.DeleteFile(archiveFile);
                    }
                }
                catch (Exception ex)
                {
                    //it is not critical if we fail to delete the .zip file
                    Trace.Warning("Failed to delete runner package zip '{0}'. Exception: {1}", archiveFile, ex);
                }
            }

            await CopyLatestRunnerToRoot(latestRunnerDirectory, targetVersion, token);
        }

        private async Task<string> DownLoadRunner(string downloadDirectory, string packageDownloadUrl, string packageHashValue, string packagePlatform, CancellationToken token)
        {
            var stopWatch = Stopwatch.StartNew();
            int runnerSuffix = 1;
            string archiveFile = null;
            bool downloadSucceeded = false;

            // Download the runner, using multiple attempts in order to be resilient against any networking/CDN issues
            for (int attempt = 1; attempt <= Constants.RunnerDownloadRetryMaxAttempts; attempt++)
            {
                // Generate an available package name, and do our best effort to clean up stale local zip files
                while (true)
                {
                    if (packagePlatform.StartsWith("win"))
                    {
                        archiveFile = Path.Combine(downloadDirectory, $"runner{runnerSuffix}.zip");
                    }
                    else
                    {
                        archiveFile = Path.Combine(downloadDirectory, $"runner{runnerSuffix}.tar.gz");
                    }

                    try
                    {
                        // delete .zip file
                        if (!string.IsNullOrEmpty(archiveFile) && File.Exists(archiveFile))
                        {
                            Trace.Verbose("Deleting latest runner package zip '{0}'", archiveFile);
                            IOUtil.DeleteFile(archiveFile);
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        // couldn't delete the file for whatever reason, so generate another name
                        Trace.Warning("Failed to delete runner package zip '{0}'. Exception: {1}", archiveFile, ex);
                        runnerSuffix++;
                    }
                }

                // Allow a 15-minute package download timeout, which is good enough to update the runner from a 1 Mbit/s ADSL connection.
                if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_DOWNLOAD_TIMEOUT") ?? string.Empty, out int timeoutSeconds))
                {
                    timeoutSeconds = 15 * 60;
                }

                Trace.Info($"Attempt {attempt}: save latest runner into {archiveFile}.");

                using (var downloadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                using (var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(downloadTimeout.Token, token))
                {
                    try
                    {
                        Trace.Info($"Download runner: begin download");
                        long downloadSize = 0;

                        //open zip stream in async mode
                        using (HttpClient httpClient = new(HostContext.CreateHttpClientHandler()))
                        {
                            Trace.Info($"Downloading {packageDownloadUrl}");

                            using (FileStream fs = new(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                            using (Stream result = await httpClient.GetStreamAsync(packageDownloadUrl))
                            {
                                //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k).
                                await result.CopyToAsync(fs, 81920, downloadCts.Token);
                                await fs.FlushAsync(downloadCts.Token);
                                downloadSize = fs.Length;
                            }
                        }

                        Trace.Info($"Download runner: finished download");
                        downloadSucceeded = true;
                        stopWatch.Stop();
                        _updateTrace.Enqueue($"PackageDownloadTime: {stopWatch.ElapsedMilliseconds}ms");
                        _updateTrace.Enqueue($"Attempts: {attempt}");
                        _updateTrace.Enqueue($"PackageSize: {downloadSize / 1024 / 1024}MB");
                        break;
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        Trace.Info($"Runner download has been cancelled.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (downloadCts.Token.IsCancellationRequested)
                        {
                            Trace.Warning($"Runner download has timed out after {timeoutSeconds} seconds");
                        }

                        Trace.Warning($"Failed to get package '{archiveFile}' from '{packageDownloadUrl}'. Exception {ex}");
                    }
                }
            }

            if (downloadSucceeded)
            {
                return archiveFile;
            }
            else
            {
                return null;
            }
        }

        private async Task ValidateRunnerHash(string archiveFile, string packageHashValue)
        {
            var stopWatch = Stopwatch.StartNew();
            // Validate Hash Matches if it is provided
            using (FileStream stream = File.OpenRead(archiveFile))
            {
                if (!string.IsNullOrEmpty(packageHashValue))
                {
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        byte[] srcHashBytes = await sha256.ComputeHashAsync(stream);
                        var hash = PrimitiveExtensions.ConvertToHexString(srcHashBytes);
                        if (hash != packageHashValue)
                        {
                            // Hash did not match, we can't recover from this, just throw
                            throw new Exception($"Computed runner hash {hash} did not match expected Runner Hash {packageHashValue} for {archiveFile}");
                        }

                        stopWatch.Stop();
                        Trace.Info($"Validated Runner Hash matches {archiveFile} : {packageHashValue}");
                        _updateTrace.Enqueue($"ValidateHashTime: {stopWatch.ElapsedMilliseconds}ms");
                    }
                }
            }
        }

        private async Task ExtractRunnerPackage(string archiveFile, string extractDirectory, CancellationToken token)
        {
            var stopWatch = Stopwatch.StartNew();

            if (archiveFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(archiveFile, extractDirectory);
            }
            else if (archiveFile.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                string tar = WhichUtil.Which("tar", trace: Trace);

                if (string.IsNullOrEmpty(tar))
                {
                    throw new NotSupportedException($"tar -xzf");
                }

                // tar -xzf
                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                {
                    processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Trace.Info(args.Data);
                        }
                    });

                    processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Trace.Error(args.Data);
                        }
                    });

                    int exitCode = await processInvoker.ExecuteAsync(extractDirectory, tar, $"-xzf \"{archiveFile}\"", null, token);
                    if (exitCode != 0)
                    {
                        throw new NotSupportedException($"Can't use 'tar -xzf' to extract archive file: {archiveFile}. return code: {exitCode}.");
                    }
                }
            }
            else
            {
                throw new NotSupportedException($"{archiveFile}");
            }

            stopWatch.Stop();
            Trace.Info($"Finished getting latest runner package at: {extractDirectory}.");
            _updateTrace.Enqueue($"PackageExtractTime: {stopWatch.ElapsedMilliseconds}ms");
        }

        private Task CopyLatestRunnerToRoot(string latestRunnerDirectory, string targetVersion, CancellationToken token)
        {
            var stopWatch = Stopwatch.StartNew();
            // copy latest runner into runner root folder
            // copy bin from _work/_update -> bin.version under root
            string binVersionDir = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"{Constants.Path.BinDirectory}.{targetVersion}");
            Directory.CreateDirectory(binVersionDir);
            Trace.Info($"Copy {Path.Combine(latestRunnerDirectory, Constants.Path.BinDirectory)} to {binVersionDir}.");
            IOUtil.CopyDirectory(Path.Combine(latestRunnerDirectory, Constants.Path.BinDirectory), binVersionDir, token);

            // copy externals from _work/_update -> externals.version under root
            string externalsVersionDir = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"{Constants.Path.ExternalsDirectory}.{targetVersion}");
            Directory.CreateDirectory(externalsVersionDir);
            Trace.Info($"Copy {Path.Combine(latestRunnerDirectory, Constants.Path.ExternalsDirectory)} to {externalsVersionDir}.");
            IOUtil.CopyDirectory(Path.Combine(latestRunnerDirectory, Constants.Path.ExternalsDirectory), externalsVersionDir, token);

            // copy and replace all .sh/.cmd files
            Trace.Info($"Copy any remaining .sh/.cmd files into runner root.");
            foreach (FileInfo file in new DirectoryInfo(latestRunnerDirectory).GetFiles() ?? new FileInfo[0])
            {
                string destination = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), file.Name);

                // Removing the file instead of just trying to overwrite it works around permissions issues on linux.
                // https://github.com/actions/runner/issues/981
                Trace.Info($"Copy {file.FullName} to {destination}");
                IOUtil.DeleteFile(destination);
                file.CopyTo(destination, true);
            }

            stopWatch.Stop();
            _updateTrace.Enqueue($"CopyRunnerToRootTime: {stopWatch.ElapsedMilliseconds}ms");
            return Task.CompletedTask;
        }

        private void DeletePreviousVersionRunnerBackup(CancellationToken token, string targetVersion)
        {
            // delete previous backup runner (back compat, can be remove after serval sprints)
            // bin.bak.2.99.0
            // externals.bak.2.99.0
            foreach (string existBackUp in Directory.GetDirectories(HostContext.GetDirectory(WellKnownDirectory.Root), "*.bak.*"))
            {
                Trace.Info($"Delete existing runner backup at {existBackUp}.");
                try
                {
                    IOUtil.DeleteDirectory(existBackUp, token);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Trace.Error(ex);
                    Trace.Info($"Catch exception during delete backup folder {existBackUp}, ignore this error try delete the backup folder on next auto-update.");
                }
            }

            // delete old bin.2.99.0 folder, only leave the current version and the latest download version
            var allBinDirs = Directory.GetDirectories(HostContext.GetDirectory(WellKnownDirectory.Root), "bin.*");
            if (allBinDirs.Length > 2)
            {
                // there are more than 2 bin.version folder.
                // delete older bin.version folders.
                foreach (var oldBinDir in allBinDirs)
                {
                    if (string.Equals(oldBinDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"bin"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldBinDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"bin.{BuildConstants.RunnerPackage.Version}"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldBinDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"bin.{targetVersion}"), StringComparison.OrdinalIgnoreCase))
                    {
                        // skip for current runner version
                        continue;
                    }

                    Trace.Info($"Delete runner bin folder's backup at {oldBinDir}.");
                    try
                    {
                        IOUtil.DeleteDirectory(oldBinDir, token);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        Trace.Error(ex);
                        Trace.Info($"Catch exception during delete backup folder {oldBinDir}, ignore this error try delete the backup folder on next auto-update.");
                    }
                }
            }

            // delete old externals.2.99.0 folder, only leave the current version and the latest download version
            var allExternalsDirs = Directory.GetDirectories(HostContext.GetDirectory(WellKnownDirectory.Root), "externals.*");
            if (allExternalsDirs.Length > 2)
            {
                // there are more than 2 externals.version folder.
                // delete older externals.version folders.
                foreach (var oldExternalDir in allExternalsDirs)
                {
                    if (string.Equals(oldExternalDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"externals"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldExternalDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"externals.{BuildConstants.RunnerPackage.Version}"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldExternalDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"externals.{targetVersion}"), StringComparison.OrdinalIgnoreCase))
                    {
                        // skip for current runner version
                        continue;
                    }

                    Trace.Info($"Delete runner externals folder's backup at {oldExternalDir}.");
                    try
                    {
                        IOUtil.DeleteDirectory(oldExternalDir, token);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        Trace.Error(ex);
                        Trace.Info($"Catch exception during delete backup folder {oldExternalDir}, ignore this error try delete the backup folder on next auto-update.");
                    }
                }
            }
        }

        private string GenerateUpdateScript(bool restartInteractiveRunner, string targetVersion)
        {
            int processId = Process.GetCurrentProcess().Id;
            string updateLog = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), $"SelfUpdate-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.log");
            string runnerRoot = HostContext.GetDirectory(WellKnownDirectory.Root);

#if OS_WINDOWS
            string templateName = "update.cmd.template";
#else
            string templateName = "update.sh.template";
#endif

            string templatePath = Path.Combine(runnerRoot, $"bin.{targetVersion}", templateName);
            string template = File.ReadAllText(templatePath);

            template = template.Replace("_PROCESS_ID_", processId.ToString());
            template = template.Replace("_RUNNER_PROCESS_NAME_", $"Runner.Listener{IOUtil.ExeExtension}");
            template = template.Replace("_ROOT_FOLDER_", runnerRoot);
            template = template.Replace("_EXIST_RUNNER_VERSION_", BuildConstants.RunnerPackage.Version);
            template = template.Replace("_DOWNLOAD_RUNNER_VERSION_", targetVersion);
            template = template.Replace("_UPDATE_LOG_", updateLog);
            template = template.Replace("_RESTART_INTERACTIVE_RUNNER_", restartInteractiveRunner ? "1" : "0");

#if OS_WINDOWS
            string scriptName = "_update.cmd";
#else
            string scriptName = "_update.sh";
#endif

            string updateScript = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), scriptName);
            if (File.Exists(updateScript))
            {
                IOUtil.DeleteFile(updateScript);
            }

            File.WriteAllText(updateScript, template);
            return updateScript;
        }

        private async Task<string> HashFiles(string fileFolder, CancellationToken token)
        {
            Trace.Info($"Calculating hash for {fileFolder}");

            var stopWatch = Stopwatch.StartNew();
            string binDir = HostContext.GetDirectory(WellKnownDirectory.Bin);
            string node = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), NodeUtil.GetInternalNodeVersion(), "bin", $"node{IOUtil.ExeExtension}");
            string hashFilesScript = Path.Combine(binDir, "hashFiles");
            var hashResult = string.Empty;

            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.ErrorDataReceived += (_, data) =>
                {
                    if (!string.IsNullOrEmpty(data.Data) && data.Data.StartsWith("__OUTPUT__") && data.Data.EndsWith("__OUTPUT__"))
                    {
                        hashResult = data.Data.Substring(10, data.Data.Length - 20);
                        Trace.Info($"Hash result: '{hashResult}'");
                    }
                    else
                    {
                        Trace.Info(data.Data);
                    }
                };

                processInvoker.OutputDataReceived += (_, data) =>
                {
                    Trace.Verbose(data.Data);
                };

                var env = new Dictionary<string, string>
                {
                    ["patterns"] = "**"
                };

                int exitCode = await processInvoker.ExecuteAsync(workingDirectory: fileFolder,
                                              fileName: node,
                                              arguments: $"\"{hashFilesScript.Replace("\"", "\\\"")}\"",
                                              environment: env,
                                              requireExitCodeZero: false,
                                              outputEncoding: null,
                                              killProcessOnCancel: true,
                                              cancellationToken: token);

                if (exitCode != 0)
                {
                    Trace.Error($"hashFiles returns '{exitCode}' failed. Fail to hash files under directory '{fileFolder}'");
                }

                stopWatch.Stop();
                _updateTrace.Enqueue($"{nameof(HashFiles)}{Path.GetFileName(fileFolder)}Time: {stopWatch.ElapsedMilliseconds}ms");
                return hashResult;
            }
        }
    }
}
