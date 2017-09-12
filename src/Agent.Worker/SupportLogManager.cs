using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(SupportLogManager))]
    public interface ISupportLogManager
    {
        void UploadSupportLogs(IExecutionContext executionContext, 
                               HostContext hostContext, 
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
    public class SupportLogManager
    {
        public void UploadSupportLogs(IExecutionContext executionContext, 
                                      HostContext hostContext, 
                                      string jobName, 
                                      string tempDirectory /* Can we get this from host context? */, 
                                      string workerLogFile)
        {
            if (String.IsNullOrEmpty(tempDirectory)) { throw new ArgumentNullException(nameof(tempDirectory)); }
            if (!Directory.Exists(tempDirectory)) { throw new DirectoryNotFoundException(nameof(tempDirectory)); }
            if (!File.Exists(workerLogFile)) { throw new FileNotFoundException(nameof(workerLogFile)); }

            // Setup folders
            // \_layout\_work\_temp\[jobname-support]
            string supportRootFolder = Path.Combine(tempDirectory, jobName + "-support");
            Directory.CreateDirectory(supportRootFolder);

            // \_layout\_work\_temp\[jobname-support]\files
            string supportFilesFolder = Path.Combine(supportRootFolder, "files");
            Directory.CreateDirectory(supportFilesFolder);

            var filesToUpload = new List<string>();

            // Copy the worker log from the _diag folder into the support folder
            // TODO: The job needs to hold the name of the worker log file?
            string newWorkerLogFile = supportFilesFolder + Path.GetFileName(workerLogFile)
            File.Copy(workerLogFile, newWorkerLogFile);
            filesToUpload.Add(newWorkerLogFile);
            
            // Create the environment file
            // \_layout\_work\_temp\[jobname-support]\files\environment.txt
            string environmentFile = Path.Combine(supportFilesFolder, "environment.txt");
            string content = GetEnvironmentContent();
            using (StreamWriter writer = File.CreateText(environmentFile)) 
            {
                writer.Write(content);
            }
            filesToUpload.Add(environmentFile);
            
            // Download build logs
            string buildLogsZip = DownloadBuildLogs();
            filesToUpload.Add(buildLogsZip);

            // Upload support zip
            IJobServerQueue jobServerQueue = hostContext.GetService<IJobServerQueue>();

            // TODO: Keep a list of files to upload and queue them all? Do that if we aren't going to zip them
            // We would iterate all of the files we added to the support folder
            // TODO: Find how we set the name when we normally use this method.
            foreach (string fileToUpload in filesToUpload)
            {
                jobServerQueue.QueueFileUpload(
                    timelineId: _mainTimelineId, 
                    timelineRecordId: _record.Id, 
                    type: CoreAttachmentType.Support, 
                    name: "SupportLog", 
                    filePath: fileToUpload, 
                    deleteSource: false);
            }

            // Delete support folder
            Directory.Delete(supportRootFolder);
        }

        private string DownloadBuildLogs()
        {
            // download from build client endpoint
            // /_build/

            // add method to BuildServer
            // _buildHttpCLient.?

            //_extractionFolder
            // TODO: Implement.
            // TODO: There is a download build artifact task...

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
    public static class CoreAttachmentTypeExtensions
    {
        public static string Support(this CoreAttachmentType coreAttachmentType)
        {
            return "DistributedTask.Core.Support";
        }
    }
}