using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskDefinitionReference
    {
        Guid Id { get; }

        String Name { get; }

        String Version { get; }
    }
}
