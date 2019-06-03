using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Graph;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using GitHub.Services.WebApi;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;

namespace GitHub.Services.Common
{
    [TypeConverter(typeof(SocialDescriptorConverter))]
    public struct SocialDescriptor : IEquatable<SocialDescriptor>, IXmlSerializable
    {
        public SocialDescriptor(string socialType, string identifier)
        {
            ValidateSocialType(socialType);
            ValidateIdentifier(identifier);

            SocialType = NormalizeSocialType(socialType);
            Identifier = identifier;
        }

        [DataMember]
        public string SocialType { get; private set; }

        [DataMember]
        public string Identifier { get; private set; }

        public override string ToString()
        {
            if (this == default(SocialDescriptor))
            {
                return null;
            }

            return string.Concat(
                Constants.SocialDescriptorPrefix,
                SocialType,
                Constants.SocialDescriptorPartsSeparator,
                PrimitiveExtensions.ToBase64StringNoPaddingFromString(Identifier));
        }

        public static SocialDescriptor FromString(string socialDescriptorString)
        {
            if (string.IsNullOrEmpty(socialDescriptorString))
            {
                return default(SocialDescriptor);
            }

            if (!socialDescriptorString.StartsWith(Constants.SocialDescriptorPrefix))
            {
                return new SocialDescriptor(Constants.SocialType.Unknown, socialDescriptorString);
            }

            if (socialDescriptorString.Length < Constants.SocialDescriptorPolicies.MinSocialDescriptorStringLength)
            {
                return new SocialDescriptor(Constants.SocialType.Unknown, socialDescriptorString);
            }

            var tokens = socialDescriptorString.Split(new char[] { Constants.SocialDescriptorPartsSeparator }, 3);
            if (tokens.Length != 2)
            {
                return new SocialDescriptor(Constants.SocialType.Unknown, socialDescriptorString);
            }

            string moniker = tokens[0].Substring(1);
            string identifier = tokens[1];

            try
            {
                return new SocialDescriptor(moniker, PrimitiveExtensions.FromBase64StringNoPaddingToString(identifier));
            }
            catch { }

            return new SocialDescriptor(Constants.SocialType.Unknown, socialDescriptorString);
        }

        /// <summary>
        /// Parses a string of comma separated social descriptors into a enumerable list of <see cref="SocialDescriptor"/> objects.
        /// </summary>
        /// <returns>empty enumerable if parameter 'descriptors' is null or empty</returns>
        public static IEnumerable<SocialDescriptor> FromCommaSeperatedStrings(string descriptors)
        {
            if (string.IsNullOrEmpty(descriptors))
            {
                return Enumerable.Empty<SocialDescriptor>();
            }

            return descriptors.Split(Constants.SocialListSeparator).Where(descriptor => !string.IsNullOrEmpty(descriptor)).Select(descriptor => FromString(descriptor));
        }

        #region Equality and Compare

        #region Implement IEquatable to avoid boxing
        public bool Equals(SocialDescriptor socialDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(SocialType, socialDescriptor.SocialType) &&
                    StringComparer.Ordinal.Equals(Identifier, socialDescriptor.Identifier); // The Social Identifier can be case sensitive, hence avoiding the case ignore check
        }
        #endregion

        public override bool Equals(object obj)
        {
            return obj is SocialDescriptor && this == (SocialDescriptor)obj;
        }

        public override int GetHashCode()
        {
            if (this == default(SocialDescriptor))
            {
                return 0;
            }

            int hashCode = 7443; // "large" prime to start the seed

            // Bitshifting and subtracting once is an efficient way to multiply by our second "large" prime, 0x7ffff = 524287
            hashCode = (hashCode << 19) - hashCode + StringComparer.OrdinalIgnoreCase.GetHashCode(SocialType);
            hashCode = (hashCode << 19) - hashCode + StringComparer.Ordinal.GetHashCode(Identifier);

            return hashCode;
        }

        public static bool operator ==(SocialDescriptor left, SocialDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SocialDescriptor left, SocialDescriptor right)
        {
            return !left.Equals(right);
        }

        public static implicit operator string(SocialDescriptor socialDescriptor)
        {
            return socialDescriptor.ToString();
        }

        internal static int Compare(SocialDescriptor left, SocialDescriptor right)
        {
            int retValue = StringComparer.OrdinalIgnoreCase.Compare(left.SocialType, right.SocialType);

            if (0 == retValue)
            {
                retValue = StringComparer.Ordinal.Compare(left.Identifier, right.Identifier);
            }

            return retValue;
        }

        private static string NormalizeSocialType(String socialType)
        {
            // Look up the string in the static dictionary. If we get a hit, then
            // we'll use that string for the social type instead. This saves memory
            // as well as improves compare/equals performance when comparing descriptors,
            // since Object.ReferenceEquals will return true a lot more often
            if (!Constants.SocialTypeMap.TryGetValue(socialType, out string normalizedSocialType))
            {
                normalizedSocialType = socialType;
            }

            return normalizedSocialType;
        }
        #endregion

        #region Validation
        //Copied from TFCommonUtil.cs
        private static void ValidateSocialType(string socialType)
        {
            if (string.IsNullOrEmpty(socialType))
            {
                throw new ArgumentNullException(nameof(socialType));
            }

            if (socialType.Length < Constants.SocialDescriptorPolicies.MinSocialTypeLength || socialType.Length > Constants.SocialDescriptorPolicies.MaxSocialTypeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(socialType), socialType, GraphResources.SubjectTypeLengthOutOfRange());
            }
        }

        private static void ValidateIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException(nameof(identifier));
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
                SocialType = sourceDescriptor.SocialType;
                Identifier = sourceDescriptor.Identifier;
            }
            else
            {
                while (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case nameof(SocialType):
                            var socialType = reader.ReadElementContentAsString();
                            ValidateSocialType(socialType);
                            SocialType = socialType;
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

            if (Equals(default(SocialDescriptor)))
            {
                return;
            }

            writer.WriteElementString(nameof(SocialType), SocialType);
            writer.WriteElementString(nameof(Identifier), Identifier);
        }
        #endregion
    }

    public class SocialDescriptorComparer : IComparer<SocialDescriptor>, IEqualityComparer<SocialDescriptor>
    {
        private SocialDescriptorComparer() { }

        public int Compare(SocialDescriptor left, SocialDescriptor right)
        {
            return SocialDescriptor.Compare(left, right);
        }

        public bool Equals(SocialDescriptor left, SocialDescriptor right)
        {
            return left == right;
        }

        public int GetHashCode(SocialDescriptor socialDescriptor)
        {
            return socialDescriptor.GetHashCode();
        }

        public static SocialDescriptorComparer Instance { get; } = new SocialDescriptorComparer();
    }

    public static class SocialDescriptorExtensions
    {
        public static bool IsGitHubSocialType(this SocialDescriptor socialDescriptor)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(socialDescriptor.SocialType ?? String.Empty, Constants.SocialType.GitHub);
        }

        public static bool IsSocialType(this SubjectDescriptor subjectDescriptor)
        {
            return subjectDescriptor.ToString().StartsWith(Constants.SocialDescriptorPrefix);
        }
    }

    /// <summary>
    /// Converter to support data contract serialization.
    /// </summary>
    /// <remarks>
    /// This class should only be used to convert a descriptor string from the client back into a string
    /// tuple SocialDescriptor type on the server. The client should be unaware that this tuple relationship exists
    /// and this should not permit that relationship to leak to the client.
    /// 
    /// Specifically, this is provided so that the MVC router can convert a string => SocialDescriptor so
    /// that we can use the  [ClientParameterType(typeof(string))] SocialDescriptor socialDescriptor) convenience in each
    /// controller method.
    /// </remarks>
    public class SocialDescriptorConverter : TypeConverter
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
                return SocialDescriptor.FromString((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is SocialDescriptor)
            {
                SocialDescriptor socialDescriptor = (SocialDescriptor)value;
                if (socialDescriptor == default(SocialDescriptor))
                {
                    // socialDescriptor.ToString() returns null in the case of default(SocialDescriptor)
                    // and null can not be deserialized when the object is a struct.
                    return string.Empty;
                }

                return socialDescriptor.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
