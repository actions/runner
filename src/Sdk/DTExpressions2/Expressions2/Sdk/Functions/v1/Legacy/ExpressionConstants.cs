using System;
using System.Collections.Generic;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Legacy
{
    internal static class ExpressionConstants
    {
        internal static readonly String DateTimeFormat = @"yyyy\-MM\-dd\ HH\:mm\:sszzz";
        internal static readonly Int32 MaxDepth = 50;
        internal static readonly Int32 MaxLength = 21000; // Under 85,000 large object heap threshold, even if .NET switches to UTF-32
        internal static readonly String NumberFormat = "0.#######";

        // Punctuation
        internal const Char StartIndex = '[';
        internal const Char StartParameter = '(';
        internal const Char EndIndex = ']';
        internal const Char EndParameter = ')';
        internal const Char Separator = ',';
        internal const Char Dereference = '.';
        internal const Char Wildcard = '*';
    }
}
