using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IEnvironmentResolver
    {
        EnvironmentInstance Resolve(String environmentName);

        EnvironmentInstance Resolve(Int32 environmentId);
    }
}
