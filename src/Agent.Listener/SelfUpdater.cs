using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(SelfUpdater))]
    public interface ISelfUpdater : IAgentService
    {
        Task<bool> SelfUpdate(IJobDispatcher jobDispatcher, bool restartInteractiveAgent, CancellationToken token);
    }

    public class SelfUpdater : AgentService, ISelfUpdater
    {
        private static string _packageType = "agent";
        private static string _platform = BuildConstants.AgentPackage.PackageName;

        private PackageMetadata _latestPackage;

        public async Task<bool> SelfUpdate(IJobDispatcher jobDispatcher, bool restartInteractiveAgent, CancellationToken token)
        {
            if (!await UpdateNeeded(token))
            {
                Trace.Info($"Can't find availiable update package.");
                return false;
            }

            Trace.Info($"An update is availiable.");

            // Print console line that warn user not shutdown agent.
            var terminal = HostContext.GetService<ITerminal>();
            terminal.WriteLine(StringUtil.Loc("UpdateInProgress"));

            terminal.WriteLine(StringUtil.Loc("DownloadAgent"));
            await DownloadLatestAgent(token);
            Trace.Info($"Download latest agent and unzip into agent root.");

            // wait till all running job finish
            terminal.WriteLine(StringUtil.Loc("EnsureJobFinished"));
            await jobDispatcher.WaitAsync(token);
            Trace.Info($"All running job has exited.");

            // delete agent backup
            DeletePreviousVersionAgentBackup(token);
            Trace.Info($"Delete old version agent backup.");

            // generate update script from template
            terminal.WriteLine(StringUtil.Loc("GenerateAndRunUpdateScript"));
            string updateScript = GenerateUpdateScript(restartInteractiveAgent);
            Trace.Info($"Generate update script into: {updateScript}");

            // kick off update script
            Process invokeScript = new Process();
            var whichUtil = HostContext.GetService<IWhichUtil>();
#if OS_WINDOWS
            invokeScript.StartInfo.FileName = whichUtil.Which("cmd.exe");
            invokeScript.StartInfo.Arguments = $"/c \"{updateScript}\"";
#elif (OS_OSX || OS_LINUX)
            invokeScript.StartInfo.FileName = whichUtil.Which("bash");
            invokeScript.StartInfo.Arguments = $"\"{updateScript}\"";
#endif
            invokeScript.Start();
            Trace.Info($"Update script start running");
            terminal.WriteLine(StringUtil.Loc("AgentExit"));

            return true;
        }

        private async Task<bool> UpdateNeeded(CancellationToken token)
        {
            var agentServer = HostContext.GetService<IAgentServer>();
            var packages = await agentServer.GetPackagesAsync(_packageType, _platform, 1, token);
            if (packages == null || packages.Count == 0)
            {
                Trace.Info($"There is no package for {_packageType} and {_platform}.");
                return false;
            }

            _latestPackage = packages.FirstOrDefault();
            Trace.Info($"Latest version of '{_latestPackage.Type}' package availiable in server is {_latestPackage.Version}");
            PackageVersion serverVersion = new PackageVersion(_latestPackage.Version);

            Trace.Info($"Current running agent version is {Constants.Agent.Version}");
            PackageVersion agentVersion = new PackageVersion(Constants.Agent.Version);

            return serverVersion.CompareTo(agentVersion) > 0;
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
        private async Task DownloadLatestAgent(CancellationToken token)
        {
            var agentServer = HostContext.GetService<IAgentServer>();
            string latestAgentDirectory = IOUtil.GetUpdatePath(HostContext);
            IOUtil.DeleteDirectory(latestAgentDirectory, token);
            Directory.CreateDirectory(latestAgentDirectory);

            string archiveFile;
            if (_latestPackage.Platform.StartsWith("win"))
            {
                archiveFile = Path.Combine(latestAgentDirectory, "agent.zip");
            }
            else
            {
                archiveFile = Path.Combine(latestAgentDirectory, "agent.tar.gz");
            }

            Trace.Info($"Save latest agent into {archiveFile}.");
            try
            {
                var proxyConfig = HostContext.GetService<IProxyConfiguration>();
                using (var httpClient = new HttpClient(proxyConfig.HttpClientHandlerWithProxySetting))
                {
                    //open zip stream in async mode
                    using (FileStream fs = new FileStream(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        using (Stream result = await httpClient.GetStreamAsync(_latestPackage.DownloadUrl))
                        {
                            //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
                            await result.CopyToAsync(fs, 81920, token);
                            await fs.FlushAsync(token);
                        }
                    }
                }

                if (archiveFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(archiveFile, latestAgentDirectory);
                }
                else if (archiveFile.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                {
                    var whichUtil = HostContext.GetService<IWhichUtil>();
                    string tar = whichUtil.Which("tar");
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

                        int exitCode = await processInvoker.ExecuteAsync(latestAgentDirectory, tar, $"-xzf \"{archiveFile}\"", null, token);
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

                Trace.Info($"Finished getting latest agent package at: {latestAgentDirectory}.");
            }
            finally
            {
                try
                {
                    // delete .zip file
                    if (!string.IsNullOrEmpty(archiveFile) && File.Exists(archiveFile))
                    {
                        Trace.Verbose("Deleting latest agent package zip: {0}", archiveFile);
                        IOUtil.DeleteFile(archiveFile);
                    }
                }
                catch (Exception ex)
                {
                    //it is not critical if we fail to delete the temp folder
                    Trace.Warning("Failed to delete agent package zip '{0}'. Exception: {1}", archiveFile, ex);
                }
            }

            // copy latest agent into agent root folder
            // copy bin from _work/_update -> bin.version under root
            string binVersionDir = Path.Combine(IOUtil.GetRootPath(), $"{Constants.Path.BinDirectory}.{_latestPackage.Version}");
            Directory.CreateDirectory(binVersionDir);
            Trace.Info($"Copy {Path.Combine(latestAgentDirectory, Constants.Path.BinDirectory)} to {binVersionDir}.");
            IOUtil.CopyDirectory(Path.Combine(latestAgentDirectory, Constants.Path.BinDirectory), binVersionDir, token);

            // copy externals from _work/_update -> externals.version under root
            string externalsVersionDir = Path.Combine(IOUtil.GetRootPath(), $"{Constants.Path.ExternalsDirectory}.{_latestPackage.Version}");
            Directory.CreateDirectory(externalsVersionDir);
            Trace.Info($"Copy {Path.Combine(latestAgentDirectory, Constants.Path.ExternalsDirectory)} to {externalsVersionDir}.");
            IOUtil.CopyDirectory(Path.Combine(latestAgentDirectory, Constants.Path.ExternalsDirectory), externalsVersionDir, token);

            // copy and replace all .sh/.cmd files
            Trace.Info($"Copy any remaining .sh/.cmd files into agent root.");
            foreach (FileInfo file in new DirectoryInfo(latestAgentDirectory).GetFiles() ?? new FileInfo[0])
            {
                // Copy and replace the file.
                file.CopyTo(Path.Combine(IOUtil.GetRootPath(), file.Name), true);
            }

            // for windows service back compat with old windows agent, we need make sure the servicehost.exe is still the old name
            // if the current bin folder has VsoAgentService.exe, then the new agent bin folder needs VsoAgentService.exe as well
#if OS_WINDOWS
            if (File.Exists(Path.Combine(IOUtil.GetBinPath(), "VsoAgentService.exe")))
            {
                Trace.Info($"Make a copy of AgentService.exe, name it VsoAgentService.exe");
                File.Copy(Path.Combine(binVersionDir, "AgentService.exe"), Path.Combine(binVersionDir, "VsoAgentService.exe"), true);
                File.Copy(Path.Combine(binVersionDir, "AgentService.exe.config"), Path.Combine(binVersionDir, "VsoAgentService.exe.config"), true);

                Trace.Info($"Make a copy of Agent.Listener.exe, name it VsoAgent.exe");
                File.Copy(Path.Combine(binVersionDir, "Agent.Listener.exe"), Path.Combine(binVersionDir, "VsoAgent.exe"), true);
                File.Copy(Path.Combine(binVersionDir, "Agent.Listener.dll"), Path.Combine(binVersionDir, "VsoAgent.dll"), true);

                // in case of we remove all pdb file from agent package.
                if (File.Exists(Path.Combine(binVersionDir, "AgentService.pdb")))
                {
                    File.Copy(Path.Combine(binVersionDir, "AgentService.pdb"), Path.Combine(binVersionDir, "VsoAgentService.pdb"), true);
                }

                if (File.Exists(Path.Combine(binVersionDir, "Agent.Listener.pdb")))
                {
                    File.Copy(Path.Combine(binVersionDir, "Agent.Listener.pdb"), Path.Combine(binVersionDir, "VsoAgent.pdb"), true);
                }
            }
#endif
        }

        private void DeletePreviousVersionAgentBackup(CancellationToken token)
        {
            // delete previous backup agent (back compat, can be remove after serval sprints)
            // bin.bak.2.99.0
            // externals.bak.2.99.0
            foreach (string existBackUp in Directory.GetDirectories(IOUtil.GetRootPath(), "*.bak.*"))
            {
                Trace.Info($"Delete existing agent backup at {existBackUp}.");
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
            var allBinDirs = Directory.GetDirectories(IOUtil.GetRootPath(), "bin.*");
            if (allBinDirs.Length > 2)
            {
                // there are more than 2 bin.version folder.
                // delete older bin.version folders.
                foreach (var oldBinDir in allBinDirs)
                {
                    if (string.Equals(oldBinDir, Path.Combine(IOUtil.GetRootPath(), $"bin"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldBinDir, Path.Combine(IOUtil.GetRootPath(), $"bin.{Constants.Agent.Version}"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldBinDir, Path.Combine(IOUtil.GetRootPath(), $"bin.{_latestPackage.Version}"), StringComparison.OrdinalIgnoreCase))
                    {
                        // skip for current agent version
                        continue;
                    }

                    Trace.Info($"Delete agent bin folder's backup at {oldBinDir}.");
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
            var allExternalsDirs = Directory.GetDirectories(IOUtil.GetRootPath(), "externals.*");
            if (allExternalsDirs.Length > 2)
            {
                // there are more than 2 externals.version folder.
                // delete older externals.version folders.
                foreach (var oldExternalDir in allExternalsDirs)
                {
                    if (string.Equals(oldExternalDir, Path.Combine(IOUtil.GetRootPath(), $"externals"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldExternalDir, Path.Combine(IOUtil.GetRootPath(), $"externals.{Constants.Agent.Version}"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldExternalDir, Path.Combine(IOUtil.GetRootPath(), $"externals.{_latestPackage.Version}"), StringComparison.OrdinalIgnoreCase))
                    {
                        // skip for current agent version
                        continue;
                    }

                    Trace.Info($"Delete agent externals folder's backup at {oldExternalDir}.");
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

        private string GenerateUpdateScript(bool restartInteractiveAgent)
        {
            int processId = Process.GetCurrentProcess().Id;
            string updateLog = Path.Combine(IOUtil.GetDiagPath(), $"SelfUpdate-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.log");
            string agentRoot = IOUtil.GetRootPath();

#if OS_WINDOWS
            string templateName = "update.cmd.template";
#else
            string templateName = "update.sh.template";
#endif

            string templatePath = Path.Combine(agentRoot, $"bin.{_latestPackage.Version}", templateName);
            string template = File.ReadAllText(templatePath);

            template = template.Replace("_PROCESS_ID_", processId.ToString());
            template = template.Replace("_AGENT_PROCESS_NAME_", $"Agent.Listener{IOUtil.ExeExtension}");
            template = template.Replace("_ROOT_FOLDER_", agentRoot);
            template = template.Replace("_EXIST_AGENT_VERSION_", Constants.Agent.Version);
            template = template.Replace("_DOWNLOAD_AGENT_VERSION_", _latestPackage.Version);
            template = template.Replace("_UPDATE_LOG_", updateLog);
            template = template.Replace("_RESTART_INTERACTIVE_AGENT_", restartInteractiveAgent ? "1" : "0");

#if OS_WINDOWS
            string scriptName = "_update.cmd";
#else
            string scriptName = "_update.sh";
#endif

            string updateScript = Path.Combine(IOUtil.GetWorkPath(HostContext), scriptName);
            if (File.Exists(updateScript))
            {
                IOUtil.DeleteFile(updateScript);
            }

            File.WriteAllText(updateScript, template);
            return updateScript;
        }
    }
}
