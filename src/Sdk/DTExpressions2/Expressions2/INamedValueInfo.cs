using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INamedValueInfo
    {
        String Name { get; }
        NamedValue CreateNode();
    }
}
