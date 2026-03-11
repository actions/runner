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

    [ServiceLocator(Default = typeof(DapDebugSession))]
    public interface IDapDebugSession : IRunnerService
    {
        bool IsActive { get; }
        DapSessionState State { get; }
        void SetDapServer(IDapServer server);
        Task WaitForHandshakeAsync(CancellationToken cancellationToken);
        Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken);
        void OnStepCompleted(IStep step);
        void OnJobCompleted();
        void CancelSession();
        void HandleClientConnected();
        void HandleClientDisconnected();
        Task HandleMessageAsync(string messageJson, CancellationToken cancellationToken);
    }
}
