using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class PartitioningResources
    {
        public static string PartitionContainerMustBeOfflineError(params object[] args)
        {
            const string Format = @"The partition container with Id '{0}' must be offline before deleting.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
