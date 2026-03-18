using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    public enum DapSessionState
    {
        WaitingForConnection,
        Initializing,
        Ready,
        Paused,
        Running,
        Terminated
    }

    [ServiceLocator(Default = typeof(DapDebugger))]
    public interface IDapDebugger : IRunnerService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task WaitUntilReadyAsync(CancellationToken cancellationToken);
        Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, CancellationToken cancellationToken);
        void OnStepCompleted(IStep step);
        Task OnJobCompletedAsync();
    }
}
