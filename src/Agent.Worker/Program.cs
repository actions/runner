using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public static class Program
    {
        public static Int32 Main(string[] args)
        {
            return RunAsync(args).GetAwaiter().GetResult();
        }        

        public static async Task<Int32> RunAsync(string[] args)
        {
            using (HostContext hc = new HostContext("Worker"))
            {
                //TODO: move this code in a new class - too much code in main program
                TraceSource m_trace =  hc.GetTrace("WorkerProcess");
                m_trace.Info("Info Hello Worker!");
                m_trace.Warning("Warning Hello Worker!");
                m_trace.Error("Error Hello Worker!");
                m_trace.Verbose("Verbose Hello Worker!");
                try
                {
                    if (null != args && 3 == args.Length && "spawnclient".Equals(args[0].ToLower()))
                    {
                        using (var channel = hc.CreateService<IProcessChannel>())
                        {
                            var jobRunner = hc.GetService<IJobRunner>();
                            channel.StartClient(args[1], args[2]);
                            Task<WorkerMessage> packetReceiveTask = null;
                            Task<int> jobRunnerTask = null;
                            var tasks = new List<Task>();
                            while (!hc.CancellationToken.IsCancellationRequested && 
                                (null == jobRunnerTask || (!jobRunnerTask.IsCompleted)))
                            {
                                tasks.Clear();
                                if (null == packetReceiveTask)
                                {
                                    packetReceiveTask = channel.ReceiveAsync(hc.CancellationToken);
                                }                                
                                tasks.Add(packetReceiveTask);
                                if (null != jobRunnerTask)
                                {
                                    tasks.Add(jobRunnerTask);
                                }
                                //wait for either a new packet to be received or for the job runner to finish execution
                                await Task.WhenAny(tasks);
                                if (packetReceiveTask.IsCompleted)
                                {
                                    var packet = await packetReceiveTask;
                                    switch (packet.MessageType)
                                    {
                                        case MessageType.NewJobRequest:
                                            {
                                                var message = JsonUtility.FromString<JobRequestMessage>(packet.Body);
                                                jobRunnerTask = jobRunner.RunAsync(message);
                                            }
                                            break;
                                        case MessageType.CancelRequest:
                                            {
                                                hc.CancellationTokenSource.Cancel();
                                                if (null != jobRunnerTask)
                                                {
                                                    //next line should throw OperationCanceledException
                                                    await jobRunnerTask;
                                                }
                                            }
                                            break;
                                    }
                                    packetReceiveTask = null;
                                }
                            }
                            if (null != jobRunnerTask && jobRunnerTask.IsCompleted)
                            {
                                //next line will throw if job runner failed with an exception
                                return await jobRunnerTask;
                            }
                        }
                    }
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
                catch (Exception ex)
                {
                    m_trace.Error(ex);
                    return 1;
                }                
            }            
            return 0;
        }
    }
}
