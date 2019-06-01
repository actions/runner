using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a contract for resolving environment from a given store.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IEnvironmentStore
    {
        EnvironmentInstance ResolveEnvironment(String environmentName);

        EnvironmentInstance ResolveEnvironment(Int32 environmentId);

        EnvironmentInstance Get(EnvironmentReference reference);

        IList<EnvironmentReference> GetReferences();
    }
}
