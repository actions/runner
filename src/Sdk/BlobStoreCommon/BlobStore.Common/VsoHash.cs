using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.Common
{
    public partial class VsoHash : IBlobHasher
    {
        public const byte AlgorithmId = 0;
        public const int PagesPerBlock = 32;
        public const int PageSize = 64 * 1024;
        public const int BlockSize = PagesPerBlock * PageSize; // 32 * 64 * 1024 = 2MB

        public static readonly BlobIdentifierWithBlocks OfNothing;

        internal static readonly bool BCryptAvailable;
        private static readonly byte[] EmptyByteArray = new byte[0];
        private static readonly ByteArrayPool PoolLocalBlockBuffer = new ByteArrayPool(BlockSize, maxToKeep: 1000);
        private static readonly Pool<SHA256CryptoServiceProvider> PoolSha256 = new Pool<SHA256CryptoServiceProvider>(
            factory: () => new SHA256CryptoServiceProvider(),
            reset: sha256 => sha256.Initialize(),
            maxToKeep: 2 * Environment.ProcessorCount);

        private static readonly Pool<BCrypt.BCryptVsoHashContext> PoolBCrypt = new Pool<BCrypt.BCryptVsoHashContext>(
            factory: () => new BCrypt.BCryptVsoHashContext(),
            reset: _ => { },
            maxToKeep: 2 * Environment.ProcessorCount);

        public static IPoolHandle<byte[]> BorrowBlockBuffer()
        {
            return PoolLocalBlockBuffer.Get();
        }

        public static IPoolHandle<SHA256CryptoServiceProvider> BorrowSHA256()
        {
            return PoolSha256.Get();
        }

        private VsoHash()
        {
        }

        static VsoHash()
        {
            bool isWindowsOs
#if NET_STANDARD
                = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
                = true;
#endif

            if (isWindowsOs)
            {
                var testBuffer = new byte[10];
                try
                {
                    HashBlockBCrypt(testBuffer, testBuffer.Length);
                    BCryptAvailable = true;
                }
                catch
                {
                    // fall back in case of old version of Windows
                    BCryptAvailable = false;
                }
            }
            else
            {
                BCryptAvailable = false;
            }

            using (var emptyStream = new MemoryStream())
            {
                OfNothing = CalculateBlobIdentifierWithBlocks(emptyStream);
            }
        }

        public static readonly VsoHash Instance = new VsoHash();

        private delegate Task BlockReadCompleteAsync(Pool<byte[]>.PoolHandle blockBufferHandle, int blockLength, BlobBlockHash blockHash);

        private delegate void BlockReadComplete(Pool<byte[]>.PoolHandle blockBufferHandle, int blockLength, BlobBlockHash blockHash);

        public static BlobBlockHash HashBlock(byte[] block, int blockLength)
        {
            return BCryptAvailable
                ? HashBlockBCrypt(block, blockLength)
                : HashBlockCng(block, blockLength);
        }

        internal static BlobBlockHash HashBlockBCrypt(byte[] block, int blockLength)
        {
            using (var bcrypt = PoolBCrypt.Get())
            {
                return bcrypt.Value.HashBlock(block, blockLength);
            }
        }

        internal static BlobBlockHash HashBlockCng(byte[] block, int blockLength)
        {
            using (var pageIdentifiersSha256Handle = PoolSha256.Get())
            {
                using (var pageContentsSha256Handle = PoolSha256.Get())
                {
                    int blockOffset = 0;
                    while (blockOffset < blockLength)
                    {
                        int bytesInPage = Math.Min(blockLength - blockOffset, PageSize);
                        byte[] pageHash = pageContentsSha256Handle.Value.ComputeHash(block, blockOffset, bytesInPage);
                        pageIdentifiersSha256Handle.Value.TransformBlock(pageHash, 0, pageHash.Length, null, 0);
                        blockOffset += bytesInPage;
                    }
                }

                // calculate the block buffer as we have make pages or have a partial page
                pageIdentifiersSha256Handle.Value.TransformFinalBlock(EmptyByteArray, 0, 0);
                return new BlobBlockHash(pageIdentifiersSha256Handle.Value.Hash);
            }
        }

        public static async Task<BlobIdentifierWithBlocks> WalkAllBlobBlocksAsync(Stream stream, SemaphoreSlim blockActionSemaphore, bool multiBlocksInParallel,
            MultiBlockBlobCallbackAsync multiBlockCallback, long? bytesToReadFromStream = null)
        {
            bytesToReadFromStream = bytesToReadFromStream ?? (stream.Length - stream.Position);
            BlobIdentifierWithBlocks blobIdWithBlocks = default(BlobIdentifierWithBlocks);
            await WalkMultiBlockBlobAsync(stream, blockActionSemaphore, multiBlocksInParallel, multiBlockCallback,
                computedBlobIdWithBlocks =>
                {
                    blobIdWithBlocks = computedBlobIdWithBlocks;
                    return Task.FromResult(0);
                },
                bytesToReadFromStream.GetValueOrDefault()).ConfigureAwait(false);
            return blobIdWithBlocks;
        }

        /// <summary>
        /// Asynchronously walks a stream, calling back into supplied delegates at a block level
        /// </summary>
        /// <param name="stream">The stream to read bytes from.  The caller is responsible for correctly setting the stream's starting positon.</param>
        /// <param name="blockActionSemaphore">Optional: If non-null, a SemaphoreSlim to bound the number of callbacks in flight.  This can be used to bound the number of block-sized that are allocated at any one time.</param>
        /// <param name="multiBlocksInParallel">Only affects multi-block blobs.  Determines if multiBlockCallback delegates are called in parallel (True) or serial (False).</param>
        /// <param name="singleBlockCallback">Only will be called if the blob is composed of a single block. Is called with the byte buffer for the block, the length of block (possibly less than buffer's length), and the hash of the block.</param>
        /// <param name="multiBlockCallback">Only will be called if the blob is composed of a multiple blocks. Is called with the byte buffer for the block, the length of block (possibly less than buffer's length), the index of this block, the hash of the block, and whether or not this is the final block.</param>
        /// <param name="multiBlockSealCallback">Only will be called if the blob is composed of a multiple blocks. Is called after all multiBlockCallback delegates have returned.</param>
        /// <param name="bytesToReadFromStream">Number of bytes to read from the stream. Specify -1 to read to the end of the stream.</param>
        /// <returns></returns>
        public static async Task WalkBlocksAsync(
            Stream stream,
            SemaphoreSlim blockActionSemaphore,
            bool multiBlocksInParallel,
            SingleBlockBlobCallbackAsync singleBlockCallback,
            MultiBlockBlobCallbackAsync multiBlockCallback,
            MultiBlockBlobSealCallbackAsync multiBlockSealCallback,
            long bytesToReadFromStream = -1)
        {
            bytesToReadFromStream = (bytesToReadFromStream >= 0) ? bytesToReadFromStream : (stream.Length - stream.Position);
            bool isSingleBlockBlob = (bytesToReadFromStream <= BlockSize);

            if (isSingleBlockBlob)
            {
                await WalkSingleBlockBlobAsync(stream, blockActionSemaphore, singleBlockCallback, bytesToReadFromStream).ConfigureAwait(false);
            }
            else
            {
                await WalkMultiBlockBlobAsync(stream, blockActionSemaphore, multiBlocksInParallel, multiBlockCallback, multiBlockSealCallback, bytesToReadFromStream).ConfigureAwait(false);
            }
        }

        public static void WalkBlocks(
            Stream stream,
            SemaphoreSlim blockActionSemaphore,
            bool multiBlocksInParallel,
            SingleBlockBlobCallback singleBlockCallback,
            MultiBlockBlobCallback multiBlockCallback,
            MultiBlockBlobSealCallback multiBlockSealCallback,
            long bytesToReadFromStream = -1)
        {
            bytesToReadFromStream = (bytesToReadFromStream >= 0) ? bytesToReadFromStream : (stream.Length - stream.Position);
            bool isSingleBlockBlob = (bytesToReadFromStream <= BlockSize);

            if (isSingleBlockBlob)
            {
                WalkSingleBlockBlob(stream, blockActionSemaphore, singleBlockCallback, bytesToReadFromStream);
            }
            else
            {
                WalkMultiBlockBlob(stream, blockActionSemaphore, multiBlocksInParallel, multiBlockCallback, multiBlockSealCallback, bytesToReadFromStream);
            }
        }

        public static BlobIdentifierWithBlocks CalculateBlobIdentifierWithBlocks(Stream stream)
        {
            BlobIdentifierWithBlocks result = null;

            WalkBlocks(
                stream,
                blockActionSemaphore: null,
                multiBlocksInParallel: false,
                singleBlockCallback: (block, blockLength, blobIdWithBlocks) =>
                {
                    result = blobIdWithBlocks;
                },
                multiBlockCallback: (block, blockLength, blockHash, isFinalBlock) =>
                {
                },
                multiBlockSealCallback: (blobIdWithBlocks) =>
                {
                    result = blobIdWithBlocks;
                });

            if (result == null)
            {
                throw new InvalidOperationException("Program error: CalculateBlobIdentifierWithBlocks did not calculate a value.");
            }

            return result;
        }

        public static async Task<BlobIdentifierWithBlocks> CalculateBlobIdentifierWithBlocksAsync(Stream stream)
        {
            BlobIdentifierWithBlocks result = null;

            await WalkBlocksAsync(
                stream,
                blockActionSemaphore: null,
                multiBlocksInParallel: false,
                singleBlockCallback: (block, blockLength, blobIdWithBlocks) =>
                {
                    result = blobIdWithBlocks;
                    return Task.FromResult(0);
                },
                multiBlockCallback: (block, blockLength, blockHash, isFinalBlock) => Task.FromResult(0),
                multiBlockSealCallback: (blobIdWithBlocks) =>
                {
                    result = blobIdWithBlocks;
                    return Task.FromResult(0);
                }).ConfigureAwait(false);

            if (result == null)
            {
                throw new InvalidOperationException("Program error: CalculateBlobIdentifierWithBlocksAsync did not calculate a value.");
            }

            return result;
        }

        public static BlobIdentifier CalculateBlobIdentifier(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return CalculateBlobIdentifierWithBlocks(stream).BlobId;
        }

        public static BlobIdentifier CalculateBlobIdentifier(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            using (var stream = new MemoryStream(bytes))
            {
                return CalculateBlobIdentifierWithBlocks(stream).BlobId;
            }
        }

        public static async Task<BlobIdentifier> CalculateBlobIdentifierAsync(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            BlobIdentifierWithBlocks blobIdentifierWithBlocks = await CalculateBlobIdentifierWithBlocksAsync(stream).ConfigureAwait(false);

            return blobIdentifierWithBlocks.BlobId;
        }

        public static async Task<BlobIdentifier> CalculateBlobIdentifierAsync(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            using (var stream = new MemoryStream(bytes))
            {
                return (await CalculateBlobIdentifierWithBlocksAsync(stream).ConfigureAwait(false)).BlobId;
            }
        }

        private static byte[] ComputeSHA256Hash(List<byte> bytes)
        {
            byte[] byteArray = bytes.ToArray();
            using (var sha256Handle = PoolSha256.Get())
            {
                return sha256Handle.Value.ComputeHash(byteArray, 0, byteArray.Length);
            }
        }

        private static void ReadBlock(Stream stream, SemaphoreSlim blockActionSemaphore, long bytesLeftInBlob, BlockReadComplete readCallback)
        {
            blockActionSemaphore?.Wait();

            bool disposeNeeded = true;
            try
            {
                Pool<byte[]>.PoolHandle blockBufferHandle = PoolLocalBlockBuffer.Get();
                try
                {
                    byte[] blockBuffer = blockBufferHandle.Value;
                    int bytesToRead = (int)Math.Min(VsoHash.BlockSize, bytesLeftInBlob);
                    int bufferOffset = 0;
                    while (bytesToRead > 0)
                    {
                        int bytesRead = stream.Read(blockBuffer, bufferOffset, bytesToRead);
                        bytesToRead -= bytesRead;
                        bufferOffset += bytesRead;
                        if (bytesRead == 0)
                        {
                            // ReadAsync returns 0 when the stream has ended.
                            if (bytesToRead > 0)
                            {
                                throw new EndOfStreamException();
                            }
                        }
                    }

                    BlobBlockHash blockHash = HashBlock(blockBuffer, bufferOffset);
                    disposeNeeded = false;
                    readCallback(blockBufferHandle, bufferOffset, blockHash);
                }
                finally
                {
                    if (disposeNeeded)
                    {
                        blockBufferHandle.Dispose();
                    }
                }
            }
            finally
            {
                if (disposeNeeded && blockActionSemaphore != null)
                {
                    blockActionSemaphore.Release();
                }
            }
        }

        private static async Task ReadBlockAsync(
            Stream stream,
            SemaphoreSlim blockActionSemaphore,
            long bytesLeftInBlob,
            BlockReadCompleteAsync readCallback)
        {
            if (blockActionSemaphore != null)
            {
                await blockActionSemaphore.WaitAsync().ConfigureAwait(false);
            }

            bool disposeNeeded = true;
            try
            {
                Pool<byte[]>.PoolHandle blockBufferHandle = PoolLocalBlockBuffer.Get();
                try
                {
                    byte[] blockBuffer = blockBufferHandle.Value;
                    int bytesToRead = (int)Math.Min(VsoHash.BlockSize, bytesLeftInBlob);
                    int bufferOffset = 0;
                    while (bytesToRead > 0)
                    {
                        int bytesRead = await stream.ReadAsync(blockBuffer, bufferOffset, bytesToRead).ConfigureAwait(false);
                        bytesToRead -= bytesRead;
                        bufferOffset += bytesRead;
                        if (bytesRead == 0)
                        {
                            // ReadAsync returns 0 when the stream has ended.
                            if (bytesToRead > 0)
                            {
                                throw new EndOfStreamException();
                            }
                        }
                    }

                    BlobBlockHash blockHash = HashBlock(blockBuffer, bufferOffset);
                    disposeNeeded = false; // readCallback is now responsible for disposing the blockBufferHandle
                    await readCallback(blockBufferHandle, bufferOffset, blockHash).ConfigureAwait(false);
                }
                finally
                {
                    if (disposeNeeded)
                    {
                        blockBufferHandle.Dispose();
                    }
                }
            }
            finally
            {
                if (disposeNeeded)
                {
                    blockActionSemaphore?.Release();
                }
            }
        }

        private static void WalkMultiBlockBlob(Stream stream, SemaphoreSlim blockActionSemaphore, bool multiBlocksInParallel,
            MultiBlockBlobCallback multiBlockCallback, MultiBlockBlobSealCallback multiBlockSealCallback, long bytesLeftInBlob)
        {
            var rollingId = new RollingBlobIdentifierWithBlocks();
            BlobIdentifierWithBlocks blobIdentifierWithBlocks = null;

            Lazy<List<Task>> tasks = new Lazy<List<Task>>(() => new List<Task>());
            do
            {
                ReadBlock(stream, blockActionSemaphore, bytesLeftInBlob,
                    (Pool<byte[]>.PoolHandle blockBufferHandle, int blockLength, BlobBlockHash blockHash) =>
                    {
                        bytesLeftInBlob -= blockLength;
                        bool isFinalBlock = (bytesLeftInBlob == 0);

                        try
                        {
                            if (isFinalBlock)
                            {
                                blobIdentifierWithBlocks = rollingId.Finalize(blockHash);
                            }
                            else
                            {
                                rollingId.Update(blockHash);
                            }
                        }
                        catch
                        {
                            CleanupBufferAndSemaphore(blockBufferHandle, blockActionSemaphore);
                            throw;
                        }

                        if (multiBlocksInParallel)
                        {
                            tasks.Value.Add(Task.Run(() =>
                            {
                                try
                                {
                                    multiBlockCallback(blockBufferHandle.Value, blockLength, blockHash, isFinalBlock);
                                }
                                finally
                                {
                                    CleanupBufferAndSemaphore(blockBufferHandle, blockActionSemaphore);
                                }
                            }));
                        }
                        else
                        {
                            try
                            {
                                multiBlockCallback(blockBufferHandle.Value, blockLength, blockHash, isFinalBlock);
                            }
                            finally
                            {
                                CleanupBufferAndSemaphore(blockBufferHandle, blockActionSemaphore);
                            }
                        }
                    });
            }
            while (bytesLeftInBlob > 0);

            if (tasks.IsValueCreated)
            {
                Task.WaitAll(tasks.Value.ToArray());
            }

            multiBlockSealCallback(blobIdentifierWithBlocks);
        }

        private static async Task WalkMultiBlockBlobAsync(Stream stream, SemaphoreSlim blockActionSemaphore, bool multiBlocksInParallel,
            MultiBlockBlobCallbackAsync multiBlockCallback, MultiBlockBlobSealCallbackAsync multiBlockSealCallback, long bytesLeftInBlob)
        {
            var rollingId = new RollingBlobIdentifierWithBlocks();
            BlobIdentifierWithBlocks blobIdentifierWithBlocks = null;

            var tasks = new List<Task>();
            do
            {
                await ReadBlockAsync(stream, blockActionSemaphore, bytesLeftInBlob,
                    async (Pool<byte[]>.PoolHandle blockBufferHandle, int blockLength, BlobBlockHash blockHash) =>
                    {
                        bytesLeftInBlob -= blockLength;
                        bool isFinalBlock = (bytesLeftInBlob == 0);

                        try
                        {
                            if (isFinalBlock)
                            {
                                blobIdentifierWithBlocks = rollingId.Finalize(blockHash);
                            }
                            else
                            {
                                rollingId.Update(blockHash);
                            }
                        }
                        catch
                        {
                            CleanupBufferAndSemaphore(blockBufferHandle, blockActionSemaphore);
                            throw;
                        }

                        Task multiBlockTask = Task.Run(async () =>
                        {
                            try
                            {
                                await multiBlockCallback(blockBufferHandle.Value, blockLength, blockHash, isFinalBlock).ConfigureAwait(false);
                            }
                            finally
                            {
                                CleanupBufferAndSemaphore(blockBufferHandle, blockActionSemaphore);
                            }
                        });
                        tasks.Add(multiBlockTask);

                        if (!multiBlocksInParallel)
                        {
                            await multiBlockTask.ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
            }
            while (bytesLeftInBlob > 0);

            await Task.WhenAll(tasks).ConfigureAwait(false);
            await multiBlockSealCallback(blobIdentifierWithBlocks).ConfigureAwait(false);
        }

        private static void CleanupBufferAndSemaphore(
            Pool<byte[]>.PoolHandle blockBufferHandle,
            SemaphoreSlim blockActionSemaphore)
        {
            blockBufferHandle.Dispose();
            if (blockActionSemaphore != null)
            {
                blockActionSemaphore.Release();
            }
        }

        private static void WalkSingleBlockBlob(Stream stream, SemaphoreSlim blockActionSemaphore, SingleBlockBlobCallback singleBlockCallback, long bytesLeftInBlob)
        {
            ReadBlock(stream, blockActionSemaphore, bytesLeftInBlob,
                (Pool<byte[]>.PoolHandle blockBufferHandle, int blockLength, BlobBlockHash blockHash) =>
                {
                    try
                    {
                        var rollingId = new RollingBlobIdentifierWithBlocks();
                        var blobIdentifierWithBlocks = rollingId.Finalize(blockHash);
                        singleBlockCallback(blockBufferHandle.Value, blockLength, blobIdentifierWithBlocks);
                    }
                    finally
                    {
                        blockBufferHandle.Dispose();
                        blockActionSemaphore?.Release();
                    }
                });
        }

        private static Task WalkSingleBlockBlobAsync(
            Stream stream,
            SemaphoreSlim blockActionSemaphore,
            SingleBlockBlobCallbackAsync singleBlockCallback,
            long bytesLeftInBlob)
        {
            return ReadBlockAsync(stream, blockActionSemaphore, bytesLeftInBlob,
                async (Pool<byte[]>.PoolHandle blockBufferHandle, int blockLength, BlobBlockHash blockHash) =>
                {
                    try
                    {
                        var rollingId = new RollingBlobIdentifierWithBlocks();
                        var blobIdentifierWithBlocks = rollingId.Finalize(blockHash);
                        await singleBlockCallback(blockBufferHandle.Value, blockLength, blobIdentifierWithBlocks).ConfigureAwait(false);
                    }
                    finally
                    {
                        blockBufferHandle.Dispose();
                        blockActionSemaphore?.Release();
                    }
                });
        }

#region IBlobHash
        BlobIdentifier IBlobHasher.OfNothing => OfNothing.BlobId;

        byte IBlobHasher.AlgorithmId
        {
            get
            {
                return AlgorithmId;
            }
        }

        public Task WalkBlocksAsync(Stream data, bool multiBlocksInParallel, SingleBlockBlobCallbackAsync singleBlockCallback, MultiBlockBlobCallbackAsync multiBlockCallback, MultiBlockBlobSealCallbackAsync multiBlockSealCallback)
        {
            return WalkBlocksAsync(data, null, multiBlocksInParallel, singleBlockCallback, multiBlockCallback, multiBlockSealCallback);
        }

        async Task<BlobIdentifier> IBlobHasher.CalculateBlobIdentifierAsync(Stream data)
        {
            return (await CalculateBlobIdentifierWithBlocksAsync(data).ConfigureAwait(false)).BlobId;
        }

        BlobBlockHash IBlobHasher.CalculateBlobBlockHash(byte[] data, int length)
        {
            return HashBlock(data, length);
        }

        Task<BlobIdentifierWithBlocks> IBlobHasher.CalculateBlobIdentifierWithBlocksAsync(Stream data)
        {
            return CalculateBlobIdentifierWithBlocksAsync(data);
        }

        public BlobIdentifier CalculateBlobIdentifierFromBlobBlockHashes(IEnumerable<BlobBlockHash> blocks)
        {
            var rollingId = new VsoHash.RollingBlobIdentifier();
            IEnumerator<BlobBlockHash> enumerator = blocks.GetEnumerator();

            bool isLast = !enumerator.MoveNext();
            if (isLast)
            {
                throw new InvalidDataException("Blob must have at least one block.");
            }

            BlobBlockHash current = enumerator.Current;
            isLast = !enumerator.MoveNext();
            while (!isLast)
            {
                rollingId.Update(current);
                current = enumerator.Current;
                isLast = !enumerator.MoveNext();
            }
            return rollingId.Finalize(current);
        }

#endregion

        public class RollingBlobIdentifierWithBlocks
        {
            private List<BlobBlockHash> blockHashes;
            private readonly RollingBlobIdentifier inner;

            public RollingBlobIdentifierWithBlocks()
            {
                this.inner = new RollingBlobIdentifier();
                this.blockHashes = new List<BlobBlockHash>();
            }

            public void Update(BlobBlockHash currentBlockIdentifier)
            {
                blockHashes.Add(currentBlockIdentifier);
                inner.Update(currentBlockIdentifier);
            }

            public BlobIdentifierWithBlocks Finalize(BlobBlockHash currentBlockIdentifier)
            {
                blockHashes.Add(currentBlockIdentifier);
                var blobId = inner.Finalize(currentBlockIdentifier);
                return new BlobIdentifierWithBlocks(blobId, blockHashes);
            }
        }

        public class RollingBlobIdentifier
        {
            private static readonly byte[] InitialRollingId = Encoding.ASCII.GetBytes("VSO Content Identifier Seed");
            private byte[] rollingId = InitialRollingId;
            private bool finalAdded;

            public void Update(BlobBlockHash currentBlockIdentifier)
            {
                // TODO if we want to enforce this we should implement BlobBlockHash.BlockSize
                //
                // var currentBlockSize = currentBlockIdentifier.BlockSize;
                // if (currentBlockSize != BlockSize)
                // {
                //     throw new InvalidOperationException($"Non-final blocks must be of size {BlockSize}; but the given block has size {currentBlockSize}");
                // }
                UpdateInternal(currentBlockIdentifier, false);
            }

            public BlobIdentifier Finalize(BlobBlockHash currentBlockIdentifier)
            {
                // TODO if we want to enforce this we should implement BlobBlockHash.BlockSize
                //
                // if (blockSize > BlockSize)
                // {
                //     throw new InvalidOperationException("Blocks cannot be bigger than BlockSize.");
                // }

                UpdateInternal(currentBlockIdentifier, true);
                return new BlobIdentifier(rollingId, AlgorithmId);
            }

            private void UpdateInternal(BlobBlockHash currentBlockIdentifier, bool isFinalBlock)
            {
                if (finalAdded && isFinalBlock)
                {
                    throw new InvalidOperationException("Final block already added.");
                }

                var resultBuffer = new List<byte>(rollingId);
                resultBuffer.AddRange(currentBlockIdentifier.HashBytes);
                resultBuffer.Add(Convert.ToByte(isFinalBlock));
                rollingId = ComputeSHA256Hash(resultBuffer);

                if (isFinalBlock)
                {
                    finalAdded = true;
                }
            }
        }
    }
}
