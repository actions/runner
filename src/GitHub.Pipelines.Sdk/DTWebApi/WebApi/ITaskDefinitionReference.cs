using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskDefinitionReference
    {
        Guid Id { get; }

        String Name { get; }

        String Version { get; }
    }
}
