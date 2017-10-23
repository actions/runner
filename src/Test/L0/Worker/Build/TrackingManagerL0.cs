using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Build
{
    public sealed class TrackingManagerL0
    {
        private const string CollectionId = "226466ab-342b-4ca4-bbee-0b87154d4936";
        private const string CollectionName = "Some collection name";
        // TODO: Add a test for collection in the domain.
        private const string CollectionUrl = "http://contoso:8080/tfs/DefaultCollection/";
        private const string DefinitionId = "1234";
        private const string DefinitionName = "Some definition name";
        private const string EndpointUrl = "http://contoso:8080/tfs/DefaultCollection/_git/gitTest";
        private Mock<IExecutionContext> _ec;
        private ServiceEndpoint _endpoint;
        private TrackingManager _trackingManager;
        private Variables _variables;
        private string _workFolder;

        public TestHostContext Setup([CallerMemberName] string name = "")
        {
            // Setup the host context.
            TestHostContext hc = new TestHostContext(this, name);

            // Create a random work path.
            var configStore = new Mock<IConfigurationStore>();
            _workFolder = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                $"_work_{Path.GetRandomFileName()}");
            var settings = new AgentSettings()
            {
                WorkFolder = _workFolder,
            };
            configStore.Setup(x => x.GetSettings()).Returns(settings);
            hc.SetSingleton<IConfigurationStore>(configStore.Object);

            // Setup the execution context.
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            _variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            _variables.Set(Constants.Variables.System.CollectionId, CollectionId);
            _variables.Set(WellKnownDistributedTaskVariables.TFCollectionUrl, CollectionUrl);
            _variables.Set(Constants.Variables.System.DefinitionId, DefinitionId);
            _variables.Set(Constants.Variables.Build.DefinitionName, DefinitionName);
            _ec.Setup(x => x.Variables).Returns(_variables);

            // Setup the endpoint.
            _endpoint = new ServiceEndpoint() { Url = new Uri(EndpointUrl) };

            // Setup the tracking manager.
            _trackingManager = new TrackingManager();
            _trackingManager.Initialize(hc);

            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreatesTopLevelTrackingConfig()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
                DateTimeOffset testStartOn = DateTimeOffset.Now;

                // Act.
                _trackingManager.Create(_ec.Object, _endpoint, "some hash key", trackingFile, false);

                // Assert.
                string topLevelFile = Path.Combine(
                    _workFolder,
                    Constants.Build.Path.SourceRootMappingDirectory,
                    Constants.Build.Path.TopLevelTrackingConfigFile);
                var config = JsonConvert.DeserializeObject<TopLevelTrackingConfig>(
                    value: File.ReadAllText(topLevelFile));
                Assert.Equal(1, config.LastBuildDirectoryNumber);
                // Manipulate the expected seconds due to loss of granularity when the
                // date-time-offset is serialized in a friendly format.
                Assert.True(testStartOn.AddSeconds(-1) <= config.LastBuildDirectoryCreatedOn);
                Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastBuildDirectoryCreatedOn);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreatesTrackingConfig()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                const string HashKey = "Some hash key";
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
                DateTimeOffset testStartOn = DateTimeOffset.Now;

                // Act.
                _trackingManager.Create(_ec.Object, _endpoint, HashKey, trackingFile, false);

                // Assert.
                TrackingConfig config = _trackingManager.LoadIfExists(_ec.Object, trackingFile) as TrackingConfig;
                Assert.Equal(
                    Path.Combine("1", Constants.Build.Path.ArtifactsDirectory),
                    config.ArtifactsDirectory);
                Assert.Equal("1", config.BuildDirectory);
                Assert.Equal(CollectionId, config.CollectionId);
                Assert.Equal(CollectionUrl, config.CollectionUrl);
                Assert.Equal(DefinitionId, config.DefinitionId);
                Assert.Equal(DefinitionName, config.DefinitionName);
                Assert.Equal(3, config.FileFormatVersion);
                Assert.Equal(HashKey, config.HashKey);
                // Manipulate the expected seconds due to loss of granularity when the
                // date-time-offset is serialized in a friendly format.
                Assert.True(testStartOn.AddSeconds(-1) <= config.LastRunOn);
                Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastRunOn);
                Assert.Equal(EndpointUrl, config.RepositoryUrl);
                Assert.Equal(
                    Path.Combine("1", Constants.Build.Path.SourcesDirectory),
                    config.SourcesDirectory);
                Assert.Equal("build", config.System);
                Assert.Equal(
                    Path.Combine("1", Constants.Build.Path.TestResultsDirectory),
                    config.TestResultsDirectory);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig_FileFormatVersion1()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                // It doesn't matter for this test whether the line endings are CRLF or just LF.
                const string Contents = @"{ 
    ""system"" : ""build"", 
    ""collectionId"" = ""7aee6dde-6381-4098-93e7-50a8264cf066"", 
    ""definitionId"" = ""7"", 
    ""repositoryUrl"" = ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"", 
    ""sourceFolder"" = ""C:\VsoTest\onprem\Agent\_work\b00335b6"",
    ""hashKey"" = ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c""
}";
                Directory.CreateDirectory(_workFolder);
                string filePath = Path.Combine(_workFolder, "trackingconfig.json");
                File.WriteAllText(filePath, Contents);

                // Act.
                TrackingConfigBase config = _trackingManager.LoadIfExists(_ec.Object, filePath);

                // Assert.
                Assert.NotNull(config);
                var legacyConfig = config as LegacyTrackingConfig;
                Assert.NotNull(legacyConfig);
                Assert.Equal(@"C:\VsoTest\onprem\Agent\_work\b00335b6", legacyConfig.BuildDirectory);
                Assert.Equal(@"7aee6dde-6381-4098-93e7-50a8264cf066", legacyConfig.CollectionId);
                Assert.Equal(@"7", legacyConfig.DefinitionId);
                Assert.Equal(@"b00335b6923adfa64f46f3abb7da1cdc0d9bae6c", legacyConfig.HashKey);
                Assert.Equal(@"http://contoso:8080/tfs/DefaultCollection/_git/gitTest", legacyConfig.RepositoryUrl);
                Assert.Equal(@"build", legacyConfig.System);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig_FileFormatVersion1_MissingProperty()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                // It doesn't matter for this test whether the line endings are CRLF or just LF.
                const string Contents = @"{ 
    ""system"" : ""build"", 
    ""collectionId"" = ""7aee6dde-6381-4098-93e7-50a8264cf066"", 
    ""definitionId"" = ""7"", 
    ""repositoryUrl"" = ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"", 
    ""sourceFolder"" = ""C:\VsoTest\onprem\Agent\_work\b00335b6"",
    ""hashKey"" = """"
}";
                // An expected property is missing from the legacy content - the hash key - so the
                // file should fail to parse properly.
                Directory.CreateDirectory(_workFolder);
                string filePath = Path.Combine(_workFolder, "trackingconfig.json");
                File.WriteAllText(filePath, Contents);

                // Act.
                TrackingConfigBase config = _trackingManager.LoadIfExists(_ec.Object, filePath);

                // Assert.
                Assert.Null(config);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig_FileFormatVersion1_InvalidJson()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                // It doesn't matter for this test whether the line endings are CRLF or just LF.
                string contents = @"{ 
    ""system"" : ""build"", 
    ""collectionId"" = ""7aee6dde-6381-4098-93e7-50a8264cf066"", 
    ""definitionId"" = ""7"", 
    ""repositoryUrl"" = ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"", 
    ""sourceFolder"" = ""C:\VsoTest\onprem\Agent\_work\b00335b6"",
    ""hashKey"" = ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c""
}";
                // Trim the trailing curly brace to make the legacy parser throw an exception.
                contents = contents.TrimEnd('}');
                Directory.CreateDirectory(_workFolder);
                string filePath = Path.Combine(_workFolder, "trackingconfig.json");
                File.WriteAllText(filePath, contents);

                // Act.
                TrackingConfigBase config = _trackingManager.LoadIfExists(_ec.Object, filePath);

                // Assert.
                Assert.Null(config);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig_FileFormatVersion2()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                // It doesn't matter for this test whether the line endings are CRLF or just LF.
                const string Contents = @"{
  ""build_artifactstagingdirectory"": ""b00335b6\\a"",
  ""agent_builddirectory"": ""b00335b6"",
  ""collectionName"": ""DefaultCollection"",
  ""definitionName"": ""M87_PrintEnvVars"",
  ""fileFormatVersion"": 2,
  ""lastRunOn"": ""09/16/2015 23:56:46 -04:00"",
  ""build_sourcesdirectory"": ""b00335b6\\gitTest"",
  ""common_testresultsdirectory"": ""b00335b6\\TestResults"",
  ""collectionId"": ""7aee6dde-6381-4098-93e7-50a8264cf066"",
  ""definitionId"": ""7"",
  ""hashKey"": ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c"",
  ""repositoryUrl"": ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"",
  ""system"": ""build""
}";
                Directory.CreateDirectory(_workFolder);
                string filePath = Path.Combine(_workFolder, "trackingconfig.json");
                File.WriteAllText(filePath, Contents);

                // Act.
                TrackingConfigBase baseConfig = _trackingManager.LoadIfExists(_ec.Object, filePath);

                // Assert.
                Assert.NotNull(baseConfig);
                TrackingConfig config = baseConfig as TrackingConfig;
                Assert.NotNull(config);
                Assert.Equal(@"b00335b6\a", config.ArtifactsDirectory);
                Assert.Equal(@"b00335b6", config.BuildDirectory);
                Assert.Equal(@"7aee6dde-6381-4098-93e7-50a8264cf066", config.CollectionId);
                Assert.Equal(@"", config.CollectionUrl ?? string.Empty);
                Assert.Equal(@"7", config.DefinitionId);
                Assert.Equal(@"M87_PrintEnvVars", config.DefinitionName);
                Assert.Equal(3, config.FileFormatVersion);
                Assert.Equal(@"b00335b6923adfa64f46f3abb7da1cdc0d9bae6c", config.HashKey);
                Assert.Equal(new DateTimeOffset(2015, 9, 16, 23, 56, 46, TimeSpan.FromHours(-4)), config.LastRunOn);
                Assert.Equal(@"http://contoso:8080/tfs/DefaultCollection/_git/gitTest", config.RepositoryUrl);
                Assert.Equal(@"b00335b6\gitTest", config.SourcesDirectory);
                Assert.Equal(@"build", config.System);
                Assert.Equal(@"b00335b6\TestResults", config.TestResultsDirectory);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig_FileFormatVersion3()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                // It doesn't matter for this test whether the line endings are CRLF or just LF.
                const string Contents = @"{
  ""build_artifactstagingdirectory"": ""b00335b6\\a"",
  ""agent_builddirectory"": ""b00335b6"",
  ""collectionUrl"": ""http://contoso:8080/tfs/DefaultCollection/"",
  ""definitionName"": ""M87_PrintEnvVars"",
  ""fileFormatVersion"": 3,
  ""lastRunOn"": ""09/16/2015 23:56:46 -04:00"",
  ""build_sourcesdirectory"": ""b00335b6\\gitTest"",
  ""common_testresultsdirectory"": ""b00335b6\\TestResults"",
  ""collectionId"": ""7aee6dde-6381-4098-93e7-50a8264cf066"",
  ""definitionId"": ""7"",
  ""hashKey"": ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c"",
  ""repositoryUrl"": ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"",
  ""system"": ""build""
}";
                Directory.CreateDirectory(_workFolder);
                string filePath = Path.Combine(_workFolder, "trackingconfig.json");
                File.WriteAllText(filePath, Contents);

                // Act.
                TrackingConfigBase baseConfig = _trackingManager.LoadIfExists(_ec.Object, filePath);

                // Assert.
                Assert.NotNull(baseConfig);
                TrackingConfig config = baseConfig as TrackingConfig;
                Assert.NotNull(config);
                Assert.Equal(@"b00335b6\a", config.ArtifactsDirectory);
                Assert.Equal(@"b00335b6", config.BuildDirectory);
                Assert.Equal(@"7aee6dde-6381-4098-93e7-50a8264cf066", config.CollectionId);
                Assert.Equal(CollectionUrl, config.CollectionUrl);
                Assert.Equal(@"7", config.DefinitionId);
                Assert.Equal(@"M87_PrintEnvVars", config.DefinitionName);
                Assert.Equal(3, config.FileFormatVersion);
                Assert.Equal(@"b00335b6923adfa64f46f3abb7da1cdc0d9bae6c", config.HashKey);
                Assert.Equal(new DateTimeOffset(2015, 9, 16, 23, 56, 46, TimeSpan.FromHours(-4)), config.LastRunOn);
                Assert.Equal(@"http://contoso:8080/tfs/DefaultCollection/_git/gitTest", config.RepositoryUrl);
                Assert.Equal(@"b00335b6\gitTest", config.SourcesDirectory);
                Assert.Equal(@"build", config.System);
                Assert.Equal(@"b00335b6\TestResults", config.TestResultsDirectory);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LoadsTrackingConfig_NotExists()
        {
            using (TestHostContext hc = Setup())
            {
                // Act.
                TrackingConfigBase config = _trackingManager.LoadIfExists(
                    _ec.Object,
                    Path.Combine(_workFolder, "foo.json"));

                // Assert.
                Assert.Null(config);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MarksTrackingConfigForGarbageCollection()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                // It doesn't matter for this test whether the line endings are CRLF or just LF.
                const string TrackingContents = @"{
  ""build_artifactstagingdirectory"": ""b00335b6\\a"",
  ""agent_builddirectory"": ""b00335b6"",
  ""collectionUrl"": ""http://contoso:8080/tfs/DefaultCollection/"",
  ""definitionName"": ""M87_PrintEnvVars"",
  ""fileFormatVersion"": 3,
  ""lastRunOn"": ""09/16/2015 23:56:46 -04:00"",
  ""repositoryType"": ""tfsgit"",
  ""lastMaintenanceAttemptedOn"": ""09/16/2015 23:56:46 -04:00"",
  ""lastMaintenanceCompletedOn"": ""09/16/2015 23:56:46 -04:00"",
  ""build_sourcesdirectory"": ""b00335b6\\gitTest"",
  ""common_testresultsdirectory"": ""b00335b6\\TestResults"",
  ""collectionId"": ""7aee6dde-6381-4098-93e7-50a8264cf066"",
  ""definitionId"": ""7"",
  ""hashKey"": ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c"",
  ""repositoryUrl"": ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"",
  ""system"": ""build""
}";
                Directory.CreateDirectory(_workFolder);
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
                File.WriteAllText(trackingFile, TrackingContents);
                TrackingConfig config = _trackingManager.LoadIfExists(_ec.Object, trackingFile) as TrackingConfig;
                Assert.NotNull(config);

                // Act.
                _trackingManager.MarkForGarbageCollection(_ec.Object, config);

                // Assert.
                string gcDirectory = Path.Combine(
                    _workFolder,
                    Constants.Build.Path.SourceRootMappingDirectory,
                    Constants.Build.Path.GarbageCollectionDirectory);
                Assert.True(Directory.Exists(gcDirectory));
                string[] gcFiles = Directory.GetFiles(gcDirectory);
                Assert.Equal(1, gcFiles.Length);
                string gcFile = gcFiles.Single();
                string gcContents = File.ReadAllText(gcFile);
                Assert.Equal(TrackingContents, gcContents);
                // File name should a GUID.
                Assert.True(Regex.IsMatch(Path.GetFileNameWithoutExtension(gcFile), "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"));
                // File name should not be the default GUID.
                Assert.NotEqual("00000000-0000-0000-0000-000000000000", Path.GetFileNameWithoutExtension(gcFile));
            }
        }

#if OS_WINDOWS
        // Legacy config back-compat is required for Windows only.
        // The legacy config files never existed on xplat in this form.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
#endif
        public void MarksTrackingConfigForGarbageCollection_Legacy()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                // It doesn't matter for this test whether the line endings are CRLF or just LF.
                const string TrackingContents = @"{
    ""system"" : ""build"", 
    ""collectionId"" = ""7aee6dde-6381-4098-93e7-50a8264cf066"", 
    ""definitionId"" = ""7"", 
    ""repositoryUrl"" = ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"", 
    ""sourceFolder"" = ""C:\VsoTest\onprem\Agent\_work\b00335b6"",
    ""hashKey"" = ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c""
}";
                Directory.CreateDirectory(_workFolder);
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
                File.WriteAllText(trackingFile, TrackingContents);
                LegacyTrackingConfig config = _trackingManager.LoadIfExists(_ec.Object, trackingFile) as LegacyTrackingConfig;
                Assert.NotNull(config);

                // Act.
                _trackingManager.MarkForGarbageCollection(_ec.Object, config);

                // Assert.
                string gcDirectory = Path.Combine(
                    _workFolder,
                    Constants.Build.Path.SourceRootMappingDirectory,
                    Constants.Build.Path.GarbageCollectionDirectory);
                Assert.True(Directory.Exists(gcDirectory));
                string[] gcFiles = Directory.GetFiles(gcDirectory);
                Assert.Equal(1, gcFiles.Length);
                string gcContents = File.ReadAllText(gcFiles.Single());
                const string ExpectedGCContents = @"{
  ""build_artifactstagingdirectory"": ""b00335b6\\artifacts"",
  ""agent_builddirectory"": ""b00335b6"",
  ""collectionUrl"": ""http://contoso:8080/tfs/DefaultCollection/"",
  ""definitionName"": null,
  ""fileFormatVersion"": 3,
  ""lastRunOn"": """",
  ""repositoryType"": """",
  ""lastMaintenanceAttemptedOn"": """",
  ""lastMaintenanceCompletedOn"": """",
  ""build_sourcesdirectory"": ""b00335b6\\s"",
  ""common_testresultsdirectory"": ""b00335b6\\TestResults"",
  ""collectionId"": ""7aee6dde-6381-4098-93e7-50a8264cf066"",
  ""definitionId"": ""7"",
  ""hashKey"": ""b00335b6923adfa64f46f3abb7da1cdc0d9bae6c"",
  ""repositoryUrl"": ""http://contoso:8080/tfs/DefaultCollection/_git/gitTest"",
  ""system"": ""build""
}";
                Assert.Equal(ExpectedGCContents, gcContents);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UpdatesTopLevelTrackingConfig()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");
                _trackingManager.Create(_ec.Object, _endpoint, "some hash key", trackingFile, false);
                DateTimeOffset testStartOn = DateTimeOffset.Now;

                // Act.
                _trackingManager.Create(_ec.Object, _endpoint, "some hash key", trackingFile, false);

                // Assert.
                string topLevelFile = Path.Combine(
                    _workFolder,
                    Constants.Build.Path.SourceRootMappingDirectory,
                    Constants.Build.Path.TopLevelTrackingConfigFile);
                TopLevelTrackingConfig config = JsonConvert.DeserializeObject<TopLevelTrackingConfig>(
                    value: File.ReadAllText(topLevelFile));
                Assert.Equal(2, config.LastBuildDirectoryNumber);
                // Manipulate the expected seconds due to loss of granularity when the
                // date-time-offset is serialized in a friendly format.
                Assert.True(testStartOn.AddSeconds(-1) <= config.LastBuildDirectoryCreatedOn);
                Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastBuildDirectoryCreatedOn);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UpdatesTrackingConfigJobRunProperties()
        {
            using (TestHostContext hc = Setup())
            {
                // Arrange.
                DateTimeOffset testStartOn = DateTimeOffset.Now;
                TrackingConfig config = new TrackingConfig();
                string trackingFile = Path.Combine(_workFolder, "trackingconfig.json");

                // Act.
                _trackingManager.UpdateJobRunProperties(_ec.Object, config, trackingFile);

                // Assert.
                config = _trackingManager.LoadIfExists(_ec.Object, trackingFile) as TrackingConfig;
                Assert.NotNull(config);
                Assert.Equal(CollectionUrl, config.CollectionUrl);
                Assert.Equal(DefinitionName, config.DefinitionName);
                // Manipulate the expected seconds due to loss of granularity when the
                // date-time-offset is serialized in a friendly format.
                Assert.True(testStartOn.AddSeconds(-1) <= config.LastRunOn);
                Assert.True(DateTimeOffset.Now.AddSeconds(1) >= config.LastRunOn);
            }
        }
    }
}
