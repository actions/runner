namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal enum TokenKind
    {
        // Punctuation
        StartIndex,
        StartParameter,
        EndIndex,
        EndParameter,
        Separator,
        Dereference,
        Wildcard,

        // Values
        Boolean,
        Number,
        Version,
        String,
        PropertyName,

        // Functions and named-values
        WellKnownFunction,
        ExtensionFunction,
        ExtensionNamedValue,
        UnknownKeyword,

        Unrecognized,
    }
}
