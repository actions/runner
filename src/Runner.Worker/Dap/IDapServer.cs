using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    [ServiceLocator(Default = typeof(DapServer))]
    public interface IDapServer : IRunnerService
    {
        void SetSession(IDapDebugSession session);
        Task StartAsync(int port, CancellationToken cancellationToken);
        Task WaitForConnectionAsync(CancellationToken cancellationToken);
        Task StopAsync();
        void SendMessage(ProtocolMessage message);
        void SendEvent(Event evt);
        void SendResponse(Response response);
    }
}
