using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class SecurityResources
    {
        public static string InvalidAclStoreException(params object[] args)
        {
            const string Format = @"The ACL store with identifier '{1}' was not found in the security namespace '{0}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidPermissionsException(params object[] args)
        {
            const string Format = @"VS403284: Invalid operation. Unable to set bits '{1}' in security namespace '{0}' as it is reserved by the system.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
