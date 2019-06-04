// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Identity
{
    /// <summary>
    /// An Identity descriptor is a wrapper for the identity type (Windows SID, Passport)
    /// along with a unique identifier such as the SID or PUID.
    /// </summary>
    /// <remarks>
    /// This is the only legacy type moved into VSS (by necessity, it is used everywhere)
    /// so it must support both Xml and DataContract serialization
    /// </remarks>
    [XmlInclude(typeof(ReadOnlyIdentityDescriptor))]
    [KnownType(typeof(ReadOnlyIdentityDescriptor))]
    [TypeConverter(typeof(IdentityDescriptorConverter))]
    [DataContract]
    public class IdentityDescriptor : IEquatable<IdentityDescriptor>, IComparable<IdentityDescriptor>
    {
        /// <summary>
        /// Default constructor, for Xml serializer only.
        /// </summary>
        public IdentityDescriptor() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public IdentityDescriptor(string identityType, string identifier, object data)
            : this(identityType, identifier)
        {
            this.Data = data;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public IdentityDescriptor(string identityType, string identifier)
        {
            //Validation in Setters...
            IdentityType = identityType;
            Identifier = identifier;
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        public IdentityDescriptor(IdentityDescriptor clone)
        {
            IdentityType = clone.IdentityType;
            Identifier = clone.Identifier;
        }

        /// <summary>
        /// Type of descriptor (for example, Windows, Passport, etc.).
        /// </summary>
        [XmlAttribute("identityType")]
        [DataMember]
        public virtual string IdentityType
        {
            get
            {
                return m_identityType ?? IdentityConstants.UnknownIdentityType;
            }
            set
            {
                ValidateIdentityType(value);
                m_identityType = NormalizeIdentityType(value);

                // Drop any existing data
                Data = null;
            }
        }

        /// <summary>
        /// The unique identifier for this identity, not exceeding 256 chars,
        /// which will be persisted.
        /// </summary>
        [XmlAttribute("identifier")]
        [DataMember]
        public virtual string Identifier
        {
            get
            {
                return m_identifier;
            }
            set
            {
                ValidateIdentifier(value);
                m_identifier = value;

                // Drop any existing data
                Data = null;
            }
        }

        /// <summary>
        /// Any additional data specific to identity type.
        /// </summary>
        /// <remarks>
        /// Not serialized under either method.
        /// </remarks>
        [XmlIgnore]
        public virtual object Data { get; set; }

        public override string ToString()
        {
            return string.Concat(m_identityType, IdentityConstants.IdentityDescriptorPartsSeparator, m_identifier);
        }

        public static IdentityDescriptor FromString(string identityDescriptorString)
        {
            if (string.IsNullOrEmpty(identityDescriptorString))
            {
                return null;
            }

            string[] tokens;
            try
            {
                tokens = identityDescriptorString.Split(new[] { IdentityConstants.IdentityDescriptorPartsSeparator }, 2, StringSplitOptions.RemoveEmptyEntries);
            }
            catch
            {
                return new IdentityDescriptor(IdentityConstants.UnknownIdentityType, identityDescriptorString);
            }

            if (tokens.Length == 2)
            {
                return new IdentityDescriptor(tokens[0], tokens[1]);
            }

            return new IdentityDescriptor(IdentityConstants.UnknownIdentityType, identityDescriptorString);
        }

        //Copied from TFCommonUtil.cs
        private static void ValidateIdentityType(string identityType)
        {
            if (string.IsNullOrEmpty(identityType))
            {
                throw new ArgumentNullException(nameof(identityType));
            }

            if (identityType.Length > MaxTypeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(identityType));
            }
        }

        private static String NormalizeIdentityType(String identityType)
        {
            String normalizedIdentityType;

            // Look up the string in the static dictionary. If we get a hit, then
            // we'll use that string for the identity type instead. This saves memory
            // as well as improves compare/equals performance when comparing descriptors,
            // since Object.ReferenceEquals will return true a lot more often
            if (!IdentityConstants.IdentityTypeMap.TryGetValue(identityType, out normalizedIdentityType))
            {
                normalizedIdentityType = identityType;
            }

            return normalizedIdentityType;
        }

        private static void ValidateIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (identifier.Length > MaxIdLength)
            {
                throw new ArgumentOutOfRangeException(nameof(identifier));
            }
        }

        internal static IdentityDescriptor FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            string identifier = string.Empty;
            string identityType = string.Empty;

            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            bool empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "identifier":
                            identifier = reader.Value;
                            break;
                        case "identityType":
                            identityType = reader.Value;
                            break;
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }
                }
            }

            IdentityDescriptor obj = new IdentityDescriptor(identityType, identifier);

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }

            return obj;
        }

        protected string m_identityType;
        private string m_identifier;

        private const int MaxIdLength = 256;
        private const int MaxTypeLength = 128;

        #region Equality and Compare

        // IEquatable
        public bool Equals(IdentityDescriptor other) => IdentityDescriptorComparer.Instance.Equals(this, other);

        // IComparable
        public int CompareTo(IdentityDescriptor other) => IdentityDescriptorComparer.Instance.Compare(this, other);

        public override bool Equals(object obj) => this.Equals(obj as IdentityDescriptor);

        public override int GetHashCode() => IdentityDescriptorComparer.Instance.GetHashCode(this);

        public static bool operator ==(IdentityDescriptor x, IdentityDescriptor y)
        {
            return IdentityDescriptorComparer.Instance.Equals(x, y);
        }

        public static bool operator !=(IdentityDescriptor x, IdentityDescriptor y)
        {
            return !IdentityDescriptorComparer.Instance.Equals(x, y);
        }

        #endregion // Equality and Compare
    }

    /// <summary>
    /// Class used for comparing IdentityDescriptors
    /// </summary>
    public class IdentityDescriptorComparer : IComparer<IdentityDescriptor>, IEqualityComparer<IdentityDescriptor>
    {
        private IdentityDescriptorComparer()
        {
        }

        /// <summary>
        /// Compares two instances of IdentityDescriptor.
        /// </summary>
        /// <param name="x">The first IdentityDescriptor to compare. </param>
        /// <param name="y">The second IdentityDescriptor to compare. </param>
        /// <returns>Compares two specified IdentityDescriptor objects and returns an integer that indicates their relative position in the sort order.</returns>
        public int Compare(IdentityDescriptor x, IdentityDescriptor y)
        {
            if (Object.ReferenceEquals(x, y))
            {
                return 0;
            }

            if (Object.ReferenceEquals(x, null) && !Object.ReferenceEquals(y, null))
            {
                return -1;
            }

            if (!Object.ReferenceEquals(x, null) && Object.ReferenceEquals(y, null))
            {
                return 1;
            }

            int retValue = StringComparer.OrdinalIgnoreCase.Compare(x.IdentityType, y.IdentityType);

            //have to maintain equivalence for service principals while we are migrating them
            if (0 != retValue &&
               ((x.IsSystemServicePrincipalType() && y.IsClaimsIdentityType()) ||
                (y.IsSystemServicePrincipalType() && x.IsClaimsIdentityType())))
            {
                retValue = 0;
            }

            if (0 == retValue)
            {
                retValue = StringComparer.OrdinalIgnoreCase.Compare(x.Identifier, y.Identifier);
            }

            return retValue;
        }

        public bool Equals(IdentityDescriptor x, IdentityDescriptor y)
        {
            if (Object.ReferenceEquals(x, y))
            {
                return true;
            }

            return 0 == Compare(x, y);
        }

        public int GetHashCode(IdentityDescriptor obj)
        {
            int hashCode = 7443;
            string identityType = obj.IdentityType;

            //until all service principals are in the system store, we treat them as Claims identities for hash code
            if(obj.IsSystemServicePrincipalType())
            {
                identityType = IdentityConstants.ClaimsType;
            }

            hashCode = 524287 * hashCode + StringComparer.OrdinalIgnoreCase.GetHashCode(identityType);
            hashCode = 524287 * hashCode + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Identifier ?? string.Empty);

            return hashCode;
        }

        public static IdentityDescriptorComparer Instance { get; } = new IdentityDescriptorComparer();
    }

    // Keep this in sync with the SubjectDescriptorExtensions to avoid extra casting/conversions
    public static class IdentityDescriptorExtensions
    {
        public static bool IsTeamFoundationType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.TeamFoundationType);
        }

        public static bool IsWindowsType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.WindowsType);
        }

        public static bool IsUnknownIdentityType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.UnknownIdentityType);
        }

        public static bool IsSystemServicePrincipalType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.System_ServicePrincipal);
        }

        public static bool IsClaimsIdentityType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.ClaimsType);
        }

        public static bool IsImportedIdentityType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.ImportedIdentityType);
        }

        public static bool IsServiceIdentityType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.ServiceIdentityType);
        }

        public static bool IsBindPendingType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.BindPendingIdentityType);
        }

        public static bool IsAggregateIdentityType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.AggregateIdentityType);
        }

        public static bool IsUnauthenticatedIdentity(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.UnauthenticatedIdentityType);
        }

        public static bool IsSubjectStoreType(this IdentityDescriptor identityDescriptor)
        {
            return ReferenceEquals(identityDescriptor.IdentityType, IdentityConstants.System_License)
                || ReferenceEquals(identityDescriptor.IdentityType, IdentityConstants.System_Scope)
                || ReferenceEquals(identityDescriptor.IdentityType, IdentityConstants.System_ServicePrincipal)
                || ReferenceEquals(identityDescriptor.IdentityType, IdentityConstants.System_WellKnownGroup)
                || ReferenceEquals(identityDescriptor.IdentityType, IdentityConstants.System_CspPartner);
        }

        /// <summary>
        /// true if the descriptor matches any of the passed types
        /// </summary>
        /// <param name="identityDescriptor"></param>
        /// <param name="identityTypes"></param>
        /// <returns></returns>
        public static bool IsIdentityType(this IdentityDescriptor identityDescriptor, IEnumerable<string> identityTypes)
        {
            return identityTypes.Any(id => StringComparer.OrdinalIgnoreCase.Equals(identityDescriptor.IdentityType, id));
        }

        public static bool IsIdentityType(this IdentityDescriptor identityDescriptor, string identityType)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(identityDescriptor.IdentityType, identityType);
        }

        public static bool IsCspPartnerIdentityType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.CspPartnerIdentityType);
        }

        public static bool IsGroupScopeType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.GroupScopeType);
        }

        public static bool IsSystemLicenseType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.System_License);
        }

        public static bool IsSystemScopeType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.System_Scope);
        }

        public static bool IsSystemPublicAccessType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.System_PublicAccess);
        }

        public static bool IsSystemAccessControlType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.System_AccessControl);
        }

        public static bool IsServerTestIdentityType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.ServerTestIdentity);
        }

        public static bool IsSystemCspPartnerType(this IdentityDescriptor identityDescriptor)
        {
            return identityDescriptor.IsIdentityType(IdentityConstants.System_CspPartner);
        }
    }

    [DataContract]
    public sealed class ReadOnlyIdentityDescriptor : IdentityDescriptor
    {
        /// <summary>
        /// Default constructor, for Xml serializer only.
        /// </summary>
        public ReadOnlyIdentityDescriptor() { }

        public ReadOnlyIdentityDescriptor(string identityType, string identifier, object data)
            : base(identityType, identifier, data)
        {
        }

        [XmlAttribute("identityType")]
        [DataMember]
        public override string IdentityType
        {
            get
            {
                return base.IdentityType;
            }
            set
            {
                if (m_identityType != null)
                {
                    throw new InvalidOperationException(IdentityResources.FieldReadOnly(nameof(IdentityType)));
                }
                base.IdentityType = value;
            }
        }

        [XmlAttribute("identifier")]
        [DataMember]
        public override string Identifier
        {
            get
            {
                return base.Identifier;
            }
            set
            {
                if (!string.IsNullOrEmpty(base.Identifier))
                {
                    throw new InvalidOperationException(IdentityResources.FieldReadOnly(nameof(Identifier)));
                }
                base.Identifier = value;
            }
        }

        [XmlIgnore]
        public override object Data
        {
            get
            {
                return base.Data;
            }
            set
            {
                if (base.Data != null)
                {
                    throw new InvalidOperationException(IdentityResources.FieldReadOnly(nameof(Data)));
                }
                base.Data = value;
            }
        }
    }

    /// <summary>
    /// Converter to support data contract serialization.
    /// </summary>
    public class IdentityDescriptorConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType.Equals(typeof(string)) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType.Equals(typeof(string)) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string descriptor = value as string;
                string[] tokens = descriptor.Split(new[] { IdentityConstants.IdentityDescriptorPartsSeparator }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length == 2)
                {
                    return new IdentityDescriptor(tokens[0], tokens[1]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType.Equals(typeof(string)))
            {
                IdentityDescriptor descriptor = value as IdentityDescriptor;

                return descriptor?.ToString() ?? string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
