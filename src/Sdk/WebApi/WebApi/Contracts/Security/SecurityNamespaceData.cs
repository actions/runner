using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.Security
{
    /// <summary>
    /// Encapsulates the result of a QuerySecurityData call to the backing store.
    /// </summary>
    [DataContract]
    public sealed class SecurityNamespaceData
    {
        public SecurityNamespaceData()
        {
        }

        /// <summary>
        /// Creates a SecurityNamespaceData based on the supplied information.
        /// </summary>
        /// <param name="aclStoreId">The ACL store ID</param>
        /// <param name="oldSequenceId">The old sequence ID</param>
        /// <param name="newSequenceId">The new sequence ID</param>
        /// <param name="identityDomain"></param>
        /// <param name="accessControlEntries">The access control entries for this snapshot or delta</param>
        /// <param name="noInheritTokens">The list of tokens which have inheritance disabled</param>
        public SecurityNamespaceData(
            Guid aclStoreId,
            long oldSequenceId,
            long[] newSequenceId,
            Guid identityDomain,
            IEnumerable<RemoteBackingStoreAccessControlEntry> accessControlEntries,
            IEnumerable<String> noInheritTokens)
        {
            AclStoreId = aclStoreId;
            OldSequenceId = oldSequenceId;
            NewSequenceId = newSequenceId;
            IdentityDomain = identityDomain;
            AccessControlEntries = accessControlEntries;
            NoInheritTokens = noInheritTokens;
        }

        /// <summary>
        /// True if this instance represents a delta; false if it is a full snapshot.
        /// </summary>
        public bool IsDelta
        {
            get
            {
                return OldSequenceId != -1;
            }
        }

        /// <summary>
        /// Indicates the ACL store whose data is persisted in this SecurityNamespaceData object.
        /// </summary>
        [DataMember]
        public Guid AclStoreId { get; set; }

        /// <summary>
        /// If this is a full snapshot of the security namespace data, this value is -1. Otherwise, this
        /// instance represents the delta from OldSequenceId to NewSequenceId.
        /// </summary>
        [DataMember]
        public long OldSequenceId { get; set; }

        /// <summary>
        /// The sequence ID for this snapshot of or incremental update to the security namespace data.
        /// </summary>
        [DataMember, JsonConverter(typeof(PluralSequenceIdJsonConverter))]
        public long[] NewSequenceId { get; set; }

        /// <summary>
        /// The identity domain for the service host on which this security namespace resides.
        /// </summary>
        [DataMember]
        public Guid IdentityDomain { get; set; }

        /// <summary>
        /// The access control entries in this snapshot of the security namespace data.
        /// </summary>
        [DataMember]
        public IEnumerable<RemoteBackingStoreAccessControlEntry> AccessControlEntries { get; set; }

        /// <summary>
        /// The list of tokens in the security namespace which have inheritance disabled.
        /// </summary>
        [DataMember]
        public IEnumerable<String> NoInheritTokens { get; set; }

        /// <summary>
        /// The JSON converter for plural sequence IDs that allows for backwards compatibility
        /// with other services that encode a scalar value.
        /// </summary>
        private class PluralSequenceIdJsonConverter : VssSecureJsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(long[]);
            }

            public override Object ReadJson(
                JsonReader reader,
                Type objectType,
                Object existingValue,
                JsonSerializer serializer)
            {
                if (JsonToken.Null == reader.TokenType)
                {
                    return null;
                }

                List<long> toReturn = new List<long>();

                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();

                    while (reader.TokenType == JsonToken.Integer)
                    {
                        toReturn.Add((long)reader.Value);
                        reader.Read();
                    }

                    if (reader.TokenType != JsonToken.EndArray)
                    {
                        throw new JsonSerializationException();
                    }
                }
                else if (reader.TokenType == JsonToken.Integer)
                {
                    toReturn.Add((long)reader.Value);
                }

                return toReturn.ToArray();
            }

            public override void WriteJson(
                JsonWriter writer,
                Object value,
                JsonSerializer serializer)
            {
                base.WriteJson(writer, value, serializer);
                long[] array = (long[])value;

                switch (array.Length)
                {
                    case 0:
                        throw new InvalidOperationException();

                    case 1:
                        writer.WriteValue(array[0]);
                        break;

                    default:
                        writer.WriteStartArray();

                        for (int i = 0; i < array.Length; i++)
                        {
                            writer.WriteValue(array[i]);
                        }

                        writer.WriteEndArray();
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Represents a set of SecurityNamespaceData objects.
    /// </summary>
    [CollectionDataContract(Name = "AclStores", ItemName = "AclStore")]
    public class SecurityNamespaceDataCollection : List<SecurityNamespaceData>
    {
        public SecurityNamespaceDataCollection()
        {
        }

        public SecurityNamespaceDataCollection(IList<SecurityNamespaceData> source)
            : base(source)
        {
        }
    }
}
