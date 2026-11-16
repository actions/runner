using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ArtifactsListFileCommandL0
    {
        private Mock<IExecutionContext> _executionContext;
        private string _rootDirectory;
        private string _outputFile;
        private ArtifactsListFileCommand _command;
        private GlobalContext _global;
        private ITraceWriter _trace;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EmptyAggregate_WritesVersionedJsonWithEmptySubjects()
        {
            using (var hostContext = Setup())
            {
                _command.PopulateInitialContents(_executionContext.Object, _outputFile, null);
                var json = JObject.Parse(File.ReadAllText(_outputFile));
                Assert.Equal(ArtifactsListFileCommand.FormatVersion, json["version"].Value<int>());
                Assert.Empty((JArray)json["subjects"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SingleSubject_SerializedCorrectly()
        {
            using (var hostContext = Setup())
            {
                _global.ArtifactSubjects["myapp"] = new ArtifactSubject(
                    "myapp",
                    "sha256:" + new string('a', 64),
                    ArtifactSubjectKind.File);

                _command.PopulateInitialContents(_executionContext.Object, _outputFile, null);

                var json = JObject.Parse(File.ReadAllText(_outputFile));
                var subjects = (JArray)json["subjects"];
                Assert.Single(subjects);
                Assert.Equal("myapp", subjects[0]["name"].Value<string>());
                Assert.Equal("sha256:" + new string('a', 64), subjects[0]["digest"].Value<string>());
                Assert.Equal("file", subjects[0]["kind"].Value<string>());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_KindIsLowercaseOci()
        {
            using (var hostContext = Setup())
            {
                _global.ArtifactSubjects["ghcr.io/x:1"] = new ArtifactSubject(
                    "ghcr.io/x:1",
                    "sha256:" + new string('b', 64),
                    ArtifactSubjectKind.OciSubject);

                _command.PopulateInitialContents(_executionContext.Object, _outputFile, null);

                var json = JObject.Parse(File.ReadAllText(_outputFile));
                Assert.Equal("oci", json["subjects"][0]["kind"].Value<string>());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MultipleSubjects_SortedByName()
        {
            using (var hostContext = Setup())
            {
                // Insert deliberately out of alphabetical order to prove the
                // output is sorted by name rather than by insertion order.
                _global.ArtifactSubjects["two"] = new ArtifactSubject("two", "sha256:" + new string('2', 64), ArtifactSubjectKind.File);
                _global.ArtifactSubjects["one"] = new ArtifactSubject("one", "sha256:" + new string('1', 64), ArtifactSubjectKind.File);
                _global.ArtifactSubjects["three"] = new ArtifactSubject("three", "sha256:" + new string('3', 64), ArtifactSubjectKind.OciSubject);

                _command.PopulateInitialContents(_executionContext.Object, _outputFile, null);

                var subjects = (JArray)JObject.Parse(File.ReadAllText(_outputFile))["subjects"];
                Assert.Equal(3, subjects.Count);
                Assert.Equal("one", subjects[0]["name"].Value<string>());
                Assert.Equal("three", subjects[1]["name"].Value<string>());
                Assert.Equal("two", subjects[2]["name"].Value<string>());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FeatureFlagOff_LeavesFileEmpty()
        {
            using (var hostContext = Setup(featureFlag: false))
            {
                _global.ArtifactSubjects["myapp"] = new ArtifactSubject("myapp", "sha256:" + new string('a', 64), ArtifactSubjectKind.File);

                _command.PopulateInitialContents(_executionContext.Object, _outputFile, null);

                Assert.Equal(string.Empty, File.ReadAllText(_outputFile));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnvVarFallback_EnablesPublishing()
        {
            using (var hostContext = Setup(featureFlag: false, envVarOverride: "true"))
            {
                _global.ArtifactSubjects["myapp"] = new ArtifactSubject("myapp", "sha256:" + new string('a', 64), ArtifactSubjectKind.File);

                _command.PopulateInitialContents(_executionContext.Object, _outputFile, null);

                var json = JObject.Parse(File.ReadAllText(_outputFile));
                Assert.Single((JArray)json["subjects"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OutputFileIsUtf8WithoutBom()
        {
            using (var hostContext = Setup())
            {
                _command.PopulateInitialContents(_executionContext.Object, _outputFile, null);

                var bytes = File.ReadAllBytes(_outputFile);
                // UTF-8 BOM is EF BB BF
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    "File should not begin with a UTF-8 BOM.");
                // Sanity check that the file is valid JSON.
                Assert.NotNull(JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes)));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ProcessCommand_IsNoOp()
        {
            using (var hostContext = Setup())
            {
                // Even if the step writes garbage, ProcessCommand must not touch the aggregate.
                File.WriteAllText(_outputFile, "anything the step wrote");
                _command.ProcessCommand(_executionContext.Object, _outputFile, null);
                Assert.Empty(_global.ArtifactSubjects);
            }
        }

        private TestHostContext Setup(bool featureFlag = true, string envVarOverride = null, [CallerMemberName] string name = "")
        {
            // Reset env-var state across test runs in the same process.
            Environment.SetEnvironmentVariable(CreateArtifactsFileCommand.EnableEnvVar, envVarOverride);

            var hostContext = new TestHostContext(this, name);
            _trace = hostContext.GetTrace();

            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(ArtifactsListFileCommandL0), name);
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
            Directory.CreateDirectory(_rootDirectory);
            _outputFile = Path.Combine(_rootDirectory, "artifacts_list");
            File.WriteAllText(_outputFile, string.Empty);

            var variableValues = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            if (featureFlag)
            {
                variableValues[Common.Constants.Runner.Features.AllowArtifactsFile] = new VariableValue("true");
            }
            var variables = new Variables(hostContext, variableValues);

            _global = new GlobalContext
            {
                EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                Variables = variables,
                WriteDebug = true,
                ArtifactSubjects = new Dictionary<string, ArtifactSubject>(StringComparer.Ordinal),
            };

            _executionContext = new Mock<IExecutionContext>();
            _executionContext.Setup(x => x.Global).Returns(_global);
            _executionContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) =>
                {
                    _trace.Info($"{tag}{message}");
                });

            _command = new ArtifactsListFileCommand();
            _command.Initialize(hostContext);

            return hostContext;
        }
    }
}
