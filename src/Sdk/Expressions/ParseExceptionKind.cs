namespace GitHub.Actions.Expressions
{
    internal enum ParseExceptionKind
    {
        ExceededMaxDepth,
        ExceededMaxLength,
        TooFewParameters,
        TooManyParameters,
        UnexpectedEndOfExpression,
        UnexpectedSymbol,
        UnrecognizedFunction,
        UnrecognizedNamedValue,
    }
}