using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IReadOnlyObject : IReadOnlyDictionary<String, Object>
    {
    }
}
