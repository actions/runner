using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace GitHub.Services.Content.Common
{
    public static class AsyncFile
    {
        private static readonly Task CompletedTask = Task.FromResult(0);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static unsafe extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, IntPtr numBytesRead_mustBeZero, NativeOverlapped* overlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);


        [Flags]
        private enum EMethod : uint
        {
            Buffered = 0,
        }

        [Flags]
        private enum EFileDevice : uint
        {
            FileSystem = 0x00000009,
        }

        /// <summary>
        /// IO Control Codes
        /// Useful links:
        ///     http://www.ioctls.net/
        ///     http://msdn.microsoft.com/en-us/library/windows/hardware/ff543023(v=vs.85).aspx
        /// </summary>
        [Flags]
        private enum EIOControlCode : uint
        {
            FsctlSetSparse = (EFileDevice.FileSystem << 16) | (49 << 2) | EMethod.Buffered | (0 << 14),
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIOControlCode IoControlCode,
            [MarshalAs(UnmanagedType.AsAny)]
            [In] object InBuffer,
            uint nInBufferSize,
            [MarshalAs(UnmanagedType.AsAny)]
            [Out] object OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            [In] IntPtr /*NativeOverlapped*/ Overlapped
        );

        internal sealed class AsyncResult : IAsyncResult
        {
            public readonly Lazy<SafeTaskCompletionSource<int>> CompletionSource =
                new Lazy<SafeTaskCompletionSource<int>>(LazyThreadSafetyMode.ExecutionAndPublication);

            public bool IsCompleted { get { throw new NotSupportedException(); } }
            public WaitHandle AsyncWaitHandle { get { throw new NotSupportedException(); } }
            public object AsyncState { get { throw new NotSupportedException(); } }
            public bool CompletedSynchronously { get { throw new NotSupportedException(); } }
        }

        private unsafe static void CompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlap)
        {
            try
            {
                Overlapped overlapped = Overlapped.Unpack(pOverlap);
                var asr = (AsyncResult)overlapped.AsyncResult;
                if (errorCode == 0)
                {
                    asr.CompletionSource.Value.SetResult((int)numBytes);
                }
                else if (errorCode == ERROR_IO_DEVICE)
                {
                    asr.CompletionSource.Value.SetException(new IOException("Async file operation failed with ERROR_IO_DEVICE", new Win32Exception((int)errorCode)));
                }
                else
                {
                    asr.CompletionSource.Value.SetException(new Win32Exception((int)errorCode, $"Async file operation failed with {errorCode}"));
                }
            }
            finally
            {
                Overlapped.Free(pOverlap);
            }
        }

        private const int ERROR_HANDLE_EOF = 38;
        private const int ERROR_IO_PENDING = 997;
        private const int ERROR_IO_DEVICE = 1117;

        public static int TryMarkSparse(SafeFileHandle hFile, bool sparse)
        {
            uint dwTemp = 0;
            short sSparse = sparse ? (short)1 : (short)0;
            if (DeviceIoControl(hFile, EIOControlCode.FsctlSetSparse, sSparse, 2, IntPtr.Zero, 0, ref dwTemp, IntPtr.Zero))
            {
                return 0;
            }

            return Marshal.GetLastWin32Error();
        }

        private static async Task<int> ReadAsyncNetStandard(FileStream file, long fileOffset, byte[] buffer, int bytesToRead)
        {
            // TODO figure out how to do this without constantly opening handles
            using (var f = new FileStream(
                file.Name,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            {
                f.Position = fileOffset;
                return await f.ReadAsync(buffer, 0, bytesToRead);
            }
        }

        public unsafe static Task<int> ReadAsync(FileStream file, long fileOffset, byte[] buffer, int bytesToRead)
        {

#if NETSTANDARD
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ReadAsyncNetStandard(file, fileOffset, buffer, bytesToRead);
            }
#endif
            if (fileOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileOffset));
            }

            if (bytesToRead < 0 || bytesToRead > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesToRead));
            }

            if (!file.IsAsync)
            {
                throw new ArgumentException("FileStream must be opened for async access");
            }

            var asyncResult = new AsyncResult();
            var o = new Overlapped((int)(fileOffset & 0xFFFFFFFF), (int)(fileOffset >> 32), IntPtr.Zero, asyncResult);
            fixed (byte* bufferBase = buffer)
            {
                // https://docs.microsoft.com/en-us/dotnet/api/system.threading.overlapped.pack?view=netframework-4.7#System_Threading_Overlapped_Pack_System_Threading_IOCompletionCallback_System_Object_
                // The buffer or buffers specified in userData must be the same as those passed to the unmanaged operating system function that performs the asynchronous I/O. 
                // The runtime pins the buffer or buffers specified in userData for the duration of the I/O operation.
                NativeOverlapped* pOverlapped = o.Pack(CompletionCallback, buffer);

                bool freeOverlapped = true;
                try
                {
                    if (0 != ReadFile(file.SafeFileHandle, bufferBase, bytesToRead, IntPtr.Zero, pOverlapped))
                    {
                        freeOverlapped = false;
                    }
                    else
                    {
                        // If FALSE is returned, any error other than ERROR_IO_PENDING indicates failure.

                        int systemErrorCode = Marshal.GetLastWin32Error();
                        if (systemErrorCode == ERROR_IO_PENDING)
                        {
                            freeOverlapped = false;
                        }
                        else if (systemErrorCode == ERROR_IO_DEVICE)
                        {
                            throw new IOException($"ReadFile failed with system error ERROR_IO_DEVICE", new Win32Exception(systemErrorCode));
                        }
                        else
                        {
                            throw new Win32Exception(systemErrorCode, $"ReadFile failed with system error code:{systemErrorCode}");
                        }
                    }
                }
                finally
                {
                    // For an error, free the memory from the overlapped IO.
                    if (freeOverlapped)
                    {
                        Overlapped.Unpack(pOverlapped);
                        Overlapped.Free(pOverlapped);
                    }
                }

                return asyncResult.CompletionSource.Value.Task;
            }
        }

        private static async Task WriteAsyncNetStandard(FileStream file, long fileOffset, ArraySegment<byte> bytes)
        {
            // TODO figure out how to do this without constantly opening handles
            using (var f = new FileStream(
                file.Name,
                FileMode.Open,
                FileAccess.Write,
                FileShare.Write,
                bufferSize: 4096,
                useAsync: true))
            {
                f.Position = fileOffset;
                await f.WriteAsync(bytes.Array, 0, bytes.Count);
                return;
            }
        }


        public static async Task WriteAsync(FileStream file, long fileOffset, ArraySegment<byte> bytes)
        {

#if NETSTANDARD
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await WriteAsyncNetStandard(file, fileOffset, bytes).ConfigureAwait(false);
                return;
            }
#endif

            if (fileOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileOffset));
            }

            if (!file.IsAsync)
            {
                throw new ArgumentException("FileStream must be opened for async");
            }

            var asyncResult = new AsyncResult();
            var o = new Overlapped((int)(fileOffset & 0xFFFFFFFF), (int)(fileOffset >> 32), IntPtr.Zero, asyncResult);
            unsafe
            {
                fixed (byte* bufferBase = bytes.Array)
                {
                    // https://docs.microsoft.com/en-us/dotnet/api/system.threading.overlapped.pack?view=netframework-4.7#System_Threading_Overlapped_Pack_System_Threading_IOCompletionCallback_System_Object_
                    // The buffer or buffers specified in userData must be the same as those passed to the unmanaged operating system function that performs the asynchronous I/O. 
                    // The runtime pins the buffer or buffers specified in userData for the duration of the I/O operation.
                    NativeOverlapped* pOverlapped = o.Pack(CompletionCallback, bytes.Array);

                    bool freeOverlapped = true;
                    try
                    {
                        if (0 != WriteFile(file.SafeFileHandle, bufferBase + bytes.Offset, bytes.Count, IntPtr.Zero, pOverlapped))
                        {
                            freeOverlapped = false;
                        }
                        else
                        {
                            // If FALSE is returned, any error other than ERROR_IO_PENDING indicates failure.

                            int systemErrorCode = Marshal.GetLastWin32Error();
                            if (systemErrorCode == ERROR_IO_PENDING)
                            {
                                freeOverlapped = false;
                            }
                            else if (systemErrorCode == ERROR_IO_DEVICE)
                            {
                                throw new IOException($"WriteFile failed with system error ERROR_IO_DEVICE", new Win32Exception(systemErrorCode));
                            }
                            else
                            {
                                throw new Win32Exception(systemErrorCode, $"WriteFile failed with system error code:{systemErrorCode}");
                            }
                        }
                    }
                    finally
                    {
                        // For an error, free the memory from the overlapped IO.
                        if (freeOverlapped)
                        {
                            Overlapped.Unpack(pOverlapped);
                            Overlapped.Free(pOverlapped);
                        }
                    }
                }
            }

            int bytesWritten = await asyncResult.CompletionSource.Value.Task.ConfigureAwait(false);
            if (bytesWritten != bytes.Count)
            {
                throw new EndOfStreamException("Could not write all the bytes. (Async)");
            }
        }
    }
}
