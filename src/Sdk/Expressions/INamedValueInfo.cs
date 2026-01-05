using System;
using GitHub.Actions.Expressions.Sdk;

namespace GitHub.Actions.Expressions
{
    public interface INamedValueInfo
    {
        String Name { get; }
        NamedValue CreateNode();
    }
}