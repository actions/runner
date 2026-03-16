using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    [ServiceLocator(Default = typeof(DapDebugger))]
    public interface IDapDebugger : IRunnerService
    {
        bool IsActive { get; }
        Task StartAsync(CancellationToken cancellationToken);
        Task WaitUntilReadyAsync(CancellationToken cancellationToken);
        Task StopAsync();
        void CancelSession();
        Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken);
        void OnStepCompleted(IStep step);
        void OnJobCompleted();
    }
}
