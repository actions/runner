using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Partitioning
{
    [GenerateAllConstants]
    public static class PartitioningResourceIds
    {
        public const String AreaName = "Partitioning";
        public const String AreaId = "{0129E64E-3F98-43F8-9073-212C19D832CB}";

        public const String PartitionContainersResource = "Containers";
        public static readonly Guid PartitionContainers = new Guid("{55FDD96F-CBFE-461A-B0AC-890454FF434A}");

        public const String PartitionsResource = "Partitions";
        public static readonly Guid Partitions = new Guid("{4ECE3A4B-1D02-4313-8843-DD7B02C8F639}");
    }

    [DataContract]
    public enum PartitionContainerStatus
    {
        /// <summary>
        /// Online means available to acquire new partitions (assuming capacity exists)
        /// </summary>
        [EnumMember]
        Online = 1,

        /// <summary>
        /// Offline means unable to acquire new partitions even if there is capacity
        /// (but partitions can be manually assigned using CreatePartition)
        /// </summary>
        [EnumMember]
        Offline = 2
    }

    [DataContract]
    public class PartitionContainer
    {
        public PartitionContainer()
        {
            Tags = new List<String>();
        }

        [DataMember]
        public Guid ContainerId { get; set; }

        [DataMember]
        public Guid ContainerType { get; set; }

        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public String Address { get; set; }

        [DataMember]
        public String InternalAddress { get; set; }

        [DataMember]
        public Int32 MaxPartitions { get; set; }

        [DataMember]
        public PartitionContainerStatus Status { get; set; }

        [DataMember]
        public List<String> Tags { get; set; }
    }

    [DataContract]
    public class Partition
    {
        [DataMember]
        public String PartitionKey { get; set; }

        [DataMember]
        public PartitionContainer Container { get; set; }
    }
}
