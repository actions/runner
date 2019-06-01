using System.Globalization;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating
{
    public static class TemplateStrings
    {
        public static string DirectiveNotAllowed(params object[] args)
        {
            const string Format = @"The expression directive '{0}' is not supported in this context";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DirectiveNotAllowedInline(params object[] args)
        {
            const string Format = @"The directive '{0}' is not allowed in this context. Directives are not supported for expressions that are embedded within a string. Directives are only supported when the entire value is an expression.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedExpression(params object[] args)
        {
            const string Format = @"An expression was expected";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedMapping(params object[] args)
        {
            const string Format = @"Expected a mapping";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedNParametersFollowingDirective(params object[] args)
        {
            const string Format = @"Exactly {0} parameter(s) were expected following the directive '{1}'. Actual parameter count: {2}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedScalar(params object[] args)
        {
            const string Format = @"Expected a scalar value";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedScalarSequenceOrMapping(params object[] args)
        {
            const string Format = @"Expected a scalar value, a sequence, or a mapping";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpectedSequence(params object[] args)
        {
            const string Format = @"Expected a sequence";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpressionNotAllowed(params object[] args)
        {
            const string Format = @"A template expression is not allowed in this context";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpressionNotClosed(params object[] args)
        {
            const string Format = @"The expression is not closed. An unescaped ${{ sequence was found, but the closing }} sequence was not found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string LineColumn(params object[] args)
        {
            const string Format = @"(Line: {0}, Col: {1})";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MaxObjectDepthExceeded(params object[] args)
        {
            const string Format = @"Maximum object depth exceeded";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MaxObjectSizeExceeded(params object[] args)
        {
            const string Format = @"Maximum object size exceeded";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MaxTemplateEventsExceeded(params object[] args)
        {
            const string Format = @"Maximum events exceeded while evaluating the template. This may indicate an infinite loop or too many nested loops.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TemplateNotValid(params object[] args)
        {
            const string Format = @"The template is not valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TemplateNotValidWithErrors(params object[] args)
        {
            const string Format = @"The template is not valid. {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnableToConvertToTemplateToken(params object[] args)
        {
            const string Format = @"Unable to convert the object to a template token. Actual type '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnableToDetermineOneOf(params object[] args)
        {
            const string Format = @"There's not enough info to determine what you meant. Add one of these properties: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnexpectedMappingStart(params object[] args)
        {
            const string Format = @"A mapping was not expected";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnexpectedSequenceStart(params object[] args)
        {
            const string Format = @"A sequence was not expected";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnexpectedValue(params object[] args)
        {
            const string Format = @"Unexpected value '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValueAlreadyDefined(params object[] args)
        {
            const string Format = @"'{0}' is already defined";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
