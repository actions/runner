using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class GraphResources
    {

        public static string CannotEditChildrenOfNonGroup(object arg0)
        {
            const string Format = @"VS403339: Cannot add or remove child from graph subject with descriptor '{0}' because it is not a group.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string EmptySubjectDescriptorNotAllowed(object arg0)
        {
            const string Format = @"VS403350: The empty subject descriptor is not a valid value for parameter '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string WellKnownSidNotAllowed(object arg0)
        {
            const string Format = @"VS403350: Well-known SIDs are not valid for the parameter '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string GraphMembershipNotFound(object arg0, object arg1)
        {
            const string Format = @"VS403328: The graph membership for member descriptor '{0}' and container descriptor '{1}' could not be found. You may need to create this membership in the enclosing enterprise or organization.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string GraphSubjectNotFound(object arg0)
        {
            const string Format = @"VS403325: The graph subject with descriptor '{0}' could not be found. You may need to create the subject in the enclosing enterprise, or add organization-level memberships to make a subject in the enterprise visible in the enclosing organization";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidGraphLegacyDescriptor(object arg0)
        {
            const string Format = @"VS860018: The provided legacy descriptor '{0}' is not a valid legacy descriptor for this end point.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidGraphMemberCuid(object arg0)
        {
            const string Format = @"VS403323: Cannot find graph member storage key for cuid: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidGraphMemberStorageKey(object arg0)
        {
            const string Format = @"VS403324: Cannot find graph member cuid for storage key {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidGraphSubjectDescriptor(object arg0)
        {
            const string Format = @"VS860021: The provided descriptor '{0}' is not a valid graph subject descriptor for this end point.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string StorageKeyNotFound(object arg0)
        {
            const string Format = @"VS403369: The storage key for descriptor '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SubjectDescriptorNotFoundWithIdentityDescriptor(object arg0)
        {
            const string Format = @"VS403370: The subject descriptor for identity descriptor '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SubjectDescriptorNotFoundWithStorageKey(object arg0)
        {
            const string Format = @"VS403368: The subject descriptor for storage key '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentifierLengthOutOfRange()
        {
            const string Format = @"Given identifier length is out of range of valid values.";
            return Format;
        }

        public static string SubjectTypeLengthOutOfRange()
        {
            const string Format = @"Given subject type length is out of range of valid values.";
            return Format;
        }
    }
}
