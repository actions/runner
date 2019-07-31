using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.InteropServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker;
using GitHub.Runner.Common.Capabilities;
using GitHub.Services.WebApi;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(DiagnosticLogManager))]
    public interface IDiagnosticLogManager : IRunnerService
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
    public sealed class DiagnosticLogManager : RunnerService, IDiagnosticLogManager
    {
        private static string DateTimeFormat = "yyyyMMdd-HHmmss";
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
            RunnerSettings settings = configurationStore.GetSettings();
            int runnerId = settings.AgentId;
            string runnerName = settings.AgentName;
            int poolId = settings.PoolId;

            executionContext.Debug("Creating diagnostic log environment file.");
            string environmentFile = Path.Combine(supportFilesFolder, "environment.txt");
#if OS_WINDOWS            
            string content = await GetEnvironmentContent(runnerId, runnerName, message.Steps);
#else            
            string content = GetEnvironmentContent(runnerId, runnerName, message.Steps);
#endif            
            File.WriteAllText(environmentFile, content);

            // Copy worker diagnostic log files
            List<string> workerDiagnosticLogFiles = GetWorkerDiagnosticLogFiles(HostContext.GetDirectory(WellKnownDirectory.Diag), jobStartTimeUtc);
            executionContext.Debug($"Copying {workerDiagnosticLogFiles.Count()} worker diagnostic logs.");

            foreach (string workerLogFile in workerDiagnosticLogFiles)
            {
                ArgUtil.File(workerLogFile, nameof(workerLogFile));

                string destination = Path.Combine(supportFilesFolder, Path.GetFileName(workerLogFile));
                File.Copy(workerLogFile, destination);
            }

            // Copy runner diag log files
            List<string> runnerDiagnosticLogFiles = GetRunnerDiagnosticLogFiles(HostContext.GetDirectory(WellKnownDirectory.Diag), jobStartTimeUtc);
            executionContext.Debug($"Copying {runnerDiagnosticLogFiles.Count()} runner diagnostic logs.");

            foreach (string runnerLogFile in runnerDiagnosticLogFiles)
            {
                ArgUtil.File(runnerLogFile, nameof(runnerLogFile));

                string destination = Path.Combine(supportFilesFolder, Path.GetFileName(runnerLogFile));
                File.Copy(runnerLogFile, destination);
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

            IOUtil.SaveObject(new DiagnosticLogMetadata(runnerName, runnerId, poolId, phaseName, diagnosticsZipFileName, phaseResult), metadataFilePath);

            executionContext.QueueAttachFile(type: CoreAttachmentType.DiagnosticLog, name: metadataFileName, filePath: metadataFilePath);

            executionContext.QueueAttachFile(type: CoreAttachmentType.DiagnosticLog, name: diagnosticsZipFileName, filePath: diagnosticsZipFilePath);

            executionContext.Debug("Diagnostic file upload complete.");
        }

        private string GetTaskResultAsString(TaskResult? taskResult)
        {
            if (!taskResult.HasValue) { return "Unknown"; }

            return taskResult.ToString();
        }

        // The current solution is a hack. We need to rethink this and find a better one.
        // The list of worker log files isn't available from the logger. It's also nested several levels deep.
        // For this solution we deduce the applicable worker log files by comparing their create time to the start time of the job.
        private List<string> GetWorkerDiagnosticLogFiles(string diagnosticFolder, DateTime jobStartTimeUtc)
        {
            // Get all worker log files with a timestamp equal or greater than the start of the job
            var workerLogFiles = new List<string>();
            var directoryInfo = new DirectoryInfo(diagnosticFolder);

            // Sometimes the timing is off between the job start time and the time the worker log file is created.
            // This adds a small buffer that provides some leeway in case the worker log file was created slightly
            // before the time we log as job start time.
            int bufferInSeconds = -30;
            DateTime searchTimeUtc = jobStartTimeUtc.AddSeconds(bufferInSeconds);

            foreach (FileInfo file in directoryInfo.GetFiles().Where(f => f.Name.StartsWith(Constants.Path.WorkerDiagnosticLogPrefix)))
            {
                // The format of the logs is:
                // Worker_20171003-143110-utc.log
                DateTime fileCreateTime = DateTime.ParseExact(
                    s: file.Name.Substring(startIndex: Constants.Path.WorkerDiagnosticLogPrefix.Length, length: DateTimeFormat.Length),
                    format: DateTimeFormat,
                    provider: CultureInfo.InvariantCulture);

                if (fileCreateTime >= searchTimeUtc)
                {
                    workerLogFiles.Add(file.FullName);
                }
            }

            return workerLogFiles;
        }

        private List<string> GetRunnerDiagnosticLogFiles(string diagnosticFolder, DateTime jobStartTimeUtc)
        {
            // Get the newest runner log file that created just before the start of the job
            var runnerLogFiles = new List<string>();
            var directoryInfo = new DirectoryInfo(diagnosticFolder);

            // The runner log that record the start point of the job should created before the job start time.
            // The runner log may get paged if it reach size limit.
            // We will only need upload 1 runner log file in 99%.
            // There might be 1% we need to upload 2 runner log files.
            String recentLog = null;
            DateTime recentTimeUtc = DateTime.MinValue;

            foreach (FileInfo file in directoryInfo.GetFiles().Where(f => f.Name.StartsWith(Constants.Path.RunnerDiagnosticLogPrefix)))
            {
                // The format of the logs is:
                // Runner_20171003-143110-utc.log
                if (DateTime.TryParseExact(
                    s: file.Name.Substring(startIndex: Constants.Path.RunnerDiagnosticLogPrefix.Length, length: DateTimeFormat.Length),
                    format: DateTimeFormat,
                    provider: CultureInfo.InvariantCulture,
                    style: DateTimeStyles.None,
                    result: out DateTime fileCreateTime))
                {
                    // always add log file created after the job start.
                    if (fileCreateTime >= jobStartTimeUtc)
                    {
                        runnerLogFiles.Add(file.FullName);
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
                runnerLogFiles.Add(recentLog);
            }

            return runnerLogFiles;
        }

#if OS_WINDOWS
        private async Task<string> GetEnvironmentContent(int runnerId, string runnerName, IList<Pipelines.JobStep> steps)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Environment file created at(UTC): {DateTime.UtcNow}"); // TODO: Format this like we do in other places.
            builder.AppendLine($"Runner Version: {BuildConstants.RunnerPackage.Version}");
            builder.AppendLine($"Runner Id: {runnerId}");
            builder.AppendLine($"Runner Name: {runnerName}");
            builder.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            builder.AppendLine("Steps:");

            foreach (Pipelines.ActionStep action in steps.OfType<Pipelines.ActionStep>())
            {
                builder.AppendLine($"\tName: {action.Name} Id: {action.Id}");
            }

            // windows defender on/off
            builder.AppendLine($"Defender enabled: {IsDefenderEnabled()}");

            // firewall on/off
            builder.AppendLine($"Firewall enabled: {IsFirewallEnabled()}");

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
#else
        private string GetEnvironmentContent(int runnerId, string runnerName, IList<Pipelines.JobStep> steps)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Environment file created at(UTC): {DateTime.UtcNow}"); // TODO: Format this like we do in other places.
            builder.AppendLine($"Runner Version: {BuildConstants.RunnerPackage.Version}");
            builder.AppendLine($"Runner Id: {runnerId}");
            builder.AppendLine($"Runner Name: {runnerName}");
            builder.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            builder.AppendLine("Steps:");

            foreach (Pipelines.ActionStep action in steps.OfType<Pipelines.ActionStep>())
            {
                builder.AppendLine($"\tName: {action.Name} Id: {action.Id}");
            }

            return builder.ToString();
        }
#endif
    }
}
