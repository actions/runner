using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation
{
    internal static class GraphValidator
    {
        internal delegate String ErrorFormatter(String code, params Object[] values);

        internal static void Validate<T>(
            PipelineBuildContext context,
            ValidationResult result,
            Func<Object, String> getBaseRefName,
            String graphName,
            IList<T> nodes,
            ErrorFormatter formatError) where T : class, IGraphNode
        {
            var unnamedNodes = new List<T>();
            var startingNodes = new List<T>();
            var knownNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            Boolean hasDuplicateName = false;
            foreach (var node in nodes)
            {
                if (!String.IsNullOrEmpty(node.Name))
                {
                    if (!NameValidation.IsValid(node.Name, context.BuildOptions.AllowHyphenNames))
                    {
                        result.Errors.Add(new PipelineValidationError(PipelineConstants.NameInvalid, formatError(PipelineConstants.NameInvalid, graphName, node.Name)));
                    }
                    else if (!knownNames.Add(node.Name))
                    {
                        hasDuplicateName = true;
                        result.Errors.Add(new PipelineValidationError(PipelineConstants.NameNotUnique, formatError(PipelineConstants.NameNotUnique, graphName, node.Name)));
                    }
                }
                else
                {
                    unnamedNodes.Add(node);
                }

                if (node.DependsOn.Count == 0)
                {
                    startingNodes.Add(node);
                }
            }

            Int32 nodeCounter = 1;
            foreach (var unnamedNode in unnamedNodes)
            {
                var candidateName = getBaseRefName(nodeCounter);
                while (!knownNames.Add(candidateName))
                {
                    nodeCounter++;
                    candidateName = getBaseRefName(nodeCounter);
                }

                nodeCounter++;
                unnamedNode.Name = candidateName;
            }

            // Now that we have generated default names we can validate and provide error messages
            foreach (var node in nodes)
            {
                node.Validate(context, result);
            }

            if (startingNodes.Count == 0)
            {
                result.Errors.Add(new PipelineValidationError(PipelineConstants.StartingPointNotFound, formatError(PipelineConstants.StartingPointNotFound, graphName)));
                return;
            }

            // Skip validating the graph if duplicate phase names
            if (hasDuplicateName)
            {
                return;
            }

            var nodesToVisit = new Queue<T>(startingNodes);
            var nodeLookup = nodes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var unsatisfiedDependencies = nodes.ToDictionary(x => x.Name, x => new List<String>(x.DependsOn), StringComparer.OrdinalIgnoreCase);
            var visitedNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            while (nodesToVisit.Count > 0)
            {
                var currentPhase = nodesToVisit.Dequeue();

                visitedNames.Add(currentPhase.Name);

                // Now figure out which nodes would start as a result of this 
                foreach (var nodeState in unsatisfiedDependencies)
                {
                    for (Int32 i = nodeState.Value.Count - 1; i >= 0; i--)
                    {
                        if (nodeState.Value[i].Equals(currentPhase.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            nodeState.Value.RemoveAt(i);
                            if (nodeState.Value.Count == 0)
                            {
                                nodesToVisit.Enqueue(nodeLookup[nodeState.Key]);
                            }
                        }
                    }
                }
            }

            // There are nodes which are never going to execute, which is generally caused by a cycle in the graph.
            var unreachableNodeCount = nodes.Count - visitedNames.Count;
            if (unreachableNodeCount > 0)
            {
                foreach (var unreachableNode in unsatisfiedDependencies.Where(x => x.Value.Count > 0))
                {
                    foreach (var unsatisifedDependency in unreachableNode.Value)
                    {
                        if (!nodeLookup.ContainsKey(unsatisifedDependency))
                        {
                            result.Errors.Add(new PipelineValidationError(PipelineConstants.DependencyNotFound, formatError(PipelineConstants.DependencyNotFound, graphName, unreachableNode.Key, unsatisifedDependency)));
                        }
                        else
                        {
                            result.Errors.Add(new PipelineValidationError(PipelineConstants.GraphContainsCycle, formatError(PipelineConstants.GraphContainsCycle, graphName, unreachableNode.Key, unsatisifedDependency)));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Traverses a validated graph running a callback on each node in the order it would execute at runtime.
        /// </summary>
        /// <typeparam name="T">The type of graph node</typeparam>
        /// <param name="nodes">The full set of nodes in the graph</param>
        /// <param name="handleNode">A callback which is invoked for each node as execution would begin</param>
        internal static void Traverse<T>(
            IList<T> nodes, 
            Action<T, ISet<String>> handleNode) where T : class, IGraphNode
        {
            var nodeLookup = nodes.ToDictionary(x => x.Name, x => new GraphTraversalState<T>(x), StringComparer.OrdinalIgnoreCase);
            var pendingNodes = nodes.ToDictionary(x => x.Name, x => new List<String>(x.DependsOn), StringComparer.OrdinalIgnoreCase);
            var nodesToVisit = new Queue<GraphTraversalState<T>>(nodes.Where(x => x.DependsOn.Count == 0).Select(x => new GraphTraversalState<T>(x)));
            while (nodesToVisit.Count > 0)
            {
                var currentNode = nodesToVisit.Dequeue();

                // Invoke the callback on this node since it would execute next. The dependencies provided to the 
                // callback is a fully recursive set of all dependencies for context on how a node would execute
                // at runtime.
                handleNode(currentNode.Node, currentNode.Dependencies);

                // Now figure out which nodes would start as a result of this 
                foreach (var nodeState in pendingNodes)
                {
                    for (Int32 i = nodeState.Value.Count - 1; i >= 0; i--)
                    {
                        if (nodeState.Value[i].Equals(currentNode.Node.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            nodeState.Value.RemoveAt(i);

                            // Make sure we include the completed nodes recursive dependency set into the dependent
                            // node recursive dependency set for accurate hit detection.
                            var traversalState = nodeLookup[nodeState.Key];
                            traversalState.Dependencies.Add(currentNode.Node.Name);
                            traversalState.Dependencies.UnionWith(currentNode.Dependencies);

                            if (nodeState.Value.Count == 0)
                            {
                                nodesToVisit.Enqueue(traversalState);
                            }
                        }
                    }
                }
            }
        }

        private class GraphTraversalState<T> where T : class, IGraphNode
        {
            public GraphTraversalState(T node)
            {
                this.Node = node;
            }

            public T Node { get; }
            public ISet<String> Dependencies { get; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
