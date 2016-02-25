using System;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(Worker))]
    public interface IWorker : IDisposable, IAgentService
    {
        Task<int> RunAsync(JobRequestMessage jobRequestMessage, CancellationToken cancellationToken);
    }

    public class Worker : AgentService, IWorker
    {
#if OS_WINDOWS
        private const String WorkerProcessName = "Agent.Worker.exe";
#else
        private const String WorkerProcessName = "Agent.Worker";
#endif        

        public async Task<int> RunAsync(JobRequestMessage jobRequestMessage, CancellationToken cancellationToken)
        {            
            Task<int> workerProcessTask = null;
            using (var processChannel = HostContext.GetService<IProcessChannel>())
            using (var processInvoker = HostContext.GetService<IProcessInvoker>())
            {
                processChannel.StartServer(
                    (pipeHandleOut, pipeHandleIn) =>
                    {                        
                        var assemblyDirectory = AssemblyUtil.AssemblyDirectory;
                        string workerFileName = Path.Combine(assemblyDirectory, WorkerProcessName);
                        workerProcessTask = processInvoker.ExecuteAsync(assemblyDirectory, workerFileName, "spawnclient " + pipeHandleOut + " " + pipeHandleIn, null, cancellationToken);
                    }
                );
                await processChannel.SendAsync(1, JsonUtility.ToString(jobRequestMessage), cancellationToken);
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
                    await processChannel.SendAsync(2, "", ct.Token);
                }
                return resultCode;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
#endregion
    }
}
