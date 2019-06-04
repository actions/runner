using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.NameResolution
{
    [GenerateAllConstants]
    public static class NameResolutionResourceIds
    {
        public const String AreaId = "{81AEC033-EAE2-42B8-82F6-90B93A662EF5}";
        public const String AreaName = "NameResolution";

        public static readonly Guid EntriesLocationId = Guid.Parse("{CAE3D437-CD60-485A-B8B0-CE6ACF234E44}");
        public const String EntriesResource = "Entries";
    }

    [DataContract]
    public class NameResolutionEntry
    {
        public NameResolutionEntry()
        {
        }

        public NameResolutionEntry(String @namespace, String name)
        {
            Namespace = @namespace;
            Name = name;
        }

        [DataMember]
        public String Namespace { get; set; }

        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public Guid Value { get; set; }

        [DataMember]
        public Boolean IsPrimary { get; set; }

        [DataMember]
        public Boolean IsEnabled { get; set; }

        [DataMember]
        public Int32? TTL
        {
            get
            {
                if (ExpiresOn == null)
                {
                    return null;
                }
                else
                {
                    // Round up the time delta to the nearest second
                    // Expired entries (negative time delta) will always show TTL=0
                    return Math.Max((Int32)((ExpiresOn.Value - DateTime.UtcNow).TotalSeconds + 0.5), 0);
                }
            }
            set
            {
                if (value == null)
                {
                    ExpiresOn = null;
                }
                else
                {
                    ExpiresOn = DateTime.UtcNow.AddSeconds(value.Value);
                }
            }
        }

        [DataMember]
        public Int32 Revision { get; set; }

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DateTime? ExpiresOn { get; set; }

        [IgnoreDataMember]
        public Boolean HasExpiration
        {
            get { return ExpiresOn != null; }
        }

        [IgnoreDataMember]
        public Boolean IsExpired
        {
            get { return HasExpiration && ExpiresOn.Value <= DateTime.UtcNow; }
        }

        /// <summary>
        /// When set to true, we will insert the name resolution record
        /// even if a conflicting record exists in another namespace.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [IgnoreDataMember]
        public Boolean Force { get; set; }

        public NameResolutionEntry Clone()
        {
            return new NameResolutionEntry()
            {
                Namespace = Namespace,
                Name = Name,
                Value = Value,
                IsPrimary = IsPrimary,
                IsEnabled = IsEnabled,
                ExpiresOn = ExpiresOn,
                Revision = Revision
            };
        }

        public override bool Equals(object obj)
        {
            NameResolutionEntry other = obj as NameResolutionEntry;

            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(other, this))
            {
                return true;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(this.Namespace, other.Namespace) &&
                   StringComparer.OrdinalIgnoreCase.Equals(this.Name, other.Name);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(this.Namespace) ^
                   StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name);

        }

        public override string ToString()
        {
            return $"=> Namespace: {Namespace} Name: {Name} Value: {Value} IsPrimary: {IsPrimary} IsEnabled: {IsEnabled} ExpiresOn: {ExpiresOn} Revision: {Revision}";
        }
    }

    [DataContract]
    public class NameResolutionQuery
    {
        public NameResolutionQuery()
        {
        }

        public NameResolutionQuery(String @namespace, String name)
        {
            Namespace = @namespace;
            Name = name;
        }

        [DataMember]
        public String Namespace { get; set; }

        [DataMember]
        public String Name { get; set; }

        public override Boolean Equals(Object obj)
        {
            NameResolutionQuery other = obj as NameResolutionQuery;

            if (other == null)
            {
                return false;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(this.Namespace, other.Namespace) &&
                   StringComparer.OrdinalIgnoreCase.Equals(this.Name, other.Name);
        }

        public override Int32 GetHashCode()
        {
            return this.Namespace.GetHashCode() ^ this.Name.GetHashCode();
        }
    }
}
