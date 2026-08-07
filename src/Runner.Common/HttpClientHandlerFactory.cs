using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
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

            // Configure mTLS client certificate for proxy authentication
            if (!string.IsNullOrEmpty(webProxy.HttpsProxyClientCert))
            {
                var clientCert = LoadClientCertificate(
                    webProxy.HttpsProxyClientCert,
                    webProxy.HttpsProxyClientKey);
                if (clientCert != null)
                {
                    client.ClientCertificates.Add(clientCert);
                }
            }

            return client;
        }

        private X509Certificate2 LoadClientCertificate(string certPath, string keyPath)
        {
            try
            {
                if (!File.Exists(certPath))
                {
                    Trace.Warning($"Client certificate file not found: {certPath}");
                    return null;
                }

                // If key path is provided separately, load cert and key from separate files
                if (!string.IsNullOrEmpty(keyPath))
                {
                    if (!File.Exists(keyPath))
                    {
                        Trace.Warning($"Client key file not found: {keyPath}");
                        return null;
                    }

                    // Load certificate and private key from separate PEM files
                    var certPem = File.ReadAllText(certPath);
                    var keyPem = File.ReadAllText(keyPath);
                    var cert = X509Certificate2.CreateFromPem(certPem, keyPem);

                    // On Windows, we need to export and re-import to make the certificate usable
                    // with SslStream/HttpClient
                    return new X509Certificate2(cert.Export(X509ContentType.Pfx));
                }
                else
                {
                    // Assume the cert file contains both certificate and key (PFX/PKCS12 format)
                    return new X509Certificate2(certPath);
                }
            }
            catch (Exception ex)
            {
                Trace.Warning($"Failed to load client certificate: {ex.Message}");
                return null;
            }
        }
    }
}
