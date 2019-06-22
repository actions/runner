using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public class SafeTaskCompletionSource<T>
    {
        private readonly TaskCompletionSource<T> inner;

        public SafeTaskCompletionSource() : this(null) { }
        public SafeTaskCompletionSource(object state) : this(state, TaskCreationOptions.None) { }
        public SafeTaskCompletionSource(TaskCreationOptions options) : this(null, options) { }
        public SafeTaskCompletionSource(object state, TaskCreationOptions options)
        {
            inner = new TaskCompletionSource<T>(state, options);
        }

        public Task<T> Task => inner.Task;

        public void SetCanceled()
        {
            inner.SetCanceled();
            GC.SuppressFinalize(this);
        }

        public void SetException(Exception exception)
        {
            inner.SetException(exception);
            GC.SuppressFinalize(this);
        }
    
        public void SetException(IEnumerable<Exception> exceptions)
        {
            inner.SetException(exceptions);
            GC.SuppressFinalize(this);
        }

        public void SetResult(T result)
        {
            inner.SetResult(result);
            GC.SuppressFinalize(this);
        }

        public bool TrySetCanceled()
        {
            bool b = inner.TrySetCanceled();
            if (b)
            {
                GC.SuppressFinalize(this);
            }
            return b;
        }
        public bool TrySetException(Exception exception)
        {
            bool b = inner.TrySetException(exception);
            if (b)
            {
                GC.SuppressFinalize(this);
            }
            return b;
        }

        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            bool b = inner.TrySetException(exceptions);
            if (b)
            {
                GC.SuppressFinalize(this);
            }
            return b;
        }

        public bool TrySetResult(T result)
        {
            bool b = inner.TrySetResult(result);
            if (b)
            {
                GC.SuppressFinalize(this);
            }
            return b;
        }

        public void MarkTaskAsUnused()
        {
            GC.SuppressFinalize(this);
        }

        ~SafeTaskCompletionSource()
        {
            this.TrySetException(new ObjectDisposedException("TaskCompeletionSource was GC'd without completing its task."));
        }
    }
}
