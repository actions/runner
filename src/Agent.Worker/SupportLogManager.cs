using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Build2 = Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(SupportLogManager))]
    public interface ISupportLogManager : IAgentService
    {
        void UploadSupportLogs(IExecutionContext executionContext, 
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
    public sealed class SupportLogManager : AgentService, ISupportLogManager
    {
        public void UploadSupportLogs(IExecutionContext executionContext, 
                                      string jobName, 
                                      string tempDirectory /* Can we get this from host context? */, 
                                      string workerLogFile)
        {
            if (String.IsNullOrEmpty(tempDirectory)) { throw new ArgumentNullException(nameof(tempDirectory)); }
            if (!Directory.Exists(tempDirectory)) { throw new DirectoryNotFoundException(nameof(tempDirectory)); }
            if (!File.Exists(workerLogFile)) { throw new FileNotFoundException(nameof(workerLogFile)); }

            // Setup folders
            // \_layout\_work\_temp\[jobname-support]
            executionContext.Debug("Setting up folders.");
            string supportRootFolder = Path.Combine(tempDirectory, jobName + "-support");
            Directory.CreateDirectory(supportRootFolder);

            // \_layout\_work\_temp\[jobname-support]\files
            // TODO: I dont think we need this any more since we arent zipping anything on the agent.
            // executionContext.Debug("Creating files folder.");
            // string supportFilesFolder = Path.Combine(supportRootFolder, "files");
            // Directory.CreateDirectory(supportFilesFolder);

            var filesToUpload = new List<string>();

            // Copy the worker log from the _diag folder into the support folder
            // TODO: The job needs to hold the name of the worker log file?
            executionContext.Debug("Copying worker _diag log file.");
            string newWorkerLogFile = supportRootFolder + Path.GetFileName(workerLogFile);
            File.Copy(workerLogFile, newWorkerLogFile);
            filesToUpload.Add(newWorkerLogFile);
            
            // Create the environment file
            // \_layout\_work\_temp\[jobname-support]\files\environment.txt
            executionContext.Debug("Creating environment file.");
            string environmentFile = Path.Combine(supportRootFolder, "environment.txt");
            string content = GetEnvironmentContent();
            using (StreamWriter writer = File.CreateText(environmentFile)) 
            {
                writer.Write(content);
            }
            filesToUpload.Add(environmentFile);
            
            // Download build logs
            executionContext.Debug("Downloading build logs.");
            string buildLogsZip = ""; // TODO: Set this.

            Guid projectId = executionContext.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            int? buildId = executionContext.Variables.Build_BuildId;
            ArgUtil.NotNull(buildId, nameof(buildId));

            // TODO: Make async?
            var buildServer = new BuildServer(WorkerUtilities.GetVssConnection(executionContext), projectId);
            Build2.BuildArtifact artifact = buildServer.DownloadBuildArtifact(
                project: projectId.ToString(), // TODO: Not sure if this is right. Other methods take the guid.
                buildId: buildId.Value, 
                artifactName: ""
            ).Result;

            // TODO: Do I then download this?
            //artifact.Resource.DownloadUrl

            filesToUpload.Add(buildLogsZip);

            // Upload support zip
            IJobServerQueue jobServerQueue = HostContext.GetService<IJobServerQueue>();

            // TODO: Keep a list of files to upload and queue them all? Do that if we aren't going to zip them
            // We would iterate all of the files we added to the support folder
            // TODO: Find how we set the name when we normally use this method.
            executionContext.Debug("Uploading diagnostic log files.");
            foreach (string fileToUpload in filesToUpload)
            {
                jobServerQueue.QueueFileUpload(
                    timelineId: executionContext.MainTimelineId, 
                    timelineRecordId: executionContext.TimelineId, 
                    // type: CoreAttachmentType.DiagnosticLog,
                    type: "DistributedTask.Core.DiagnosticLog", 
                    name: "SupportLog", 
                    path: fileToUpload, 
                    deleteSource: false);
            }

            // Delete support folder
            executionContext.Debug("Deleting support folder.");
            Directory.Delete(supportRootFolder);
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

            builder.AppendLine("Tools: "); // TODO: Not sure what goes here

            return builder.ToString();
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