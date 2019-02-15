using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Capabilities;
using Microsoft.VisualStudio.Services.WebApi;
using Build2 = Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(DiagnosticLogManager))]
    public interface IDiagnosticLogManager : IAgentService
    {
        Task UploadDiagnosticLogsAsync(IExecutionContext executionContext,
                                  Pipelines.AgentJobRequestMessage message,
                                  DateTime jobStartTimeUtc);
    }

    // This class manages gathering data for support logs, zipping the data, and uploading it.
    // The files are created with the following folder structure:
    // ..\_layout\_work\_temp
    //      \[job name]-support (supportRootFolder)
    //          \files (supportFolder)
    //              ...
    //          support.zip
    public sealed class DiagnosticLogManager : AgentService, IDiagnosticLogManager
    {
        public async Task UploadDiagnosticLogsAsync(IExecutionContext executionContext,
                                         Pipelines.AgentJobRequestMessage message,
                                         DateTime jobStartTimeUtc)
        {
            executionContext.Debug("Starting diagnostic file upload.");

            // Setup folders
            // \_layout\_work\_temp\[jobname-support]
            executionContext.Debug("Setting up diagnostic log folders.");
            string tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);
            ArgUtil.Directory(tempDirectory, nameof(tempDirectory));

            string supportRootFolder = Path.Combine(tempDirectory, message.JobName + "-support");
            Directory.CreateDirectory(supportRootFolder);

            // \_layout\_work\_temp\[jobname-support]\files
            executionContext.Debug("Creating diagnostic log files folder.");
            string supportFilesFolder = Path.Combine(supportRootFolder, "files");
            Directory.CreateDirectory(supportFilesFolder);

            // Create the environment file
            // \_layout\_work\_temp\[jobname-support]\files\environment.txt
            var configurationStore = HostContext.GetService<IConfigurationStore>();
            AgentSettings settings = configurationStore.GetSettings();
            int agentId = settings.AgentId;
            string agentName = settings.AgentName;
            int poolId = settings.PoolId;

            executionContext.Debug("Creating diagnostic log environment file.");
            string environmentFile = Path.Combine(supportFilesFolder, "environment.txt");
#if OS_WINDOWS            
            string content = await GetEnvironmentContent(agentId, agentName, message.Steps);
#else            
            string content = GetEnvironmentContent(agentId, agentName, message.Steps);
#endif            
            File.WriteAllText(environmentFile, content);

            // Create the capabilities file
            var capabilitiesManager = HostContext.GetService<ICapabilitiesManager>();
            Dictionary<string, string> capabilities = await capabilitiesManager.GetCapabilitiesAsync(configurationStore.GetSettings(), default(CancellationToken));
            executionContext.Debug("Creating capabilities file.");
            string capabilitiesFile = Path.Combine(supportFilesFolder, "capabilities.txt");
            string capabilitiesContent = GetCapabilitiesContent(capabilities);
            File.WriteAllText(capabilitiesFile, capabilitiesContent);

            // Copy worker diag log files
            List<string> workerDiagLogFiles = GetWorkerDiagLogFiles(HostContext.GetDirectory(WellKnownDirectory.Diag), jobStartTimeUtc);
            executionContext.Debug($"Copying {workerDiagLogFiles.Count()} worker diag logs.");

            foreach (string workerLogFile in workerDiagLogFiles)
            {
                ArgUtil.File(workerLogFile, nameof(workerLogFile));

                string destination = Path.Combine(supportFilesFolder, Path.GetFileName(workerLogFile));
                File.Copy(workerLogFile, destination);
            }

            // Copy agent diag log files
            List<string> agentDiagLogFiles = GetAgentDiagLogFiles(HostContext.GetDirectory(WellKnownDirectory.Diag), jobStartTimeUtc);
            executionContext.Debug($"Copying {agentDiagLogFiles.Count()} agent diag logs.");

            foreach (string agentLogFile in agentDiagLogFiles)
            {
                ArgUtil.File(agentLogFile, nameof(agentLogFile));

                string destination = Path.Combine(supportFilesFolder, Path.GetFileName(agentLogFile));
                File.Copy(agentLogFile, destination);
            }

            executionContext.Debug("Zipping diagnostic files.");

            string buildNumber = executionContext.Variables.Build_Number ?? "UnknownBuildNumber";
            string buildName = $"Build {buildNumber}";
            string phaseName = executionContext.Variables.System_PhaseDisplayName ?? "UnknownPhaseName";

            // zip the files
            string diagnosticsZipFileName = $"{buildName}-{phaseName}.zip";
            string diagnosticsZipFilePath = Path.Combine(supportRootFolder, diagnosticsZipFileName);
            ZipFile.CreateFromDirectory(supportFilesFolder, diagnosticsZipFilePath);

            // upload the json metadata file
            executionContext.Debug("Uploading diagnostic metadata file.");
            string metadataFileName = $"diagnostics-{buildName}-{phaseName}.json";
            string metadataFilePath = Path.Combine(supportFilesFolder, metadataFileName);
            string phaseResult = GetTaskResultAsString(executionContext.Result);

            IOUtil.SaveObject(new DiagnosticLogMetadata(agentName, agentId, poolId, phaseName, diagnosticsZipFileName, phaseResult), metadataFilePath);

            executionContext.QueueAttachFile(type: CoreAttachmentType.DiagnosticLog, name: metadataFileName, filePath: metadataFilePath);

            executionContext.QueueAttachFile(type: CoreAttachmentType.DiagnosticLog, name: diagnosticsZipFileName, filePath: diagnosticsZipFilePath);

            executionContext.Debug("Diagnostic file upload complete.");
        }

        private string GetCapabilitiesContent(Dictionary<string, string> capabilities)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Capabilities");
            builder.AppendLine("");

            foreach (string key in capabilities.Keys)
            {
                builder.Append(key);

                if (!string.IsNullOrEmpty(capabilities[key]))
                {
                    builder.Append($" = {capabilities[key]}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private string GetTaskResultAsString(TaskResult? taskResult)
        {
            if (!taskResult.HasValue) { return "Unknown"; }

            return taskResult.ToString();
        }

        // The current solution is a hack. We need to rethink this and find a better one.
        // The list of worker log files isn't available from the logger. It's also nested several levels deep.
        // For this solution we deduce the applicable worker log files by comparing their create time to the start time of the job.
        private List<string> GetWorkerDiagLogFiles(string diagFolder, DateTime jobStartTimeUtc)
        {
            // Get all worker log files with a timestamp equal or greater than the start of the job
            var workerLogFiles = new List<string>();
            var directoryInfo = new DirectoryInfo(diagFolder);

            // Sometimes the timing is off between the job start time and the time the worker log file is created.
            // This adds a small buffer that provides some leeway in case the worker log file was created slightly
            // before the time we log as job start time.
            int bufferInSeconds = -30;
            DateTime searchTimeUtc = jobStartTimeUtc.AddSeconds(bufferInSeconds);

            foreach (FileInfo file in directoryInfo.GetFiles().Where(f => f.Name.StartsWith("Worker_")))
            {
                // The format of the logs is:
                // Worker_20171003-143110-utc.log
                DateTime fileCreateTime = DateTime.ParseExact(s: file.Name.Substring(startIndex: 7, length: 15), format: "yyyyMMdd-HHmmss", provider: CultureInfo.InvariantCulture);

                if (fileCreateTime >= searchTimeUtc)
                {
                    workerLogFiles.Add(file.FullName);
                }
            }

            return workerLogFiles;
        }

        private List<string> GetAgentDiagLogFiles(string diagFolder, DateTime jobStartTimeUtc)
        {
            // Get the newest agent log file that created just before the start of the job
            var agentLogFiles = new List<string>();
            var directoryInfo = new DirectoryInfo(diagFolder);

            // The agent log that record the start point of the job should created before the job start time.
            // The agent log may get paged if it reach size limit.
            // We will only need upload 1 agent log file in 99%.
            // There might be 1% we need to upload 2 agent log files.
            String recentLog = null;
            DateTime recentTimeUtc = DateTime.MinValue;

            foreach (FileInfo file in directoryInfo.GetFiles().Where(f => f.Name.StartsWith("Agent_")))
            {
                // The format of the logs is:
                // Agent_20171003-143110-utc.log
                if (DateTime.TryParseExact(s: file.Name.Substring(startIndex: 6, length: 15), format: "yyyyMMdd-HHmmss", provider: CultureInfo.InvariantCulture, style: DateTimeStyles.None, result: out DateTime fileCreateTime))
                {
                    // always add log file created after the job start.
                    if (fileCreateTime >= jobStartTimeUtc)
                    {
                        agentLogFiles.Add(file.FullName);
                    }
                    else if (fileCreateTime > recentTimeUtc)
                    {
                        recentLog = file.FullName;
                        recentTimeUtc = fileCreateTime;
                    }
                }
            }

            if (!String.IsNullOrEmpty(recentLog))
            {
                agentLogFiles.Add(recentLog);
            }

            return agentLogFiles;
        }

#if OS_WINDOWS
        private async Task<string> GetEnvironmentContent(int agentId, string agentName, IList<Pipelines.JobStep> steps)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Environment file created at(UTC): {DateTime.UtcNow}"); // TODO: Format this like we do in other places.
            builder.AppendLine($"Agent Version: {BuildConstants.AgentPackage.Version}");
            builder.AppendLine($"Agent Id: {agentId}");
            builder.AppendLine($"Agent Name: {agentName}");
            builder.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            builder.AppendLine("Steps:");

            foreach (Pipelines.TaskStep task in steps.OfType<Pipelines.TaskStep>())
            {
                builder.AppendLine($"\tName: {task.Reference.Name} Version: {task.Reference.Version}");
            }

            // windows defender on/off
            builder.AppendLine($"Defender enabled: {IsDefenderEnabled()}");

            // firewall on/off
            builder.AppendLine($"Firewall enabled: {IsFirewallEnabled()}");

            // $psversiontable
            builder.AppendLine("Powershell Version Info:");
            builder.AppendLine(await GetPsVersionInfo());
            return builder.ToString();
        }

        // Returns whether or not Windows Defender is running.
        private bool IsDefenderEnabled()
        {
            return Process.GetProcessesByName("MsMpEng.exe").FirstOrDefault() != null;
        }

        // Returns whether or not the Windows firewall is enabled.
        private bool IsFirewallEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile"))
                {
                    if (key == null) { return false; }

                    Object o = key.GetValue("EnableFirewall");
                    if (o == null) { return false; }

                    int firewall = (int)o;
                    if (firewall == 1) { return true; }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GetPsVersionInfo()
        {
            var builder = new StringBuilder();

            string powerShellExe = HostContext.GetService<IPowerShellExeUtil>().GetPath();
            string arguments = @"Write-Host ($PSVersionTable | Out-String)";
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs args) =>
                {
                    builder.AppendLine(args.Data);
                };

                processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs args) =>
                {
                    builder.AppendLine(args.Data);
                };

                await processInvoker.ExecuteAsync(
                    workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Bin),
                    fileName: powerShellExe,
                    arguments: arguments,
                    environment: null,
                    requireExitCodeZero: false,
                    outputEncoding: null,
                    killProcessOnCancel: false,
                    cancellationToken: default(CancellationToken));
            }

            return builder.ToString();
        }
#else
        private string GetEnvironmentContent(int agentId, string agentName, IList<Pipelines.JobStep> steps)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Environment file created at(UTC): {DateTime.UtcNow}"); // TODO: Format this like we do in other places.
            builder.AppendLine($"Agent Version: {BuildConstants.AgentPackage.Version}");
            builder.AppendLine($"Agent Id: {agentId}");
            builder.AppendLine($"Agent Name: {agentName}");
            builder.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            builder.AppendLine("Steps:");

            foreach (Pipelines.TaskStep task in steps.OfType<Pipelines.TaskStep>())
            {
                builder.AppendLine($"\tName: {task.Reference.Name} Version: {task.Reference.Version}");
            }

            return builder.ToString();
        }
#endif
    }
}