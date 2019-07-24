using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ActionManagerL0
    {
        private const string TestDataFolderName = "TestData";
        private CancellationTokenSource _ecTokenSource;
        private Mock<IJobServer> _jobServer;
        private Mock<IConfigurationStore> _configurationStore;
        private Mock<IDockerCommandManager> _dockerManager;
        private Mock<IExecutionContext> _ec;
        private Mock<IRunnerPluginManager> _pluginManager;
        private TestHostContext _hc;
        private ActionManager _actionManager;
        private string _workFolder;

        // //Test how exceptions are propagated to the caller.
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Worker")]
        // public async void RetryNetworkException()
        // {
        //     try
        //     {
        //         // Arrange.
        //         Setup();
        //         var pingTask = new Pipelines.TaskStep()
        //         {
        //             Enabled = true,
        //             Reference = new Pipelines.TaskStepDefinitionReference()
        //             {
        //                 Name = "Ping",
        //                 Version = "0.1.1",
        //                 Id = Guid.NewGuid()
        //             }
        //         };

        //         var pingVersion = new TaskVersion(pingTask.Reference.Version);
        //         Exception expectedException = new System.Net.Http.HttpRequestException("simulated network error");
        //         _taskServer
        //             .Setup(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), It.IsAny<CancellationToken>()))
        //             .Returns((Guid taskId, TaskVersion taskVersion, CancellationToken token) =>
        //             {
        //                 throw expectedException;
        //             });

        //         var tasks = new List<Pipelines.TaskStep>(new Pipelines.TaskStep[] { pingTask });

        //         //Act
        //         Exception actualException = null;
        //         try
        //         {
        //             await _actionManager.DownloadAsync(_ec.Object, tasks);
        //         }
        //         catch (Exception ex)
        //         {
        //             actualException = ex;
        //         }

        //         //Assert
        //         //verify task completed in less than 2sec and it is in failed state state
        //         Assert.Equal(expectedException, actualException);

        //         //assert download was invoked 3 times, because we retry on task download
        //         _taskServer
        //             .Verify(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

        //         //see if the task.json was not downloaded
        //         Assert.Equal(
        //             0,
        //             Directory.GetFiles(_hc.GetDirectory(WellKnownDirectory.Tasks), "*", SearchOption.AllDirectories).Length);
        //     }
        //     finally
        //     {
        //         Teardown();
        //     }
        // }

        // //Test how exceptions are propagated to the caller.
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Worker")]
        // public async void RetryStreamException()
        // {
        //     try
        //     {
        //         // Arrange.
        //         Setup();
        //         var pingTask = new Pipelines.TaskStep()
        //         {
        //             Enabled = true,
        //             Reference = new Pipelines.TaskStepDefinitionReference()
        //             {
        //                 Name = "Ping",
        //                 Version = "0.1.1",
        //                 Id = Guid.NewGuid()
        //             }
        //         };

        //         var pingVersion = new TaskVersion(pingTask.Reference.Version);
        //         Exception expectedException = new System.Net.Http.HttpRequestException("simulated network error");
        //         _taskServer
        //             .Setup(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), It.IsAny<CancellationToken>()))
        //             .Returns((Guid taskId, TaskVersion taskVersion, CancellationToken token) =>
        //             {
        //                 return Task.FromResult<Stream>(new ExceptionStream());
        //             });

        //         var tasks = new List<Pipelines.TaskStep>(new Pipelines.TaskStep[] { pingTask });

        //         //Act
        //         Exception actualException = null;
        //         try
        //         {
        //             await _actionManager.DownloadAsync(_ec.Object, tasks);
        //         }
        //         catch (Exception ex)
        //         {
        //             actualException = ex;
        //         }

        //         //Assert
        //         //verify task completed in less than 2sec and it is in failed state state
        //         Assert.Equal("NotImplementedException", actualException.GetType().Name);

        //         //assert download was invoked 3 times, because we retry on task download
        //         _taskServer
        //             .Verify(x => x.GetTaskContentZipAsync(It.IsAny<Guid>(), It.IsAny<TaskVersion>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

        //         //see if the task.json was not downloaded
        //         Assert.Equal(
        //             0,
        //             Directory.GetFiles(_hc.GetDirectory(WellKnownDirectory.Tasks), "*", SearchOption.AllDirectories).Length);
        //     }
        //     finally
        //     {
        //         Teardown();
        //     }
        // }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_PullImageFromDockerHub()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.ContainerRegistryReference()
                        {
                            Image = "ubuntu:16.04"
                        }
                    }
                };

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                Assert.Equal((steps[0].Data as ContainerSetupInfo).StepId, actionId);
                Assert.Equal((steps[0].Data as ContainerSetupInfo).Image, "ubuntu:16.04");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_DownloadActionFromGraph()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "actions/npm",
                            Ref = "master",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions/npm", "master.completed");
                Assert.True(File.Exists(watermarkFile));

                var actionDockerfile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions/npm", "master", "Dockerfile");
                Assert.True(File.Exists(actionDockerfile));
                _hc.GetTrace().Info(File.ReadAllText(actionDockerfile));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_SkipDownloadActionFromGraphWhenCache()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "notexist/no",
                            Ref = "notexist",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "notexist/no", "notexist.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                File.Copy(Path.Combine(Environment.GetEnvironmentVariable("GITHUB_RUNNER_SRC_DIR"), "Test", TestDataFolderName, nameof(ActionManagerL0), "dockerfileaction.yml"), Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "action.yml"));

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_SkipDownloadActionForSelfRepo()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Path = "action",
                            RepositoryType = Pipelines.PipelineConstants.SelfAlias
                        }
                    }
                };

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithDockerfile()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "actions/test",
                            Ref = "master",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions/test", "master.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "master"));
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(watermarkFile), "master", "Dockerfile"), "Fake Dockerfile");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);
                Assert.Equal((steps[0].Data as ContainerSetupInfo).StepId, actionId);
                Assert.Equal((steps[0].Data as ContainerSetupInfo).WorkingDirectory, Path.Combine(Path.GetDirectoryName(watermarkFile), "master"));
                Assert.Equal((steps[0].Data as ContainerSetupInfo).Dockerfile, Path.Combine(Path.GetDirectoryName(watermarkFile), "master", "Dockerfile"));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithDockerfileInRelativePath()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "actions/test",
                            Ref = "master",
                            Path = "images/cli",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions/test", "master.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "master/images/cli"));
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(watermarkFile), "master/images/cli/Dockerfile"), "Fake Dockerfile");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal((steps[0].Data as ContainerSetupInfo).StepId, actionId);
                Assert.Equal((steps[0].Data as ContainerSetupInfo).WorkingDirectory, Path.Combine(Path.GetDirectoryName(watermarkFile), "master"));
                Assert.Equal((steps[0].Data as ContainerSetupInfo).Dockerfile, Path.Combine(Path.GetDirectoryName(watermarkFile), "master", "images/cli", "Dockerfile"));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithActionfile_Dockerfile()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "notexist/no",
                            Ref = "notexist",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "notexist/no", "notexist.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                File.Copy(Path.Combine(Environment.GetEnvironmentVariable("GITHUB_RUNNER_SRC_DIR"), "Test", TestDataFolderName, nameof(ActionManagerL0), "dockerfileaction.yml"), Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "action.yml"));
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist/Dockerfile"), "Fake Dockerfile");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal((steps[0].Data as ContainerSetupInfo).StepId, actionId);
                Assert.Equal((steps[0].Data as ContainerSetupInfo).WorkingDirectory, Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                Assert.Equal((steps[0].Data as ContainerSetupInfo).Dockerfile, Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "Dockerfile"));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithActionfile_DockerfileRelativePath()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "notexist/no",
                            Ref = "notexist",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "notexist/no", "notexist.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                File.Copy(Path.Combine(Environment.GetEnvironmentVariable("GITHUB_RUNNER_SRC_DIR"), "Test", TestDataFolderName, nameof(ActionManagerL0), "dockerfilerelativeaction.yml"), Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "action.yml"));
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "master/images"));
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(watermarkFile), "master/images/Dockerfile"), "Fake Dockerfile");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal((steps[0].Data as ContainerSetupInfo).StepId, actionId);
                Assert.Equal((steps[0].Data as ContainerSetupInfo).WorkingDirectory, Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                Assert.Equal((steps[0].Data as ContainerSetupInfo).Dockerfile, Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "images/Dockerfile"));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithActionfile_DockerHubImage()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "notexist/no",
                            Ref = "notexist",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "notexist/no", "notexist.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                File.Copy(Path.Combine(Environment.GetEnvironmentVariable("GITHUB_RUNNER_SRC_DIR"), "Test", TestDataFolderName, nameof(ActionManagerL0), "dockerhubaction.yml"), Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "action.yml"));

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal((steps[0].Data as ContainerSetupInfo).StepId, actionId);
                Assert.Equal((steps[0].Data as ContainerSetupInfo).Image, "ubuntu:18.04");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithActionfile_Node()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "notexist/no",
                            Ref = "notexist",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "notexist/no", "notexist.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                File.Copy(Path.Combine(Environment.GetEnvironmentVariable("GITHUB_RUNNER_SRC_DIR"), "Test", TestDataFolderName, nameof(ActionManagerL0), "nodeaction.yml"), Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "action.yml"));

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsContainerRegistryActionDefinition()
        {
            try
            {
                //Arrange
                Setup();

                Pipelines.ActionStep instance = new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.ContainerRegistryReference()
                    {
                        Image = "ubuntu:16.04"
                    }
                };

                _actionManager.CachedActionContainers[instance.Id] = new ContainerInfo() { ContainerImage = "ubuntu:16.04" };

                // Act.
                Definition definition = _actionManager.LoadAction(_ec.Object, instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.NotNull(definition.Data);
                Assert.Equal("ubuntu:16.04", definition.Data.Execution.ContainerAction.ContainerImage);
                Assert.True(string.IsNullOrEmpty(definition.Data.Execution.ContainerAction.EntryPoint));
                Assert.Null(definition.Data.Execution.ContainerAction.Arguments);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsScriptActionDefinition()
        {
            try
            {
                //Arrange
                Setup();

                Pipelines.ActionStep instance = new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.ScriptReference()
                };

                // Act.
                Definition definition = _actionManager.LoadAction(_ec.Object, instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.NotNull(definition.Data);
                Assert.NotNull(definition.Data.Execution.ScriptAction);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsPluginActionDefinition()
        {
            try
            {
                //Arrange
                Setup();

                Pipelines.ActionStep instance = new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.PluginReference()
                    {
                        Plugin = "my-plugin"
                    }
                };

                // Act.
                Definition definition = _actionManager.LoadAction(_ec.Object, instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.NotNull(definition.Data);
                Assert.NotNull(definition.Data.Execution.RunnerPlugin);
                Assert.Equal("plugin.class, plugin", definition.Data.Execution.RunnerPlugin.Target);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsContainerActionDefinitionDockerfile()
        {
            try
            {
                // Arrange.
                Setup();
                // Prepare the task.json content.
                const string Content = @"
# Container action
name: 'Hello World'
description: 'Greet the world and record the time'
author: 'GitHub'
inputs: 
  greeting: # id of input
    description: 'The greeting we choose - will print ""{greeting}, World!"" on stdout'
    required: true
    default: 'Hello'
  entryPoint: # id of input
    description: 'optional docker entrypoint overwrite.'
    required: false
outputs:
  time: # id of output
    description: 'The time we did the greeting'
icon: 'hello.svg' # vector art to display in the GitHub Marketplace
color: 'green' # optional, decorates the entry in the GitHub Marketplace
runs:
  using: 'docker'
  image: 'Dockerfile'
  args:
  - '${{ inputs.greeting }}'
  entrypoint: 'main.sh'
  env:
    Token: foo
    Url: bar
";
                Pipelines.ActionStep instance;
                string directory;
                CreateAction(yamlContent: Content, instance: out instance, directory: out directory);
                _actionManager.CachedActionContainers[instance.Id] = new ContainerInfo() { ContainerImage = "image:1234" };

                // Act.
                Definition definition = _actionManager.LoadAction(_ec.Object, instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.Equal(directory, definition.Directory);
                Assert.NotNull(definition.Data);
                Assert.NotNull(definition.Data.Inputs); // inputs

                Dictionary<string, string> inputDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var input in definition.Data.Inputs)
                {
                    var name = input.Key.AssertString("key").Value;
                    var value = input.Value.AssertScalar("value").ToString();

                    _hc.GetTrace().Info($"Default: {name} = {value}");
                    inputDefaults[name] = value;
                }

                Assert.Equal(2, inputDefaults.Count);
                Assert.True(inputDefaults.ContainsKey("greeting"));
                Assert.Equal("Hello", inputDefaults["greeting"]);
                Assert.True(string.IsNullOrEmpty(inputDefaults["entryPoint"]));
                Assert.NotNull(definition.Data.Execution); // execution


                // Node handler should always be deserialized.
                Assert.NotNull(definition.Data.Execution.ContainerAction); // execution.Node
                Assert.Equal(definition.Data.Execution.ContainerAction, definition.Data.Execution.All[0]);
                Assert.Equal("Dockerfile", definition.Data.Execution.ContainerAction.Target);
                Assert.Equal("image:1234", definition.Data.Execution.ContainerAction.ContainerImage);
                Assert.Equal("main.sh", definition.Data.Execution.ContainerAction.EntryPoint);

                foreach (var arg in definition.Data.Execution.ContainerAction.Arguments)
                {
                    Assert.Equal("${{ inputs.greeting }}", arg.AssertScalar("arg").ToString());
                }

                foreach (var env in definition.Data.Execution.ContainerAction.Environment)
                {
                    var key = env.Key.AssertString("key").Value;
                    if (key == "Token")
                    {
                        Assert.Equal("foo", env.Value.AssertString("value").Value);
                    }
                    else if (key == "Url")
                    {
                        Assert.Equal("bar", env.Value.AssertString("value").Value);
                    }
                    else
                    {
                        throw new NotSupportedException(key);
                    }
                }
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsContainerActionDefinitionRegistry()
        {
            try
            {
                // Arrange.
                Setup();
                // Prepare the task.json content.
                const string Content = @"
# Container action
name: 'Hello World'
description: 'Greet the world and record the time'
author: 'GitHub'
inputs: 
  greeting: # id of input
    description: 'The greeting we choose - will print ""{greeting}, World!"" on stdout'
    required: true
    default: 'hello'
  entryPoint: # id of input
    description: 'optional docker entrypoint overwrite.'
    required: false
outputs:
  time: # id of output
    description: 'The time we did the greeting'
icon: 'hello.svg' # vector art to display in the GitHub Marketplace
color: 'green' # optional, decorates the entry in the GitHub Marketplace
runs:
  using: 'docker'
  image: 'docker://ubuntu:16.04'
  args:
  - '${{ inputs.greeting }}'
  entrypoint: 'main.sh'
  env:
    Token: foo
    Url: ${{inputs.greeting}}
";
                Pipelines.ActionStep instance;
                string directory;
                CreateAction(yamlContent: Content, instance: out instance, directory: out directory);

                _actionManager.CachedActionContainers[instance.Id] = new ContainerInfo() { ContainerImage = "ubuntu:16.04" };
                // Act.
                Definition definition = _actionManager.LoadAction(_ec.Object, instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.Equal(directory, definition.Directory);
                Assert.NotNull(definition.Data);
                Assert.NotNull(definition.Data.Inputs); // inputs
                Dictionary<string, string> inputDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var input in definition.Data.Inputs)
                {
                    var name = input.Key.AssertString("key").Value;
                    var value = input.Value.AssertScalar("value").ToString();

                    _hc.GetTrace().Info($"Default: {name} = {value}");
                    inputDefaults[name] = value;
                }

                Assert.Equal(2, inputDefaults.Count);
                Assert.True(inputDefaults.ContainsKey("greeting"));
                Assert.Equal("hello", inputDefaults["greeting"]);
                Assert.True(string.IsNullOrEmpty(inputDefaults["entryPoint"]));
                Assert.NotNull(definition.Data.Execution); // execution


                // Node handler should always be deserialized.
                Assert.NotNull(definition.Data.Execution.ContainerAction); // execution.Node
                Assert.Equal(definition.Data.Execution.ContainerAction, definition.Data.Execution.All[0]);
                Assert.Equal("docker://ubuntu:16.04", definition.Data.Execution.ContainerAction.Target);
                Assert.Equal("ubuntu:16.04", definition.Data.Execution.ContainerAction.ContainerImage);
                Assert.Equal("main.sh", definition.Data.Execution.ContainerAction.EntryPoint);

                foreach (var arg in definition.Data.Execution.ContainerAction.Arguments)
                {
                    Assert.Equal("${{ inputs.greeting }}", arg.AssertScalar("arg").ToString());
                }

                foreach (var env in definition.Data.Execution.ContainerAction.Environment)
                {
                    var key = env.Key.AssertString("key").Value;
                    if (key == "Token")
                    {
                        Assert.Equal("foo", env.Value.AssertString("value").Value);
                    }
                    else if (key == "Url")
                    {
                        Assert.Equal("${{ inputs.greeting }}", env.Value.AssertScalar("value").ToString());
                    }
                    else
                    {
                        throw new NotSupportedException(key);
                    }
                }

            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsNodeActionDefinition()
        {
            try
            {
                // Arrange.
                Setup();
                const string Content = @"
# Container action
name: 'Hello World'
description: 'Greet the world and record the time'
author: 'GitHub'
inputs: 
  greeting: # id of input
    description: 'The greeting we choose - will print ""{greeting}, World!"" on stdout'
    required: true
    default: 'Hello'
  entryPoint: # id of input
    description: 'optional docker entrypoint overwrite.'
    required: false
outputs:
  time: # id of output
    description: 'The time we did the greeting'
icon: 'hello.svg' # vector art to display in the GitHub Marketplace
color: 'green' # optional, decorates the entry in the GitHub Marketplace
runs:
  using: 'node'
  main: 'task.js'
";
                Pipelines.ActionStep instance;
                string directory;
                CreateAction(yamlContent: Content, instance: out instance, directory: out directory);

                // Act.
                Definition definition = _actionManager.LoadAction(_ec.Object, instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.Equal(directory, definition.Directory);
                Assert.NotNull(definition.Data);
                Assert.NotNull(definition.Data.Inputs); // inputs
                Dictionary<string, string> inputDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var input in definition.Data.Inputs)
                {
                    var name = input.Key.AssertString("key").Value;
                    var value = input.Value.AssertScalar("value").ToString();

                    _hc.GetTrace().Info($"Default: {name} = {value}");
                    inputDefaults[name] = value;
                }

                Assert.Equal(2, inputDefaults.Count);
                Assert.True(inputDefaults.ContainsKey("greeting"));
                Assert.Equal("Hello", inputDefaults["greeting"]);
                Assert.True(string.IsNullOrEmpty(inputDefaults["entryPoint"]));
                Assert.NotNull(definition.Data.Execution); // execution


                // Node handler should always be deserialized.
                Assert.NotNull(definition.Data.Execution.NodeAction); // execution.Node
                Assert.Equal(definition.Data.Execution.NodeAction, definition.Data.Execution.All[0]);
                Assert.Equal("task.js", definition.Data.Execution.NodeAction.Target);
            }
            finally
            {
                Teardown();
            }
        }

        private void CreateAction(string yamlContent, out Pipelines.ActionStep instance, out string directory)
        {
            directory = Path.Combine(_workFolder, Constants.Path.ActionsDirectory, "GitHub/actions".Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "master");
            string file = Path.Combine(directory, Constants.Path.ActionManifestFile);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, yamlContent);
            instance = new Pipelines.ActionStep()
            {
                Id = Guid.NewGuid(),
                Reference = new Pipelines.RepositoryPathReference()
                {
                    Name = "GitHub/actions",
                    Ref = "master",
                    RepositoryType = Pipelines.RepositoryTypes.GitHub
                }
            };
        }

        private void Setup([CallerMemberName] string name = "")
        {
            _ecTokenSource?.Dispose();
            _ecTokenSource = new CancellationTokenSource();

            // Mocks.
            _jobServer = new Mock<IJobServer>();

            // Test host context.
            _hc = new TestHostContext(this, name);

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Setup(x => x.Variables).Returns(new Variables(_hc, new Dictionary<string, VariableValue>()));
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"[{tag}]{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>())).Callback((Issue issue, string message) => { _hc.GetTrace().Info($"[{issue.Type}]{issue.Message ?? message}"); });

            _dockerManager = new Mock<IDockerCommandManager>();
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:16.04")).Returns(Task.FromResult(0));
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:100.04")).Returns(Task.FromResult(1));

            _dockerManager.Setup(x => x.DockerBuild(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(0));

            _pluginManager = new Mock<IRunnerPluginManager>();
            _pluginManager.Setup(x => x.GetPluginAction(It.IsAny<string>())).Returns(new RunnerPluginActionInfo() { PluginTypeName = "plugin.class, plugin" });

            var actionManifest = new ActionManifestManager();
            actionManifest.Initialize(_hc);

            // Random work folder.
            _workFolder = _hc.GetDirectory(WellKnownDirectory.Work);

            _hc.SetSingleton<IJobServer>(_jobServer.Object);
            _hc.SetSingleton<IDockerCommandManager>(_dockerManager.Object);
            _hc.SetSingleton<IRunnerPluginManager>(_pluginManager.Object);
            _hc.SetSingleton<IActionManifestManager>(actionManifest);

            var proxy = new RunnerWebProxy();
            proxy.Initialize(_hc);
            _hc.SetSingleton<IRunnerWebProxy>(proxy);

            _configurationStore = new Mock<IConfigurationStore>();
            _configurationStore
                .Setup(x => x.GetSettings())
                .Returns(
                    new RunnerSettings
                    {
                        WorkFolder = _workFolder
                    });
            _hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);

            var pInvoker = new ProcessInvokerWrapper();
            pInvoker.Initialize(_hc);
            _hc.EnqueueInstance<IProcessInvoker>(pInvoker);

            // Instance to test.
            _actionManager = new ActionManager();
            _actionManager.Initialize(_hc);

            Environment.SetEnvironmentVariable("GITHUB_ACTION_DOWNLOAD_NO_BACKOFF", "1");
        }

        private void Teardown()
        {
            _hc?.Dispose();
            if (!string.IsNullOrEmpty(_workFolder) && Directory.Exists(_workFolder))
            {
                Directory.Delete(_workFolder, recursive: true);
            }
        }

        private class ExceptionStream : Stream
        {
            public override bool CanRead => throw new NotImplementedException();

            public override bool CanSeek => throw new NotImplementedException();

            public override bool CanWrite => throw new NotImplementedException();

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
    }
}
