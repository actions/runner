using System;
using System.Net.Http;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(HttpClientHandlerFactory))]
    public interface IHttpClientHandlerFactory : IRunnerService
    {
        HttpClientHandler CreateClientHandler(RunnerWebProxy webProxy);
    }

    public class HttpClientHandlerFactory : RunnerService, IHttpClientHandlerFactory
    {
        public HttpClientHandler CreateClientHandler(RunnerWebProxy webProxy)
        {
            var client = new HttpClientHandler() { Proxy = webProxy };

            if (StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_TLS_NO_VERIFY")))
            {
                client.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return client;
        }
    }
}