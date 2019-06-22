using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public class AsyncEnumeratorSelectMany<T1, T2> : IAsyncEnumerator<T2>
    {
        private readonly IAsyncEnumerator<T1> baseEnumerator;
        private IAsyncEnumerator<T2> currentEnumerator;
        private readonly Func<T1, IAsyncEnumerator<T2>> selector;

        public AsyncEnumeratorSelectMany(IAsyncEnumerator<T1> baseEnumerator, Func<T1, IAsyncEnumerator<T2>> selector)
        {
            baseEnumerator.AssertNotEnumerated();
            this.baseEnumerator = baseEnumerator;
            this.selector = selector;
        }

        public T2 Current => currentEnumerator.Current;

        public bool EnumerationStarted => this.baseEnumerator.EnumerationStarted;

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            if (!EnumerationStarted || !await currentEnumerator.MoveNextAsync(cancellationToken))
            {
                do
                {
                    if (await baseEnumerator.MoveNextAsync(cancellationToken).ConfigureAwait(true))
                    {
                        currentEnumerator = selector(baseEnumerator.Current);
                    }
                    else
                    {
                        return false;
                    }
                }
                while (!await currentEnumerator.MoveNextAsync(cancellationToken));
            }
            return true;
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                baseEnumerator.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    public class AsyncEnumeratorSelectManyAsync<T1, T2> : IAsyncEnumerator<T2>
    {
        private readonly IAsyncEnumerator<T1> baseEnumerator;
        private IAsyncEnumerator<T2> currentEnumerator;
        private readonly Func<T1, CancellationToken, Task<IAsyncEnumerator<T2>>> selector;

        public AsyncEnumeratorSelectManyAsync(IAsyncEnumerator<T1> baseEnumerator, Func<T1, CancellationToken, Task<IAsyncEnumerator<T2>>> selector)
        {
            baseEnumerator.AssertNotEnumerated();
            this.baseEnumerator = baseEnumerator;
            this.selector = selector;
        }

        public T2 Current => currentEnumerator.Current;

        public bool EnumerationStarted => this.baseEnumerator.EnumerationStarted;

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            if (!EnumerationStarted || !await currentEnumerator.MoveNextAsync(cancellationToken))
            {
                do
                {
                    if (await baseEnumerator.MoveNextAsync(cancellationToken).ConfigureAwait(true))
                    {
                        currentEnumerator = await selector(baseEnumerator.Current, cancellationToken).ConfigureAwait(true);
                    }
                    else
                    {
                        return false;
                    }
                }
                while (!await currentEnumerator.MoveNextAsync(cancellationToken));
            }
            return true;
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                baseEnumerator.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
