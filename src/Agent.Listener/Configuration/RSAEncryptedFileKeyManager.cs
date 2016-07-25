#if OS_WINDOWS
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class RSAEncryptedFileKeyManager : AgentService, IRSAKeyManager
    {
        private string _keyFile;
        private IHostContext _context;

        public RSA CreateKey()
        {
            RSA rsa = null;
            if (!File.Exists(_keyFile))
            {
                Trace.Info("Creating new RSA key using 2048-bit key length");

                rsa = RSA.Create();
                rsa.KeySize = 2048;

                // Now write the parameters to disk
                SaveParameters(rsa.ExportParameters(true));
                Trace.Info("Successfully saved RSA key parameters to file {0}", _keyFile);
            }
            else
            {
                Trace.Info("Found existing RSA key parameters file {0}", _keyFile);

                rsa = RSA.Create();
                rsa.ImportParameters(LoadParameters());
            }

            return rsa;
        }

        public void DeleteKey()
        {
            if (File.Exists(_keyFile))
            {
                Trace.Info("Deleting RSA key parameters file {0}", _keyFile);
                File.Delete(_keyFile);
            }
        }

        public RSA GetKey()
        {
            if (!File.Exists(_keyFile))
            {
                throw new CryptographicException(StringUtil.Loc("RSAKeyFileNotFound", _keyFile));
            }

            Trace.Info("Loading RSA key parameters from file {0}", _keyFile);

            var rsa = RSA.Create();
            rsa.ImportParameters(LoadParameters());
            return rsa;
        }

        private RSAParameters LoadParameters()
        {
            var encryptedBytes = File.ReadAllBytes(_keyFile);
            var parametersString = Encoding.UTF8.GetString(ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine));
            return StringUtil.ConvertFromJson<RSAParameters>(parametersString);
        }

        private void SaveParameters(RSAParameters parameters)
        {
            var parametersString = StringUtil.ConvertToJson(parameters);
            var encryptedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(parametersString), null, DataProtectionScope.LocalMachine);
            File.WriteAllBytes(_keyFile, encryptedBytes);
            File.SetAttributes(_keyFile, File.GetAttributes(_keyFile) | FileAttributes.Hidden);
        }

        void IAgentService.Initialize(IHostContext context)
        {
            base.Initialize(context);

            _context = context;
            _keyFile = IOUtil.GetRSACredFilePath();
        }
    }
}
#endif
