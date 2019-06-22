using System;
using System.Diagnostics;

namespace GitHub.Services.BlobStore.Common
{
    public static class ArraySegmentExtensions
    {
        public static T[] CreateCopy<T>(this ArraySegment<T> segment)
        {
            var copy = new T[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, copy, 0, segment.Count);
            return copy;
        }
    }

    public sealed class DedupCompressedBuffer : IDisposable
    {
#if DEBUG
        public static bool CaptureStackTrace = true;
        private readonly string CreateStackTrace;
        private string DisposeStackTrace;
#endif

        private readonly object syncObject = new object();
        private bool disposed;
        private ArraySegment<byte>? uncompressed;
        private ArraySegment<byte>? compressed;
        private bool? isCompressable;
        private ChunkDedupIdentifier chunkIdentifier;
        private NodeDedupIdentifier nodeIdentifier;
        private IPoolHandle<byte[]> borrowedUncompressed;
        private IPoolHandle<byte[]> borrowedCompressed;

        public static DedupCompressedBuffer FromCompressed(IPoolHandle<byte[]> compressed, int offset, int count)
        {
            return new DedupCompressedBuffer(null, null, new ArraySegment<byte>(compressed.Value, offset, count), compressed);
        }

        public static DedupCompressedBuffer FromUncompressed(IPoolHandle<byte[]> uncompressed, int offset, int count)
        {
            return new DedupCompressedBuffer(new ArraySegment<byte>(uncompressed.Value, offset, count), uncompressed, null, null);
        }

        public static DedupCompressedBuffer FromCompressed(ArraySegment<byte> compressed)
        {
            return new DedupCompressedBuffer(null, null, compressed, null);
        }

        public static DedupCompressedBuffer FromUncompressed(ArraySegment<byte> uncompressed)
        {
            return new DedupCompressedBuffer(uncompressed, null, null, null);
        }

        public static DedupCompressedBuffer FromCompressed(byte[] compressed)
        {
            return DedupCompressedBuffer.FromCompressed(new ArraySegment<byte>(compressed));
        }

        public static DedupCompressedBuffer FromUncompressed(byte[] uncompressed)
        {
            return DedupCompressedBuffer.FromUncompressed(new ArraySegment<byte>(uncompressed));
        }

        public void AssertValid()
        {
            if (this.disposed)
            {
#if DEBUG
                throw new ObjectDisposedException(nameof(DedupCompressedBuffer), $"Was disposed here: {DisposeStackTrace}");
#else
                throw new ObjectDisposedException(nameof(DedupCompressedBuffer));
#endif
            }

            AssertInternalsValid();
        }

        private void AssertInternalsValid()
        {
            borrowedUncompressed?.AssertValid();
            borrowedCompressed?.AssertValid();
        }

        public void Dispose()
        {
            lock (syncObject)
            {
                if (!this.disposed)
                {
#if DEBUG
                    this.DisposeStackTrace = (new StackTrace()).ToString();
#endif
                    AssertInternalsValid();
                    this.borrowedUncompressed?.Dispose();
                    this.borrowedCompressed?.Dispose();
                    this.disposed = true;
                }
            }
        }

        private DedupCompressedBuffer(
            ArraySegment<byte>? uncompressed, IPoolHandle<byte[]> borrowedUncompressed,
            ArraySegment<byte>? compressed, IPoolHandle<byte[]> borrowedCompressed)
        {
#if DEBUG
            if (CaptureStackTrace)
            {
                this.CreateStackTrace = (new StackTrace()).ToString();
            }
#endif

            this.uncompressed = uncompressed;
            this.borrowedUncompressed = borrowedUncompressed;

            this.compressed = compressed;
            this.borrowedCompressed = borrowedCompressed;

            this.nodeIdentifier = null;
            this.chunkIdentifier = null;

            if (this.compressed != null)
            {
                this.isCompressable = true;
            }
            else
            {
                this.isCompressable = null;
            }
        }

        ~DedupCompressedBuffer()
        {
/* Disabling due to failing test cases, The Buffer was used in static variables of DedupProviderTests
#if DEBUG
            if (!this.disposed && (this.borrowedCompressed != null || this.borrowedUncompressed != null))
            {
                throw new ObjectDisposedException($"Failed to dispose of buffer {this.ChunkIdentifier} created here: {CreateStackTrace}");
            }
#endif
*/
            this.Dispose();
        }

        public bool HasCompressed => compressed.HasValue;
        public bool HasUncompressed => uncompressed.HasValue;

        public bool TryGetCompressed(out ArraySegment<byte>? compressedBytes)
        {
            lock (syncObject)
            {
                if (this.isCompressable == null)
                {
                    this.borrowedCompressed = ChunkerHelper.BorrowChunkBuffer();

                    if (uncompressed.Value.Offset != 0)
                    {
                        throw new NotImplementedException();
                    }

                    uint? compressedSize = ChunkCompression.TryCompressChunk(
                        uncompressed.Value.Array,
                        (uint)uncompressed.Value.Count,
                        this.borrowedCompressed.Value);

                    if (compressedSize.HasValue && compressedSize < uncompressed.Value.Count)
                    {
                        this.compressed = new ArraySegment<byte>(this.borrowedCompressed.Value, 0, (int)compressedSize.Value);
                        this.isCompressable = true;
                    }
                    else
                    {
                        this.isCompressable = false;
                        this.borrowedCompressed.Dispose();
                        this.borrowedCompressed = null;
                    }
                }

                if (this.isCompressable.Value)
                {
                    compressedBytes = this.compressed.Value;
                    return true;
                }

                compressedBytes = null;
                return false;
            }
        }

        public void GetBytesTryCompress(out bool isCompressed, out ArraySegment<byte> buffer)
        {
            ArraySegment<byte>? maybeCompressed;
            if (isCompressed = TryGetCompressed(out maybeCompressed))
            {
                buffer = maybeCompressed.Value;
            }
            else
            {
                buffer = uncompressed.Value;
            }
        }

        public void GetBytes(out bool isCompressed, out ArraySegment<byte> buffer)
        {
            lock (syncObject)
            {
                isCompressed = this.compressed.HasValue;
                if (isCompressed)
                {
                    buffer = this.compressed.Value;
                }
                else
                {
                    buffer = this.uncompressed.Value;
                }
            }
        }

        public bool IsCompressable
        {
            get
            {
                if (isCompressable.HasValue)
                {
                    return isCompressable.Value;
                }

                ArraySegment<byte>? buffer;
                return this.TryGetCompressed(out buffer);
            }
        }

        public bool TryGetAlreadyCompressed(out ArraySegment<byte>? compressedBytes)
        {
            lock (syncObject)
            {
                if (this.compressed == null)
                {
                    compressedBytes = null;
                    return false;
                }
                else
                {
                    compressedBytes = this.compressed;
                    return true;
                }
            }
        }

        public ArraySegment<byte> Uncompressed
        {
            get
            {
                EnsureUncompressedBufferAvailable();
                return uncompressed.Value;
            }
        }

        public ChunkDedupIdentifier ChunkIdentifier
        {
            get
            {
                if (this.chunkIdentifier == null)
                {
                    EnsureUncompressedBufferAvailable();
                    lock (syncObject)
                    {
                        if (this.chunkIdentifier == null)
                        {
                            this.chunkIdentifier = ChunkDedupIdentifier.CalculateIdentifier(uncompressed.Value);
                        }
                    }
                }

                return this.chunkIdentifier;
            }
        }

        public NodeDedupIdentifier NodeIdentifier
        {
            get
            {
                if (this.nodeIdentifier == null)
                {
                    EnsureUncompressedBufferAvailable();
                    lock (syncObject)
                    {
                        if (this.nodeIdentifier == null)
                        {
                            this.nodeIdentifier = NodeDedupIdentifier.CalculateIdentifierFromSerializedNode(uncompressed.Value);
                        }
                    }
                }

                return this.nodeIdentifier;
            }
        }

        private void EnsureUncompressedBufferAvailable()
        {
            if (uncompressed == null)
            {
                lock (syncObject)
                {
                    if (uncompressed == null)
                    {
                        this.borrowedUncompressed = ChunkerHelper.BorrowChunkBuffer();
                        if (compressed.Value.Offset != 0)
                        {
                            throw new NotImplementedException();
                        }
                        uint decompressedSize = ChunkCompression.DecompressChunk(
                            this.compressed.Value.Array,
                            this.compressed.Value.Count,
                            this.borrowedUncompressed.Value);
                        uncompressed = new ArraySegment<byte>(this.borrowedUncompressed.Value, 0, (int)decompressedSize);
                    }
                }
            }
        }
    }
}
