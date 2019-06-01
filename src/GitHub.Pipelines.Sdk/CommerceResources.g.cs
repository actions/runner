using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class CommerceResources
    {
        public static string UnsupportedSubscriptionTypeExceptionMessage(params object[] args)
        {
            const string Format = @"The subscription type is unsupported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UserIsNotSubscriptionAdmin(params object[] args)
        {
            const string Format = @"User is not a subscription administrator or co-administrator of the Azure subscription.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UserNotAccountAdministrator(params object[] args)
        {
            const string Format = @"User ""{0}"" is not the organization owner of ""{1}"".";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OfferMeterNotFoundExceptionMessage(params object[] args)
        {
            const string Format = @"No offer meter found for name or galleryItemId: ""{0}"".";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
