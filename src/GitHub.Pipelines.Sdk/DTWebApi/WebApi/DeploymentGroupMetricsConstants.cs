using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public class DeploymentGroupMetricsValidTargetState
    {
        public const String Online = "Online";
        public const String Offline = "Offline";
    }

    [GenerateAllConstants]
    public class DeploymentGroupMetricsValidJobStatus
    {
        public const String Succeeded = "Succeeded";
        public const String NotSucceeded = "Not succeeded";
        public const String NotDeployed = "Not deployed";
    }

    [GenerateAllConstants]
    public class DeploymentGroupMetricsValidColumnNames
    {
        public const String DeploymentTargetState = "DeploymentTargetState";
        public const String LastDeploymentStatus = "LastDeploymentStatus";
        public const String TotalDeploymentTargetCount = "TotalDeploymentTargetCount";
    }

    [GenerateAllConstants]
    public class DeploymentGroupMetricsValidColumnValueTypes
    {
        public const String Number = "number";
        public const String String = "string";
    }
}
