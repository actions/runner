namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System.Collections.Generic;
    using VisualStudio.Services.Common;

    [GenerateAllConstants]
    public class TaskRunsOnConstants
    {
        public const string RunsOnAgent = "Agent";
        public const string RunsOnMachineGroup = "MachineGroup";
        public const string RunsOnDeploymentGroup = "DeploymentGroup";
        public const string RunsOnServer = "Server";
        
        public static readonly List<string> DefaultValue = new List<string> { RunsOnAgent, RunsOnDeploymentGroup };

        public static readonly List<string> RunsOnAllTypes = new List<string>
        {
            RunsOnAgent,
            RunsOnDeploymentGroup,
            RunsOnServer,
        };
    }
}
