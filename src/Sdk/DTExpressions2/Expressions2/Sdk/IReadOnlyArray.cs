using System;
using System.Collections;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IReadOnlyArray
    {
        Int32 Count { get; }

        Object this[Int32 index] { get; }

        IEnumerator GetEnumerator();
    }
}
