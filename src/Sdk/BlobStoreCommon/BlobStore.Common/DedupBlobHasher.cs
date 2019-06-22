using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Hashing;

namespace GitHub.Services.BlobStore.Common
{
    public abstract class DedupBlobHasher : IBlobHasher
    {
        private static readonly IContentHasher chunkHasher = DedupChunkHashInfo.Instance.CreateContentHasher();

        private readonly byte algorithmId;

        protected DedupBlobHasher(byte algorithmId)
        {
            this.algorithmId = algorithmId;
        }

        byte IBlobHasher.AlgorithmId => this.algorithmId;

        public abstract BlobIdentifier OfNothing { get; }

        public BlobBlockHash CalculateBlobBlockHash(byte[] data, int length)
        {
            return new BlobBlockHash(chunkHasher.GetContentHash(data, 0, length).ToHashByteArray());
        }

        public async Task<BlobIdentifier> CalculateBlobIdentifierAsync(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                return new BlobIdentifier(chunkHasher.GetContentHash(memoryStream.ToArray()).ToHashByteArray(), this.algorithmId);
            }
        }

        public async Task<BlobIdentifierWithBlocks> CalculateBlobIdentifierWithBlocksAsync(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                var hash = chunkHasher.GetContentHash(memoryStream.ToArray()).ToHashByteArray();
                memoryStream.Position = 0;
                var blobId = await this.CalculateBlobIdentifierAsync(memoryStream).ConfigureAwait(false);
                return new BlobIdentifierWithBlocks(blobId, new[] { new BlobBlockHash(hash) });
            }
        }

        public BlobIdentifier CalculateBlobIdentifierFromBlobBlockHashes(IEnumerable<BlobBlockHash> blocks)
        {
            if(blocks.Count() != 1)
            {
                throw new ArgumentException("DedupBlobs can only have one block");
            }

            return new BlobIdentifier(blocks.FirstOrDefault().HashBytes, this.algorithmId);
        }

        public async Task WalkBlocksAsync(Stream stream, bool multiBlocksInParallel, SingleBlockBlobCallbackAsync singleBlockCallback, MultiBlockBlobCallbackAsync multiBlockCallback, MultiBlockBlobSealCallbackAsync multiBlockSealCallback)
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                var data = memoryStream.ToArray();
                memoryStream.Position = 0;
                var blobIdWithBlocks = await this.CalculateBlobIdentifierWithBlocksAsync(memoryStream).ConfigureAwait(false);
                await singleBlockCallback(data, data.Length, blobIdWithBlocks).ConfigureAwait(false);
            }
        }
    }
}
