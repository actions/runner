using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal static class ExpressionConstants
    {
        static ExpressionConstants()
        {
            AddFunction<AndNode>("and", 2, Int32.MaxValue);
            AddFunction<CoalesceNode>("coalesce", 2, Int32.MaxValue);
            AddFunction<ContainsNode>("contains", 2, 2);
            AddFunction<ContainsValueNode>("containsValue", 2, 2);
            AddFunction<EndsWithNode>("endsWith", 2, 2);
            AddFunction<EqualNode>("eq", 2, 2);
            AddFunction<FormatNode>("format", 1, Byte.MaxValue);
            AddFunction<GreaterThanNode>("gt", 2, 2);
            AddFunction<GreaterThanOrEqualNode>("ge", 2, 2);
            AddFunction<LessThanNode>("lt", 2, 2);
            AddFunction<JoinNode>("join", 2, 2);
            AddFunction<LessThanOrEqualNode>("le", 2, 2);
            AddFunction<InNode>("in", 2, Int32.MaxValue);
            AddFunction<NotNode>("not", 1, 1);
            AddFunction<NotEqualNode>("ne", 2, 2);
            AddFunction<NotInNode>("notIn", 2, Int32.MaxValue);
            AddFunction<OrNode>("or", 2, Int32.MaxValue);
            AddFunction<StartsWithNode>("startsWith", 2, 2);
            AddFunction<XorNode>("xor", 2, 2);
        }

        private static void AddFunction<T>(String name, Int32 minParameters, Int32 maxParameters)
            where T : FunctionNode, new()
        {
            WellKnownFunctions.Add(name, new FunctionInfo<T>(name, minParameters, maxParameters));
        }

        internal static readonly String DateTimeFormat = @"yyyy\-MM\-dd\ HH\:mm\:sszzz";
        internal static readonly Int32 MaxDepth = 50;
        internal static readonly Int32 MaxLength = 21000; // Under 85,000 large object heap threshold, even if .NET switches to UTF-32
        internal static readonly String NumberFormat = "0.#######";
        internal static readonly Dictionary<String, IFunctionInfo> WellKnownFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);

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
