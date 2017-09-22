using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.WebApi;
using Build2 = Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(DiagnosticLogManager))]
    public interface IDiagnosticLogManager : IAgentService
    {
        void UploadDiagnosticLogs(IExecutionContext executionContext, 
                                  string jobName, 
                                  string tempDirectory, /* Can we get this from host context? */
                                  string workerLogFile);
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
                                      string workerLogFile)
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
            executionContext.Debug("Creating environment file.");
            string environmentFile = Path.Combine(supportFilesFolder, "environment.txt");
            string content = GetEnvironmentContent();
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
            // foreach (string fileToUpload in filesToUpload)
            // {
                
            // }

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
                writer.Write(JsonUtility.Serialize(new DiagnosticLogMetadata("AGENTNAME", "AGENTID", "PHASEID")));
            }

            // upload it
            jobServerQueue.QueueFileUpload(
                    timelineId: executionContext.MainTimelineId, 
                    timelineRecordId: executionContext.TimelineId, 
                    // type: CoreAttachmentType.DiagnosticLog,
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
            executionContext.Debug("Deleting support folder.");
            Directory.Delete(supportRootFolder);

            executionContext.Debug("Diagnostic file upload complete.");
        }

        private string GetEnvironmentContent()
        {
            // TODO: names and versions of all tasks, agent version, os and version, tools?
            var builder = new StringBuilder();

            builder.AppendLine("Environment file created at: " + DateTime.UtcNow); // TODO: Format this like we do in other places.
            builder.AppendLine("Agent Version: " + ""); // TODO: Get Agent version.
            builder.AppendLine("OS: "); // TODO: Implement.
            builder.AppendLine("OS Version: "); // TODO: Implement.
            builder.AppendLine("Tasks:");

            builder.AppendLine("\tName: " + "TASKNAME" + " Version: " + "TASKVERSION");
            builder.AppendLine("\tName: " + "TASKNAME" + " Version: " + "TASKVERSION");
            builder.AppendLine("\tName: " + "TASKNAME" + " Version: " + "TASKVERSION");

            // TODO: Check windows defender and stuff, do an if and find stuff based on specific OS we are currently running on.

            builder.AppendLine("Tools: "); // TODO: Not sure what goes here

            return builder.ToString();
        }

        private class DiagnosticLogMetadata
        {
            public DiagnosticLogMetadata(string agentName, string agentId, string phaseId)
            {
                //ArgUtil.NotNullOrEmpty(agentName, nameof(agentName));
                //ArgUtil.NotNullOrEmpty(agentName, nameof(agentName));
                AgentName = agentName;
                AgentId = agentId;
                PhaseId = phaseId;
            }

            // TODO: Maybe we only need id?
            public string AgentName { get; }

            public string AgentId { get; }

            public string PhaseId { get; }
        }
    }

    // TODO: This is temporary, we should add this to WebAPI directly.
    // public static class CoreAttachmentTypeExtensions
    // {
    //     public static string Support(this CoreAttachmentType coreAttachmentType)
    //     {
    //         return "DistributedTask.Core.Support";
    //     }
    // }
}