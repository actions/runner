namespace GitHub.Actions.Expressions
{
    internal enum ParseExceptionKind
    {
        ExceededMaxDepth,
        ExceededMaxLength,
        TooFewParameters,
        TooManyParameters,
        EvenParameters,
        UnexpectedEndOfExpression,
        UnexpectedSymbol,
        UnrecognizedFunction,
        UnrecognizedNamedValue,
    }
}