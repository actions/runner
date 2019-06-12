using System;

namespace GitHub.DistributedTask.WebApi
{
    public class TaskAgentProvisioningStateConstants
    {
        public const String Deallocated = "Deallocated";
        public const String Provisioning = "Provisioning";
        public const String Provisioned = "Provisioned";
        public const String Deprovisioning = "Deprovisioning";
        public const String RunningRequest = "RunningRequest";
    }
}
