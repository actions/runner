using System;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IEnvironmentResolver
    {
        EnvironmentInstance Resolve(String environmentName);

        EnvironmentInstance Resolve(Int32 environmentId);
    }
}
