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
using System.Diagnostics;

namespace GitHub.Services.CircuitBreaker
{
    /// <summary>
    /// Provides a mechanism to be notified of events for <see cref="Command"/> instances. 
    /// </summary>
    public enum EventType
    {
        Success,
        Failure,
        Timeout,
        ShortCircuited,
        ConcurrencyRejected,
        LimitRejected,
        FallbackSuccess,
        FallbackFailure,
        FallbackConcurrencyRejected,
        FallbackLimitRejected,
    }

    /// <summary>
    /// The circuit breaker trace points.
    /// </summary>
    public static class CircuitBreakerTracePoints
    {
        public const int ExecutionFailure = 10003202;
        public const int ExecutionConcurrencyRejection = 10003203;
        public const int ExecutionLimitRejection = 10003201;
        public const int ShortCircuited = 10003204;
        public const int FallbackMissingDelegate = 10003205;
        public const int FallbackFailure = 10003206;
        public const int FallbackConcurrencyRejection = 10003207;
        public const int FallbackLimitRejection = 10003209;
        public const int FallbackDisabled = 10003208;
    }

    public interface IEventNotifier
    {
        void MarkEvent(CommandGroupKey group, CommandKey key, EventType eventType);
        void MarkCommandExecution(CommandGroupKey group, CommandKey key, long elapsedTimeInMilliseconds);
        void MarkExecutionConcurrency(CommandGroupKey group, CommandKey key, long executionSemaphoreNumberOfPermitsUsed);
        void MarkFallbackConcurrency(CommandGroupKey group, CommandKey key, long fallbackSemaphoreNumberOfPermitsUsed);
        void MarkExecutionCount(CommandGroupKey group, CommandKey key, long executionCount);
        void MarkFallbackCount(CommandGroupKey group, CommandKey key, long fallbackCount);
        void TraceRaw(int tracepoint, TraceLevel level, string featurearea, string classname, string message);
    }
}
