using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Moq;
using Xunit;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ActionCommandManagerL0
    {
        private ActionCommandManager _commandManager;
        private Mock<IExecutionContext> _ec;
        private Mock<IExtensionManager> _extensionManager;
        private Mock<IPipelineDirectoryManager> _pipelineDirectoryManager;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EnablePluginInternalCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });
                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });

                _commandManager.EnablePluginInternalCommand();

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                _pipelineDirectoryManager.Verify(x => x.UpdateRepositoryDirectory(_ec.Object, "actions/runner", "somepath", true), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DisablePluginInternalCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });
                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });

                _commandManager.EnablePluginInternalCommand();

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                _commandManager.DisablePluginInternalCommand();

                Assert.False(_commandManager.TryProcessCommand(_ec.Object, "##[internal-set-repo-path repoFullName=actions/runner;workspaceRepo=true]somepath", null));

                _pipelineDirectoryManager.Verify(x => x.UpdateRepositoryDirectory(_ec.Object, "actions/runner", "somepath", true), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StopProcessCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.ExpressionValues).Returns(GetExpressionValues());
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>()))
                   .Callback((Issue issue, string message) =>
                   {
                       hc.GetTrace().Info($"{issue.Type} {issue.Message} {message ?? string.Empty}");
                   });

                _ec.Object.Global.EnvironmentVariables = new Dictionary<string, string>();

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[stop-commands]stopToken", null));
                Assert.False(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[stopToken]", null));
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
            }
        }

        [Theory]
        [InlineData("stop-commands", "1")]
        [InlineData("", "1")]
        [InlineData("set-env", "1")]
        [InlineData("stop-commands", "true")]
        [InlineData("", "true")]
        [InlineData("set-env", "true")]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StopProcessCommand__AllowsInvalidStopTokens__IfEnvVarIsSet(string invalidToken, string allowUnsupportedStopCommandTokens)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Object.Global.EnvironmentVariables = new Dictionary<string, string>();
                _ec.Object.Global.JobTelemetry = new List<JobTelemetry>();
                var expressionValues = new DictionaryContextData
                {
                    ["env"] =
#if OS_WINDOWS
                        new DictionaryContextData{ { Constants.Variables.Actions.AllowUnsupportedStopCommandTokens, new StringContextData(allowUnsupportedStopCommandTokens) }}
#else
                        new CaseSensitiveDictionaryContextData { { Constants.Variables.Actions.AllowUnsupportedStopCommandTokens, new StringContextData(allowUnsupportedStopCommandTokens) } }
#endif
                };
                _ec.Setup(x => x.ExpressionValues).Returns(expressionValues);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, $"::stop-commands::{invalidToken}", null));
            }
        }

        [Theory]
        [InlineData("stop-commands")]
        [InlineData("")]
        [InlineData("set-env")]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StopProcessCommand__FailOnInvalidStopTokens(string invalidToken)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Object.Global.EnvironmentVariables = new Dictionary<string, string>();
                _ec.Object.Global.JobTelemetry = new List<JobTelemetry>();
                _ec.Setup(x => x.ExpressionValues).Returns(GetExpressionValues());
                Assert.Throws<Exception>(() => _commandManager.TryProcessCommand(_ec.Object, $"::stop-commands::{invalidToken}", null));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StopProcessCommandAcceptsValidToken()
        {
            var validToken = "randomToken";
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.ExpressionValues).Returns(GetExpressionValues());
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, $"::stop-commands::{validToken}", null));
                Assert.False(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, $"::{validToken}::", null));
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StopProcessCommandMasksValidTokenForEntireRun()
        {
            var validToken = "randomToken";
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.ExpressionValues).Returns(GetExpressionValues());
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, $"::stop-commands::{validToken}", null));
                Assert.False(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
                Assert.Equal("***", hc.SecretMasker.MaskSecrets(validToken));

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, $"::{validToken}::", null));
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "##[set-env name=foo]bar", null));
                Assert.Equal("***", hc.SecretMasker.MaskSecrets(validToken));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommand()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                Assert.False(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::on", null));
                Assert.True(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::off", null));
                Assert.False(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::ON", null));
                Assert.True(_ec.Object.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::Off   ", null));
                Assert.False(_ec.Object.EchoOnActionCommand);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommandDebugOn()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Set up a few things
                // 1. Job request message (with ACTIONS_STEP_DEBUG = true)
                TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
                TimelineReference timeline = new TimelineReference();
                Guid jobId = Guid.NewGuid();
                string jobName = "some job name";
                var jobRequest = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, jobName, jobName, null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null, null, null, null);
                jobRequest.Resources.Repositories.Add(new Pipelines.RepositoryResource()
                {
                    Alias = Pipelines.PipelineConstants.SelfAlias,
                    Id = "github",
                    Version = "sha1"
                });
                jobRequest.ContextData["github"] = new Pipelines.ContextData.DictionaryContextData();
                jobRequest.Variables["ACTIONS_STEP_DEBUG"] = "true";

                // Some service dependencies
                var jobServerQueue = new Mock<IJobServerQueue>();
                jobServerQueue.Setup(x => x.QueueTimelineRecordUpdate(It.IsAny<Guid>(), It.IsAny<TimelineRecord>()));

                hc.SetSingleton(jobServerQueue.Object);

                var configurationStore = new Mock<IConfigurationStore>();
                configurationStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings());
                hc.SetSingleton(configurationStore.Object);

                var pagingLogger = new Mock<IPagingLogger>();
                hc.EnqueueInstance(pagingLogger.Object);

                // Initialize the job (to exercise logic that sets EchoOnActionCommand)
                var ec = new Runner.Worker.ExecutionContext();
                ec.Initialize(hc);
                ec.InitializeJob(jobRequest, System.Threading.CancellationToken.None);

                ec.Complete();

                Assert.True(ec.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(ec, "::echo::off", null));
                Assert.False(ec.EchoOnActionCommand);

                Assert.True(_commandManager.TryProcessCommand(ec, "::echo::on", null));
                Assert.True(ec.EchoOnActionCommand);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IssueCommandInvalidColumns()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                var registeredCommands = new HashSet<string>(new string[1] { "warning" });
                ActionCommand command;

                // Columns when lines are different
                ActionCommand.TryParseV2("::warning line=1,endLine=2,col=1,endColumn=2::this is a warning", registeredCommands, out command);
                Assert.Equal("1", command.Properties["col"]);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.False(command.Properties.ContainsKey("col"));

                // No lines with columns
                ActionCommand.TryParseV2("::warning col=1,endColumn=2::this is a warning", registeredCommands, out command);
                Assert.Equal("1", command.Properties["col"]);
                Assert.Equal("2", command.Properties["endColumn"]);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.False(command.Properties.ContainsKey("col"));
                Assert.False(command.Properties.ContainsKey("endColumn"));

                // No line with endLine
                ActionCommand.TryParseV2("::warning endLine=1::this is a warning", registeredCommands, out command);
                Assert.Equal("1", command.Properties["endLine"]);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.Equal(command.Properties["endLine"], command.Properties["line"]);

                // No column with endColumn
                ActionCommand.TryParseV2("::warning line=1,endColumn=2::this is a warning", registeredCommands, out command);
                Assert.Equal("2", command.Properties["endColumn"]);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.Equal(command.Properties["endColumn"], command.Properties["col"]);

                // Empty Strings
                ActionCommand.TryParseV2("::warning line=,endLine=3::this is a warning", registeredCommands, out command);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.Equal(command.Properties["line"], command.Properties["endLine"]);

                // Nonsensical line values
                ActionCommand.TryParseV2("::warning line=4,endLine=3::this is a warning", registeredCommands, out command);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.False(command.Properties.ContainsKey("line"));
                Assert.False(command.Properties.ContainsKey("endLine"));

                /// Nonsensical column values
                ActionCommand.TryParseV2("::warning line=1,endLine=1,col=3,endColumn=2::this is a warning", registeredCommands, out command);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.False(command.Properties.ContainsKey("col"));
                Assert.False(command.Properties.ContainsKey("endColumn"));

                // Valid
                ActionCommand.TryParseV2("::warning line=1,endLine=1,col=1,endColumn=2::this is a warning", registeredCommands, out command);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.Equal("1", command.Properties["line"]);
                Assert.Equal("1", command.Properties["endLine"]);
                Assert.Equal("1", command.Properties["col"]);
                Assert.Equal("2", command.Properties["endColumn"]);

                // Backwards compatibility
                ActionCommand.TryParseV2("::warning line=1,col=1,file=test.txt::this is a warning", registeredCommands, out command);
                IssueCommandExtension.ValidateLinesAndColumns(command, _ec.Object);
                Assert.Equal("1", command.Properties["line"]);
                Assert.False(command.Properties.ContainsKey("endLine"));
                Assert.Equal("1", command.Properties["col"]);
                Assert.False(command.Properties.ContainsKey("endColumn"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EchoProcessCommandInvalid()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string tag, string line) =>
                            {
                                hc.GetTrace().Info($"{tag} {line}");
                                return 1;
                            });

                // Echo commands below are considered "processed", but are invalid
                // 1. Invalid echo value
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::invalid", null));
                Assert.Equal(TaskResult.Failed, _ec.Object.CommandResult);
                Assert.False(_ec.Object.EchoOnActionCommand);

                // 2. No value
                Assert.True(_commandManager.TryProcessCommand(_ec.Object, "::echo::", null));
                Assert.Equal(TaskResult.Failed, _ec.Object.CommandResult);
                Assert.False(_ec.Object.EchoOnActionCommand);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AddMatcherTranslatesFilePath()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Create a problem matcher config file
                var hostDirectory = hc.GetDirectory(WellKnownDirectory.Temp);
                var hostFile = Path.Combine(hostDirectory, "my-matcher.json");
                Directory.CreateDirectory(hostDirectory);
                var content = @"
{
    ""problemMatcher"": [
        {
            ""owner"": ""my-matcher"",
            ""pattern"": [
                {
                    ""regexp"": ""^ERROR: (.+)$"",
                    ""message"": 1
                }
            ]
        }
    ]
}";
                File.WriteAllText(hostFile, content);

                // Setup translation info
                var container = new ContainerInfo();
                var containerDirectory = "/some-container-directory";
                var containerFile = Path.Combine(containerDirectory, "my-matcher.json");
                container.AddPathTranslateMapping(hostDirectory, containerDirectory);

                // Act
                _commandManager.TryProcessCommand(_ec.Object, $"::add-matcher::{containerFile}", container);

                // Assert
                _ec.Verify(x => x.AddMatchers(It.IsAny<IssueMatchersConfig>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AddMaskWithMultilineValue()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Act
                _commandManager.TryProcessCommand(_ec.Object, $"::add-mask::abc%0Ddef%0Aghi%0D%0Ajkl", null);
                _commandManager.TryProcessCommand(_ec.Object, $"::add-mask:: %0D  %0A   %0D%0A    %0D", null);

                // Assert
                Assert.Equal("***", hc.SecretMasker.MaskSecrets("abc"));
                Assert.Equal("***", hc.SecretMasker.MaskSecrets("def"));
                Assert.Equal("***", hc.SecretMasker.MaskSecrets("ghi"));
                Assert.Equal("***", hc.SecretMasker.MaskSecrets("jkl"));
                Assert.Equal("***", hc.SecretMasker.MaskSecrets("abc\rdef\nghi\r\njkl"));
                Assert.Equal("", hc.SecretMasker.MaskSecrets(""));
                Assert.Equal(" ", hc.SecretMasker.MaskSecrets(" "));
                Assert.Equal("  ", hc.SecretMasker.MaskSecrets("  "));
                Assert.Equal("   ", hc.SecretMasker.MaskSecrets("   "));
                Assert.Equal("    ", hc.SecretMasker.MaskSecrets("    "));
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hostContext = new TestHostContext(this, testName);

            // Mock extension manager
            _extensionManager = new Mock<IExtensionManager>();
            var commands = new IActionCommandExtension[]
            {
                new AddMatcherCommandExtension(),
                new EchoCommandExtension(),
                new InternalPluginSetRepoPathCommandExtension(),
                new SetEnvCommandExtension(),
                new WarningCommandExtension(),
                new AddMaskCommandExtension(),
            };
            foreach (var command in commands)
            {
                command.Initialize(hostContext);
            }
            _extensionManager.Setup(x => x.GetExtensions<IActionCommandExtension>())
                .Returns(new List<IActionCommandExtension>(commands));
            hostContext.SetSingleton<IExtensionManager>(_extensionManager.Object);

            // Mock pipeline directory manager
            _pipelineDirectoryManager = new Mock<IPipelineDirectoryManager>();
            hostContext.SetSingleton<IPipelineDirectoryManager>(_pipelineDirectoryManager.Object);

            // Execution context
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.Global).Returns(new GlobalContext());
            _ec.Object.Global.Variables = new Variables(
                hostContext,
                new Dictionary<string, VariableValue>()
            );

            // Command manager
            _commandManager = new ActionCommandManager();
            _commandManager.Initialize(hostContext);

            return hostContext;
        }

        private DictionaryContextData GetExpressionValues()
        {
            return new DictionaryContextData
            {
                ["env"] =
#if OS_WINDOWS
                        new DictionaryContextData()
#else
                        new CaseSensitiveDictionaryContextData()
#endif
            };
        }

    }
}
