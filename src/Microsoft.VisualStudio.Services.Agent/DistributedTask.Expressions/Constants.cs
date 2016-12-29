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
            AddFunction<AndNode>("and", 2, Int32.MaxValue);
            AddFunction<ContainsNode>("contains", 2, 2);
            AddFunction<EndsWithNode>("endsWith", 2, 2);
            AddFunction<EqualNode>("eq", 2, 2);
            AddFunction<GreaterThanNode>("gt", 2, 2);
            AddFunction<GreaterThanOrEqualNode>("ge", 2, 2);
            AddFunction<LessThanNode>("lt", 2, 2);
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

        internal static readonly Dictionary<String, IFunctionInfo> WellKnownFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);
    }
}