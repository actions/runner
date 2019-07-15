using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFunctionInfo
    {
        String Name { get; }
        Int32 MinParameters { get; }
        Int32 MaxParameters { get; }
        Function CreateNode();
    }
}
