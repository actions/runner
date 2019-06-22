namespace GitHub.Services.BlobStore.Common
{
    public static class DedupIdentifierExtensions
    {
        public static BlobIdentifier ToBlobIdentifier(this DedupIdentifier dedupId)
        {
            return new BlobIdentifier(dedupId.AlgorithmResult, dedupId.AlgorithmId);
        }

        public static DedupIdentifier ToDedupIdentifier(this BlobIdentifier blobId)
        {
            return DedupIdentifier.Create(blobId.AlgorithmResultBytes, blobId.AlgorithmId);
        }
    }
}
