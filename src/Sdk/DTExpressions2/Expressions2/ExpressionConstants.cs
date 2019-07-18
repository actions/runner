using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.LegacyFunctions;
using GitHub.DistributedTask.Expressions2.Sdk.Functions;

namespace GitHub.DistributedTask.Expressions2
{
    internal static class ExpressionConstants
    {
        static ExpressionConstants()
        {
            AddFunction<Contains>("contains", 2, 2);
            AddFunction<ConvertToJson>("convertToJson", 1, 1);
            AddFunction<EndsWith>("endsWith", 2, 2);
            AddFunction<Format>("format", 1, Byte.MaxValue);
            AddFunction<Join>("join", 1, 2);
            AddFunction<StartsWith>("startsWith", 2, 2);
            AddFunction<LegacyAnd>("and", 2, Int32.MaxValue);
            AddFunction<LegacyEqual>("eq", 2, 2);
            AddFunction<LegacyGreaterThan>("gt", 2, 2);
            AddFunction<LegacyGreaterThanOrEqual>("ge", 2, 2);
            AddFunction<LegacyLessThan>("lt", 2, 2);
            AddFunction<LegacyLessThanOrEqual>("le", 2, 2);
            AddFunction<LegacyNot>("not", 1, 1);
            AddFunction<LegacyNotEqual>("ne", 2, 2);
            AddFunction<LegacyOr>("or", 2, Int32.MaxValue);
        }

        private static void AddFunction<T>(String name, Int32 minParameters, Int32 maxParameters)
            where T : Function, new()
        {
            WellKnownFunctions.Add(name, new FunctionInfo<T>(name, minParameters, maxParameters));
        }

        internal static readonly String False = "false";
        internal static readonly String Infinity = "Infinity";
        internal static readonly Int32 MaxDepth = 50;
        internal static readonly Int32 MaxLength = 21000; // Under 85,000 large object heap threshold, even if .NET switches to UTF-32
        internal static readonly String NaN = "NaN";
        internal static readonly String NegativeInfinity = "-Infinity";
        internal static readonly String Null = "null";
        internal static readonly String NumberFormat = "G17";
        internal static readonly String True = "true";
        internal static readonly Dictionary<String, IFunctionInfo> WellKnownFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);

        // Punctuation
        internal const Char StartGroup = '(';       // logical grouping
        internal const Char StartIndex = '[';
        internal const Char StartParameter = '(';   // function call
        internal const Char EndGroup = ')';         // logical grouping
        internal const Char EndIndex = ']';
        internal const Char EndParameter = ')';     // function calll
        internal const Char Separator = ',';
        internal const Char Dereference = '.';
        internal const Char Wildcard = '*';

        // Operators
        internal const String Not = "!";
        internal const String NotEqual = "!=";
        internal const String GreaterThan = ">";
        internal const String GreaterThanOrEqual = ">=";
        internal const String LessThan = "<";
        internal const String LessThanOrEqual = "<=";
        internal const String Equal = "==";
        internal const String And = "&&";
        internal const String Or = "||";
    }
}
