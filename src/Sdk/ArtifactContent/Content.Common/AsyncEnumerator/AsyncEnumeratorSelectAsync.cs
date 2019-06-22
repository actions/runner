using System;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public class AsyncEnumeratorSelect<T1, T2> : AsyncEnumeratorWrapper<T1, T2>
    {
        private readonly Func<T1, T2> selector;

        public AsyncEnumeratorSelect(IAsyncEnumerator<T1> baseEnumerator, Func<T1, T2> selector) : base(baseEnumerator)
        {
            this.selector = selector;
        }

        protected override bool OnBaseValueEnumerated(T1 value)
        {
            Current = selector(value);
            return true;
        }
    }

    public class AsyncEnumeratorSelectAsync<T1, T2> : AsyncEnumeratorWrapperAsync<T1, T2>
    {
        private readonly Func<T1, Task<T2>> selectorAsync;

        public AsyncEnumeratorSelectAsync(IAsyncEnumerator<T1> baseEnumerator, Func<T1, Task<T2>> selectorAsync) : base(baseEnumerator)
        {
            this.selectorAsync = selectorAsync;
        }

        protected override async Task<bool> OnBaseValueEnumeratedAsync(T1 value)
        {
            Current = await selectorAsync(value).ConfigureAwait(true);
            return true;
        }
    }
}
