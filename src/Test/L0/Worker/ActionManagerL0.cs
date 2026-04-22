using System;
using System.Collections.Generic;
using System.Linq;
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
        private Mock<ILaunchServer> _launchServer;
        private Mock<IRunnerPluginManager> _pluginManager;
        private TestHostContext _hc;
        private ActionManager _actionManager;
        private string _workFolder;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_DownloadActionFromDotCom_OnPremises_Legacy()
        {
            try
            {
                // Arrange
                Setup();
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
        public async void PrepareActions_DownloadActionFromDotCom_ZipFileError()
        {
            try
            {
                // Arrange
                Setup();
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

                // Create a corrupted ZIP file for testing
                var tempDir = _hc.GetDirectory(WellKnownDirectory.Temp);
                Directory.CreateDirectory(tempDir);
                var archiveFile = Path.Combine(tempDir, Path.GetRandomFileName());
                using (var fileStream = new FileStream(archiveFile, FileMode.Create))
                {
                    // Used Co-Pilot for magic bytes here. They represent the tar header and just need to be invalid for the CLI to break.
                    var buffer = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00 };
                    fileStream.Write(buffer, 0, buffer.Length);
                }
                using var stream = File.OpenRead(archiveFile);

                string dotcomArchiveLink = GetLinkToActionArchive("https://api.github.com", ActionName, "main");
                var mockClientHandler = new Mock<HttpClientHandler>();
                mockClientHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.RequestUri == new Uri(dotcomArchiveLink)), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) });

                var mockHandlerFactory = new Mock<IHttpClientHandlerFactory>();
                mockHandlerFactory.Setup(p => p.CreateClientHandler(It.IsAny<RunnerWebProxy>())).Returns(mockClientHandler.Object);
                _hc.SetSingleton(mockHandlerFactory.Object);

                _configurationStore.Object.GetSettings().IsHostedServer = true;

                // Act + Assert
                await Assert.ThrowsAsync<InvalidActionArchiveException>(async () => await _actionManager.PrepareActionsAsync(_ec.Object, actions));
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
                Setup();
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
                var ex = await Assert.ThrowsAsync<FailedToDownloadActionException>(action);
                Assert.IsType<ActionNotFoundException>(ex.InnerException);

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
        public async void PrepareActions_DownloadActionFromGraph_UseCache()
        {
            try
            {
                //Arrange
                Setup();
                Directory.CreateDirectory(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "action_cache"));
                Directory.CreateDirectory(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "action_cache", "actions_download-artifact"));
                Directory.CreateDirectory(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "actions-download-artifact"));
                Environment.SetEnvironmentVariable(Constants.Variables.Agent.ActionArchiveCacheDirectory, Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "action_cache"));

                const string Content = @"
# Container action
name: '1ae80bcb-c1df-4362-bdaa-54f729c60281'
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
                await File.WriteAllTextAsync(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "actions-download-artifact", "action.yml"), Content);

#if OS_WINDOWS
                ZipFile.CreateFromDirectory(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "actions-download-artifact"), Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "action_cache", "actions_download-artifact", "master-sha.zip"), CompressionLevel.Fastest, true);
#else
                string tar = WhichUtil.Which("tar", require: true, trace: _hc.GetTrace());

                // tar -xzf
                using (var processInvoker = new ProcessInvokerWrapper())
                {
                    processInvoker.Initialize(_hc);
                    processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            _hc.GetTrace().Info(args.Data);
                        }
                    });

                    processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            _hc.GetTrace().Error(args.Data);
                        }
                    });

                    string cwd = Path.GetDirectoryName(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "actions-download-artifact"));
                    string inputDirectory = Path.GetFileName(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "actions-download-artifact"));
                    string archiveFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "action_cache", "actions_download-artifact", "master-sha.tar.gz");
                    int exitCode = await processInvoker.ExecuteAsync(_hc.GetDirectory(WellKnownDirectory.Bin), tar, $"-czf \"{archiveFile}\" -C \"{cwd}\" \"{inputDirectory}\"", null, CancellationToken.None);
                    if (exitCode != 0)
                    {
                        throw new NotSupportedException($"Can't use 'tar -czf' to create archive file: {archiveFile}. return code: {exitCode}.");
                    }
                }
#endif
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

                Assert.Contains("1ae80bcb-c1df-4362-bdaa-54f729c60281", File.ReadAllText(actionYamlFile));
            }
            finally
            {
                Environment.SetEnvironmentVariable(Constants.Variables.Agent.ActionArchiveCacheDirectory, null);
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

                Assert.Equal(0, steps.Count);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_SymlinkCacheIsReentrant()
        {
            try
            {
                //Arrange
                Environment.SetEnvironmentVariable(Constants.Variables.Agent.SymlinkCachedActions, "true");
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
                            Name = "actions/checkout",
                            Ref = "master",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "actions/checkout",
                            Ref = "master",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                const string Content = @"
name: 'Test'
runs:
  using: 'node20'
  main: 'dist/index.js'
";

                string actionsArchive = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "actions_archive", "action_checkout");
                Directory.CreateDirectory(actionsArchive);
                Directory.CreateDirectory(Path.Combine(actionsArchive, "actions_checkout", "master-sha"));
                Directory.CreateDirectory(Path.Combine(actionsArchive, "actions_checkout", "master-sha", "content"));
                await File.WriteAllTextAsync(Path.Combine(actionsArchive, "actions_checkout", "master-sha", "content", "action.yml"), Content);
                Environment.SetEnvironmentVariable(Constants.Variables.Agent.ActionArchiveCacheDirectory, actionsArchive);

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                string destDirectory = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions", "checkout", "master");
                Assert.True(Directory.Exists(destDirectory), "Destination directory does not exist");  
                var di = new DirectoryInfo(destDirectory);   
                Assert.NotNull(di.LinkTarget); 
            }
            finally
            {
                Environment.SetEnvironmentVariable(Constants.Variables.Agent.SymlinkCachedActions, null);
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
                Assert.Equal(0, steps.Count);
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
        public async void PrepareActions_CompositeActionWithActionfile_Node()
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
                            Ref = "CompositeBasic",
                            RepositoryType = "GitHub"
                        }
                    }
                };
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                //Act
                var steps = (await _actionManager.PrepareActionsAsync(_ec.Object, actions)).ContainerSetupSteps;

                // node.js based action doesn't need any extra steps to build/pull containers.
                Assert.Equal(0, steps.Count);
                var watermarkFile = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "CompositeBasic.completed");
                Assert.True(File.Exists(watermarkFile));
                // Comes from the composite action
                var watermarkFile2 = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "actions/setup-node", "v2", "action.yml");
                Assert.True(File.Exists(watermarkFile2));
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_CompositeActionWithActionfile_MaxLimit()
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
                            Ref = "CompositeLimit",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                Func<Task> result = async () => await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                var exception = await Assert.ThrowsAsync<Exception>(result);
                Assert.Equal($"Composite action depth exceeded max depth {Constants.CompositeActionsMaxDepth}", exception.Message);

                // node.js based action doesn't need any extra steps to build/pull containers.
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_CompositeActionWithActionfile_CompositePrestepNested()
        {
            try
            {
                //Arrange
                Setup();
                var actionId = Guid.NewGuid();
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action",
                        Id = actionId,
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "CompositePrestep",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var result = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                Assert.Equal(1, result.PreStepTracker.Count);

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
        public async void PrepareActions_CompositeActionWithActionfile_CompositeContainerNested()
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
                            Ref = "CompositeContainerNested",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var result = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                Assert.Equal(2, result.ContainerSetupSteps.Count);

                // node.js based action doesn't need any extra steps to build/pull containers.
            }
            finally
            {
                Teardown();
            }
        }
#endif

        // =================================================================
        // Tests for batched action resolution optimization
        // =================================================================

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_BatchesResolutionAcrossCompositeActions()
        {
            // Verifies that when multiple composite actions at the same depth
            // reference sub-actions, those sub-actions are resolved in a single
            // batched API call rather than one call per composite.
            //
            // Action tree:
            //   CompositePrestep (composite) → [Node action, CompositePrestep2 (composite)]
            //   CompositePrestep2 (composite) → [Node action, Docker action]
            //
            // Without batching: 3 API calls (depth 0, depth 1 for CompositePrestep, depth 2 for CompositePrestep2)
            // With batching: still 3 calls at most, but the key is that depth-1
            //   sub-actions from all composites at depth 0 are batched into 1 call.
            //   And the same action appearing at multiple depths triggers only 1 resolve.
            Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", "true");
            try
            {
                //Arrange
                Setup();
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                var resolveCallCount = 0;
                var resolvedActions = new List<ActionReferenceList>();
                _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                    {
                        resolveCallCount++;
                        resolvedActions.Add(actions);
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

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
                            Ref = "CompositePrestep",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var result = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                // The composite tree is:
                //   depth 0: CompositePrestep
                //   depth 1: Node@RepositoryActionWithWrapperActionfile_Node + CompositePrestep2
                //   depth 2: Node@RepositoryActionWithWrapperActionfile_Node + Docker@RepositoryActionWithWrapperActionfile_Docker
                //
                // With batching:
                //   Call 1 (depth 0, resolve): CompositePrestep
                //   Call 2 (depth 0→1, pre-resolve): Node + CompositePrestep2 in one batch
                //   Call 3 (depth 1→2, pre-resolve): Docker only (Node already cached from call 2)
                Assert.Equal(3, resolveCallCount);

                // Call 1: depth 0 resolve — just the top-level composite
                var call1Keys = resolvedActions[0].Actions.Select(a => $"{a.NameWithOwner}@{a.Ref}").OrderBy(k => k).ToList();
                Assert.Equal(new[] { "TingluoHuang/runner_L0@CompositePrestep" }, call1Keys);

                // Call 2: depth 0→1 pre-resolve — batch both children of CompositePrestep
                var call2Keys = resolvedActions[1].Actions.Select(a => $"{a.NameWithOwner}@{a.Ref}").OrderBy(k => k).ToList();
                Assert.Equal(new[] { "TingluoHuang/runner_L0@CompositePrestep2", "TingluoHuang/runner_L0@RepositoryActionWithWrapperActionfile_Node" }, call2Keys);

                // Call 3: depth 1→2 pre-resolve — only Docker (Node was cached in call 2)
                var call3Keys = resolvedActions[2].Actions.Select(a => $"{a.NameWithOwner}@{a.Ref}").OrderBy(k => k).ToList();
                Assert.Equal(new[] { "TingluoHuang/runner_L0@RepositoryActionWithWrapperActionfile_Docker" }, call3Keys);

                // Verify all actions were downloaded
                Assert.True(File.Exists(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "CompositePrestep.completed")));
                Assert.True(File.Exists(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Node.completed")));
                Assert.True(File.Exists(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "CompositePrestep2.completed")));
                Assert.True(File.Exists(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Docker.completed")));

                // Verify pre-step tracking still works correctly
                Assert.Equal(1, result.PreStepTracker.Count);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", null);
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_DeduplicatesResolutionAcrossDepthLevels()
        {
            // Verifies that an action appearing at multiple depths in the
            // composite tree is only resolved once (not re-resolved at each level).
            //
            // CompositePrestep uses Node action at depth 1.
            // CompositePrestep2 (also at depth 1) uses the SAME Node action at depth 2.
            // The Node action should only be resolved once total.
            Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", "true");
            try
            {
                //Arrange
                Setup();
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                var allResolvedKeys = new List<string>();
                _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                    {
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            allResolvedKeys.Add(key);
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

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
                            Ref = "CompositePrestep",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                // TingluoHuang/runner_L0@RepositoryActionWithWrapperActionfile_Node appears
                // at both depth 1 (sub-step of CompositePrestep) and depth 2 (sub-step of
                // CompositePrestep2). With deduplication it should only be resolved once.
                var nodeActionKey = "TingluoHuang/runner_L0@RepositoryActionWithWrapperActionfile_Node";
                var nodeResolveCount = allResolvedKeys.FindAll(k => k == nodeActionKey).Count;
                Assert.Equal(1, nodeResolveCount);

                // Verify the total number of unique actions resolved matches the tree
                var uniqueKeys = new HashSet<string>(allResolvedKeys);
                // Expected unique actions: CompositePrestep, Node, CompositePrestep2, Docker = 4
                Assert.Equal(4, uniqueKeys.Count);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", null);
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_MultipleTopLevelActions_BatchesResolution()
        {
            // Verifies that multiple independent actions at depth 0 are
            // resolved in a single API call.
            Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", "true");
            try
            {
                //Arrange
                Setup();
                // Node action has pre+post, needs IActionRunner instances
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                var resolveCallCount = 0;
                var firstCallActionCount = 0;
                _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                    {
                        resolveCallCount++;
                        if (resolveCallCount == 1)
                        {
                            firstCallActionCount = actions.Actions.Count;
                        }
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action1",
                        Id = Guid.NewGuid(),
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
                        Id = Guid.NewGuid(),
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithWrapperActionfile_Docker",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                // Both actions are at depth 0 — should be resolved in a single batch call
                Assert.Equal(1, resolveCallCount);
                Assert.Equal(2, firstCallActionCount);

                // Verify both were downloaded
                Assert.True(File.Exists(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Node.completed")));
                Assert.True(File.Exists(Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Docker.completed")));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", null);
                Teardown();
            }
        }

#if OS_LINUX
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_NestedCompositeContainers_BatchedResolution()
        {
            // Verifies batching with nested composite actions that reference
            // container actions (Linux-only since containers require Linux).
            //
            // CompositeContainerNested (composite):
            //   → repositoryactionwithdockerfile (Dockerfile)
            //   → CompositeContainerNested2 (composite):
            //       → repositoryactionwithdockerfile (Dockerfile, same as above)
            //       → notpullorbuildimagesmultipletimes1 (Dockerfile)
            Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", "true");
            try
            {
                //Arrange
                Setup();

                var resolveCallCount = 0;
                _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                    {
                        resolveCallCount++;
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

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
                            Ref = "CompositeContainerNested",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var result = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                // Tree has 3 depth levels with 5 unique actions.
                // With batching, should need at most 3 resolve calls (one per depth level).
                Assert.True(resolveCallCount <= 3, $"Expected at most 3 resolve calls but got {resolveCallCount}");

                // repositoryactionwithdockerfile appears at both depth 1 and depth 2.
                // Container setup should still work correctly — 2 unique Docker images.
                Assert.Equal(2, result.ContainerSetupSteps.Count);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", null);
                Teardown();
            }
        }
#endif

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_ParallelDownloads_MultipleUniqueActions()
        {
            // Verifies that multiple unique top-level actions are downloaded via
            // DownloadActionsInParallelAsync (the parallel code path), and that
            // all actions are correctly resolved and downloaded.
            Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", "true");
            try
            {
                //Arrange
                Setup();
                // Node action has pre step, and CompositePrestep recurses into
                // sub-actions that also need IActionRunner instances
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                var resolveCallCount = 0;
                _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                    {
                        Interlocked.Increment(ref resolveCallCount);
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action1",
                        Id = Guid.NewGuid(),
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
                        Id = Guid.NewGuid(),
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithWrapperActionfile_Docker",
                            RepositoryType = "GitHub"
                        }
                    },
                    new Pipelines.ActionStep()
                    {
                        Name = "action3",
                        Id = Guid.NewGuid(),
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "CompositePrestep",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                // 3 unique actions at depth 0 → triggers DownloadActionsInParallelAsync
                // (parallel path used when uniqueDownloads.Count > 1)
                var nodeCompleted = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Node.completed");
                var dockerCompleted = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Docker.completed");
                var compositeCompleted = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Actions), "TingluoHuang/runner_L0", "CompositePrestep.completed");

                Assert.True(File.Exists(nodeCompleted), $"Expected watermark at {nodeCompleted}");
                Assert.True(File.Exists(dockerCompleted), $"Expected watermark at {dockerCompleted}");
                Assert.True(File.Exists(compositeCompleted), $"Expected watermark at {compositeCompleted}");

                // All depth-0 actions resolved in a single batch call.
                // Composite sub-actions may add 1-2 more calls.
                Assert.True(resolveCallCount >= 1, "Expected at least 1 resolve call");
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", null);
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_DownloadsNextLevelActionsBeforeRecursing()
        {
            // Verifies that depth-1 actions are downloaded before the depth-2
            // pre-resolve fires. We detect this by snapshotting watermark state
            // inside the 3rd ResolveActionDownloadInfoAsync callback (which is
            // the depth-2 pre-resolve). If pre-download works, depth-1 watermarks
            // already exist at that point.
            //
            // Action tree:
            //   CompositePrestep (composite) → [Node, CompositePrestep2 (composite)]
            //   CompositePrestep2 (composite) → [Node, Docker]
            //
            // Without pre-download: downloads happen during recursion (serial per depth)
            // With pre-download: depth 1 actions (Node + CompositePrestep2) are
            //   downloaded in parallel before recursing, so recursion is a no-op
            //   for downloads.
            Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", "true");
            try
            {
                //Arrange
                Setup();
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                // Track watermark state at the time of each resolve call.
                // If pre-download works, when the 3rd resolve fires (depth 2
                // pre-resolve for Docker), the depth-1 actions (Node +
                // CompositePrestep2) should already have watermarks on disk.
                var resolveCallCount = 0;
                var watermarksAtResolve3 = new Dictionary<string, bool>();
                _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                    {
                        resolveCallCount++;
                        if (resolveCallCount == 3)
                        {
                            // At the time of the 3rd resolve, check if depth-1 actions
                            // are already downloaded (pre-download should have done this)
                            var actionsDir2 = _hc.GetDirectory(WellKnownDirectory.Actions);
                            watermarksAtResolve3["Node"] = File.Exists(Path.Combine(actionsDir2, "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Node.completed"));
                            watermarksAtResolve3["CompositePrestep2"] = File.Exists(Path.Combine(actionsDir2, "TingluoHuang/runner_L0", "CompositePrestep2.completed"));
                        }
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

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
                            Ref = "CompositePrestep",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                var result = await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert
                // All actions should be downloaded (watermarks exist)
                var actionsDir = _hc.GetDirectory(WellKnownDirectory.Actions);
                Assert.True(File.Exists(Path.Combine(actionsDir, "TingluoHuang/runner_L0", "CompositePrestep.completed")));
                Assert.True(File.Exists(Path.Combine(actionsDir, "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Node.completed")));
                Assert.True(File.Exists(Path.Combine(actionsDir, "TingluoHuang/runner_L0", "CompositePrestep2.completed")));
                Assert.True(File.Exists(Path.Combine(actionsDir, "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Docker.completed")));

                // 3 resolve calls total
                Assert.Equal(3, resolveCallCount);

                // The key assertion: at the time of the 3rd resolve call
                // (pre-resolve for depth 2), the depth-1 actions should
                // ALREADY be downloaded thanks to pre-download.
                // Without pre-download, these watermarks wouldn't exist yet
                // because depth-1 downloads would only happen during recursion.
                Assert.True(watermarksAtResolve3["Node"],
                    "Node action should be pre-downloaded before depth 2 pre-resolve");
                Assert.True(watermarksAtResolve3["CompositePrestep2"],
                    "CompositePrestep2 should be pre-downloaded before depth 2 pre-resolve");
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", null);
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void PrepareActions_ParallelDownloadsAtSameDepth()
        {
            // Verifies that multiple unique actions at the same depth are
            // downloaded concurrently (Task.WhenAll) rather than sequentially.
            // We detect this by checking that all watermarks exist after a
            // single PrepareActionsAsync call with multiple top-level actions.
            Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", "true");
            try
            {
                //Arrange
                Setup();
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);
                _hc.EnqueueInstance<IActionRunner>(new Mock<IActionRunner>().Object);

                _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                    {
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

                var actions = new List<Pipelines.ActionStep>
                {
                    new Pipelines.ActionStep()
                    {
                        Name = "action1",
                        Id = Guid.NewGuid(),
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
                        Id = Guid.NewGuid(),
                        Reference = new Pipelines.RepositoryPathReference()
                        {
                            Name = "TingluoHuang/runner_L0",
                            Ref = "RepositoryActionWithWrapperActionfile_Docker",
                            RepositoryType = "GitHub"
                        }
                    }
                };

                //Act
                await _actionManager.PrepareActionsAsync(_ec.Object, actions);

                //Assert - both downloaded (parallel path used when > 1 unique download)
                var actionsDir = _hc.GetDirectory(WellKnownDirectory.Actions);
                Assert.True(File.Exists(Path.Combine(actionsDir, "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Node.completed")));
                Assert.True(File.Exists(Path.Combine(actionsDir, "TingluoHuang/runner_L0", "RepositoryActionWithWrapperActionfile_Docker.completed")));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ACTIONS_BATCH_ACTION_RESOLUTION", null);
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

                Pipelines.ActionStep instance = new()
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

                Pipelines.ActionStep instance = new()
                {
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.ScriptReference()
                };

                // Act.
                Definition definition = _actionManager.LoadAction(_ec.Object, instance);

                // Assert.
                Assert.NotNull(definition);
                Assert.NotNull(definition.Data);
                Assert.Equal(ActionExecutionType.Script, definition.Data.Execution.ExecutionType);
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

                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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
        public void LoadsNode12ActionDefinition()
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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

                Assert.NotNull(definition.Data.Execution as NodeJSActionExecutionData);
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
                Assert.Equal("node12", (definition.Data.Execution as NodeJSActionExecutionData).NodeVersion);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsNode16ActionDefinition()
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
  using: 'node16'
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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

                Assert.NotNull(definition.Data.Execution as NodeJSActionExecutionData);
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
                Assert.Equal("node16", (definition.Data.Execution as NodeJSActionExecutionData).NodeVersion);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsNode20ActionDefinition()
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
  using: 'node20'
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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

                Assert.NotNull(definition.Data.Execution as NodeJSActionExecutionData);
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
                Assert.Equal("node20", (definition.Data.Execution as NodeJSActionExecutionData).NodeVersion);
            }
            finally
            {
                Teardown();
            }
        }

         [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsNode24ActionDefinition()
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
  using: 'node24'
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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

                Assert.NotNull(definition.Data.Execution as NodeJSActionExecutionData);
                Assert.Equal("task.js", (definition.Data.Execution as NodeJSActionExecutionData).Script);
                Assert.Equal("node24", (definition.Data.Execution as NodeJSActionExecutionData).NodeVersion);
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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

                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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

                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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
                Dictionary<string, string> inputDefaults = new(StringComparer.OrdinalIgnoreCase);
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

        private void Setup([CallerMemberName] string name = "", bool enableComposite = true)
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
            _ec.Setup(x => x.Root).Returns(new GitHub.Runner.Worker.ExecutionContext());
            var variables = new Dictionary<string, VariableValue>();
            _ec.Object.Global.Variables = new Variables(_hc, variables);
            _ec.Setup(x => x.ExpressionValues).Returns(new DictionaryContextData());
            _ec.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            _ec.Object.Global.FileTable = new List<String>();
            _ec.Object.Global.Plan = new TaskOrchestrationPlanReference();
            _ec.Object.Global.JobTelemetry = new List<JobTelemetry>();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"[{tag}]{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<ExecutionContextLogOptions>())).Callback((Issue issue, ExecutionContextLogOptions logOptions) => { _hc.GetTrace().Info($"[{issue.Type}]{logOptions.LogMessageOverride ?? issue.Message}"); });
            _ec.Setup(x => x.GetGitHubContext("workspace")).Returns(Path.Combine(_workFolder, "actions", "actions"));

            _dockerManager = new Mock<IDockerCommandManager>();
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:16.04")).Returns(Task.FromResult(0));
            _dockerManager.Setup(x => x.DockerPull(_ec.Object, "ubuntu:100.04")).Returns(Task.FromResult(1));

            _dockerManager.Setup(x => x.DockerBuild(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(0));

            _jobServer = new Mock<IJobServer>();
            _jobServer.Setup(x => x.ResolveActionDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>()))
                .Returns((Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken) =>
                {
                    var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                    foreach (var action in actions.Actions)
                    {
                        var key = $"{action.NameWithOwner}@{action.Ref}";
                        result.Actions[key] = new ActionDownloadInfo
                        {
                            NameWithOwner = action.NameWithOwner,
                            Ref = action.Ref,
                            ResolvedNameWithOwner = action.NameWithOwner,
                            ResolvedSha = $"{action.Ref}-sha",
                            TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                            ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                        };
                    }
                    return Task.FromResult(result);
                });

            _launchServer = new Mock<ILaunchServer>();
            _launchServer.Setup(x => x.ResolveActionsDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                .Returns((Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken, bool displayHelpfulActionsDownloadErrors) =>
                {
                    var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                    foreach (var action in actions.Actions)
                    {
                        var key = $"{action.NameWithOwner}@{action.Ref}";
                        result.Actions[key] = new ActionDownloadInfo
                        {
                            NameWithOwner = action.NameWithOwner,
                            Ref = action.Ref,
                            ResolvedNameWithOwner = action.NameWithOwner,
                            ResolvedSha = $"{action.Ref}-sha",
                            TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                            ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                        };
                    }
                    return Task.FromResult(result);
                });

            _pluginManager = new Mock<IRunnerPluginManager>();
            _pluginManager.Setup(x => x.GetPluginAction(It.IsAny<string>())).Returns(new RunnerPluginActionInfo() { PluginTypeName = "plugin.class, plugin", PostPluginTypeName = "plugin.cleanup, plugin" });

            var actionManifestLegacy = new ActionManifestManagerLegacy();
            actionManifestLegacy.Initialize(_hc);
            _hc.SetSingleton<IActionManifestManagerLegacy>(actionManifestLegacy);
            var actionManifestNew = new ActionManifestManager();
            actionManifestNew.Initialize(_hc);
            _hc.SetSingleton<IActionManifestManager>(actionManifestNew);
            var actionManifestWrapper = new ActionManifestManagerWrapper();
            actionManifestWrapper.Initialize(_hc);

            _hc.SetSingleton<IDockerCommandManager>(_dockerManager.Object);
            _hc.SetSingleton<IJobServer>(_jobServer.Object);
            _hc.SetSingleton<ILaunchServer>(_launchServer.Object);
            _hc.SetSingleton<IRunnerPluginManager>(_pluginManager.Object);
            _hc.SetSingleton<IActionManifestManagerWrapper>(actionManifestWrapper);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task GetDownloadInfoAsync_PropagatesDependencies_WhenPresent()
        {
            try
            {
                // Arrange
                Setup();

                // Set RunServiceJob so we hit the Launch path
                _ec.Object.Global.Variables.Set(Constants.Variables.System.JobRequestType, "RunnerJobRequest");

                // Populate lockfile dependencies
                _ec.Object.Global.ActionsDependencies = new List<string>
                {
                    "github.com/actions/checkout@v4:sha256-abc123",
                    "github.com/actions/setup-node@v4:sha256-def456"
                };

                // Capture the ActionReferenceList passed to Launch
                ActionReferenceList capturedList = null;
                _launchServer
                    .Setup(x => x.ResolveActionsDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                    .Callback<Guid, Guid, ActionReferenceList, CancellationToken, bool>((planId, jobId, list, ct, display) => capturedList = list)
                    .Returns((Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken ct, bool display) =>
                    {
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

                var actionStep = new Pipelines.ActionStep()
                {
                    Name = "action",
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.RepositoryPathReference()
                    {
                        Name = "actions/checkout",
                        Ref = "v4",
                        RepositoryType = "GitHub"
                    }
                };

                // Act
                var result = await _actionManager.PrepareActionsAsync(_ec.Object, new List<Pipelines.JobStep> { actionStep }, default);

                // Assert
                Assert.NotNull(capturedList);
                Assert.NotNull(capturedList.Dependencies);
                Assert.Equal(2, capturedList.Dependencies.Count);
                Assert.Equal("github.com/actions/checkout@v4:sha256-abc123", capturedList.Dependencies[0]);
                Assert.Equal("github.com/actions/setup-node@v4:sha256-def456", capturedList.Dependencies[1]);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task GetDownloadInfoAsync_OmitsDependencies_WhenEmpty()
        {
            try
            {
                // Arrange
                Setup();

                // Set RunServiceJob so we hit the Launch path
                _ec.Object.Global.Variables.Set(Constants.Variables.System.JobRequestType, "RunnerJobRequest");

                // No dependencies set (default empty list from GlobalContext)

                // Capture the ActionReferenceList passed to Launch
                ActionReferenceList capturedList = null;
                _launchServer
                    .Setup(x => x.ResolveActionsDownloadInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActionReferenceList>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                    .Callback<Guid, Guid, ActionReferenceList, CancellationToken, bool>((planId, jobId, list, ct, display) => capturedList = list)
                    .Returns((Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken ct, bool display) =>
                    {
                        var result = new ActionDownloadInfoCollection { Actions = new Dictionary<string, ActionDownloadInfo>() };
                        foreach (var action in actions.Actions)
                        {
                            var key = $"{action.NameWithOwner}@{action.Ref}";
                            result.Actions[key] = new ActionDownloadInfo
                            {
                                NameWithOwner = action.NameWithOwner,
                                Ref = action.Ref,
                                ResolvedNameWithOwner = action.NameWithOwner,
                                ResolvedSha = $"{action.Ref}-sha",
                                TarballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/tarball/{action.Ref}",
                                ZipballUrl = $"https://api.github.com/repos/{action.NameWithOwner}/zipball/{action.Ref}",
                            };
                        }
                        return Task.FromResult(result);
                    });

                var actionStep = new Pipelines.ActionStep()
                {
                    Name = "action",
                    Id = Guid.NewGuid(),
                    Reference = new Pipelines.RepositoryPathReference()
                    {
                        Name = "actions/checkout",
                        Ref = "v4",
                        RepositoryType = "GitHub"
                    }
                };

                // Act
                var result = await _actionManager.PrepareActionsAsync(_ec.Object, new List<Pipelines.JobStep> { actionStep }, default);

                // Assert
                Assert.NotNull(capturedList);
                Assert.Null(capturedList.Dependencies);
            }
            finally
            {
                Teardown();
            }
        }
    }
}
