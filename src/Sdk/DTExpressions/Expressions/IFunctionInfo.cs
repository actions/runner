using System;

namespace GitHub.DistributedTask.Expressions
{
    public interface IFunctionInfo
    {
        String Name { get; }
        Int32 MinParameters { get; }
        Int32 MaxParameters { get; }
        FunctionNode CreateNode();
    }
}
