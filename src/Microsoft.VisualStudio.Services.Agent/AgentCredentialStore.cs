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
    // The purpose of this class is to store user's credential during agent configuration and retrive the credential back at runtime.
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
    // Windows credential store is per user.
    // This is a limitation for user configure the agent run as windows service, when user's current login account is different with the service run as account.
    // Ex: I login the box as domain\admin, configure the agent as windows service and run as domian\buildserver
    // domain\buildserver won't read the stored credential from domain\admin's windows credential store.
    // To workaround this limitation.
    // Anytime we try to save a credential:
    //   1. store it into current user's windows credential store 
    //   2. use DP-API do a machine level encrypt and store the encrypted content on disk.
    // At the first time we try to read the credential:
    //   1. read from current user's windows credential store, delete the DP-API encrypted backup content on disk if the windows credential store read succeed.
    //   2. if credential not found in current user's windows credential store, read from the DP-API encrypted backup content on disk, 
    //      write the credential back the current user's windows credential store and delete the backup on disk.
    public sealed class WindowsAgentCredentialStore : AgentService, IAgentCredentialStore
    {
        private string _credStoreFile;
        private Dictionary<string, string> _credStore;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _credStoreFile = hostContext.GetConfigFile(WellKnownConfigFile.CredentialStore);
            if (File.Exists(_credStoreFile))
            {
                _credStore = IOUtil.LoadObject<Dictionary<string, string>>(_credStoreFile);
            }
            else
            {
                _credStore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public NetworkCredential Write(string target, string username, string password)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            ArgUtil.NotNullOrEmpty(username, nameof(username));
            ArgUtil.NotNullOrEmpty(password, nameof(password));

            // save to .credential_store file first, then Windows credential store
            string usernameBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));
            string passwordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

            // Base64Username:Base64Password -> DP-API machine level encrypt -> Base64Encoding
            string encryptedUsernamePassword = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes($"{usernameBase64}:{passwordBase64}"), null, DataProtectionScope.LocalMachine));
            Trace.Info($"Credentials for '{target}' written to credential store file.");
            _credStore[target] = encryptedUsernamePassword;

            // save to .credential_store file
            SyncCredentialStoreFile();

            // save to Windows Credential Store
            return WriteInternal(target, username, password);
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
                    Trace.Info($"Credentials for '{target}' read from windows credential store.");

                    // delete from .credential_store file since we are able to read it from windows credential store
                    if (_credStore.Remove(target))
                    {
                        Trace.Info($"Delete credentials for '{target}' from credential store file.");
                        SyncCredentialStoreFile();
                    }

                    return new NetworkCredential(username, password);
                }
                else
                {
                    // Can't read from Windows Credential Store, fail back to .credential_store file
                    if (_credStore.ContainsKey(target) && !string.IsNullOrEmpty(_credStore[target]))
                    {
                        Trace.Info($"Credentials for '{target}' read from credential store file.");

                        // Base64Decode -> DP-API machine level decrypt -> Base64Username:Base64Password -> Base64Decode
                        string decryptedUsernamePassword = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(_credStore[target]), null, DataProtectionScope.LocalMachine));

                        string[] credential = decryptedUsernamePassword.Split(':');
                        if (credential.Length == 2 && !string.IsNullOrEmpty(credential[0]) && !string.IsNullOrEmpty(credential[1]))
                        {
                            string username = Encoding.UTF8.GetString(Convert.FromBase64String(credential[0]));
                            string password = Encoding.UTF8.GetString(Convert.FromBase64String(credential[1]));

                            // store back to windows credential store for current user
                            NetworkCredential creds = WriteInternal(target, username, password);

                            // delete from .credential_store file since we are able to write the credential to windows credential store for current user.
                            if (_credStore.Remove(target))
                            {
                                Trace.Info($"Delete credentials for '{target}' from credential store file.");
                                SyncCredentialStoreFile();
                            }

                            return creds;
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException(nameof(decryptedUsernamePassword));
                        }
                    }

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

            // remove from .credential_store file
            if (_credStore.Remove(target))
            {
                Trace.Info($"Delete credentials for '{target}' from credential store file.");
                SyncCredentialStoreFile();
            }

            // remove from windows credential store
            if (!CredDelete(target, CredentialType.Generic, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to delete credentials for {target}");
            }
            else
            {
                Trace.Info($"Credentials for '{target}' deleted from windows credential store.");
            }
        }

        private NetworkCredential WriteInternal(string target, string username, string password)
        {
            // save to Windows Credential Store
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
                    Trace.Info($"Credentials for '{target}' written to windows credential store.");
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

        private void SyncCredentialStoreFile()
        {
            Trace.Info("Sync in-memory credential store with credential store file.");

            // delete the cred store file first anyway, since it's a readonly file.
            IOUtil.DeleteFile(_credStoreFile);

            // delete cred store file when all creds gone
            if (_credStore.Count == 0)
            {
                return;
            }
            else
            {
                IOUtil.SaveObject(_credStore, _credStoreFile);
                File.SetAttributes(_credStoreFile, File.GetAttributes(_credStoreFile) | FileAttributes.Hidden);
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
        private const string _osxAgentCredStoreKeyChainName = "_VSTS_AGENT_CREDSTORE_INTERNAL_";

        // Keychain requires a password, but this is not intended to add security
        private const string _osxAgentCredStoreKeyChainPassword = "A1DC2A63B3D14817A64619FDDBC92264";

        private string _securityUtil;

        private string _agentCredStoreKeyChain;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _securityUtil = WhichUtil.Which("security", true, Trace);

            _agentCredStoreKeyChain = hostContext.GetConfigFile(WellKnownConfigFile.CredentialStore);

            // Create osx key chain if it doesn't exists.
            if (!File.Exists(_agentCredStoreKeyChain))
            {
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
                    int exitCode = p.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Root),
                                                  fileName: _securityUtil,
                                                  arguments: $"create-keychain -p {_osxAgentCredStoreKeyChainPassword} \"{_agentCredStoreKeyChain}\"",
                                                  environment: null,
                                                  cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                    if (exitCode == 0)
                    {
                        Trace.Info($"Successfully create-keychain for {_agentCredStoreKeyChain}");
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

                        throw new InvalidOperationException($"'security create-keychain' failed with exit code {exitCode}.");
                    }
                }
            }
            else
            {
                // Try unlock and lock the keychain, make sure it's still in good stage
                UnlockKeyChain();
                LockKeyChain();
            }
        }

        public NetworkCredential Write(string target, string username, string password)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            ArgUtil.NotNullOrEmpty(username, nameof(username));
            ArgUtil.NotNullOrEmpty(password, nameof(password));

            try
            {
                UnlockKeyChain();

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
                    int exitCode = p.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Root),
                                                fileName: _securityUtil,
                                                arguments: $"add-generic-password -s {target} -a VSTSAGENT -w {secretForKeyChain} -T \"{_securityUtil}\" \"{_agentCredStoreKeyChain}\"",
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
            finally
            {
                LockKeyChain();
            }
        }

        public NetworkCredential Read(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));

            try
            {
                UnlockKeyChain();

                string username;
                string password;

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

                    int exitCode = p.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Root),
                                                  fileName: _securityUtil,
                                                  arguments: $"find-generic-password -s {target} -a VSTSAGENT -w -g \"{_agentCredStoreKeyChain}\"",
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
            finally
            {
                LockKeyChain();
            }
        }

        public void Delete(string target)
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(target, nameof(target));

            try
            {
                UnlockKeyChain();

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

                    int exitCode = p.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Root),
                                                  fileName: _securityUtil,
                                                  arguments: $"delete-generic-password -s {target} -a VSTSAGENT \"{_agentCredStoreKeyChain}\"",
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
            finally
            {
                LockKeyChain();
            }
        }

        private void UnlockKeyChain()
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(_securityUtil, nameof(_securityUtil));
            ArgUtil.NotNullOrEmpty(_agentCredStoreKeyChain, nameof(_agentCredStoreKeyChain));

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
                int exitCode = p.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Root),
                                              fileName: _securityUtil,
                                              arguments: $"unlock-keychain -p {_osxAgentCredStoreKeyChainPassword} \"{_agentCredStoreKeyChain}\"",
                                              environment: null,
                                              cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                if (exitCode == 0)
                {
                    Trace.Info($"Successfully unlock-keychain for {_agentCredStoreKeyChain}");
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

                    throw new InvalidOperationException($"'security unlock-keychain' failed with exit code {exitCode}.");
                }
            }
        }

        private void LockKeyChain()
        {
            Trace.Entering();
            ArgUtil.NotNullOrEmpty(_securityUtil, nameof(_securityUtil));
            ArgUtil.NotNullOrEmpty(_agentCredStoreKeyChain, nameof(_agentCredStoreKeyChain));

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
                int exitCode = p.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Root),
                                              fileName: _securityUtil,
                                              arguments: $"lock-keychain \"{_agentCredStoreKeyChain}\"",
                                              environment: null,
                                              cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                if (exitCode == 0)
                {
                    Trace.Info($"Successfully lock-keychain for {_agentCredStoreKeyChain}");
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

                    throw new InvalidOperationException($"'security lock-keychain' failed with exit code {exitCode}.");
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

            _credStoreFile = hostContext.GetConfigFile(WellKnownConfigFile.CredentialStore);
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
                // this helps avoid accidental information disclosure, but isn't intended for true security
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
            var chmodPath = WhichUtil.Which("chmod", trace: Trace);
            if (!String.IsNullOrEmpty(chmodPath))
            {
                var arguments = $"600 {new FileInfo(_credStoreFile).FullName}";
                using (var invoker = HostContext.CreateService<IProcessInvoker>())
                {
                    var exitCode = invoker.ExecuteAsync(HostContext.GetDirectory(WellKnownDirectory.Root), chmodPath, arguments, null, default(CancellationToken)).GetAwaiter().GetResult();
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
