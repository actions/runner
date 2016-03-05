using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;


namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskManager))]
    public interface ITaskManager : IAgentService
    {
        Task EnsureTaskExists(string taskName, Guid taskId, string taskVersion);

        string GetDestinationPath(string componentName, Guid taskId, string version);
    }

    public sealed class TaskManager : AgentService, ITaskManager
    {
        private const string TaskJsonFileName = "task.json";

        public async Task EnsureTaskExists(string taskName, Guid taskId, string taskVersion)
        {
            var jobServer = HostContext.GetService<IJobServer>();

            if (string.IsNullOrEmpty(taskName))
            {
                throw new ArgumentNullException("taskName");
            }

            if (taskId == null || taskId == Guid.Empty)
            {
                throw new ArgumentNullException("taskId");
            }

            if (string.IsNullOrEmpty(taskVersion))
            {
                throw new ArgumentNullException("taskVersion");
            }
            
            string destPath = GetDestinationPath(taskName, taskId, taskVersion);

            // first check to see if we already have the task
            bool taskDirectoryExists = Directory.Exists(destPath);
            if (taskDirectoryExists && await IsValidTask(destPath))
            {
                Trace.Verbose("{0} - Task:{1}, found in the cache at {2}", nameof(EnsureTaskExists), taskName, destPath);
                return;
            }

            //directory exists, but data is bad or incomplete - wipe it
            if (taskDirectoryExists)
            {
                Directory.Delete(destPath, true);
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
                //this is a task with no content to download
                return;
            }

            //download and extract task in a temp folder and rename it on success
            string tempPath = GetDestinationPath(taskName, taskId, Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempPath);
                taskZipFile = Path.Combine(tempPath, string.Format("{0}.zip", Guid.NewGuid()));
                using (FileStream fs = new FileStream(taskZipFile, FileMode.Create))
                {
                    Stream result = await jobServer.GetTaskContentZipAsync(taskToDownload.Id, taskToDownload.Version, HostContext.CancellationToken);
                    await result.CopyToAsync(fs, 81920, HostContext.CancellationToken);
                }

                await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(taskZipFile, tempPath), HostContext.CancellationToken);
                File.Delete(taskZipFile);
                Directory.Move(tempPath, destPath);
                Trace.Verbose("{0} - Download Task:{1}, cached to: {2}", nameof(EnsureTaskExists), taskToDownload.Name, destPath);
            }
            finally
            {
                try
                {
                    //sometimes the temp folder is not deleted -> wipe it
                    bool tempDirectoryExists = Directory.Exists(tempPath);
                    if (tempDirectoryExists)
                    {
                        Directory.Delete(tempPath, true);
                    }
                }
                catch (Exception ex)
                {
                    //it is not critical if we fail to delete the temp folder -> just log an error
                    Trace.Error(ex);
                }
            }
        }

        private async Task<bool> IsValidTask(string destPath)
        {
            string taskJsonPath = Path.Combine(destPath, TaskJsonFileName);
            try
            {
                string json = await Task.Run(() => File.ReadAllText(taskJsonPath), HostContext.CancellationToken);
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
    }
}
