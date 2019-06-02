using System;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public class EventConstants
    {
        public const String AgentAdded = "MS.TF.DistributedTask.AgentAdded";
        public const String AgentDeleted = "MS.TF.DistributedTask.AgentDeleted";
        public const String AgentRequestAssigned = "MS.TF.DistributedTask.AgentRequestAssigned";
        public const String AgentRequestCompleted = "MS.TF.DistributedTask.AgentRequestCompleted";
        public const String AgentRequestQueued = "MS.TF.DistributedTask.AgentRequestQueued";
        public const String AgentUpdated = "MS.TF.DistributedTask.AgentUpdated";
        public const String AuthorizePipelines = "MS.TF.DistributedTask.AuthorizePipelines";
        public const String DeploymentFailed = "MS.TF.DistributedTask.DeploymentFailed";
        public const String DeploymentGatesChanged = "MS.TF.DistributedTask.DeploymentGatesChanged";
        public const String DeploymentMachinesChanged = "MS.TF.DistributedTask.DeploymentMachinesChanged";
        public const String PoolCreated = "MS.TF.DistributedTask.AgentPoolCreated";
        public const String PoolDeleted = "MS.TF.DistributedTask.AgentPoolDeleted";
        public const String QueueCreated = "MS.TF.DistributedTask.AgentQueueCreated";
        public const String QueueDeleted = "MS.TF.DistributedTask.AgentQueueDeleted";
        public const String QueuesDeleted = "MS.TF.DistributedTask.AgentQueuesDeleted";
        public const String TasksChanged = "MS.TF.DistributedTask.TasksChanged";
        public const String Version = "2.0";
    }
}
