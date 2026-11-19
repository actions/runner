using GitHub.DistributedTask.WebApi;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.Runner.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.Services.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class WorkerL0
    {
        private Mock<IProcessChannel> _processChannel;
        private Mock<IJobRunner> _jobRunner;
        private Mock<IVSockSecretNotifier> _vsockSecretNotifier;
        private Mock<IProcessGroupNative> _processGroupNative;

        public WorkerL0()
        {
            _processChannel = new Mock<IProcessChannel>();
            _jobRunner = new Mock<IJobRunner>();
            _vsockSecretNotifier = new Mock<IVSockSecretNotifier>();
            _processGroupNative = new Mock<IProcessGroupNative>();
        }

        private Pipelines.AgentJobRequestMessage CreateJobRequestMessage(string jobName)
        {
            TaskOrchestrationPlanReference plan = new() { PlanId = Guid.NewGuid() };
            TimelineReference timeline = null;
            Dictionary<string, VariableValue> variables = new(StringComparer.OrdinalIgnoreCase);
            variables[Constants.Variables.System.Culture] = "en-US";
            Pipelines.JobResources resources = new();
            var serviceEndpoint = new ServiceEndpoint();
            serviceEndpoint.Authorization = new EndpointAuthorization();
            serviceEndpoint.Authorization.Parameters.Add("nullValue", null);
            resources.Endpoints.Add(serviceEndpoint);

            List<Pipelines.ActionStep> actions = new();
            actions.Add(new Pipelines.ActionStep()
            {
                Id = Guid.NewGuid(),
                Reference = new Pipelines.RepositoryPathReference()
                {
                    RepositoryType = "GitHub",
                    Name = "actions/test",
                    Ref = "v1"
                }
            });
            Guid JobId = Guid.NewGuid();
            var sidecarContainers = new MappingToken(null, null, null)
            {
                {
                    new StringToken(null, null, null, "nginx"),
                    new MappingToken(null, null, null)
                    {
                        {
                            new StringToken(null, null, null, "image"),
                            new StringToken(null, null, null, "nginx")
                        },
                    }
                },
            };
            var context = new Pipelines.ContextData.DictionaryContextData
            {
                {
                    "github",
                    new Pipelines.ContextData.DictionaryContextData()
                },
            };
            var jobRequest = new Pipelines.AgentJobRequestMessage(plan, timeline, JobId, jobName, jobName, new StringToken(null, null, null, "ubuntu"), sidecarContainers, null, variables, new List<MaskHint>(), resources, context, null, actions, null, null, null, null, null);
            return jobRequest;
        }

        private JobCancelMessage CreateJobCancelMessage(Guid jobId)
        {
            return new JobCancelMessage(jobId, TimeSpan.FromSeconds(0));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void DispatchRunNewJob()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            using (var tokenSource = new CancellationTokenSource())
            {
                var worker = new GitHub.Runner.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IJobRunner>(_jobRunner.Object);
                hc.SetSingleton<IVSockSecretNotifier>(_vsockSecretNotifier.Object);
                worker.Initialize(hc);
                var jobMessage = CreateJobRequestMessage("job1");
                var arWorkerMessages = new WorkerMessage[]
                    {
                        new WorkerMessage
                        {
                            Body = JsonUtility.ToString(jobMessage),
                            MessageType = MessageType.NewJobRequest
                        }
                    };
                var workerMessages = new Queue<WorkerMessage>(arWorkerMessages);

                _processChannel
                    .Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                    {
                        // Return the job message.
                        if (workerMessages.Count > 0)
                        {
                            return workerMessages.Dequeue();
                        }

                        // Wait for the text to run
                        await Task.Delay(-1, tokenSource.Token);
                        return default(WorkerMessage);
                    });
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<TaskResult>(TaskResult.Succeeded));

                //Act
                await worker.RunAsync(pipeIn: "1", pipeOut: "2");

                //Assert
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
                _jobRunner.Verify(x => x.RunAsync(
                    It.Is<Pipelines.AgentJobRequestMessage>(y => IsMessageIdentical(y, jobMessage)), It.IsAny<CancellationToken>()));
                tokenSource.Cancel();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void DispatchCancellation()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                var worker = new GitHub.Runner.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IJobRunner>(_jobRunner.Object);
                hc.SetSingleton<IVSockSecretNotifier>(_vsockSecretNotifier.Object);
                worker.Initialize(hc);
                var jobMessage = CreateJobRequestMessage("job1");
                var cancelMessage = CreateJobCancelMessage(jobMessage.JobId);
                var arWorkerMessages = new WorkerMessage[]
                    {
                        new WorkerMessage
                        {
                            Body = JsonUtility.ToString(jobMessage),
                            MessageType = MessageType.NewJobRequest
                        },
                        new WorkerMessage
                        {
                            Body = JsonUtility.ToString(cancelMessage),
                            MessageType = MessageType.CancelRequest
                        }

                    };
                var workerMessages = new Queue<WorkerMessage>(arWorkerMessages);

                _processChannel.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(workerMessages.Dequeue()));
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(
                    async (Pipelines.AgentJobRequestMessage jm, CancellationToken ct) =>
                    {
                        await Task.Delay(-1, ct);
                        return TaskResult.Canceled;
                    });

                //Act
                await Assert.ThrowsAsync<TaskCanceledException>(
                    async () => await worker.RunAsync("1", "2"));

                //Assert
                _processChannel.Verify(x => x.StartClient("1", "2"), Times.Once());
                _jobRunner.Verify(x => x.RunAsync(
                    It.Is<Pipelines.AgentJobRequestMessage>(y => IsMessageIdentical(y, jobMessage)), It.IsAny<CancellationToken>()));
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("false", false)]
        [InlineData("true", true)]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OwnProcessGroupFeatureFlagGatesProcessGroupMove(string featureFlag, bool expectMove)
        {
            using (var hc = new TestHostContext(this))
            using (new ManuallyTrapSigScope(null))
            {
                var worker = new GitHub.Runner.Worker.Worker();
                hc.EnqueueInstance<IProcessChannel>(_processChannel.Object);
                hc.EnqueueInstance<IJobRunner>(_jobRunner.Object);
                hc.SetSingleton<IVSockSecretNotifier>(_vsockSecretNotifier.Object);
                hc.SetSingleton<IProcessGroupNative>(_processGroupNative.Object);
                worker.Initialize(hc);
                var jobMessage = CreateJobRequestMessage("job1");
                if (featureFlag != null)
                {
                    jobMessage.Variables[Constants.Runner.Features.WorkerOwnProcessGroup] = featureFlag;
                }

                var workerMessages = new Queue<WorkerMessage>(new[]
                {
                    new WorkerMessage
                    {
                        Body = JsonUtility.ToString(jobMessage),
                        MessageType = MessageType.NewJobRequest
                    }
                });
                _processChannel.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                    .Returns(async (CancellationToken ct) =>
                    {
                        if (workerMessages.Count > 0)
                        {
                            return workerMessages.Dequeue();
                        }

                        await Task.Delay(-1, ct);
                        return default(WorkerMessage);
                    });
                _processGroupNative.Setup(x => x.ProbeControllingTerminal()).Returns(ProcessGroupNative.ENXIO);
                // The move has to happen before the job starts, so that steps inherit the new process group.
                _processGroupNative.Setup(x => x.MoveToNewProcessGroup())
                    .Callback(() => _jobRunner.Verify(y => y.RunAsync(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never()))
                    .Returns(0);
                _jobRunner.Setup(x => x.RunAsync(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<TaskResult>(TaskResult.Succeeded));

                await worker.RunAsync(pipeIn: "1", pipeOut: "2");

#if OS_WINDOWS
                // Process groups are not used on Windows, whatever the feature flag says.
                expectMove = false;
#endif
                _processGroupNative.Verify(x => x.MoveToNewProcessGroup(), expectMove ? Times.Once() : Times.Never());
                _jobRunner.Verify(x => x.RunAsync(It.IsAny<Pipelines.AgentJobRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Theory]
        [InlineData(ProcessGroupNative.ENXIO, true)]
        [InlineData(ProcessGroupNative.ENOENT, true)]
        [InlineData(0, false)] // Controlling terminal found.
        [InlineData(5, false)] // EIO, unknown whether there is a terminal.
        [InlineData(24, false)] // EMFILE, unknown whether there is a terminal.
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MoveToOwnProcessGroupOnlyWithoutControllingTerminal(int terminalErrno, bool expectMove)
        {
            using (var hc = new TestHostContext(this))
            using (new ManuallyTrapSigScope(null))
            {
                var worker = CreateWorkerWithProcessGroupNative(hc);
                _processGroupNative.Setup(x => x.ProbeControllingTerminal()).Returns(terminalErrno);
                _processGroupNative.Setup(x => x.MoveToNewProcessGroup()).Returns(0);

                worker.MoveToOwnProcessGroup();

                _processGroupNative.Verify(x => x.MoveToNewProcessGroup(), expectMove ? Times.Once() : Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MoveToOwnProcessGroupSkippedWhenManuallyTrappingSignals()
        {
            using (var hc = new TestHostContext(this))
            using (new ManuallyTrapSigScope("1"))
            {
                var worker = CreateWorkerWithProcessGroupNative(hc);

                worker.MoveToOwnProcessGroup();

                _processGroupNative.Verify(x => x.ProbeControllingTerminal(), Times.Never());
                _processGroupNative.Verify(x => x.MoveToNewProcessGroup(), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MoveToOwnProcessGroupToleratesSetpgidFailure()
        {
            using (var hc = new TestHostContext(this))
            using (new ManuallyTrapSigScope(null))
            {
                var worker = CreateWorkerWithProcessGroupNative(hc);
                _processGroupNative.Setup(x => x.ProbeControllingTerminal()).Returns(ProcessGroupNative.ENXIO);
                _processGroupNative.Setup(x => x.MoveToNewProcessGroup()).Returns(1); // EPERM

                worker.MoveToOwnProcessGroup();

                _processGroupNative.Verify(x => x.MoveToNewProcessGroup(), Times.Once());
                _processGroupNative.Verify(x => x.GetProcessGroup(), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MoveToOwnProcessGroupToleratesNativeException()
        {
            using (var hc = new TestHostContext(this))
            using (new ManuallyTrapSigScope(null))
            {
                var worker = CreateWorkerWithProcessGroupNative(hc);
                _processGroupNative.Setup(x => x.ProbeControllingTerminal()).Throws(new DllNotFoundException("libc"));

                worker.MoveToOwnProcessGroup();

                _processGroupNative.Verify(x => x.MoveToNewProcessGroup(), Times.Never());
            }
        }

        private GitHub.Runner.Worker.Worker CreateWorkerWithProcessGroupNative(TestHostContext hc)
        {
            var worker = new GitHub.Runner.Worker.Worker();
            hc.SetSingleton<IProcessGroupNative>(_processGroupNative.Object);
            worker.Initialize(hc);
            return worker;
        }

        private sealed class ManuallyTrapSigScope : IDisposable
        {
            private const string Name = "RUNNER_MANUALLY_TRAP_SIG";
            private readonly string _original;

            public ManuallyTrapSigScope(string value)
            {
                _original = Environment.GetEnvironmentVariable(Name);
                Environment.SetEnvironmentVariable(Name, value);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(Name, _original);
            }
        }

        private bool IsMessageIdentical(Pipelines.AgentJobRequestMessage source, Pipelines.AgentJobRequestMessage target)
        {
            if (source == null && target == null)
            {
                return true;
            }
            if (source != null && target == null)
            {
                return false;
            }
            if (source == null && target != null)
            {
                return false;
            }
            if (JsonUtility.ToString(source.JobContainer) != JsonUtility.ToString(target.JobContainer))
            {
                return false;
            }
            if (source.JobDisplayName != target.JobDisplayName)
            {
                return false;
            }
            if (source.JobId != target.JobId)
            {
                return false;
            }
            if (source.JobName != target.JobName)
            {
                return false;
            }
            if (source.MaskHints.Count != target.MaskHints.Count)
            {
                return false;
            }
            if (source.MessageType != target.MessageType)
            {
                return false;
            }
            if (source.Plan.PlanId != target.Plan.PlanId)
            {
                return false;
            }
            if (source.RequestId != target.RequestId)
            {
                return false;
            }
            if (source.Resources.Endpoints.Count != target.Resources.Endpoints.Count)
            {
                return false;
            }
            if (source.Steps.Count != target.Steps.Count)
            {
                return false;
            }
            if (source.Variables.Count != target.Variables.Count)
            {
                return false;
            }

            return true;
        }
    }
}
