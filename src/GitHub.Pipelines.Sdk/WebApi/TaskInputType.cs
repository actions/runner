using CommonContracts = Microsoft.TeamFoundation.DistributedTask.Common.Contracts;
using System.Runtime.Serialization;
using System;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public static class TaskInputType
    {
        public const String String = CommonContracts.TaskInputType.String;
        public const String Repository = CommonContracts.TaskInputType.Repository;
        public const String Boolean = CommonContracts.TaskInputType.Boolean;
        public const String KeyValue = CommonContracts.TaskInputType.KeyValue;
        public const String FilePath = CommonContracts.TaskInputType.FilePath;
    }
}
