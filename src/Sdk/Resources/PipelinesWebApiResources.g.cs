using System.Globalization;

namespace GitHub.Actions.Pipelines.WebApi
{
    public static class PipelinesWebApiResources
    {

        public static string FlagEnumTypeRequired()
        {
            const string Format = @"Invalid type. An enum type with the Flags attribute must be supplied.";
            return Format;
        }

        public static string InvalidFlagsEnumValue(object arg0, object arg1)
        {
            const string Format = @"'{0}' is not a valid value for {1}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string NonEmptyEnumElementsRequired(object arg0)
        {
            const string Format = @"Each comma separated enum value must be non-empty: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
