using System;

namespace GitHub.Services.BlobStore.Common
{
    public class NodeBlobHasher: DedupBlobHasher
    {
        public const byte AlgorithmId = NodeDedupIdentifier.NodeAlgorithmId;

        public static readonly DedupBlobHasher Instance = new NodeBlobHasher();

        public NodeBlobHasher() : base(AlgorithmId)
        {
        }

        public override BlobIdentifier OfNothing
        {
            get
            {
                throw new InvalidOperationException();
            }
        }
    }
}
