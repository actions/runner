using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Moq;
using Moq.Protected;
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
        private Mock<IJobServer> _jobServer;
        private Mock<IRunnerPluginManager> _pluginManager;
        private TestHostContext _hc;
        private ActionManager _actionManager;
        private string _workFolder;

#if OS_LINUX
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_PullImageFromDockerHub_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_DownloadActionFromGraph_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
        public async void PrepareActions_DownloadBuiltInActionFromGraph_OnPremises_Legacy()
        {
            try
            {
                // Arrange
                Setup(newActionMetadata: false);
                const string ActionName = "actions/sample-action";
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = Guid.NewGuid(),
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = ActionName,
                            Ref = "main",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                // Return a valid action from GHES via mock
                const string ApiUrl = "https://ghes.example.com/api/v3";
                string expectedArchiveLink = GetLinkToActionArchive(ApiUrl, ActionName, "main");
                string archiveFile = await CreateRepoArchive();
                using var stream = File.OpenRead(archiveFile);
                var mockClientHandler = new Mock<HttpClientHandler>();
                mockClientHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.RequestUri == new Uri(expectedArchiveLink)), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) });

                var mockHandlerFactory = new Mock<IHttpClientHandlerFactory>();
                mockHandlerFactory.Setup(p => p.CreateClientHandler(It.IsAny<RunnerWebProxy>())).Returns(mockClientHandler.Object);
                _hc.SetSingleton(mockHandlerFactory.Object);

                _ec.Setup(x => x.GetGitHubContext("api_url")).Returns(ApiUrl);
                _configurationStore.Object.GetSettings().IsHostedServer = false;

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), ActionName, "main.completed");
                Assert.True(File.Exists(watermarkFile));

                var actionYamlFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), ActionName, "main", "action.yml");
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
        public async void PrepareActions_DownloadActionFromDotCom_OnPremises_Legacy()
        {
            try
            {
                // Arrange
                Setup(newActionMetadata: false);
                const string ActionName = "ownerName/sample-action";
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = Guid.NewGuid(),
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = ActionName,
                            Ref = "main",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                // Return a valid action from GHES via mock
                const string ApiUrl = "https://ghes.example.com/api/v3";
                string builtInArchiveLink = GetLinkToActionArchive(ApiUrl, ActionName, "main");
                string dotcomArchiveLink = GetLinkToActionArchive("https://api.github.com", ActionName, "main");
                string archiveFile = await CreateRepoArchive();
                using var stream = File.OpenRead(archiveFile);
                var mockClientHandler = new Mock<HttpClientHandler>();
                mockClientHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.RequestUri == new Uri(builtInArchiveLink)), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));
                mockClientHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.RequestUri == new Uri(dotcomArchiveLink)), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) });

                var mockHandlerFactory = new Mock<IHttpClientHandlerFactory>();
                mockHandlerFactory.Setup(p => p.CreateClientHandler(It.IsAny<RunnerWebProxy>())).Returns(mockClientHandler.Object);
                _hc.SetSingleton(mockHandlerFactory.Object);

                _ec.Setup(x => x.GetGitHubContext("api_url")).Returns(ApiUrl);
                _configurationStore.Object.GetSettings().IsHostedServer = false;

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), ActionName, "main.completed");
                Assert.True(File.Exists(watermarkFile));

                var actionYamlFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), ActionName, "main", "action.yml");
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
        public async void PrepareActions_DownloadUnknownActionFromGraph_OnPremises_Legacy()
        {
            try
            {
                // Arrange
                Setup(newActionMetadata: false);
                const string ActionName = "ownerName/sample-action";
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = Guid.NewGuid(),
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = ActionName,
                            Ref = "main",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                // Return a valid action from GHES via mock
                const string ApiUrl = "https://ghes.example.com/api/v3";
                string archiveLink = GetLinkToActionArchive(ApiUrl, ActionName, "main");
                string archiveFile = await CreateRepoArchive();
                using var stream = File.OpenRead(archiveFile);
                var mockClientHandler = new Mock<HttpClientHandler>();
                mockClientHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

                var mockHandlerFactory = new Mock<IHttpClientHandlerFactory>();
                mockHandlerFactory.Setup(p => p.CreateClientHandler(It.IsAny<RunnerWebProxy>())).Returns(mockClientHandler.Object);
                _hc.SetSingleton(mockHandlerFactory.Object);

                _ec.Setup(x => x.GetGitHubContext("api_url")).Returns(ApiUrl);
                _configurationStore.Object.GetSettings().IsHostedServer = false;

                //Act
                Func<Task> action = async () => await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                await Assert.ThrowsAsync<ActionNotFoundException>(action);

                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), ActionName, "main.completed");
                Assert.False(File.Exists(watermarkFile));

                var actionYamlFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), ActionName, "main", "action.yml");
                Assert.False(File.Exists(actionYamlFile));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_AlwaysClearActionsCache_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
        public async void PrepareActions_SkipDownloadActionForSelfRepo_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithDockerfile_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;
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
        public async void PrepareActions_RepositoryActionWithDockerfileInRelativePath_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithActionfile_Dockerfile_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithActionfile_DockerfileRelativePath_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithActionfile_DockerHubImage_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithActionYamlFile_DockerHubImage_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithActionfileAndDockerfile_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_NotPullOrBuildImagesMultipleTimes_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithActionfile_Node_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithInvalidWrapperActionfile_Node_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);
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
                            Ref = "RepositoryActionWithInvalidWrapperActionfile_Node",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                try
                {
                    await _actionManager.PrepareActionsAsync(_ec.Object, actions);
                }
                catch (ArgumentException)
                {
                    var traceFile = Path.GetTempFileName();
                    File.Copy(_hc.TraceFileName, traceFile, true);
                    Assert.Contains("You are using a JavaScript Action but there is not an entry JavaScript file provided in", File.ReadAllText(traceFile));
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
        public async void PrepareActions_RepositoryActionWithWrapperActionfile_PreSteps_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);

                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                var actionId1 = Guid.NewGuid();
                var actionId2 = Guid.NewGuid();
                _hc.GetTrace().Info(actionId1);
                _hc.GetTrace().Info(actionId2);
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action1",
                        Id = actionId1,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithWrapperActionfile_Node",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action2",
                        Id = actionId2,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithWrapperActionfile_Docker",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var preResult = await _actionManager.PrepareActionsAsync(_ec.Object, actions);
                Assert.Equal(2, preResult.PreStepTracker.Count);
                Assert.NotNull(preResult.PreStepTracker[actionId1]);
                Assert.NotNull(preResult.PreStepTracker[actionId2]);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsContainerRegistryActionDefinition_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);

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
        public void LoadsScriptActionDefinition_Legacy()
        {
            try
            {
                //Arrange
                Setup(newActionMetadata: false);

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
        public void LoadsContainerActionDefinitionDockerfile_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
        public void LoadsContainerActionDefinitionRegistry_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
        public void LoadsNodeActionDefinition_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
        public void LoadsNodeActionDefinitionYaml_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
                directory = Path.Combine(_workFolder, Constants.Path.ActionsDirectory, "GitHub/actions".Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "main");
                string file = Path.Combine(directory, Constants.Path.ActionManifestYamlFile);
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, Content);
                instance = new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.RepositoryPathReference()
                    {
                        Name = "GitHub/actions",
                        Ref = "main",
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
        public void LoadsContainerActionDefinitionDockerfile_SelfRepo_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
        public void LoadsContainerActionDefinitionRegistry_SelfRepo_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
        public void LoadsNodeActionDefinition_SelfRepo_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
        public void LoadsNodeActionDefinition_Cleanup_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
                Assert.Equal("cleanup.js", (definition.Data.Execution as NodeJSActionExecutionData).Post);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsContainerActionDefinitionDockerfile_Cleanup_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
                Assert.Equal("cleanup.sh", (definition.Data.Execution as ContainerActionExecutionData).Post);

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
        public void LoadsPluginActionDefinition_Legacy()
        {
            try
            {
                // Arrange.
                Setup(newActionMetadata: false);
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
                Assert.Equal("plugin.cleanup, plugin", (definition.Data.Execution as PluginActionExecutionData).Post);
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
        public async void PrepareActions_PullImageFromDockerHub()
        {
            try
            {
                //Arrange
                Setup();
                // _ec.Variables.
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;
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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

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
        public async void PrepareActions_RepositoryActionWithInvalidWrapperActionfile_Node()
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
                            Ref = "RepositoryActionWithInvalidWrapperActionfile_Node",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                try
                {
                    await _actionManager.PrepareActionsAsync(_ec.Object, actions);
                }
                catch (ArgumentException)
                {
                    var traceFile = Path.GetTempFileName();
                    File.Copy(_hc.TraceFileName, traceFile, true);
                    Assert.Contains("You are using a JavaScript Action but there is not an entry JavaScript file provided in", File.ReadAllText(traceFile));
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
        public async void PrepareActions_RepositoryActionWithWrapperActionfile_PreSteps()
        {
            try
            {
                //Arrange
                Setup();

                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                var actionId1 = Guid.NewGuid();
                var actionId2 = Guid.NewGuid();
                _hc.GetTrace().Info(actionId1);
                _hc.GetTrace().Info(actionId2);
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action1",
                        Id = actionId1,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithWrapperActionfile_Node",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action2",
                        Id = actionId2,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithWrapperActionfile_Docker",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var preResult = await _actionManager.PrepareActionsAsync(_ec.Object, actions);
                Assert.Equal(2, preResult.PreStepTracker.Count);
                Assert.NotNull(preResult.PreStepTracker[actionId1]);
                Assert.NotNull(preResult.PreStepTracker[actionId2]);
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
                directory = Path.Combine(_workFolder, Constants.Path.ActionsDirectory, "GitHub/actions".Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "main");
                string file = Path.Combine(directory, Constants.Path.ActionManifestYamlFile);
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, Content);
                instance = new Pipelines.ActionStep()
                {
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.RepositoryPathReference()
                    {
                        Name = "GitHub/actions",
                        Ref = "main",
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
                Assert.Equal("cleanup.js", (definition.Data.Execution as NodeJSActionExecutionData).Post);
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
                Assert.Equal("cleanup.sh", (definition.Data.Execution as ContainerActionExecutionData).Post);

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
                Assert.Equal("plugin.cleanup, plugin", (definition.Data.Execution as PluginActionExecutionData).Post);
            }
            finally
            {
                Teardown();
            }
        }

        private void CreateAction(string yamlContent, out Pipelines.ActionStep instance, out string directory)
        {
            directory = Path.Combine(_workFolder, Constants.Path.ActionsDirectory, "GitHub/actions".Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "main");
            string file = Path.Combine(directory, Constants.Path.ActionManifestYmlFile);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, yamlContent);
            instance = new Pipelines.ActionStep()
            {
                Id = Guid.NewGuid(),
                Reference = new Pipelines.RepositoryPathReference()
                {
                    Name = "GitHub/actions",
                    Ref = "main",
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
                    Ref = "main",
                    RepositoryType = Pipelines.PipelineConstants.SelfAlias
                }
            };
        }

        /// <summary>
        /// Creates a sample action in an archive on disk, similar to the archive
        /// retrieved from GitHub's or GHES' repository API.
        /// </summary>
        /// <returns>The path on disk to the archive.</returns>
#if OS_WINDOWS
        private Task<string> CreateRepoArchive()
#else
        private async Task<string> CreateRepoArchive()
#endif
        {
            const string Content = @"
# Container action
name: 'Hello World'
description: 'Greet the world'
author: 'GitHub'
icon: 'hello.svg' # vector art to display in the GitHub Marketplace
color: 'green' # optional, decorates the entry in the GitHub Marketplace
runs:
  using: 'node12'
  main: 'task.js'
";
            CreateAction(yamlContent: Content, instance: out _, directory: out string directory);

            var tempDir = _hc.GetDirectory(WellKnownDirectory.Temp);
            Directory.CreateDirectory(tempDir);
            var archiveFile = Path.Combine(tempDir, Path.GetRandomFileName());
            var trace = _hc.GetTrace();

#if OS_WINDOWS
            ZipFile.CreateFromDirectory(directory, archiveFile, CompressionLevel.Fastest, includeBaseDirectory: true);
            return Task.FromResult(archiveFile);
#else
            string tar = WhichUtil.Which("tar", require: true, trace: trace);

            // tar -xzf
            using (var processInvoker = new ProcessInvokerWrapper())
            {
                processInvoker.Initialize(_hc);
                processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        trace.Info(args.Data);
                    }
                });

                processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        trace.Error(args.Data);
                    }
                });

                string cwd = Path.GetDirectoryName(directory);
                string inputDirectory = Path.GetFileName(directory);
                int exitCode = await processInvoker.ExecuteAsync(_hc.GetDirectory(WellKnownDirectory.Bin), tar, $"-czf \"{archiveFile}\" -C \"{cwd}\" \"{inputDirectory}\"", null, CancellationToken.None);
                if (exitCode != 0)
                {
                    throw new NotSupportedException($"Can't use 'tar -czf' to create archive file: {archiveFile}. return code: {exitCode}.");
                }
            }
            return archiveFile;
#endif
        }

        private static string GetLinkToActionArchive(string apiUrl, string repository, string @ref)
        {
#if OS_WINDOWS
            return $"{apiUrl}/repos/{repository}/zipball/{@ref}";
#else
            return $"{apiUrl}/repos/{repository}/tarball/{@ref}";
#endif
        }

        private void Setup([CallerMemberName] string name = "", bool newActionMetadata = true)
        {
            _ecTokenSource?.Dispose();
            _ecTokenSource = new CancellationTokenSource();

            // Test host context.
            _hc = new TestHostContext(this, name);

            // Random work folder.
            _workFolder = _hc.GetDirectory(WellKnownDirectory.Work);

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Global).Returns(new GlobalContext());
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            var variables = new Dictionary<string, VariableValue>();
            if (newActionMetadata)
            {
                variables["DistributedTask.NewActionMetadata"] = "true";
            }
            _ec.Object.Global.Variables = new Variables(_hc, variables);
            _ec.Setup(x => x.ExpressionValues).Returns(new DictionaryContextData());
            _ec.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            _ec.Object.Global.FileTable = new List<String>();
            _ec.Object.Global.Plan = new TaskOrchestrationPlanReference();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"[{tag}]{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>())).Callback((Issue issue, string message) => { _hc.GetTrace().Info($"[{issue.Type}]{issue.Message ?? message}"); });
            _ec.Setup(x => x.GetGitHubContext("workspace")).Returns(Path.Combine(_workFolder, "actions", "actions"));

            _dockerManager = new Mock<IDockerCommandManager>();
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:16.04")).Returns(Task.FromResult(0));
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:100.04")).Returns(Task.FromResult(1));

            _dockerManager.Setup(x => x.DockerBuild(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(0));

            _jobServer = new Mock<IJobServer>();
            _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                .Returns((Guid scopeIdentifier, string hubName, Guid planId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                {
                    var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                    foreach (var action in actions.Actions)
                    {
                        var key = $"{action.NameWithOwner}@{action.Ref}";
                        result.Actions[key] = new ActionDownloadInfo
                        {
                            NameWithOwner = action.NameWithOwner,
                            Ref = action.Ref,
                            TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                            ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                        };
                    }
                    return Task.FromResult(result);
                });

            _pluginManager = new Mock<IRunnerPluginManager>();
            _pluginManager.Setup(x => x.GetPluginAction(It.IsAny<string>())).Returns(new RunnerPluginActionInfo() { PluginTypeName = "plugin.class, plugin", PostPluginTypeName = "plugin.cleanup, plugin" });

            var actionManifest = new ActionManifestManager();
            actionManifest.Initialize(_hc);

            _hc.SetSingleton<IDockerCommandManager>(_dockerManager.Object);
            _hc.SetSingleton<IJobServer>(_jobServer.Object);
            _hc.SetSingleton<IRunnerPluginManager>(_pluginManager.Object);
            _hc.SetSingleton<IActionManifestManager>(actionManifest);
            _hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

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
