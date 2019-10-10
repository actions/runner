using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.Logging;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Operators;

namespace GitHub.DistributedTask.Expressions2
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
        /// the function requires the sub-property. Therefore, it is assumed that it may.
        ///
        /// Wildcards are supported in the pattern, and are treated as matching any literal.
        /// For example, the expression "needs.my-job.outputs.my-output" matches the pattern "needs.*.outputs".
        /// </summary>
        public static Boolean[] CheckReferencesContext(
            this IExpressionNode tree,
            params String[] patterns)
        {
            var result = new Boolean[patterns.Length];

            var segmentedPatterns = default(Stack<IExpressionNode>[]);

            // Walk the tree
            var stack = new Stack<IExpressionNode>();
            stack.Push(tree);
            while (stack.Count > 0)
            {
                var node = stack.Pop();

                // Attempt to match a named-value or index operator.
                // Note, do not push children of the index operator.
                if (node is NamedValue || node is Sdk.Operators.Index)
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
                            var patternSegments = GetMatchSegments(patternTree);
                            if (patternSegments.Count == 0)
                            {
                                throw new InvalidOperationException($"Invalid context-match-pattern '{pattern}'");
                            }
                            segmentedPatterns[i] = patternSegments;
                        }
                    }

                    // Match
                    Match(node, segmentedPatterns, result);
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

        private static void Match(
            IExpressionNode node,
            Stack<IExpressionNode>[] patterns,
            Boolean[] result)
        {
            var nodeSegments = GetMatchSegments(node);

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
                    nodeSegments = new Stack<IExpressionNode>(originalNodeSegments.Reverse());
                    nodeSegments.Pop();
                    patternSegments = new Stack<IExpressionNode>(patternSegments.Reverse());
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

        private static Stack<IExpressionNode> GetMatchSegments(IExpressionNode node)
        {
            var result = new Stack<IExpressionNode>();

            // Node is a named-value
            if (node is NamedValue)
            {
                result.Push(node);
            }
            // Node is an index
            else if (node is Sdk.Operators.Index index)
            {
                while (true)
                {
                    // Push parameter 1. Treat anything other than literal as a wildcard.
                    var parameter1 = index.Parameters[1];
                    result.Push(parameter1 is Literal ? parameter1 : new Wildcard());

                    var parameter0 = index.Parameters[0];

                    // Parameter 0 is a named-value
                    if (parameter0 is NamedValue)
                    {
                        result.Push(parameter0);
                        break;
                    }
                    // Parameter 0 is an index
                    else if (parameter0 is Sdk.Operators.Index index2)
                    {
                        index = index2;
                    }
                    // Otherwise clear
                    else
                    {
                        result.Clear();
                        break;
                    }
                }
            }

            return result;
        }
    }
}
