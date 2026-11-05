using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace GitHub.Runner.Listener
{
    // Requesting drain must never cancel the listener's HTTP or worker tokens.
    internal sealed class DrainRequest : IDisposable
    {
        private readonly string _file;
        private readonly PosixSignalRegistration _signal;
        private int _requested;

        public DrainRequest(string file, bool useSigusr1)
        {
            _file = file;
            if (useSigusr1)
            {
                // SIGUSR1 is 10 on supported Linux architectures and 30 on macOS.
                // .NET accepts native signal numbers for signals outside its enum.
                int signal = OperatingSystem.IsLinux() ? 10 : OperatingSystem.IsMacOS() ? 30 :
                    throw new PlatformNotSupportedException("--drain-on-sigusr1 requires Linux or macOS; use --drain-file on other platforms.");
                _signal = PosixSignalRegistration.Create((PosixSignal)signal, context =>
                {
                    context.Cancel = true;
                    Request();
                });
            }
        }

        public void Request() => Interlocked.Exchange(ref _requested, 1);

        public bool IsRequested
        {
            get
            {
                if (Volatile.Read(ref _requested) == 0 && _file != null && File.Exists(_file))
                {
                    Request();
                }
                return Volatile.Read(ref _requested) != 0;
            }
        }

        public void Dispose() => _signal?.Dispose();
    }
}
