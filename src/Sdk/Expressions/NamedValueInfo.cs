using System;
using GitHub.Actions.Expressions.Sdk;

namespace GitHub.Actions.Expressions
{
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