using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.Content.Common;
using Newtonsoft.Json;

namespace GitHub.Services.BlobStore.Common
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BlobIdentifierWithBlocks : IEquatable<BlobIdentifierWithBlocks>
    {
        // Serialization proxy for BlobId
        // TODO Remove with BlobStore v1
        [JsonProperty(PropertyName = "identifierValue")]
        [JsonConverter(typeof(ByteArrayAsNumberArrayJsonConvertor))]
        private byte[] identifierValue
        {
            get
            {
                return this.BlobId?.Bytes;
            }

            set
            {
                this.BlobId = new BlobIdentifier(value);
            }
        }

        /// <summary>
        /// The globally unique identities of each chunk in this blob
        /// </summary>
        [JsonProperty(PropertyName = "BlockHashes")]
        public IList<BlobBlockHash> BlockHashes { get; set; }

        public BlobIdentifier BlobId { get; private set; }

        private static readonly char[] SplitCharacters = { ',' };

        // Just for serialization
        public BlobIdentifierWithBlocks()
        {
        }

        public BlobIdentifierWithBlocks(BlobIdentifier blobId, IEnumerable<BlobBlockHash> blockIdentifiers)
        {
            this.BlockHashes = blockIdentifiers.ToList();
            this.BlobId = blobId;
            Validate();
        }

        public static BlobIdentifierWithBlocks Deserialize(string serialized)
        {
            // Marked "new" 
            string[] tokens = serialized.Split(':');
            return new BlobIdentifierWithBlocks(
                BlobIdentifier.Deserialize(tokens[0]), 
                tokens[1].Split(SplitCharacters, StringSplitOptions.RemoveEmptyEntries).Select(idString => new BlobBlockHash(idString)).ToList());
        }

        public string Serialize()
        {
            return string.Format("{0}:{1}", BlobId.ValueString, string.Join(",", BlockHashes.Select(id => id.HashString)));
        }

        public bool BlocksContainThisId(IEnumerable<BlobBlockHash> blocks)
        {
            return this.BlockHashes.All(blockHash => blocks.Any(block => block == blockHash));
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Validate();
        }

        private void Validate()
        {
            if (BlobId == null)
            {
                throw new ArgumentNullException(nameof(BlobId));
            }

            if (BlockHashes == null)
            {
                throw new ArgumentNullException(nameof(BlockHashes));
            }

            BlobIdentifier computedBlobId = BlobId.GetBlobHasher().CalculateBlobIdentifierFromBlobBlockHashes(BlockHashes);
            if (!BlobId.Equals(computedBlobId))
            {
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Computed BlobIdentifier '{0}' does not match given one '{1}'.",
                    computedBlobId, BlobId));
            }
        }

        /// <summary>
        /// Equality is based on the BlobId and the type.
        /// </summary>
        public override bool Equals(Object obj) => Equals(obj as BlobIdentifierWithBlocks);

        /// <summary>
        /// Equality is based on the BlobId and the type.
        /// </summary>
        public bool Equals(BlobIdentifierWithBlocks other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return !object.ReferenceEquals(other, null) && this.BlobId.Equals(other.BlobId);
        }

        /// <summary>
        /// The hash is computed from the BlobIdand tagged with the type to
        /// distiguish it from the respective BlobIdentifier.
        /// </summary>
        public override int GetHashCode()
        {
            return EqualityHelper.GetCombinedHashCode(BlobId, this.GetType());
        }

        /// <summary>
        /// Returns a user-friendly, non-canonical string representation of the unique identifier for binary content
        /// </summary>
        /// <returns>
        /// A user-friendly, non-canonical string representation of the content identifier
        /// </returns>
        public override string ToString()
        {
            return $"BlobWithBlocks:{this.Serialize()}";
        }

        public int CompareTo(object obj)
        {
            if (!(obj is BlobIdentifierWithBlocks))
            {
                throw new ArgumentException("Object is not a BlobIdentifierWithBlocks");
            }

            return this.CompareTo((BlobIdentifierWithBlocks)obj);
        }

        public int CompareTo(BlobIdentifierWithBlocks other)
        {
            return other == null ? 1 : string.Compare(this.Serialize(), other.Serialize(), StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool operator ==(BlobIdentifierWithBlocks x, BlobIdentifierWithBlocks y)
        {
            if (ReferenceEquals(x, null))
            {
                return ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        public static bool operator !=(BlobIdentifierWithBlocks x, BlobIdentifierWithBlocks y)
        {
            return !(x == y);
        }
    }
}
