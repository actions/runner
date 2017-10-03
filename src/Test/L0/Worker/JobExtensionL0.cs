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

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class JobExtensionL0
    {
        private class TestJobExtension : JobExtension
        {
            public override HostTypes HostType => HostTypes.None;

            public override Type ExtensionType => typeof(IJobExtension);

            public override void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath)
            {
                repoName = "";
                sourcePath = "";
            }

            public override IStep GetExtensionPostJobStep(IExecutionContext jobContext)
            {
                return null;
            }

            public override IStep GetExtensionPreJobStep(IExecutionContext jobContext)
            {
                return null;
            }

            public override string GetRootedPath(IExecutionContext context, string path)
            {
                return path;
            }

            public override void InitializeJobExtension(IExecutionContext context)
            {
                return;
            }
        }

        private IExecutionContext _jobEc;
        private JobInitializeResult _initResult = new JobInitializeResult();
        private AgentJobRequestMessage _message;
        private Mock<ITaskManager> _taskManager;

        private Mock<IJobServerQueue> _jobServerQueue;
        private Mock<ISecretMasker> _secretMasker;
        private Mock<IVstsAgentWebProxy> _proxy;
        private Mock<IAgentCertificateManager> _cert;
        private Mock<IConfigurationStore> _config;
        private Mock<IPagingLogger> _logger;
        private Mock<IExpressionManager> _express;
        private Mock<IContainerOperationProvider> _containerProvider;
        private CancellationTokenSource _tokenSource;
        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _jobEc = new Agent.Worker.ExecutionContext();
            _taskManager = new Mock<ITaskManager>();
            _jobServerQueue = new Mock<IJobServerQueue>();
            _secretMasker = new Mock<ISecretMasker>();
            _config = new Mock<IConfigurationStore>();
            _logger = new Mock<IPagingLogger>();
            _proxy = new Mock<IVstsAgentWebProxy>();
            _cert = new Mock<IAgentCertificateManager>();
            _express = new Mock<IExpressionManager>();
            _containerProvider = new Mock<IContainerOperationProvider>();

            TaskRunner step1 = new TaskRunner();
            TaskRunner step2 = new TaskRunner();
            TaskRunner step3 = new TaskRunner();
            TaskRunner step4 = new TaskRunner();
            TaskRunner step5 = new TaskRunner();
            TaskRunner step6 = new TaskRunner();
            TaskRunner step7 = new TaskRunner();
            TaskRunner step8 = new TaskRunner();
            TaskRunner step9 = new TaskRunner();
            TaskRunner step10 = new TaskRunner();
            TaskRunner step11 = new TaskRunner();
            TaskRunner step12 = new TaskRunner();

            _logger.Setup(x => x.Setup(It.IsAny<Guid>(), It.IsAny<Guid>()));
            var settings = new AgentSettings
            {
                AgentId = 1,
                AgentName = "agent1",
                ServerUrl = "https://test.visualstudio.com",
                WorkFolder = "_work",
            };

            _config.Setup(x => x.GetSettings())
                .Returns(settings);

            _proxy.Setup(x => x.ProxyAddress)
                            .Returns(string.Empty);

            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }

            _tokenSource = new CancellationTokenSource();
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = new Timeline(Guid.NewGuid());
            JobEnvironment environment = new JobEnvironment();
            environment.Variables[Constants.Variables.System.Culture] = "en-US";
            environment.SystemConnection = new ServiceEndpoint()
            {
                Url = new Uri("https://test.visualstudio.com"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = "Test",
                }
            };
            environment.SystemConnection.Authorization.Parameters["AccessToken"] = "token";

            List<TaskInstance> tasks = new List<TaskInstance>()
            {
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task1",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task2",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task3",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task4",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task5",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task6",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task7",
                },
            };

            Guid JobId = Guid.NewGuid();
            _message = new AgentJobRequestMessage(plan, timeline, JobId, testName, testName, environment, tasks);

            _initResult.PreJobSteps.Clear();
            _initResult.JobSteps.Clear();
            _initResult.PostJobStep.Clear();

            _taskManager.Setup(x => x.DownloadAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<TaskInstance>>()))
                .Returns(Task.CompletedTask);

            _taskManager.Setup(x => x.Load(It.Is<TaskInstance>(t => t.DisplayName == "task1")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = new ExecutionData(),
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<TaskInstance>(t => t.DisplayName == "task2")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = new ExecutionData(),
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<TaskInstance>(t => t.DisplayName == "task3")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = null,
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<TaskInstance>(t => t.DisplayName == "task4")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = null,
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<TaskInstance>(t => t.DisplayName == "task5")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = null,
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<TaskInstance>(t => t.DisplayName == "task6")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = new ExecutionData(),
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<TaskInstance>(t => t.DisplayName == "task7")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = new ExecutionData(),
                        PostJobExecution = new ExecutionData(),
                    },
                });

            hc.SetSingleton(_taskManager.Object);
            hc.SetSingleton(_config.Object);
            hc.SetSingleton(_secretMasker.Object);
            hc.SetSingleton(_jobServerQueue.Object);
            hc.SetSingleton(_proxy.Object);
            hc.SetSingleton(_cert.Object);
            hc.SetSingleton(_express.Object);
            hc.SetSingleton(_containerProvider.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // jobcontext logger
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // init step logger
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step 1
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step 12

            hc.EnqueueInstance<ITaskRunner>(step1);
            hc.EnqueueInstance<ITaskRunner>(step2);
            hc.EnqueueInstance<ITaskRunner>(step3);
            hc.EnqueueInstance<ITaskRunner>(step4);
            hc.EnqueueInstance<ITaskRunner>(step5);
            hc.EnqueueInstance<ITaskRunner>(step6);
            hc.EnqueueInstance<ITaskRunner>(step7);
            hc.EnqueueInstance<ITaskRunner>(step8);
            hc.EnqueueInstance<ITaskRunner>(step9);
            hc.EnqueueInstance<ITaskRunner>(step10);
            hc.EnqueueInstance<ITaskRunner>(step11);
            hc.EnqueueInstance<ITaskRunner>(step12);

            _jobEc.Initialize(hc);
            _jobEc.InitializeJob(_message, _tokenSource.Token);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensioBuildStepsList()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                TestJobExtension testExtension = new TestJobExtension();
                testExtension.Initialize(hc);
                JobInitializeResult result = await testExtension.InitializeJob(_jobEc, _message);

                var trace = hc.GetTrace();

                trace.Info(string.Join(", ", result.PreJobSteps.Select(x => x.DisplayName)));
                trace.Info(string.Join(", ", result.JobSteps.Select(x => x.DisplayName)));
                trace.Info(string.Join(", ", result.PostJobStep.Select(x => x.DisplayName)));

                Assert.Equal(4, result.PreJobSteps.Count);
                Assert.Equal(4, result.JobSteps.Count);
                Assert.Equal(4, result.PostJobStep.Count);

                Assert.Equal("task2", result.PreJobSteps[0].DisplayName);
                Assert.Equal("task3", result.PreJobSteps[1].DisplayName);
                Assert.Equal("task4", result.PreJobSteps[2].DisplayName);
                Assert.Equal("task6", result.PreJobSteps[3].DisplayName);
                Assert.Equal("task1", result.JobSteps[0].DisplayName);
                Assert.Equal("task2", result.JobSteps[1].DisplayName);
                Assert.Equal("task6", result.JobSteps[2].DisplayName);
                Assert.Equal("task7", result.JobSteps[3].DisplayName);
                Assert.Equal("task7", result.PostJobStep[0].DisplayName);
                Assert.Equal("task5", result.PostJobStep[1].DisplayName);
                Assert.Equal("task3", result.PostJobStep[2].DisplayName);
                Assert.Equal("task2", result.PostJobStep[3].DisplayName);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionIntraTaskState()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                TestJobExtension testExtension = new TestJobExtension();
                testExtension.Initialize(hc);
                JobInitializeResult result = await testExtension.InitializeJob(_jobEc, _message);

                var trace = hc.GetTrace();

                trace.Info(string.Join(", ", result.PreJobSteps.Select(x => x.DisplayName)));
                trace.Info(string.Join(", ", result.JobSteps.Select(x => x.DisplayName)));
                trace.Info(string.Join(", ", result.PostJobStep.Select(x => x.DisplayName)));

                Assert.Equal(4, result.PreJobSteps.Count);
                Assert.Equal(4, result.JobSteps.Count);
                Assert.Equal(4, result.PostJobStep.Count);

                result.PreJobSteps[0].ExecutionContext.TaskVariables.Set("state1", "value1", false);
                Assert.Equal("value1", result.JobSteps[1].ExecutionContext.TaskVariables.Get("state1"));
                Assert.Equal("value1", result.PostJobStep[3].ExecutionContext.TaskVariables.Get("state1"));

                Assert.Null(result.JobSteps[0].ExecutionContext.TaskVariables.Get("state1"));
                Assert.Null(result.PreJobSteps[1].ExecutionContext.TaskVariables.Get("state1"));
                Assert.Null(result.PreJobSteps[2].ExecutionContext.TaskVariables.Get("state1"));
                Assert.Null(result.PostJobStep[2].ExecutionContext.TaskVariables.Get("state1"));
                Assert.Null(result.JobSteps[2].ExecutionContext.TaskVariables.Get("state1"));
                Assert.Null(result.JobSteps[3].ExecutionContext.TaskVariables.Get("state1"));
            }
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionManagementScriptStep()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                hc.EnqueueInstance<IPagingLogger>(_logger.Object);
                hc.EnqueueInstance<IPagingLogger>(_logger.Object);

                Environment.SetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK", "C:\\init.ps1");
                Environment.SetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK", "C:\\clenup.ps1");

                try
                {
                    TestJobExtension testExtension = new TestJobExtension();
                    testExtension.Initialize(hc);
                    JobInitializeResult result = await testExtension.InitializeJob(_jobEc, _message);

                    var trace = hc.GetTrace();

                    trace.Info(string.Join(", ", result.PreJobSteps.Select(x => x.DisplayName)));
                    trace.Info(string.Join(", ", result.JobSteps.Select(x => x.DisplayName)));
                    trace.Info(string.Join(", ", result.PostJobStep.Select(x => x.DisplayName)));

                    Assert.Equal(5, result.PreJobSteps.Count);
                    Assert.Equal(4, result.JobSteps.Count);
                    Assert.Equal(5, result.PostJobStep.Count);

                    Assert.True(result.PreJobSteps[0] is ManagementScriptStep);
                    Assert.True(result.PostJobStep[4] is ManagementScriptStep);

                    Assert.Equal(result.PreJobSteps[0].DisplayName, "Agent Initialization");
                    Assert.Equal(result.PostJobStep[4].DisplayName, "Agent Cleanup");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK", "");
                    Environment.SetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK", "");
                }
            }
        }
#endif
    }
}
