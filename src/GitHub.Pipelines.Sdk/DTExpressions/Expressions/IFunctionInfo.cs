using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    public interface IFunctionInfo
    {
        String Name { get; }
        Int32 MinParameters { get; }
        Int32 MaxParameters { get; }
        FunctionNode CreateNode();
    }
}