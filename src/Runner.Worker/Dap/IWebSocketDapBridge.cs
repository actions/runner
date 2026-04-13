using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    [ServiceLocator(Default = typeof(WebSocketDapBridge))]
    public interface IWebSocketDapBridge : IRunnerService
    {
        int ListenPort { get; }
        void Start(int listenPort, int targetPort);
        Task ShutdownAsync();
    }
}
