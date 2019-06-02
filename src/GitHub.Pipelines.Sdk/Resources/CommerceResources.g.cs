using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class CommerceResources
    {

        public static string UnsupportedSubscriptionTypeExceptionMessage()
        {
            const string Format = @"The subscription type is unsupported.";
            return Format;
        }

        public static string UserIsNotSubscriptionAdmin()
        {
            const string Format = @"User is not a subscription administrator or co-administrator of the Azure subscription.";
            return Format;
        }

        public static string UserNotAccountAdministrator(object arg0, object arg1)
        {
            const string Format = @"User ""{0}"" is not the organization owner of ""{1}"".";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string OfferMeterNotFoundExceptionMessage(object arg0)
        {
            const string Format = @"No offer meter found for name or galleryItemId: ""{0}"".";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
