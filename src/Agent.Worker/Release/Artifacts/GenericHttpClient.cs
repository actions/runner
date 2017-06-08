using System;
using System.Globalization;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    [ServiceLocator(Default = typeof(GenericHttpClient))]
    public interface IGenericHttpClient : IAgentService
    {
        Task<HttpResponseMessage> GetAsync(string url, string userName, string password, bool acceptUntrustedCertifact);
        Task<string> GetStringAsync(string url, string userName, string password, bool acceptUntrustedCertifact);
    }

    public class GenericHttpClient : AgentService, IGenericHttpClient
    {
        public async Task<HttpResponseMessage> GetAsync(string url, string userName, string password, bool acceptUntrustedCertifact)
        {
            using (HttpClientHandler handler = HostContext.CreateHttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) =>
                {
                    return acceptUntrustedCertifact;
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    SetupHttpClient(client, userName, password);
                    return await client.GetAsync(url);
                }
            }
        }

        public async Task<string> GetStringAsync(string url, string userName, string password, bool acceptUntrustedCertifact)
        {
            HttpResponseMessage response = GetAsync(url, userName, password, acceptUntrustedCertifact).Result;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else 
            {
                throw new Exception(StringUtil.Loc("RMApiFailure", url, response.StatusCode));
            }
        }

        private static void SetupHttpClient(HttpClient httpClient, string userName, string password)
        {
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
            httpClient.DefaultRequestHeaders.Authorization = CreateBasicAuthenticationHeader(userName, password);
        }

        private static AuthenticationHeaderValue CreateBasicAuthenticationHeader(string username, string password)
        {
            var authenticationHeader = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                username ?? string.Empty,
                password ?? string.Empty);

            return new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationHeader)));
        }
    }
}