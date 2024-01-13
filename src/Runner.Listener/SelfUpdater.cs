using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Listener
{
    [ServiceLocator(Default = typeof(SelfUpdater))]
    public interface ISelfUpdater : IRunnerService
    {
        bool Busy { get; }
        Task<bool> SelfUpdate(AgentRefreshMessage updateMessage, IJobDispatcher jobDispatcher, bool restartInteractiveRunner, CancellationToken token);
    }

    public class SelfUpdater : RunnerService, ISelfUpdater
    {
        private static string _packageType = "agent";
        private static string _platform = BuildConstants.RunnerPackage.PackageName;

        private PackageMetadata _targetPackage;
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

        public async Task<bool> SelfUpdate(AgentRefreshMessage updateMessage, IJobDispatcher jobDispatcher, bool restartInteractiveRunner, CancellationToken token)
        {
            Busy = true;
            try
            {
                var totalUpdateTime = Stopwatch.StartNew();

                if (!await UpdateNeeded(updateMessage.TargetVersion, token))
                {
                    Trace.Info($"Can't find available update package.");
                    return false;
                }

                Trace.Info($"An update is available.");
                _updateTrace.Enqueue($"RunnerPlatform: {_targetPackage.Platform}");

                // Print console line that warn user not shutdown runner.
                await UpdateRunnerUpdateStateAsync("Runner update in progress, do not shutdown runner.");
                await UpdateRunnerUpdateStateAsync($"Downloading {_targetPackage.Version} runner");

                await DownloadLatestRunner(token, updateMessage.TargetVersion);
                Trace.Info($"Download latest runner and unzip into runner root.");

                // wait till all running job finish
                await UpdateRunnerUpdateStateAsync("Waiting for current job finish running.");

                await jobDispatcher.WaitAsync(token);
                Trace.Info($"All running job has exited.");

                // We need to keep runner backup around for macOS until we fixed https://github.com/actions/runner/issues/743
                // delete runner backup
                var stopWatch = Stopwatch.StartNew();
                DeletePreviousVersionRunnerBackup(token);
                Trace.Info($"Delete old version runner backup.");
                stopWatch.Stop();
                // generate update script from template
                _updateTrace.Enqueue($"DeleteRunnerBackupTime: {stopWatch.ElapsedMilliseconds}ms");
                await UpdateRunnerUpdateStateAsync("Generate and execute update script.");

                string updateScript = GenerateUpdateScript(restartInteractiveRunner);
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
                await UpdateRunnerUpdateStateAsync("Runner will exit shortly for update, should be back online within 10 seconds.");

                return true;
            }
            catch (Exception ex)
            {
                _updateTrace.Enqueue(ex.ToString());
                throw;
            }
            finally
            {
                await UpdateRunnerUpdateStateAsync("Runner update process finished.");
                Busy = false;
            }
        }

        private async Task<bool> UpdateNeeded(string targetVersion, CancellationToken token)
        {
            // when talk to old version server, always prefer latest package.
            // old server won't send target version as part of update message.
            if (string.IsNullOrEmpty(targetVersion))
            {
                var packages = await _runnerServer.GetPackagesAsync(_packageType, _platform, 1, true, token);
                if (packages == null || packages.Count == 0)
                {
                    Trace.Info($"There is no package for {_packageType} and {_platform}.");
                    return false;
                }

                _targetPackage = packages.FirstOrDefault();
            }
            else
            {
                _targetPackage = await _runnerServer.GetPackageAsync(_packageType, _platform, targetVersion, true, token);
                if (_targetPackage == null)
                {
                    Trace.Info($"There is no package for {_packageType} and {_platform} with version {targetVersion}.");
                    return false;
                }
            }

            Trace.Info($"Version '{_targetPackage.Version}' of '{_targetPackage.Type}' package available in server.");
            PackageVersion serverVersion = new(_targetPackage.Version);
            Trace.Info($"Current running runner version is {BuildConstants.RunnerPackage.Version}");
            PackageVersion runnerVersion = new(BuildConstants.RunnerPackage.Version);

            return serverVersion.CompareTo(runnerVersion) > 0;
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
        private async Task DownloadLatestRunner(CancellationToken token, string targetVersion)
        {
            string latestRunnerDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.UpdateDirectory);
            IOUtil.DeleteDirectory(latestRunnerDirectory, token);
            Directory.CreateDirectory(latestRunnerDirectory);

            string archiveFile = null;
            var packageDownloadUrl = _targetPackage.DownloadUrl;
            var packageHashValue = _targetPackage.HashValue;

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

                    if (_targetPackage.Platform.StartsWith("win"))
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
                    archiveFile = await DownLoadRunner(latestRunnerDirectory, packageDownloadUrl, packageHashValue, token);

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

            await CopyLatestRunnerToRoot(latestRunnerDirectory, token);
        }

        private async Task<string> DownLoadRunner(string downloadDirectory, string packageDownloadUrl, string packageHashValue, CancellationToken token)
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
                    if (_targetPackage.Platform.StartsWith("win"))
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
                            if (!string.IsNullOrEmpty(_targetPackage.Token))
                            {
                                Trace.Info($"Adding authorization token ({_targetPackage.Token.Length} chars)");
                                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _targetPackage.Token);
                            }

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

        private Task CopyLatestRunnerToRoot(string latestRunnerDirectory, CancellationToken token)
        {
            var stopWatch = Stopwatch.StartNew();
            // copy latest runner into runner root folder
            // copy bin from _work/_update -> bin.version under root
            string binVersionDir = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"{Constants.Path.BinDirectory}.{_targetPackage.Version}");
            Directory.CreateDirectory(binVersionDir);
            Trace.Info($"Copy {Path.Combine(latestRunnerDirectory, Constants.Path.BinDirectory)} to {binVersionDir}.");
            IOUtil.CopyDirectory(Path.Combine(latestRunnerDirectory, Constants.Path.BinDirectory), binVersionDir, token);

            // copy externals from _work/_update -> externals.version under root
            string externalsVersionDir = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"{Constants.Path.ExternalsDirectory}.{_targetPackage.Version}");
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

        private void DeletePreviousVersionRunnerBackup(CancellationToken token)
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
                        string.Equals(oldBinDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"bin.{_targetPackage.Version}"), StringComparison.OrdinalIgnoreCase))
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
                        string.Equals(oldExternalDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"externals.{_targetPackage.Version}"), StringComparison.OrdinalIgnoreCase))
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

        private string GenerateUpdateScript(bool restartInteractiveRunner)
        {
            int processId = Process.GetCurrentProcess().Id;
            string updateLog = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Diag), $"SelfUpdate-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.log");
            string runnerRoot = HostContext.GetDirectory(WellKnownDirectory.Root);

#if OS_WINDOWS
            string templateName = "update.cmd.template";
#else
            string templateName = "update.sh.template";
#endif

            string templatePath = Path.Combine(runnerRoot, $"bin.{_targetPackage.Version}", templateName);
            string template = File.ReadAllText(templatePath);

            template = template.Replace("_PROCESS_ID_", processId.ToString());
            template = template.Replace("_RUNNER_PROCESS_NAME_", $"Runner.Listener{IOUtil.ExeExtension}");
            template = template.Replace("_ROOT_FOLDER_", runnerRoot);
            template = template.Replace("_EXIST_RUNNER_VERSION_", BuildConstants.RunnerPackage.Version);
            template = template.Replace("_DOWNLOAD_RUNNER_VERSION_", _targetPackage.Version);
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

        private async Task UpdateRunnerUpdateStateAsync(string currentState)
        {
            _terminal.WriteLine(currentState);

            var traces = new List<string>();
            while (_updateTrace.TryDequeue(out var trace))
            {
                traces.Add(trace);
            }

            if (traces.Count > 0)
            {
                foreach (var trace in traces)
                {
                    Trace.Info(trace);
                }
            }

            try
            {
                await _runnerServer.UpdateAgentUpdateStateAsync(_poolId, _agentId, currentState, string.Join(Environment.NewLine, traces));
                _updateTrace.Clear();
            }
            catch (VssResourceNotFoundException)
            {
                // ignore VssResourceNotFoundException, this exception means the runner is configured against a old server that doesn't support report runner update detail.
                Trace.Info($"Catch VssResourceNotFoundException during report update state, ignore this error for backcompat.");
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                Trace.Info($"Catch exception during report update state, ignore this error and continue auto-update.");
            }
        }
    }
}
