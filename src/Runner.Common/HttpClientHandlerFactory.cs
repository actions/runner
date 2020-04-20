using System.Net.Http;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(HttpClientHandlerFactory))]
    public interface IHttpClientHandlerFactory : IRunnerService
    {
        HttpClientHandler CreateClientHandler();
    }

    public class HttpClientHandlerFactory : IHttpClientHandlerFactory
    {
        public HttpClientHandler CreateClientHandler()
        {
            return new HttpClientHandler();
        }

        public void Initialize(IHostContext context)
        {
        }
    }
}