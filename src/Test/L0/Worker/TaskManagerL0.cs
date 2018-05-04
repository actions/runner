using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class TaskManagerL0
    {
        private const string TestDataFolderName = "TestData";
        private readonly CancellationTokenSource _ecTokenSource = new CancellationTokenSource();
        private Mock<IJobServer> _jobServer;
        private Mock<ITaskServer> _taskServer;
        private Mock<IConfigurationStore> _configurationStore;
        private Mock<IExecutionContext> _ec;
        private Mock<IAgentPluginManager> _agentPlugin;
        private TestHostContext _hc;
        private TaskManager _taskManager;
        private string _workFolder;

        //Test the cancellation flow: interrupt download task via HostContext cancellation token.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void BubblesCancellation()
        {
            try
            {
                //Arrange
                Setup();
                var bingTask = new Pipelines.TaskStep()
                {
                    Enabled = true,
                    Reference = new Pipelines.TaskStepDefinitionReference()
                    {
                        Name = "Bing",
                        Version = "0.1.2",
                        Id = Guid.NewGuid()
                    }
                };
                var pingTask = new Pipelines.TaskStep()
                {
                    Enabled = true,
                    Reference = new Pipelines.TaskStepDefinitionReference()
                    {
                        Name = "Ping",
                        Version = "0.1.1",
                        Id = Guid.NewGuid()
                    }
                };

                var bingVersion = new TaskVersion(bingTask.Reference.Version);
                var pingVersion = new TaskVersion(pingTask.Reference.Version);

                _taskServer
                    .Setup(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), _ec.Object.CancellationToken))
                    .Returns((Guid taskId, TaskVersion taskVersion, CancellationToken token) =>
                    {
                        _ecTokenSource.Cancel();
                        return Task.FromResult<Stream>(GetZipStream());
                    });

                var tasks = new List<Pipelines.TaskStep>(new Pipelines.TaskStep[] { bingTask, pingTask });

                //Act
                //should initiate a download with a mocked IJobServer, which sets a cancellation token and
                //download task is expected to be in cancelled state
                Task downloadTask = _taskManager.DownloadAsync(_ec.Object, tasks);
                Task[] taskToWait = { downloadTask, Task.Delay(2000) };
                //wait for the task to be cancelled to exit
                await Task.WhenAny(taskToWait);

                //Assert
                //verify task completed in less than 2sec and it is in cancelled state
                Assert.True(downloadTask.IsCompleted, $"{nameof(_taskManager.DownloadAsync)} timed out.");
                Assert.True(!downloadTask.IsFaulted, downloadTask.Exception?.ToString());
                Assert.True(downloadTask.IsCanceled);
                //check if the task.json was not downloaded for ping and bing tasks
                Assert.Equal(
                    0,
                    Directory.GetFiles(IOUtil.GetTasksPath(_hc), "*", SearchOption.AllDirectories).Length);
                //assert download was invoked only once, because the first task cancelled the second task download
                _taskServer
                    .Verify(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), _ec.Object.CancellationToken), Times.Once());
            }
            finally
            {
                Teardown();
            }
        }

        //Test how exceptions are propagated to the caller.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void BubblesNetworkException()
        {
            try
            {
                // Arrange.
                Setup();
                var pingTask = new Pipelines.TaskStep()
                {
                    Enabled = true,
                    Reference = new Pipelines.TaskStepDefinitionReference()
                    {
                        Name = "Ping",
                        Version = "0.1.1",
                        Id = Guid.NewGuid()
                    }
                };

                var pingVersion = new TaskVersion(pingTask.Reference.Version);
                Exception expectedException = new System.Net.Http.HttpRequestException("simulated network error");
                _taskServer
                    .Setup(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), _ec.Object.CancellationToken))
                    .Returns((Guid taskId, TaskVersion taskVersion, CancellationToken token) =>
                    {
                        throw expectedException;
                    });

                var tasks = new List<Pipelines.TaskStep>(new Pipelines.TaskStep[] { pingTask });

                //Act
                Exception actualException = null;
                try
                {
                    await _taskManager.DownloadAsync(_ec.Object, tasks);
                }
                catch (Exception ex)
                {
                    actualException = ex;
                }

                //Assert
                //verify task completed in less than 2sec and it is in failed state state
                Assert.Equal(expectedException, actualException);
                //see if the task.json was not downloaded
                Assert.Equal(
                    0,
                    Directory.GetFiles(IOUtil.GetTasksPath(_hc), "*", SearchOption.AllDirectories).Length);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DeserializesPlatformSupportedHandlersOnly()
        {
            try
            {
                // Arrange.
                Setup();
                // Prepare the content.
                const string Content = @"
{
    ""execution"": {
        ""Node"": { },
        ""Process"": { },
    }
}";
                // Write the task.json to disk.
                Pipelines.TaskStep instance;
                string directory;
                CreateTask(jsonContent: Content, instance: out instance, directory: out directory);

                // Act.
                Definition definition = _taskManager.Load(instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.NotNull(definition.Data);
                Assert.NotNull(definition.Data.Execution);
                Assert.NotNull(definition.Data.Execution.Node);
#if OS_WINDOWS
                Assert.NotNull(definition.Data.Execution.Process);
#else
                Assert.Null(definition.Data.Execution.Process);
#endif
            }
            finally
            {
                Teardown();
            }
        }

        //Test the normal flow, which downloads a few tasks and skips disabled, duplicate and cached tasks.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void DownloadsTasks()
        {
            try
            {
                //Arrange
                Setup();
                var bingGuid = Guid.NewGuid();
                string bingTaskName = "Bing";
                string bingVersion = "1.21.2";
                var tasks = new List<Pipelines.TaskStep>
                {
                    new Pipelines.TaskStep()
                    {
                        Enabled = true,
                        Reference = new Pipelines.TaskStepDefinitionReference()
                        {
                            Name = bingTaskName,
                            Version = bingVersion,
                            Id = bingGuid
                        }
                    },
                    new Pipelines.TaskStep()
                    {
                        Enabled = true,
                        Reference = new Pipelines.TaskStepDefinitionReference()
                        {
                            Name = bingTaskName,
                            Version = bingVersion,
                            Id = bingGuid
                        }
                    }
                };
                _taskServer
                    .Setup(x => x.GetTaskContentZipAsync(
                        bingGuid,
                        It.Is<TaskVersion>(y => string.Equals(y.ToString(), bingVersion, StringComparison.Ordinal)),
                        _ec.Object.CancellationToken))
                    .Returns(Task.FromResult<Stream>(GetZipStream()));

                //Act
                //first invocation will download and unzip the task from mocked IJobServer
                await _taskManager.DownloadAsync(_ec.Object, tasks);
                //second and third invocations should find the task in the cache and do nothing
                await _taskManager.DownloadAsync(_ec.Object, tasks);
                await _taskManager.DownloadAsync(_ec.Object, tasks);

                //Assert
                //see if the task.json was downloaded
                string destDirectory = Path.Combine(
                    IOUtil.GetTasksPath(_hc),
                    $"{bingTaskName}_{bingGuid}",
                    bingVersion);
                Assert.True(File.Exists(Path.Combine(destDirectory, Constants.Path.TaskJsonFile)));
                //assert download has happened only once, because disabled, duplicate and cached tasks are not downloaded
                _taskServer
                    .Verify(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), It.IsAny<CancellationToken>()), Times.Once());
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatchPlatform()
        {
            try
            {
                // Arrange.
                Setup();
#if !OS_WINDOWS
                const string Platform = "windows";
#else
                const string Platform = "nosuch"; // TODO: What to do here?
#endif
                HandlerData data = new NodeHandlerData() { Platforms = new string[] { Platform } };

                // Act/Assert.
                Assert.False(data.PreferredOnCurrentPlatform());
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsDefinition()
        {
            try
            {
                // Arrange.
                Setup();
                // Prepare the task.json content.
                const string Content = @"
{
    ""inputs"": [
        {
            ""extraInputKey"": ""Extra input value"",
            ""name"": ""someFilePathInput"",
            ""type"": ""filePath"",
            ""label"": ""Some file path input label"",
            ""defaultValue"": ""Some default file path"",
            ""required"": true,
            ""helpMarkDown"": ""Some file path input markdown""
        },
        {
            ""name"": ""someStringInput"",
            ""type"": ""string"",
            ""label"": ""Some string input label"",
            ""defaultValue"": ""Some default string"",
            ""helpMarkDown"": ""Some string input markdown"",
            ""required"": false,
            ""groupName"": ""advanced""
        }
    ],
    ""execution"": {
        ""Node"": {
            ""target"": ""Some Node target"",
            ""extraNodeArg"": ""Extra node arg value""
        },
        ""Process"": {
            ""target"": ""Some process target"",
            ""argumentFormat"": ""Some process argument format"",
            ""workingDirectory"": ""Some process working directory"",
            ""extraProcessArg"": ""Some extra process arg"",
            ""platforms"": [
                ""windows""
            ]
        },
        ""NoSuchHandler"": {
            ""target"": ""no such target""
        }
    },
    ""someExtraSection"": {
        ""someExtraKey"": ""Some extra value""
    }
}";
                // Write the task.json to disk.
                Pipelines.TaskStep instance;
                string directory;
                CreateTask(jsonContent: Content, instance: out instance, directory: out directory);

                // Act.
                Definition definition = _taskManager.Load(instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.Equal(directory, definition.Directory);
                Assert.NotNull(definition.Data);
                Assert.NotNull(definition.Data.Inputs); // inputs
                Assert.Equal(2, definition.Data.Inputs.Length);
                Assert.Equal("someFilePathInput", definition.Data.Inputs[0].Name);
                Assert.Equal("Some default file path", definition.Data.Inputs[0].DefaultValue);
                Assert.Equal("someStringInput", definition.Data.Inputs[1].Name);
                Assert.Equal("Some default string", definition.Data.Inputs[1].DefaultValue);
                Assert.NotNull(definition.Data.Execution); // execution

#if OS_WINDOWS
                // Process handler should only be deserialized on Windows.
                Assert.Equal(2, definition.Data.Execution.All.Count);
#else
                // Only Node handler should be deserialized on non-Windows.
                Assert.Equal(1, definition.Data.Execution.All.Count);
#endif

                // Node handler should always be deserialized.
                Assert.NotNull(definition.Data.Execution.Node); // execution.Node
                Assert.Equal(definition.Data.Execution.Node, definition.Data.Execution.All[0]);
                Assert.Equal("Some Node target", definition.Data.Execution.Node.Target);

#if OS_WINDOWS
                // Process handler should only be deserialized on Windows.
                Assert.NotNull(definition.Data.Execution.Process); // execution.Process
                Assert.Equal(definition.Data.Execution.Process, definition.Data.Execution.All[1]);
                Assert.Equal("Some process argument format", definition.Data.Execution.Process.ArgumentFormat);
                Assert.NotNull(definition.Data.Execution.Process.Platforms);
                Assert.Equal(1, definition.Data.Execution.Process.Platforms.Length);
                Assert.Equal("windows", definition.Data.Execution.Process.Platforms[0]);
                Assert.Equal("Some process target", definition.Data.Execution.Process.Target);
                Assert.Equal("Some process working directory", definition.Data.Execution.Process.WorkingDirectory);
#endif
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatchesPlatform()
        {
            try
            {
                // Arrange.
                Setup();
#if OS_WINDOWS
                const string Platform = "WiNdOwS";
#else
                // TODO: What to do here?
                const string Platform = "";
                if (string.IsNullOrEmpty(Platform))
                {
                    return;
                }
#endif
                HandlerData data = new NodeHandlerData() { Platforms = new[] { Platform } };

                // Act/Assert.
                Assert.True(data.PreferredOnCurrentPlatform());
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ReplacesMacros()
        {
            try
            {
                // Arrange.
                Setup();
                const string Directory = "Some directory";
                Definition definition = new Definition() { Directory = Directory };
                NodeHandlerData node = new NodeHandlerData()
                {
                    Target = @"$(CuRrEnTdIrEcToRy)\Some node target",
                    WorkingDirectory = @"$(CuRrEnTdIrEcToRy)\Some node working directory",
                };
                ProcessHandlerData process = new ProcessHandlerData()
                {
                    ArgumentFormat = @"$(CuRrEnTdIrEcToRy)\Some process argument format",
                    Target = @"$(CuRrEnTdIrEcToRy)\Some process target",
                    WorkingDirectory = @"$(CuRrEnTdIrEcToRy)\Some process working directory",
                };

                // Act.
                node.ReplaceMacros(_hc, definition);
                process.ReplaceMacros(_hc, definition);

                // Assert.
                Assert.Equal($@"{Directory}\Some node target", node.Target);
                Assert.Equal($@"{Directory}\Some node working directory", node.WorkingDirectory);
                Assert.Equal($@"{Directory}\Some process argument format", process.ArgumentFormat);
                Assert.Equal($@"{Directory}\Some process target", process.Target);
                Assert.Equal($@"{Directory}\Some process working directory", process.WorkingDirectory);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ReplacesMacrosAndPreventsInfiniteRecursion()
        {
            try
            {
                // Arrange.
                Setup();
                string directory = "$(currentdirectory)$(currentdirectory)";
                Definition definition = new Definition() { Directory = directory };
                NodeHandlerData node = new NodeHandlerData()
                {
                    Target = @"$(currentDirectory)\Some node target",
                };

                // Act.
                node.ReplaceMacros(_hc, definition);

                // Assert.
                Assert.Equal($@"{directory}\Some node target", node.Target);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ReplacesMultipleMacroInstances()
        {
            try
            {
                // Arrange.
                Setup();
                const string Directory = "Some directory";
                Definition definition = new Definition() { Directory = Directory };
                NodeHandlerData node = new NodeHandlerData()
                {
                    Target = @"$(CuRrEnTdIrEcToRy)$(CuRrEnTdIrEcToRy)\Some node target",
                };

                // Act.
                node.ReplaceMacros(_hc, definition);

                // Assert.
                Assert.Equal($@"{Directory}{Directory}\Some node target", node.Target);
            }
            finally
            {
                Teardown();
            }
        }

        private void CreateTask(string jsonContent, out Pipelines.TaskStep instance, out string directory)
        {
            const string TaskName = "SomeTask";
            const string TaskVersion = "1.2.3";
            Guid taskGuid = Guid.NewGuid();
            directory = Path.Combine(_workFolder, Constants.Path.TasksDirectory, $"{TaskName}_{taskGuid}", TaskVersion);
            string file = Path.Combine(directory, Constants.Path.TaskJsonFile);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, jsonContent);
            instance = new Pipelines.TaskStep()
            {
                Reference = new Pipelines.TaskStepDefinitionReference()
                {
                    Id = taskGuid,
                    Name = TaskName,
                    Version = TaskVersion,
                }
            };
        }

        private Stream GetZipStream()
        {
            // Locate the test data folder containing the task.json.
            string sourceFolder = Path.Combine(TestUtil.GetTestDataPath(), nameof(TaskManagerL0));
            Assert.True(Directory.Exists(sourceFolder), $"Directory does not exist: {sourceFolder}");
            Assert.True(File.Exists(Path.Combine(sourceFolder, Constants.Path.TaskJsonFile)));

            // Create the zip file under the work folder so it gets cleaned up.
            string zipFile = Path.Combine(
                _workFolder,
                $"{Guid.NewGuid()}.zip");
            Directory.CreateDirectory(_workFolder);
            ZipFile.CreateFromDirectory(sourceFolder, zipFile, CompressionLevel.Fastest, includeBaseDirectory: false);
            return new FileStream(zipFile, FileMode.Open);
        }

        private void Setup([CallerMemberName] string name = "")
        {
            // Mocks.
            _jobServer = new Mock<IJobServer>();
            _taskServer = new Mock<ITaskServer>();
            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);

            // Test host context.
            _hc = new TestHostContext(this, name);

            // Random work folder.
            _workFolder = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Bin), $"_work_{Path.GetRandomFileName()}");

            _hc.SetSingleton<IJobServer>(_jobServer.Object);
            _hc.SetSingleton<ITaskServer>(_taskServer.Object);

            _configurationStore = new Mock<IConfigurationStore>();
            _configurationStore
                .Setup(x => x.GetSettings())
                .Returns(
                    new AgentSettings
                    {
                        WorkFolder = _workFolder
                    });
            _hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);

            _agentPlugin = new Mock<IAgentPluginManager>();
            _agentPlugin
                .Setup(x => x.GetPluginTask(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns<Definition>(null);
            _agentPlugin
                .Setup(x => x.GetPluginCommad(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<AgentPluginInfo>(null);
            _hc.SetSingleton<IAgentPluginManager>(_agentPlugin.Object);

            // Instance to test.
            _taskManager = new TaskManager();
            _taskManager.Initialize(_hc);
        }

        private void Teardown()
        {
            _hc?.Dispose();
            if (!string.IsNullOrEmpty(_workFolder) && Directory.Exists(_workFolder))
            {
                Directory.Delete(_workFolder, recursive: true);
            }
        }
    }
}
