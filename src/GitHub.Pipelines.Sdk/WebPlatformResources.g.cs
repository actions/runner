using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class WebPlatformResources
    {
        public static string AppSessionTokenException(params object[] args)
        {
            const string Format = @"Error issuing app session token: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SessionTokenException(params object[] args)
        {
            const string Format = @"Error issuing session token: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
