using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace GitHub.Services.BlobStore.Common
{
    public interface IPoolHandle<T> : IDisposable
    {
        T Value { get; }
        void AssertValid();
    }

    public sealed class ByteArrayPool : Pool<byte[]>
    {
        private static Action<byte[]> Reset = b =>
        {
#if DEBUG
            b[0] = (byte)0xcc;
            b[b.Length-1] = (byte)0xcc;
#endif
        };

        private static byte[] CreateNew(int bufferSize)
        {
            var bytes = new byte[bufferSize];
            Reset(bytes);
            return bytes;
        }

        public ByteArrayPool(int bufferSize, int maxToKeep) 
            : base(() => CreateNew(bufferSize), Reset, maxToKeep)
        {
        }

        public override PoolHandle Get()
        {
            var bytes = base.Get();
#if DEBUG
            Debug.Assert(bytes.Value[0] == (byte)0xcc);
            Debug.Assert(bytes.Value[bytes.Value.Length-1] == (byte)0xcc);
#endif
            return bytes;
        }
    }

    public class Pool<T> : IDisposable
    {
        private readonly Func<T> factory;
        private readonly Action<T> reset;
        private readonly int maxToKeep;
        private readonly ConcurrentBag<T> bag = new ConcurrentBag<T>();
        private int countOverApproximation = 0;

        internal int CountOverApproximation => countOverApproximation;
        internal int CountExactSlow => bag.Count;
        public Pool(Func<T> factory, Action<T> reset, int maxToKeep)
        {
            this.factory = factory;
            this.reset = reset;
            this.maxToKeep = maxToKeep;
        }

        public virtual PoolHandle Get()
        {
            T item;
            if (!TryTakeFromBag(out item))
            {
                item = factory();
            }

            return new PoolHandle(this, item);
        }

        private void Return(T item)
        {
            if (countOverApproximation < maxToKeep)
            {
                reset(item);
                AddToBag(item);
            }
            else
            {
                (item as IDisposable)?.Dispose();
            }
        }

        private void AddToBag(T item)
        {
            Interlocked.Increment(ref countOverApproximation);
            // Between the line above and below, count is an over-approximation
            bag.Add(item);
        }

        private bool TryTakeFromBag(out T item)
        {
            if (bag.TryTake(out item))
            {
                // Between the line above and below, count is an over-approximation
                Interlocked.Decrement(ref countOverApproximation);
                return true;
            }
            else
            {
                return false;
            }
        }

        public struct PoolHandle : IPoolHandle<T>
        {
            private readonly Pool<T> pool;
            private readonly T value;
            private bool disposed;

            public PoolHandle(Pool<T> pool, T value)
            {
                this.pool = pool;
                this.value = value;
                this.disposed = false;
            }

            public T Value
            {
                get
                {
                    AssertValid();
                    return this.value;
                }
            }

            public void AssertValid()
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    try
                    {
                        pool.Return(value);
                    }
                    catch(ObjectDisposedException)
                    {
                        // Nothing to return to...
                    }

                    disposed = true;
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    T item;
                    while (bag.TryTake(out item))
                    {
                        (item as IDisposable)?.Dispose();
                    }
                    
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
