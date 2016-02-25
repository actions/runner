using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Collections.Generic;

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
                Console.WriteLine("Hello Worker!");

#if OS_WINDOWS
                Console.WriteLine("Hello Windows");
#endif

#if OS_OSX
                Console.WriteLine("Hello OSX");
#endif

#if OS_LINUX
                Console.WriteLine("Hello Linux");
#endif

                TraceSource m_trace =  hc.GetTrace("WorkerProcess");
                m_trace.Info("Info Hello Worker!");
                m_trace.Warning("Warning Hello Worker!");
                m_trace.Error("Error Hello Worker!");
                m_trace.Verbose("Verbos Hello Worker!");
                try
                {
                    if (null != args && 3 == args.Length && "spawnclient".Equals(args[0].ToLower()))
                    {
                        using (var channel = hc.GetService<IProcessChannel>())
                        {
                            var jobRunner = new JobRunner(hc);
                            channel.StartClient(args[1], args[2]);
                            Task<IPCPacket> packetReceiveTask = null;
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
                                    switch (packet._messageType)
                                    {
                                        case 1:
                                            {
                                                var message = JsonUtility.FromString<JobRequestMessage>(packet._body);
                                                jobRunnerTask = jobRunner.RunAsync(message, hc.CancellationToken);
                                            }
                                            break;
                                        case 2:
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
