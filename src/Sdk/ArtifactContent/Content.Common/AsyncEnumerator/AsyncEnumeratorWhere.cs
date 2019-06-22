using System;

namespace GitHub.Services.Content.Common
{
    public class AsyncEnumeratorWhere<T1> : AsyncEnumeratorWrapper<T1, T1>
    {
        private readonly Func<T1, bool> selector;
        private long? count;

        public AsyncEnumeratorWhere(IAsyncEnumerator<T1> baseEnumerator, Func<T1, bool> selector, long? take = null) : base(baseEnumerator)
        {
            this.selector = selector;
            this.count = take;
        }

        protected override bool OnBaseValueEnumerated(T1 value)
        {
            if (count.HasValue)
            {
                if (count.Value <= 0)
                {
                    return false;
                }
                else
                {
                    --count;
                }
            }

            bool match = selector(value);
            if (match)
            {
                Current = value;
            }

            return match;
        }
    }
}
