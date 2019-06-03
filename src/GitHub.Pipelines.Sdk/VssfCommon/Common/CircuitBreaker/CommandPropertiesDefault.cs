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
    public class CommandPropertiesDefault : ICommandProperties
    {
        private const bool DefaultCircuitBreakerDisabled = false;
        private const int DefaultCircuitBreakerErrorThresholdPercentage = 50;               // default => errorThresholdPercentage = 50 = if 50%+ of requests in 10 seconds are failures or latent when we will trip the circuit
        private const bool DefaultCircuitBreakerForceClosed = false;                        // default => ignoreErrors = false 
        private const bool DefaultCircuitBreakerForceOpen = false;                          // default => forceCircuitOpen = false (we want to allow traffic)
        private const int DefaultCircuitBreakerRequestVolumeThreshold = 20;                  // default => statisticalWindowVolumeThreshold: 20 requests in 10 seconds must occur before statistics matter
        private static TimeSpan DefaultCircuitBreakerMinBackoff = TimeSpan.FromSeconds(0);  // default => Minimum back-off interval. This is added to the retry interval computed from deltaBackoff.
        private static TimeSpan DefaultCircuitBreakerMaxBackoff = TimeSpan.FromSeconds(30); // default => Maximum back-off interval. MaximumBackoff is used if the computed retry interval is greater than MaxBackoff.
        private static TimeSpan DefaultCircuitBreakerDeltaBackoff = TimeSpan.FromMilliseconds(300); // default -> Back-off interval between retries. Multiples of this timespan will be used for subsequent retry attempts.
        private static readonly TimeSpan DefaultExecutionTimeout = TimeSpan.FromSeconds(1.0); // default => executionTimeoutInMilliseconds: 1000 = 1 second
        private const int DefaultExecutionMaxConcurrentRequests = int.MaxValue;             // default => don't limit concurrent execution requests by default
        private const int DefaultFallbackMaxConcurrentRequests = int.MaxValue;              // default => don't limit concurrent fallback requests by default
        private const int DefaultExecutionMaxRequests = int.MaxValue;                       // default => don't limit execution requests by default
        private const int DefaultFallbackMaxRequests = int.MaxValue;                        // default => don't limit fallback requests by default
        private const bool DefaultFallbackDisabled = false;
        private static readonly TimeSpan DefaultMetricsHealthSnapshotInterval = TimeSpan.FromSeconds(0.5); // default to 500ms as max frequency between allowing snapshots of health (error percentage etc)
        private const int DefaultMetricsRollingStatisticalWindowInMilliseconds = 10000;     // default => statisticalWindow: 10000 = 10 seconds (and default of 10 buckets so each bucket is 1 second)
        private const int DefaultMetricsRollingStatisticalWindowBuckets = 10;               // default => statisticalWindowBuckets: 10 = 10 buckets in a 10 second window so each bucket is 1 second

        public bool CircuitBreakerDisabled { get; protected set; }
        public int CircuitBreakerErrorThresholdPercentage { get; protected set; }
        public bool CircuitBreakerForceClosed { get; protected set; }
        public bool CircuitBreakerForceOpen { get; protected set; }
        public int CircuitBreakerRequestVolumeThreshold { get; protected set; }
        public TimeSpan CircuitBreakerMinBackoff { get; protected set; }
        public TimeSpan CircuitBreakerMaxBackoff { get; protected set; }
        public TimeSpan CircuitBreakerDeltaBackoff { get; protected set; }

        public TimeSpan ExecutionTimeout { get; protected set; }
        public int ExecutionMaxConcurrentRequests { get; protected set; }
        public int FallbackMaxConcurrentRequests { get; protected set; }
        public int ExecutionMaxRequests { get; protected set; }
        public int FallbackMaxRequests { get; protected set; }
        public bool FallbackDisabled { get; protected set; }
        public TimeSpan MetricsHealthSnapshotInterval { get; protected set; }
        public int MetricsRollingStatisticalWindowInMilliseconds { get; protected set; }
        public int MetricsRollingStatisticalWindowBuckets { get; protected set; }

        public CommandPropertiesDefault(CommandPropertiesSetter setter = null)
        {
            setter = setter ?? new CommandPropertiesSetter();

            CircuitBreakerDisabled = setter.CircuitBreakerDisabled ?? DefaultCircuitBreakerDisabled;
            CircuitBreakerErrorThresholdPercentage = setter.CircuitBreakerErrorThresholdPercentage ?? DefaultCircuitBreakerErrorThresholdPercentage;
            CircuitBreakerForceClosed = setter.CircuitBreakerForceClosed ?? DefaultCircuitBreakerForceClosed;
            CircuitBreakerForceOpen = setter.CircuitBreakerForceOpen ?? DefaultCircuitBreakerForceOpen;
            CircuitBreakerRequestVolumeThreshold = setter.CircuitBreakerRequestVolumeThreshold ?? DefaultCircuitBreakerRequestVolumeThreshold;
            CircuitBreakerMinBackoff = setter.CircuitBreakerMinBackoff ?? DefaultCircuitBreakerMinBackoff;
            CircuitBreakerMaxBackoff = setter.CircuitBreakerMaxBackoff ?? DefaultCircuitBreakerMaxBackoff;
            CircuitBreakerDeltaBackoff = setter.CircuitBreakerDeltaBackoff ?? DefaultCircuitBreakerDeltaBackoff;
            ExecutionTimeout = setter.ExecutionTimeout ?? DefaultExecutionTimeout;
            ExecutionMaxConcurrentRequests = setter.ExecutionMaxConcurrentRequests ?? DefaultExecutionMaxConcurrentRequests;
            ExecutionMaxRequests = setter.ExecutionMaxRequests ?? DefaultExecutionMaxRequests;
            FallbackMaxConcurrentRequests = setter.FallbackMaxConcurrentRequests ?? DefaultFallbackMaxConcurrentRequests;
            FallbackMaxRequests = setter.FallbackMaxRequests ?? DefaultFallbackMaxRequests;
            FallbackDisabled = setter.FallbackDisabled ?? DefaultFallbackDisabled;
            MetricsHealthSnapshotInterval = setter.MetricsHealthSnapshotInterval ?? DefaultMetricsHealthSnapshotInterval;
            MetricsRollingStatisticalWindowInMilliseconds = setter.MetricsRollingStatisticalWindowInMilliseconds ?? DefaultMetricsRollingStatisticalWindowInMilliseconds;
            MetricsRollingStatisticalWindowBuckets = setter.MetricsRollingStatisticalWindowBuckets ?? DefaultMetricsRollingStatisticalWindowBuckets;
        }
    }
}
