using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.Expressions2.Sdk;

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
    }
}