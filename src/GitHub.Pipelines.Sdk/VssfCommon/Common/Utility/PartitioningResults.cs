using System.Collections.Generic;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Contains results from two-way variant of EnuemrableExtensions.Partition()
    /// </summary>
    /// <typeparam name="T">The type of the elements in the contained lists.</typeparam>
    public sealed class PartitionResults<T>
    {
        public List<T> MatchingPartition { get; } = new List<T>();

        public List<T> NonMatchingPartition { get; } = new List<T>();
    }

    /// <summary>
    /// Contains results from multi-partitioning variant of EnuemrableExtensions.Partition()
    /// </summary>
    /// <typeparam name="T">The type of the elements in the contained lists.</typeparam>
    public sealed class MultiPartitionResults<T>
    {
        public List<List<T>> MatchingPartitions { get; } = new List<List<T>>();

        public List<T> NonMatchingPartition { get; } = new List<T>();
    }
}
