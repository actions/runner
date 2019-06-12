using System;

namespace GitHub.DistributedTask.Expressions
{
    public interface INamedValueInfo
    {
        String Name { get; }
        NamedValueNode CreateNode();
    }
}
