using System;
using System.Collections.Generic;

namespace GitHub.DistributedTask.Pipelines
{
    internal sealed class ResourceComparer : IEqualityComparer<Resource>
    {
        public Boolean Equals(
            Resource x, 
            Resource y)
        {
            return String.Equals(x?.Alias, y?.Alias, StringComparison.OrdinalIgnoreCase);
        }

        public Int32 GetHashCode(Resource obj)
        {
            return obj?.Alias?.GetHashCode() ?? 0;
        }
    }
}
