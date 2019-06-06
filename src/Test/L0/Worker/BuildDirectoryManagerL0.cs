// using GitHub.DistributedTask.WebApi;
// using Pipelines = GitHub.DistributedTask.Pipelines;
// using GitHub.Runner.Worker;
// using Moq;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Reflection;
// using System.Runtime.CompilerServices;
// using Xunit;

// namespace GitHub.Runner.Common.Tests.Worker
// {
//     public sealed class BuildDirectoryManagerL0
//     {
//         private const string HashKey = "5a4b79d27e8ec0ab9d94ca606ce3adbeb750aa73";
//         private const string NonmatchingHashKey = "0987654321098765432109876543210987654321";
//         private const string CollectionId = "31ffacb8-b468-4e60-b2f9-c50ce437da92";
//         private const string DefinitionId = "1234";
//         private PipelineDirectoryManager _buildDirectoryManager;
//         private Mock<IExecutionContext> _ec;
//         private Pipelines.RepositoryResource _repository;
//         private Pipelines.WorkspaceOptions _workspaceOptions;
//         private TrackingConfig _existingConfig;
//         private TrackingConfig _newConfig;
//         private string _trackingFile;
//         private Mock<ITrackingManager> _trackingManager;
//         private Variables _variables;
//         private string _workFolder;

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void CreatesBuildDirectories()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup())
//             {
//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 Assert.True(Directory.Exists(Path.Combine(_workFolder, _newConfig.PipelineDirectory, _newConfig.ArtifactsDirectory)));
//                 Assert.True(Directory.Exists(Path.Combine(_workFolder, _newConfig.PipelineDirectory, _newConfig.SourcesDirectory)));
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void CreatesNewConfig()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup())
//             {
//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 _trackingManager.Verify(x => x.Create(_ec.Object, _repository, HashKey, _trackingFile));
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void CreatesNewConfigWhenHashKeyIsDifferent()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup(existingConfigKind: ExistingConfigKind.Nonmatching))
//             {
//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 _trackingManager.Verify(x => x.LoadIfExists(_ec.Object, _trackingFile));
//                 _trackingManager.Verify(x => x.Create(_ec.Object, _repository, HashKey, _trackingFile));
//                 _trackingManager.Verify(x => x.MarkForGarbageCollection(_ec.Object, _existingConfig));
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void DeletesSourcesDirectoryWhenCleanIsSources()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup(cleanOption: BuildCleanOption.Source))
//             {
//                 string sourcesDirectory = Path.Combine(_workFolder, _newConfig.PipelineDirectory, _newConfig.SourcesDirectory);
//                 string sourceFile = Path.Combine(sourcesDirectory, "some subdirectory", "some source file");
//                 Directory.CreateDirectory(Path.GetDirectoryName(sourceFile));
//                 File.WriteAllText(path: sourceFile, contents: "some source contents");

//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 Assert.True(Directory.Exists(sourcesDirectory));
//                 Assert.Equal(0, Directory.GetFileSystemEntries(sourcesDirectory, "*", SearchOption.AllDirectories).Length);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void RecreatesArtifactsAndTestResultsDirectory()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup())
//             {
//                 string artifactsDirectory = Path.Combine(_workFolder, _newConfig.PipelineDirectory, _newConfig.ArtifactsDirectory);
//                 string artifactFile = Path.Combine(artifactsDirectory, "some subdirectory", "some artifact file");
//                 Directory.CreateDirectory(Path.GetDirectoryName(artifactFile));
//                 File.WriteAllText(path: artifactFile, contents: "some artifact contents");

//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 Assert.True(Directory.Exists(artifactsDirectory));
//                 Assert.Equal(0, Directory.GetFileSystemEntries(artifactsDirectory).Length);
//             }
//         }

//         // Recreates build directory when clean is all.
//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void RecreatesBuildDirectoryWhenCleanIsAll()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup(cleanOption: BuildCleanOption.All))
//             {
//                 string buildDirectory = Path.Combine(_workFolder, _newConfig.PipelineDirectory);
//                 string looseFile = Path.Combine(buildDirectory, "some loose directory", "some loose file");
//                 Directory.CreateDirectory(Path.GetDirectoryName(looseFile));
//                 File.WriteAllText(path: looseFile, contents: "some loose file contents");

//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 Assert.Equal(3, Directory.GetFileSystemEntries(buildDirectory, "*", SearchOption.AllDirectories).Length);
//                 Assert.True(Directory.Exists(Path.Combine(_workFolder, _newConfig.PipelineDirectory, _newConfig.ArtifactsDirectory)));
//                 Assert.True(Directory.Exists(Path.Combine(_workFolder, _newConfig.PipelineDirectory, Constants.Build.Path.BinariesDirectory)));
//                 Assert.True(Directory.Exists(Path.Combine(_workFolder, _newConfig.PipelineDirectory, _newConfig.SourcesDirectory)));
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void RecreatesBinariesDirectoryWhenCleanIsBinary()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup(cleanOption: BuildCleanOption.Binary))
//             {
//                 string binariesDirectory = Path.Combine(_workFolder, _newConfig.PipelineDirectory, Constants.Build.Path.BinariesDirectory);
//                 string binaryFile = Path.Combine(binariesDirectory, "some subdirectory", "some binary file");
//                 Directory.CreateDirectory(Path.GetDirectoryName(binaryFile));
//                 File.WriteAllText(path: binaryFile, contents: "some binary contents");

//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 Assert.True(Directory.Exists(binariesDirectory));
//                 Assert.Equal(0, Directory.GetFileSystemEntries(binariesDirectory).Length);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void PrepareDirectoryUpdateRepositoryPath()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup())
//             {
//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 Assert.Equal(Path.Combine(_workFolder, _newConfig.SourcesDirectory), _repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path));
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void UpdatesExistingConfig()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup(existingConfigKind: ExistingConfigKind.Matching))
//             {
//                 // Act.
//                 _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 // Assert.
//                 _trackingManager.Verify(x => x.LoadIfExists(_ec.Object, _trackingFile));
//                 _trackingManager.Verify(x => x.UpdateJobRunProperties(_ec.Object, _existingConfig, _trackingFile));
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void UpdateDirectory()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup(existingConfigKind: ExistingConfigKind.Matching))
//             {
//                 // Act.
//                 var tracking = _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 _repository.Properties.Set<string>(Pipelines.RepositoryPropertyNames.Path, Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), tracking.PipelineDirectory, $"test{Path.DirectorySeparatorChar}foo"));

//                 var newTracking = _buildDirectoryManager.UpdateDirectory(_ec.Object, _repository);

//                 // Assert.
//                 Assert.Equal(newTracking.SourcesDirectory, $"1{Path.DirectorySeparatorChar}test{Path.DirectorySeparatorChar}foo");
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void UpdateDirectoryFailOnInvalidPath()
//         {
//             // Arrange.
//             using (TestHostContext hc = Setup(existingConfigKind: ExistingConfigKind.Matching))
//             {
//                 // Act.
//                 var tracking = _buildDirectoryManager.PrepareDirectory(_ec.Object, _repository, _workspaceOptions);

//                 _repository.Properties.Set<string>(Pipelines.RepositoryPropertyNames.Path, Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), "test\\foo"));

//                 var exception = Assert.Throws<ArgumentException>(() => _buildDirectoryManager.UpdateDirectory(_ec.Object, _repository));

//                 // Assert.
//                 Assert.True(exception.Message.Contains("should be located under agent's work directory"));
//             }
//         }
//         // TODO: Updates legacy config.

//         private TestHostContext Setup(
//             [CallerMemberName] string name = "",
//             BuildCleanOption? cleanOption = null,
//             ExistingConfigKind existingConfigKind = ExistingConfigKind.None)
//         {
//             // Setup the host context.
//             TestHostContext hc = new TestHostContext(this, name);

//             // Create a random work path.
//             var configStore = new Mock<IConfigurationStore>();
//             _workFolder = hc.GetDirectory(WellKnownDirectory.Work);
//             var settings = new RunnerSettings() { WorkFolder = _workFolder };
//             configStore.Setup(x => x.GetSettings()).Returns(settings);
//             hc.SetSingleton<IConfigurationStore>(configStore.Object);

//             // Setup the execution context.
//             _ec = new Mock<IExecutionContext>();
//             var variables = new Dictionary<string, VariableValue>();
//             variables[Constants.Variables.System.CollectionId] = CollectionId;
//             variables[Constants.Variables.System.DefinitionId] = DefinitionId;
//             _variables = new Variables(hc, variables);

//             _ec.Setup(x => x.Variables).Returns(_variables);

//             // Store the expected tracking file path.
//             _trackingFile = Path.Combine(
//                 _workFolder,
//                 Constants.Build.Path.SourceRootMappingDirectory,
//                 _ec.Object.Variables.System_CollectionId,
//                 _ec.Object.Variables.System_DefinitionId,
//                 Constants.Build.Path.TrackingConfigFile);

//             // Setup the endpoint.
//             _repository = new Pipelines.RepositoryResource()
//             {
//                 Alias = "test",
//                 Type = Pipelines.RepositoryTypes.Git,
//                 Url = new Uri("http://contoso.visualstudio.com"),
//             };
//             _repository.Properties.Set<String>(Pipelines.RepositoryPropertyNames.Name, "actions/runner");

//             _workspaceOptions = new Pipelines.WorkspaceOptions();
//             if (cleanOption != null)
//             {
//                 if (cleanOption == BuildCleanOption.All)
//                 {
//                     _workspaceOptions.Clean = Pipelines.PipelineConstants.WorkspaceCleanOptions.All;
//                 }
//                 else if (cleanOption == BuildCleanOption.Source)
//                 {
//                     _workspaceOptions.Clean = Pipelines.PipelineConstants.WorkspaceCleanOptions.Resources;
//                 }
//                 else if (cleanOption == BuildCleanOption.Binary)
//                 {
//                     _workspaceOptions.Clean = Pipelines.PipelineConstants.WorkspaceCleanOptions.Outputs;
//                 }
//             }
//             // // Setup the source provider.
//             // _sourceProvider = new Mock<ISourceProvider>();
//             // _sourceProvider
//             //     .Setup(x => x.GetBuildDirectoryHashKey(_ec.Object, _repository))
//             //     .Returns(HashKey);
//             // hc.SetSingleton<ISourceProvider>(_sourceProvider.Object);

//             // Store the existing config object.
//             switch (existingConfigKind)
//             {
//                 case ExistingConfigKind.Matching:
//                     _existingConfig = new TrackingConfig(_ec.Object, _repository, 1, HashKey);
//                     Assert.Equal("1", _existingConfig.PipelineDirectory);
//                     break;
//                 case ExistingConfigKind.Nonmatching:
//                     _existingConfig = new TrackingConfig(_ec.Object, _repository, 2, NonmatchingHashKey);
//                     Assert.Equal("2", _existingConfig.PipelineDirectory);
//                     break;
//                 case ExistingConfigKind.None:
//                     break;
//                 default:
//                     throw new NotSupportedException();
//             }

//             // Store the new config object.
//             if (existingConfigKind == ExistingConfigKind.Matching)
//             {
//                 _newConfig = _existingConfig;
//             }
//             else
//             {
//                 _newConfig = new TrackingConfig(_ec.Object, _repository, 3, HashKey);
//                 Assert.Equal("3", _newConfig.PipelineDirectory);
//             }

//             // Setup the tracking manager.
//             _trackingManager = new Mock<ITrackingManager>();
//             _trackingManager
//                 .Setup(x => x.LoadIfExists(_ec.Object, _trackingFile))
//                 .Returns(_existingConfig);
//             if (existingConfigKind == ExistingConfigKind.None || existingConfigKind == ExistingConfigKind.Nonmatching)
//             {
//                 _trackingManager
//                     .Setup(x => x.Create(_ec.Object, _repository, HashKey, _trackingFile))
//                     .Returns(_newConfig);
//                 if (existingConfigKind == ExistingConfigKind.Nonmatching)
//                 {
//                     _trackingManager
//                         .Setup(x => x.MarkForGarbageCollection(_ec.Object, _existingConfig));
//                 }
//             }
//             else if (existingConfigKind == ExistingConfigKind.Matching)
//             {
//                 _trackingManager
//                     .Setup(x => x.UpdateJobRunProperties(_ec.Object, _existingConfig, _trackingFile));
//             }
//             else
//             {
//                 throw new NotSupportedException();
//             }

//             hc.SetSingleton<ITrackingManager>(_trackingManager.Object);

//             // Setup the build directory manager.
//             _buildDirectoryManager = new PipelineDirectoryManager();
//             _buildDirectoryManager.Initialize(hc);
//             return hc;
//         }

//         private enum ExistingConfigKind
//         {
//             None,
//             Matching,
//             Nonmatching,
//         }
//     }
// }
