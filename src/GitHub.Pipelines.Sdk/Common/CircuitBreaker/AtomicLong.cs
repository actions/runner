// Copyright 2012 Netflix, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Globalization;
using System.Threading;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    [Serializable]
    public class AtomicLong : IFormattable
    {
        private long longValue;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public long Value
        {
            get
            {
                return Interlocked.Read(ref this.longValue);
            }
            set
            {
                Interlocked.Exchange(ref this.longValue, value);
            }
        }

        public AtomicLong()
            : this(0)
        {
        }
        public AtomicLong(long initialValue)
        {
            this.longValue = initialValue;
        }

        public long AddAndGet(long delta)
        {
            return Interlocked.Add(ref this.longValue, delta);
        }
        /// <summary>
        /// Atomically sets the value to the given updated value if the current value == the expected value.
        /// </summary>
        /// <returns>True if the value was the expected value.</returns>
        public bool CompareAndSet(long expect, long update)
        {
            return Interlocked.CompareExchange(ref this.longValue, update, expect) == expect;
        }
        public long DecrementAndGet()
        {
            return Interlocked.Decrement(ref this.longValue);
        }
        public long GetAndDecrement()
        {
            return Interlocked.Decrement(ref this.longValue) + 1;
        }
        public long GetAndIncrement()
        {
            return Interlocked.Increment(ref this.longValue) - 1;
        }
        public long GetAndSet(long newValue)
        {
            return Interlocked.Exchange(ref this.longValue, newValue);
        }
        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref this.longValue);
        }
        public bool WeakCompareAndSet(long expect, long update)
        {
            return CompareAndSet(expect, update);
        }

        public override bool Equals(object obj)
        {
            return obj as AtomicLong == this;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }
        public string ToString(IFormatProvider formatProvider)
        {
            return Value.ToString(formatProvider);
        }
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return Value.ToString(formatProvider);
        }

        public static bool operator ==(AtomicLong left, AtomicLong right)
        {
            if (Object.ReferenceEquals(left, null) || Object.ReferenceEquals(right, null))
                return false;

            return left.Value == right.Value;
        }
        public static bool operator !=(AtomicLong left, AtomicLong right)
        {
            return !(left == right);
        }
        public static implicit operator long (AtomicLong atomic)
        {
            if (atomic == null)
            {
                return 0;
            }
            else
            {
                return atomic.Value;
            }
        }
    }
}
