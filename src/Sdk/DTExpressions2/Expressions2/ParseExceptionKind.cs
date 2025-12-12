namespace GitHub.DistributedTask.Expressions2
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
