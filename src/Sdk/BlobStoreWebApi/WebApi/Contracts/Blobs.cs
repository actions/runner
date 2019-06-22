using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.WebApi.Contracts
{
    [DataContract]
    public class Blob : IEquatable<Blob>
    {
        public Blob()
        {
        }

        public Blob(string blobId)
        {
            this.Id = blobId;
        }

        public Blob(BlobIdentifier blobId) : this(blobId.ValueString)
        {
        }

        public Blob(BlobIdentifierWithBlocks blobIdWithBlocks) : this(blobIdWithBlocks.BlobId)
        {
            this.BlockHashes = blobIdWithBlocks.BlockHashes.Select(blockHash => blockHash.HashString).ToList();
        }

        /// <summary>
        /// ID of the blob. It is calculated by a specialized hashing algorithm from the blob content.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// A SAS-based URL, containing an implicit TTL (time-to-live) value for downloading the blob. 
        /// The caller is advised to (1) treat the URL as sensible data (2) download the blob in a timely fashion.
        /// </summary>
        /// <remarks>
        /// This value is only provided when Azure storagre is used at the server side. If provided, the URL
        /// will be only availble for a limitied period.
        /// </remarks>
        [DataMember(EmitDefaultValue = false, Name = "url")]
        public string Url { get; set; }

		/// <summary>
		/// An array of block hashes for the blob. Block hashes are calculated by a specialized hashing algorithm from the blob content. 
		/// All the blocks except the last one must be exactly VsoHash.BlockSize; the last one must be no larger than VsoHash.BlockSize. 
		/// </summary>
		[DataMember(EmitDefaultValue = false, Name = "blockHashes")]
        public List<String> BlockHashes { get; set; }

        public BlobIdentifier ToBlobIdentifier()
        {
            return BlobIdentifier.Deserialize(this.Id);
        }

        public BlobIdentifierWithBlocks ToBlobIdentifierWithBlocks()
        {
            if (!HasBlockHashes())
            {
                throw new ArgumentException("This blob has no block hashes.");
            }

            return new BlobIdentifierWithBlocks(BlobIdentifier.Deserialize(this.Id), this.BlockHashes.Select(bh => new BlobBlockHash(bh)));
        }

        public bool HasBlockHashes()
        {
            return BlockHashes != null && BlockHashes.Any();
        }
        public bool Equals(Blob other)
        {
            return ! object.ReferenceEquals(other, null)
                && Id == other.Id
                && HasBlockHashes() == other.HasBlockHashes()
                && (!HasBlockHashes() || BlockHashes == other.BlockHashes || BlockHashes.SequenceEqual(other.BlockHashes));
        }

        public override bool Equals(object other) => Equals(other as Blob);

        public static bool operator ==(Blob r1, Blob r2)
        {
            if (object.ReferenceEquals(r1, null))
            {
                return object.ReferenceEquals(r2, null);
            }

            return r1.Equals(r2);
        }

        public static bool operator !=(Blob r1, Blob r2)
        {
            return !(r1 == r2);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id?.GetHashCode() ?? 23;
                foreach (var blockHash in BlockHashes ?? Enumerable.Empty<string>())
                {
                    hashCode = (hashCode * 397) ^ blockHash.GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    /// <summary>
    /// An array of blob items. Each blob item contains a blob ID.
    /// </summary>
    [DataContract]
    public class BlobBatch
    {
        public BlobBatch()
        {
        }

        public BlobBatch(IEnumerable<BlobIdentifier> blobs)
        {
            this.Blobs = blobs.Select(b => new Blob(b)).ToList();
        }

        public BlobBatch(IEnumerable<BlobIdentifierWithBlocks> blobs)
        {
            this.Blobs = blobs.Select(b => new Blob(b)).ToList();
        }

        [DataMember(EmitDefaultValue = false, Name = "blobs")]
        public List<Blob> Blobs { get; set; }
    }

    [DataContract]
    [CLSCompliant(false)]
    public class BlobMappings
    {
        public BlobMappings()
        {
        }

        public BlobMappings(IDictionary<ulong, PreauthenticatedUri> mappings)
        {
            Mappings = mappings.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.NotNullUri.AbsoluteUri);
        }

        [DataMember(EmitDefaultValue = false, Name = "mappings")]
        public Dictionary<ulong, string> Mappings { get; set; }

        public IDictionary<ulong, Uri> Deserialize()
        {
            return Mappings.ToDictionary(
                kvp => kvp.Key,
                kvp => new Uri(kvp.Value));
        }
    }
}
