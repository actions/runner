using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Functions;

namespace GitHub.DistributedTask.Expressions2
{
    internal static class ExpressionConstants
    {
        static ExpressionConstants()
        {
            AddFunction<Contains>("contains", 2, 2);
            AddFunction<EndsWith>("endsWith", 2, 2);
            AddFunction<Format>("format", 1, Byte.MaxValue);
            AddFunction<Join>("join", 1, 2);
            AddFunction<StartsWith>("startsWith", 2, 2);
            AddFunction<ToJson>("toJson", 1, 1);
            AddFunction<FromJson>("fromJson", 1, 1);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.And>("and", 2, Int32.MaxValue);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Coalesce>("coalesce", 2, Int32.MaxValue);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Contains>("contains", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.ContainsValue>("containsvalue", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.ToJson>("converttojson", 1, 1);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.EndsWith>("endsWith", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Equal>("eq", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Legacy.Format>("format", 1, Int32.MaxValue);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.GreaterThanOrEqual>("ge", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.GreaterThan>("gt", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.In>("in", 1, Int32.MaxValue);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Join>("join", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.LessThanOrEqual>("le", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Length>("length", 1, 1);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Lower>("lower", 1, 1);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.LessThan>("lt", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.NotEqual>("ne", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Not>("not", 1, 1);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.NotIn>("notin", 1, Int32.MaxValue);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Or>("or", 2, Int32.MaxValue);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Replace>("replace", 3, 3);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.StartsWith>("startsWith", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Upper>("upper", 1, 1);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Xor>("xor", 2, 2);
            AddAzureFunction<GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Split>("split", 2, 2);
        }

        private static void AddFunction<T>(String name, Int32 minParameters, Int32 maxParameters)
            where T : Function, new()
        {
            WellKnownFunctions.Add(name, new FunctionInfo<T>(name, minParameters, maxParameters));
        }

        private static void AddAzureFunction<T>(String name, Int32 minParameters, Int32 maxParameters)
            where T : Function, new()
        {
            AzureWellKnownFunctions.Add(name, new FunctionInfo<T>(name, minParameters, maxParameters));
        }

        internal static readonly String False = "false";
        internal static readonly String Infinity = "Infinity";
        internal static readonly Int32 MaxDepth = 50;
        internal static readonly Int32 MaxLength = 21000; // Under 85,000 large object heap threshold, even if .NET switches to UTF-32
        internal static readonly String NaN = "NaN";
        internal static readonly String NegativeInfinity = "-Infinity";
        internal static readonly String Null = "null";
        internal static readonly String NumberFormat = "G15";
        internal static readonly String True = "true";
        internal static readonly Dictionary<String, IFunctionInfo> WellKnownFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);
        internal static readonly Dictionary<String, IFunctionInfo> AzureWellKnownFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);

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
