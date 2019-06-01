using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Graph;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using Microsoft.VisualStudio.Services.WebApi;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Common
{
    [TypeConverter(typeof(SubjectDescriptorConverter))]
    public struct SubjectDescriptor : IEquatable<SubjectDescriptor>, IXmlSerializable
    {
        public SubjectDescriptor(string subjectType, string identifier)
        {
            ValidateSubjectType(subjectType);
            ValidateIdentifier(identifier);

            SubjectType = NormalizeSubjectType(subjectType);
            Identifier = identifier;
        }

        [DataMember]
        public string SubjectType { get; private set; }

        [DataMember]
        public string Identifier { get; private set; }

        public override string ToString()
        {
            if (this == default(SubjectDescriptor))
            {
                return null;
            }

            return string.Concat(
                SubjectType,
                Constants.SubjectDescriptorPartsSeparator,
                PrimitiveExtensions.ToBase64StringNoPaddingFromString(Identifier));
        }

        public static SubjectDescriptor FromString(string subjectDescriptorString)
        {
            if (string.IsNullOrEmpty(subjectDescriptorString))
            {
                return default(SubjectDescriptor);
            }

            if (subjectDescriptorString.Length < Constants.SubjectDescriptorPolicies.MinSubjectDescriptorStringLength)
            {
                return new SubjectDescriptor(Constants.SubjectType.Unknown, subjectDescriptorString);
            }

            int splitIndex = subjectDescriptorString.IndexOf(Constants.SubjectDescriptorPartsSeparator, Constants.SubjectDescriptorPolicies.MinSubjectTypeLength, 3);

            // Either the separator is not there, or it's before the MinSubjectTypeLength or it's at the end the string; either way it's wrong. 
            if (splitIndex < 3 || splitIndex == subjectDescriptorString.Length - 1)
            {
                return new SubjectDescriptor(Constants.SubjectType.Unknown, subjectDescriptorString);
            }

            string moniker = subjectDescriptorString.Substring(0, splitIndex);
            string identifier = subjectDescriptorString.Substring(splitIndex + 1);

            try
            {
                return new SubjectDescriptor(moniker, PrimitiveExtensions.FromBase64StringNoPaddingToString(identifier));
            }
            catch { }

            return new SubjectDescriptor(Constants.SubjectType.Unknown, subjectDescriptorString);
        }

        /// <summary>
        /// Parses a string of comma separated subject descriptors into a enumerable list of <see cref="SubjectDescriptor"/> objects.
        /// </summary>
        /// <returns>empty enumerable if parameter 'descriptors' is null or empty</returns>
        public static IEnumerable<SubjectDescriptor> FromCommaSeperatedStrings(string descriptors)
        {
            if (string.IsNullOrEmpty(descriptors))
            {
                return Enumerable.Empty<SubjectDescriptor>();
            }

            return descriptors.Split(',').Where(descriptor => !string.IsNullOrEmpty(descriptor)).Select(descriptor => FromString(descriptor));
        }

        #region Equality and Compare

        #region Implement IEquatable to avoid boxing
        public bool Equals(SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(SubjectType, subjectDescriptor.SubjectType) &&
                    StringComparer.OrdinalIgnoreCase.Equals(Identifier, subjectDescriptor.Identifier);
        }
        #endregion

        public override bool Equals(object obj)
        {
            return obj is SubjectDescriptor && this == (SubjectDescriptor)obj;
        }

        public override int GetHashCode()
        {
            if (this == default(SubjectDescriptor))
            {
                return 0;
            }

            int hashCode = 7443; // "large" prime to start the seed

            // Bitshifting and subtracting once is an efficient way to multiply by our second "large" prime, 0x7ffff = 524287
            hashCode = (hashCode << 19) - hashCode + StringComparer.OrdinalIgnoreCase.GetHashCode(SubjectType);
            hashCode = (hashCode << 19) - hashCode + StringComparer.OrdinalIgnoreCase.GetHashCode(Identifier);

            return hashCode;
        }

        public static bool operator ==(SubjectDescriptor left, SubjectDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SubjectDescriptor left, SubjectDescriptor right)
        {
            return !left.Equals(right);
        }

        public static implicit operator string(SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.ToString();
        }

        internal static int Compare(SubjectDescriptor left, SubjectDescriptor right)
        {
            int retValue = StringComparer.OrdinalIgnoreCase.Compare(left.SubjectType, right.SubjectType);

            if (0 == retValue)
            {
                retValue = StringComparer.OrdinalIgnoreCase.Compare(left.Identifier, right.Identifier);
            }

            return retValue;
        }

        private static string NormalizeSubjectType(String subjectType)
        {
            // Look up the string in the static dictionary. If we get a hit, then
            // we'll use that string for the subject type instead. This saves memory
            // as well as improves compare/equals performance when comparing descriptors,
            // since Object.ReferenceEquals will return true a lot more often
            if (!Constants.SubjectTypeMap.TryGetValue(subjectType, out string normalizedSubjectType))
            {
                normalizedSubjectType = subjectType;
            }

            return normalizedSubjectType;
        }
        #endregion

        #region Validation
        //Copied from TFCommonUtil.cs
        private static void ValidateSubjectType(string subjectType)
        {
            if (string.IsNullOrEmpty(subjectType))
            {
                throw new ArgumentNullException(nameof(subjectType));
            }

            if (subjectType.Length < Constants.SubjectDescriptorPolicies.MinSubjectTypeLength || subjectType.Length > Constants.SubjectDescriptorPolicies.MaxSubjectTypeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(subjectType), subjectType, GraphResources.SubjectTypeLengthOutOfRange());
            }
        }

        private static void ValidateIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (identifier.Length > Constants.SubjectDescriptorPolicies.MaxIdentifierLength)
            {
                throw new ArgumentOutOfRangeException(nameof(identifier), identifier, GraphResources.IdentifierLengthOutOfRange());
            }
        }

        #endregion

        #region XML Serialization
        XmlSchema IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ArgumentUtility.CheckForNull(reader, nameof(reader));

            var isEmptyElement = reader.IsEmptyElement;

            reader.ReadStartElement();

            if (isEmptyElement)
            {
                return;
            }

            if (reader.NodeType == XmlNodeType.Text)
            {
                var sourceDescriptor = FromString(reader.ReadContentAsString());
                SubjectType = sourceDescriptor.SubjectType;
                Identifier = sourceDescriptor.Identifier;
            }
            else
            {
                while (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case nameof(SubjectType):
                            var subjectType = reader.ReadElementContentAsString();
                            ValidateSubjectType(subjectType);
                            SubjectType = subjectType;
                            break;
                        case nameof(Identifier):
                            var identifier = reader.ReadElementContentAsString();
                            ValidateIdentifier(identifier);
                            Identifier = identifier;
                            break;
                        default:
                            reader.ReadOuterXml();
                            break;
                    }
                }
            }

            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            ArgumentUtility.CheckForNull(writer, nameof(writer));

            if (Equals(default(SubjectDescriptor)))
            {
                return;
            }

            writer.WriteElementString(nameof(SubjectType), SubjectType);
            writer.WriteElementString(nameof(Identifier), Identifier);
        }
        #endregion
    }

    public class SubjectDescriptorComparer : IComparer<SubjectDescriptor>, IEqualityComparer<SubjectDescriptor>
    {
        private SubjectDescriptorComparer() { }

        public int Compare(SubjectDescriptor left, SubjectDescriptor right)
        {
            return SubjectDescriptor.Compare(left, right);
        }

        public bool Equals(SubjectDescriptor left, SubjectDescriptor right)
        {
            return left == right;
        }

        public int GetHashCode(SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.GetHashCode();
        }

        public static SubjectDescriptorComparer Instance { get; } = new SubjectDescriptorComparer();
    }

    // Keep this in sync with the IdentityDescriptorExtensions to avoid extra casting/conversions
    public static class SubjectDescriptorExtensions
    {
        internal static Guid GetMasterScopeId(this SubjectDescriptor subjectDescriptor)
        {
            if (!subjectDescriptor.IsGroupScopeType())
            {
                throw new InvalidSubjectTypeException(subjectDescriptor.SubjectType);
            }

            if (!Guid.TryParse(subjectDescriptor.Identifier, out Guid masterScopeId))
            {
                throw new ArgumentException($"Parameter {nameof(subjectDescriptor)} does not have a valid master scope ID");
            }

            return masterScopeId;
        }

        internal static Guid GetCuid(this SubjectDescriptor subjectDescriptor)
        {
            if (!subjectDescriptor.IsCuidBased())
            {
                throw new InvalidSubjectTypeException(subjectDescriptor.SubjectType);
            }

            if (!Guid.TryParse(subjectDescriptor.Identifier, out Guid cuid))
            {
                throw new ArgumentException($"Parameter {nameof(subjectDescriptor)} does not have a valid CUID");
            }

            return cuid;
        }

        public static bool IsWindowsType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.WindowsIdentity);
        }

        public static bool IsGroupType(this SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.IsAadGroupType() || subjectDescriptor.IsVstsGroupType();
        }

        public static bool IsAadGroupType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.AadGroup);
        }

        public static bool IsVstsGroupType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.VstsGroup);
        }

        public static bool IsClaimsUserType(this SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.IsAadUserType() || subjectDescriptor.IsMsaUserType();
        }

        public static bool IsAadUserType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.AadUser);
        }

        public static bool IsMsaUserType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.MsaUser);
        }

        public static bool IsBindPendingUserType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.BindPendingUser);
        }

        public static bool IsUnauthenticatedIdentityType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.UnauthenticatedIdentity);
        }

        public static bool IsServiceIdentityType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.ServiceIdentity);
        }

        public static bool IsAggregateIdentityType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.AggregateIdentity);
        }

        public static bool IsImportedIdentityType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.ImportedIdentity);
        }

        public static bool IsGroupScopeType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.GroupScopeType);
        }

        public static bool IsServerTestIdentityType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.ServerTestIdentity);
        }

        // ******* All types below this line are not backed by the graph or identity service ************************
        public static bool IsSystemServicePrincipalType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.SystemServicePrincipal);
        }

        public static bool IsSystemScopeType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.SystemScope);
        }

        public static bool IsSystemCspPartnerType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.SystemCspPartner);
        }

        public static bool IsSystemLicenseType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.SystemLicense);
        }

        public static bool IsSystemPublicAccessType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.SystemPublicAccess);
        }

        public static bool IsSystemAccessControlType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.SystemAccessControl);
        }

        public static bool IsSystemType(this SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.IsSystemServicePrincipalType() ||
                subjectDescriptor.IsSystemScopeType() ||
                subjectDescriptor.IsSystemLicenseType() ||
                subjectDescriptor.IsSystemCspPartnerType() ||
                subjectDescriptor.IsSystemPublicAccessType() ||
                subjectDescriptor.IsSystemAccessControlType();
        }

        public static bool IsSubjectStoreType(this SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.IsSystemServicePrincipalType() ||
               subjectDescriptor.IsSystemScopeType() ||
               subjectDescriptor.IsSystemLicenseType() ||
               subjectDescriptor.IsSystemCspPartnerType();
        }

        public static bool IsCspPartnerIdentityType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.CspPartnerIdentity);
        }

        public static bool IsUnknownSubjectType(this SubjectDescriptor subjectDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.Unknown) ||
                StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.UnknownGroup) ||
                StringComparer.OrdinalIgnoreCase.Equals(subjectDescriptor.SubjectType, Constants.SubjectType.UnknownUser);
        }

        public static bool IsCuidBased(this SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.IsClaimsUserType() || subjectDescriptor.IsCspPartnerIdentityType();
        }

        public static bool IsUserType(this SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.IsClaimsUserType() ||
                subjectDescriptor.IsCspPartnerIdentityType() ||
                subjectDescriptor.IsBindPendingUserType() ||
                subjectDescriptor.IsServiceIdentityType();
        }

        public static bool IsPubliclyAvailableGraphSubjectType(this SubjectDescriptor subjectDescriptor)
        {
            return (subjectDescriptor == default(SubjectDescriptor)) ||
                subjectDescriptor.IsUserType() ||
                subjectDescriptor.IsGroupType() ||
                subjectDescriptor.IsGroupScopeType();
        }
    }

    /// <summary>
    /// Converter to support data contract serialization.
    /// </summary>
    /// <remarks>
    /// This class should only be used to convert a descriptor string from the client back into a string
    /// tuple SubjectDescriptor type on the server. The client should be unaware that this tuple relationship exists
    /// and this should not permit that relationship to leak to the client.
    /// 
    /// Specifically, this is provided so that the MVC router can convert a string => SubjectDescriptor so
    /// that we can use the  [ClientParameterType(typeof(string))] SubjectDescriptor userDescriptor) convenience in each
    /// controller method.
    /// </remarks>
    public class SubjectDescriptorConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                return SubjectDescriptor.FromString((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is SubjectDescriptor)
            {
                SubjectDescriptor subjectDescriptor = (SubjectDescriptor)value;
                if (subjectDescriptor == default(SubjectDescriptor))
                {
                    // subjectDescriptor.ToString() returns null in the case of default(SubjectDescriptor)
                    // and null can not be deserialized when the object is a struct.
                    return string.Empty;
                }

                return subjectDescriptor.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
