#if OS_LINUX || OS_OSX
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Configuration
{
    public class RSAFileKeyManager : RunnerService, IRSAKeyManager
    {
        private string _keyFile;
        private IHostContext _context;

        public RSA CreateKey()
        {
            RSA rsa = null;
            if (!File.Exists(_keyFile))
            {
                Trace.Info("Creating new RSA key using 2048-bit key length");

                rsa = RSA.Create(2048);

                // Now write the parameters to disk
                IOUtil.SaveObject(new RSAParametersSerializable(rsa.ExportParameters(true)), _keyFile);
                Trace.Info("Successfully saved RSA key parameters to file {0}", _keyFile);

                // Try to lock down the credentials_key file to the owner/group
                var chmodPath = WhichUtil.Which("chmod", trace: Trace);
                if (!String.IsNullOrEmpty(chmodPath))
                {
                    var arguments = $"600 {new FileInfo(_keyFile).FullName}";
                    using (var invoker = _context.CreateService<IProcessInvoker>())
                    {
                        var exitCode = invoker.ExecuteAsync(HostContext.GetDirectory(WellKnownDirectory.Root), chmodPath, arguments, null, default(CancellationToken)).GetAwaiter().GetResult();
                        if (exitCode == 0)
                        {
                            Trace.Info("Successfully set permissions for RSA key parameters file {0}", _keyFile);
                        }
                        else
                        {
                            Trace.Warning("Unable to succesfully set permissions for RSA key parameters file {0}. Received exit code {1} from {2}", _keyFile, exitCode, chmodPath);
                        }
                    }
                }
                else
                {
                    Trace.Warning("Unable to locate chmod to set permissions for RSA key parameters file {0}.", _keyFile);
                }
            }
            else
            {
                Trace.Info("Found existing RSA key parameters file {0}", _keyFile);

                rsa = RSA.Create();
                rsa.ImportParameters(IOUtil.LoadObject<RSAParametersSerializable>(_keyFile).RSAParameters);
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
                throw new CryptographicException($"RSA key file {_keyFile} was not found");
            }

            Trace.Info("Loading RSA key parameters from file {0}", _keyFile);

            var parameters = IOUtil.LoadObject<RSAParametersSerializable>(_keyFile).RSAParameters;
            var rsa = RSA.Create();
            rsa.ImportParameters(parameters);
            return rsa;
        }

        void IRunnerService.Initialize(IHostContext context)
        {
            base.Initialize(context);

            _context = context;
            _keyFile = context.GetConfigFile(WellKnownConfigFile.RSACredentials);
        }
    }
}
#endif
