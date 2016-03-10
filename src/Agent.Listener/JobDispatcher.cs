using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(JobDispatcher))]
    public interface IJobDispatcher : IDisposable, IAgentService
    {
        Task<int> RunAsync(JobRequestMessage message, CancellationToken token);
    }

    public sealed class JobDispatcher : AgentService, IJobDispatcher
    {
#if OS_WINDOWS
        private const string WorkerProcessName = "Agent.Worker.exe";
#else
        private const string WorkerProcessName = "Agent.Worker";
#endif
        //allow up to 30sec for any data to be transmitted over the process channel
        private readonly TimeSpan ChannelTimeout = TimeSpan.FromSeconds(30);

        public async Task<int> RunAsync(JobRequestMessage message, CancellationToken token)
        {
            Task<int> workerProcessTask = null;
            using (var processChannel = HostContext.CreateService<IProcessChannel>())
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processChannel.StartServer(
                    (pipeHandleOut, pipeHandleIn) =>
                    {
                        var assemblyDirectory = IOUtil.GetBinPath();
                        string workerFileName = Path.Combine(assemblyDirectory, WorkerProcessName);
                        workerProcessTask = processInvoker.ExecuteAsync(assemblyDirectory, workerFileName, "spawnclient " + pipeHandleOut + " " + pipeHandleIn, null, token);
                    }
                );

                //send the job request
                var ct1 = new CancellationTokenSource(ChannelTimeout);
                await processChannel.SendAsync(MessageType.NewJobRequest, JsonUtility.ToString(message), ct1.Token);

                int resultCode = 0;
                bool canceled = false;
                try
                {
                    resultCode = await workerProcessTask;
                } 
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                catch (AggregateException errors)
                {
                    canceled = true;
                    // Ignore OperationCanceledException and TaskCanceledException exceptions
                    errors.Handle(e => e is OperationCanceledException);
                }
                if (canceled)
                {
                    //we create internal cancellation token, because the parent token is already cancelled
                    CancellationTokenSource ct2 = new CancellationTokenSource(ChannelTimeout);
                    await processChannel.SendAsync(MessageType.CancelRequest, string.Empty, ct2.Token);
                }
                return resultCode;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}
