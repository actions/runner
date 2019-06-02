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

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    /// <summary>
    /// Provides properties for <see cref="Command"/> instances. The instances of <see cref="ICommandProperties"/>
    /// will be created by <see cref="CommandPropertiesDefault"/>.
    /// We can create only a <see cref="CommandPropertiesSetter"/> for a command, which is only used to get the
    /// default values for the current <see cref="ICommandProperties"/> implementation.
    /// </summary>
    /// <seealso cref="CommandPropertiesDefault"/>
    /// <seealso cref="CommandPropertiesSetter"/>
    public interface ICommandProperties
    {
        bool CircuitBreakerDisabled { get; }
        int CircuitBreakerErrorThresholdPercentage { get; }
        bool CircuitBreakerForceClosed { get; }
        bool CircuitBreakerForceOpen { get; }
        int CircuitBreakerRequestVolumeThreshold { get; }
        TimeSpan CircuitBreakerMinBackoff { get; }
        TimeSpan CircuitBreakerMaxBackoff { get; }
        TimeSpan CircuitBreakerDeltaBackoff { get; }

        TimeSpan ExecutionTimeout { get; }
        int ExecutionMaxConcurrentRequests { get; }
        int FallbackMaxConcurrentRequests { get; }
        int ExecutionMaxRequests { get; }
        int FallbackMaxRequests { get; }

        bool FallbackDisabled { get; }

        TimeSpan MetricsHealthSnapshotInterval { get; }
        int MetricsRollingStatisticalWindowInMilliseconds { get; }
        int MetricsRollingStatisticalWindowBuckets { get; }
    }
}
