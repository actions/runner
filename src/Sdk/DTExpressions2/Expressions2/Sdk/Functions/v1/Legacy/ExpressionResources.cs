using System.Globalization;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Legacy
{
    public static class ExpressionResources
    {

        public static string ExceededAllowedMemory(object arg0)
        {
            const string Format = @"The maximum allowed memory size was exceeded while evaluating the following expression: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ExceededMaxExpressionDepth(object arg0)
        {
            const string Format = @"Exceeded max expression depth {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ExceededMaxExpressionLength(object arg0)
        {
            const string Format = @"Exceeded max expression length {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ExpectedPropertyName()
        {
            const string Format = @"Expected a property name to follow the dereference operator '.'";
            return Format;
        }

        public static string ExpectedStartParameter()
        {
            const string Format = @"Expected '(' to follow a function";
            return Format;
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

        public static string KeyNotFound(object arg0)
        {
            const string Format = @"Key not found '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ParseErrorWithFwlink(object arg0)
        {
            const string Format = @"{0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ParseErrorWithTokenInfo(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"{0}: '{1}'. Located at position {2} within expression: {3}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string TypeCastError(object arg0, object arg1, object arg2)
        {
            const string Format = @"Unable to convert from {0} to {1}. Value: {2}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string TypeCastErrorNoValue(object arg0, object arg1)
        {
            const string Format = @"Unable to convert from {0} to {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string TypeCastErrorWithError(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"Unable to convert from {0} to {1}. Value: {2}. Error: {3}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string UnclosedFunction()
        {
            const string Format = @"Unclosed function";
            return Format;
        }

        public static string UnclosedIndexer()
        {
            const string Format = @"Unclosed indexer";
            return Format;
        }

        public static string UnexpectedSymbol()
        {
            const string Format = @"Unexpected symbol";
            return Format;
        }

        public static string UnrecognizedValue()
        {
            const string Format = @"Unrecognized value";
            return Format;
        }
    }
}
