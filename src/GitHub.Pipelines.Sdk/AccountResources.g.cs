using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class AccountResources
    {
        public static string AccountExists(params object[] args)
        {
            const string Format = @"The following organization already exists: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountMarkedForDeletionError(params object[] args)
        {
            const string Format = @"Operation not permitted. Organization with id {0} has been marked for deletion.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountNotFound(params object[] args)
        {
            const string Format = @"Organization not found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountNotFoundByIdError(params object[] args)
        {
            const string Format = @"No organization found for accountId {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountNotMarkedForDeletion(params object[] args)
        {
            const string Format = @"Hosting account cannot be deleted. Organization is not marked EligibleForDeletion.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MaxNumberOfAccountsExceptions(params object[] args)
        {
            const string Format = @"Maximum number of organizations reached.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MaxNumberOfAccountsPerUserException(params object[] args)
        {
            const string Format = @"Maximum number of organizations for user reached.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountNotMarkedForDeletionError(params object[] args)
        {
            const string Format = @"Operation not permitted. Organization with id {0} has not been marked for deletion.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountHostmappingNotFoundById(params object[] args)
        {
            const string Format = @"No organization host mapping found for hostId {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountServiceLockDownModeException(params object[] args)
        {
            const string Format = @"Organization Service is currently in lock down mode.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountUserNotFoundException(params object[] args)
        {
            const string Format = @"User with userId={0} is not a member of accountId={1}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RegionExists(params object[] args)
        {
            const string Format = @"TF1510000 :The following region already exists: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountNameReserved(params object[] args)
        {
            const string Format = @"The specified organization name is reserved: '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountServiceUnavailableException(params object[] args)
        {
            const string Format = @"Organization Service is temporarily not available.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountNameTemporarilyUnavailable(params object[] args)
        {
            const string Format = @"The organization URL is not available. Please try again later.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountMustBeUnlinkedBeforeDeletion(params object[] args)
        {
            const string Format = @"You must first remove billing before deleting your organization.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
