using System;
using System.ComponentModel;
using System.Linq;
using BuildXL.Cache.ContentStore.Hashing;
using GitHub.Services.Content.Common;
using BuildXL.Cache.ContentStore.Interfaces.Utils;
using Newtonsoft.Json;

namespace GitHub.Services.BlobStore.Common
{
    public struct HashAndAlgorithm
    {
        public readonly byte[] Bytes;

        public HashAndAlgorithm(byte[] bytes)
        {
            this.Bytes = bytes;
        }

        public byte AlgorithmId => this.Bytes[this.Bytes.Length - 1];
    }

    public class ChunkDedupIdentifier : DedupIdentifier
    {
        private static readonly IContentHasher chunkHasher = DedupChunkHashInfo.Instance.CreateContentHasher();

        public const byte ChunkAlgorithmId = 1;

        public ChunkDedupIdentifier(HashAndAlgorithm hash)
            : base(hash)
        {
            Validate();
        }

        public ChunkDedupIdentifier(byte[] hashResult)
            : base(hashResult, ChunkAlgorithmId)
        {
            Validate();
        }

        public static ChunkDedupIdentifier CalculateIdentifier(byte[] bytes)
        {
            return new ChunkDedupIdentifier(chunkHasher.GetContentHash(bytes).ToHashByteArray());
        }

        public static ChunkDedupIdentifier CalculateIdentifier(byte[] bytes, int offset, int count)
        {
            return new ChunkDedupIdentifier(chunkHasher.GetContentHash(bytes, offset, count).ToHashByteArray());
        }

        public static ChunkDedupIdentifier CalculateIdentifier(ArraySegment<byte> range)
        {
            return new ChunkDedupIdentifier(chunkHasher.GetContentHash(range.Array, range.Offset, range.Count).ToHashByteArray());
        }

        public static ChunkDedupIdentifier Parse(string value)
        {
            return (ChunkDedupIdentifier)Create(value);
        }

        private void Validate()
        {
            if (AlgorithmId != ChunkAlgorithmId)
            {
                throw new ArgumentException($"The given hash does not represent a {nameof(ChunkDedupIdentifier)}");
            }
        }
    }

    public class NodeDedupIdentifier : DedupIdentifier
    {
        private static readonly IContentHasher chunkHasher = DedupChunkHashInfo.Instance.CreateContentHasher();

        public const byte NodeAlgorithmId = 2;

        public NodeDedupIdentifier(HashAndAlgorithm hash)
            : base(hash)
        {
            Validate();
        }

        public NodeDedupIdentifier(byte[] algorithmResult)
            : base(algorithmResult, NodeAlgorithmId)
        {
            Validate();
        }

        public static NodeDedupIdentifier CalculateIdentifierFromSerializedNode(byte[] bytes)
        {
            return new NodeDedupIdentifier(chunkHasher.GetContentHash(bytes).ToHashByteArray());
        }

        public static NodeDedupIdentifier CalculateIdentifierFromSerializedNode(byte[] bytes, int offset, int count)
        {
            return new NodeDedupIdentifier(chunkHasher.GetContentHash(bytes, offset, count).ToHashByteArray());
        }

        public static NodeDedupIdentifier CalculateIdentifierFromSerializedNode(ArraySegment<byte> bytes)
        {
            return new NodeDedupIdentifier(chunkHasher.GetContentHash(bytes.Array, bytes.Offset, bytes.Count).ToHashByteArray());
        }

        public static NodeDedupIdentifier Parse(string value)
        {
            return (NodeDedupIdentifier)Create(value);
        }

        private void Validate()
        {
            if (AlgorithmId != NodeAlgorithmId)
            {
                throw new ArgumentException($"The given hash does not represent a {nameof(NodeDedupIdentifier)}");
            }
        }
    }

    public static class DedupNodeExtensions
    {
        private static readonly IContentHasher chunkHasher = DedupChunkHashInfo.Instance.CreateContentHasher();

        [CLSCompliant(false)]
        public static NodeDedupIdentifier CalculateNodeDedupIdentifier(this DedupNode node)
        {
            // node.Serialize() will fail if this is node is a chunk.
            return new NodeDedupIdentifier(chunkHasher.GetContentHash(node.Serialize()).ToHashByteArray());
        }

        [CLSCompliant(false)]
        public static ChunkDedupIdentifier GetChunkId(this DedupNode node)
        {
            if(node.Type != DedupNode.NodeType.ChunkLeaf)
            {
                throw new ArgumentException($"The given hash does not represent a {nameof(ChunkDedupIdentifier)}");
            }
            return new ChunkDedupIdentifier(node.Hash);
        }

        [CLSCompliant(false)]
        public static NodeDedupIdentifier GetNodeId(this DedupNode node)
        {
            if(node.Type != DedupNode.NodeType.InnerNode)
            {
                throw new ArgumentException($"The given hash does not represent a {nameof(NodeDedupIdentifier)}");
            }
            return new NodeDedupIdentifier(node.Hash);
        }

        [CLSCompliant(false)]
        public static DedupIdentifier GetDedupId(this DedupNode node)
        {
            if (node.Type == DedupNode.NodeType.InnerNode)
            {
                return node.GetNodeId();
            }
            else
            {
                return node.GetChunkId();
            }
        }
    }

    // As Newtonsoft.Json upgraded to v10.0.1 or newer, we should bring back TypeConverter as it makes serialization of Dictionaries simpler
    //[TypeConverter(typeof(DedupIdentifierTypeConverter))]
    [JsonConverter(typeof(DedupIdentifierJsonConvertor))]
    public abstract class DedupIdentifier : IEquatable<DedupIdentifier>, IComparable<DedupIdentifier>, ILongHash
    {
        private readonly byte[] value;

        protected DedupIdentifier(HashAndAlgorithm hashAndAlgorithm)
        {
            if (null == hashAndAlgorithm.Bytes)
            {
                throw new ArgumentNullException(nameof(hashAndAlgorithm));
            }

            this.value = hashAndAlgorithm.Bytes;
            this.ValidateLength();
        }

        protected DedupIdentifier(byte[] algorithmResult, byte algorithmId)
        {
            if (null == algorithmResult)
            {
                throw new ArgumentNullException(nameof(algorithmResult));
            }

            this.value = new byte[algorithmResult.Length + 1];
            algorithmResult.CopyTo(value, 0);
            this.value[algorithmResult.Length] = algorithmId;
            this.ValidateLength();
        }

        public byte[] AlgorithmResult => this.value.Take(AlgorithmIdIndex).ToArray();

        public string AlgorithmResultString => this.AlgorithmResult.ToHexString();

        public string ValueString => this.value.ToHexString();

        public byte[] Value
        {
            get
            {
                byte[] copy = new byte[this.value.Length];
                this.value.CopyTo(copy, 0);
                return copy;
            }
        }

        public byte AlgorithmId => this.value[this.AlgorithmIdIndex];

        private int AlgorithmIdIndex => this.value.Length - 1;

        private void ValidateLength()
        {
            if (this.AlgorithmIdIndex != 32)
            {
                throw new ArgumentException(BlobStoreCommonResources.InvalidHashLength(this.value));
            }
        }

        public bool Equals(DedupIdentifier other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.value.SequenceEqual(other.value);
        }

        public override bool Equals(Object obj) => Equals(obj as DedupIdentifier);

        public override string ToString()
        {
            return ValueString;
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(this.value, 0);
        }

        public long GetLongHashCode()
        {
            return BitConverter.ToInt64(this.value, 0);
        }

        public NodeDedupIdentifier CastToNodeDedupIdentifier()
        {
            return new NodeDedupIdentifier(new HashAndAlgorithm(this.Value));
        }

        public ChunkDedupIdentifier CastToChunkDedupIdentifier()
        {
            return new ChunkDedupIdentifier(new HashAndAlgorithm(this.Value));
        }

        public static bool operator ==(DedupIdentifier x, DedupIdentifier y)
        {
            if (ReferenceEquals(x, null))
            {
                return ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        public static bool operator !=(DedupIdentifier x, DedupIdentifier y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Create a dedup identifier from a string.
        /// </summary>
        /// <remarks>
        /// <pre>
        /// This method has two purposes:
        /// 1) Create is overloaded, so any library referencing Create() must be able to resolve all the 
        ///    parameter types across all the overloaded versions, which include 
        ///    BuildXL.Cache.ContentStore.Hashing.DedupNode. This method can help reduce compile-time dependency.
        /// 2) To add some API consistency, it has the same signature as BlobIdentifier.Deserialize(string).
        /// </pre>
        /// </remarks>
        public static DedupIdentifier Deserialize(string valueIncludingAlgorithm)
        {
            return Create(valueIncludingAlgorithm);
        }

        public static DedupIdentifier Create(byte[] algorithmResult, byte algorithmId)
        {
            switch (algorithmId)
            {
                case ChunkDedupIdentifier.ChunkAlgorithmId:
                    return new ChunkDedupIdentifier(algorithmResult);
                case NodeDedupIdentifier.NodeAlgorithmId:
                    return new NodeDedupIdentifier(algorithmResult);
                default:
                    throw new NotSupportedException($"Unknown algorithm {algorithmId}");
            }
        }

        public static DedupIdentifier Create(string valueIncludingAlgorithm)
        {
            if (String.IsNullOrWhiteSpace(valueIncludingAlgorithm))
            {
                throw new ArgumentNullException(nameof(valueIncludingAlgorithm), BlobStoreCommonResources.InvalidContentHashValue("null"));
            }

            byte[] value;
            // Ignore the result of this call as ValidateInternal will check for null.
            Content.Common.HexUtilities.TryToByteArray(valueIncludingAlgorithm, out value);

            return DedupIdentifier.Create(new HashAndAlgorithm(value));
        }

        [CLSCompliant(false)]
        public static DedupIdentifier Create(DedupNode node)
        {
            return Create(
                node.Hash,
                node.Type == DedupNode.NodeType.ChunkLeaf
                    ? ChunkDedupIdentifier.ChunkAlgorithmId
                    : NodeDedupIdentifier.NodeAlgorithmId);
        }

        public static DedupIdentifier Create(HashAndAlgorithm hashAndAlgorithm)
        {
            if (null == hashAndAlgorithm.Bytes)
            {
                throw new ArgumentNullException(nameof(hashAndAlgorithm));
            }

            byte algorithmId = hashAndAlgorithm.AlgorithmId;
            if (algorithmId == ChunkDedupIdentifier.ChunkAlgorithmId)
            {
                return new ChunkDedupIdentifier(hashAndAlgorithm);
            }
            else if (algorithmId == NodeDedupIdentifier.NodeAlgorithmId)
            {
                return new NodeDedupIdentifier(hashAndAlgorithm);
            }
            else
            {
                throw new NotSupportedException($"Unknown algorithm {algorithmId}");
            }
        }

        public int CompareTo(DedupIdentifier other)
        {
            if(other == null)
            {
                return -1;
            }

            return ByteArrayComparer.Instance.Compare(this.value, other.value);
        }
    }
}
