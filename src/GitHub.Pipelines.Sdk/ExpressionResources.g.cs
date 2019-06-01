using System.Globalization;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    public static class ExpressionResources
    {
        public static string ExceededAllowedMemory(params object[] args)
        {
            const string Format = @"The maximum allowed memory size was exceeded while evaluating the following expression: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExceededMaxExpressionDepth(params object[] args)
        {
            const string Format = @"Exceeded max expression depth {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExceededMaxExpressionLength(params object[] args)
        {
            const string Format = @"Exceeded max expression length {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedPropertyName(params object[] args)
        {
            const string Format = @"Expected a property name to follow the dereference operator '.'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedStartParameter(params object[] args)
        {
            const string Format = @"Expected '(' to follow a function";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidFormatArgIndex(params object[] args)
        {
            const string Format = @"The following format string references more arguments than were supplied: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidFormatSpecifiers(params object[] args)
        {
            const string Format = @"The format specifiers '{0}' are not valid for objects of type '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidFormatString(params object[] args)
        {
            const string Format = @"The following format string is invalid: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string KeyNotFound(params object[] args)
        {
            const string Format = @"Key not found '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ParseErrorWithFwlink(params object[] args)
        {
            const string Format = @"{0}. For more help, refer to https://go.microsoft.com/fwlink/?linkid=842996";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ParseErrorWithTokenInfo(params object[] args)
        {
            const string Format = @"{0}: '{1}'. Located at position {2} within expression: {3}. For more help, refer to https://go.microsoft.com/fwlink/?linkid=842996";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TypeCastError(params object[] args)
        {
            const string Format = @"Unable to convert from {0} to {1}. Value: {2}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TypeCastErrorNoValue(params object[] args)
        {
            const string Format = @"Unable to convert from {0} to {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TypeCastErrorWithError(params object[] args)
        {
            const string Format = @"Unable to convert from {0} to {1}. Value: {2}. Error: {3}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnclosedFunction(params object[] args)
        {
            const string Format = @"Unclosed function";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnclosedIndexer(params object[] args)
        {
            const string Format = @"Unclosed indexer";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnexpectedSymbol(params object[] args)
        {
            const string Format = @"Unexpected symbol";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnrecognizedValue(params object[] args)
        {
            const string Format = @"Unrecognized value";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
