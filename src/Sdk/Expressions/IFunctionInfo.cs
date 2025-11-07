using System;
using GitHub.Actions.Expressions.Sdk;

namespace GitHub.Actions.Expressions
{
    public interface IFunctionInfo
    {
        String Name { get; }
        Int32 MinParameters { get; }
        Int32 MaxParameters { get; }
        Function CreateNode();
    }
}