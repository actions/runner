using System;
using System.Collections;
using System.Collections.Generic;

namespace GitHub.Actions.Expressions.Sdk
{
    public interface IReadOnlyObject
    {
        Int32 Count { get; }

        IEnumerable<String> Keys { get; }

        IEnumerable<Object> Values { get; }

        Object this[String key] { get; }

        Boolean ContainsKey(String key);

        IEnumerator GetEnumerator();

        Boolean TryGetValue(
            String key,
            out Object value);
    }
}
