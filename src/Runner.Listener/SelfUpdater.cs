using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

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
        private int _agentId;

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
                if (!await UpdateNeeded(updateMessage.TargetVersion, token))
                {
                    Trace.Info($"Can't find available update package.");
                    return false;
                }

                Trace.Info($"An update is available.");

                // Print console line that warn user not shutdown runner.
                await UpdateRunnerUpdateStateAsync("Runner update in progress, do not shutdown runner.");
                await UpdateRunnerUpdateStateAsync($"Downloading {_targetPackage.Version} runner");

                await DownloadLatestRunner(token);
                Trace.Info($"Download latest runner and unzip into runner root.");

                // wait till all running job finish
                await UpdateRunnerUpdateStateAsync("Waiting for current job finish running.");

                await jobDispatcher.WaitAsync(token);
                Trace.Info($"All running job has exited.");

                // delete runner backup
                DeletePreviousVersionRunnerBackup(token);
                Trace.Info($"Delete old version runner backup.");

                // generate update script from template
                await UpdateRunnerUpdateStateAsync("Generate and execute update script.");

                string updateScript = GenerateUpdateScript(restartInteractiveRunner);
                Trace.Info($"Generate update script into: {updateScript}");

                // kick off update script
                Process invokeScript = new Process();
#if OS_WINDOWS
            invokeScript.StartInfo.FileName = WhichUtil.Which("cmd.exe", trace: Trace);
            invokeScript.StartInfo.Arguments = $"/c \"{updateScript}\"";
#elif (OS_OSX || OS_LINUX)
                invokeScript.StartInfo.FileName = WhichUtil.Which("bash", trace: Trace);
                invokeScript.StartInfo.Arguments = $"\"{updateScript}\"";
#endif
                invokeScript.Start();
                Trace.Info($"Update script start running");

                await UpdateRunnerUpdateStateAsync("Runner will exit shortly for update, should back online within 10 seconds.");

                return true;
            }
            finally
            {
                Busy = false;
            }
        }

        private async Task<bool> UpdateNeeded(string targetVersion, CancellationToken token)
        {
            // when talk to old version server, always prefer latest package.
            // old server won't send target version as part of update message.
            if (string.IsNullOrEmpty(targetVersion))
            {
                var packages = await _runnerServer.GetPackagesAsync(_packageType, _platform, 1, token);
                if (packages == null || packages.Count == 0)
                {
                    Trace.Info($"There is no package for {_packageType} and {_platform}.");
                    return false;
                }

                _targetPackage = packages.FirstOrDefault();
            }
            else
            {
                _targetPackage = await _runnerServer.GetPackageAsync(_packageType, _platform, targetVersion, token);
                if (_targetPackage == null)
                {
                    Trace.Info($"There is no package for {_packageType} and {_platform} with version {targetVersion}.");
                    return false;
                }
            }

            Trace.Info($"Version '{_targetPackage.Version}' of '{_targetPackage.Type}' package available in server.");
            PackageVersion serverVersion = new PackageVersion(_targetPackage.Version);
            Trace.Info($"Current running runner version is {BuildConstants.RunnerPackage.Version}");
            PackageVersion runnerVersion = new PackageVersion(BuildConstants.RunnerPackage.Version);

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
        private async Task DownloadLatestRunner(CancellationToken token)
        {
            string latestRunnerDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.UpdateDirectory);
            IOUtil.DeleteDirectory(latestRunnerDirectory, token);
            Directory.CreateDirectory(latestRunnerDirectory);

            int runnerSuffix = 1;
            string archiveFile = null;
            bool downloadSucceeded = false;

            try
            {
                // Download the runner, using multiple attempts in order to be resilient against any networking/CDN issues
                for (int attempt = 1; attempt <= Constants.RunnerDownloadRetryMaxAttempts; attempt++)
                {
                    // Generate an available package name, and do our best effort to clean up stale local zip files
                    while (true)
                    {
                        if (_targetPackage.Platform.StartsWith("win"))
                        {
                            archiveFile = Path.Combine(latestRunnerDirectory, $"runner{runnerSuffix}.zip");
                        }
                        else
                        {
                            archiveFile = Path.Combine(latestRunnerDirectory, $"runner{runnerSuffix}.tar.gz");
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

                            //open zip stream in async mode
                            using (HttpClient httpClient = new HttpClient(HostContext.CreateHttpClientHandler()))
                            using (FileStream fs = new FileStream(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                            using (Stream result = await httpClient.GetStreamAsync(_targetPackage.DownloadUrl))
                            {
                                //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
                                await result.CopyToAsync(fs, 81920, downloadCts.Token);
                                await fs.FlushAsync(downloadCts.Token);
                            }

                            Trace.Info($"Download runner: finished download");
                            downloadSucceeded = true;
                            break;
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            Trace.Info($"Runner download has been canceled.");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            if (downloadCts.Token.IsCancellationRequested)
                            {
                                Trace.Warning($"Runner download has timed out after {timeoutSeconds} seconds");
                            }

                            Trace.Warning($"Failed to get package '{archiveFile}' from '{_targetPackage.DownloadUrl}'. Exception {ex}");
                        }
                    }
                }

                if (!downloadSucceeded)
                {
                    throw new TaskCanceledException($"Runner package '{archiveFile}' failed after {Constants.RunnerDownloadRetryMaxAttempts} download attempts");
                }

                // If we got this far, we know that we've successfully downloaded the runner package
                if (archiveFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(archiveFile, latestRunnerDirectory);
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

                        int exitCode = await processInvoker.ExecuteAsync(latestRunnerDirectory, tar, $"-xzf \"{archiveFile}\"", null, token);
                        if (exitCode != 0)
                        {
                            throw new NotSupportedException($"Can't use 'tar -xzf' extract archive file: {archiveFile}. return code: {exitCode}.");
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException($"{archiveFile}");
                }

                Trace.Info($"Finished getting latest runner package at: {latestRunnerDirectory}.");
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
                // Copy and replace the file.
                file.CopyTo(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), file.Name), true);
            }
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

            try
            {
                await _runnerServer.UpdateAgentUpdateStateAsync(_poolId, _agentId, currentState);
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
