using System;
using System.Linq;
using System.Runtime.Serialization;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.UtilitiesCore;
using GitHub.Services.Content.Common;
using Newtonsoft.Json;

namespace GitHub.Services.BlobStore.Common
{
    /// <summary>
    /// Represents a hash identifier for content stored in the Content Repository.
    /// Internally represented as a byte array of the alogrithm result with a single byte algorithm identifier appended.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BlobIdentifier : IEquatable<BlobIdentifier>, IComparable, ILongHash, IHashCount
    {
        private const int MinimumIdentifierValueByteCount = 4; 
        private const int MinimumAlgorithmResultByteCount = MinimumIdentifierValueByteCount - 1;

        [JsonProperty(PropertyName = "identifierValue")]
        [JsonConverter(typeof(ByteArrayAsNumberArrayJsonConvertor))]
        private readonly byte[] identifierValue;

        public static readonly BlobIdentifier MinValue = CreateFromAlgorithmResult(Enumerable.Repeat<byte>(byte.MinValue, 32).ToArray(), algorithmId: byte.MinValue);
        public static readonly BlobIdentifier MaxValue = CreateFromAlgorithmResult(Enumerable.Repeat<byte>(byte.MaxValue, 32).ToArray(), algorithmId: byte.MaxValue);

        public BlobIdentifier()
        {
        }

        public BlobIdentifier(byte[] algorithmResult, byte algorithmId)
        {
            if (null == algorithmResult)
            {
                throw new ArgumentNullException(nameof(algorithmResult), BlobStoreCommonResources.InvalidContentHashValue("null"));
            }

            // copy algorithmResult and append AlgorithmId to identifierValue
            identifierValue = new byte[algorithmResult.Length + 1];
            algorithmResult.CopyTo(identifierValue, 0);
            identifierValue[algorithmResult.Length] = algorithmId;
            Validate();
        }

        /// <summary>
        /// Create a new identifier based on the given value.  
        /// </summary>
        /// <remarks>
        /// The value is expected to contain both the hash and the algorithm id.
        /// </remarks>
        /// <param name="value">Must be the value corresponding to the Bytes of the id to be created.</param>
        public BlobIdentifier(byte[] value)
        {
            identifierValue = value;
            Validate();
        }

        /// <summary>
        /// Create a new identifier based on the given value.  
        /// </summary>
        /// <remarks>
        /// The value is expected to contain both the hash and the algorithm id.
        /// </remarks>
        /// <param name="valueIncludingAlgorithm">Must be the value corresponding to the ValueString of the id to be created.</param>
        private BlobIdentifier(string valueIncludingAlgorithm)
        {
            if (String.IsNullOrWhiteSpace(valueIncludingAlgorithm))
            {
                throw new ArgumentNullException(nameof(valueIncludingAlgorithm), BlobStoreCommonResources.InvalidContentHashValue("null"));
            }

            // Ignore the result of this call as ValidateInternal will check for null.
            HexUtilities.TryToByteArray(valueIncludingAlgorithm, out identifierValue);
            Validate();
        }

        /// <summary>
        /// Gets the unique identifier for binary content computed when the
        /// class instance was created
        /// This is *NOT* the complete value as it *excludes* the AlgorithmId suffix.
        /// </summary>
        public byte[] AlgorithmResultBytes => this.identifierValue.Take(AlgorithmIdIndex).ToArray();

        /// <summary>
        /// AlgorithmResult in HexString format (ex:  54CE418A2A89A74B42CC3963)
        /// </summary>
        public string AlgorithmResultString => this.AlgorithmResultBytes.ToHexString();

        /// <summary>
        /// Gets the unique identifier for binary content computed when the
        /// class instance was created  (ex:  54CE418A2A89A74B42CC396301). 
        /// This is the complete value as it includes the AlgorithmId suffix.
        /// </summary>
        public string ValueString => this.identifierValue.ToHexString();

        /// <summary>
        /// Gets a copy of byte array underlying this identifier.
        /// </summary>
        public byte[] Bytes
        {
            get
            {
                byte[] copy = new byte[identifierValue.Length];
                identifierValue.CopyTo(copy, 0);
                return copy;
            }
        }

        /// <summary>
        /// Gets the (single byte) algorithm id used to generate the blob identifier (hash).
        /// </summary>
        public byte AlgorithmId => this.identifierValue[this.AlgorithmIdIndex];

        public bool IsOfNothing => this == this.GetBlobHasher().OfNothing;

        [JsonIgnore]
        public BlobDedupLevel BlobDedupLevel
        {
            get
            {
                switch (this.AlgorithmId)
                {
                    case VsoHash.AlgorithmId:
                        return BlobDedupLevel.FileLevel;
                    case ChunkBlobHasher.AlgorithmId:
                    case NodeBlobHasher.AlgorithmId:
                        return BlobDedupLevel.ChunkLevel;
                    default:
                        return BlobDedupLevel.Unknown;
                }
            }
        }

        public IBlobHasher GetBlobHasher()
        {
            switch(this.AlgorithmId)
            {
                case VsoHash.AlgorithmId:
                    return VsoHash.Instance;
                case NodeBlobHasher.AlgorithmId:
                    return NodeBlobHasher.Instance;
                case ChunkBlobHasher.AlgorithmId:
                    return ChunkBlobHasher.Instance;
                default:
                    throw new NotSupportedException($"Unknown algorithm {this.AlgorithmId}");
            }
        }

        [CLSCompliant(false)]
        public ContentHash ToContentHash()
        {
            switch (AlgorithmId)
            {
                case VsoHash.AlgorithmId:
                    return new ContentHash(HashType.Vso0, Bytes);
                case NodeDedupIdentifier.NodeAlgorithmId:
                case ChunkDedupIdentifier.ChunkAlgorithmId:
                    return new ContentHash(HashType.DedupNodeOrChunk, Bytes);
                default:
                    throw new InvalidOperationException($"Unknown algorithm ID: {AlgorithmId}");
            }
        }

        private int AlgorithmIdIndex => identifierValue.Length - 1;

        public static BlobIdentifier CreateFromAlgorithmResult(string algorithmResult, byte algorithmId = VsoHash.AlgorithmId)
        {
            return new BlobIdentifier(HexUtilities.ToByteArray(algorithmResult), algorithmId);
        }

        public static BlobIdentifier CreateFromAlgorithmResult(byte[] algorithmResult, byte algorithmId = VsoHash.AlgorithmId)
        {
            return new BlobIdentifier(algorithmResult, algorithmId);
        }

        public static BlobIdentifier Deserialize(string valueIncludingAlgorithm)
        {
            return new BlobIdentifier(valueIncludingAlgorithm);
        }

        public static bool operator ==(BlobIdentifier x, BlobIdentifier y)
        {
            if (ReferenceEquals(x, null))
            {
                return ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        public static bool operator !=(BlobIdentifier x, BlobIdentifier y)
        {
            return !(x == y);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            Validate();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Validate();
        }

        /// <summary>
        /// Returns true/false whether the object is equal to the current <see cref="BlobIdentifier"/>
        /// </summary>
        /// <param name="obj">The object to compare against the current instance</param>
        /// <returns>
        /// <c>true</c> if the objects are equal, otherwise <c>false</c>.
        /// </returns>
        public override bool Equals(Object obj) => Equals(obj as BlobIdentifier);

        /// <summary>
        /// Returns true/false whether the <see cref="BlobIdentifier"/> is equal to the current <see cref="BlobIdentifier"/>
        /// </summary>
        /// <param name="other">The <see cref="BlobIdentifier"/> to compare against the current instance</param>
        /// <returns>
        /// <c>true</c> if the objects are equal, otherwise <c>false</c>.
        /// </returns>
        public bool Equals(BlobIdentifier other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return !object.ReferenceEquals(other, null) && identifierValue.SequenceEqual(other.identifierValue);
        }

        /// <summary>
        /// Gets the unique hash for this unique identifier for binary content.
        /// </summary>
        /// <returns>
        /// A hash value for the content identifier
        /// </returns>
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(identifierValue, 0);
        }

        public long GetLongHashCode()
        {
            return BitConverter.ToInt64(identifierValue, 0);
        }

        /// <summary>
        /// Returns a user-friendly, non-canonical string representation of the unique identifier for binary content
        /// </summary>
        /// <returns>
        /// A user-friendly, non-canonical string representation of the content identifier
        /// </returns>
        public override string ToString()
        {
            return $"Blob:{ValueString}";
        }

        public int CompareTo(object obj)
        {
            if (!(obj is BlobIdentifier))
            {
                throw new ArgumentException("Object is not a BlobIdentifier");
            }

            return this.CompareTo((BlobIdentifier)obj);
        }

        public int CompareTo(BlobIdentifier other)
        {
            return other == null ? 1 : string.Compare(this.ValueString, other.ValueString, StringComparison.InvariantCultureIgnoreCase);
        }

        private void Validate()
        {
            if (identifierValue == null)
            {
                throw new ArgumentException(BlobStoreCommonResources.InvalidContentHashValue(identifierValue));
            }

            int algorithmResultLength = identifierValue.Length - 1;

            // The final byte array needs to be at least 4 bytes long for GetHashCode to work.
            // We prevent ourselves from accidentally passing the wrong byte array (with the algorithm id instead of without
            // or vice versa) by requiring that all algorithm results have an even length.  Since the given string should
            // have the algorithm id, it should be an odd number of bytes.
            if ((algorithmResultLength < MinimumAlgorithmResultByteCount) || (algorithmResultLength % 2 != 0))
            {
                throw new ArgumentException(BlobStoreCommonResources.InvalidHashLength(identifierValue), nameof(identifierValue));
            }
        }

        public int GetByteCount()
        {
            return this.Bytes.Length;
        }

        /// <summary>
        /// Produces a non-cryptographic pseudo random BlobIdentifier. This function must not be used
        /// when it is required that the result can't be predicted.
        /// </summary>
        [CLSCompliant(false)]
        public static BlobIdentifier Random(HashType hashType = HashType.Vso0)
        {
            var randomBlob = new byte[32];
            ThreadSafeRandom.Generator.NextBytes(randomBlob);
            return CreateFromAlgorithmResult(randomBlob, AlgorithmIdLookup.Find(hashType));
        }
    }

    [CLSCompliant(false)]
    public static class BlobIdentifierExtensions
    {
        /// <summary>
        /// Takes a given BlobIdentifier and maps it to within a range of unsigned integers by using the first
        /// four bytes of the blob identifier.
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="firstValue">The lowest unsigned integer to return.</param>
        /// <param name="count">The range of the unsigned integers.  The highest value that can be returned is (firstValue + count - 1).</param>
        /// <remarks>Useful for exploiting the uniform distribution of hashes.</remarks>
        public static uint MapToIntegerRange(this BlobIdentifier blobId, uint firstValue, uint count)
        {
            if(count == 0)
            {
                throw new ArgumentException("count must be positive.");
            }

            if (UInt32.MaxValue - count < firstValue)
            {
                throw new OverflowException("firstValue + count exceeds int.MaxValue");
            }

            // Pull out bytes as big-endian
            UInt64 me = blobId.Bytes[0];
            me <<= 8;
            me |= blobId.Bytes[1];
            me <<= 8;
            me |= blobId.Bytes[2];
            me <<= 8;
            me |= blobId.Bytes[3];

            // Map range
            me *= (count - 1);
            me /= UInt32.MaxValue;
            me += firstValue;
            return (uint)me;
        }

        /// <summary>
        /// Converts a ContentStore ContentHash to a Blob Identifier.
        /// </summary>
        public static BlobIdentifier ToBlobIdentifier(this ContentHash contentHash)
        {
            switch (contentHash.HashType)
            {
                case HashType.Vso0:
                case HashType.DedupNodeOrChunk:
                    return BlobIdentifier.Deserialize(contentHash.ToHex());
                default:
                    throw new ArgumentException($"ContentHash has unsupported type when converting to BlobIdentifier: {contentHash.HashType}");
            }
        }

    }
}
