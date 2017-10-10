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
using Microsoft.VisualStudio.Services.WebApi;
using Build2 = Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(DiagnosticLogManager))]
    public interface IDiagnosticLogManager : IAgentService
    {
        void UploadDiagnosticLogs(IExecutionContext executionContext, 
                                  AgentJobRequestMessage message, 
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
        public void UploadDiagnosticLogs(IExecutionContext executionContext, 
                                         AgentJobRequestMessage message, 
                                         DateTime jobStartTimeUtc)
        {
            executionContext.Debug("Starting diagnostic file upload.");

            // Setup folders
            // \_layout\_work\_temp\[jobname-support]
            executionContext.Debug("Setting up diagnostic log folders.");
            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.TempDirectory);
            ArgUtil.NotNullOrEmpty(tempDirectory, nameof(tempDirectory));
            ArgUtil.Directory(tempDirectory, nameof(tempDirectory));

            string supportRootFolder = Path.Combine(tempDirectory, message.JobName + "-support"); // TODO: Is JobName safe to use as a path? We could just generate a GUID as the name of our scoped folder?
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
            string content = GetEnvironmentContent(agentId, agentName, message.Tasks);
            using (StreamWriter writer = File.CreateText(environmentFile)) 
            {
                writer.Write(content);
            }

            // Copy worker diag log files

            // Sometimes the timing is off between the job start time and the time the worker log file is created.
            // This adds a small buffer that provides some leeway in case the worker log file was created slightly
            // before the time we log as job start time.
            int bufferInSeconds = -30;
            List<string> workerDiagLogFiles = GetWorkerDiagLogFiles(HostContext.GetDirectory(WellKnownDirectory.Diag), jobStartTimeUtc.AddSeconds(bufferInSeconds));
            executionContext.Debug($"Copying {workerDiagLogFiles.Count()} worker diag logs.");

            foreach(string workerLogFile in workerDiagLogFiles)
            {
                ArgUtil.File(workerLogFile, nameof(workerLogFile));

                string destination = Path.Combine(supportFilesFolder, Path.GetFileName(workerLogFile));
                File.Copy(workerLogFile, destination);
            }
            
            executionContext.Debug("Zipping diagnostic files.");

            string buildNumber;
            if (!message.Environment.Variables.TryGetValue(Constants.Variables.Build.Number, out buildNumber))
            {
                buildNumber = "UnknownBuildNumber";
            }
            string buildName = $"Build {buildNumber}";
            
            string phaseName;
            if (!message.Environment.Variables.TryGetValue(Constants.Variables.System.PhaseDisplayName, out phaseName))
            {
                phaseName = "UnknownPhaseName";
            }

            // zip the files
            string diagnosticsZipFileName = $"{buildName}-{phaseName}.zip";
            string diagnosticsZipFilePath = Path.Combine(supportRootFolder, diagnosticsZipFileName);
            ZipFile.CreateFromDirectory(supportFilesFolder, diagnosticsZipFilePath);

            // upload the json metadata file
            executionContext.Debug("Uploading diagnostic metadata file.");
            string metadataFileName = $"diagnostics-{buildName}-{phaseName}.json";
            string metadataFilePath = Path.Combine(supportFilesFolder, metadataFileName);
            string phaseResult = GetTaskResultAsString(executionContext.Result);
            
            using (StreamWriter writer = File.CreateText(metadataFilePath)) 
            {
                writer.Write(JsonUtility.ToString(new DiagnosticLogMetadata(agentName, agentId.ToString(), poolId, phaseName, diagnosticsZipFileName, phaseResult)));
            }

            // CoreAttachmentType.DiagnosticLog
            executionContext.QueueAttachFile(type: "DistributedTask.Core.DiagnosticLog", name: metadataFileName, filePath: metadataFilePath);

            // CoreAttachmentType.DiagnosticLog
            executionContext.QueueAttachFile(type: "DistributedTask.Core.DiagnosticLog", name: diagnosticsZipFileName, filePath: diagnosticsZipFilePath);

            // Delete support folder
            // TODO: We can't delete here. The file upload is queued so they will be gone when its time to upload. Will normal cleanup take care of it?

            executionContext.Debug("Diagnostic file upload complete.");
        }

        // TODO: Is there a better place to put this? I didn't see any code in DT that does this.
        private string GetTaskResultAsString(TaskResult? taskResult)
        {
            if (!taskResult.HasValue) { return "Unknown"; }
            
            switch (taskResult.Value)
            {
                case TaskResult.Abandoned:
                    return "Abandoned";
                case TaskResult.Canceled:
                    return "Canceled";
                case TaskResult.Failed:
                    return "Failed";
                case TaskResult.Skipped:
                    return "Skipped";
                case TaskResult.Succeeded:
                    return "Succeeded";
                case TaskResult.SucceededWithIssues:
                    return "SucceededWithIssues";
                default:
                    return "Unknown";
            }
        }

        // The current solution is a hack. We need to rethink this and find a better one.
        // The list of worker log files isnt available from the logger. It's also nested several levels deep.
        // For this solution we deduce the applicable worker log files by comparing their create time to the start time of the job.
        private List<string> GetWorkerDiagLogFiles(string diagFolder, DateTime jobStartTimeUtc)
        {
            // Get all worker log files with a timestamp equal or greater than the start of the job
            var workerLogFiles = new List<string>();
            var directoryInfo = new DirectoryInfo(diagFolder);
            foreach (FileInfo file in directoryInfo.GetFiles().Where(f => f.Name.StartsWith("Worker_")))
            {
                // The format of the logs is:
                // Worker_20171003-143110-utc.log
                DateTime fileCreateTime = DateTime.ParseExact(s: file.Name.Substring(startIndex: 7, length: 15), format: "yyyyMMdd-HHmmss", provider: CultureInfo.InvariantCulture);

                if (fileCreateTime >= jobStartTimeUtc)
                {
                    workerLogFiles.Add(file.FullName);
                }
            }
            
            return workerLogFiles;
        }

        private string GetEnvironmentContent(int agentId, string agentName, ReadOnlyCollection<TaskInstance> tasks)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Environment file created at(UTC): {DateTime.UtcNow}"); // TODO: Format this like we do in other places.
            builder.AppendLine($"Agent Version: {Constants.Agent.Version}");
            builder.AppendLine($"Agent Id: {agentId}");
            builder.AppendLine($"Agent Name: {agentName}");
            builder.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            builder.AppendLine("Tasks:");

            foreach (TaskInstance task in tasks)
            {
                builder.AppendLine($"\tName: {task.Name} Version: {task.Version}");
            }

#if OS_WINDOWS
            // windows defender on/off
            builder.AppendLine($"Defender enabled: {IsDefenderEnabled()}");

            // firewall on/off
            builder.AppendLine($"Firewall enabled: {IsFirewallEnabled()}");
#endif

            return builder.ToString();
        }

#if OS_WINDOWS
        // Returns whether or not Windows Defender is running.
        private static bool IsDefenderEnabled()
        {
            return Process.GetProcessesByName("MsMpEng.exe").FirstOrDefault() != null;
        }
#endif

#if OS_WINDOWS
        // Returns whether or not the Windows firewall is enabled.
        private static bool IsFirewallEnabled()
        {
            try 
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile")) 
                {
                    if (key == null) { return false; } 

                    Object o = key.GetValue("EnableFirewall");
                    if (o == null) { return false; } 

                    int firewall = (int) o;
                    if (firewall == 1) { return true; } 
                    return false;
                }
            } 
            catch 
            {
                return false;
            }
        }
#endif

        private class DiagnosticLogMetadata
        {
            public DiagnosticLogMetadata(string agentName, string agentId, int poolId, string phaseName, string fileName, string phaseResult)
            {
                AgentName = agentName;
                AgentId = agentId;
                PoolId = poolId;
                PhaseName = phaseName;
                FileName = fileName;
                PhaseResult = phaseResult;
            }

            public string AgentName { get; }

            public string AgentId { get; }

            public int PoolId { get; }

            public string PhaseName { get; }

            public string FileName { get; }

            public string PhaseResult { get; }
        }
    }
}