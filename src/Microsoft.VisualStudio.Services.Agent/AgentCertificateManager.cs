using System;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(AgentCertificateManager))]
    public interface IAgentCertificateManager : IAgentService, IVssClientCertificateManager
    {
        string CACertificateFile { get; }
        string ClientCertificateFile { get; }
        string ClientCertificatePrivateKeyFile { get; }
        string ClientCertificateArchiveFile { get; }
        string ClientCertificatePassword { get; }
    }

    public class AgentCertificateManager : AgentService, IAgentCertificateManager
    {
        private readonly X509Certificate2Collection _clientCertificates = new X509Certificate2Collection();

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            LoadCertificateSettings();
        }

        // This should only be called from config
        public void SetupCertificate(string caCert, string clientCert, string clientCertPrivateKey, string clientCertArchive, string clientCertPassword)
        {
            Trace.Info("Setup agent certificate setting base on configuration inputs.");

            if (!string.IsNullOrEmpty(caCert))
            {
                ArgUtil.File(caCert, nameof(caCert));
                Trace.Info($"Self-Signed CA '{caCert}'");
            }

            if (!string.IsNullOrEmpty(clientCert))
            {
                ArgUtil.File(clientCert, nameof(clientCert));
                ArgUtil.File(clientCertPrivateKey, nameof(clientCertPrivateKey));
                ArgUtil.File(clientCertArchive, nameof(clientCertArchive));
                ArgUtil.NotNullOrEmpty(clientCertPassword, nameof(clientCertPassword));

                Trace.Info($"Client cert '{clientCert}'");
                Trace.Info($"Client cert private key '{clientCertPrivateKey}'");
                Trace.Info($"Client cert archive '{clientCertArchive}'");
            }

            // TODO: Setup ServicePointManager.ServerCertificateValidationCallback when adopt netcore 2.0 to support self-signed cert for agent infrastructure.
            CACertificateFile = caCert;
            ClientCertificateFile = clientCert;
            ClientCertificatePrivateKeyFile = clientCertPrivateKey;
            ClientCertificateArchiveFile = clientCertArchive;
            ClientCertificatePassword = clientCertPassword;

            _clientCertificates.Clear();
            if (!string.IsNullOrEmpty(ClientCertificateArchiveFile))
            {
                _clientCertificates.Add(new X509Certificate2(ClientCertificateArchiveFile, ClientCertificatePassword));
            }
        }

        // This should only be called from config
        public void SaveCertificateSetting()
        {
            string certSettingFile = IOUtil.GetAgentCertificateSettingFilePath();
            IOUtil.DeleteFile(certSettingFile);

            var setting = new AgentCertificateSetting();
            if (!string.IsNullOrEmpty(CACertificateFile))
            {
                Trace.Info($"Store CA cert setting to '{certSettingFile}'");
                setting.CACert = CACertificateFile;
            }

            if (!string.IsNullOrEmpty(ClientCertificateFile) &&
                !string.IsNullOrEmpty(ClientCertificatePrivateKeyFile) &&
                !string.IsNullOrEmpty(ClientCertificateArchiveFile) &&
                !string.IsNullOrEmpty(ClientCertificatePassword))
            {
                Trace.Info($"Store client cert settings to '{certSettingFile}'");

                string lookupKey = Guid.NewGuid().ToString("D").ToUpperInvariant();
                Trace.Info($"Store client cert private key password with lookup key {lookupKey}");

                var credStore = HostContext.GetService<IAgentCredentialStore>();
                credStore.Write($"VSTS_AGENT_CLIENT_CERT_PASSWORD_{lookupKey}", "VSTS", ClientCertificatePassword);

                setting.ClientCert = ClientCertificateFile;
                setting.ClientCertPrivatekey = ClientCertificatePrivateKeyFile;
                setting.ClientCertArchive = ClientCertificateArchiveFile;
                setting.ClientCertPasswordLookupKey = lookupKey;
            }

            if (!string.IsNullOrEmpty(CACertificateFile) ||
                !string.IsNullOrEmpty(ClientCertificateFile))
            {
                IOUtil.SaveObject(setting, certSettingFile);
                File.SetAttributes(certSettingFile, File.GetAttributes(certSettingFile) | FileAttributes.Hidden);
            }
        }

        // This should only be called from unconfig
        public void DeleteCertificateSetting()
        {
            string certSettingFile = IOUtil.GetAgentCertificateSettingFilePath();
            if (File.Exists(certSettingFile))
            {
                Trace.Info($"Load agent certificate setting from '{certSettingFile}'");
                var certSetting = IOUtil.LoadObject<AgentCertificateSetting>(certSettingFile);

                if (certSetting != null && !string.IsNullOrEmpty(certSetting.ClientCertPasswordLookupKey))
                {
                    Trace.Info("Delete client cert private key password from credential store.");
                    var credStore = HostContext.GetService<IAgentCredentialStore>();
                    credStore.Delete($"VSTS_AGENT_CLIENT_CERT_PASSWORD_{certSetting.ClientCertPasswordLookupKey}");
                }

                Trace.Info($"Delete cert setting file: {certSettingFile}");
                IOUtil.DeleteFile(certSettingFile);
            }
        }

        public void LoadCertificateSettings()
        {
            string certSettingFile = IOUtil.GetAgentCertificateSettingFilePath();
            if (File.Exists(certSettingFile))
            {
                Trace.Info($"Load agent certificate setting from '{certSettingFile}'");
                var certSetting = IOUtil.LoadObject<AgentCertificateSetting>(certSettingFile);
                ArgUtil.NotNull(certSetting, nameof(AgentCertificateSetting));

                // make sure all settings file exist
                if (!string.IsNullOrEmpty(certSetting.CACert))
                {
                    ArgUtil.File(certSetting.CACert, nameof(certSetting.CACert));
                    Trace.Info($"CA '{certSetting.CACert}'");
                    CACertificateFile = certSetting.CACert;
                    // TODO: Setup ServicePointManager.ServerCertificateValidationCallback when adopt netcore 2.0 to support self-signed cert for agent infrastructure.
                }

                if (!string.IsNullOrEmpty(certSetting.ClientCert))
                {
                    ArgUtil.File(certSetting.ClientCert, nameof(certSetting.ClientCert));
                    ArgUtil.File(certSetting.ClientCertPrivatekey, nameof(certSetting.ClientCertPrivatekey));
                    ArgUtil.File(certSetting.ClientCertArchive, nameof(certSetting.ClientCertArchive));
                    ArgUtil.NotNullOrEmpty(certSetting.ClientCertPasswordLookupKey, nameof(certSetting.ClientCertPasswordLookupKey));

                    Trace.Info($"Client cert '{certSetting.ClientCert}'");
                    Trace.Info($"Client cert private key '{certSetting.ClientCertPrivatekey}'");
                    Trace.Info($"Client cert archive '{certSetting.ClientCertArchive}'");

                    ClientCertificateFile = certSetting.ClientCert;
                    ClientCertificatePrivateKeyFile = certSetting.ClientCertPrivatekey;
                    ClientCertificateArchiveFile = certSetting.ClientCertArchive;

                    var cerdStore = HostContext.GetService<IAgentCredentialStore>();
                    ClientCertificatePassword = cerdStore.Read($"VSTS_AGENT_CLIENT_CERT_PASSWORD_{certSetting.ClientCertPasswordLookupKey}").Password;

                    var secretMasker = HostContext.GetService<ISecretMasker>();
                    secretMasker.AddValue(ClientCertificatePassword);

                    _clientCertificates.Clear();
                    _clientCertificates.Add(new X509Certificate2(ClientCertificateArchiveFile, ClientCertificatePassword));
                }
            }
            else
            {
                Trace.Info("No certificate setting found.");
            }
        }

        public string CACertificateFile { private set; get; }
        public string ClientCertificateFile { private set; get; }
        public string ClientCertificatePrivateKeyFile { private set; get; }
        public string ClientCertificateArchiveFile { private set; get; }
        public string ClientCertificatePassword { private set; get; }

        public X509Certificate2Collection ClientCertificates => _clientCertificates;
    }

    [DataContract]
    internal class AgentCertificateSetting
    {
        [DataMember]
        public string CACert { get; set; }

        [DataMember]
        public string ClientCert { get; set; }

        [DataMember]
        public string ClientCertPrivatekey { get; set; }

        [DataMember]
        public string ClientCertArchive { get; set; }

        [DataMember]
        public string ClientCertPasswordLookupKey { get; set; }
    }
}
