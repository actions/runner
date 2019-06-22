using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public interface IAsyncEnumerator<out T> : IDisposable
    {
        T Current { get; }

        bool EnumerationStarted { get; }

        Task<bool> MoveNextAsync(CancellationToken token);
    }

    /// <summary>
    /// A cursor is a compact representation of the already enumerated
    /// values at any given time during the enumeration.
    /// 
    /// The Cursor property must be defined whenever MoveNext would return true.
    /// </summary>
    /// <typeparam name="T">The type of the enumerated values</typeparam>
    /// <typeparam name="TCursor">The type of the cursor</typeparam>
    public interface IAsyncEnumeratorWithCursor<out T, TCursor> : IAsyncEnumerator<T>
    {
        /// <summary>
        /// Always when MoveNext would return true, this function returns a value.
        /// </summary>
        TCursor Cursor { get; }
    }
}
