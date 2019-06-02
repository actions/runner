using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class AccountResources
    {

        public static string AccountExists(object arg0)
        {
            const string Format = @"The following organization already exists: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountMarkedForDeletionError(object arg0)
        {
            const string Format = @"Operation not permitted. Organization with id {0} has been marked for deletion.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountNotFound()
        {
            const string Format = @"Organization not found.";
            return Format;
        }

        public static string AccountNotFoundByIdError(object arg0)
        {
            const string Format = @"No organization found for accountId {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountNotMarkedForDeletion()
        {
            const string Format = @"Hosting account cannot be deleted. Organization is not marked EligibleForDeletion.";
            return Format;
        }

        public static string MaxNumberOfAccountsExceptions()
        {
            const string Format = @"Maximum number of organizations reached.";
            return Format;
        }

        public static string MaxNumberOfAccountsPerUserException()
        {
            const string Format = @"Maximum number of organizations for user reached.";
            return Format;
        }

        public static string AccountNotMarkedForDeletionError(object arg0)
        {
            const string Format = @"Operation not permitted. Organization with id {0} has not been marked for deletion.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountHostmappingNotFoundById(object arg0)
        {
            const string Format = @"No organization host mapping found for hostId {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountServiceLockDownModeException()
        {
            const string Format = @"Organization Service is currently in lock down mode.";
            return Format;
        }

        public static string AccountUserNotFoundException(object arg0, object arg1)
        {
            const string Format = @"User with userId={0} is not a member of accountId={1}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string RegionExists(object arg0)
        {
            const string Format = @"TF1510000 :The following region already exists: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountNameReserved(object arg0)
        {
            const string Format = @"The specified organization name is reserved: '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountServiceUnavailableException()
        {
            const string Format = @"Organization Service is temporarily not available.";
            return Format;
        }

        public static string AccountNameTemporarilyUnavailable()
        {
            const string Format = @"The organization URL is not available. Please try again later.";
            return Format;
        }

        public static string AccountMustBeUnlinkedBeforeDeletion()
        {
            const string Format = @"You must first remove billing before deleting your organization.";
            return Format;
        }
    }
}
