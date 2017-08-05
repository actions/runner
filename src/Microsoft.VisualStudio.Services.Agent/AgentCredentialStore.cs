using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Microsoft.VisualStudio.Services.Agent
{
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(WindowsAgentCredentialStore))]
#elif OS_OSX
    [ServiceLocator(Default = typeof(MacOSAgentCredentialStore))]
#else
    [ServiceLocator(Default = typeof(LinuxAgentCredentialStore))]
#endif
    public interface IAgentCredentialStore : IAgentService
    {
        NetworkCredential Write(string target, string username, string password);

        // throw exception when target not found from cred store
        NetworkCredential Read(string target);

        // throw exception when target not found from cred store
        void Delete(string target);
    }

#if OS_WINDOWS
    public sealed class WindowsAgentCredentialStore : AgentService, IAgentCredentialStore
    {
        public NetworkCredential Write(string target, string username, string password)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            ArgUtil.NotNullOrEmpty(username, nameof(username));
            ArgUtil.NotNullOrEmpty(password, nameof(password));

            Credential credential = new Credential()
            {
                Type = CredentialType.Generic,
                Persist = (UInt32)CredentialPersist.LocalMachine,
                TargetName = Marshal.StringToCoTaskMemUni(target),
                UserName = Marshal.StringToCoTaskMemUni(username),
                CredentialBlob = Marshal.StringToCoTaskMemUni(password),
                CredentialBlobSize = (UInt32)Encoding.Unicode.GetByteCount(password),
                AttributeCount = 0,
                Comment = IntPtr.Zero,
                Attributes = IntPtr.Zero,
                TargetAlias = IntPtr.Zero
            };

            try
            {
                if (CredWrite(ref credential, 0))
                {
                    Trace.Info($"credentials for '{target}' written to store.");
                    return new NetworkCredential(username, password);
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Failed to write credentials");
                }
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(credential.CredentialBlob);
                }
                if (credential.TargetName != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(credential.TargetName);
                }
                if (credential.UserName != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(credential.UserName);
                }
            }
        }

        public NetworkCredential Read(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            IntPtr credPtr = IntPtr.Zero;
            try
            {
                if (CredRead(target, CredentialType.Generic, 0, out credPtr))
                {
                    Credential credStruct = (Credential)Marshal.PtrToStructure(credPtr, typeof(Credential));
                    int passwordLength = (int)credStruct.CredentialBlobSize;
                    string password = passwordLength > 0 ? Marshal.PtrToStringUni(credStruct.CredentialBlob, passwordLength / sizeof(char)) : String.Empty;
                    string username = Marshal.PtrToStringUni(credStruct.UserName);
                    Trace.Info($"Credentials for '{target}' read from store.");
                    return new NetworkCredential(username, password);
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"CredRead throw an error for '{target}'");
                }
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    CredFree(credPtr);
                }
            }
        }

        public void Delete(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));

            if (!CredDelete(target, CredentialType.Generic, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to delete credentials for {target}");
            }
            else
            {
                Trace.Info($"Credentials for '{target}' deleted from store.");
            }
        }

        [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredDelete(string target, CredentialType type, int reservedFlag);

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr CredentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredWrite([In] ref Credential userCredential, [In] UInt32 flags);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        internal static extern bool CredFree([In] IntPtr cred);

        internal enum CredentialPersist : UInt32
        {
            Session = 0x01,
            LocalMachine = 0x02
        }

        internal enum CredentialType : uint
        {
            Generic = 0x01,
            DomainPassword = 0x02,
            DomainCertificate = 0x03
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct Credential
        {
            public UInt32 Flags;
            public CredentialType Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public UInt32 CredentialBlobSize;
            public IntPtr CredentialBlob;
            public UInt32 Persist;
            public UInt32 AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }
    }
#elif OS_OSX
    public sealed class MacOSAgentCredentialStore : AgentService, IAgentCredentialStore
    {
        public NetworkCredential Write(string target, string username, string password)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            ArgUtil.NotNullOrEmpty(username, nameof(username));
            ArgUtil.NotNullOrEmpty(password, nameof(password));

            var whichUtil = HostContext.GetService<IWhichUtil>();
            string securityUtil = whichUtil.Which("security", true);

            // base64encode username + ':' + base64encode password
            // OSX keychain requires you provide -s target and -a username to retrieve password
            // So, we will trade both username and password as 'secret' store into keychain
            string usernameBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));
            string passwordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            string secretForKeyChain = $"{usernameBase64}:{passwordBase64}";

            List<string> securityOut = new List<string>();
            List<string> securityError = new List<string>();
            object outputLock = new object();
            using (var p = HostContext.CreateService<IProcessInvoker>())
            {
                p.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stdout)
                {
                    if (!string.IsNullOrEmpty(stdout.Data))
                    {
                        lock (outputLock)
                        {
                            securityOut.Add(stdout.Data);
                        }
                    }
                };

                p.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stderr)
                {
                    if (!string.IsNullOrEmpty(stderr.Data))
                    {
                        lock (outputLock)
                        {
                            securityError.Add(stderr.Data);
                        }
                    }
                };

                // make sure the 'security' has access to the key so we won't get prompt at runtime.
                int exitCode = p.ExecuteAsync(workingDirectory: IOUtil.GetRootPath(),
                                              fileName: securityUtil,
                                              arguments: $"add-generic-password -s {target} -a VSTSAGENT -w {secretForKeyChain} -T \"{securityUtil}\"",
                                              environment: null,
                                              cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                if (exitCode == 0)
                {
                    Trace.Info($"Successfully add-generic-password for {target} (VSTSAGENT)");
                }
                else
                {
                    if (securityOut.Count > 0)
                    {
                        Trace.Error(string.Join(Environment.NewLine, securityOut));
                    }
                    if (securityError.Count > 0)
                    {
                        Trace.Error(string.Join(Environment.NewLine, securityError));
                    }

                    throw new InvalidOperationException($"'security add-generic-password' failed with exit code {exitCode}.");
                }
            }

            return new NetworkCredential(username, password);
        }

        public NetworkCredential Read(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            string username;
            string password;

            var whichUtil = HostContext.GetService<IWhichUtil>();
            string securityUtil = whichUtil.Which("security", true);

            List<string> securityOut = new List<string>();
            List<string> securityError = new List<string>();
            object outputLock = new object();
            using (var p = HostContext.CreateService<IProcessInvoker>())
            {
                p.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stdout)
                {
                    if (!string.IsNullOrEmpty(stdout.Data))
                    {
                        lock (outputLock)
                        {
                            securityOut.Add(stdout.Data);
                        }
                    }
                };

                p.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stderr)
                {
                    if (!string.IsNullOrEmpty(stderr.Data))
                    {
                        lock (outputLock)
                        {
                            securityError.Add(stderr.Data);
                        }
                    }
                };

                int exitCode = p.ExecuteAsync(workingDirectory: IOUtil.GetRootPath(),
                                                fileName: securityUtil,
                                                arguments: $"find-generic-password -s {target} -a VSTSAGENT -w -g",
                                                environment: null,
                                                cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                if (exitCode == 0)
                {
                    string keyChainSecret = securityOut.First();
                    string[] secrets = keyChainSecret.Split(':');
                    if (secrets.Length == 2 && !string.IsNullOrEmpty(secrets[0]) && !string.IsNullOrEmpty(secrets[1]))
                    {
                        Trace.Info($"Successfully find-generic-password for {target} (VSTSAGENT)");
                        username = Encoding.UTF8.GetString(Convert.FromBase64String(secrets[0]));
                        password = Encoding.UTF8.GetString(Convert.FromBase64String(secrets[1]));
                        return new NetworkCredential(username, password);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(keyChainSecret));
                    }
                }
                else
                {
                    if (securityOut.Count > 0)
                    {
                        Trace.Error(string.Join(Environment.NewLine, securityOut));
                    }
                    if (securityError.Count > 0)
                    {
                        Trace.Error(string.Join(Environment.NewLine, securityError));
                    }

                    throw new InvalidOperationException($"'security find-generic-password' failed with exit code {exitCode}.");
                }
            }
        }

        public void Delete(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));

            var whichUtil = HostContext.GetService<IWhichUtil>();
            string securityUtil = whichUtil.Which("security", true);

            List<string> securityOut = new List<string>();
            List<string> securityError = new List<string>();
            object outputLock = new object();

            using (var p = HostContext.CreateService<IProcessInvoker>())
            {
                p.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stdout)
                {
                    if (!string.IsNullOrEmpty(stdout.Data))
                    {
                        lock (outputLock)
                        {
                            securityOut.Add(stdout.Data);
                        }
                    }
                };

                p.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stderr)
                {
                    if (!string.IsNullOrEmpty(stderr.Data))
                    {
                        lock (outputLock)
                        {
                            securityError.Add(stderr.Data);
                        }
                    }
                };

                int exitCode = p.ExecuteAsync(workingDirectory: IOUtil.GetRootPath(),
                                              fileName: securityUtil,
                                              arguments: $"delete-generic-password -s {target} -a VSTSAGENT",
                                              environment: null,
                                              cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                if (exitCode == 0)
                {
                    Trace.Info($"Successfully delete-generic-password for {target} (VSTSAGENT)");
                }
                else
                {
                    if (securityOut.Count > 0)
                    {
                        Trace.Error(string.Join(Environment.NewLine, securityOut));
                    }
                    if (securityError.Count > 0)
                    {
                        Trace.Error(string.Join(Environment.NewLine, securityError));
                    }

                    throw new InvalidOperationException($"'security delete-generic-password' failed with exit code {exitCode}.");
                }
            }
        }
    }
#else
    public sealed class LinuxAgentCredentialStore : AgentService, IAgentCredentialStore
    {
        // 'msftvsts' 128 bits iv
        private readonly byte[] iv = new byte[] { 0x36, 0x64, 0x37, 0x33, 0x36, 0x36, 0x37, 0x34, 0x37, 0x36, 0x37, 0x33, 0x37, 0x34, 0x37, 0x33 };

        // 256 bits key
        private byte[] _symmetricKey;
        private string _credStoreFile;
        private Dictionary<string, Credential> _credStore;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _credStoreFile = IOUtil.GetAgentCredStoreFilePath();
            if (File.Exists(_credStoreFile))
            {
                _credStore = IOUtil.LoadObject<Dictionary<string, Credential>>(_credStoreFile);
            }
            else
            {
                _credStore = new Dictionary<string, Credential>(StringComparer.OrdinalIgnoreCase);
            }

            string machineId;
            if (File.Exists("/etc/machine-id"))
            {
                // try use machine-id as encryption key
                machineId = File.ReadAllLines("/etc/machine-id").FirstOrDefault();
                Trace.Info($"machine-id length {machineId?.Length ?? 0}.");

                // machine-id doesn't exist or machine-id is not 256 bits
                if (string.IsNullOrEmpty(machineId) || machineId.Length != 32)
                {
                    Trace.Warning("Can not get valid machine id from '/etc/machine-id'.");
                    machineId = "5f767374735f6167656e745f63726564"; //_vsts_agent_cred
                }
            }
            else
            {
                // /etc/machine-id not exist
                Trace.Warning("/etc/machine-id doesn't exist.");
                machineId = "5f767374735f6167656e745f63726564"; //_vsts_agent_cred
            }

            List<byte> keyBuilder = new List<byte>();
            foreach (var c in machineId)
            {
                keyBuilder.Add(Convert.ToByte(c));
            }

            _symmetricKey = keyBuilder.ToArray();
        }

        public NetworkCredential Write(string target, string username, string password)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            ArgUtil.NotNullOrEmpty(username, nameof(username));
            ArgUtil.NotNullOrEmpty(password, nameof(password));

            Trace.Info($"Store credential for '{target}' to cred store.");
            Credential cred = new Credential(username, Encrypt(password));
            _credStore[target] = cred;
            SyncCredentialStoreFile();
            return new NetworkCredential(username, password);
        }

        public NetworkCredential Read(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            Trace.Info($"Read credential for '{target}' from cred store.");
            if (_credStore.ContainsKey(target))
            {
                Credential cred = _credStore[target];
                if (!string.IsNullOrEmpty(cred.UserName) && !string.IsNullOrEmpty(cred.Password))
                {
                    Trace.Info($"Return credential for '{target}' from cred store.");
                    return new NetworkCredential(cred.UserName, Decrypt(cred.Password));
                }
            }

            throw new KeyNotFoundException(target);
        }

        public void Delete(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));

            if (_credStore.ContainsKey(target))
            {
                Trace.Info($"Delete credential for '{target}' from cred store.");
                _credStore.Remove(target);
                SyncCredentialStoreFile();
            }
            else
            {
                throw new KeyNotFoundException(target);
            }
        }

        private void SyncCredentialStoreFile()
        {
            Trace.Entering();
            Trace.Info("Sync in-memory credential store with credential store file.");

            // delete cred store file when all creds gone
            if (_credStore.Count == 0)
            {
                IOUtil.DeleteFile(_credStoreFile);
                return;
            }

            if (!File.Exists(_credStoreFile))
            {
                CreateCredentialStoreFile();
            }

            IOUtil.SaveObject(_credStore, _credStoreFile);
        }

        private string Encrypt(string secret)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _symmetricKey;
                aes.IV = iv;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aes.CreateEncryptor();

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(secret);
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        private string Decrypt(string encryptedText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _symmetricKey;
                aes.IV = iv;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aes.CreateDecryptor();

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream and place them in a string.
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        private void CreateCredentialStoreFile()
        {
            File.WriteAllText(_credStoreFile, "");
            File.SetAttributes(_credStoreFile, File.GetAttributes(_credStoreFile) | FileAttributes.Hidden);

            // Try to lock down the .credentials_store file to the owner/group
            var whichUtil = HostContext.GetService<IWhichUtil>();
            var chmodPath = whichUtil.Which("chmod");
            if (!String.IsNullOrEmpty(chmodPath))
            {
                var arguments = $"600 {new FileInfo(_credStoreFile).FullName}";
                using (var invoker = HostContext.CreateService<IProcessInvoker>())
                {
                    var exitCode = invoker.ExecuteAsync(IOUtil.GetRootPath(), chmodPath, arguments, null, default(CancellationToken)).GetAwaiter().GetResult();
                    if (exitCode == 0)
                    {
                        Trace.Info("Successfully set permissions for credentials store file {0}", _credStoreFile);
                    }
                    else
                    {
                        Trace.Warning("Unable to successfully set permissions for credentials store file {0}. Received exit code {1} from {2}", _credStoreFile, exitCode, chmodPath);
                    }
                }
            }
            else
            {
                Trace.Warning("Unable to locate chmod to set permissions for credentials store file {0}.", _credStoreFile);
            }
        }
    }

    [DataContract]
    internal class Credential
    {
        public Credential()
        { }

        public Credential(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        [DataMember(IsRequired = true)]
        public string UserName { get; set; }

        [DataMember(IsRequired = true)]
        public string Password { get; set; }
    }
#endif
}
