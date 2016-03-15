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
                // Start the process channel.
                // It's OK if StartServer bubbles an execption after the worker process has already started.
                // The worker will shutdown after 30 seconds if it hasn't received the job message.
                processChannel.StartServer(
                    // Delegate to start the child process.
                    startProcess: (string pipeHandleOut, string pipeHandleIn) =>
                    {
                        // Validate args.
                        ArgUtil.NotNullOrEmpty(pipeHandleOut, nameof(pipeHandleOut));
                        ArgUtil.NotNullOrEmpty(pipeHandleIn, nameof(pipeHandleIn));

                        // Start the child process.
                        var assemblyDirectory = IOUtil.GetBinPath();
                        string workerFileName = Path.Combine(assemblyDirectory, WorkerProcessName);
                        workerProcessTask = processInvoker.ExecuteAsync(
                            workingDirectory: assemblyDirectory,
                            fileName: workerFileName,
                            arguments: "spawnclient " + pipeHandleOut + " " + pipeHandleIn,
                            environment: null,
                            cancellationToken: token);
                    });

                // Send the job request message.
                // TODO: Kill the worker process if sending the job message times out. The worker
                // process may have successfully received the job message.
                await processChannel.SendAsync(
                    messageType: MessageType.NewJobRequest,
                    body: JsonUtility.ToString(message),
                    cancellationToken: new CancellationTokenSource(ChannelTimeout).Token);

                try
                {
                    // Wait for the process to exit.
                    return await workerProcessTask;
                } 
                catch (OperationCanceledException)
                {
                }
                catch (AggregateException errors)
                {
                    // Ignore OperationCanceledException and TaskCanceledException exceptions.
                    // Otherwise bubble out.
                    errors.Handle(e => e is OperationCanceledException);
                }

                // Send a cancellation message.
                // TODO: Regardless of whether the cancellation message is sent successfully, wait for a certain amount of time, then kill the worker process if it is still running.
                await processChannel.SendAsync(
                    messageType: MessageType.CancelRequest,
                    body: string.Empty,
                    cancellationToken: new CancellationTokenSource(ChannelTimeout).Token);
                return 0;
            }
        }

        // TODO: REMOVE DEAD CODE.
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
