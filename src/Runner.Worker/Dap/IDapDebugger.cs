using System.Collections.Generic;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    public enum DapSessionState
    {
        NotStarted,
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
        Task StartAsync(IExecutionContext jobContext);
        Task WaitUntilReadyAsync();
        Task OnJobStepsInitializedAsync(IEnumerable<IStep> steps, IEnumerable<IStep> initialPostSteps);
        void OnPostStepRegistered(IStep step);
        Task OnStepStartingAsync(IStep step);
        void OnStepCompleted(IStep step);
        Task OnJobCompletedAsync();
        Task StopAsync();
    }
}
