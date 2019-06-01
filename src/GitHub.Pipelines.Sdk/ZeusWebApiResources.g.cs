using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class ZeusWebApiResources
    {
        public static string BlobCopyRequestNotFoundException(params object[] args)
        {
            const string Format = @"The blob copy with id '{0}' could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DatabaseMigrationNotFoundException(params object[] args)
        {
            const string Format = @"The migration with id '{0}' could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
