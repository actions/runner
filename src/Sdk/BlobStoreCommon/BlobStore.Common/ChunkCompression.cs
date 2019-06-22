using System;

namespace GitHub.Services.BlobStore.Common
{
    [CLSCompliant(false)]
    public static class ChunkCompression
    {
        private static readonly bool IsWindows =
#if NET_STANDARD
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#else
            true;
#endif

        // Win8 (per https://docs.microsoft.com/en-us/windows/desktop/SysInfo/operating-system-version)
        private static readonly Version MinSupportedNativeWindowsVersion = new Version(6, 2);

        private static bool DetermineUseNativeCompression()
        {
            string overrideValue = Environment.GetEnvironmentVariable("VSTS_XPRESS_COMPRESSION");
            switch (overrideValue)
            {
                case null:
                    // By default, use native only for Win8 or higher since previous versions don't natively support Xpress
                    return IsWindows && Environment.OSVersion.Version >= MinSupportedNativeWindowsVersion;
                case "NATIVE":
                    return true;
                case "MANAGED":
                    return false;
                default:
                    throw new ArgumentException("Unknown VSTS_XPRESS_COMPRESSION value: " + overrideValue);
            }
        }

        private static readonly bool UseNative = DetermineUseNativeCompression();

        public static uint? TryCompressChunk(
            byte[] uncompressedChunk,
            uint uncompressedChunkSize,
            byte[] compressedChunk)
        {
            uint compressedChunkSize;
            uint ntStatus;

            if (UseNative)
            {
                ntStatus = XpressNative.TryCompressChunk(
                    uncompressedChunk,
                    (uint)uncompressedChunkSize,
                    compressedChunk,
                    out compressedChunkSize);
            }
            else
            {
                ntStatus = XpressManaged.TryCompressChunk(
                    uncompressedChunk,
                    (uint)uncompressedChunkSize,
                    compressedChunk,
                    out compressedChunkSize);
            }

            if (ntStatus == XpressNative.STATUS_BUFFER_TOO_SMALL)
            {
                return null;
            }
            else if (ntStatus != 0)
            {
                throw new Exception($"RtlCompressBuffer 0x{ntStatus:X}");
            }
            else if (compressedChunkSize >= uncompressedChunkSize)
            {
                return null;
            }

            return compressedChunkSize;
        }

        public static uint DecompressChunk(
            byte[] compressedChunk,
            byte[] uncompressedChunk)
        {
            return DecompressChunk(
                compressedChunk,
                compressedChunk.Length,
                uncompressedChunk);
        }

        public static uint DecompressChunk(
            byte[] compressedChunk, int compressedCount,
            byte[] uncompressedChunk)
        {
            uint uncompressedChunkSize;
            uint ntStatus;
            if (UseNative)
            {
                ntStatus = XpressNative.DecompressChunk(
                    compressedChunk,
                    (uint)compressedCount,
                    uncompressedChunk,
                    out uncompressedChunkSize);
            }
            else
            {
                ntStatus = XpressManaged.RtlDecompressBufferXpressLz(
                    uncompressedChunk,
                    compressedChunk,
                    (uint)compressedCount,
                    out uncompressedChunkSize);
            }

            if (ntStatus != 0)
            {
                throw new Exception($"RtlDecompressBuffer 0x{ntStatus:X}");
            }

            return uncompressedChunkSize;
        }
    }
}
