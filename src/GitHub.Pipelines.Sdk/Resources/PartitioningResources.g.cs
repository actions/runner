using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class PartitioningResources
    {

        public static string PartitionContainerMustBeOfflineError(object arg0)
        {
            const string Format = @"The partition container with Id '{0}' must be offline before deleting.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
