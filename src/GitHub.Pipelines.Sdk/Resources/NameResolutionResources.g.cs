using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class NameResolutionResources
    {

        public static string MultiplePrimaryNameResolutionEntriesError(object arg0)
        {
            const string Format = @"The name resolution entry update contains multiple IsPrimary entries for Value: {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string NameResolutionEntryAlreadyExistsError(object arg0, object arg1, object arg2)
        {
            const string Format = @"A name resolution entry already exists for the Name but with a different Value. Name: {0}. Value: {1}. ConflictingValue: {2}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }
    }
}
