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
using GitHub.Services.Common;
using System;
using System.Runtime.Serialization;

namespace GitHub.Services.CircuitBreaker
{
    /// <summary>
    /// Base class for all CB exceptions to facilitate easier handling of circuit breaker exceptions.
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CircuitBreakerException", "GitHub.Services.CircuitBreaker.CircuitBreakerException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class CircuitBreakerException : VssException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public CircuitBreakerException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerException"/> class with serialized data.
        /// </summary>
        protected CircuitBreakerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// CircuitBreakerShortCircuitException is thrown when 
    /// a <see cref="Command"/> fails and does not have a fallback.
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CircuitBreakerShortCircuitException", "GitHub.Services.CircuitBreaker.CircuitBreakerShortCircuitException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CircuitBreakerShortCircuitException : CircuitBreakerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerShortCircuitException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public CircuitBreakerShortCircuitException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerShortCircuitException"/> class with serialized data.
        /// </summary>
        protected CircuitBreakerShortCircuitException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// CircuitBreakerExceededConcurrencyException is thrown when 
    /// the maximum number of concurrent requests permitted to Command.Run() is exceeded.
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CircuitBreakerExceededConcurrencyException", "GitHub.Services.CircuitBreaker.CircuitBreakerExceededConcurrencyException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CircuitBreakerExceededConcurrencyException : CircuitBreakerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerExceededConcurrencyException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public CircuitBreakerExceededConcurrencyException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerExceededConcurrencyException"/> class with serialized data.
        /// </summary>
        protected CircuitBreakerExceededConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// CircuitBreakerExceededExecutionLimitException is thrown when 
    /// the maximum number of requests permitted to Command.Run() is exceeded.
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CircuitBreakerExceededExecutionLimitException", "GitHub.Services.CircuitBreaker.CircuitBreakerExceededExecutionLimitException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CircuitBreakerExceededExecutionLimitException : CircuitBreakerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerExceededExecutionLimitException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public CircuitBreakerExceededExecutionLimitException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerExceededExecutionLimitException"/> class with serialized data.
        /// </summary>
        protected CircuitBreakerExceededExecutionLimitException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
