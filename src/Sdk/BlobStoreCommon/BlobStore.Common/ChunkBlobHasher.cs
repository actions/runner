using System;

namespace GitHub.Services.BlobStore.Common
{
    public class ChunkBlobHasher : DedupBlobHasher
    {
        public const byte AlgorithmId = ChunkDedupIdentifier.ChunkAlgorithmId;

        private static readonly Lazy<BlobIdentifier> ofNothing = new Lazy<BlobIdentifier>(
            () => ChunkDedupIdentifier.CalculateIdentifier(new byte[] { }).ToBlobIdentifier());

        public static readonly DedupBlobHasher Instance = new ChunkBlobHasher();

        public ChunkBlobHasher()
            : base(AlgorithmId)
        {
        }

        public override BlobIdentifier OfNothing => ofNothing.Value;
    }
}
