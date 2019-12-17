using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class IdentityResources
    {
        public static string FieldReadOnly(object arg0)
        {
            const string Format = @"{0} is read-only.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
