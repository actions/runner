using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GitHub.Services.BlobStore.Common
{
    [CLSCompliant(false)]
    public static class XpressNative
    {
        /// <summary>
        /// UncompressedChunkSize [in]
        /// The chunk size to use when compressing the UncompressedBuffer buffer.This parameter must be one of the following values: 512, 1024, 2048, or 4096. The operating system uses 4096, and the recommended value for this parameter is also 4096.
        /// https://msdn.microsoft.com/en-us/library/windows/hardware/ff552127(v=vs.85).aspx
        /// </summary>
        private const int UncompressedChunkSize = 4096;

        public const uint STATUS_BUFFER_TOO_SMALL = 0xC0000023;

        internal static readonly Lazy<ByteArrayPool> NativeWorkspacePool = 
            new Lazy<ByteArrayPool>(
                () => new ByteArrayPool(
                    (int)NativeMethods.CompressBufferWorkSpaceSize.Value,
                    maxToKeep: 4 * Environment.ProcessorCount));

        public static uint TryCompressChunk(
            byte[] uncompressedChunk,
            uint uncompressedChunkSize,
            byte[] compressedChunk,
            out uint compressedChunkSize)
        {
            if (uncompressedChunkSize < 16)
            {
                compressedChunkSize = 0;
                return STATUS_BUFFER_TOO_SMALL;
            }

            using (var workspace = NativeWorkspacePool.Value.Get())
            {
                return NativeMethods.RtlCompressBuffer(
                    NativeMethods.COMPRESSION_FORMAT_XPRESS | NativeMethods.COMPRESSION_ENGINE_MAXIMUM,
                    uncompressedChunk,
                    uncompressedChunkSize,
                    compressedChunk,
                    (uint)compressedChunk.Length,
                    UncompressedChunkSize,
                    out compressedChunkSize,
                    workspace.Value);
            }
        }

        public static uint DecompressChunk(
            byte[] compressedChunk, uint compressedCount,
            byte[] uncompressedChunk, out uint uncompressedChunkSize)
        {
            using (var workspace = NativeWorkspacePool.Value.Get())
            {
                return NativeMethods.RtlDecompressBufferEx(
                    NativeMethods.COMPRESSION_FORMAT_XPRESS | NativeMethods.COMPRESSION_ENGINE_MAXIMUM,
                    uncompressedChunk,
                    (uint)uncompressedChunk.Length,
                    compressedChunk,
                    compressedCount,
                    out uncompressedChunkSize,
                    workspace.Value);
            }
        }

        private static class NativeMethods
        {
            public const ushort COMPRESSION_ENGINE_MAXIMUM = 0x0100;
            public const ushort COMPRESSION_FORMAT_XPRESS = 0x0003;

            public static readonly Lazy<uint> CompressBufferWorkSpaceSize = new Lazy<uint>(
                () =>
                {
                    uint compressBufferWorkSpaceSize;
                    uint compressFragmentWorkSpaceSize;
                    uint ntStatus = RtlGetCompressionWorkSpaceSize(
                        COMPRESSION_FORMAT_XPRESS | COMPRESSION_ENGINE_MAXIMUM,
                        out compressBufferWorkSpaceSize,
                        out compressFragmentWorkSpaceSize);

                    if (ntStatus != 0)
                    {
                        throw new Exception($"RtlGetCompressionWorkSpaceSize failed 0x{ntStatus:X}");
                    }

                    return compressBufferWorkSpaceSize;
                },
                LazyThreadSafetyMode.PublicationOnly);

            [DllImport("ntdll.dll")]
            private static extern uint RtlGetCompressionWorkSpaceSize(
                ushort compressionFormat,
                out uint compressBufferWorkSpaceSize,
                out uint compressFragmentWorkSpaceSize);

            [DllImport("ntdll.dll")]
            public static extern uint RtlDecompressBufferEx(
                ushort compressionFormat,
                byte[] uncompressedBuffer,
                uint uncompressedBufferSize,
                byte[] compressedBuffer,
                uint compressedBufferSize,
                out uint finalUncompressedSize,
                byte[] workSpace);

            [DllImport("ntdll.dll")]
            public static extern uint RtlCompressBuffer(
                ushort compressionFormatAndEngine,
                byte[] uncompressedBuffer,
                uint uncompressedBufferSize,
                byte[] compressedBuffer,
                uint compressedBufferSize,
                uint uncompressedChunkSize,
                out uint finalCompressedSize,
                byte[] workSpace);
        }
    }
}
