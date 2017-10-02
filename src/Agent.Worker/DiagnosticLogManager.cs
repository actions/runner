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

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(DiagnosticLogManager))]
    public interface IDiagnosticLogManager : IAgentService
    {
        void UploadDiagnosticLogs(IExecutionContext executionContext, 
                                  string jobName, 
                                  string tempDirectory, /* Can we get this from host context? */
                                  string workerLogFile, 
                                  ReadOnlyCollection<TaskInstance> tasks);
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
                                      string jobName, 
                                      string tempDirectory /* Can we get this from host context? */, 
                                      string workerLogFile, 
                                      ReadOnlyCollection<TaskInstance> tasks)
        {
            if (String.IsNullOrEmpty(tempDirectory)) { throw new ArgumentNullException(nameof(tempDirectory)); }
            if (!Directory.Exists(tempDirectory)) { throw new DirectoryNotFoundException(nameof(tempDirectory)); }
            // if (!File.Exists(workerLogFile)) { throw new FileNotFoundException(nameof(workerLogFile)); } // TODO: Add back.

            executionContext.Debug("Starting diagnostic file upload.");

            // Setup folders
            // \_layout\_work\_temp\[jobname-support]
            executionContext.Debug("Setting up folders.");
            string supportRootFolder = Path.Combine(tempDirectory, jobName + "-support"); // TODO: Is JobName safe to use as a path? We could just generate a GUID as the name of our scoped folder?
            Directory.CreateDirectory(supportRootFolder);

            // \_layout\_work\_temp\[jobname-support]\files
            // TODO: I dont think we need this any more since we arent zipping anything on the agent.
            // TODO2: Yes we need this again. This should upload a zip of diag information for the phase.
            executionContext.Debug("Creating files folder.");
            string supportFilesFolder = Path.Combine(supportRootFolder, "files");
            Directory.CreateDirectory(supportFilesFolder);

            var filesToUpload = new List<string>(); // TODO: Get rid of this.

            // Copy the worker log from the _diag folder into the support folder
            // TODO: The job needs to hold the name of the worker log file?
            // TODO: Add back.
            // executionContext.Debug("Copying worker _diag log file.");
            // string newWorkerLogFile = supportFilesFolder + Path.GetFileName(workerLogFile);
            // File.Copy(workerLogFile, newWorkerLogFile);
            // filesToUpload.Add(newWorkerLogFile);
            
            // Create the environment file
            // \_layout\_work\_temp\[jobname-support]\files\environment.txt

            var configurationStore = HostContext.GetService<IConfigurationStore>();
            AgentSettings settings = configurationStore.GetSettings();
            int agentId = settings.AgentId;
            string agentName = settings.AgentName;

            executionContext.Debug("Creating environment file.");
            string environmentFile = Path.Combine(supportFilesFolder, "environment.txt");
            string content = GetEnvironmentContent(agentId, agentName, tasks);
            using (StreamWriter writer = File.CreateText(environmentFile)) 
            {
                writer.Write(content);
            }
            filesToUpload.Add(environmentFile);
            
            // Download build logs
            // executionContext.Debug("Downloading build logs.");
            // string buildLogsZip = ""; // TODO: Set this.

            // Guid projectId = executionContext.Variables.System_TeamProjectId ?? Guid.Empty;
            // ArgUtil.NotEmpty(projectId, nameof(projectId));

            // int? buildId = executionContext.Variables.Build_BuildId;
            // ArgUtil.NotNull(buildId, nameof(buildId));

            // TODO: Make async?
            // var buildServer = new BuildServer(WorkerUtilities.GetVssConnection(executionContext), projectId);
            // Build2.BuildArtifact artifact = buildServer.DownloadBuildArtifact(
            //     project: projectId.ToString(), // TODO: Not sure if this is right. Other methods take the guid.
            //     buildId: buildId.Value, 
            //     artifactName: ""
            // ).Result;

            // TODO: Do I then download this?
            //artifact.Resource.DownloadUrl

            //filesToUpload.Add(buildLogsZip);

            // Upload support zip
            IJobServerQueue jobServerQueue = HostContext.GetService<IJobServerQueue>();

            // TODO: Keep a list of files to upload and queue them all? Do that if we aren't going to zip them
            // We would iterate all of the files we added to the support folder
            // TODO: Find how we set the name when we normally use this method.
            executionContext.Debug("Zipping diagnostic files.");

            // zip the files
            string diagnosticsZipFileName = "build1-phase1.zip";
            string diagnosticsZipFilePath = Path.Combine(supportRootFolder, diagnosticsZipFileName);
            ZipFile.CreateFromDirectory(supportFilesFolder, diagnosticsZipFilePath);

            // upload the json metadata file
            executionContext.Debug("Uploading diagnostic metadata file.");
            string metadataFileName = "diagnostics-build1-phase1.json";
            // create the file
            string metadataFilePath = Path.Combine(supportFilesFolder, metadataFileName);
            using (StreamWriter writer = File.CreateText(metadataFilePath)) 
            {
                writer.Write(JsonUtility.ToString(new DiagnosticLogMetadata(agentName, agentId.ToString(), "PHASEID", diagnosticsZipFileName)));
            }

            // upload it
            jobServerQueue.QueueFileUpload(
                    timelineId: executionContext.MainTimelineId, 
                    timelineRecordId: executionContext.TimelineId, 
                    // type: CoreAttachmentType.DiagnosticLog, may need to rev dependency version?
                    type: "DistributedTask.Core.DiagnosticLog", 
                    name: metadataFileName, 
                    path: metadataFilePath, 
                    deleteSource: false);

            // upload the diagnostics zip file
            executionContext.Debug("Uploading Diagnostic zip file.");
            jobServerQueue.QueueFileUpload(
                    timelineId: executionContext.MainTimelineId, 
                    timelineRecordId: executionContext.TimelineId, 
                    // type: CoreAttachmentType.DiagnosticLog,
                    type: "DistributedTask.Core.DiagnosticLog", 
                    name: diagnosticsZipFileName, 
                    path: diagnosticsZipFilePath, 
                    deleteSource: false);

            // Delete support folder
            // TODO: We can't delete here. The file upload is queued so they will be gone when its time to upload. Will normal cleanup take care of it?

            executionContext.Debug("Diagnostic file upload complete.");
        }

        private string GetEnvironmentContent(int agentId, string agentName, ReadOnlyCollection<TaskInstance> tasks)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Environment file created at(UTC): {DateTime.UtcNow}"); // TODO: Format this like we do in other places.
            builder.AppendLine("Agent Version: " + ""); // TODO: Get Agent version.
            builder.AppendLine($"Agent Id: {agentId}");
            builder.AppendLine($"Agent Name: {agentName}");
            builder.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            builder.AppendLine("Tasks:");

            foreach (TaskInstance task in tasks)
            {
                builder.AppendLine($"\tName: {task.Name} Version: {task.Version}");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // windows defender on/off
                builder.AppendLine($"Defender enabled: {IsDefenderEnabled()}");

                // firewall on/off
                builder.AppendLine($"Firewall enabled: {IsFirewallEnabled()}");
            }

            // TODO: Add information for tools.
            builder.AppendLine("Tools: "); // TODO: Not sure what goes here

            return builder.ToString();
        }

        // Returns whether or not Windows Defender is running.
        private static bool IsDefenderEnabled()
        {
            return Process.GetProcessesByName("MsMpEng.exe").FirstOrDefault() != null;
        }

        // Returns whether or not the Windows firewall is enabled.
        private static bool IsFirewallEnabled()
        {
            try 
            {
                // TODO: Dont use var here.
                using (var key = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile")) 
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

        private class DiagnosticLogMetadata
        {
            public DiagnosticLogMetadata(string agentName, string agentId, string phaseId, string fileName)
            {
                //ArgUtil.NotNullOrEmpty(agentName, nameof(agentName));
                //ArgUtil.NotNullOrEmpty(agentName, nameof(agentName));
                AgentName = agentName;
                AgentId = agentId;
                PhaseId = phaseId;
                FileName = fileName;
            }

            // TODO: Maybe we only need id?
            public string AgentName { get; }

            public string AgentId { get; }

            public string PhaseId { get; }

            public string FileName { get; }
        }
    }
}