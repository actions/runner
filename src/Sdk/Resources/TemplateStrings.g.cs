using System.Globalization;

namespace GitHub.DistributedTask.ObjectTemplating
{
    public static class TemplateStrings
    {
        public static string DirectiveNotAllowed(object arg0)
        {
            const string Format = @"The expression directive '{0}' is not supported in this context";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string DirectiveNotAllowedInline(object arg0)
        {
            const string Format = @"The directive '{0}' is not allowed in this context. Directives are not supported for expressions that are embedded within a string. Directives are only supported when the entire value is an expression.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ExpectedExpression()
        {
            const string Format = @"An expression was expected";
            return Format;
        }

        public static string ExpectedMapping()
        {
            const string Format = @"Expected a mapping";
            return Format;
        }

        public static string ExpectedNParametersFollowingDirective(object arg0, object arg1, object arg2)
        {
            const string Format = @"Exactly {0} parameter(s) were expected following the directive '{1}'. Actual parameter count: {2}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string ExpectedScalarSequenceOrMapping()
        {
            const string Format = @"Expected a scalar value, a sequence, or a mapping";
            return Format;
        }

        public static string ExpectedSequence()
        {
            const string Format = @"Expected a sequence";
            return Format;
        }

        public static string ExpressionNotAllowed()
        {
            const string Format = @"A template expression is not allowed in this context";
            return Format;
        }

        public static string ExpressionNotClosed()
        {
            const string Format = @"The expression is not closed. An unescaped ${{ sequence was found, but the closing }} sequence was not found.";
            return Format;
        }

        public static string LineColumn(object arg0, object arg1)
        {
            const string Format = @"(Line: {0}, Col: {1})";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MaxObjectDepthExceeded()
        {
            const string Format = @"Maximum object depth exceeded";
            return Format;
        }

        public static string MaxObjectSizeExceeded()
        {
            const string Format = @"Maximum object size exceeded";
            return Format;
        }

        public static string MaxTemplateEventsExceeded()
        {
            const string Format = @"Maximum events exceeded while evaluating the template. This may indicate an infinite loop or too many nested loops.";
            return Format;
        }

        public static string TemplateNotValid()
        {
            const string Format = @"The template is not valid.";
            return Format;
        }

        public static string TemplateNotValidWithErrors(object arg0)
        {
            const string Format = @"The template is not valid. {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UnableToConvertToTemplateToken(object arg0)
        {
            const string Format = @"Unable to convert the object to a template token. Actual type '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UnableToDetermineOneOf(object arg0)
        {
            const string Format = @"There's not enough info to determine what you meant. Add one of these properties: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UnexpectedMappingStart()
        {
            const string Format = @"A mapping was not expected";
            return Format;
        }

        public static string UnexpectedSequenceStart()
        {
            const string Format = @"A sequence was not expected";
            return Format;
        }

        public static string UnexpectedValue(object arg0)
        {
            const string Format = @"Unexpected value '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ValueAlreadyDefined(object arg0)
        {
            const string Format = @"'{0}' is already defined";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
