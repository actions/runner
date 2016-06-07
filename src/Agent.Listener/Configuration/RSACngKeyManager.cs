#if OS_WINDOWS
using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class RSACngKeyManager : AgentService, IRSAKeyManager
    {
        private string _keyFile;

        public RSA CreateKey()
        {
            String keyName;
            if (File.Exists(_keyFile))
            {
                Trace.Info("Found existing RSA key parameters file {0}", _keyFile);
                keyName = File.ReadAllText(_keyFile);
            }
            else
            {
                keyName = $"VSTSAgent-{Guid.NewGuid().ToString("D")}";

                Trace.Info("Creating new RSA key {0}", keyName);
                File.WriteAllText(_keyFile, keyName);
                Trace.Info("Successfully saved RSA key parameters to file {0}", _keyFile);
                File.SetAttributes(_keyFile, File.GetAttributes(_keyFile) | FileAttributes.Hidden);
            }

            CngKey key = null;
            if (CngKey.Exists(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.UserKey))
            {
                key = CngKey.Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.UserKey);
            }
            else
            {
                var cngParameters = new CngKeyCreationParameters
                {
                    ExportPolicy = CngExportPolicies.None,
                    KeyCreationOptions = CngKeyCreationOptions.None,
                    KeyUsage = CngKeyUsages.Decryption | CngKeyUsages.Signing,
                    Parameters =
                    {
                        new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None),
                    },
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                };

                key = CngKey.Create(CngAlgorithm.Rsa, keyName, cngParameters);
            }

            Trace.Info("Using RSA key {0}", keyName);
            return new RSACng(key);
        }

        public void DeleteKey()
        {
            if (!File.Exists(_keyFile))
            {
                Trace.Info("RSA key parameters file does not exist");
                return;
            }

            var keyName = File.ReadAllText(_keyFile);
            Trace.Info("Deleting RSA key {0}", keyName);

            try
            {
                if (CngKey.Exists(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.UserKey))
                {
                    var key = CngKey.Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.UserKey);
                    if (key != null)
                    {
                        key.Delete();
                    }
                }
            }
            finally
            {
                Trace.Info("Deleting RSA key parameters file {0}", _keyFile);
                File.Delete(_keyFile);
            }
        }

        public RSA GetKey()
        {
            if (!File.Exists(_keyFile))
            {
                throw new CryptographicException(StringUtil.Loc("RSAKeyFileNotFound"));
            }

            Trace.Info("Loading RSA key parameters from file {0}", _keyFile);

            var keyName = File.ReadAllText(_keyFile);

            Trace.Info("Reading RSA key from CSP key container {0}", keyName);
            return new RSACng(CngKey.Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.UserKey));
        }

        void IAgentService.Initialize(IHostContext context)
        {
            base.Initialize(context);

            _keyFile = Path.Combine(IOUtil.GetRootPath(), ".credentials_rsaparams");
        }
    }
}
#endif
