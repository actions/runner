using System;

namespace GitHub.Actions.Expressions.Tokens
{
    internal enum TokenKind
    {
        // Punctuation
        StartGroup,         // "(" logical grouping
        StartIndex,         // "["
        StartParameters,    // "(" function call
        EndGroup,           // ")" logical grouping
        EndIndex,           // "]"
        EndParameters,      // ")" function call
        Separator,          // ","
        Dereference,        // "."
        Wildcard,           // "*"
        LogicalOperator,    // "!", "==", etc

        // Values
        Null,
        Boolean,
        Number,
        String,
        PropertyName,
        Function,
        NamedValue,

        Unexpected,
    }
}
