using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.Agent;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

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

                JobRunner jobRunner = null;
                Func<CancellationToken, JobCancelMessage, Task> cancelHandler = (token, message) =>
                {
                    hc.CancellationTokenSource.Cancel();
                    return Task.CompletedTask;
                };

                Func<CancellationToken, JobRequestMessage, Task> newRequestHandler = async (token, message) =>
                {
                    await jobRunner.Run(message);
                };

                try {
                    if (null != args && 3 == args.Length && "spawnclient".Equals(args[0].ToLower()))
                    {
                        using (var channel = hc.GetService<IProcessChannel>())
                        {
                            channel.JobRequestMessageReceived += newRequestHandler;
                            channel.JobCancelMessageReceived += cancelHandler;
                            jobRunner = new JobRunner(hc);
                            channel.StartClient(args[1], args[2]);
                            await jobRunner.WaitToFinish(hc);
                            channel.JobRequestMessageReceived -= newRequestHandler;
                            channel.JobCancelMessageReceived -= cancelHandler;
                            await channel.Stop();
                        }
                    }
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
