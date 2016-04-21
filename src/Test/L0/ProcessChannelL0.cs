using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ProcessChannelL0
    {      
        //RunAsync is an "echo" type service which reads
        //one message and sends back to the server same data 
        public static async Task RunAsync(string[] args)
        {
            using (var client = new ProcessChannel())
            {
                client.StartClient(args[1], args[2]);

                var cs2 = new CancellationTokenSource();
                var packetReceiveTask = client.ReceiveAsync(cs2.Token);
                Task[] taskToWait = { packetReceiveTask, Task.Delay(30*1000)  };
                //Wait up to 5 seconds for the server to call us and then reply back with the same data
                await Task.WhenAny(taskToWait);
                bool timedOut = !packetReceiveTask.IsCompleted;
                if (timedOut)
                {
                    cs2.Cancel();
                    try
                    {
                        await packetReceiveTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore OperationCanceledException and TaskCanceledException exceptions
                    }
                    catch (AggregateException errors)
                    {
                        // Ignore OperationCanceledException and TaskCanceledException exceptions
                        errors.Handle(e => e is OperationCanceledException);
                    }
                }
                else
                {
                    var message = JsonUtility.FromString<JobRequestMessage>(packetReceiveTask.Result.Body);
                    await client.SendAsync(MessageType.NewJobRequest, JsonUtility.ToString(message), cs2.Token);
                }
            }
        }

        //RunIPCEndToEnd test starts another process (the RunAsync function above),
        //sends one packet and receives one packet using ProcessChannel class,
        //and finally verifies if the data we sent is identical to what we have received
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunIPCEndToEnd()
        {
            using (var server = new ProcessChannel())
            {                
                JobRequestMessage result = null;                
                TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
                TimelineReference timeline = null;
                JobEnvironment environment = new JobEnvironment();
                List<TaskInstance> tasks = new List<TaskInstance>();
                Guid JobId = Guid.NewGuid();
                var jobRequest = new JobRequestMessage(plan, timeline, JobId, "someJob", environment, tasks);
                Process jobProcess;
                server.StartServer((p1, p2) =>
                {
                    string clientFileName = $"Test{IOUtil.ExeExtension}";
                    jobProcess = new Process();
                    jobProcess.StartInfo.FileName = clientFileName;
                    jobProcess.StartInfo.Arguments = "spawnclient " + p1 + " " + p2;
                    jobProcess.EnableRaisingEvents = true;                    
                    jobProcess.Start();
                });
                var cs = new CancellationTokenSource();                
                await server.SendAsync(MessageType.NewJobRequest, JsonUtility.ToString(jobRequest), cs.Token);
                var packetReceiveTask = server.ReceiveAsync(cs.Token);
                Task[] taskToWait = { packetReceiveTask, Task.Delay(30*1000) };
                await Task.WhenAny(taskToWait);
                bool timedOut = !packetReceiveTask.IsCompleted;

                // Wait until response is received
                if (timedOut)
                {
                    cs.Cancel();
                    try
                    {
                        await packetReceiveTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore OperationCanceledException and TaskCanceledException exceptions
                    }
                    catch (AggregateException errors)
                    {
                        // Ignore OperationCanceledException and TaskCanceledException exceptions
                        errors.Handle(e => e is OperationCanceledException);
                    }
                }
                else
                {
                    result = JsonUtility.FromString<JobRequestMessage>(packetReceiveTask.Result.Body);                    
                }

                // Wait until response is received
                if (timedOut)
                {
                    Assert.True(false, "Test timed out.");
                }
                else
                {
                    Assert.True(jobRequest.JobId.Equals(result.JobId) && jobRequest.JobName.Equals(result.JobName));
                }
            }
        }
    }
}
