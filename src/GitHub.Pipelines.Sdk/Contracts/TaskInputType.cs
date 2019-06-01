using System.Runtime.Serialization;
using System;

namespace Microsoft.TeamFoundation.DistributedTask.Common.Contracts
{
    public static class TaskInputType
    {
        public const String String = "string";
        public const String Repository = "repository";
        public const String Boolean = "boolean";
        public const String KeyValue = "keyvalue";
        public const String FilePath = "filepath";
    }
}
