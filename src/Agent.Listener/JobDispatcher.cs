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

    public class JobDispatcher : AgentService, IJobDispatcher
    {
#if OS_WINDOWS
        private const String WorkerProcessName = "Agent.Worker.exe";
#else
        private const String WorkerProcessName = "Agent.Worker";
#endif        

        public async Task<int> RunAsync(JobRequestMessage message, CancellationToken token)
        {
            Task<int> workerProcessTask = null;
            using (var processChannel = HostContext.GetService<IProcessChannel>())
            using (var processInvoker = HostContext.GetService<IProcessInvoker>())
            {
                processChannel.StartServer(
                    (pipeHandleOut, pipeHandleIn) =>
                    {
                        var assemblyDirectory = IOUtil.GetBinPath();
                        string workerFileName = Path.Combine(assemblyDirectory, WorkerProcessName);
                        workerProcessTask = processInvoker.ExecuteAsync(assemblyDirectory, workerFileName, "spawnclient " + pipeHandleOut + " " + pipeHandleIn, null, token);
                    }
                );
                await processChannel.SendAsync(MessageType.NewJobRequest, JsonUtility.ToString(message), token);
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
                    //we create internal cancelation token, which nobody can access, because the parent token is already cancelled
                    //Hopefully not an issue, because cancelation should not take long
                    CancellationTokenSource ct = new CancellationTokenSource();
                    await processChannel.SendAsync(MessageType.CancelRequest, "", ct.Token);
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
