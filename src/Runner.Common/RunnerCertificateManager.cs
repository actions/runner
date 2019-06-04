using System;
using GitHub.Runner.Common.Util;
using System.IO;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Security;
using System.Net.Http;
using GitHub.Services.WebApi;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(RunnerCertificateManager))]
    public interface IRunnerCertificateManager : IRunnerService
    {
        bool SkipServerCertificateValidation { get; }
        string CACertificateFile { get; }
        string ClientCertificateFile { get; }
        string ClientCertificatePrivateKeyFile { get; }
        string ClientCertificateArchiveFile { get; }
        string ClientCertificatePassword { get; }
        IVssClientCertificateManager VssClientCertificateManager { get; }
    }

    public class RunnerCertificateManager : RunnerService, IRunnerCertificateManager
    {
        private RunnerClientCertificateManager _runnerClientCertificateManager = new RunnerClientCertificateManager();

        public bool SkipServerCertificateValidation { private set; get; }
        public string CACertificateFile { private set; get; }
        public string ClientCertificateFile { private set; get; }
        public string ClientCertificatePrivateKeyFile { private set; get; }
        public string ClientCertificateArchiveFile { private set; get; }
        public string ClientCertificatePassword { private set; get; }
        public IVssClientCertificateManager VssClientCertificateManager => _runnerClientCertificateManager;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            LoadCertificateSettings();
        }

        // This should only be called from config
        public void SetupCertificate(bool skipCertValidation, string caCert, string clientCert, string clientCertPrivateKey, string clientCertArchive, string clientCertPassword)
        {
            Trace.Info("Setup runner certificate setting base on configuration inputs.");

            if (skipCertValidation)
            {
                Trace.Info("Ignore SSL server certificate validation error");
                SkipServerCertificateValidation = true;
                VssClientHttpRequestSettings.Default.ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

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

                Trace.Info($"Client cert '{clientCert}'");
                Trace.Info($"Client cert private key '{clientCertPrivateKey}'");
                Trace.Info($"Client cert archive '{clientCertArchive}'");
            }

            CACertificateFile = caCert;
            ClientCertificateFile = clientCert;
            ClientCertificatePrivateKeyFile = clientCertPrivateKey;
            ClientCertificateArchiveFile = clientCertArchive;
            ClientCertificatePassword = clientCertPassword;

            _runnerClientCertificateManager.AddClientCertificate(ClientCertificateArchiveFile, ClientCertificatePassword);
        }

        // This should only be called from config
        public void SaveCertificateSetting()
        {
            string certSettingFile = HostContext.GetConfigFile(WellKnownConfigFile.Certificates);
            IOUtil.DeleteFile(certSettingFile);

            var setting = new RunnerCertificateSetting();
            if (SkipServerCertificateValidation)
            {
                Trace.Info($"Store Skip ServerCertificateValidation setting to '{certSettingFile}'");
                setting.SkipServerCertValidation = true;
            }

            if (!string.IsNullOrEmpty(CACertificateFile))
            {
                Trace.Info($"Store CA cert setting to '{certSettingFile}'");
                setting.CACert = CACertificateFile;
            }

            if (!string.IsNullOrEmpty(ClientCertificateFile) &&
                !string.IsNullOrEmpty(ClientCertificatePrivateKeyFile) &&
                !string.IsNullOrEmpty(ClientCertificateArchiveFile))
            {
                Trace.Info($"Store client cert settings to '{certSettingFile}'");

                setting.ClientCert = ClientCertificateFile;
                setting.ClientCertPrivatekey = ClientCertificatePrivateKeyFile;
                setting.ClientCertArchive = ClientCertificateArchiveFile;

                if (!string.IsNullOrEmpty(ClientCertificatePassword))
                {
                    string lookupKey = Guid.NewGuid().ToString("D").ToUpperInvariant();
                    Trace.Info($"Store client cert private key password with lookup key {lookupKey}");

                    var credStore = HostContext.GetService<IRunnerCredentialStore>();
                    credStore.Write($"GITHUB_ACTIONS_RUNNER_CLIENT_CERT_PASSWORD_{lookupKey}", "VSTS", ClientCertificatePassword);

                    setting.ClientCertPasswordLookupKey = lookupKey;
                }
            }

            if (SkipServerCertificateValidation ||
                !string.IsNullOrEmpty(CACertificateFile) ||
                !string.IsNullOrEmpty(ClientCertificateFile))
            {
                IOUtil.SaveObject(setting, certSettingFile);
                File.SetAttributes(certSettingFile, File.GetAttributes(certSettingFile) | FileAttributes.Hidden);
            }
        }

        // This should only be called from unconfig
        public void DeleteCertificateSetting()
        {
            string certSettingFile = HostContext.GetConfigFile(WellKnownConfigFile.Certificates);
            if (File.Exists(certSettingFile))
            {
                Trace.Info($"Load runner certificate setting from '{certSettingFile}'");
                var certSetting = IOUtil.LoadObject<RunnerCertificateSetting>(certSettingFile);

                if (certSetting != null && !string.IsNullOrEmpty(certSetting.ClientCertPasswordLookupKey))
                {
                    Trace.Info("Delete client cert private key password from credential store.");
                    var credStore = HostContext.GetService<IRunnerCredentialStore>();
                    credStore.Delete($"GITHUB_ACTIONS_RUNNER_CLIENT_CERT_PASSWORD_{certSetting.ClientCertPasswordLookupKey}");
                }

                Trace.Info($"Delete cert setting file: {certSettingFile}");
                IOUtil.DeleteFile(certSettingFile);
            }
        }

        public void LoadCertificateSettings()
        {
            string certSettingFile = HostContext.GetConfigFile(WellKnownConfigFile.Certificates);
            if (File.Exists(certSettingFile))
            {
                Trace.Info($"Load runner certificate setting from '{certSettingFile}'");
                var certSetting = IOUtil.LoadObject<RunnerCertificateSetting>(certSettingFile);
                ArgUtil.NotNull(certSetting, nameof(RunnerCertificateSetting));

                if (certSetting.SkipServerCertValidation)
                {
                    Trace.Info("Ignore SSL server certificate validation error");
                    SkipServerCertificateValidation = true;
                    VssClientHttpRequestSettings.Default.ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                if (!string.IsNullOrEmpty(certSetting.CACert))
                {
                    // make sure all settings file exist
                    ArgUtil.File(certSetting.CACert, nameof(certSetting.CACert));
                    Trace.Info($"CA '{certSetting.CACert}'");
                    CACertificateFile = certSetting.CACert;
                }

                if (!string.IsNullOrEmpty(certSetting.ClientCert))
                {
                    // make sure all settings file exist
                    ArgUtil.File(certSetting.ClientCert, nameof(certSetting.ClientCert));
                    ArgUtil.File(certSetting.ClientCertPrivatekey, nameof(certSetting.ClientCertPrivatekey));
                    ArgUtil.File(certSetting.ClientCertArchive, nameof(certSetting.ClientCertArchive));

                    Trace.Info($"Client cert '{certSetting.ClientCert}'");
                    Trace.Info($"Client cert private key '{certSetting.ClientCertPrivatekey}'");
                    Trace.Info($"Client cert archive '{certSetting.ClientCertArchive}'");

                    ClientCertificateFile = certSetting.ClientCert;
                    ClientCertificatePrivateKeyFile = certSetting.ClientCertPrivatekey;
                    ClientCertificateArchiveFile = certSetting.ClientCertArchive;

                    if (!string.IsNullOrEmpty(certSetting.ClientCertPasswordLookupKey))
                    {
                        var cerdStore = HostContext.GetService<IRunnerCredentialStore>();
                        ClientCertificatePassword = cerdStore.Read($"GITHUB_ACTIONS_RUNNER_CLIENT_CERT_PASSWORD_{certSetting.ClientCertPasswordLookupKey}").Password;
                        HostContext.SecretMasker.AddValue(ClientCertificatePassword);
                    }

                    _runnerClientCertificateManager.AddClientCertificate(ClientCertificateArchiveFile, ClientCertificatePassword);
                }
            }
            else
            {
                Trace.Info("No certificate setting found.");
            }
        }
    }

    [DataContract]
    internal class RunnerCertificateSetting
    {
        [DataMember]
        public bool SkipServerCertValidation { get; set; }

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
