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

        //TODO: we need pass ExecutionContext for logging
        Task EnsureTasksExist(IEnumerable<TaskInstance> tasks);
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
            var version = new TaskVersion(taskVersion);

            //download and extract task in a temp folder and rename it on success
            string tempPath = Path.Combine(IOUtil.GetTasksPath(HostContext), "_temp_" + Guid.NewGuid());
            try
            {
                Directory.CreateDirectory(tempPath);
                taskZipFile = Path.Combine(tempPath, string.Format("{0}.zip", Guid.NewGuid()));
                //open zip stream in async mode
                using (FileStream fs = new FileStream(taskZipFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    using (Stream result = await jobServer.GetTaskContentZipAsync(taskId, version, HostContext.CancellationToken))
                    {
                        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
                        await result.CopyToAsync(fs, 81920, HostContext.CancellationToken);
                        await fs.FlushAsync(HostContext.CancellationToken);
                    }
                }

                System.IO.Compression.ZipFile.ExtractToDirectory(taskZipFile, tempPath);
                File.Delete(taskZipFile);
                //TODO: find out if this check belongs here or it is handler's responsibility 
                if (!IsValidTask(tempPath))
                {
                    //TODO: add localized message
                    throw new InvalidDataException("Invalid task content (task.json)");
                }
                string destPathParent = Path.Combine(IOUtil.GetTasksPath(HostContext), taskName + "_" + taskId.ToString());
                Directory.CreateDirectory(destPathParent);
                Directory.Move(tempPath, destPath);
                Trace.Info("{0} - Downloaded Task:{1}, version {2}, cached to: {3}", nameof(EnsureTaskExists), taskName, taskVersion, destPath);
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
            return Path.Combine(IOUtil.GetTasksPath(HostContext), componentName + "_" + taskId.ToString(), version);
        }

        public async Task EnsureTasksExist(IEnumerable<TaskInstance> tasks)
        {
            //remove duplicate and disabled tasks
            var uniqueTasks =
                from task in tasks
                where task.Enabled
                group task by new
                    {
                        task.Id,
                        task.Name,
                        task.Version
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
