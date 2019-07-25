﻿using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        private string _workFolder;
        private Dictionary<string, PipelineContextData> _context = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);

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

            Dictionary<string, string> finialInputs = new Dictionary<string, string>();
            _handlerFactory.Setup(x => x.Create(It.IsAny<IExecutionContext>(), It.IsAny<ActionStepDefinitionReference>(), It.IsAny<IStepHost>(), It.IsAny<ActionExecutionData>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Variables>(), It.IsAny<string>()))
                           .Callback((IExecutionContext executionContext, Pipelines.ActionStepDefinitionReference actionReference, IStepHost stepHost, ActionExecutionData data, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, string taskDirectory) =>
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
            Assert.Equal(finialInputs["input1"], "test1");
            Assert.Equal(finialInputs["input2"], "test2");
            Assert.Equal(finialInputs["input3"], "github");
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

            Dictionary<string, string> finialInputs = new Dictionary<string, string>();
            _handlerFactory.Setup(x => x.Create(It.IsAny<IExecutionContext>(), It.IsAny<ActionStepDefinitionReference>(), It.IsAny<IStepHost>(), It.IsAny<ActionExecutionData>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Variables>(), It.IsAny<string>()))
                           .Callback((IExecutionContext executionContext, Pipelines.ActionStepDefinitionReference actionReference, IStepHost stepHost, ActionExecutionData data, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, string taskDirectory) =>
                           {
                               finialInputs = inputs;
                           })
                           .Returns(new Mock<IHandler>().Object);

            //Act
            await _actionRunner.RunAsync();

            //Assert
            _ec.Verify(x => x.SetGitHubContext("event_path", Path.Combine(_hc.GetDirectory(WellKnownDirectory.Temp), "_github_workflow", "event.json")), Times.Once);
        }

        private void CreateAction(string yamlContent, out Pipelines.ActionStep instance, out string directory)
        {
            directory = Path.Combine(_workFolder, Constants.Path.ActionsDirectory, "GitHub/actions".Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "master");
            string file = Path.Combine(directory, Constants.Path.ActionManifestFile);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, yamlContent);
            instance = new Pipelines.ActionStep()
            {
                Id = Guid.NewGuid(),
                Reference = new Pipelines.RepositoryPathReference()
                {
                    Name = "GitHub/actions",
                    Ref = "master",
                    RepositoryType = Pipelines.RepositoryTypes.GitHub
                }
            };
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

            var githubContext = new GitHubContext();
            githubContext.Add("event", JToken.Parse("{\"foo\":\"bar\"}").ToPipelineContextData());
            _context.Add("github", githubContext);

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.ExpressionValues).Returns(_context);
            _ec.Setup(x => x.EnvironmentVariables).Returns(new Dictionary<string, string>());
            _ec.Setup(x => x.SetGitHubContext(It.IsAny<string>(), It.IsAny<string>()));
            _ec.Setup(x => x.GetGitHubContext(It.IsAny<string>())).Returns("{\"foo\":\"bar\"}");
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Setup(x => x.Variables).Returns(new Variables(_hc, new Dictionary<string, VariableValue>()));
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"[{tag}]{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>())).Callback((Issue issue, string message) => { _hc.GetTrace().Info($"[{issue.Type}]{issue.Message ?? message}"); });

            _hc.SetSingleton<IActionManager>(_actionManager.Object);
            _hc.SetSingleton<IHandlerFactory>(_handlerFactory.Object);

            _hc.EnqueueInstance<IDefaultStepHost>(_defaultStepHost.Object);

            // Instance to test.
            _actionRunner = new ActionRunner();
            _actionRunner.Initialize(_hc);
            _actionRunner.ExecutionContext = _ec.Object;
        }
    }
}
