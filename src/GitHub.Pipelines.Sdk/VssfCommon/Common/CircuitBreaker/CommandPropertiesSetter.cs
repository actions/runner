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

namespace GitHub.Services.CircuitBreaker
{
    /// <summary>
    /// Provides code driven properties overrides for <see cref="Command"/> instances.
    /// </summary>
    public class CommandPropertiesSetter
    {
        public bool? CircuitBreakerDisabled { get; private set; }
        public int? CircuitBreakerErrorThresholdPercentage { get; private set; }
        public bool? CircuitBreakerForceClosed { get; private set; }
        public bool? CircuitBreakerForceOpen { get; private set; }
        public int? CircuitBreakerRequestVolumeThreshold { get; private set; }
        public TimeSpan? CircuitBreakerMinBackoff { get; private set; }
        public TimeSpan? CircuitBreakerMaxBackoff { get; private set; }
        public TimeSpan? CircuitBreakerDeltaBackoff { get; private set; }
        public TimeSpan? ExecutionTimeout { get; private set; }
        public int? ExecutionMaxConcurrentRequests { get; private set; }
        public int? FallbackMaxConcurrentRequests { get; private set; }
        public int? ExecutionMaxRequests { get; private set; }
        public int? FallbackMaxRequests { get; private set; }
        public bool? FallbackDisabled { get; private set; }
        public TimeSpan? MetricsHealthSnapshotInterval { get; private set; }
        public int? MetricsRollingStatisticalWindowInMilliseconds { get; private set; }
        public int? MetricsRollingStatisticalWindowBuckets { get; private set; }

        public CommandPropertiesSetter()
        {
        }

        public CommandPropertiesSetter(ICommandProperties values)
        {
            CircuitBreakerDisabled = values?.CircuitBreakerDisabled;
            CircuitBreakerErrorThresholdPercentage = values?.CircuitBreakerErrorThresholdPercentage;
            CircuitBreakerForceClosed = values?.CircuitBreakerForceClosed;
            CircuitBreakerForceOpen = values?.CircuitBreakerForceOpen;
            CircuitBreakerRequestVolumeThreshold = values?.CircuitBreakerRequestVolumeThreshold;
            CircuitBreakerMinBackoff = values?.CircuitBreakerMinBackoff;
            CircuitBreakerMaxBackoff = values?.CircuitBreakerMaxBackoff;
            CircuitBreakerDeltaBackoff = values?.CircuitBreakerDeltaBackoff;
            ExecutionTimeout = values?.ExecutionTimeout;
            ExecutionMaxConcurrentRequests = values?.ExecutionMaxConcurrentRequests;
            ExecutionMaxRequests = values?.ExecutionMaxRequests;
            FallbackMaxConcurrentRequests = values?.FallbackMaxConcurrentRequests;
            FallbackMaxRequests = values?.FallbackMaxRequests;
            FallbackDisabled = values?.FallbackDisabled;
            MetricsHealthSnapshotInterval = values?.MetricsHealthSnapshotInterval;
            MetricsRollingStatisticalWindowInMilliseconds = values?.MetricsRollingStatisticalWindowInMilliseconds;
            MetricsRollingStatisticalWindowBuckets = values?.MetricsRollingStatisticalWindowBuckets;
        }

        public CommandPropertiesSetter WithCircuitBreakerDisabled(bool value)
        {
            CircuitBreakerDisabled = value;
            return this;
        }

        public CommandPropertiesSetter WithCircuitBreakerErrorThresholdPercentage(int value)
        {
            CircuitBreakerErrorThresholdPercentage = value;
            return this;
        }

        public CommandPropertiesSetter WithCircuitBreakerForceClosed(bool value)
        {
            CircuitBreakerForceClosed = value;
            return this;
        }

        public CommandPropertiesSetter WithCircuitBreakerForceOpen(bool value)
        {
            CircuitBreakerForceOpen = value;
            return this;
        }

        public CommandPropertiesSetter WithCircuitBreakerRequestVolumeThreshold(int value)
        {
            CircuitBreakerRequestVolumeThreshold = value;
            return this;
        }

        public CommandPropertiesSetter WithCircuitBreakerMinBackoff(TimeSpan value)
        {
            CircuitBreakerMinBackoff = value;
            return this;
        }

        public CommandPropertiesSetter WithCircuitBreakerMaxBackoff(TimeSpan value)
        {
            CircuitBreakerMaxBackoff = value;
            return this;
        }

        public CommandPropertiesSetter WithCircuitBreakerDeltaBackoff(TimeSpan value)
        {
            CircuitBreakerDeltaBackoff = value;
            return this;
        }

        public CommandPropertiesSetter WithExecutionTimeoutInMilliseconds(int milliseconds)
        {
            ExecutionTimeout = TimeSpan.FromMilliseconds(milliseconds);
            return this;
        }

        public CommandPropertiesSetter WithExecutionTimeout(TimeSpan value)
        {
            ExecutionTimeout = value;
            return this;
        }

        public CommandPropertiesSetter WithExecutionMaxConcurrentRequests(int value)
        {
            ExecutionMaxConcurrentRequests = value;
            return this;
        }

        public CommandPropertiesSetter WithFallbackMaxConcurrentRequests(int value)
        {
            FallbackMaxConcurrentRequests = value;
            return this;
        }

        public CommandPropertiesSetter WithExecutionMaxRequests(int value)
        {
            ExecutionMaxRequests = value;
            return this;
        }

        public CommandPropertiesSetter WithFallbackMaxRequests(int value)
        {
            FallbackMaxRequests = value;
            return this;
        }

        public CommandPropertiesSetter WithFallbackDisabled(bool value)
        {
            FallbackDisabled = value;
            return this;
        }

        public CommandPropertiesSetter WithMetricsHealthSnapshotInterval(TimeSpan value)
        {
            MetricsHealthSnapshotInterval = value;
            return this;
        }

        public CommandPropertiesSetter WithMetricsRollingStatisticalWindowInMilliseconds(int value)
        {
            MetricsRollingStatisticalWindowInMilliseconds = value;
            return this;
        }

        public CommandPropertiesSetter WithMetricsRollingStatisticalWindow(TimeSpan value)
        {
            MetricsRollingStatisticalWindowInMilliseconds = (int)value.TotalMilliseconds;
            return this;
        }

        public CommandPropertiesSetter WithMetricsRollingStatisticalWindowBuckets(int value)
        {
            MetricsRollingStatisticalWindowBuckets = value;
            return this;
        }
    }
}
