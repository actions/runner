using System.Globalization;

namespace GitHub.DistributedTask.Expressions
{
    public static class ExpressionResources
    {
        public static string ExceededAllowedMemory(object arg0)
        {
            const string Format = @"The maximum allowed memory size was exceeded while evaluating the following expression: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidFormatArgIndex(object arg0)
        {
            const string Format = @"The following format string references more arguments than were supplied: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidFormatSpecifiers(object arg0, object arg1)
        {
            const string Format = @"The format specifiers '{0}' are not valid for objects of type '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidFormatString(object arg0)
        {
            const string Format = @"The following format string is invalid: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
