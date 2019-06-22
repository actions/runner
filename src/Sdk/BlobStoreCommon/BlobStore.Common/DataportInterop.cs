using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.DataDeduplication.Interop
{
    // Dataport
    // These interop classes are based on ddp*.idl from the public SDK
    // e.g. C:\Program Files (x86)\Windows Kits\10\Include\10.0.16299.0\um\ddpdataport.idl

    public enum DedupChunkingAlgorithm
    {
        Unknown = 0,
        V1 = 1
    }

    public enum DedupHashingAlgorithm
    {
        Unknown = 0,
        V1 = 1
    }

    public enum DedupCompressionAlgorithm
    {
        Unknown = 0,
        Xpress = 1
    }

    [CLSCompliant(false)]
    public struct DedupStream
    {
        [MarshalAs(UnmanagedType.BStr)]
        public string Path;
        public ulong Offset;
        public ulong Length;
        public uint ChunkCount;
    }

    public struct DedupHash
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Hash;
    }

    [CLSCompliant(false)]
    public struct DedupStreamEntry
    {
        public DedupHash Hash;
        public uint LogicalSize;
        public ulong Offset;
    }

    [Flags]
    public enum DedupChunkFlags
    {
        None = 0,
        Compressed = 1
    }

    [CLSCompliant(false)]
    public struct DedupChunk
    {
        public DedupHash Hash;
        public DedupChunkFlags Flags;
        public uint LogicalSize;
        public uint DataSize;
    }

    public enum DedupDataPortRequestStatus
    {
        Unknown = 0,
        Queued = 1,
        Processing = 2,
        Partial = 3,
        Complete = 4,
        Failed = 5
    }

    public enum DedupDataPortVolumeStatus
    {
        Unknown = 0,
        NotEnabled = 1,
        NotAvailable = 2,
        Initializing = 3,
        Ready = 4,
        Maintenance = 5,
        Shutdown = 6
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("7963d734-40a9-4ea3-bbf6-5a89d26f7ae8")]
    [CLSCompliant(false)]
    public interface IDedupDataPort
    {
        void GetStatus(out DedupDataPortVolumeStatus status, out uint maintenanceMB);
        void LookupChunks(uint count, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] DedupHash[] hashes, out Guid requestId);
        void InsertChunks(
            uint chunkCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] DedupChunk[] chunkMetadata, 
            uint dataByteCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] chunkData, 
            out Guid requestId);
        void InsertChunksWithStream(
            uint chunkCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] DedupChunk[] chunkMetadata, 
            uint dataByteCount, [MarshalAs(UnmanagedType.Interface)] IStream chunkDataStream, 
            out Guid requestId);
        void CommitStreams(
            uint streamCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] DedupStream[] streams, 
            uint entryCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] DedupStreamEntry[] entries, 
            out Guid requestId);
        void CommitStreamsWithStream(
            uint streamCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] DedupStream[] streams, 
            uint entryCount, [MarshalAs(UnmanagedType.Interface, SizeParamIndex = 2)] IStream streamEntriesStream,
            out Guid requestId);
        void GetStreams(
            uint streamCount, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] streamPaths, 
            out Guid requestId);
        void GetStreamsResults(Guid requestId, uint waitMs, uint entryIndex, out uint streamCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] out DedupStream[] streams, out uint entryCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] out DedupStreamEntry[] entries, out DedupDataPortRequestStatus status, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] out int[] itemResults);
        void GetChunks(uint count, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] DedupHash[] hashes, out Guid requestId);
        void GetChunksResults(Guid requestId, uint waitMs, uint index, 
            out uint chunkCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] out DedupChunk[] chunkMetadata,
            out uint dataByteCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] out byte[] chunkData, 
            out DedupDataPortRequestStatus status,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] out int[] itemResults);
        void GetRequestStatus(Guid requestId, out DedupDataPortRequestStatus status);
        void GetRequestResults(Guid requestId, uint waitMs, out int batchResult, out uint batchCount, out DedupDataPortRequestStatus status,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] out int[] itemResults);
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("44677452-b90a-445e-8192-cdcfe81511fb")]
    [CLSCompliant(false)]
    public interface IDedupDataPortManager
    {
        void GetConfiguration(out uint minChunkSize, out uint maxChunkSize, out DedupChunkingAlgorithm chunking, out DedupHashingAlgorithm hashing, out DedupCompressionAlgorithm compression);
        void GetVolumeStatus(uint options, [MarshalAs(UnmanagedType.BStr)] string path, out DedupDataPortVolumeStatus status);
        void GetVolumeDataPort(uint options, [MarshalAs(UnmanagedType.BStr)] string path, [MarshalAs(UnmanagedType.Interface)] out IDedupDataPort dataPort);
    }

    [Flags]
    public enum DedupDataPortManagerOptions
    {
        None = 0,
        AutoStart = 1
    }
}
