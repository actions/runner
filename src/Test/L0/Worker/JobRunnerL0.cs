using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using System.Collections.ObjectModel;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class JobRunnerL0
    {
        private IExecutionContext _jobEc;
        private JobRunner _jobRunner;
        private List<IStep> _initResult = new List<IStep>();
        private Pipelines.AgentJobRequestMessage _message;
        private CancellationTokenSource _tokenSource;
        private Mock<IJobServer> _jobServer;
        private Mock<IJobServerQueue> _jobServerQueue;
        private Mock<IConfigurationStore> _config;
        private Mock<IExtensionManager> _extensions;
        private Mock<IStepsRunner> _stepRunner;

        private Mock<IJobExtension> _jobExtension;
        private Mock<IPagingLogger> _logger;
        private Mock<ITempDirectoryManager> _temp;
        private Mock<IDiagnosticLogManager> _diagnosticLogManager;

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);

            _jobEc = new Runner.Worker.ExecutionContext();
            _config = new Mock<IConfigurationStore>();
            _extensions = new Mock<IExtensionManager>();
            _jobExtension = new Mock<IJobExtension>();
            _jobServer = new Mock<IJobServer>();
            _jobServerQueue = new Mock<IJobServerQueue>();
            _stepRunner = new Mock<IStepsRunner>();
            _logger = new Mock<IPagingLogger>();
            _temp = new Mock<ITempDirectoryManager>();
            _diagnosticLogManager = new Mock<IDiagnosticLogManager>();

            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }

            _tokenSource = new CancellationTokenSource();

            _jobRunner = new JobRunner();
            _jobRunner.Initialize(hc);

            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = new Timeline(Guid.NewGuid());
            Guid jobId = Guid.NewGuid();
            _message = new Pipelines.AgentJobRequestMessage(plan, timeline, jobId, testName, testName, null, null, null, new Dictionary<string, VariableValue>(), new List<MaskHint>(), new Pipelines.JobResources(), new Pipelines.ContextData.DictionaryContextData(), new Pipelines.WorkspaceOptions(), new List<Pipelines.ActionStep>(), null, null, null, null);
            _message.Variables[Constants.Variables.System.Culture] = "en-US";
            _message.Resources.Endpoints.Add(new ServiceEndpoint()
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Url = new Uri("https://pipelines.actions.githubusercontent.com"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = "Test",
                    Parameters = {
                        {"AccessToken", "token"}
                    }
                },

            });

            _message.Resources.Repositories.Add(new Pipelines.RepositoryResource()
            {
                Alias = Pipelines.PipelineConstants.SelfAlias,
                Id = "github",
                Version = "sha1"
            });
            _message.ContextData.Add("github", new Pipelines.ContextData.DictionaryContextData());

            _initResult.Clear();

            _jobExtension.Setup(x => x.InitializeJob(It.IsAny<IExecutionContext>(), It.IsAny<Pipelines.AgentJobRequestMessage>())).
                Returns(Task.FromResult(_initResult));

            var settings = new RunnerSettings
            {
                AgentId = 1,
                AgentName = "agent1",
                ServerUrl = "https://pipelines.actions.githubusercontent.com",
                WorkFolder = "_work",
            };

            _config.Setup(x => x.GetSettings())
                .Returns(settings);

            _logger.Setup(x => x.Setup(It.IsAny<Guid>(), It.IsAny<Guid>()));

            hc.SetSingleton(_config.Object);
            hc.SetSingleton(_jobServer.Object);
            hc.SetSingleton(_jobServerQueue.Object);
            hc.SetSingleton(_stepRunner.Object);
            hc.SetSingleton(_extensions.Object);
            hc.SetSingleton(_temp.Object);
            hc.SetSingleton(_diagnosticLogManager.Object);
            hc.EnqueueInstance<IExecutionContext>(_jobEc);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IJobExtension>(_jobExtension.Object);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionInitializeFailure()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _jobExtension.Setup(x => x.InitializeJob(It.IsAny<IExecutionContext>(), It.IsAny<Pipelines.AgentJobRequestMessage>()))
                    .Throws(new Exception());

                await _jobRunner.RunAsync(_message, _tokenSource.Token);

                Assert.Equal(TaskResult.Failed, _jobEc.Result);
                _stepRunner.Verify(x => x.RunAsync(It.IsAny<IExecutionContext>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionInitializeCancelled()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _jobExtension.Setup(x => x.InitializeJob(It.IsAny<IExecutionContext>(), It.IsAny<Pipelines.AgentJobRequestMessage>()))
                    .Throws(new OperationCanceledException());
                _tokenSource.Cancel();

                await _jobRunner.RunAsync(_message, _tokenSource.Token);

                Assert.Equal(TaskResult.Canceled, _jobEc.Result);
                _stepRunner.Verify(x => x.RunAsync(It.IsAny<IExecutionContext>()), Times.Never);
            }
        }
    }
}
