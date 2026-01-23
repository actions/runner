using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ActionRunnerL0
    {
        private CancellationTokenSource _ecTokenSource;
        private Mock<IHandlerFactory> _handlerFactory;
        private Mock<IActionManager> _actionManager;
        private Mock<IDefaultStepHost> _defaultStepHost;
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private ActionRunner _actionRunner;
        private IActionManifestManagerWrapper _actionManifestManager;
        private Mock<IFileCommandManager> _fileCommandManager;

        private DictionaryContextData _context = new();

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void MergeDefaultInputs()
        {
            //Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var actionInputs = new MappingToken(null, null, null);
            actionInputs.Add(new StringToken(null, null, null, "input1"), new StringToken(null, null, null, "test1"));
            actionInputs.Add(new StringToken(null, null, null, "input2"), new StringToken(null, null, null, "test2"));
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                Reference = new Pipelines.ContainerRegistryReference()
                {
                    Image = "ubuntu:16.04"
                },
                Inputs = actionInputs
            };

            _actionRunner.Action = action;

            Dictionary<string, string> finialInputs = new();
            _handlerFactory.Setup(x => x.Create(It.IsAny<IExecutionContext>(), It.IsAny<ActionStepDefinitionReference>(), It.IsAny<IStepHost>(), It.IsAny<ActionExecutionData>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Variables>(), It.IsAny<string>(), It.IsAny<List<JobExtensionRunner>>()))
                           .Callback((IExecutionContext executionContext, Pipelines.ActionStepDefinitionReference actionReference, IStepHost stepHost, ActionExecutionData data, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, string taskDirectory, List<JobExtensionRunner> localActionContainerSetupSteps) =>
                           {
                               finialInputs = inputs;
                           })
                           .Returns(new Mock<IHandler>().Object);

            //Act
            await _actionRunner.RunAsync();

            foreach (var input in finialInputs)
            {
                _hc.GetTrace().Info($"Input: {input.Key}={input.Value}");
            }

            //Assert
            Assert.Equal("test1", finialInputs["input1"]);
            Assert.Equal("test2", finialInputs["input2"]);
            Assert.Equal("github", finialInputs["input3"]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void WriteEventPayload()
        {
            //Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var actionInputs = new MappingToken(null, null, null);
            actionInputs.Add(new StringToken(null, null, null, "input1"), new StringToken(null, null, null, "test1"));
            actionInputs.Add(new StringToken(null, null, null, "input2"), new StringToken(null, null, null, "test2"));
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                Reference = new Pipelines.ContainerRegistryReference()
                {
                    Image = "ubuntu:16.04"
                },
                Inputs = actionInputs
            };

            _actionRunner.Action = action;

            Dictionary<string, string> finialInputs = new();
            _handlerFactory.Setup(x => x.Create(It.IsAny<IExecutionContext>(), It.IsAny<ActionStepDefinitionReference>(), It.IsAny<IStepHost>(), It.IsAny<ActionExecutionData>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Variables>(), It.IsAny<string>(), It.IsAny<List<JobExtensionRunner>>()))
                           .Callback((IExecutionContext executionContext, Pipelines.ActionStepDefinitionReference actionReference, IStepHost stepHost, ActionExecutionData data, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, string taskDirectory, List<JobExtensionRunner> localActionContainerSetupSteps) =>
                           {
                               finialInputs = inputs;
                           })
                           .Returns(new Mock<IHandler>().Object);

            //Act
            await _actionRunner.RunAsync();

            //Assert
            _ec.Verify(x => x.WriteWebhookPayload(), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateLegacyDisplayName()
        {
            // Arrange
            Setup();
            var actionInputs = new MappingToken(null, null, null);
            actionInputs.Add(new StringToken(null, null, null, "script"), new StringToken(null, null, null, "echo hello world"));
            var actionId = Guid.NewGuid();
            var actionDisplayName = "Run echo hello world";
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                DisplayName = actionDisplayName,
                Inputs = actionInputs,
            };

            _actionRunner.Action = action;

            var matrixData = new DictionaryContextData
            {
                ["node"] = new NumberContextData(8)
            };
            _context.Add("matrix", matrixData);

            // Act
            // Should report success with no updated required if there's already a valid display name.
            var validDisplayName = _actionRunner.EvaluateDisplayName(_context, _actionRunner.ExecutionContext, out bool updated);

            // Assert
            Assert.True(validDisplayName);
            Assert.False(updated);
            Assert.Equal(actionDisplayName, _actionRunner.DisplayName);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpansionOfDisplayNameToken()
        {
            // Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                DisplayNameToken = new BasicExpressionToken(null, null, null, "matrix.node"),
            };

            _actionRunner.Action = action;
            var expectedString = "8";

            var matrixData = new DictionaryContextData
            {
                ["node"] = new StringContextData(expectedString)
            };
            _context.Add("matrix", matrixData);

            // Act
            // Should expand the displaynameToken and set the display name to that
            var validDisplayName = _actionRunner.EvaluateDisplayName(_context, _actionRunner.ExecutionContext, out bool updated);

            // Assert
            Assert.True(validDisplayName);
            Assert.True(updated);
            Assert.Equal(expectedString, _actionRunner.DisplayName);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IgnoreDisplayNameTokenWhenDisplayNameIsExplicitlySet()
        {
            var explicitDisplayName = "Explicitly Set Name";

            // Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                DisplayName = explicitDisplayName,
                DisplayNameToken = new BasicExpressionToken(null, null, null, "matrix.node"),
            };

            _actionRunner.Action = action;

            var matrixData = new DictionaryContextData
            {
                ["node"] = new StringContextData("8")
            };
            _context.Add("matrix", matrixData);

            // Act
            // Should ignore the displayNameToken since there's already an explicit value for DisplayName
            var validDisplayName = _actionRunner.EvaluateDisplayName(_context, _actionRunner.ExecutionContext, out bool updated);

            // Assert
            Assert.True(validDisplayName);
            Assert.False(updated);
            Assert.Equal(explicitDisplayName, _actionRunner.DisplayName);
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpansionOfScriptDisplayName()
        {
            // Arrange
            Setup();
            var actionInputs = new MappingToken(null, null, null);
            actionInputs.Add(new StringToken(null, null, null, "script"), new BasicExpressionToken(null, null, null, "matrix.node"));
            var actionId = Guid.NewGuid();
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                Inputs = actionInputs,
                Reference = new Pipelines.ScriptReference()
            };

            _actionRunner.Action = action;

            var matrixData = new DictionaryContextData
            {
                ["node"] = new StringContextData("8")
            };
            _context.Add("matrix", matrixData);

            // Act
            // Should expand the displaynameToken and set the display name to that
            var validDisplayName = _actionRunner.EvaluateDisplayName(_context, _actionRunner.ExecutionContext, out bool updated);

            // Assert
            Assert.True(validDisplayName);
            Assert.True(updated);
            Assert.Equal("Run 8", _actionRunner.DisplayName);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpansionOfContainerDisplayName()
        {
            // Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                Reference = new Pipelines.ContainerRegistryReference()
                {
                    Image = "TestImageName:latest"
                }
            };
            _actionRunner.Action = action;

            // Act
            // Should expand the displaynameToken and set the display name to that
            var validDisplayName = _actionRunner.EvaluateDisplayName(_context, _actionRunner.ExecutionContext, out bool updated);

            // Assert
            Assert.True(validDisplayName);
            Assert.True(updated);
            Assert.Equal("Run TestImageName:latest", _actionRunner.DisplayName);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateDisplayNameWithoutContext()
        {
            // Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                DisplayNameToken = new BasicExpressionToken(null, null, null, "matrix.node"),
            };

            _actionRunner.Action = action;

            // Act
            // Should not do anything if we don't have context on the display name
            var validDisplayName = _actionRunner.EvaluateDisplayName(_context, _actionRunner.ExecutionContext, out bool updated);

            // Assert
            Assert.False(validDisplayName);
            Assert.False(updated);
            // Should use the pretty display name until we can eval
            Assert.Equal("${{ matrix.node }}", _actionRunner.DisplayName);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void WarnInvalidInputs()
        {
            //Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var actionInputs = new MappingToken(null, null, null);
            actionInputs.Add(new StringToken(null, null, null, "input1"), new StringToken(null, null, null, "test1"));
            actionInputs.Add(new StringToken(null, null, null, "input2"), new StringToken(null, null, null, "test2"));
            actionInputs.Add(new StringToken(null, null, null, "invalid1"), new StringToken(null, null, null, "invalid1"));
            actionInputs.Add(new StringToken(null, null, null, "invalid2"), new StringToken(null, null, null, "invalid2"));
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                Reference = new Pipelines.RepositoryPathReference()
                {
                    Name = "actions/runner",
                    Ref = "v1"
                },
                Inputs = actionInputs
            };

            _actionRunner.Action = action;

            Dictionary<string, string> finialInputs = new();
            _handlerFactory.Setup(x => x.Create(It.IsAny<IExecutionContext>(), It.IsAny<ActionStepDefinitionReference>(), It.IsAny<IStepHost>(), It.IsAny<ActionExecutionData>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Variables>(), It.IsAny<string>(), It.IsAny<List<JobExtensionRunner>>()))
                           .Callback((IExecutionContext executionContext, Pipelines.ActionStepDefinitionReference actionReference, IStepHost stepHost, ActionExecutionData data, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, string taskDirectory, List<JobExtensionRunner> localActionContainerSetupSteps) =>
                           {
                               finialInputs = inputs;
                           })
                           .Returns(new Mock<IHandler>().Object);

            //Act
            await _actionRunner.RunAsync();

            foreach (var input in finialInputs)
            {
                _hc.GetTrace().Info($"Input: {input.Key}={input.Value}");
            }

            //Assert
            Assert.Equal("test1", finialInputs["input1"]);
            Assert.Equal("test2", finialInputs["input2"]);
            Assert.Equal("github", finialInputs["input3"]);
            Assert.Equal("invalid1", finialInputs["invalid1"]);
            Assert.Equal("invalid2", finialInputs["invalid2"]);

            _ec.Verify(x => x.AddIssue(It.Is<Issue>(s => s.Message.Contains("Unexpected input(s) 'invalid1', 'invalid2'")), It.IsAny<ExecutionContextLogOptions>()), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void SetGitHubContextActionRepoRef()
        {
            //Arrange
            Setup();
            var actionId = Guid.NewGuid();
            var actionInputs = new MappingToken(null, null, null);
            actionInputs.Add(new StringToken(null, null, null, "input1"), new StringToken(null, null, null, "test1"));
            actionInputs.Add(new StringToken(null, null, null, "input2"), new StringToken(null, null, null, "test2"));
            var action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                Reference = new Pipelines.RepositoryPathReference()
                {
                    Name = "actions/test",
                    Ref = "master"
                },
                Inputs = actionInputs
            };

            _actionRunner.Action = action;

            Dictionary<string, string> finialInputs = new();
            _handlerFactory.Setup(x => x.Create(It.IsAny<IExecutionContext>(), It.IsAny<ActionStepDefinitionReference>(), It.IsAny<IStepHost>(), It.IsAny<ActionExecutionData>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Variables>(), It.IsAny<string>(), It.IsAny<List<JobExtensionRunner>>()))
                           .Callback((IExecutionContext executionContext, Pipelines.ActionStepDefinitionReference actionReference, IStepHost stepHost, ActionExecutionData data, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, string taskDirectory, List<JobExtensionRunner> localActionContainerSetupSteps) =>
                           {
                               finialInputs = inputs;
                           })
                           .Returns(new Mock<IHandler>().Object);

            //Act
            await _actionRunner.RunAsync();

            //Assert
            _ec.Verify(x => x.SetGitHubContext("action_repository", "actions/test"), Times.Once);
            _ec.Verify(x => x.SetGitHubContext("action_ref", "master"), Times.Once);

            action = new Pipelines.ActionStep()
            {
                Name = "action",
                Id = actionId,
                Reference = new Pipelines.ScriptReference(),
                Inputs = actionInputs
            };
            _actionRunner.Action = action;

            _hc.EnqueueInstance<IDefaultStepHost>(_defaultStepHost.Object);
            _hc.EnqueueInstance(_fileCommandManager.Object);

            //Act
            await _actionRunner.RunAsync();

            //Assert
            _ec.Verify(x => x.SetGitHubContext("action_repository", null), Times.Once);
            _ec.Verify(x => x.SetGitHubContext("action_ref", null), Times.Once);
        }

        private void Setup([CallerMemberName] string name = "")
        {
            _ecTokenSource?.Dispose();
            _ecTokenSource = new CancellationTokenSource();

            // Test host context.
            _hc = new TestHostContext(this, name);

            var actionInputs = new MappingToken(null, null, null);
            actionInputs.Add(new StringToken(null, null, null, "input1"), new StringToken(null, null, null, "input1"));
            actionInputs.Add(new StringToken(null, null, null, "input2"), new StringToken(null, null, null, ""));
            actionInputs.Add(new StringToken(null, null, null, "input3"), new StringToken(null, null, null, "github"));
            var actionDefinition = new Definition()
            {
                Directory = _hc.GetDirectory(WellKnownDirectory.Work),
                Data = new ActionDefinitionData()
                {
                    Name = name,
                    Description = name,
                    Inputs = actionInputs,
                    Execution = new ScriptActionExecutionData()
                }
            };

            // Mocks.
            _actionManager = new Mock<IActionManager>();
            _actionManager.Setup(x => x.LoadAction(It.IsAny<IExecutionContext>(), It.IsAny<ActionStep>())).Returns(actionDefinition);

            _handlerFactory = new Mock<IHandlerFactory>();
            _defaultStepHost = new Mock<IDefaultStepHost>();
            
            var actionManifestLegacy = new ActionManifestManagerLegacy();
            actionManifestLegacy.Initialize(_hc);
            _hc.SetSingleton<IActionManifestManagerLegacy>(actionManifestLegacy);
            var actionManifestNew = new ActionManifestManager();
            actionManifestNew.Initialize(_hc);
            _hc.SetSingleton<IActionManifestManager>(actionManifestNew);
            _actionManifestManager = new ActionManifestManagerWrapper();
            _actionManifestManager.Initialize(_hc);
            _fileCommandManager = new Mock<IFileCommandManager>();

            var githubContext = new GitHubContext();
            githubContext.Add("event", JToken.Parse("{\"foo\":\"bar\"}").ToPipelineContextData());
            _context.Add("github", githubContext);

#if OS_WINDOWS
            _context["env"] = new DictionaryContextData();
#else
            _context["env"] = new CaseSensitiveDictionaryContextData();
#endif

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Global).Returns(new GlobalContext());
            _ec.Setup(x => x.ExpressionValues).Returns(_context);
            _ec.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            _ec.Setup(x => x.IntraActionState).Returns(new Dictionary<string, string>());
            _ec.Object.Global.EnvironmentVariables = new Dictionary<string, string>();
            _ec.Object.Global.FileTable = new List<String>();
            _ec.Setup(x => x.SetGitHubContext(It.IsAny<string>(), It.IsAny<string>()));
            _ec.Setup(x => x.GetGitHubContext(It.IsAny<string>())).Returns("{\"foo\":\"bar\"}");
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Object.Global.Variables = new Variables(_hc, new Dictionary<string, VariableValue>());
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"[{tag}]{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<ExecutionContextLogOptions>())).Callback((Issue issue, ExecutionContextLogOptions logOptions) => { _hc.GetTrace().Info($"[{issue.Type}]{logOptions.LogMessageOverride ?? issue.Message}"); });

            _hc.SetSingleton<IActionManager>(_actionManager.Object);
            _hc.SetSingleton<IHandlerFactory>(_handlerFactory.Object);
            _hc.SetSingleton<IActionManifestManagerWrapper>(_actionManifestManager);

            _hc.EnqueueInstance<IDefaultStepHost>(_defaultStepHost.Object);

            _hc.EnqueueInstance(_fileCommandManager.Object);

            // Instance to test.
            _actionRunner = new ActionRunner();
            _actionRunner.Initialize(_hc);
            _actionRunner.ExecutionContext = _ec.Object;
        }
    }
}
