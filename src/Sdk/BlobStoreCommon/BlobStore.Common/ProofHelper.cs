using BuildXL.Cache.ContentStore.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Services.BlobStore.Common
{
    [CLSCompliant(false)]
    public static class ProofHelper
    {
        public static IReadOnlyDictionary<DedupIdentifier, NodeDedupIdentifier> CreateParentLookup(
            IReadOnlyDictionary<NodeDedupIdentifier, DedupNode> allNodes)
        {
            var parentLookup = new Dictionary<DedupIdentifier, NodeDedupIdentifier>();
            foreach (var node in allNodes)
            {
                foreach (var child in node.Value.ChildNodes)
                {
                    parentLookup[child.GetDedupId()] = node.Key;
                }
            }
            return parentLookup;
        }

        public static HashSet<DedupNode> CreateProofNodes(
            IEnumerable<DedupNode> allNodes,
            IEnumerable<DedupIdentifier> dedupIds)
        {
            var nodeLookup = allNodes.ToDictionary(n => n.CalculateNodeDedupIdentifier(), n => n);
            return CreateProofNodes(
                nodeLookup,
                CreateParentLookup(nodeLookup),
                dedupIds);
        }

        public static HashSet<DedupNode> CreateProofNodes(
            IReadOnlyDictionary<NodeDedupIdentifier, DedupNode> allNodes,
            IReadOnlyDictionary<DedupIdentifier, NodeDedupIdentifier> parentLookup,
            IEnumerable<DedupIdentifier> dedupIds)
        {
            var proofNodes = new HashSet<DedupNode>();
            foreach (var dedupId in dedupIds)
            {
                DedupIdentifier nextId = dedupId;
                NodeDedupIdentifier parentId;

                bool found = false;
                while (parentLookup.TryGetValue(nextId, out parentId))
                {
                    found = true;
                    if (proofNodes.Add(allNodes[parentId]))
                    {
                        nextId = parentId;
                    }
                    else
                    {
                        break;
                    }
                }

                if(!found)
                {
                    proofNodes.Add(allNodes[(NodeDedupIdentifier)dedupId]);
                }
            }

            return proofNodes;
        }

        public static ISet<NodeDedupIdentifier> ApproximatelyOptimalMinCoverage(ISet<DedupNode> proofNodes, IDictionary<DedupIdentifier, ulong> idsToValidateWithSize)
        {
            return ApproximatelyOptimalMinCoverage(proofNodes.ToDictionary(n => n.CalculateNodeDedupIdentifier(), n => n), idsToValidateWithSize);
        }

        public static HashSet<NodeDedupIdentifier> ApproximatelyOptimalMinCoverage(IReadOnlyDictionary<NodeDedupIdentifier, DedupNode> proofNodes, IDictionary<DedupIdentifier, ulong> idsToValidateWithClaimedSize)
        {
            // https://en.wikipedia.org/wiki/Set_cover_problem#Greedy_algorithm

            var coverSets = proofNodes.ToDictionary(
                kvp => kvp.Key,
                _ => new HashSet<DedupIdentifier>());

            var idsStillNeedCoverage = new Dictionary<DedupIdentifier, ulong>(idsToValidateWithClaimedSize);

            var nodesThatCover = idsStillNeedCoverage
                .ToDictionary(
                    idToCover => idToCover.Key,
                    _ => new HashSet<NodeDedupIdentifier>());

            foreach (var coverSet in coverSets)
            {
                FillCoverSet(proofNodes, coverSet.Value, nodesThatCover, idsStillNeedCoverage, coverSet.Key, proofNodes[coverSet.Key]);
            }

            var approximateMinCover = new HashSet<NodeDedupIdentifier>();

            while (idsStillNeedCoverage.Any())
            {
                if (!coverSets.Any())
                {
                    throw new ArgumentException($"ProofNodes do not cover all dedups. (DedupId: {idsStillNeedCoverage.First()})");
                }

                int bestCoverageCount = -1;
                NodeDedupIdentifier bestNodeId = null;
                foreach (var coverSet in coverSets)
                {
                    if(coverSet.Value.Count > bestCoverageCount)
                    {
                        bestCoverageCount = coverSet.Value.Count;
                        bestNodeId = coverSet.Key;
                    }
                }

                approximateMinCover.Add(bestNodeId);
                var bestSet = coverSets[bestNodeId];
                coverSets.Remove(bestNodeId);
                foreach (var newlyCovered in bestSet)
                {
                    idsStillNeedCoverage.Remove(newlyCovered);
                    foreach (var coveringNode in nodesThatCover[newlyCovered].Where(n => n != bestNodeId))
                    {
                        coverSets[coveringNode].Remove(newlyCovered);
                    }
                }
            }

            return approximateMinCover;
        }

        private static void FillCoverSet(
            IReadOnlyDictionary<NodeDedupIdentifier, DedupNode> proofNodes,
            ISet<DedupIdentifier> coverageSet,
            IDictionary<DedupIdentifier, HashSet<NodeDedupIdentifier>> nodesThatCover,
            IDictionary<DedupIdentifier, ulong> idsToCover,
            NodeDedupIdentifier currentNodeId,
            DedupNode currentNode)
        {
            Action<DedupIdentifier, DedupNode> checkAddNode = (id, node) =>
            {
                if(idsToCover.Keys.Contains(id))
                {
                    if (node.TransitiveContentBytes != idsToCover[id])
                    {
                        throw new ArgumentException($"The dedup size is not consistent with ProofNodes. (DedupId: {id})");
                    }

                    coverageSet.Add(id);
                    nodesThatCover[id].Add(currentNodeId);
                }
            };

            if (coverageSet.Count == 0)
            {
                checkAddNode(currentNodeId, currentNode);
            }

            foreach (var child in currentNode.ChildNodes)
            {
                var childId = child.GetDedupId();
                checkAddNode(childId, child);

                var childNodeId = childId as NodeDedupIdentifier;
                if(childNodeId != null)
                {
                    DedupNode childNode;
                    if(proofNodes.TryGetValue(childNodeId, out childNode))
                    {
                        FillCoverSet(proofNodes, coverageSet, nodesThatCover, idsToCover, childNodeId, childNode);
                    }
                }
            }
        }

        public static IEnumerable<DedupIdentifier> DetermineUnvalidatedIds(
            ISet<DedupNode> proofNodes,
            ISet<NodeDedupIdentifier> validatedRoots,
            IDictionary<DedupIdentifier, ulong> idsToValidate
            )
        {
            return DetermineUnvalidatedIds(
                proofNodes.ToDictionary(n => n.CalculateNodeDedupIdentifier(), n => n),
                validatedRoots,
                idsToValidate);
        }

        public static IEnumerable<DedupIdentifier> DetermineUnvalidatedIds(
            IDictionary<NodeDedupIdentifier, DedupNode> proofNodes,
            ISet<NodeDedupIdentifier> roots,
            IDictionary<DedupIdentifier, ulong> idsToValidate
            )
        {
            var visited = new HashSet<DedupIdentifier>();
            var validIds = new Dictionary<DedupIdentifier, ulong>();
            var toVisit = new Queue<NodeDedupIdentifier>(roots);
            while (toVisit.Any())
            {
                var nodeId = toVisit.Dequeue();
                if (!visited.Add(nodeId))
                {
                    continue;
                }

                DedupNode node;
                if (!proofNodes.TryGetValue(nodeId, out node))
                {
                    continue;
                }

                if (!validIds.ContainsKey(nodeId))
                {
                    validIds.Add(nodeId, node.TransitiveContentBytes);
                }

                if (node.ChildNodes == null)
                {
                    // This shouldn't happen.
                    continue;
                }

                foreach (var child in node.ChildNodes)
                {
                    var childId = DedupIdentifier.Create(child);

                    if (!validIds.ContainsKey(childId))
                    {
                        validIds.Add(childId, child.TransitiveContentBytes);
                    }

                    var childNodeId = childId as NodeDedupIdentifier;
                    if (childNodeId != null)
                    {
                        toVisit.Enqueue(childNodeId);
                    }
                }
            }

            foreach (var idToValidate in idsToValidate)
            {
                if (!validIds.ContainsKey(idToValidate.Key))
                {
                    yield return idToValidate.Key;
                }
                // check sizes
                else if (idToValidate.Value != validIds[idToValidate.Key])
                {
                    yield return idToValidate.Key;
                }
            }
        }
    }
}
