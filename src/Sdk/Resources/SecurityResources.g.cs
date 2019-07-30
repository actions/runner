using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class SecurityResources
    {

        public static string InvalidAclStoreException(object arg0, object arg1)
        {
            const string Format = @"The ACL store with identifier '{1}' was not found in the security namespace '{0}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidPermissionsException(object arg0, object arg1)
        {
            const string Format = @"Invalid operation. Unable to set bits '{1}' in security namespace '{0}' as it is reserved by the system.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }
    }
}
