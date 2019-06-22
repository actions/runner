using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.Common
{
    public delegate void MultiBlockBlobCallback(byte[] block, int blockLength, BlobBlockHash blockHash, bool isFinalBlock);

    public delegate Task MultiBlockBlobCallbackAsync(byte[] block, int blockLength, BlobBlockHash blockHash, bool isFinalBlock);

    public delegate void MultiBlockBlobSealCallback(BlobIdentifierWithBlocks blobIdWithBlocks);

    public delegate Task MultiBlockBlobSealCallbackAsync(BlobIdentifierWithBlocks blobIdWithBlocks);

    public delegate void SingleBlockBlobCallback(byte[] block, int blockLength, BlobIdentifierWithBlocks blobIdWithBlocks);

    public delegate Task SingleBlockBlobCallbackAsync(byte[] block, int blockLength, BlobIdentifierWithBlocks blobIdWithBlocks);

    public interface IBlobHasher
    {
        byte AlgorithmId { get; }

        BlobIdentifier OfNothing { get; }
        
        Task<BlobIdentifier> CalculateBlobIdentifierAsync(Stream data);
        
        Task<BlobIdentifierWithBlocks> CalculateBlobIdentifierWithBlocksAsync(Stream data);

        BlobBlockHash CalculateBlobBlockHash(byte[] data, int length);

        BlobIdentifier CalculateBlobIdentifierFromBlobBlockHashes(IEnumerable<BlobBlockHash> blocks);

        Task WalkBlocksAsync(
            Stream data,
            bool multiBlocksInParallel,
            SingleBlockBlobCallbackAsync singleBlockCallback,
            MultiBlockBlobCallbackAsync multiBlockCallback,
            MultiBlockBlobSealCallbackAsync multiBlockSealCallback
            );
    }
}
