namespace GitHub.DistributedTask.Expressions2
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
