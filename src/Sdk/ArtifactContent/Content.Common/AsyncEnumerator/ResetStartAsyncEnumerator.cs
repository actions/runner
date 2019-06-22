using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public sealed class ResetStartAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> inner;

        public ResetStartAsyncEnumerator(IAsyncEnumerator<T> inner)
        {
            this.inner = inner;
        }

        public T Current => this.inner.Current;
        public bool EnumerationStarted { get; private set; }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            EnumerationStarted = true;
            return this.inner.MoveNextAsync(cancellationToken);
        }

        public void Dispose()
        {
            this.inner.Dispose();
        }
    }
}
