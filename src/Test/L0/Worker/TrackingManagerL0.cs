// using GitHub.DistributedTask.WebApi;
// using Pipelines = GitHub.DistributedTask.Pipelines;
// using GitHub.Runner.Worker;
// using Moq;
// using Newtonsoft.Json;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using System.Runtime.CompilerServices;
// using System.Text.RegularExpressions;
// using Xunit;
// using GitHub.DistributedTask.Pipelines;

// namespace GitHub.Runner.Common.Tests.Worker
// {
//     public sealed class TrackingManagerL0
//     {
//         private const string CollectionId = "226466ab-342b-4ca4-bbee-0b87154d4936";
//         private const string CollectionName = "Some collection name";
//         // TODO: Add a test for collection in the domain.
//         private const string CollectionUrl = "http://contoso:8080/tfs/DefaultCollection/";
//         private const string DefinitionId = "1234";
//         private const string DefinitionName = "Some definition name";
//         private const string RepositoryUrl = "http://contoso:8080/tfs/DefaultCollection/_git/gitTest";
//         private Mock<IExecutionContext> _ec;
//         private Pipelines.RepositoryResource _repository;
//         private TrackingManager _trackingManager;
//         private Variables _variables;
//         private string _workFolder;

//         public TestHostContext Setup([CallerMemberName] string name = "")
//         {
//             // Setup the host context.
//             TestHostContext hc = new TestHostContext(this, name);

//             // Create a random work path.
//             _workFolder = hc.GetDirectory(WellKnownDirectory.Work);

//             // Setup the execution context.
//             _ec = new Mock<IExecutionContext>();
//             var variables = new Dictionary<string, VariableValue>();
//             variables[Constants.Variables.System.CollectionId] = CollectionId;
//             variables[WellKnownDistributedTaskVariables.TFCollectionUrl] = CollectionUrl;
//             variables[Constants.Variables.System.DefinitionId] = DefinitionId;
//             variables[Constants.Variables.Build.DefinitionName] = DefinitionName;

//             _variables = new Variables(hc, variables);
//             _ec.Setup(x => x.Variables).Returns(_variables);

//             // Setup the endpoint.
//             _repository = new Pipelines.RepositoryResource() { Url = new Uri(RepositoryUrl) };
//             _repository.Properties.Set(RepositoryPropertyNames.Name, "test/gitTest");

//             // Setup the tracking manager.
//             _trackingManager = new TrackingManager();
//             _trackingManager.Initialize(hc);

//             return hc;
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void CreatesTopLevelTrackingConfig()
//         {
//             using (TestHostContext hc = Setup())
//             {
//                 // Arrange.
//                 string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
//                 DateTimeOffset testStartOn = DateTimeOffset.Now;

//                 // Act.
//                 _trackingManager.Create(_ec.Object, _repository, "some hash key", trackingFile);

//                 // Assert.
//                 string topLevelFile = Path.Combine(
//                     _workFolder,
//                     Constants.Build.Path.SourceRootMappingDirectory,
//                     Constants.Build.Path.TopLevelTrackingConfigFile);
//                 var config = JsonConvert.DeserializeObject<TopLevelTrackingConfig>(
//                     value: File.ReadAllText(topLevelFile));
//                 Assert.Equal(1, config.LastPipelineDirectoryNumber);
//                 // Manipulate the expected seconds due to loss of granularity when the
//                 // date-time-offset is serialized in a friendly format.
//                 Assert.True(testStartOn.AddSeconds(-1) <= config.LastPipelineDirectoryCreatedOn);
//                 Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastPipelineDirectoryCreatedOn);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void CreatesTrackingConfig()
//         {
//             using (TestHostContext hc = Setup())
//             {
//                 // Arrange.
//                 const string HashKey = "Some hash key";
//                 string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
//                 DateTimeOffset testStartOn = DateTimeOffset.Now;

//                 // Act.
//                 _trackingManager.Create(_ec.Object, _repository, HashKey, trackingFile);

//                 // Assert.
//                 TrackingConfig config = _trackingManager.LoadIfExists(_ec.Object, trackingFile) as TrackingConfig;
//                 Assert.Equal(
//                     Path.Combine("1", Constants.Build.Path.ArtifactsDirectory),
//                     config.ArtifactsDirectory);
//                 Assert.Equal("1", config.PipelineDirectory);
//                 Assert.Equal(CollectionId, config.CollectionId);
//                 Assert.Equal(CollectionUrl, config.CollectionUrl);
//                 Assert.Equal(DefinitionId, config.DefinitionId);
//                 Assert.Equal(DefinitionName, config.DefinitionName);
//                 Assert.Equal(3, config.FileFormatVersion);
//                 Assert.Equal(HashKey, config.HashKey);
//                 // Manipulate the expected seconds due to loss of granularity when the
//                 // date-time-offset is serialized in a friendly format.
//                 Assert.True(testStartOn.AddSeconds(-1) <= config.LastRunOn);
//                 Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastRunOn);
//                 Assert.Equal(RepositoryUrl, config.RepositoryUrl);
//                 Assert.Equal(
//                     Path.Combine("1", "gitTest"),
//                     config.SourcesDirectory);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void LoadsTrackingConfig_FileFormatVersion3()
//         {
//             using (TestHostContext hc = Setup())
//             {
//                 // Arrange.
//                 // It doesn't matter for this test whether the line endings are CRLF or just LF.
//                 const string Contents = @"{
//   ""runner_artifactdirectory"": ""b00335b6\\a"",
//   ""runner_pipelinedirectory"": ""b00335b6"",
//   ""collectionUrl"": ""http://contoso:8080/tfs/DefaultCollection/"",
//   ""definitionName"": ""M87_PrintEnvVars"",
//   ""fileFormatVersion"": 3,
//   ""lastRunOn"": ""09/16/2015 23:56:46 -04:00"",
//   ""runner_sourcesdirectory"": ""b00335b6\\gitTest"",
//   ""collectionId"": ""7aee6dde-6381-4098-93e7-50a8264cf066"",
//   ""definitionId"": ""7"",
//   ""hashKey"": ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c"",
//   ""repositoryUrl"": ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest""
// }";
//                 Directory.CreateDirectory(_workFolder);
//                 string filePath = Path.Combine(_workFolder, "trackingconfig.json");
//                 File.WriteAllText(filePath, Contents);

//                 // Act.
//                 TrackingConfig baseConfig = _trackingManager.LoadIfExists(_ec.Object, filePath);

//                 // Assert.
//                 Assert.NotNull(baseConfig);
//                 TrackingConfig config = baseConfig as TrackingConfig;
//                 Assert.NotNull(config);
//                 Assert.Equal(@"b00335b6\a", config.ArtifactsDirectory);
//                 Assert.Equal(@"b00335b6", config.PipelineDirectory);
//                 Assert.Equal(@"7aee6dde-6381-4098-93e7-50a8264cf066", config.CollectionId);
//                 Assert.Equal(CollectionUrl, config.CollectionUrl);
//                 Assert.Equal(@"7", config.DefinitionId);
//                 Assert.Equal(@"M87_PrintEnvVars", config.DefinitionName);
//                 Assert.Equal(3, config.FileFormatVersion);
//                 Assert.Equal(@"b00335b6923adfa64f46f3abb7da1cdc0d9bae6c", config.HashKey);
//                 Assert.Equal(new DateTimeOffset(2015, 9, 16, 23, 56, 46, TimeSpan.FromHours(-4)), config.LastRunOn);
//                 Assert.Equal(@"http://contoso:8080/tfs/DefaultCollection/_git/gitTest", config.RepositoryUrl);
//                 Assert.Equal(@"b00335b6\gitTest", config.SourcesDirectory);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void LoadsTrackingConfig_NotExists()
//         {
//             using (TestHostContext hc = Setup())
//             {
//                 // Act.
//                 TrackingConfig config = _trackingManager.LoadIfExists(
//                     _ec.Object,
//                     Path.Combine(_workFolder, "foo.json"));

//                 // Assert.
//                 Assert.Null(config);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void MarksTrackingConfigForGarbageCollection()
//         {
//             using (TestHostContext hc = Setup())
//             {
//                 // Arrange.
//                 // It doesn't matter for this test whether the line endings are CRLF or just LF.
//                 const string TrackingContents = @"{
//   ""runner_artifactdirectory"": ""b00335b6\\a"",
//   ""runner_pipelinedirectory"": ""b00335b6"",
//   ""collectionUrl"": ""http://contoso:8080/tfs/DefaultCollection/"",
//   ""definitionName"": ""M87_PrintEnvVars"",
//   ""fileFormatVersion"": 3,
//   ""lastRunOn"": ""09/16/2015 23:56:46 -04:00"",
//   ""runner_sourcesdirectory"": ""b00335b6\\gitTest"",
//   ""collectionId"": ""7aee6dde-6381-4098-93e7-50a8264cf066"",
//   ""definitionId"": ""7"",
//   ""hashKey"": ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c"",
//   ""repositoryUrl"": ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest""
// }";
//                 Directory.CreateDirectory(_workFolder);
//                 string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
//                 File.WriteAllText(trackingFile, TrackingContents);
//                 TrackingConfig config = _trackingManager.LoadIfExists(_ec.Object, trackingFile) as TrackingConfig;
//                 Assert.NotNull(config);

//                 // Act.
//                 _trackingManager.MarkForGarbageCollection(_ec.Object, config);

//                 // Assert.
//                 string gcDirectory = Path.Combine(
//                     _workFolder,
//                     Constants.Build.Path.SourceRootMappingDirectory,
//                     Constants.Build.Path.GarbageCollectionDirectory);
//                 Assert.True(Directory.Exists(gcDirectory));
//                 string[] gcFiles = Directory.GetFiles(gcDirectory);
//                 Assert.Equal(1, gcFiles.Length);
//                 string gcFile = gcFiles.Single();
//                 string gcContents = File.ReadAllText(gcFile);
//                 Assert.Equal(TrackingContents, gcContents);
//                 // File name should a GUID.
//                 Assert.True(Regex.IsMatch(Path.GetFileNameWithoutExtension(gcFile), "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"));
//                 // File name should not be the default GUID.
//                 Assert.NotEqual("00000000-0000-0000-0000-000000000000", Path.GetFileNameWithoutExtension(gcFile));
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void UpdatesTopLevelTrackingConfig()
//         {
//             using (TestHostContext hc = Setup())
//             {
//                 // Arrange.
//                 string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
//                 _trackingManager.Create(_ec.Object, _repository, "some hash key", trackingFile);
//                 DateTimeOffset testStartOn = DateTimeOffset.Now;

//                 // Act.
//                 _trackingManager.Create(_ec.Object, _repository, "some hash key", trackingFile);

//                 // Assert.
//                 string topLevelFile = Path.Combine(
//                     _workFolder,
//                     Constants.Build.Path.SourceRootMappingDirectory,
//                     Constants.Build.Path.TopLevelTrackingConfigFile);
//                 TopLevelTrackingConfig config = JsonConvert.DeserializeObject<TopLevelTrackingConfig>(
//                     value: File.ReadAllText(topLevelFile));
//                 Assert.Equal(2, config.LastPipelineDirectoryNumber);
//                 // Manipulate the expected seconds due to loss of granularity when the
//                 // date-time-offset is serialized in a friendly format.
//                 Assert.True(testStartOn.AddSeconds(-1) <= config.LastPipelineDirectoryCreatedOn);
//                 Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastPipelineDirectoryCreatedOn);
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void UpdatesTrackingConfigJobRunProperties()
//         {
//             using (TestHostContext hc = Setup())
//             {
//                 // Arrange.
//                 DateTimeOffset testStartOn = DateTimeOffset.Now;
//                 TrackingConfig config = new TrackingConfig();
//                 string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");

//                 // Act.
//                 _trackingManager.UpdateJobRunProperties(_ec.Object, config, trackingFile);

//                 // Assert.
//                 config = _trackingManager.LoadIfExists(_ec.Object, trackingFile) as TrackingConfig;
//                 Assert.NotNull(config);
//                 Assert.Equal(CollectionUrl, config.CollectionUrl);
//                 Assert.Equal(DefinitionName, config.DefinitionName);
//                 // Manipulate the expected seconds due to loss of granularity when the
//                 // date-time-offset is serialized in a friendly format.
//                 Assert.True(testStartOn.AddSeconds(-1) <= config.LastRunOn);
//                 Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastRunOn);
//             }
//         }
//     }
// }
