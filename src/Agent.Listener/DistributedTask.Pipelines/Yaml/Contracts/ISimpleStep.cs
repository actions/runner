namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal interface ISimpleStep : IStep
    {
        ISimpleStep Clone();
    }
}
