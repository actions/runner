using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(Worker))]
    public interface IWorker : IAgentService
    {
        Task<int> RunAsync(string pipeIn, string pipeOut, CancellationTokenSource tokenSource);
    }

    public sealed class Worker : AgentService, IWorker
    {
        public async Task<int> RunAsync(string pipeIn, string pipeOut, CancellationTokenSource tokenSource)
        {
            using (var channel = HostContext.CreateService<IProcessChannel>())
            {
                var jobRunner = HostContext.GetService<IJobRunner>();
                channel.StartClient(pipeIn, pipeOut);
                Task<WorkerMessage> packetReceiveTask = null;
                Task<int> jobRunnerTask = null;
                var tasks = new List<Task>();
                while (!HostContext.CancellationToken.IsCancellationRequested &&
                    (null == jobRunnerTask || (!jobRunnerTask.IsCompleted)))
                {
                    tasks.Clear();
                    if (null == packetReceiveTask)
                    {
                        packetReceiveTask = channel.ReceiveAsync(HostContext.CancellationToken);
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
                        WorkerMessage packet = await packetReceiveTask;
                        switch (packet.MessageType)
                        {
                            case MessageType.NewJobRequest:
                                // Deserialize the job message.
                                var message = JsonUtility.FromString<JobRequestMessage>(packet.Body);

                                // Set the default thread culture.
                                string culture;
                                if (!message.Environment.Variables.TryGetValue(Constants.Variables.System.Culture, out culture))
                                {
                                    culture = null;
                                }

                                ArgUtil.NotNullOrEmpty(culture, nameof(culture));
                                HostContext.SetDefaultCulture(culture);

                                // Run the job.
                                jobRunnerTask = jobRunner.RunAsync(message);
                                break;
                            case MessageType.CancelRequest:
                                tokenSource.Cancel();
                                if (null != jobRunnerTask)
                                {
                                    //next line should throw OperationCanceledException
                                    await jobRunnerTask;
                                }

                                break;
                            default:
                                throw new System.NotSupportedException();
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
         
            return 0;
        }
    }
}
