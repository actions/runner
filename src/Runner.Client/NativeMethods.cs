using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Runner.Client
{
    partial class Program
    {
        public static class NativeMethods
        {
            private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            private const uint FILE_READ_EA = 0x0008;
            private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool CloseHandle(IntPtr hObject);

            public static Int32 FSCTL_GET_REPARSE_POINT = ( ((0x00000009) << 16) | ((0) << 14) | ((42) << 2) | (0) );
            public static uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

            [StructLayout(LayoutKind.Sequential)]
            class REPARSE_DATA_BUFFER {
                public uint ReparseTag;
                public ushort ReparseDataLength;
                public ushort Reserved;
                public ushort SubstituteNameOffset;
                public ushort SubstituteNameLength;
                public ushort PrintNameOffset;
                public ushort PrintNameLength;
                public uint Flags;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
                public byte[] PathBuffer;
            }


            [DllImport("kernel32.dll")]
            public static extern byte DeviceIoControl(IntPtr hDevice, Int32 dwIoControlCode, IntPtr lpInBuffer, Int32 nInBufferSize, IntPtr lpOutBuffer, Int32 nOutBufferSize, ref Int32 lpBytesReturned, IntPtr lpOverlapped);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CreateFile(
                    [MarshalAs(UnmanagedType.LPTStr)] string filename,
                    [MarshalAs(UnmanagedType.U4)] uint access,
                    [MarshalAs(UnmanagedType.U4)] FileShare share,
                    IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                    [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                    [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
                    IntPtr templateFile);

            public static string GetFinalPathName(string path)
            {
                var h = CreateFile(path, 
                    FILE_READ_EA, 
                    FileShare.ReadWrite | FileShare.Delete, 
                    IntPtr.Zero, 
                    FileMode.Open, 
                    FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);
                if (h == INVALID_HANDLE_VALUE)
                    throw new Win32Exception();

                try
                {
                    var sb = new StringBuilder(1024);
                    var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
                    if (res == 0)
                        throw new Win32Exception();

                    return sb.ToString();
                }
                finally
                {
                    CloseHandle(h);
                }
            }

            private static uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

            public static string ReadSymlink(string path)
            {
                var h = CreateFile(path, 
                    FILE_READ_EA, 
                    FileShare.ReadWrite | FileShare.Delete, 
                    IntPtr.Zero, 
                    FileMode.Open, 
                    FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                    IntPtr.Zero);
                if (h == INVALID_HANDLE_VALUE)
                    throw new Win32Exception();

                try
                {
                    REPARSE_DATA_BUFFER rdb = new REPARSE_DATA_BUFFER();
                    var buf = Marshal.AllocHGlobal(Marshal.SizeOf(rdb));
                    int size = 0;
                    var res = DeviceIoControl(h, FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, buf, Marshal.SizeOf(rdb), ref size, IntPtr.Zero);
                    if (res == 0)
                        throw new Win32Exception();
                    Marshal.PtrToStructure<REPARSE_DATA_BUFFER>(buf, rdb);
                    if(rdb.ReparseTag != IO_REPARSE_TAG_SYMLINK) {
                        throw new Exception("Invalid reparse point, only symlinks are supported");
                    }
                    //var sres = Encoding.Unicode.GetString(rdb.PathBuffer, rdb.PrintNameOffset, rdb.PrintNameLength);
                    var sres2 = Encoding.Unicode.GetString(rdb.PathBuffer, rdb.SubstituteNameOffset, rdb.SubstituteNameLength);
                    return sres2;
                }
                finally
                {
                    CloseHandle(h);
                }
            }
        }
    }
}
