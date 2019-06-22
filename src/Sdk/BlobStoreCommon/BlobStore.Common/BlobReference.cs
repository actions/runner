using System;
using System.Globalization;
using System.Runtime.Serialization;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.Common
{
    public class BlobReference : EquatableTaggedUnion<IdBlobReference, KeepUntilBlobReference>, IEquatable<BlobReference>
    {
        public BlobReference(IdBlobReference reference) : base(reference) { }
        public BlobReference(KeepUntilBlobReference reference) : base(reference) { }

        public BlobReference(DateTime date) : base(new KeepUntilBlobReference(date)) { }
        public BlobReference(string id, string scope) : base(new IdBlobReference(id, scope)) { }

        public bool Equals(BlobReference other) => base.Equals(other);

        public override bool Equals(object obj) => Equals(obj as BlobReference);

        public static bool operator ==(BlobReference r1, BlobReference r2)
            => object.ReferenceEquals(r1, null) ? object.ReferenceEquals(r2, null) : r1.Equals(r2);

        public static bool operator !=(BlobReference r1, BlobReference r2) => !(r1 == r2);

        public override int GetHashCode()
            => EqualityHelper.GetCombinedHashCode(typeof(BlobReference), base.GetHashCode());

        public bool IsInThePast(IClock clock) => this.Match(idRef => false, keepUntilRef => keepUntilRef.KeepUntil < clock.Now.UtcDateTime);

        public bool IsKeepUntil => this.Match(idRef => false, keepUntilRef => true);
    }

    [DataContract]
    [Serializable]
    public struct KeepUntilBlobReference : IEquatable<KeepUntilBlobReference>, IComparable<KeepUntilBlobReference>
    {
        public KeepUntilBlobReference(DateTimeOffset date) : this(date.UtcDateTime) { }

        public KeepUntilBlobReference(DateTime date)
        {
            // round to the nearest second as we cut off the milliseconds on serialization
            this.KeepUntil = date;
            long ticksRemainder = this.KeepUntil.Ticks % TimeSpan.TicksPerSecond;
            if (ticksRemainder > 0)
            {
                this.KeepUntil = this.KeepUntil.AddTicks(TimeSpan.TicksPerSecond - ticksRemainder);
            }
            Validate();
        }

        public KeepUntilBlobReference(string dateString)
        {
            this.KeepUntil = ParseDate(dateString);
        }

        public static KeepUntilBlobReference Parse(string dateString)
        {
            return new KeepUntilBlobReference(ParseDate(dateString));
        }

        public static DateTime ParseDate(string dateString)
        {
            try
            {
                return DateTime.ParseExact(
                    dateString,
                    KeepUntilFormat,
                    CultureInfo.InvariantCulture,

                    // Guaratees that the resulting date is UTC; if timezone isn't
                    // explicitly specified assumes that it is already UTC.
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                );
            }
            catch
            {
                throw new ArgumentException($"Failed to parse date string {dateString}; keepUntil dates must be formated as {KeepUntilFormat}");
            }
        }

        [DataMember]
        public DateTime KeepUntil { get; private set; }

        public bool Equals(KeepUntilBlobReference other) => DateTime.Equals(KeepUntil, other.KeepUntil);

        public override bool Equals(object obj) => (obj is KeepUntilBlobReference) && Equals((KeepUntilBlobReference)obj);

        public static bool operator ==(KeepUntilBlobReference r1, KeepUntilBlobReference r2)
            => r1.Equals(r2);

        public static bool operator !=(KeepUntilBlobReference r1, KeepUntilBlobReference r2)
            => !(r1 == r2);

        public override int GetHashCode() => KeepUntil.GetHashCode();

        public static string KeepUntilFormat = "yyyy-MM-ddTHH:mm:ss'Z'";

        public string KeepUntilString
            => KeepUntil.ToString(KeepUntilFormat, CultureInfo.InvariantCulture); // NOTE: we're cutting off the milliseconds here

        public void Validate()
        {
            if (this.KeepUntil == null || this.KeepUntil.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentNullException("KeepUntil");
            }
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context) => Validate();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context) => Validate();

        public override string ToString() => $"KeepUntil:{KeepUntilString}";

        public int CompareTo(KeepUntilBlobReference other) => KeepUntil.CompareTo(other.KeepUntil);
    }

    /// <summary>
    /// Identifies a reference to a blob.
    /// </summary>
    /// <remarks> 
    /// A Identified Blob Reference has to parts:
    /// 
    /// 1. A reference scope that defines the namespace of the
    ///    reference. It can be null but must not be the empty string.
    ///    Access to references within a scope other than "null" is
    ///    is subject to authorization.
    ///
    /// 2. An opaque reference name that uniquely identifies the
    ///    reference within its scope. It must neither be null nor
    ///    the empty string.
    /// 
    /// Values of type BlobReference are immutable and feature
    /// extensional equality.
    /// </remarks>
    [DataContract]
    [Serializable]
    public struct IdBlobReference : IEquatable<IdBlobReference>
    {
        /// <summary>
        /// Constructs a new (immutable) BlobReference.
        /// </summary>
        /// <param name="name">the name of the reference; this must not be null or the empty string</param>
        /// <param name="scope">the scope of the reference; this must neither be null nor the empty string</param>
        /// <remarks>
        /// A scope is a namespace for reference identifiers. Adding a reference with a scope that is not
        /// null to a Blob is subject to authorization for the respective scope.
        /// 
        /// References with a null scope are rejected by the BlobStore API.
        /// </remarks>
        public IdBlobReference(string name, string scope)
        {
            this.Name = name;
            this.Scope = scope;
            Validate();
        }

        [DataMember]
        public string Name { get; }

        /// <summary>
        /// The scope of the reference. A client must be authorized to create
        /// references within a given scope. The null scope if reserved
        /// for internal usage.
        /// </summary>
        [DataMember]
        public string Scope { get; }

        public override string ToString() => string.Format("{0}/{1}", this.Scope ?? "null", this.Name);

        public bool Equals(IdBlobReference other)
        {
            return !object.ReferenceEquals(other, null)
                && String.Equals(this.Scope, other.Scope, StringComparison.Ordinal)
                && String.Equals(this.Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => (obj is IdBlobReference) && Equals((IdBlobReference)obj);

        /// <remarks>
        /// BlobReference are immutable, thus, it makes sense to define
        /// equality extensionally.
        ///
        /// Also IdBlobReferences used to be just strings which have extensional
        /// equality, by keeping this we avoid bugs due to usages of operator == that
        /// are lingering in the code.
        /// </remarks>
        public static bool operator ==(IdBlobReference r1, IdBlobReference r2) => r1.Equals(r2);

        public static bool operator !=(IdBlobReference r1, IdBlobReference r2) => !(r1 == r2);

        public override int GetHashCode() => EqualityHelper.GetCombinedHashCode(this.Scope, this.Name);

        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(this.Name))
            {
                throw new ArgumentOutOfRangeException("Name", "must not be null, empty, or whitespace");
            }
            if (this.Scope != null && String.IsNullOrWhiteSpace(this.Scope))
            {
                throw new ArgumentOutOfRangeException("Scope", "must not be empty or whitespace");
            }
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context) => Validate();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context) => Validate();
    }

    public static class IdBlobReferenceExtentions
    {
        public static bool IdReferenceScopedToFileList(this IdBlobReference reference)
        {
            return reference.Scope == "drop" &&
                (reference.Name.EndsWith("/filelist", StringComparison.OrdinalIgnoreCase)
                || reference.Name.EndsWith("/filelist2", StringComparison.OrdinalIgnoreCase));
        }
    }
}
