using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public static class FileLink
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateHardLinkW(string newFileName, string existingFileName, IntPtr reservedSecurityAttributes);

        [DllImport("libc", SetLastError = true)]
        internal static extern int link(string existingFileName, string newFileName);

        // Function to create hardlink, based on http://index/?leftProject=Domino.Native&leftSymbol=dwyockoteufx&file=IO%5CWindows%5CFileSystem.Win.cs&line=2289 
        // and http://index/?query=CreateHardLink&rightProject=Microsoft.Build.Tasks.Core&file=NativeMethods.cs&line=790
        public static CreateHardLinkStatus CreateHardLink(string existingFileName, string newFileName)
        {
#if NETSTANDARD
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateHardLinkWin(existingFileName, newFileName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return CreateHardLinkUnix(existingFileName, newFileName);
            }
            else
            {
                throw new InvalidOperationException("Invalid Operating System.");
            }
#else
            return CreateHardLinkWin(existingFileName, newFileName);
#endif
        }

        private static CreateHardLinkStatus CreateHardLinkWin(string existingFileName, string newFileName)
        {
            bool result = CreateHardLinkW(newFileName, existingFileName, IntPtr.Zero);
            if (result)
            {
                return CreateHardLinkStatus.Success;
            }
            switch (Marshal.GetLastWin32Error())
            {
                case (int)NativeIOConstants.ErrorNotSameDevice:
                    return CreateHardLinkStatus.FailedSinceDestinationIsOnDifferentVolume;
                case (int)NativeIOConstants.ErrorTooManyLinks:
                    return CreateHardLinkStatus.FailedDueToPerFileLinkLimit;
                case (int)NativeIOConstants.ErrorNotSupported:
                    return CreateHardLinkStatus.FailedSinceNotSupportedByFilesystem;
                case (int)NativeIOConstants.ErrorAccessDenied:
                    return CreateHardLinkStatus.FailedAccessDenied;
                default:
                    return CreateHardLinkStatus.Failed;
            }
        }

        private static CreateHardLinkStatus CreateHardLinkUnix(string existingFileName, string newFileName)
        {
            int result = link(existingFileName, newFileName);
            if (result != 0)
            {
                var errno = Marshal.GetLastWin32Error();
                switch (errno)
                {
                    case (int)Errno.EACCES:
                        return CreateHardLinkStatus.FailedAccessDenied;
                    case (int)Errno.ELOOP:
                    case (int)Errno.EMLINK:
                        return CreateHardLinkStatus.FailedDueToPerFileLinkLimit;
                    case (int)Errno.EPERM:
                        return CreateHardLinkStatus.FailedAccessDenied;
                    default:
                        return CreateHardLinkStatus.Failed;
                }
            }
            return CreateHardLinkStatus.Success;
        }

        public enum CreateHardLinkStatus
        {
            /// <summary>
            /// Succeeded.
            /// </summary>
            Success,

            /// <summary>
            /// Hardlinks may not span volumes, but the destination path is on a different volume.
            /// </summary>
            FailedSinceDestinationIsOnDifferentVolume,

            /// <summary>
            /// The source file cannot have more links. It is at the filesystem's link limit.
            /// </summary>
            FailedDueToPerFileLinkLimit,

            /// <summary>
            /// The filesystem containing the source and destination does not support hardlinks.
            /// </summary>
            FailedSinceNotSupportedByFilesystem,

            /// <summary>
            /// AccessDenied was returned
            /// </summary>
            FailedAccessDenied,

            /// <summary>
            /// Generic failure.
            /// </summary>
            Failed,
        }

        /// <summary>
        /// Errorno codes
        /// </summary>
        private enum Errno : int
        {
            EPERM = 1, // Operation not permitted
            ENOENT = 2, // No such file or directory
            ESRCH = 3, // No such process
            EINTR = 4, // Interrupted system call
            EIO = 5, // I/O error
            ENXIO = 6, // No such device or address
            E2BIG = 7, // Arg list too long
            ENOEXEC = 8, // Exec format error
            EBADF = 9, // Bad file number
            ECHILD = 10, // No child processes
            EDEADLK = 11, // Try again
            ENOMEM = 12, // Out of memory
            EACCES = 13, // Permission denied
            EFAULT = 14, // Bad address
            ENOTBLK = 15, // Block device required
            EBUSY = 16, // Device or resource busy
            EEXIST = 17, // File exists
            EXDEV = 18, // Cross-device link
            ENODEV = 19, // No such device
            ENOTDIR = 20, // Not a directory
            EISDIR = 21, // Is a directory
            EINVAL = 22, // Invalid argument
            ENFILE = 23, // File table overflow
            EMFILE = 24, // Too many open files
            ENOTTY = 25, // Not a typewriter
            ETXTBSY = 26, // Text file busy
            EFBIG = 27, // File too large
            ENOSPC = 28, // No space left on device
            ESPIPE = 29, // Illegal seek
            EROFS = 30, // Read-only file system
            EMLINK = 31, // Too many links
            EPIPE = 32, // Broken pipe
            EDOM = 33, // Math argument out of domain of func
            ERANGE = 34, // Math result not representable
            EAGAIN = 35, // Resource temporarily unavailable
            ELOOP = 62, // Too many levels of symbolic links
        }

        private enum NativeIOConstants
        {
            ErrorNotSameDevice = 0x11,
            ErrorTooManyLinks = 0x476,
            ErrorNotSupported = 0x32,
            ErrorAccessDenied = 0x5,
        }
    }
}
