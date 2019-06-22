// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.ContractsLight;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    ///     This is a collection of per-key exclusive locks.
    ///     Borrowed from the Domino code-base
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "LockSet")]
    public sealed class LockSet<TKey>
        where TKey : IEquatable<TKey>
    {
        // ReSharper disable once StaticFieldInGenericType
        private static long _currentHandleId = 1;

        private readonly ConcurrentDictionary<TKey, LockHandle> _exclusiveLocks =
            new ConcurrentDictionary<TKey, LockHandle>();

        /// <summary>
        ///     Acquires an exclusive lock for the given key. <see cref="Release" /> must be called
        ///     subsequently in a 'finally' block.
        /// </summary>
        public async Task<LockHandle> Acquire(TKey key)
        {
            var thisHandle = new LockHandle(this, key);

            while (true)
            {
                LockHandle currentHandle;
                if (!_exclusiveLocks.GetOrAdd(key, thisHandle, out currentHandle))
                {
                    await currentHandle.TaskCompletionSource.Task.ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }

            return thisHandle;
        }

        /// <summary>
        ///     Releases an exclusive lock for the given key. One must release a lock after first await-ing an
        ///     <see cref="Acquire(TKey)" /> (by disposing the returned lock handle).
        /// </summary>
        private void Release(LockHandle handle)
        {
            bool removeSucceeded = _exclusiveLocks.TryRemoveSpecific(handle.Key, handle);
            Contract.Assume(removeSucceeded, "TryRemoveSpecific should not fail, since Release should only be called after Acquire.");
            Task.Run(() => handle.TaskCompletionSource.SetResult(ValueUnit.Void));
        }

        /// <summary>
        ///     Represents an acquired lock in the collection. Call <see cref="Dispose" />
        ///     to release the acquired lock.
        /// </summary>
        /// <remarks>
        ///     FxCop requires equality operations to be overloaded for value types.
        ///     Because lock handles should never be compared, these will all throw.
        /// </remarks>
        public struct LockHandle : IEquatable<LockHandle>, IDisposable
        {
            private readonly long _handleId;
            private LockSet<TKey> _locks;

            /// <summary>
            ///     The associated TaskCompletionSource.
            /// </summary>
            internal readonly SafeTaskCompletionSource<ValueUnit> TaskCompletionSource;

            /// <summary>
            ///     Gets the associated Key.
            /// </summary>
            public TKey Key { get; }

            /// <summary>
            ///     Initializes a new instance of the <see cref="LockHandle" /> struct for the given collection/key.
            /// </summary>
            internal LockHandle(LockSet<TKey> locks, TKey key)
            {
                Contract.Requires(locks != null);
                Contract.Requires(key != null);

                TaskCompletionSource = new SafeTaskCompletionSource<ValueUnit>(); // No TaskCreationOptions.RunContinuationsAsynchronously (.NET 4.5.1 < 4.6.1)
                _locks = locks;
                Key = key;
                _handleId = Interlocked.Increment(ref _currentHandleId);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                Contract.Assume(_locks != null, "Lock handle already disposed.");
                _locks.Release(this);
                _locks = null;
            }

            /// <inheritdoc />
            public bool Equals(LockHandle other)
            {
                return this == other;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (obj is LockHandle)
                {
                    return Equals((LockHandle)obj);
                }

                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return unchecked((int)_handleId);
            }

            internal void MarkUnused()
            {
                this.TaskCompletionSource.MarkTaskAsUnused();
            }

            /// <summary>
            ///     Equality operator.
            /// </summary>
            public static bool operator ==(LockHandle left, LockHandle right)
            {
                return left._handleId == right._handleId;
            }

            /// <summary>
            ///     Inequality operator.
            /// </summary>
            public static bool operator !=(LockHandle left, LockHandle right)
            {
                return !(left == right);
            }
        }

        /// <summary>
        ///     Acquire exclusive locks for a set of keys.
        /// </summary>
        public async Task<LockHandleSet> Acquire(IEnumerable<TKey> keys)
        {
            Contract.Requires(keys != null);

            var sortedKeys = new List<TKey>(keys);
            sortedKeys.Sort();

            var handles = new List<LockHandle>(sortedKeys.Count);
            foreach (var key in sortedKeys)
            {
                handles.Add(await Acquire(key).ConfigureAwait(false));
            }

            return new LockHandleSet(this, handles);
        }

        /// <summary>
        ///     Represents a set of acquired locks in the collection. Call <see cref="Dispose" />
        ///     to release the acquired locks.
        /// </summary>
        public sealed class LockHandleSet : IDisposable
        {
            private readonly LockSet<TKey> _locks;
            private readonly IEnumerable<LockHandle> _handles;

            /// <summary>
            ///     Initializes a new instance of the <see cref="LockHandleSet" /> class for the given set of keys.
            /// </summary>
            public LockHandleSet(LockSet<TKey> locks, IEnumerable<LockHandle> handles)
            {
                Contract.Requires(locks != null);
                Contract.Requires(handles != null);
                _locks = locks;
                _handles = handles;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                foreach (var handle in _handles)
                {
                    _locks.Release(handle);
                }
            }
        }
    }
}
