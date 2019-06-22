using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.BlobStore.WebApi.Contracts
{
    /// <summary>
    /// The status of a reference as the result of reference adding call.
    /// </summary>
    public enum ReferenceStatus
    {
        /// <summary>
        /// The reference was added.
        /// </summary>
        Added,

        /// <summary>
        /// The reference couldn't be added because the blob is missing.
        /// </summary>
        Missing
    }

    public enum ReferenceKind
    {
        IdReference,
        KeepUntilReference
    }


    /// <summary>
    /// Base Class for API Version 2 Blob References
    /// </summary>
    [JsonObject(MemberSerialization.OptIn, ItemConverterType = typeof(JsonReferenceConverter))]
    public abstract class Reference : IEquatable<Reference>
    {
        public Reference()
        {
        }

        /// <summary>
        /// Creates a new Reference.
        /// </summary>
        /// <param name="blobId">the identifier of the referenced blob</param>
        /// <param name="isMissing">If true, that status is set to Missing. Default is false.</param>
        public Reference(BlobIdentifier blobId, bool? isMissing = null)
            : this(new Blob(blobId), isMissing)
        {
        }

        public Reference(BlobIdentifierWithBlocks blobId, bool? isMissing = null)
                : this(new Blob(blobId), isMissing)
        {
        }

        /// <summary>
        /// Creates a new Reference with the given status.
        /// </summary>
        /// <param name="blob">the referenced blob</param>
        /// <param name="isMissing">If true, the status is set to Missing.</param>
        public Reference(Blob blob, bool? isMissing = null)
        {
            if (isMissing != null)
            {
                this.Status = isMissing.Value ? ReferenceStatus.Missing.ToString().ToLower() : ReferenceStatus.Added.ToString().ToLower();
            }
            else
            {
                this.Status = null;
            }
            this.Blob = blob;
        }

        public abstract ReferenceKind Kind { get; }

        /// <summary>
        /// A blob item that contains the blob ID.
        /// </summary>
        [JsonProperty(PropertyName = "blob", Required = Required.Always)]
        public Blob Blob { get; set; }

        /// <summary>
        /// The status of this reference as a result of adding call.
        /// </summary>
        [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
        public String Status { get; set; }

        public abstract BlobReference BlobReference { get; }

        // On the type level this compares up to the arguments both being
        // References.
        // Only override this if you have a good reason to do so.
        public override bool Equals(object other) => this.Equals(other as Reference);

        // When overriding this method ReferenceEquals should be used.
        public abstract bool Equals(Reference other);

        public bool ReferenceEquals(Reference other)
        {
            return ! object.ReferenceEquals(other, null)
                && String.Equals(this.Status, other.Status)
                && this.Blob == other.Blob;
        }

        public override abstract int GetHashCode();

        public int ReferenceGetHashCode()
        {
            return EqualityHelper.GetCombinedHashCode(this.Status, this.Blob);
        }

        public static bool operator ==(Reference r1, Reference r2)
        {
            if (object.ReferenceEquals(r1, null))
            {
                return object.ReferenceEquals(r2, null);
            }

            return r1.Equals(r2);
        }

        public static bool operator !=(Reference r1, Reference r2)
        {
            return !(r1 == r2);
        }
    }

    public class JsonReferenceConverter : CustomCreationConverter<Reference>
    {
        public override Reference Create(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            if (jObject.Property("id") != null)
            {
                return serializer.Deserialize<IdReference>(jObject.CreateReader());
            }
            else if (jObject.Property("keepUntil") != null)
            {
                return serializer.Deserialize<KeepUntilReference>(jObject.CreateReader());
            }
            else
            {
                throw new ArgumentException("Malformed blob reference JSON object");
            }
        }
    }
   
    /// <summary>
    /// This replaces the default Newtonsoft DateTime JSON converter. We use this
    /// to strictly enforce the date format defined in KeepUntilBlobReference.
    /// </summary>
    internal class KeepUntilDateTimeConverter : VssSecureDateTimeConverterBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            return KeepUntilBlobReference.ParseDate((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
            writer.WriteValue(((DateTime)value).ToString(KeepUntilBlobReference.KeepUntilFormat, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// A time based reference to a blob
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class KeepUntilReference : Reference, IEquatable<KeepUntilReference>
    {
        [JsonConstructor]
        public KeepUntilReference()
        {
        } 
        
        public KeepUntilReference(DateTime date, BlobIdentifier b) 
            : this(new KeepUntilBlobReference(date), b)
        {
        }

        public KeepUntilReference(KeepUntilBlobReference reference, BlobIdentifier blobId, bool? isMissing = null) 
            : this(reference, new Blob(blobId), isMissing)
        {
        }

        public KeepUntilReference(KeepUntilBlobReference reference, BlobIdentifierWithBlocks blobId, bool? isMissing = null) 
            : this(reference, new Blob(blobId), isMissing)
        {
        }

        public KeepUntilReference(KeepUntilBlobReference reference, Blob blob, bool? isMissing = null) 
            : base(blob, isMissing)
        {
            this.KeepUntil = reference.KeepUntil;
        }

        public override ReferenceKind Kind { get { return ReferenceKind.KeepUntilReference; } }

        /// <summary>
        /// A time based reference
        /// </summary>
        [JsonProperty(PropertyName = "keepUntil", Required = Required.Always)]
        [JsonConverter(typeof(KeepUntilDateTimeConverter))]
        public DateTime KeepUntil { get; set; }

        public override BlobReference BlobReference => new BlobReference(KeepUntilBlobReference);

        public KeepUntilBlobReference KeepUntilBlobReference => new KeepUntilBlobReference(this.KeepUntil);

        public override bool Equals(Reference other) => Equals(other as KeepUntilReference);

        public bool Equals(KeepUntilReference other)
        {
            return !object.ReferenceEquals(other, null)
                && ReferenceEquals(other)
                && this.KeepUntil == other.KeepUntil;
        }


        public override int GetHashCode()
        {
            return EqualityHelper.GetCombinedHashCode(ReferenceGetHashCode(), this.KeepUntil);
        }
    }

    /// <summary>
    /// Reference counted identifiable references to blobs that can be removed
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class IdReference : Reference, IEquatable<IdReference>
    {
        public IdReference()
        {
        }

        public IdReference(IdBlobReference blobReference, BlobIdentifierWithBlocks blobId, bool? isMissing = null)
                : this(blobReference, new Blob(blobId), isMissing)
        {
        }

        public IdReference(IdBlobReference blobReference, BlobIdentifier blobId, bool? isMissing = null)
                : this(blobReference, new Blob(blobId), isMissing)
        {
        }

        public IdReference(IdBlobReference blobReference, Blob blob, bool? isMissing = null)
            : base(blob, isMissing)
        {
            this.Id = blobReference.Name;
            this.Scope = blobReference.Scope;
        }

        public override ReferenceKind Kind => ReferenceKind.IdReference;

        /// <summary>
        /// A reference ID is constructed by the calling service based on the model it exposes to
        /// its clients as customized views of blob storage. For example, a file service may use
        /// file's full name as the ID.
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>
        /// The scope of the reference, a namespace that isolates references from different experience
        /// services. Access to references within a namespace is subject to authorization.
        /// </summary>
        /// <remarks>
        /// The value is either null or an non-empty and non-whitespace string.
        /// </remarks>
        [JsonProperty(PropertyName = "scope", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Scope { get; set; }

        public override BlobReference BlobReference => new BlobReference(this.Id, this.Scope);

        public override bool Equals(Reference other) => Equals(other as IdReference);

        public bool Equals(IdReference other)
        {
            return !object.ReferenceEquals(other, null)
                && ReferenceEquals(other)
                && this.Id == other.Id
                && this.Scope == other.Scope;
        }


        public override int GetHashCode()
        {
            return EqualityHelper.GetCombinedHashCode(ReferenceGetHashCode(), this.Id, this.Scope);
        }
    }

    public class ReferenceFactory
    {
        public static Reference MakeReference(BlobReference blobReference, string blobIdStr, bool? isMissing = null)
        {
            return MakeReference(blobReference, BlobIdentifier.Deserialize(blobIdStr), isMissing);
        }

        public static Reference MakeReference(BlobReference blobReference, BlobIdentifier blobIdentifier, bool? isMissing = null)
        {
            return MakeReference(blobReference, new Blob(blobIdentifier), isMissing);
        }

        public static Reference MakeReference(BlobReference blobReference, BlobIdentifierWithBlocks blobIdentifier, bool? isMissing = null)
        {
            return MakeReference(blobReference, new Blob(blobIdentifier), isMissing);
        }

        public static Reference MakeReference(BlobReference blobReference, Blob blob, bool? isMissing = null)
        {
            return blobReference.Match<Reference>(
                idRef => 
                    new IdReference(idRef, blob, isMissing),
                keepUntilRef => 
                    new KeepUntilReference(keepUntilRef, blob, isMissing)
            );
        }
    }

    /// <summary>
    ///  An array of reference items. Each reference item contains a reference ID and a blob item.
    /// </summary>
    [JsonObject(Description = "list of References, each reference is either a KeepUntilReference or an IdReference", MemberSerialization = MemberSerialization.OptIn)]
    public class ReferenceBatch
    {
        public ReferenceBatch()
        {
        }

        public ReferenceBatch(IEnumerable<KeyValuePair<BlobReference, BlobIdentifier>> referenceToBlobMap)
        {
            this.References = referenceToBlobMap.Select(rb => ReferenceFactory.MakeReference(rb.Key, rb.Value)).ToList();
        }

        public ReferenceBatch(IEnumerable<KeyValuePair<BlobReference, BlobIdentifierWithBlocks>> referenceToBlobMap)
        {
            this.References = referenceToBlobMap.Select(rb => ReferenceFactory.MakeReference(rb.Key, rb.Value)).ToList();
        }

        [JsonProperty(ItemConverterType = typeof(JsonReferenceConverter), PropertyName = "references")]
        public List<Reference> References { get; set; }
    }
}
