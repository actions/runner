namespace GitHub.DistributedTask.Expressions
{
    internal enum ParseExceptionKind
    {
        ExceededMaxDepth,
        ExceededMaxLength,
        ExpectedPropertyName,
        ExpectedStartParameter,
        UnclosedFunction,
        UnclosedIndexer,
        UnexpectedSymbol,
        UnrecognizedValue,
    }
}
