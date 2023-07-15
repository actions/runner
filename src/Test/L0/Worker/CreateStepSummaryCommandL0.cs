﻿using System;
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
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class CreateStepSummaryCommandL0
    {
        private Mock<IExecutionContext> _executionContext;
        private Mock<IJobServerQueue> _jobServerQueue;
        private ExecutionContext _jobExecutionContext;
        private List<Tuple<DTWebApi.Issue, string>> _issues;
        private Variables _variables;
        private string _rootDirectory;
        private CreateStepSummaryCommand _createStepCommand;
        private ITraceWriter _trace;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepSummaryCommand_FileNull()
        {
            using (var hostContext = Setup())
            {
                _createStepCommand.ProcessCommand(_executionContext.Object, null, null);
                _jobExecutionContext.Complete();

                _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
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
                _jobExecutionContext.Complete();

                _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
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
                _jobExecutionContext.Complete();

                _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
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
                _jobExecutionContext.Complete();

                _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
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
                File.WriteAllBytes(stepSummaryFile, new byte[CreateStepSummaryCommand.AttachmentSizeLimit + 1]);

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);
                _jobExecutionContext.Complete();

                _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
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
                TestUtil.WriteContent(stepSummaryFile, content);

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);
                _jobExecutionContext.Complete();

                _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), ChecksAttachmentType.StepSummary, _executionContext.Object.Id.ToString(), stepSummaryFile + "-scrubbed", It.IsAny<bool>()), Times.Once());
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
                TestUtil.WriteContent(stepSummaryFile, content);

                _createStepCommand.ProcessCommand(_executionContext.Object, stepSummaryFile, null);

                var scrubbedFileContents = File.ReadAllText(scrubbedFile);
                Assert.DoesNotContain("ThisIsMySecretPassword!", scrubbedFileContents);
                Assert.DoesNotContain("ghs_verysecuretoken", scrubbedFileContents);
                _jobExecutionContext.Complete();

                _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), ChecksAttachmentType.StepSummary, _executionContext.Object.Id.ToString(), scrubbedFile, It.IsAny<bool>()), Times.Once());
                Assert.Equal(0, _issues.Count);
            }
        }

        private TestHostContext Setup([CallerMemberName] string name = "")
        {
            var hostContext = new TestHostContext(this, name);

            _issues = new List<Tuple<DTWebApi.Issue, string>>();

            // Setup a job request
            TaskOrchestrationPlanReference plan = new();
            TimelineReference timeline = new();
            Guid jobId = Guid.NewGuid();
            string jobName = "Summary Job";
            var jobRequest = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, jobName, jobName, null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null, null, null, null);
            jobRequest.Resources.Repositories.Add(new Pipelines.RepositoryResource()
            {
                Alias = Pipelines.PipelineConstants.SelfAlias,
                Id = "github",
                Version = "sha1"
            });
            jobRequest.ContextData["github"] = new Pipelines.ContextData.DictionaryContextData();
            jobRequest.Variables["ACTIONS_STEP_DEBUG"] = "true";

            // Server queue for job
            _jobServerQueue = new Mock<IJobServerQueue>();
            _jobServerQueue.Setup(x => x.QueueTimelineRecordUpdate(It.IsAny<Guid>(), It.IsAny<TimelineRecord>()));
            hostContext.SetSingleton(_jobServerQueue.Object);

            // Configuration store (required singleton)
            var configurationStore = new Mock<IConfigurationStore>();
            configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings());
            hostContext.SetSingleton(configurationStore.Object);

            // Paging Logger (required singleton)
            var pagingLogger = new Mock<IPagingLogger>();
            hostContext.EnqueueInstance(pagingLogger.Object);

            // Trace
            _trace = hostContext.GetTrace();

            // Variables to test for secret scrubbing & FF options
            _variables = new Variables(hostContext, new Dictionary<string, VariableValue>
                {
                    { "MySecretName", new VariableValue("My secret value", true) },
                });

            // Directory for test data
            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(CreateStepSummaryCommandL0));
            Directory.CreateDirectory(_rootDirectory);

            // Job execution context
            _jobExecutionContext = new ExecutionContext();
            _jobExecutionContext.Initialize(hostContext);
            _jobExecutionContext.InitializeJob(jobRequest, System.Threading.CancellationToken.None);

            // Step execution context
            _executionContext = new Mock<IExecutionContext>();
            _executionContext.Setup(x => x.Global)
                .Returns(new GlobalContext
                {
                    EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                    WriteDebug = true,
                    Variables = _variables,
                });
            _executionContext.Setup(x => x.AddIssue(It.IsAny<DTWebApi.Issue>(), It.IsAny<ExecutionContextLogOptions>()))
                .Callback((DTWebApi.Issue issue, ExecutionContextLogOptions logOptions) =>
                {
                    var resolvedMessage = issue.Message;
                    if (logOptions.WriteToLog && !string.IsNullOrEmpty(logOptions.LogMessageOverride))
                    {
                        resolvedMessage = logOptions.LogMessageOverride;
                    }
                    _issues.Add(new(issue, resolvedMessage));
                    _trace.Info($"Issue '{issue.Type}': {resolvedMessage}");
                });
            _executionContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) =>
                {
                    _trace.Info($"{tag}{message}");
                });
            _executionContext.SetupGet(x => x.Root).Returns(_jobExecutionContext);

            //CreateStepSummaryCommand
            _createStepCommand = new CreateStepSummaryCommand();
            _createStepCommand.Initialize(hostContext);

            return hostContext;
        }
    }
}
