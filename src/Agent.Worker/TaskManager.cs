using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;


namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskManager))]
    public interface ITaskManager : IAgentService
    {
        string GetDestinationPath(string componentName, Guid taskId, string version);

        Task EnsureTasksExist(List<IStep> steps);
    }

    public sealed class TaskManager : AgentService, ITaskManager
    {
        private const string TaskJsonFileName = "task.json";

        private async Task EnsureTaskExists(string taskName, Guid taskId, string taskVersion)
        {
            var jobServer = HostContext.GetService<IJobServer>();
            ArgUtil.NotNullOrEmpty(taskName, nameof(taskName));
            ArgUtil.NotEmpty(taskId, nameof(taskId));
            ArgUtil.NotNullOrEmpty(taskVersion, nameof(taskVersion));
            string destPath = GetDestinationPath(taskName, taskId, taskVersion);

            // first check to see if we already have the task
            bool taskDirectoryExists = Directory.Exists(destPath);
            if (taskDirectoryExists)
            {
                Trace.Info("{0} - Task:{1}, version {2} found in the cache at {3}", nameof(EnsureTaskExists), taskName, taskVersion, destPath);
                return;
            }

            string taskZipFile;
            TaskDefinition taskToDownload = null;
            var version = new TaskVersion(taskVersion);
            taskToDownload = await jobServer.GetTaskDefinitionAsync(taskId, version, HostContext.CancellationToken);
            if (taskToDownload == null)
            {
                Trace.Error("{0} - Can't find task definition in server with Id: {1}, Version: {2}.", nameof(EnsureTaskExists), taskId, version);
                throw new InvalidDataException();
            }

            if (!taskToDownload.ContentsUploaded)
            {
                Trace.Error("task has no content");
                throw new InvalidDataException();
            }

            //download and extract task in a temp folder and rename it on success
            string tempPath = Path.Combine(IOUtil.GetTempPath(), taskName + "_" + version);
            try
            {
                Directory.CreateDirectory(tempPath);
                taskZipFile = Path.Combine(tempPath, string.Format("{0}.zip", Guid.NewGuid()));
                using (FileStream fs = new FileStream(taskZipFile, FileMode.Create))
                {
                    Stream result = await jobServer.GetTaskContentZipAsync(taskToDownload.Id, taskToDownload.Version, HostContext.CancellationToken);
                    await result.CopyToAsync(fs, 81920, HostContext.CancellationToken);
                }

                System.IO.Compression.ZipFile.ExtractToDirectory(taskZipFile, tempPath);
                File.Delete(taskZipFile);
                if (!IsValidTask(tempPath))
                {
                    throw new InvalidDataException("Invalid task content (task.json)");
                }
                string destPathParent = Path.Combine(IOUtil.GetTasksPath(), taskName + "_" + taskId.ToString());
                Directory.CreateDirectory(destPathParent);
                Directory.Move(tempPath, destPath);
                Trace.Info("{0} - Downloaded Task:{1}, version {2}, cached to: {3}", nameof(EnsureTaskExists), taskToDownload.Name, taskToDownload.Version, destPath);
            }
            finally
            {
                try
                {
                    //sometimes the temp folder is not deleted -> wipe it
                    bool tempDirectoryExists = Directory.Exists(tempPath);
                    if (tempDirectoryExists)
                    {
                        Trace.Verbose("Deleting task temp folder: {0}", tempPath);
                        Directory.Delete(tempPath, true);
                    }
                }
                catch (Exception ex)
                {
                    //it is not critical if we fail to delete the temp folder -> just log an error
                    Trace.Info("Failed to delete temp folder {0} with exception {1}", tempPath, ex.ToString());                    
                }
            }
        }

        private bool IsValidTask(string destPath)
        {
            string taskJsonPath = Path.Combine(destPath, TaskJsonFileName);
            try
            {
                string json = File.ReadAllText(taskJsonPath);
                JObject.Parse(json);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public string GetDestinationPath(string componentName, Guid taskId, string version)
        {            
            return Path.Combine(IOUtil.GetTasksPath(), componentName + "_" + taskId.ToString(), version);
        }

        public async Task EnsureTasksExist(List<IStep> steps)
        {
            //remove duplicate and disabled tasks
            var uniqueTasks =
                from step in steps
                where step as ITaskRunner != null && step.Enabled
                group step by new
                    {
                        (step as ITaskRunner).TaskInstance.Id,
                        (step as ITaskRunner).TaskInstance.Name,
                        (step as ITaskRunner).TaskInstance.Version
                    } 
                into newTask
                select newTask;
            foreach (var task in uniqueTasks)                
            {                
                await EnsureTaskExists(task.Key.Name,
                    task.Key.Id, task.Key.Version);
            }
        }
    }
}
