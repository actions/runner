using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class GraphResources
    {
        public static string CannotEditChildrenOfNonGroup(params object[] args)
        {
            const string Format = @"VS403339: Cannot add or remove child from graph subject with descriptor '{0}' because it is not a group.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EmptySubjectDescriptorNotAllowed(params object[] args)
        {
            const string Format = @"VS403350: The empty subject descriptor is not a valid value for parameter '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string WellKnownSidNotAllowed(params object[] args)
        {
            const string Format = @"VS403350: Well-known SIDs are not valid for the parameter '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GraphMembershipNotFound(params object[] args)
        {
            const string Format = @"VS403328: The graph membership for member descriptor '{0}' and container descriptor '{1}' could not be found. You may need to create this membership in the enclosing enterprise or organization.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GraphSubjectNotFound(params object[] args)
        {
            const string Format = @"VS403325: The graph subject with descriptor '{0}' could not be found. You may need to create the subject in the enclosing enterprise, or add organization-level memberships to make a subject in the enterprise visible in the enclosing organization";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidGraphLegacyDescriptor(params object[] args)
        {
            const string Format = @"VS860018: The provided legacy descriptor '{0}' is not a valid legacy descriptor for this end point.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidGraphMemberCuid(params object[] args)
        {
            const string Format = @"VS403323: Cannot find graph member storage key for cuid: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidGraphMemberStorageKey(params object[] args)
        {
            const string Format = @"VS403324: Cannot find graph member cuid for storage key {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidGraphSubjectDescriptor(params object[] args)
        {
            const string Format = @"VS860021: The provided descriptor '{0}' is not a valid graph subject descriptor for this end point.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StorageKeyNotFound(params object[] args)
        {
            const string Format = @"VS403369: The storage key for descriptor '{0}' could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SubjectDescriptorNotFoundWithIdentityDescriptor(params object[] args)
        {
            const string Format = @"VS403370: The subject descriptor for identity descriptor '{0}' could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SubjectDescriptorNotFoundWithStorageKey(params object[] args)
        {
            const string Format = @"VS403368: The subject descriptor for storage key '{0}' could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentifierLengthOutOfRange(params object[] args)
        {
            const string Format = @"Given identifier length is out of range of valid values.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SubjectTypeLengthOutOfRange(params object[] args)
        {
            const string Format = @"Given subject type length is out of range of valid values.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
