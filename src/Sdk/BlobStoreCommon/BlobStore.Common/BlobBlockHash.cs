using System;
using System.Linq;
using GitHub.Services.Content.Common;
using Newtonsoft.Json;

namespace GitHub.Services.BlobStore.Common
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BlobBlockHash : IEquatable<BlobBlockHash>
    {
        [JsonProperty(PropertyName = "HashBytes")]
        [JsonConverter(typeof(ByteArrayAsNumberArrayJsonConvertor))]
        public readonly byte[] HashBytes;

        public BlobBlockHash(byte[] hashValue)
        {
            HashBytes = hashValue;
        }

        public BlobBlockHash(string valueString)
        {
            HashBytes = HexUtilities.ToByteArray(valueString);
        }

        // Deserialization constructor
        private BlobBlockHash()
        {
        }

        public string HashString
        {
            get { return this.HashBytes.ToHexString(); }
        }

        public static bool operator ==(BlobBlockHash x, BlobBlockHash y)
        {
            if (ReferenceEquals(x, null))
            {
                return ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        public static bool operator !=(BlobBlockHash x, BlobBlockHash y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Returns true/false whether the object is equal to the current <see cref="BlobBlockHash"/>
        /// </summary>
        /// <param name="obj">The object to compare against the current instance</param>
        /// <returns>
        /// <c>true</c> if the objects are equal, otherwise <c>false</c>.
        /// </returns>
        public override bool Equals(Object obj) => Equals(obj as BlobBlockHash);

        /// <summary>
        /// Returns true/false whether the <see cref="BlobBlockHash"/> is equal to the current <see cref="BlobBlockHash"/>
        /// </summary>
        /// <param name="other">The <see cref="BlobBlockHash"/> to compare against the current instance</param>
        /// <returns>
        /// <c>true</c> if the objects are equal, otherwise <c>false</c>.
        /// </returns>
        public bool Equals(BlobBlockHash other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return !object.ReferenceEquals(other, null) && HashBytes.SequenceEqual(other.HashBytes);
        }

        /// <summary>
        /// Gets the unique hash for this unique identifier for binary content.
        /// </summary>
        /// <returns>
        /// A hash value for the content identifier
        /// </returns>
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(HashBytes, 0);
        }

        /// <summary>
        /// Returns a user-friendly, non-canonical string representation of the unique identifier for binary content
        /// </summary>
        /// <returns>
        /// A user-friendly, non-canonical string representation of the content identifier
        /// </returns>
        public override string ToString()
        {
            return HashString;
        }
    }
}
