using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator (Default = typeof(NativeWindowsServiceHelper))]
    public interface INativeWindowsServiceHelper : IAgentService
    {
        string GetUniqueBuildGroupName();

        bool LocalGroupExists(string groupName);

        void CreateLocalGroup(string groupName);

        void AddMemberToLocalGroup(string accountName, string groupName);

        void GrantFullControlToGroup(string path, string groupName);

        bool CheckUserHasLogonAsServicePrivilege(string domain, string userName);

        bool GrantUserLogonAsServicePrivilage(string domain, string userName);

        bool IsValidCredential(string domain, string userName, string logonPassword);

        NTAccount GetDefaultServiceAccount();

        void SetPermissionForAccount(string path, string accountName);

        ServiceController TryGetServiceController(string serviceName);

        void InstallService(string serviceName, string serviceDisplayName, string logonAccount, string logonPassword);
    }

    public class NativeWindowsServiceHelper : AgentService, INativeWindowsServiceHelper
    {
        // TODO: Change it to VSTS_AgentService_G?
        private const string AgentServiceLocalGroupPrefix = "TFS_BuildService_G";
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }

        public string GetUniqueBuildGroupName()
        {
            return AgentServiceLocalGroupPrefix + GetHashCodeForAgent().Substring(0, 5);
        }

        public ServiceController TryGetServiceController(string serviceName)
        {
            Trace.Entering();

            return
                ServiceController.GetServices()
                    .FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        // TODO: Make sure to remove Old agent's group and registry changes made during auto upgrade to vsts-agent.
        public bool LocalGroupExists(string groupName)
        {
            Trace.Entering();
            bool exists = false;

            IntPtr bufptr;
            int returnCode = NetLocalGroupGetInfo(null, groupName, 1, out bufptr);

            try
            {
                switch (returnCode)
                {
                    case ReturnCode.S_OK:
                        exists = true;
                        break;

                    case ReturnCode.NERR_GroupNotFound:
                    case ReturnCode.ERROR_NO_SUCH_ALIAS:
                        exists = false;
                        break;

                    case ReturnCode.ERROR_ACCESS_DENIED:
                        // NOTE: None of the exception thrown here are userName facing. The caller logs this exception and prints a more understandable error
                        throw new UnauthorizedAccessException(StringUtil.Loc("AccessDenied"));

                    default:
                        throw new Exception(StringUtil.Loc("OperationFailed", nameof(NetLocalGroupGetInfo), returnCode));
                }
            }
            finally
            {
                // we don't need to actually read the info to determine whether it exists
                int bufferFreeError = NetApiBufferFree(bufptr);
                if (bufferFreeError != 0)
                {
                    Trace.Error(StringUtil.Format("Buffer free error, could not free buffer allocated, error code: {0}", bufferFreeError));
                }
            }

            return exists;
        }

        public void CreateLocalGroup(string groupName)
        {
            Trace.Entering();
            LocalGroupInfo groupInfo = new LocalGroupInfo();
            groupInfo.Name = groupName;
            groupInfo.Comment = StringUtil.Format("Built-in group used by Team Foundation Server.");

            int returnCode = NetLocalGroupAdd(null, 1, ref groupInfo, 0);

            // return on success
            if (returnCode == ReturnCode.S_OK)
            {
                return;
            }

            // Error Cases
            switch (returnCode)
            {
                case ReturnCode.NERR_GroupExists:
                case ReturnCode.ERROR_ALIAS_EXISTS:
                    Trace.Info(StringUtil.Format("Group {0} already exists", groupName));
                    break;
                case ReturnCode.ERROR_ACCESS_DENIED:
                    throw new UnauthorizedAccessException(StringUtil.Loc("AccessDenied"));

                case ReturnCode.ERROR_INVALID_PARAMETER:
                    throw new ArgumentException(StringUtil.Loc("InvalidGroupName", groupName));

                default:
                    throw new Exception(StringUtil.Loc("OperationFailed", nameof(NetLocalGroupAdd), returnCode));
            }
        }

        public void AddMemberToLocalGroup(string accountName, string groupName)
        {
            Trace.Entering();
            LocalGroupMemberInfo memberInfo = new LocalGroupMemberInfo();
            memberInfo.FullName = accountName;

            int returnCode = NetLocalGroupAddMembers(null, groupName, 3, ref memberInfo, 1);

            // return on success
            if (returnCode == ReturnCode.S_OK)
            {
                return;
            }

            // Error Cases
            switch (returnCode)
            {
                case ReturnCode.ERROR_MEMBER_IN_ALIAS:
                    Trace.Info(StringUtil.Format("Account {0} is already member of group {1}", accountName, groupName));
                    break;
                case ReturnCode.NERR_GroupNotFound:
                case ReturnCode.ERROR_NO_SUCH_ALIAS:
                    throw new ArgumentException(StringUtil.Loc("GroupDoesNotExists", groupName));

                case ReturnCode.ERROR_NO_SUCH_MEMBER:
                    throw new ArgumentException(StringUtil.Loc("MemberDoesNotExists", accountName));

                case ReturnCode.ERROR_INVALID_MEMBER:
                    throw new ArgumentException(StringUtil.Loc("InvalidMember"));

                case ReturnCode.ERROR_ACCESS_DENIED:
                    throw new UnauthorizedAccessException(StringUtil.Loc("AccessDenied"));

                default:
                    throw new Exception(StringUtil.Loc("OperationFailed", nameof(NetLocalGroupAddMembers), returnCode));
            }
        }

        public void GrantFullControlToGroup(string path, string groupName)
        {
            Trace.Entering();
            DirectoryInfo dInfo = new DirectoryInfo(path);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();

            if (!dSecurity.AreAccessRulesCanonical)
            {
                Trace.Warning("Acls are not canonical, this may cause failure");
            }

            dSecurity.AddAccessRule(
                new FileSystemAccessRule(
                    groupName,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
        }

        public bool CheckUserHasLogonAsServicePrivilege(string domain, string userName)
        {
            Trace.Entering();

            ArgUtil.NotNullOrEmpty(userName, nameof(userName));
            bool userHasPermission = false;

            using (LsaPolicy lsaPolicy = new LsaPolicy())
            {
                IntPtr rightsPtr;
                uint count;
                uint result = LsaEnumerateAccountRights(lsaPolicy.Handle, GetSidBinaryFromWindows(domain, userName), out rightsPtr, out count);
                try
                {
                    if (result == 0)
                    {
                        IntPtr incrementPtr = rightsPtr;
                        for (int i = 0; i < count; i++)
                        {
                            LSA_UNICODE_STRING nativeRightString = Marshal.PtrToStructure<LSA_UNICODE_STRING>(incrementPtr);
                            string rightString = Marshal.PtrToStringAnsi(nativeRightString.Buffer);
                            if (string.Equals(rightString, s_logonAsServiceName, StringComparison.OrdinalIgnoreCase))
                            {
                                userHasPermission = true;
                            }

                            incrementPtr += Marshal.SizeOf(nativeRightString);
                        }
                    }
                }
                finally
                {
                    result = LsaFreeMemory(rightsPtr);
                    if (result != 0)
                    {
                        Trace.Error(StringUtil.Format("Failed to free memory from LsaEnumerateAccountRights. Return code : {0} ", result));
                    }
                }
            }
            return userHasPermission;
        }

        public bool GrantUserLogonAsServicePrivilage(string domain, string userName)
        {
            Trace.Entering();
            IntPtr lsaPolicyHandle = IntPtr.Zero;

            ArgUtil.NotNullOrEmpty(userName, nameof(userName));

            try
            {
                LSA_UNICODE_STRING system = new LSA_UNICODE_STRING();
                LSA_OBJECT_ATTRIBUTES attrib = new LSA_OBJECT_ATTRIBUTES()
                {
                    Length = 0,
                    RootDirectory = IntPtr.Zero,
                    Attributes = 0,
                    SecurityDescriptor = IntPtr.Zero,
                    SecurityQualityOfService = IntPtr.Zero,
                };

                uint result = LsaOpenPolicy(ref system, ref attrib, LSA_POLICY_ALL_ACCESS, out lsaPolicyHandle);
                if (result != 0 || lsaPolicyHandle == IntPtr.Zero)
                {
                    if (result == ReturnCode.STATUS_ACCESS_DENIED)
                    {
                        throw new Exception(StringUtil.Loc("ShouldBeAdmin"));
                    }
                    throw new Exception(StringUtil.Loc("OperationFailed", nameof(LsaOpenPolicy), result));
                }

                result = LsaAddAccountRights(lsaPolicyHandle, GetSidBinaryFromWindows(domain, userName), LogonAsServiceRights, 1);
                Trace.Info("LsaAddAccountRights return with error code {0} ", result);

                return result == 0;
            }
            finally
            {
                int result = LsaClose(lsaPolicyHandle);
                if (result != 0)
                {
                    Trace.Error(StringUtil.Format("Can not close LasPolicy handler. LsaClose failed with error code {0}", result));
                }
            }
        }

        public static void GetAccountSegments(string account, out string domain, out string user)
        {
            string[] segments = account.Split('\\');
            domain = string.Empty;
            user = account;
            if (segments.Length == 2)
            {
                domain = segments[0];
                user = segments[1];
            }
        }

        public bool IsValidCredential(string domain, string userName, string logonPassword)
        {
            Trace.Entering();
            IntPtr tokenHandle = IntPtr.Zero;

            ArgUtil.NotNullOrEmpty(userName, nameof(userName));

            Trace.Verbose(StringUtil.Format("Received domain {0} and username {1} from logonaccount", domain, userName));
            int result = LogonUser(userName, domain, logonPassword, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, out tokenHandle);

            if (tokenHandle.ToInt32() != 0)
            {
                if (!CloseHandle(tokenHandle))
                {
                    Trace.Error("Failed during CloseHandle on token from LogonUser");
                    throw new InvalidOperationException(StringUtil.Loc("CanNotVerifyLogonAccountPassword"));
                }
            }

            Trace.Verbose(StringUtil.Format("LogonUser returned with result {0}", result));
            return result != 0;
        }

        public NTAccount GetDefaultServiceAccount()
        {
            SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, domainSid: null);
            NTAccount account = sid.Translate(typeof(NTAccount)) as NTAccount;

            if (account == null)
            {
                throw new InvalidOperationException(StringUtil.Loc("NetworkServiceNotFound"));
            }

            return account;
        }

        public void SetPermissionForAccount(string path, string accountName)
        {
            Trace.Entering();

            string groupName = GetUniqueBuildGroupName();

            Trace.Info(StringUtil.Format("Calculated unique group name {0}", groupName));
            if (!LocalGroupExists(groupName))
            {
                Trace.Info(StringUtil.Format("Trying to create group {0}", groupName));
                CreateLocalGroup(groupName);
            }

            Trace.Info(StringUtil.Format("Trying to add userName {0} to the group {0}", accountName, groupName));
            AddMemberToLocalGroup(accountName, groupName);

            Trace.Info(StringUtil.Format("Set full access control to group for the folder {0}", path));
            // TODO Check if permission exists
            GrantFullControlToGroup(path, groupName);
        }

        public void InstallService(string serviceName, string serviceDisplayName, string logonAccount, string logonPassword)
        {
            Trace.Entering();

            string agentServiceExecutable = Path.Combine(IOUtil.GetBinPath(), WindowsServiceControlManager.WindowsServiceControllerName);
            IntPtr scmHndl = OpenSCManager(null, null, ServiceManagerRights.AllAccess);

            if (scmHndl.ToInt64() <= 0)
            {
                throw new Exception("Failed to Open Service Control Manager");
            }

            try
            {
                Trace.Verbose(StringUtil.Format("Opened SCManager. Trying to create service {0}", serviceName));

                IntPtr serviceHndl = CreateService(
                                        scmHndl,
                                        serviceName,
                                        serviceDisplayName,
                                        ServiceRights.QueryStatus | ServiceRights.Start,
                                        SERVICE_WIN32_OWN_PROCESS,
                                        ServiceBootFlag.AutoStart,
                                        ServiceError.Normal,
                                        agentServiceExecutable,
                                        null,
                                        IntPtr.Zero,
                                        null,
                                        logonAccount,
                                        logonPassword);

                if (serviceHndl.ToInt64() <= 0)
                {
                    throw new InvalidOperationException(StringUtil.Loc("OperationFailed", nameof(CreateService), GetLastError()));
                }

                //invoke the service with special argument, that tells it to register an event log trace source (need to run as an admin)
                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                {
                    processInvoker.ExecuteAsync(string.Empty, agentServiceExecutable, "init", null, default(System.Threading.CancellationToken)).GetAwaiter().GetResult();                    
                }

                _term.WriteLine(StringUtil.Loc("ServiceConfigured", serviceName));
                CloseServiceHandle(serviceHndl);
            }
            finally
            {
                CloseServiceHandle(scmHndl);
            }
        }

        private byte[] GetSidBinaryFromWindows(string domain, string user)
        {
            try
            {
                SecurityIdentifier sid = (SecurityIdentifier)new NTAccount(StringUtil.Format("{0}\\{1}", domain, user).TrimStart('\\')).Translate(typeof(SecurityIdentifier));
                byte[] binaryForm = new byte[sid.BinaryLength];
                sid.GetBinaryForm(binaryForm, 0);
                return binaryForm;
            }
            catch (Exception exception)
            {
                Trace.Error(exception);
                return null;
            }
        }

        private string GetHashCodeForAgent(string hashString = null)
        {
            Trace.Entering();
            if (string.IsNullOrEmpty(hashString))
            {
                hashString = IOUtil.GetBinPath().ToLowerInvariant();
            }

            
            using (SHA256 sha256hash = SHA256.Create())
            {
                byte[] data = sha256hash.ComputeHash(Encoding.UTF8.GetBytes(hashString));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                string hash = sBuilder.ToString();
                Trace.Info("Bin path location hash = {0}", hash);
                return hash;
            }
        }

        // Helper class not to repeat whenever we deal with LSA* api
        internal class LsaPolicy : IDisposable
        {
            public IntPtr Handle { get; set; }

            public LsaPolicy()
            {
                LSA_UNICODE_STRING system = new LSA_UNICODE_STRING();

                LSA_OBJECT_ATTRIBUTES attrib = new LSA_OBJECT_ATTRIBUTES()
                {
                    Length = 0,
                    RootDirectory = IntPtr.Zero,
                    Attributes = 0,
                    SecurityDescriptor = IntPtr.Zero,
                    SecurityQualityOfService = IntPtr.Zero,
                };

                IntPtr handle = IntPtr.Zero;
                uint result = LsaOpenPolicy(ref system, ref attrib, LSA_POLICY_ALL_ACCESS, out handle);
                if (result != 0 || handle == IntPtr.Zero)
                {
                    if (result == ReturnCode.STATUS_ACCESS_DENIED)
                    {
                        throw new Exception(StringUtil.Loc("ShouldBeAdmin"));
                    }
                    throw new Exception(StringUtil.Loc("OperationFailed", nameof(LsaOpenPolicy), result));
                }

                Handle = handle;
            }

            void IDisposable.Dispose()
            {
                int result = LsaClose(Handle);
                if (result != 0)
                {
                    throw new Exception(StringUtil.Format("OperationFailed", nameof(LsaClose), result));
                }

                GC.SuppressFinalize(this);
            }
        }

        // Declaration of external pinvoke functions
        private static readonly uint LSA_POLICY_ALL_ACCESS = 0x1FFF;
        private static readonly string s_logonAsServiceName = "SeServiceLogonRight";

        private const UInt32 LOGON32_LOGON_NETWORK = 3;
        private const UInt32 LOGON32_PROVIDER_DEFAULT = 0;

        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        public const int SERVICE_NO_CHANGE = -1;

        // TODO Fix this. This is not yet available in coreclr (newer version?)
        private const int UnicodeCharSize = 2;

        private static LSA_UNICODE_STRING[] LogonAsServiceRights
        {
            get
            {
                return new[]
                           {
                               new LSA_UNICODE_STRING()
                                   {
                                       Buffer = Marshal.StringToHGlobalUni(s_logonAsServiceName),
                                       Length = (UInt16)(s_logonAsServiceName.Length * UnicodeCharSize),
                                       MaximumLength = (UInt16) ((s_logonAsServiceName.Length + 1) * UnicodeCharSize)
                                   }
                           };
            }
        }

        public struct ReturnCode
        {
            public const int S_OK = 0;
            public const int ERROR_ACCESS_DENIED = 5;
            public const int ERROR_INVALID_PARAMETER = 87;
            public const int ERROR_MEMBER_NOT_IN_ALIAS = 1377; // member not in a group            
            public const int ERROR_MEMBER_IN_ALIAS = 1378; // member already exists
            public const int ERROR_ALIAS_EXISTS = 1379;  // group already exists
            public const int ERROR_NO_SUCH_ALIAS = 1376;
            public const int ERROR_NO_SUCH_MEMBER = 1387;
            public const int ERROR_INVALID_MEMBER = 1388;
            public const int NERR_GroupNotFound = 2220;
            public const int NERR_GroupExists = 2223;
            public const int NERR_UserInGroup = 2236;
            public const uint STATUS_ACCESS_DENIED = 0XC0000022; //NTSTATUS error code: Access Denied
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LocalGroupInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_UNICODE_STRING
        {
            public UInt16 Length;
            public UInt16 MaximumLength;

            // We need to use an IntPtr because if we wrap the Buffer with a SafeHandle-derived class, we get a failure during LsaAddAccountRights
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LocalGroupMemberInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string FullName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_OBJECT_ATTRIBUTES
        {
            public UInt32 Length;
            public IntPtr RootDirectory;
            public LSA_UNICODE_STRING ObjectName;
            public UInt32 Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [Flags]
        public enum ServiceManagerRights
        {
            Connect = 0x0001,
            CreateService = 0x0002,
            EnumerateService = 0x0004,
            Lock = 0x0008,
            QueryLockStatus = 0x0010,
            ModifyBootConfig = 0x0020,
            StandardRightsRequired = 0xF0000,
            AllAccess =
                (StandardRightsRequired | Connect | CreateService | EnumerateService | Lock | QueryLockStatus
                 | ModifyBootConfig)
        }

        [Flags]
        public enum ServiceRights
        {
            QueryConfig = 0x1,
            ChangeConfig = 0x2,
            QueryStatus = 0x4,
            EnumerateDependants = 0x8,
            Start = 0x10,
            Stop = 0x20,
            PauseContinue = 0x40,
            Interrogate = 0x80,
            UserDefinedControl = 0x100,
            Delete = 0x00010000,
            StandardRightsRequired = 0xF0000,
            AllAccess =
                (StandardRightsRequired | QueryConfig | ChangeConfig | QueryStatus | EnumerateDependants | Start | Stop
                 | PauseContinue | Interrogate | UserDefinedControl)
        }

        public enum ServiceError
        {
            Ignore = 0x00000000,
            Normal = 0x00000001,
            Severe = 0x00000002,
            Critical = 0x00000003
        }

        public enum ServiceBootFlag
        {
            Start = 0x00000000,
            SystemStart = 0x00000001,
            AutoStart = 0x00000002,
            DemandStart = 0x00000003,
            Disabled = 0x00000004
        }

        [DllImport("Netapi32.dll")]
        private extern static int NetLocalGroupGetInfo(string servername,
                                                 string groupname,
                                                 int level,
                                                 out IntPtr bufptr);

        [DllImport("Netapi32.dll")]
        private extern static int NetApiBufferFree(IntPtr Buffer);


        [DllImport("Netapi32.dll")]
        private extern static int NetLocalGroupAdd([MarshalAs(UnmanagedType.LPWStr)] string servername,
                                                         int level,
                                                         ref LocalGroupInfo buf,
                                                         int parm_err);

        [DllImport("Netapi32.dll")]
        private extern static int NetLocalGroupAddMembers([MarshalAs(UnmanagedType.LPWStr)] string serverName,
                                                         [MarshalAs(UnmanagedType.LPWStr)] string groupName,
                                                         int level,
                                                         ref LocalGroupMemberInfo buf,
                                                         int totalEntries);

        [DllImport("advapi32.dll")]
        private static extern Int32 LsaClose(IntPtr ObjectHandle);

        [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
        private static extern uint LsaOpenPolicy(
            ref LSA_UNICODE_STRING SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            uint DesiredAccess,
            out IntPtr PolicyHandle);

        [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
        private static extern uint LsaAddAccountRights(
           IntPtr PolicyHandle,
           byte[] AccountSid,
           LSA_UNICODE_STRING[] UserRights,
           uint CountOfRights);

        [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
        public static extern uint LsaEnumerateAccountRights(
          IntPtr PolicyHandle,
          byte[] AccountSid,
          out IntPtr UserRights,
          out uint CountOfRights);

        [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
        public static extern uint LsaFreeMemory(IntPtr pBuffer);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int LogonUser(string userName, string domain, string password, uint logonType, uint logonProvider, out IntPtr tokenHandle);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", EntryPoint = "CreateServiceA")]
        private static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            ServiceRights dwDesiredAccess,
            int dwServiceType,
            ServiceBootFlag dwStartType,
            ServiceError dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lp,
            string lpPassword);

        [DllImport("advapi32.dll")]
        public static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, ServiceManagerRights dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceRights dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int DeleteService(IntPtr hService);

        [DllImport("advapi32.dll")]
        public static extern int CloseServiceHandle(IntPtr hSCObject);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

    }
}