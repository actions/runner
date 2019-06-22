using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public abstract class AsyncEnumeratorWrapper<T1, T2> : IAsyncEnumerator<T2>
    {
        private readonly IAsyncEnumerator<T1> baseEnumerator;

        protected AsyncEnumeratorWrapper(IAsyncEnumerator<T1> baseEnumerator)
        {
            baseEnumerator.AssertNotEnumerated();
            this.baseEnumerator = baseEnumerator;
        }

        public T2 Current { get; protected set; }

        public bool EnumerationStarted => this.baseEnumerator.EnumerationStarted;

        public void Dispose()
        {
            Dispose(true);
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            do
            {
                if (!(await baseEnumerator.MoveNextAsync(cancellationToken).ConfigureAwait(true)))
                {
                    return false;
                }
            }
            while (!OnBaseValueEnumerated(baseEnumerator.Current));

            return true;
        }

        protected abstract bool OnBaseValueEnumerated(T1 value);

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                baseEnumerator.Dispose();
            }
        }
    }

    public abstract class AsyncEnumeratorWrapperAsync<T1, T2> : IAsyncEnumerator<T2>
    {
        private readonly IAsyncEnumerator<T1> baseEnumerator;

        protected AsyncEnumeratorWrapperAsync(IAsyncEnumerator<T1> baseEnumerator)
        {
            baseEnumerator.AssertNotEnumerated();
            this.baseEnumerator = baseEnumerator;
        }

        public T2 Current { get; protected set; }

        public bool EnumerationStarted => this.baseEnumerator.EnumerationStarted;

        public void Dispose()
        {
            Dispose(true);
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            do
            {
                if (!(await baseEnumerator.MoveNextAsync(cancellationToken).ConfigureAwait(true)))
                {
                    return false;
                }
            }
            while (!(await OnBaseValueEnumeratedAsync(baseEnumerator.Current).ConfigureAwait(true)));

            return true;
        }

        protected abstract Task<bool> OnBaseValueEnumeratedAsync(T1 value);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                baseEnumerator.Dispose();
            }
        }
    }
}
