using System;
using System.Diagnostics;

namespace GitHub.Services.Common
{

    // NOTE: Since the recommendations are for Ordinal and OrdinalIgnoreCase, no need to explain those, but
    //       please explain any instances using non-Ordinal comparisons (CurrentCulture, InvariantCulture)
    //       so that developers following you can understand the choices and verify they are correct.

    // NOTE: please try to keep the semantic-named properties in alphabetical order to ease merges

    // NOTE: do NOT add xml doc comments - everything in here should be a very thin wrapper around String
    //       or StringComparer.  The usage of the methods and properties in this class should be intuitively
    //       obvious, so please don't add xml doc comments to this class since it should be wholly internal
    //       by the time we ship.

    // NOTE: Current guidelines from the CLR team (Dave Fetterman) is to stick with the same operation for both
    //       Compare and Equals for a given semantic inner class.  This has the nice side effect that you don't
    //       get different behavior between calling Equals or calling Compare == 0.  This may seem odd given the
    //       recommendations about using CurrentCulture for UI operations and Compare being used for sorting
    //       items for user display in many cases, but we need to have the type of string data determine the
    //       string comparison enum to use instead of the consumer of the comparison operation so that we're
    //       consistent in how we treat a given semantic.

    // VssStringComparer should act like StringComparer with a few additional methods for usefulness (Contains,
    // StartsWith, EndsWith, etc.) so that it can be a "one-stop shop" for string comparisons.
    public class VssStringComparer : StringComparer
    {
        private StringComparison m_stringComparison;
        private StringComparer m_stringComparer;

        protected VssStringComparer(StringComparison stringComparison)
            : base()
        {
            m_stringComparison = stringComparison;
        }

        // pass-through implementations based on our current StringComparison setting
        public override int Compare(string x, string y) { return String.Compare(x, y, m_stringComparison); }
        public override bool Equals(string x, string y) { return String.Equals(x, y, m_stringComparison); }
        public override int GetHashCode(string x) { return MatchingStringComparer.GetHashCode(x); }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public int Compare(string x, int indexX, string y, int indexY, int length) { return String.Compare(x, indexX, y, indexY, length, m_stringComparison); }

        // add new useful methods here
        public bool Contains(string main, string pattern)
        {
            ArgumentUtility.CheckForNull(main, "main");
            ArgumentUtility.CheckForNull(pattern, "pattern");

            return main.IndexOf(pattern, m_stringComparison) >= 0;
        }

        public int IndexOf(string main, string pattern)
        {
            ArgumentUtility.CheckForNull(main, "main");
            ArgumentUtility.CheckForNull(pattern, "pattern");

            return main.IndexOf(pattern, m_stringComparison);
        }

        public bool StartsWith(string main, string pattern)
        {
            ArgumentUtility.CheckForNull(main, "main");
            ArgumentUtility.CheckForNull(pattern, "pattern");

            return main.StartsWith(pattern, m_stringComparison);
        }

        public bool EndsWith(string main, string pattern)
        {
            ArgumentUtility.CheckForNull(main, "main");
            ArgumentUtility.CheckForNull(pattern, "pattern");

            return main.EndsWith(pattern, m_stringComparison);
        }

        private StringComparer MatchingStringComparer
        {
            get
            {
                if (m_stringComparer == null)
                {
                    switch (m_stringComparison)
                    {
                        case StringComparison.CurrentCulture:
                            m_stringComparer = StringComparer.CurrentCulture;
                            break;

                        case StringComparison.CurrentCultureIgnoreCase:
                            m_stringComparer = StringComparer.CurrentCultureIgnoreCase;
                            break;

                        case StringComparison.Ordinal:
                            m_stringComparer = StringComparer.Ordinal;
                            break;

                        case StringComparison.OrdinalIgnoreCase:
                            m_stringComparer = StringComparer.OrdinalIgnoreCase;
                            break;

                        default:
                            Debug.Assert(false, "Unknown StringComparison value");
                            m_stringComparer = StringComparer.Ordinal;
                            break;
                    }
                }
                return m_stringComparer;
            }
        }

        protected static VssStringComparer s_ordinal = new VssStringComparer(StringComparison.Ordinal);
        protected static VssStringComparer s_ordinalIgnoreCase = new VssStringComparer(StringComparison.OrdinalIgnoreCase);
        protected static VssStringComparer s_currentCulture = new VssStringComparer(StringComparison.CurrentCulture);
        protected static VssStringComparer s_currentCultureIgnoreCase = new VssStringComparer(StringComparison.CurrentCultureIgnoreCase);
        private static VssStringComparer s_dataSourceIgnoreProtocol = new DataSourceIgnoreProtocolComparer();


        public static VssStringComparer ActiveDirectoryEntityIdComparer { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ArtifactType { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ArtifactTool { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer AssemblyName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ContentType { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DomainName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DomainNameUI { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer DatabaseCategory { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DatabaseName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DataSource { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DataSourceIgnoreProtocol { get { return s_dataSourceIgnoreProtocol; } }
        public static VssStringComparer DirectoryName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DirectoryEntityIdentifierConstants { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DirectoryEntityPropertyComparer { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DirectoryEntityTypeComparer { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DirectoryEntryNameComparer { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer DirectoryKeyStringComparer { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer EncodingName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer EnvVar { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ExceptionSource { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer FilePath { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer FilePathUI { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer Guid { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer Hostname { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer HostnameUI { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer HttpRequestMethod { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer IdentityDescriptor { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer IdentityDomain { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer IdentityOriginId { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer IdentityType { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer LinkName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer MachineName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer MailAddress { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer PropertyName { get { return s_ordinalIgnoreCase; } }        
        public static VssStringComparer RegistrationAttributeName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ReservedGroupName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer WMDSchemaClassName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer SamAccountName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer AccountName { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer SocialType { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer ServerUrl { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ServerUrlUI { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer ServiceInterface { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ServicingOperation { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ToolId { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer Url { get { return s_ordinal; } }
        public static VssStringComparer UrlPath { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer UriScheme { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer UriAuthority { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer UserId { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer UserName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer UserNameUI { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer XmlAttributeName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer XmlNodeName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer XmlElement { get { return s_ordinal; } }
        public static VssStringComparer XmlAttributeValue { get { return s_ordinalIgnoreCase; } }

        //Framework comparers.
        public static VssStringComparer RegistryPath { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ServiceType { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer AccessMappingMoniker { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer CatalogNodePath { get { return s_ordinal; } }
        public static VssStringComparer CatalogServiceReference { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer CatalogNodeDependency { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ServicingTokenName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer IdentityPropertyName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer Collation { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer FeatureAvailabilityName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer TagName { get { return s_currentCultureIgnoreCase; } }
        
        //Framework Hosting comparers.
        public static VssStringComparer HostingAccountPropertyName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer MessageBusName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer MessageBusSubscriptionName { get { return s_ordinalIgnoreCase; } }

        public static VssStringComparer SID { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer FieldName { get { return s_ordinal; } }
        public static VssStringComparer FieldNameUI { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer FieldType { get { return s_ordinal; } }
        public static VssStringComparer EventType { get { return s_ordinal; } }
        public static VssStringComparer EventTypeIgnoreCase { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer RegistrationEntryName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ServerName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer GroupName { get { return s_currentCultureIgnoreCase; } }
        public static VssStringComparer RegistrationUtilities { get { return s_ordinal; } }
        public static VssStringComparer RegistrationUtilitiesCaseInsensitive { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer IdentityName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer IdentityNameOrdinal { get { return s_ordinal; } }
        public static VssStringComparer PlugInId { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ExtensionName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer ExtensionType { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer DomainUrl { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer AccountInfoAccount { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer AccountInfoPassword { get { return s_ordinal; } }
        public static VssStringComparer AttributesDescriptor { get { return s_ordinalIgnoreCase; } }        

        // Converters comparer
        public static VssStringComparer VSSServerPath { get { return s_ordinalIgnoreCase; } }

        // Item rename in VSS is case sensitive.
        public static VssStringComparer VSSItemName { get { return s_ordinal; } }
        // Web Access Comparers
        public static VssStringComparer HtmlElementName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer HtmlAttributeName { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer HtmlAttributeValue { get { return s_ordinalIgnoreCase; } }

        public static VssStringComparer StringFieldConditionEquality { get { return s_ordinalIgnoreCase; } }
        public static VssStringComparer StringFieldConditionOrdinal { get { return s_ordinal; } }

        // Service Endpoint Comparer
        public static VssStringComparer ServiceEndpointTypeCompararer { get { return s_ordinalIgnoreCase; } }

        private class DataSourceIgnoreProtocolComparer : VssStringComparer
        {
            public DataSourceIgnoreProtocolComparer()
                : base(StringComparison.OrdinalIgnoreCase)
            {
            }

            public override int Compare(string x, string y)
            {
                return base.Compare(RemoveProtocolPrefix(x), RemoveProtocolPrefix(y));
            }

            public override bool Equals(string x, string y)
            {
                return base.Equals(RemoveProtocolPrefix(x), RemoveProtocolPrefix(y));
            }

            private static string RemoveProtocolPrefix(string x)
            {
                if (x != null)
                {
                    if (x.StartsWith(c_tcpPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        x = x.Substring(c_tcpPrefix.Length);
                    }
                    else if (x.StartsWith(c_npPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        x = x.Substring(c_npPrefix.Length);
                    }
                }

                return x;
            }

            private const string c_tcpPrefix = "tcp:";
            private const string c_npPrefix = "np:";
        }
    }
}
