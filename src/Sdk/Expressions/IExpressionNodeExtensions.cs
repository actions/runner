#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Actions.Expressions.Sdk;
using Index = GitHub.Actions.Expressions.Sdk.Operators.Index;

namespace GitHub.Actions.Expressions
{
    public static class IExpressionNodeExtensions
    {
        /// <summary>
        /// Returns the node and all descendant nodes
        /// </summary>
        public static IEnumerable<IExpressionNode> Traverse(this IExpressionNode node)
        {
            yield return node;

            if (node is Container container && container.Parameters.Count > 0)
            {
                foreach (var parameter in container.Parameters)
                {
                    foreach (var descendant in parameter.Traverse())
                    {
                        yield return descendant;
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether specific contexts or sub-properties of contexts are referenced.
        /// If a conclusive determination cannot be made, then the pattern is considered matched.
        /// For example, the expression "toJson(github)" matches the pattern "github.event" because
        /// the value is passed to a function. Not enough information is known to determine whether
        /// the function requires the sub-property. Therefore, assume it is required.
        ///
        /// Patterns may contain wildcards to match any literal. For example, the pattern
        /// "needs.*.outputs" will produce a match for the expression "needs.my-job.outputs.my-output".
        /// </summary>
        public static Boolean[] CheckReferencesContext(
            this IExpressionNode tree,
            params String[] patterns)
        {
            // The result is an array of booleans, one per pattern
            var result = new Boolean[patterns.Length];

            // Stores the match segments for each pattern. For example
            // the patterns [ "github.event", "needs.*.outputs" ] would
            // be stored as:
            //   [
            //     [
            //       NamedValue:github
            //       Literal:"event"
            //     ],
            //     [
            //       NamedValue:needs
            //       Wildcard:*
            //       Literal:"outputs"
            //     ]
            //   ]
            var segmentedPatterns = default(Stack<IExpressionNode>[]);

            // Walk the expression tree
            var stack = new Stack<IExpressionNode>();
            stack.Push(tree);
            while (stack.Count > 0)
            {
                var node = stack.Pop();

                // Attempt to match a named-value or index operator.
                // Note, when entering this block, descendant nodes are only pushed
                // to the stack for further processing under special conditions.
                if (node is NamedValue || node is Index)
                {
                    // Lazy initialize the pattern segments
                    if (segmentedPatterns is null)
                    {
                        segmentedPatterns = new Stack<IExpressionNode>[patterns.Length];
                        var parser = new ExpressionParser();
                        for (var i = 0; i < patterns.Length; i++)
                        {
                            var pattern = patterns[i];
                            var patternTree = parser.ValidateSyntax(pattern, null);
                            var patternSegments = GetMatchSegments(patternTree, out _);
                            if (patternSegments.Count == 0)
                            {
                                throw new InvalidOperationException($"Invalid context-match-pattern '{pattern}'");
                            }
                            segmentedPatterns[i] = patternSegments;
                        }
                    }

                    // Match
                    Match(node, segmentedPatterns, result, out var needsFurtherAnalysis);

                    // Push nested nodes that need further analysis
                    if (needsFurtherAnalysis?.Count > 0)
                    {
                        foreach (var nestedNode in needsFurtherAnalysis)
                        {
                            stack.Push(nestedNode);
                        }
                    }
                }
                // Push children of any other container node.
                else if (node is Container container && container.Parameters.Count > 0)
                {
                    foreach (var child in container.Parameters)
                    {
                        stack.Push(child);
                    }
                }
            }

            return result;
        }

        // Attempts to match a node within a user-provided-expression against a set of patterns.
        //
        // For example consider the user-provided-expression "github.event.base_ref || github.event.before"
        // The Match method would be called twice, once for the sub-expression "github.event.base_ref" and
        // once for the sub-expression "github.event.before".
        private static void Match(
            IExpressionNode node,
            Stack<IExpressionNode>[] patterns,
            Boolean[] result,
            out List<ExpressionNode> needsFurtherAnalysis)
        {
            var nodeSegments = GetMatchSegments(node, out needsFurtherAnalysis);

            if (nodeSegments.Count == 0)
            {
                return;
            }

            var nodeNamedValue = nodeSegments.Peek() as NamedValue;
            var originalNodeSegments = nodeSegments;

            for (var i = 0; i < patterns.Length; i++)
            {
                var patternSegments = patterns[i];
                var patternNamedValue = patternSegments.Peek() as NamedValue;

                // Compare the named-value
                if (String.Equals(nodeNamedValue.Name, patternNamedValue.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // Clone the stacks before mutating
                    nodeSegments = new Stack<IExpressionNode>(originalNodeSegments.Reverse()); // Push reverse to preserve order
                    nodeSegments.Pop();
                    patternSegments = new Stack<IExpressionNode>(patternSegments.Reverse()); // Push reverse to preserve order
                    patternSegments.Pop();

                    // Walk the stacks
                    while (true)
                    {
                        // Every pattern segment was matched
                        if (patternSegments.Count == 0)
                        {
                            result[i] = true;
                            break;
                        }
                        // Every node segment was matched. Treat the pattern as matched. There is not
                        // enough information to determine whether the property is required; assume it is.
                        // For example, consider the pattern "github.event" and the expression "toJson(github)".
                        // In this example the function requires the full structure of the named-value.
                        else if (nodeSegments.Count == 0)
                        {
                            result[i] = true;
                            break;
                        }

                        var nodeSegment = nodeSegments.Pop();
                        var patternSegment = patternSegments.Pop();

                        // The behavior of a wildcard varies depending on whether the left operand
                        // is an array or an object. For simplicity, treat the pattern as matched.
                        if (nodeSegment is Wildcard)
                        {
                            result[i] = true;
                            break;
                        }
                        // Treat a wildcard pattern segment as matching any literal segment
                        else if (patternSegment is Wildcard)
                        {
                            continue;
                        }

                        // Convert literals to string and compare
                        var nodeLiteral = nodeSegment as Literal;
                        var nodeEvaluationResult = EvaluationResult.CreateIntermediateResult(null, nodeLiteral.Value);
                        var nodeString = nodeEvaluationResult.ConvertToString();
                        var patternLiteral = patternSegment as Literal;
                        var patternEvaluationResult = EvaluationResult.CreateIntermediateResult(null, patternLiteral.Value);
                        var patternString = patternEvaluationResult.ConvertToString();
                        if (String.Equals(nodeString, patternString, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Convert to number and compare
                        var nodeNumber = nodeEvaluationResult.ConvertToNumber();
                        if (!Double.IsNaN(nodeNumber) && nodeNumber >= 0d && nodeNumber <= (Double)Int32.MaxValue)
                        {
                            var patternNumber = patternEvaluationResult.ConvertToNumber();
                            if (!Double.IsNaN(patternNumber) && patternNumber >= 0 && patternNumber <= (Double)Int32.MaxValue)
                            {
                                nodeNumber = Math.Floor(nodeNumber);
                                patternNumber = Math.Floor(patternNumber);
                                if (nodeNumber == patternNumber)
                                {
                                    continue;
                                }
                            }
                        }

                        // Not matched
                        break;
                    }
                }
            }
        }

        // This function is used to convert a pattern or a user-provided-expression into a
        // consistent structure for easy comparison. The result is a stack containing only
        // nodes of type NamedValue, Literal, or Wildcard. All Index nodes are discarded.
        //
        // For example, consider the pattern "needs.*.outputs". The expression tree looks like:
        //   Index(
        //     Index(
        //        NamedValue:needs,
        //        Wildcard:*
        //     ),
        //     Literal:"outputs"
        //   )
        // The result would be:
        //   [
        //     NamedValue:needs
        //     Wildcard:*
        //     Literal:"outputs"
        //   ]
        //
        // Any nested expression trees that require further analysis, are returned separately.
        // For example, consider the expression "needs.build.outputs[github.event.base_ref]"
        // The result would be:
        //   [
        //     NamedValue:needs
        //     Literal:"build"
        //     Literal:"outputs"
        //   ]
        // And the nested expression tree "github.event.base_ref" would be tracked as needing
        // further analysis.
        private static Stack<IExpressionNode> GetMatchSegments(
            IExpressionNode node,
            out List<ExpressionNode> needsFurtherAnalysis)
        {
            var result = new Stack<IExpressionNode>();
            needsFurtherAnalysis = new List<ExpressionNode>();

            // Node is a named-value
            if (node is NamedValue)
            {
                result.Push(node);
            }
            // Node is an index
            else if (node is Index index)
            {
                while (true)
                {
                    //
                    // Parameter 1
                    //
                    var parameter1 = index.Parameters[1];

                    // Treat anything other than literal as a wildcard
                    result.Push(parameter1 is Literal ? parameter1 : new Wildcard());

                    // Further analysis required by the caller if parameter 1 is a Function/Operator/NamedValue
                    if (parameter1 is Container || parameter1 is NamedValue)
                    {
                        needsFurtherAnalysis.Add(parameter1);
                    }

                    //
                    // Parameter 0
                    //
                    var parameter0 = index.Parameters[0];

                    // Parameter 0 is a named-value
                    if (parameter0 is NamedValue)
                    {
                        result.Push(parameter0);
                        break;
                    }
                    // Parameter 0 is an index
                    else if (parameter0 is Index index2)
                    {
                        index = index2;
                    }
                    // Otherwise clear
                    else
                    {
                        result.Clear();

                        // Further analysis required by the caller if parameter 0 is a Function/Operator
                        if (parameter0 is Container)
                        {
                            needsFurtherAnalysis.Add(parameter0);
                        }

                        break;
                    }
                }
            }

            return result;
        }
    }
}
