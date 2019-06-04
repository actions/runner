using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class ZeusWebApiResources
    {

        public static string BlobCopyRequestNotFoundException(object arg0)
        {
            const string Format = @"The blob copy with id '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string DatabaseMigrationNotFoundException(object arg0)
        {
            const string Format = @"The migration with id '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
