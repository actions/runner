using System;
using GitHub.Actions.Expressions.Sdk;

namespace GitHub.Actions.Expressions
{
    public class FunctionInfo<T> : IFunctionInfo
        where T : Function, new()
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

        public Function CreateNode()
        {
            return new T();
        }
    }
}