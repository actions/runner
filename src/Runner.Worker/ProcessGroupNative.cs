using System;
using System.Runtime.InteropServices;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker
{
    /// <summary>
    /// Native calls used to move Runner.Worker into its own process group, behind an interface so the logic can be tested.
    /// </summary>
    [ServiceLocator(Default = typeof(ProcessGroupNative))]
    public interface IProcessGroupNative : IRunnerService
    {
        /// <summary>
        /// Opens and closes /dev/tty. Returns 0 when the process has a controlling terminal, otherwise the errno from open(2).
        /// </summary>
        int ProbeControllingTerminal();

        /// <summary>
        /// Calls setpgid(0, 0). Returns 0 on success, otherwise the errno.
        /// </summary>
        int MoveToNewProcessGroup();

        int GetProcessGroup();
    }

    public sealed class ProcessGroupNative : RunnerService, IProcessGroupNative
    {
        // Same values on Linux and macOS.
        public const int ENOENT = 2;
        public const int ENXIO = 6;

        public int ProbeControllingTerminal()
        {
#if OS_WINDOWS
            throw new PlatformNotSupportedException();
#else
            // O_RDONLY. Opening /dev/tty succeeds only when the process has a controlling terminal.
            int fd = open("/dev/tty", 0);
            if (fd < 0)
            {
                return Marshal.GetLastPInvokeError();
            }

            close(fd);
            return 0;
#endif
        }

        public int MoveToNewProcessGroup()
        {
#if OS_WINDOWS
            throw new PlatformNotSupportedException();
#else
            return setpgid(0, 0) == 0 ? 0 : Marshal.GetLastPInvokeError();
#endif
        }

        public int GetProcessGroup()
        {
#if OS_WINDOWS
            throw new PlatformNotSupportedException();
#else
            return getpgrp();
#endif
        }

#if !OS_WINDOWS
        [DllImport("libc", SetLastError = true)]
        private static extern int open(string path, int flags);

        [DllImport("libc")]
        private static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        private static extern int setpgid(int pid, int pgid);

        [DllImport("libc")]
        private static extern int getpgrp();
#endif
    }
}
