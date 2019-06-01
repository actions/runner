using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class NameResolutionResources
    {
        public static string MultiplePrimaryNameResolutionEntriesError(params object[] args)
        {
            const string Format = @"The name resolution entry update contains multiple IsPrimary entries for Value: {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NameResolutionEntryAlreadyExistsError(params object[] args)
        {
            const string Format = @"A name resolution entry already exists for the Name but with a different Value. Name: {0}. Value: {1}. ConflictingValue: {2}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
