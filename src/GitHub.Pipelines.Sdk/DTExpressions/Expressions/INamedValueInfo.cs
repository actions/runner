using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    public interface INamedValueInfo
    {
        String Name { get; }
        NamedValueNode CreateNode();
    }
}