using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class CreateStepSummaryCommandL0
    {
        private Mock<IExecutionContext> _executionContext;
        private List<Tuple<DTWebApi.Issue, string>> _issues;
        private Variables _variables;
        private string _rootDirectory;
        private CreateStepSummaryCommand _createStepCommand;
        private ITraceWriter _trace;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_FeatureDisabled()
        {
            using (var hostContext = Setup(featureFlagState: "false"))
            {
                var stepSummaryFile = Path.Combine(_rootDirectory, "feature-off");

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, It.IsAny<string>(), It.IsAny<string>()), Times.Never());

                Assert.Equal(0, _issues.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_FileNull()
        {
            using (var hostContext = Setup())
            {
                _createStepCommand.ProcessCommand(_executionContext.Object, null, null);

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                Assert.Equal(0, _issues.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_DirectoryNotFound()
        {
            using (var hostContext = Setup())
            {
                var stepSummaryFile = Path.Combine(_rootDirectory, "directory-not-found", "env");

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                Assert.Equal(0, _issues.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_FileNotFound()
        {
            using (var hostContext = Setup())
            {
                var stepSummaryFile = Path.Combine(_rootDirectory, "file-not-found");

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                Assert.Equal(0, _issues.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_EmptyFile()
        {
            using (var hostContext = Setup())
            {
                var stepSummaryFile = Path.Combine(_rootDirectory, "empty-file");
                File.Create(stepSummaryFile).Dispose();

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                Assert.Equal(0, _issues.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_LargeFile()
        {
            using (var hostContext = Setup())
            {
                var stepSummaryFile = Path.Combine(_rootDirectory, "empty-file");
                File.WriteAllBytes(stepSummaryFile, new byte[128 * 1024 + 1]);

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                Assert.Equal(1, _issues.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_Simple()
        {
            using (var hostContext = Setup())
            {
                var stepSummaryFile = Path.Combine(_rootDirectory, "simple");
                var content = new List<string>
                {
                    "# This is some markdown content",
                    "",
                    "## This is more markdown content",
                };
                WriteContent(stepSummaryFile, content);

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, _executionContext.Object.Id.ToString(), stepSummaryFile + "-scrubbed"), Times.Once());
                Assert.Equal(0, _issues.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_ScrubSecrets()
        {
            using (var hostContext = Setup())
            {
                // configure secretmasker to actually mask secrets
                hostContext.SecretMasker.AddRegex("Password=.*");
                hostContext.SecretMasker.AddRegex("ghs_.*");

                var stepSummaryFile = Path.Combine(_rootDirectory, "simple");
                var scrubbedFile = stepSummaryFile + "-scrubbed";
                var content = new List<string>
                {
                    "# Password=ThisIsMySecretPassword!",
                    "",
                    "# GITHUB_TOKEN ghs_verysecuretoken",
                };
                WriteContent(stepSummaryFile, content);

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);

                var scrubbedFileContents = File.ReadAllText(scrubbedFile);
                Assert.DoesNotContain("ThisIsMySecretPassword!", scrubbedFileContents);
                Assert.DoesNotContain("ghs_verysecuretoken", scrubbedFileContents);

                _executionContext.Verify(e => e.QueueAttachFile(ChecksAttachmentType.StepSummary, _executionContext.Object.Id.ToString(), scrubbedFile), Times.Once());
                Assert.Equal(0, _issues.Count);
            }
        }

        private void WriteContent(
            string path,
            List<string> content,
            string newline = null)
        {
            if (string.IsNullOrEmpty(newline))
            {
                newline = Environment.NewLine;
            }

            var encoding = new UTF8Encoding(true); // Emit BOM
            var contentStr = string.Join(newline, content);
            File.WriteAllText(path, contentStr, encoding);
        }

        private TestHostContext Setup([CallerMemberName] string name = "", string featureFlagState = "true")
        {
            _issues = new List<Tuple<DTWebApi.Issue, string>>();

            var hostContext = new TestHostContext(this, name);

            // Trace
            _trace = hostContext.GetTrace();

            _variables = new Variables(hostContext, new Dictionary<string, VariableValue>
                {
                    { "MySecretName", new VariableValue("My secret value", true) },
                    { "DistributedTask.UploadStepSummary", featureFlagState },
                });

            // Directory for test data
            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(CreateStepSummaryCommandL0));
            Directory.CreateDirectory(_rootDirectory);

            // Execution context
            _executionContext = new Mock<IExecutionContext>();
            _executionContext.Setup(x => x.Global)
                .Returns(new GlobalContext
                {
                    EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                    WriteDebug = true,
                    Variables = _variables,
                });
            _executionContext.Setup(x => x.AddIssue(It.IsAny<DTWebApi.Issue>(), It.IsAny<string>()))
                .Callback((DTWebApi.Issue issue, string logMessage) =>
                {
                    _issues.Add(new Tuple<DTWebApi.Issue, string>(issue, logMessage));
                    var message = !string.IsNullOrEmpty(logMessage) ? logMessage : issue.Message;
                    _trace.Info($"Issue '{issue.Type}': {message}");
                });
            _executionContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) =>
                {
                    _trace.Info($"{tag}{message}");
                });

            //CreateStepSummaryCommand
            _createStepCommand = new CreateStepSummaryCommand();
            _createStepCommand.Initialize(hostContext);

            return hostContext;
        }
    }
}
