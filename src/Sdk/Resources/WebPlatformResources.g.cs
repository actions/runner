using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class WebPlatformResources
    {

        public static string AppSessionTokenException(object arg0)
        {
            const string Format = @"Error issuing app session token: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SessionTokenException(object arg0)
        {
            const string Format = @"Error issuing session token: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
