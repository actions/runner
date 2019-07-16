using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class NamedValueInfo<T> : INamedValueInfo
        where T : NamedValue, new()
    {
        public NamedValueInfo(String name)
        {
            Name = name;
        }

        public String Name { get; }

        public NamedValue CreateNode()
        {
            return new T();
        }
    }
}
