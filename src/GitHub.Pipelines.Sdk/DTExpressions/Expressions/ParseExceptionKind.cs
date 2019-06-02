namespace Microsoft.TeamFoundation.DistributedTask.Expressions
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