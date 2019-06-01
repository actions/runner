using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    public class FunctionInfo<T> : IFunctionInfo
        where T : FunctionNode, new()
    {
        public FunctionInfo(String name, Int32 minParameters, Int32 maxParameters)
        {
            Name = name;
            MinParameters = minParameters;
            MaxParameters = maxParameters;
        }

        public String Name { get; }

        public Int32 MinParameters { get; }

        public Int32 MaxParameters { get; }

        public FunctionNode CreateNode()
        {
            return new T();
        }
    }
}