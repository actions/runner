using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    internal interface IDapDebuggerCallbacks
    {
        Task HandleMessageAsync(string messageJson, CancellationToken cancellationToken);
        void HandleClientConnected();
        void HandleClientDisconnected();
    }

    [ServiceLocator(Default = typeof(DapServer))]
    internal interface IDapServer : IRunnerService
    {
        void SetDebugger(IDapDebuggerCallbacks debugger);
        Task StartAsync(int port, CancellationToken cancellationToken);
        Task WaitForConnectionAsync(CancellationToken cancellationToken);
        Task StopAsync();
        void SendMessage(ProtocolMessage message);
        void SendEvent(Event evt);
        void SendResponse(Response response);
    }
}
