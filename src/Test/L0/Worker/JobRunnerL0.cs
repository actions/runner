using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using System.Collections.ObjectModel;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
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
        private Mock<IVstsAgentWebProxy> _proxyConfig;
        private Mock<IAgentCertificateManager> _cert;
        private Mock<IConfigurationStore> _config;
        private Mock<ITaskServer> _taskServer;
        private Mock<IExtensionManager> _extensions;
        private Mock<IStepsRunner> _stepRunner;

        private Mock<IJobExtension> _jobExtension;
        private Mock<IPagingLogger> _logger;
        private Mock<ITempDirectoryManager> _temp;
        private Mock<IDiagnosticLogManager> _diagnosticLogManager;

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);

            _jobEc = new Agent.Worker.ExecutionContext();
            _config = new Mock<IConfigurationStore>();
            _extensions = new Mock<IExtensionManager>();
            _jobExtension = new Mock<IJobExtension>();
            _jobServer = new Mock<IJobServer>();
            _jobServerQueue = new Mock<IJobServerQueue>();
            _proxyConfig = new Mock<IVstsAgentWebProxy>();
            _cert = new Mock<IAgentCertificateManager>();
            _taskServer = new Mock<ITaskServer>();
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
            var expressionManager = new ExpressionManager();
            expressionManager.Initialize(hc);
            hc.SetSingleton<IExpressionManager>(expressionManager);

            _jobRunner = new JobRunner();
            _jobRunner.Initialize(hc);

            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = new Timeline(Guid.NewGuid());
            JobEnvironment environment = new JobEnvironment();
            environment.Variables[Constants.Variables.System.Culture] = "en-US";
            environment.SystemConnection = new ServiceEndpoint()
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Url = new Uri("https://test.visualstudio.com"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = "Test",
                }
            };
            environment.SystemConnection.Authorization.Parameters["AccessToken"] = "token";

            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            _message = Pipelines.AgentJobRequestMessageUtil.Convert(new AgentJobRequestMessage(plan, timeline, JobId, testName, testName, environment, tasks));

            _extensions.Setup(x => x.GetExtensions<IJobExtension>()).
                Returns(new[] { _jobExtension.Object }.ToList());

            _initResult.Clear();

            _jobExtension.Setup(x => x.InitializeJob(It.IsAny<IExecutionContext>(), It.IsAny<Pipelines.AgentJobRequestMessage>())).
                Returns(Task.FromResult(_initResult));
            _jobExtension.Setup(x => x.HostType)
                .Returns<string>(null);

            _proxyConfig.Setup(x => x.ProxyAddress)
                .Returns(string.Empty);

            var settings = new AgentSettings
            {
                AgentId = 1,
                AgentName = "agent1",
                ServerUrl = "https://test.visualstudio.com",
                WorkFolder = "_work",
            };

            _config.Setup(x => x.GetSettings())
                .Returns(settings);

            _logger.Setup(x => x.Setup(It.IsAny<Guid>(), It.IsAny<Guid>()));

            hc.SetSingleton(_config.Object);
            hc.SetSingleton(_jobServer.Object);
            hc.SetSingleton(_jobServerQueue.Object);
            hc.SetSingleton(_proxyConfig.Object);
            hc.SetSingleton(_cert.Object);
            hc.SetSingleton(_taskServer.Object);
            hc.SetSingleton(_stepRunner.Object);
            hc.SetSingleton(_extensions.Object);
            hc.SetSingleton(_temp.Object);
            hc.SetSingleton(_diagnosticLogManager.Object);
            hc.EnqueueInstance<IExecutionContext>(_jobEc);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
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
                _stepRunner.Verify(x => x.RunAsync(It.IsAny<IExecutionContext>(), It.IsAny<IList<IStep>>()), Times.Never);
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
                _stepRunner.Verify(x => x.RunAsync(It.IsAny<IExecutionContext>(), It.IsAny<IList<IStep>>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task UploadDiganosticLogIfEnvironmentVariableSet()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _message.Variables[Constants.Variables.Agent.Diagnostic] = "true";

                await _jobRunner.RunAsync(_message, _tokenSource.Token);

                _diagnosticLogManager.Verify(x => x.UploadDiagnosticLogsAsync(It.IsAny<IExecutionContext>(),
                                                                         It.IsAny<Pipelines.AgentJobRequestMessage>(),
                                                                         It.IsAny<DateTime>()),
                                             Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DontUploadDiagnosticLogIfEnvironmentVariableFalse()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                _message.Variables[Constants.Variables.Agent.Diagnostic] = "false";

                await _jobRunner.RunAsync(_message, _tokenSource.Token);

                _diagnosticLogManager.Verify(x => x.UploadDiagnosticLogsAsync(It.IsAny<IExecutionContext>(),
                                                                         It.IsAny<Pipelines.AgentJobRequestMessage>(),
                                                                         It.IsAny<DateTime>()),
                                             Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DontUploadDiagnosticLogIfEnvironmentVariableMissing()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                await _jobRunner.RunAsync(_message, _tokenSource.Token);

                _diagnosticLogManager.Verify(x => x.UploadDiagnosticLogsAsync(It.IsAny<IExecutionContext>(),
                                                                         It.IsAny<Pipelines.AgentJobRequestMessage>(),
                                                                         It.IsAny<DateTime>()),
                                             Times.Never);
            }
        }
    }
}
