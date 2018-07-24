
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.Services.Common;

namespace Agent.Sdk
{
    public class AgentCertificateSettings
    {
        public bool SkipServerCertificateValidation { get; set; }
        public string CACertificateFile { get; set; }
        public string ClientCertificateFile { get; set; }
        public string ClientCertificatePrivateKeyFile { get; set; }
        public string ClientCertificateArchiveFile { get; set; }
        public string ClientCertificatePassword { get; set; }
        public IVssClientCertificateManager VssClientCertificateManager { get; set; }
    }

    public class AgentClientCertificateManager : IVssClientCertificateManager
    {
        private readonly X509Certificate2Collection _clientCertificates = new X509Certificate2Collection();
        public X509Certificate2Collection ClientCertificates => _clientCertificates;

        public AgentClientCertificateManager()
        {
        }

        public AgentClientCertificateManager(string clientCertificateArchiveFile, string clientCertificatePassword)
        {
            AddClientCertificate(clientCertificateArchiveFile, clientCertificatePassword);
        }

        public void AddClientCertificate(string clientCertificateArchiveFile, string clientCertificatePassword)
        {
            if (!string.IsNullOrEmpty(clientCertificateArchiveFile))
            {
                _clientCertificates.Add(new X509Certificate2(clientCertificateArchiveFile, clientCertificatePassword));
            }
        }
    }
}