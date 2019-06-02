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
    public class AtomicBoolean : IFormattable, IEquatable<AtomicBoolean>
    {
        private volatile int booleanValue;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public bool Value
        {
            get
            {
                return this.booleanValue != 0;
            }
            set
            {
                this.booleanValue = value ? 1 : 0;
            }
        }

        public AtomicBoolean()
            : this(false)
        {
        }

        public AtomicBoolean(bool initialValue)
        {
            Value = initialValue;
        }

#pragma warning disable 420 //the exception to the volatile rule is what we are doing...
        public bool CompareAndSet(bool expect, bool update)
        {
            int expectedIntValue = expect ? 1 : 0;
            int newIntValue = update ? 1 : 0;
            return Interlocked.CompareExchange(ref this.booleanValue, newIntValue, expectedIntValue) == expectedIntValue;
        }

        public bool Exchange(bool newValue)
        {
            return Interlocked.Exchange(ref this.booleanValue, newValue ? 1 : 0) != 0;
        }
#pragma warning restore 420

        /// <summary>
        /// Determines whether the specified <see cref="AtomicBoolean"/> is equal to the current <see cref="AtomicBoolean"/>.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false</returns>
        public bool Equals(AtomicBoolean other)
        {
            if (other == null)
                return false;

            return this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return Equals(obj as AtomicBoolean);
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

        public static bool operator ==(AtomicBoolean left, AtomicBoolean right)
        {
            if (Object.ReferenceEquals(left, null) || Object.ReferenceEquals(right, null))
                return false;

            return left.Value == right.Value;
        }

        public static bool operator !=(AtomicBoolean left, AtomicBoolean right)
        {
            return !(left == right);
        }

        public static implicit operator bool (AtomicBoolean atomic)
        {
            if (atomic == null)
            {
                return false;
            }
            else
            {
                return atomic.Value;
            }
        }
    }
}
