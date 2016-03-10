using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{    
    public sealed class TaskManagerL0
    {
        private Mock<IJobServer> _jobServer;
        private Mock<IConfigurationStore> _configurationStore;

        public TaskManagerL0()
        {
            _jobServer = new Mock<IJobServer>();
            _configurationStore = new Mock<IConfigurationStore>();
        }

        const string WorkFolderName = "_work";
        const string TestDataFolderName = "TestData";

        private string GetZipFolderName()
        {
            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            var baseTestDirectory = new DirectoryInfo(currentAssemblyLocation).Parent.Parent.Parent.Parent.Parent.Parent;
            string zipFileFolder = Path.Combine(baseTestDirectory.FullName, TestDataFolderName, nameof(TaskManagerL0));
            return zipFileFolder;
        }
        private Stream GetZipStream()
        {
            string zipFileFolder = GetZipFolderName();
            string zipFile = zipFileFolder + ".zip";
            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }
            ZipFile.CreateFromDirectory(zipFileFolder, zipFile, CompressionLevel.Fastest, false);
            FileStream fs = new FileStream(zipFile, FileMode.Open);
            return fs;
        }
        
        //Test the normal flow, which downloads a few tasks and skips disabled, duplicate and cached tasks.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void TestEnsureTasksExist()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IJobServer>(_jobServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                var taskManager = new TaskManager();
                taskManager.Initialize(hc);

                _configurationStore.Setup(x => x.GetSettings()).Returns(
                 new AgentSettings
                 {
                     WorkFolder = WorkFolderName
                 });

                string workingFolder = IOUtil.GetWorkPath(hc);
                if (Directory.Exists(workingFolder))
                {
                    Directory.Delete(IOUtil.GetWorkPath(hc), true);
                }                

                //pass 3 tasks: one disabled task and two duplicate tasks named "Bing"
                var bingGuid = Guid.NewGuid();
                string bingTaskName = "Bing";
                var arTasks = new TaskInstance[]
                {
                   new TaskInstance()
                    {
                        Enabled = false,
                        Name = "Ping",
                        Version = "0.1.2",
                        Id = Guid.NewGuid()
                    },                    
                    new TaskInstance()
                    {
                        Enabled = true,
                        Name = bingTaskName,
                        Version = "1.21.2",
                        Id = bingGuid
                    },
                    new TaskInstance()
                    {
                        Enabled = true,
                        Name = bingTaskName,
                        Version = "1.21.2",
                        Id = bingGuid
                    }
                };

                var tasks = new List<TaskInstance>(arTasks);
                var bingVersion = new TaskVersion(arTasks[1].Version);
                _jobServer
                    .Setup(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), hc.CancellationToken))
                    .Returns((Guid taskId, TaskVersion taskVersion, CancellationToken token) =>
                    {                        
                        return Task.FromResult<Stream>(GetZipStream());
                    });

                //Act
                //first invocation will download and unzip the task from mocked IJobServer
                await taskManager.EnsureTasksExist(tasks);
                //second and third invocations should find the task in the cache and do nothing
                await taskManager.EnsureTasksExist(tasks);
                await taskManager.EnsureTasksExist(tasks);

                //Assert
                //see if the task.json was downloaded
                string destPath = taskManager.GetDestinationPath(bingTaskName, bingGuid, bingVersion);
                Assert.True(File.Exists(Path.Combine(destPath, "task.json")));
                //assert download has happened only once, because disabled, duplicate and cached tasks are not downloaded
                _jobServer
                    .Verify(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), hc.CancellationToken), Times.Once());

                //Cleanup
                string zipFile = GetZipFolderName() + ".zip";
                if (File.Exists(zipFile))
                {
                    File.Delete(zipFile);
                }
            }
        }

        //Test the cancellation flow: interrupt download task via HostContext cancellation token.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void TestCancellationOfEnsureTasksExist()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IJobServer>(_jobServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                var taskManager = new TaskManager();
                taskManager.Initialize(hc);

                _configurationStore.Setup(x => x.GetSettings()).Returns(
                 new AgentSettings
                 {
                     WorkFolder = WorkFolderName
                 });

                string workingFolder = IOUtil.GetWorkPath(hc);
                if (Directory.Exists(workingFolder))
                {
                    Directory.Delete(IOUtil.GetWorkPath(hc), true);
                }
                
                var bingTask = new TaskInstance()
                {
                    Enabled = true,
                    Name = "Bing",
                    Version = "0.1.2",
                    Id = Guid.NewGuid()
                };
                var pingTask = new TaskInstance()
                {
                    Enabled = true,
                    Name = "Ping",
                    Version = "0.1.1",
                    Id = Guid.NewGuid()
                };

                var bingVersion = new TaskVersion(bingTask.Version);
                var pingVersion = new TaskVersion(pingTask.Version);

                _jobServer
                    .Setup(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), hc.CancellationToken))
                    .Returns((Guid taskId, TaskVersion taskVersion, CancellationToken token) =>
                    {
                        hc.Cancel();
                        return Task.FromResult<Stream>(GetZipStream());
                    });

                var tasks = new List<TaskInstance>(new TaskInstance[] { bingTask, pingTask });

                //Act
                //should initiate a download with a mocked IJobServer, which sets a cancelation token and
                //ensureExistTask task is expected to be in cancelled state
                Task ensureExistTask = taskManager.EnsureTasksExist(tasks);
                Task[] taskToWait = { ensureExistTask, Task.Delay(2000) };
                //wait for the task to be cancelled to exit
                await Task.WhenAny(taskToWait);

                //Assert
                //verify task completed in less than 2sec and it is in cancelled state
                Assert.True(ensureExistTask.IsCompleted, $"{nameof(taskManager.EnsureTasksExist)} timed out.");
                Assert.True(!ensureExistTask.IsFaulted, ensureExistTask.Exception?.ToString());
                Assert.True(ensureExistTask.IsCanceled);
                //check if the task.json was not downloaded for ping and bing tasks
                string bingDestPath = taskManager.GetDestinationPath(bingTask.Name, bingTask.Id, bingVersion);
                string pingDestPath = taskManager.GetDestinationPath(pingTask.Name, pingTask.Id, pingVersion);
                Assert.True(!File.Exists(Path.Combine(bingDestPath, "task.json")));
                Assert.True(!File.Exists(Path.Combine(pingDestPath, "task.json")));
                //assert download was invoked only once, because the first task cancelled the second task download
                _jobServer
                    .Verify(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), hc.CancellationToken), Times.Once());

                //Cleanup
                string zipFile = GetZipFolderName() + ".zip";
                if (File.Exists(zipFile))
                {
                    File.Delete(zipFile);
                }
            }
        }

        //Test how exceptions are propagated to the caller.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void TestNetworkErrorWhileDownloadingTask()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IJobServer>(_jobServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                var taskManager = new TaskManager();
                taskManager.Initialize(hc);

                _configurationStore.Setup(x => x.GetSettings()).Returns(
                 new AgentSettings
                 {
                     WorkFolder = WorkFolderName
                 });

                string workingFolder = IOUtil.GetWorkPath(hc);
                if (Directory.Exists(workingFolder))
                {
                    Directory.Delete(IOUtil.GetWorkPath(hc), true);
                }

                var pingTask = new TaskInstance()
                {
                    Enabled = true,
                    Name = "Ping",
                    Version = "0.1.1",
                    Id = Guid.NewGuid()
                };

                var pingVersion = new TaskVersion(pingTask.Version);

                _jobServer
                    .Setup(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), hc.CancellationToken))
                    .Returns((Guid taskId, TaskVersion taskVersion, CancellationToken token) =>
                    {
                        throw new System.Net.Http.HttpRequestException("simulated network error");
                    });

                var tasks = new List<TaskInstance>(new TaskInstance[] { pingTask });

                //Act
                //EnsureTasksExist should throw a HttpRequestException exception
                Task ensureExistTask = taskManager.EnsureTasksExist(tasks);
                Task[] taskToWait = { ensureExistTask, Task.Delay(2000) };
                //wait for the task to be cancelled to exit
                await Task.WhenAny(taskToWait);

                //Assert
                //verify task completed in less than 2sec and it is in failed state state
                Assert.True(ensureExistTask.IsCompleted, $"{nameof(taskManager.EnsureTasksExist)} timed out.");
                Assert.True(ensureExistTask.IsFaulted, $"{nameof(taskManager.EnsureTasksExist)} did not propage exception to caller.");
                Assert.True(!ensureExistTask.IsCanceled);
                //see if the task.json was not downloaded
                string pingDestPath = taskManager.GetDestinationPath(pingTask.Name, pingTask.Id, pingVersion);                
                Assert.True(!File.Exists(Path.Combine(pingDestPath, "task.json")));                
            }
        }
    }
}
