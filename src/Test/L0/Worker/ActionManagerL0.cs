﻿using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
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
        private Mock<IConfigurationStore> _configurationStore;
        private Mock<IDockerCommandManager> _dockerManager;
        private Mock<IExecutionContext> _ec;
        private Mock<IRunnerPluginManager> _pluginManager;
        private TestHostContext _hc;
        private ActionManager _actionManager;
        private string _workFolder;

#if OS_LINUX
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
                Assert.Equal(actionId, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal("ubuntu:16.04", (steps[0].Data as ContainerSetupInfo).Container.Image);
            }
            finally
            {
                Teardown();
            }
        }
#endif

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
                            Name = "actions/download-artifact",
                            Ref = "master",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions/download-artifact", "master.completed");
                Assert.True(File.Exists(watermarkFile));

                var actionYamlFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions/download-artifact", "master", "action.yml");
                Assert.True(File.Exists(actionYamlFile));
                _hc.GetTrace().Info(File.ReadAllText(actionYamlFile));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_AlwaysClearActionsCache()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>();

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "notexist/no", "notexist.completed");
                Directory.CreateDirectory(Path.GetDirectoryName(watermarkFile));
                File.WriteAllText(watermarkFile, DateTime.UtcNow.ToString());
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist"));
                File.Copy(Path.Combine(TestUtil.GetSrcPath(), "Test", TestDataFolderName, "dockerfileaction.yml"), Path.Combine(Path.GetDirectoryName(watermarkFile), "notexist", "action.yml"));

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                // Make sure _actions folder get deleted
                Assert.False(Directory.Exists(_hc.GetDirectory(WellKnownDirectory.Actions)));
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
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.True(steps.Count == 0);
            }
            finally
            {
                Teardown();
            }
        }

#if OS_LINUX
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
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithdockerfile",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "repositoryactionwithdockerfile");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);
                Assert.Equal(actionId, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal(actionDir, (steps[0].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "Dockerfile"), (steps[0].Data as ContainerSetupInfo).Container.Dockerfile);
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
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithdockerfileinrelativepath",
                            Path = "images/cli",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "repositoryactionwithdockerfileinrelativepath");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal(actionId, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal(actionDir, (steps[0].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "images/cli", "Dockerfile"), (steps[0].Data as ContainerSetupInfo).Container.Dockerfile);
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
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithdockerfileinrelativepath",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "repositoryactionwithdockerfileinrelativepath");
                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal(actionId, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal(actionDir, (steps[0].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "Dockerfile"), (steps[0].Data as ContainerSetupInfo).Container.Dockerfile);
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
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithActionfile_DockerfileRelativePath",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "RepositoryActionWithActionfile_DockerfileRelativePath");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal(actionId, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal(actionDir, (steps[0].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "images/Dockerfile"), (steps[0].Data as ContainerSetupInfo).Container.Dockerfile);
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
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithActionfile_DockerHubImage",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "RepositoryActionWithActionfile_DockerHubImage");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal(actionId, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal("ubuntu:18.04", (steps[0].Data as ContainerSetupInfo).Container.Image);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithActionYamlFile_DockerHubImage()
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
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithActionYamlFile_DockerHubImage",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "RepositoryActionWithActionYamlFile_DockerHubImage");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal((steps[0].Data as ContainerSetupInfo).StepIds[0], actionId);
                Assert.Equal("ubuntu:18.04", (steps[0].Data as ContainerSetupInfo).Container.Image);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_RepositoryActionWithActionfileAndDockerfile()
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
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithactionfileanddockerfile",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "repositoryactionwithactionfileanddockerfile");

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                Assert.Equal(actionId, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal(actionDir, (steps[0].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "Dockerfile"), (steps[0].Data as ContainerSetupInfo).Container.Dockerfile);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_NotPullOrBuildImagesMultipleTimes()
        {
            try
            {
                //Arrange
                Setup();
                var actionId1 = Guid.NewGuid();
                var actionId2 = Guid.NewGuid();
                var actionId3 = Guid.NewGuid();
                var actionId4 = Guid.NewGuid();
                var actionId5 = Guid.NewGuid();
                var actionId6 = Guid.NewGuid();
                var actionId7 = Guid.NewGuid();
                var actionId8 = Guid.NewGuid();
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId1,
                        Reference = new Pipelines.ContainerRegistryReference()
                        {
                            Image = "ubuntu:16.04"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId2,
                        Reference = new Pipelines.ContainerRegistryReference()
                        {
                            Image = "ubuntu:18.04"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId3,
                        Reference = new Pipelines.ContainerRegistryReference()
                        {
                            Image = "ubuntu:18.04"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId4,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "notpullorbuildimagesmultipletimes1",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId5,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithdockerfile",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId6,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithdockerfileinrelativepath",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId7,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithdockerfileinrelativepath",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId8,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "repositoryactionwithdockerfileinrelativepath",
                            Path = "images/cli",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                Assert.Equal(actionId1, (steps[0].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal("ubuntu:16.04", (steps[0].Data as ContainerSetupInfo).Container.Image);

                Assert.Contains(actionId2, (steps[1].Data as ContainerSetupInfo).StepIds);
                Assert.Contains(actionId3, (steps[1].Data as ContainerSetupInfo).StepIds);
                Assert.Contains(actionId4, (steps[1].Data as ContainerSetupInfo).StepIds);
                Assert.Equal("ubuntu:18.04", (steps[1].Data as ContainerSetupInfo).Container.Image);

                var actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "repositoryactionwithdockerfile");

                Assert.Equal(actionId5, (steps[2].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal(actionDir, (steps[2].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "Dockerfile"), (steps[2].Data as ContainerSetupInfo).Container.Dockerfile);

                actionDir = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang", "runner_L0", "repositoryactionwithdockerfileinrelativepath");

                Assert.Contains(actionId6, (steps[3].Data as ContainerSetupInfo).StepIds);
                Assert.Contains(actionId7, (steps[3].Data as ContainerSetupInfo).StepIds);
                Assert.Equal(actionDir, (steps[3].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "Dockerfile"), (steps[3].Data as ContainerSetupInfo).Container.Dockerfile);

                Assert.Equal(actionId8, (steps[4].Data as ContainerSetupInfo).StepIds[0]);
                Assert.Equal(actionDir, (steps[4].Data as ContainerSetupInfo).Container.WorkingDirectory);
                Assert.Equal(Path.Combine(actionDir, "images/cli", "Dockerfile"), (steps[4].Data as ContainerSetupInfo).Container.Dockerfile);
            }
            finally
            {
                Teardown();
            }
        }
#endif

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
                            Name = "actions/setup-node",
                            Ref = "v1",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var steps = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                // node.js based action doesn't need any extra steps to build/pull containers.
                Assert.True(steps.Count == 0);
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
                Assert.Equal("ubuntu:16.04", (definition.Data.Execution as ContainerActionExecutionData).Image);
                Assert.True(string.IsNullOrEmpty((definition.Data.Execution as ContainerActionExecutionData).EntryPoint));
                Assert.Null((definition.Data.Execution as ContainerActionExecutionData).Arguments);
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
                Assert.True(definition.Data.Execution.ExecutionType == ActionExecutionType.Script);
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

                Assert.NotNull((definition.Data.Execution as ContainerActionExecutionData)); // execution.Node
                Assert.Equal("image:1234", (definition.Data.Execution as ContainerActionExecutionData).Image);
                Assert.Equal("main.sh", (definition.Data.Execution as ContainerActionExecutionData).EntryPoint);

                foreach (var arg in (definition.Data.Execution as ContainerActionExecutionData).Arguments)
                {
                    Assert.Equal("${{ inputs.greeting }}", arg.AssertScalar("arg").ToString());
                }

                foreach (var env in (definition.Data.Execution as ContainerActionExecutionData).Environment)
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

                Assert.NotNull((definition.Data.Execution as ContainerActionExecutionData));
                Assert.Equal("ubuntu:16.04", (definition.Data.Execution as ContainerActionExecutionData).Image);
                Assert.Equal("main.sh", (definition.Data.Execution as ContainerActionExecutionData).EntryPoint);

                foreach (var arg in (definition.Data.Execution as ContainerActionExecutionData).Arguments)
                {
                    Assert.Equal("${{ inputs.greeting }}", arg.AssertScalar("arg").ToString());
                }

                foreach (var env in (definition.Data.Execution as ContainerActionExecutionData).Environment)
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
  using: 'node12'
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

                Assert.NotNull((definition.Data.Execution as NodeJSActionExecutionData));
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsNodeActionDefinitionYaml()
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
  using: 'node12'
  main: 'task.js'
";
                Pipelines.ActionStep instance;
                string directory;
                directory = Path.Combine(_workFolder, Constants.Path.ActionsDirectory, "GitHub/actions".Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "master");
                string file = Path.Combine(directory, Constants.Path.ActionManifestYamlFile);
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, Content);
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

                Assert.NotNull((definition.Data.Execution as NodeJSActionExecutionData));
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsContainerActionDefinitionDockerfile_SelfRepo()
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
                CreateSelfRepoAction(yamlContent: Content, instance: out instance, directory: out directory);

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

                Assert.NotNull((definition.Data.Execution as ContainerActionExecutionData)); // execution.Node
                Assert.Equal("Dockerfile", (definition.Data.Execution as ContainerActionExecutionData).Image);
                Assert.Equal("main.sh", (definition.Data.Execution as ContainerActionExecutionData).EntryPoint);

                foreach (var arg in (definition.Data.Execution as ContainerActionExecutionData).Arguments)
                {
                    Assert.Equal("${{ inputs.greeting }}", arg.AssertScalar("arg").ToString());
                }

                foreach (var env in (definition.Data.Execution as ContainerActionExecutionData).Environment)
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
        public void LoadsContainerActionDefinitionRegistry_SelfRepo()
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
                CreateSelfRepoAction(yamlContent: Content, instance: out instance, directory: out directory);

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

                Assert.NotNull((definition.Data.Execution as ContainerActionExecutionData));
                Assert.Equal("docker://ubuntu:16.04", (definition.Data.Execution as ContainerActionExecutionData).Image);
                Assert.Equal("main.sh", (definition.Data.Execution as ContainerActionExecutionData).EntryPoint);

                foreach (var arg in (definition.Data.Execution as ContainerActionExecutionData).Arguments)
                {
                    Assert.Equal("${{ inputs.greeting }}", arg.AssertScalar("arg").ToString());
                }

                foreach (var env in (definition.Data.Execution as ContainerActionExecutionData).Environment)
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
        public void LoadsNodeActionDefinition_SelfRepo()
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
  using: 'node12'
  main: 'task.js'
";
                Pipelines.ActionStep instance;
                string directory;
                CreateSelfRepoAction(yamlContent: Content, instance: out instance, directory: out directory);

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

                Assert.NotNull((definition.Data.Execution as NodeJSActionExecutionData));
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsNodeActionDefinition_Cleanup()
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
  using: 'node12'
  main: 'task.js'
  post: 'cleanup.js'
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

                Assert.NotNull((definition.Data.Execution as NodeJSActionExecutionData));
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
                Assert.Equal("cleanup.js", (definition.Data.Execution as NodeJSActionExecutionData).Cleanup);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsContainerActionDefinitionDockerfile_Cleanup()
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
  post-entrypoint: 'cleanup.sh'
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

                Assert.NotNull((definition.Data.Execution as ContainerActionExecutionData)); // execution.Node
                Assert.Equal("image:1234", (definition.Data.Execution as ContainerActionExecutionData).Image);
                Assert.Equal("main.sh", (definition.Data.Execution as ContainerActionExecutionData).EntryPoint);
                Assert.Equal("cleanup.sh", (definition.Data.Execution as ContainerActionExecutionData).Cleanup);

                foreach (var arg in (definition.Data.Execution as ContainerActionExecutionData).Arguments)
                {
                    Assert.Equal("${{ inputs.greeting }}", arg.AssertScalar("arg").ToString());
                }

                foreach (var env in (definition.Data.Execution as ContainerActionExecutionData).Environment)
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
        public void LoadsPluginActionDefinition()
        {
            try
            {
                // Arrange.
                Setup();
                const string Content = @"
name: 'Hello World'
description: 'Greet the world and record the time'
author: 'Test Corporation'
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
  plugin: 'someplugin'
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

                Assert.NotNull((definition.Data.Execution as PluginActionExecutionData));
                Assert.Equal("plugin.class, plugin", (definition.Data.Execution as PluginActionExecutionData).Plugin);
                Assert.Equal("plugin.cleanup, plugin", (definition.Data.Execution as PluginActionExecutionData).Cleanup);
            }
            finally
            {
                Teardown();
            }
        }

        private void CreateAction(string yamlContent, out Pipelines.ActionStep instance, out string directory)
        {
            directory = Path.Combine(_workFolder, Constants.Path.ActionsDirectory, "GitHub/actions".Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "master");
            string file = Path.Combine(directory, Constants.Path.ActionManifestYmlFile);
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

        private void CreateSelfRepoAction(string yamlContent, out Pipelines.ActionStep instance, out string directory)
        {
            directory = Path.Combine(_workFolder, "actions", "actions");
            string file = Path.Combine(directory, Constants.Path.ActionManifestYmlFile);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, yamlContent);
            instance = new Pipelines.ActionStep()
            {
                Id = Guid.NewGuid(),
                Reference = new Pipelines.RepositoryPathReference()
                {
                    Name = "GitHub/actions",
                    Ref = "master",
                    RepositoryType = Pipelines.PipelineConstants.SelfAlias
                }
            };
        }

        private void Setup([CallerMemberName] string name = "")
        {
            _ecTokenSource?.Dispose();
            _ecTokenSource = new CancellationTokenSource();

            // Test host context.
            _hc = new TestHostContext(this, name);

            // Random work folder.
            _workFolder = _hc.GetDirectory(WellKnownDirectory.Work);

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Setup(x => x.Variables).Returns(new Variables(_hc, new Dictionary<string, VariableValue>()));
            _ec.Setup(x => x.ExpressionValues).Returns(new DictionaryContextData());
            _ec.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"[{tag}]{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>())).Callback((Issue issue, string message) => { _hc.GetTrace().Info($"[{issue.Type}]{issue.Message ?? message}"); });
            _ec.Setup(x => x.GetGitHubContext("workspace")).Returns(Path.Combine(_workFolder, "actions", "actions"));

            _dockerManager = new Mock<IDockerCommandManager>();
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:16.04")).Returns(Task.FromResult(0));
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:100.04")).Returns(Task.FromResult(1));

            _dockerManager.Setup(x => x.DockerBuild(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(0));

            _pluginManager = new Mock<IRunnerPluginManager>();
            _pluginManager.Setup(x => x.GetPluginAction(It.IsAny<string>())).Returns(new RunnerPluginActionInfo() { PluginTypeName = "plugin.class, plugin", PostPluginTypeName = "plugin.cleanup, plugin" });

            var actionManifest = new ActionManifestManager();
            actionManifest.Initialize(_hc);

            _hc.SetSingleton<IDockerCommandManager>(_dockerManager.Object);
            _hc.SetSingleton<IRunnerPluginManager>(_pluginManager.Object);
            _hc.SetSingleton<IActionManifestManager>(actionManifest);

            _configurationStore = new Mock<IConfigurationStore>();
            _configurationStore
                .Setup(x => x.GetSettings())
                .Returns(
                    new RunnerSettings
                    {
                        WorkFolder = _workFolder
                    });
            _hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);

            var pInvoker1 = new ProcessInvokerWrapper();
            pInvoker1.Initialize(_hc);
            var pInvoker2 = new ProcessInvokerWrapper();
            pInvoker2.Initialize(_hc);
            var pInvoker3 = new ProcessInvokerWrapper();
            pInvoker3.Initialize(_hc);
            var pInvoker4 = new ProcessInvokerWrapper();
            pInvoker4.Initialize(_hc);
            var pInvoker5 = new ProcessInvokerWrapper();
            pInvoker5.Initialize(_hc);
            _hc.EnqueueInstance<IProcessInvoker>(pInvoker1);
            _hc.EnqueueInstance<IProcessInvoker>(pInvoker2);
            _hc.EnqueueInstance<IProcessInvoker>(pInvoker3);
            _hc.EnqueueInstance<IProcessInvoker>(pInvoker4);
            _hc.EnqueueInstance<IProcessInvoker>(pInvoker5);

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
    }
}
