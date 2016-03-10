using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener
{
    public sealed class AgentL0
    {
        private Mock<IConfigurationManager> _configurationManager;
        private Mock<IMessageListener> _messageListener;
        private Mock<IWorkerManager> _workerManager;
        private Mock<IAgentServer> _agentServer;

        public AgentL0()
        {
            _configurationManager = new Mock<IConfigurationManager>();
            _messageListener = new Mock<IMessageListener>();
            _workerManager = new Mock<IWorkerManager>();
            _agentServer = new Mock<IAgentServer>();            
        }

        private JobRequestMessage CreateJobRequestMessage(string jobName)
        {
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = null;
            JobEnvironment environment = new JobEnvironment();
            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            var jobRequest = new JobRequestMessage(plan, timeline, JobId, jobName, environment, tasks);
            return jobRequest;
        }

        private JobCancelMessage CreateJobCancelMessage()
        {
            var message = new JobCancelMessage(Guid.NewGuid(), TimeSpan.FromSeconds(0));
            return message;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        //process 2 new job messages, and one cancel message
        public async void TestRunAsync()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var agent = new Microsoft.VisualStudio.Services.Agent.Listener.Agent();
                hc.SetSingleton<IConfigurationManager>(_configurationManager.Object);
                hc.SetSingleton<IMessageListener>(_messageListener.Object);
                hc.SetSingleton<IWorkerManager>(_workerManager.Object);
                hc.SetSingleton<IAgentServer>(_agentServer.Object);
                agent.Initialize(hc);
                var settings = new AgentSettings
                {
                    PoolId = 43242
                };
                var taskAgentSession = new TaskAgentSession
                {
                    //SessionId = Guid.NewGuid() //we use reflection to achieve this, because "set" is internal
                };
                PropertyInfo sessionIdProperty = taskAgentSession.GetType().GetProperty("SessionId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(sessionIdProperty);
                sessionIdProperty.SetValue(taskAgentSession, Guid.NewGuid());

                var arMessages = new TaskAgentMessage[]
                    {
                        new TaskAgentMessage
                        {
                            Body = JsonUtility.ToString(CreateJobRequestMessage("job1")),
                            MessageId = 4234,
                            MessageType = JobRequestMessage.MessageType
                        },
                        new TaskAgentMessage
                        {
                            Body = JsonUtility.ToString(CreateJobCancelMessage()),
                            MessageId = 4235,
                            MessageType = JobCancelMessage.MessageType
                        },
                        new TaskAgentMessage
                        {
                            Body = JsonUtility.ToString(CreateJobRequestMessage("last_job")),
                            MessageId = 4236,
                            MessageType = JobRequestMessage.MessageType
                        }
                    };
                var messages = new Queue<TaskAgentMessage>(arMessages);
                var signalWorkerComplete = new SemaphoreSlim(0, 1);
                _configurationManager.Setup(x => x.LoadSettings())
                    .Returns(settings);
                _configurationManager.Setup(x => x.IsConfigured())
                    .Returns(true);
                _configurationManager.Setup(x => x.EnsureConfiguredAsync())
                    .Returns(Task.CompletedTask);
                _messageListener.Setup(x => x.CreateSessionAsync())
                    .Returns(Task.FromResult<bool>(true));
                _messageListener.Setup(x => x.Session)
                    .Returns(taskAgentSession);
                _messageListener.Setup(x => x.GetNextMessageAsync())
                    .Returns(async () =>
                        {
                            if (0 == messages.Count)
                            {
                                await Task.Delay(2000, hc.CancellationToken);
                                throw new TimeoutException();
                            }

                            return messages.Dequeue();
                        });
                _messageListener.Setup(x => x.DeleteSessionAsync())
                    .Returns(Task.CompletedTask);
                _workerManager.Setup(x => x.Run(It.IsAny<JobRequestMessage>()))
                    .Returns( (JobRequestMessage m) => 
                        {
                            //last job starts the task
                            if (m.JobName.Equals("last_job"))
                            {
                                signalWorkerComplete.Release();
                                return Task.CompletedTask;
                            }                            
                            else 
                            {
                                return Task.CompletedTask;
                            }
                            
                        }
                    );
                _workerManager.Setup(x => x.Cancel(It.IsAny<JobCancelMessage>()));
                _agentServer.Setup(x => x.DeleteAgentMessageAsync(settings.PoolId, arMessages[0].MessageId, taskAgentSession.SessionId, It.IsAny<CancellationToken>()))
                    .Returns( (Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken) =>
                    {
                        return Task.CompletedTask;
                    });

                //Act
                var parser = new CommandLineParser(hc);
                parser.Parse(new string[] { "" });
                Task agentTask = agent.ExecuteCommand(parser);

                //Assert
                //wait for the agent to run one job
                if (! await signalWorkerComplete.WaitAsync(2000) )                
                {
                    Assert.True(false, $"{nameof(_messageListener.Object.GetNextMessageAsync)} was not invoked." );
                }
                else
                {
                    //Act
                    hc.Cancel(); //stop Agent
                    
                    //Assert
                    Task[] taskToWait2 = { agentTask, Task.Delay(2000) };
                    //wait for the Agent to exit
                    await Task.WhenAny(taskToWait2);

                    Assert.True(agentTask.IsCompleted, $"{nameof(agent.ExecuteCommand)} timed out.");
                    Assert.True(!agentTask.IsFaulted, agentTask.Exception?.ToString());
                    Assert.True(agentTask.IsCanceled);

                    _workerManager.Verify(x => x.Run(It.IsAny<JobRequestMessage>()), Times.AtLeast(2),
                         $"{nameof(_workerManager.Object.Run)} was not invoked.");
                    _workerManager.Verify(x => x.Cancel(It.IsAny<JobCancelMessage>()), Times.Once(),
                        $"{nameof(_workerManager.Object.Cancel)} was not invoked.");
                    _messageListener.Verify(x => x.GetNextMessageAsync(), Times.AtLeast(arMessages.Length));
                    _messageListener.Verify(x => x.CreateSessionAsync(), Times.Once());
                    _messageListener.Verify(x => x.DeleteSessionAsync(), Times.Once());
                    _agentServer.Verify(x => x.DeleteAgentMessageAsync(settings.PoolId, arMessages[0].MessageId, taskAgentSession.SessionId,  It.IsAny<CancellationToken>()), Times.Once());
                }
            }
        }
    }
}
