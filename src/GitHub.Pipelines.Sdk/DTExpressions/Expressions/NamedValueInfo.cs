using System;

namespace GitHub.DistributedTask.Expressions
{
    public class NamedValueInfo<T> : INamedValueInfo
        where T : NamedValueNode, new()
    {
        public NamedValueInfo(String name)
        {
            Name = name;
        }

        public String Name { get; }

        public NamedValueNode CreateNode()
        {
            return new T();
        }
    }
}
