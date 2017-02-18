// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Expressions
// Repo 2) vsts-agent repo on GitHub under src/Microsoft.VisualStudio.Services.Agent/DistributedTask.Expressions
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions
{
    internal static class ExpressionConstants
    {
        static ExpressionConstants()
        {
            AddFunction<AndNode>(And, 2, Int32.MaxValue);
            AddFunction<ContainsNode>(Contains, 2, 2);
            AddFunction<EndsWithNode>(EndsWith, 2, 2);
            AddFunction<EqualNode>(Eq, 2, 2);
            AddFunction<GreaterThanNode>(GT, 2, 2);
            AddFunction<GreaterThanOrEqualNode>(GE, 2, 2);
            AddFunction<LessThanNode>(LT, 2, 2);
            AddFunction<LessThanOrEqualNode>(LE, 2, 2);
            AddFunction<InNode>(In, 2, Int32.MaxValue);
            AddFunction<NotNode>(Not, 1, 1);
            AddFunction<NotEqualNode>(NE, 2, 2);
            AddFunction<NotInNode>(NotIn, 2, Int32.MaxValue);
            AddFunction<OrNode>(Or, 2, Int32.MaxValue);
            AddFunction<StartsWithNode>(StartsWith, 2, 2);
            AddFunction<XorNode>(Xor, 2, 2);
        }

        private static void AddFunction<T>(String name, Int32 minParameters, Int32 maxParameters)
            where T : FunctionNode, new()
        {
            WellKnownFunctions.Add(name, new FunctionInfo<T>(name, minParameters, maxParameters));
        }

        internal static readonly String And = "and";
        internal static readonly String Contains = "contains";
        internal static readonly String EndsWith = "endsWith";
        internal static readonly String Eq = "eq";
        internal static readonly String GE = "ge";
        internal static readonly String GT = "gt";
        internal static readonly String LT = "lt";
        internal static readonly String LE = "le";
        internal static readonly String In = "in";
        internal static readonly String Not = "not";
        internal static readonly String NE = "ne";
        internal static readonly String NotIn = "notIn";
        internal static readonly String Or = "or";
        internal static readonly String StartsWith = "startsWith";
        internal static readonly Dictionary<String, IFunctionInfo> WellKnownFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);
        internal static readonly String Xor = "xor";
    }
}