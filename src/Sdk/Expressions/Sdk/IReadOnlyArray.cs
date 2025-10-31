using System;
using System.Collections;

namespace GitHub.Actions.Expressions.Sdk
{
    public interface IReadOnlyArray
    {
        Int32 Count { get; }

        Object this[Int32 index] { get; }

        IEnumerator GetEnumerator();
    }
}
