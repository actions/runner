using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class CreateArtifactsFileCommandL0
    {
        private const string FlagOn = "true";

        private Mock<IExecutionContext> _executionContext;
        private List<DTWebApi.Issue> _issues;
        private string _rootDirectory;
        private string _workspaceDirectory;
        private CreateArtifactsFileCommand _command;
        private GlobalContext _global;
        private ITraceWriter _trace;

        // ---------- Feature flag ----------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FeatureFlagOff_NoOp()
        {
            using (var hostContext = Setup(featureFlag: false))
            {
                var artifactsFile = WriteArtifactsFile("ghcr.io/octocat/myapp:1.0@sha256:" + new string('a', 64));
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Empty(_global.ArtifactSubjects);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnvVarOverride_EnablesFeature()
        {
            using (var hostContext = Setup(featureFlag: false, envVarOverride: "true"))
            {
                var hex = new string('a', 64);
                var artifactsFile = WriteArtifactsFile($"ghcr.io/octocat/myapp:1.0@sha256:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Single(_global.ArtifactSubjects);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnvVarFalse_DoesNotEnable()
        {
            using (var hostContext = Setup(featureFlag: false, envVarOverride: "false"))
            {
                var artifactsFile = WriteArtifactsFile("ghcr.io/x@sha256:" + new string('a', 64));
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Empty(_global.ArtifactSubjects);
            }
        }

        // ---------- Trivial cases ----------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileMissing_NoOp()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = Path.Combine(_rootDirectory, "does-not-exist");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Empty(_global.ArtifactSubjects);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EmptyFile_NoOp()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = WriteArtifactsFile(string.Empty);
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Empty(_global.ArtifactSubjects);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void BlankAndCommentLines_Skipped()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = WriteArtifactsFile(
                    "",
                    "# this is a comment",
                    "   # leading-whitespace comment",
                    "",
                    "   ");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Empty(_global.ArtifactSubjects);
            }
        }

        // ---------- OCI subjects ----------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_Sha256_HappyPath()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('a', 64);
                var artifactsFile = WriteArtifactsFile($"ghcr.io/octocat/myapp:1.0.0@sha256:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Single(_global.ArtifactSubjects);
                var subject = _global.ArtifactSubjects["ghcr.io/octocat/myapp:1.0.0"];
                Assert.Equal($"sha256:{hex}", subject.Digest);
                Assert.Equal(ArtifactSubjectKind.OciSubject, subject.Kind);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_Sha384_HappyPath()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('b', 96);
                var artifactsFile = WriteArtifactsFile($"ghcr.io/x/y@sha384:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Single(_global.ArtifactSubjects);
                Assert.Equal($"sha384:{hex}", _global.ArtifactSubjects["ghcr.io/x/y"].Digest);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_Sha512_HappyPath()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('c', 128);
                var artifactsFile = WriteArtifactsFile($"ghcr.io/x/y@sha512:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Single(_global.ArtifactSubjects);
                Assert.Equal($"sha512:{hex}", _global.ArtifactSubjects["ghcr.io/x/y"].Digest);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_DigestLowercased()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('A', 64);
                var artifactsFile = WriteArtifactsFile($"ghcr.io/x@sha256:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Equal($"sha256:{hex.ToLowerInvariant()}", _global.ArtifactSubjects["ghcr.io/x"].Digest);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_PreservesTagAndRegistryPort()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('d', 64);
                var artifactsFile = WriteArtifactsFile($"localhost:5000/repo/img:v1@sha256:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Single(_global.ArtifactSubjects);
                Assert.True(_global.ArtifactSubjects.ContainsKey("localhost:5000/repo/img:v1"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_WrongHexLength_Throws()
        {
            using (var hostContext = Setup())
            {
                // 63 hex chars instead of 64 → falls back to file path parse → file missing → throws
                var artifactsFile = WriteArtifactsFile("ghcr.io/x@sha256:" + new string('a', 63));
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 1", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciSubject_NonHexChars_TreatedAsFile_Throws()
        {
            using (var hostContext = Setup())
            {
                // Non-hex character: digest regex doesn't match → treated as file path → file missing
                var artifactsFile = WriteArtifactsFile("ghcr.io/x@sha256:" + new string('z', 64));
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 1", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciScheme_RejectsWhenDigestMissing()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = WriteArtifactsFile("oci://ghcr.io/x:1.0");
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 1", ex.Message);
                Assert.Contains("digest", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OciScheme_HappyPath()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('e', 64);
                var artifactsFile = WriteArtifactsFile($"OCI://ghcr.io/x:1.0@sha256:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Single(_global.ArtifactSubjects);
                Assert.True(_global.ArtifactSubjects.ContainsKey("ghcr.io/x:1.0"));
            }
        }

        // ---------- File subjects ----------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileSubject_Absolute_HappyPath()
        {
            using (var hostContext = Setup())
            {
                var artifactPath = Path.Combine(_rootDirectory, "binary.bin");
                File.WriteAllBytes(artifactPath, new byte[] { 1, 2, 3, 4 });
                var artifactsFile = WriteArtifactsFile(artifactPath);

                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);

                Assert.Single(_global.ArtifactSubjects);
                var subject = _global.ArtifactSubjects["binary.bin"];
                Assert.Equal(ArtifactSubjectKind.File, subject.Kind);
                // sha256("\x01\x02\x03\x04") = 9f64a747e1b97f131fabb6b447296c9b6f0201e79fb3c5356e6c77e89b6a806a
                Assert.Equal("sha256:9f64a747e1b97f131fabb6b447296c9b6f0201e79fb3c5356e6c77e89b6a806a", subject.Digest);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileSubject_RelativeToWorkspace()
        {
            using (var hostContext = Setup())
            {
                Directory.CreateDirectory(Path.Combine(_workspaceDirectory, "dist"));
                File.WriteAllBytes(Path.Combine(_workspaceDirectory, "dist", "myapp"), new byte[] { 9 });
                var artifactsFile = WriteArtifactsFile("dist/myapp");

                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);

                Assert.Single(_global.ArtifactSubjects);
                Assert.True(_global.ArtifactSubjects.ContainsKey("myapp"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileScheme_TreatsAsFilePathEvenIfLooksLikeOci()
        {
            using (var hostContext = Setup())
            {
                // File literally named "image@sha256:deadbeef..." — force file path via file:// prefix.
                var quirkyName = "image@sha256:" + new string('f', 64);
                var artifactPath = Path.Combine(_rootDirectory, quirkyName);
                File.WriteAllBytes(artifactPath, new byte[] { 1 });
                var artifactsFile = WriteArtifactsFile("file://" + artifactPath);

                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);

                Assert.Single(_global.ArtifactSubjects);
                var subject = _global.ArtifactSubjects[quirkyName];
                Assert.Equal(ArtifactSubjectKind.File, subject.Kind);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileSubject_Missing_Throws()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = WriteArtifactsFile(Path.Combine(_rootDirectory, "does-not-exist"));
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 1", ex.Message);
                Assert.Contains("does not exist", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileSubject_Directory_Throws()
        {
            using (var hostContext = Setup())
            {
                var dir = Path.Combine(_rootDirectory, "a-directory");
                Directory.CreateDirectory(dir);
                var artifactsFile = WriteArtifactsFile(dir);
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 1", ex.Message);
                Assert.Contains("not a regular file", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileSubject_ContainerAbsolute_InMount_ResolvesToHostFile()
        {
            using (var hostContext = Setup())
            {
                // The host file lives under a directory that is mounted into
                // the container. The step declares the file using its
                // container-namespace path, which must translate back to the
                // host file so the digest is computed over the right bytes.
                var hostDirectory = Path.Combine(_rootDirectory, "mounted");
                Directory.CreateDirectory(hostDirectory);
                File.WriteAllBytes(Path.Combine(hostDirectory, "app.bin"), new byte[] { 1, 2, 3, 4 });

                var container = new ContainerInfo();
                var containerDirectory = "/container-workspace";
                container.AddPathTranslateMapping(hostDirectory, containerDirectory);

                var artifactsFile = WriteArtifactsFile(Path.Combine(containerDirectory, "app.bin"));
                _command.ProcessCommand(_executionContext.Object, artifactsFile, container);

                Assert.Single(_global.ArtifactSubjects);
                var subject = _global.ArtifactSubjects["app.bin"];
                Assert.Equal(ArtifactSubjectKind.File, subject.Kind);
                // sha256("\x01\x02\x03\x04")
                Assert.Equal("sha256:9f64a747e1b97f131fabb6b447296c9b6f0201e79fb3c5356e6c77e89b6a806a", subject.Digest);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileSubject_ContainerAbsolute_OutsideMount_Throws()
        {
            using (var hostContext = Setup())
            {
                // An absolute path that does not resolve into any mounted
                // volume must be rejected rather than silently hashing the
                // host file that happens to live at that same path.
                var container = new ContainerInfo();
                container.AddPathTranslateMapping(Path.Combine(_rootDirectory, "mounted"), "/container-workspace");

                var artifactsFile = WriteArtifactsFile(Path.Combine("/unmapped-container-dir", "secret.bin"));
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, container));
                Assert.Contains("line 1", ex.Message);
                Assert.Contains("not inside a volume mounted", ex.Message);
            }
        }

        // ---------- Format / scheme rules ----------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EqualsSign_Rejected()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = WriteArtifactsFile("name=ghcr.io/x@sha256:" + new string('a', 64));
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 1", ex.Message);
                Assert.Contains("'='", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UnsupportedScheme_Rejected()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = WriteArtifactsFile("https://example.com/artifact");
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 1", ex.Message);
                Assert.Contains("unsupported URI scheme", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LineNumberInError_PointsAtRightLine()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('a', 64);
                var artifactsFile = WriteArtifactsFile(
                    "# comment",
                    $"ghcr.io/ok@sha256:{hex}",
                    "",
                    "name=bogus");
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("line 4", ex.Message);
            }
        }

        // ---------- Size and aggregate limits ----------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FileTooLarge_Throws()
        {
            using (var hostContext = Setup())
            {
                var artifactsFile = Path.Combine(_rootDirectory, "huge");
                // Slightly larger than 1 MiB.
                File.WriteAllBytes(artifactsFile, new byte[CreateArtifactsFileCommand.MaxFileSizeBytes + 1]);
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("$GITHUB_ARTIFACTS", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AggregateCap_AllowsDuplicatesAtCap()
        {
            using (var hostContext = Setup())
            {
                // Pre-fill the aggregate to exactly the cap.
                var hex = new string('a', 64);
                for (var i = 0; i < CreateArtifactsFileCommand.MaxAggregateArtifacts; i++)
                {
                    var name = $"ghcr.io/x{i}";
                    _global.ArtifactSubjects[name] = new ArtifactSubject(name, $"sha256:{hex}", ArtifactSubjectKind.OciSubject);
                }

                // A new step redeclares one of the existing artifacts identically — should NOT throw.
                var artifactsFile = WriteArtifactsFile($"ghcr.io/x0@sha256:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Equal(CreateArtifactsFileCommand.MaxAggregateArtifacts, _global.ArtifactSubjects.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AggregateCap_FailsOnFirstDistinctOverflow()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('a', 64);
                for (var i = 0; i < CreateArtifactsFileCommand.MaxAggregateArtifacts; i++)
                {
                    var name = $"ghcr.io/x{i}";
                    _global.ArtifactSubjects[name] = new ArtifactSubject(name, $"sha256:{hex}", ArtifactSubjectKind.OciSubject);
                }

                var artifactsFile = WriteArtifactsFile($"ghcr.io/new@sha256:{hex}");
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("500", ex.Message);
            }
        }

        // ---------- Aggregation rules ----------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Aggregation_DedupsIdentical()
        {
            using (var hostContext = Setup())
            {
                var hex = new string('a', 64);
                _global.ArtifactSubjects["ghcr.io/x"] = new ArtifactSubject("ghcr.io/x", $"sha256:{hex}", ArtifactSubjectKind.OciSubject);
                var artifactsFile = WriteArtifactsFile($"ghcr.io/x@sha256:{hex}");
                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);
                Assert.Single(_global.ArtifactSubjects);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Aggregation_FailsOnConflict()
        {
            using (var hostContext = Setup())
            {
                var hexA = new string('a', 64);
                var hexB = new string('b', 64);
                _global.ArtifactSubjects["ghcr.io/x"] = new ArtifactSubject("ghcr.io/x", $"sha256:{hexA}", ArtifactSubjectKind.OciSubject);
                var artifactsFile = WriteArtifactsFile($"ghcr.io/x@sha256:{hexB}");
                var ex = Assert.Throws<Exception>(() => _command.ProcessCommand(_executionContext.Object, artifactsFile, null));
                Assert.Contains("Conflicting digest", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MultipleEntries_AllProcessed()
        {
            using (var hostContext = Setup())
            {
                Directory.CreateDirectory(Path.Combine(_workspaceDirectory, "dist"));
                File.WriteAllBytes(Path.Combine(_workspaceDirectory, "dist", "myapp-linux-amd64"), new byte[] { 7 });

                var hex = new string('a', 64);
                var artifactsFile = WriteArtifactsFile(
                    "# Release binary",
                    "dist/myapp-linux-amd64",
                    "",
                    "# Published container image",
                    $"ghcr.io/octocat/myapp:1.0.0@sha256:{hex}");

                _command.ProcessCommand(_executionContext.Object, artifactsFile, null);

                Assert.Equal(2, _global.ArtifactSubjects.Count);
                Assert.True(_global.ArtifactSubjects.ContainsKey("myapp-linux-amd64"));
                Assert.True(_global.ArtifactSubjects.ContainsKey("ghcr.io/octocat/myapp:1.0.0"));
            }
        }

        // ---------- Setup helpers ----------

        private string WriteArtifactsFile(params string[] lines)
        {
            var path = Path.Combine(_rootDirectory, "artifacts");
            File.WriteAllText(path, string.Join("\n", lines), new UTF8Encoding(false));
            return path;
        }

        private TestHostContext Setup(bool featureFlag = true, string envVarOverride = null, [CallerMemberName] string name = "")
        {
            _issues = new List<DTWebApi.Issue>();

            // Ensure no leaked state from prior tests in the same process.
            Environment.SetEnvironmentVariable(CreateArtifactsFileCommand.EnableEnvVar, envVarOverride);

            var hostContext = new TestHostContext(this, name);
            _trace = hostContext.GetTrace();

            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(CreateArtifactsFileCommandL0), name);
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
            Directory.CreateDirectory(_rootDirectory);

            _workspaceDirectory = Path.Combine(_rootDirectory, "workspace");
            Directory.CreateDirectory(_workspaceDirectory);

            var variableValues = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            if (featureFlag)
            {
                variableValues[Common.Constants.Runner.Features.AllowArtifactsFile] = new VariableValue(FlagOn);
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
            _executionContext.Setup(x => x.GetGitHubContext("workspace")).Returns(_workspaceDirectory);
            _executionContext.Setup(x => x.AddIssue(It.IsAny<DTWebApi.Issue>(), It.IsAny<ExecutionContextLogOptions>()))
                .Callback((DTWebApi.Issue issue, ExecutionContextLogOptions logOptions) =>
                {
                    _issues.Add(issue);
                    _trace.Info($"Issue '{issue.Type}': {issue.Message}");
                });
            _executionContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) =>
                {
                    _trace.Info($"{tag}{message}");
                });

            _command = new CreateArtifactsFileCommand();
            _command.Initialize(hostContext);

            return hostContext;
        }
    }
}
