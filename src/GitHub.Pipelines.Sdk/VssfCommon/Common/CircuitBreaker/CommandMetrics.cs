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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    public class CommandMetrics
    {
        private static readonly ConcurrentDictionary<string, CommandMetrics> metrics = new ConcurrentDictionary<string, CommandMetrics>();

        public static CommandMetrics GetInstance(CommandKey key, CommandGroupKey commandGroup, ICommandProperties properties, IEventNotifier eventNotifier, ITime time = null)
        {
            return metrics.GetOrAdd(key.Name, w => new CommandMetrics(key, commandGroup, properties, eventNotifier, time));
        }
        public static CommandMetrics GetInstance(CommandKey key)
        {
            return metrics[key.Name];
        }
        public static IEnumerable<CommandMetrics> Instances { get { return metrics.Values; } }
        internal static void Reset()
        {
            metrics.Clear();
        }

        private readonly CommandKey key;
        private readonly CommandGroupKey group;
        private readonly ICommandProperties properties;
        private readonly IEventNotifier eventNotifier;
        private readonly ITime time;
        internal readonly RollingNumber success;
        internal readonly RollingNumber failure;
        internal readonly RollingNumber timeout;
        internal readonly RollingNumber shortCircuited;
        internal readonly RollingNumber concurrencyRejected;
        internal readonly RollingNumber limitRejected;
        internal readonly RollingNumber fallbackSuccess;
        internal readonly RollingNumber fallbackFailure;
        internal readonly RollingNumber fallbackConcurrencyRejected;
        internal readonly RollingNumber fallbackLimitRejected;

        public CommandKey CommandKey { get { return key; } }
        public CommandGroupKey CommandGroup { get { return group; } }
        public ICommandProperties Properties { get { return properties; } }


        internal CommandMetrics(CommandKey key, CommandGroupKey commandGroup, ICommandProperties properties, IEventNotifier eventNotifier, ITime time = null)
        {
            this.key = key;
            this.group = commandGroup;
            this.properties = properties;
            this.eventNotifier = eventNotifier;
            this.time = time ?? ITimeDefault.Instance;
            this.success = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.failure = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.timeout = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.shortCircuited = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.concurrencyRejected = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.limitRejected = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.fallbackSuccess = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.fallbackFailure = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.fallbackConcurrencyRejected = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            this.fallbackLimitRejected = new RollingNumber(this.time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
        }

        internal void ResetCounter()
        {
            // TODO: (benpeart) this doesn't not currently handle dynamic changes to the MetricsRollingStatisticalWindowInMilliseconds or MetricsRollingStatisticalWindowBuckets properties (bad things will happen if they change)
            success.Reset();
            failure.Reset();
            timeout.Reset();
            shortCircuited.Reset();
            concurrencyRejected.Reset();
            limitRejected.Reset();
            fallbackSuccess.Reset();
            fallbackFailure.Reset();
            fallbackConcurrencyRejected.Reset();
            fallbackLimitRejected.Reset();
            healthCountsSnapshot = new HealthCounts(0, 0, 0);
            lastHealthCountsSnapshot = time.GetCurrentTimeInMillis();
        }

        internal void MarkSuccess()
        {
            LastException = null;
            eventNotifier.MarkEvent(group, key, EventType.Success);
            success.Increment();
        }

        internal void MarkFailure()
        {
            eventNotifier.MarkEvent(group, key, EventType.Failure);
            failure.Increment();
        }

        internal void MarkTimeout()
        {
            LastException = null;
            eventNotifier.MarkEvent(group, key, EventType.Timeout);
            timeout.Increment();
        }

        internal void MarkShortCircuited()
        {
            eventNotifier.MarkEvent(group, key, EventType.ShortCircuited);
            shortCircuited.Increment();
        }

        internal void MarkConcurrencyRejected()
        {
            eventNotifier.MarkEvent(group, key, EventType.ConcurrencyRejected);
            concurrencyRejected.Increment();
        }

        internal void MarkLimitRejected()
        {
            eventNotifier.MarkEvent(group, key, EventType.LimitRejected);
            limitRejected.Increment();
        }

        internal void MarkFallbackSuccess()
        {
            eventNotifier.MarkEvent(group, key, EventType.FallbackSuccess);
            fallbackSuccess.Increment();
        }

        internal void MarkFallbackFailure()
        {
            eventNotifier.MarkEvent(group, key, EventType.FallbackFailure);
            fallbackFailure.Increment();
        }

        internal void MarkFallbackConcurrencyRejected()
        {
            eventNotifier.MarkEvent(group, key, EventType.FallbackConcurrencyRejected);
            fallbackConcurrencyRejected.Increment();
        }

        internal void MarkFallbackLimitRejected()
        {
            eventNotifier.MarkEvent(group, key, EventType.FallbackLimitRejected);
            fallbackLimitRejected.Increment();
        }

        private volatile HealthCounts healthCountsSnapshot = new HealthCounts(0, 0, 0);
        private long lastHealthCountsSnapshot = 0;

        public HealthCounts GetHealthCounts()
        {
            // we put an interval between snapshots so high-volume commands don't 
            // spend too much unnecessary time calculating metrics in very small time periods
            long lastTime = lastHealthCountsSnapshot;
            long currentTime = time.GetCurrentTimeInMillis();
            if (currentTime - lastTime >= properties.MetricsHealthSnapshotInterval.TotalMilliseconds)
            {
                if (Interlocked.CompareExchange(ref lastHealthCountsSnapshot, currentTime, lastTime) == lastTime)
                {
                    // our thread won setting the snapshot time so we will proceed with generating a new snapshot
                    // losing threads will continue using the old snapshot
                    long success = this.success.GetRollingSum();
                    long failure = this.failure.GetRollingSum(); // fallbacks occur on this
                    long timeout = this.timeout.GetRollingSum(); // fallbacks occur on this
                    long shortCircuited = this.shortCircuited.GetRollingSum(); // fallbacks occur on this
                    long concurrencyRejected = this.concurrencyRejected.GetRollingSum(); // fallbacks occur on this
                    long limitRejected = this.limitRejected.GetRollingSum(); // fallbacks occur on this

                    long totalCount = success + failure + timeout + shortCircuited + concurrencyRejected + limitRejected;
                    long errorCount = failure + timeout + shortCircuited;

                    healthCountsSnapshot = new HealthCounts(totalCount, errorCount, concurrencyRejected);
                }
            }
            return healthCountsSnapshot;
        }

        /// <summary>
        /// Gets/Sets the last exception associated with this circuit breaker
        /// Used by <see cref="Command"/> to provide better diagnostic information.
        /// </summary>
        public Exception LastException { get; set; }
    }



    /// <summary>
    /// Stores summarized health metrics about Circuit Breaker Commands.
    /// </summary>
    public class HealthCounts
    {
        private readonly long totalCount;
        private readonly long errorCount;
        private readonly int errorPercentage;
        private readonly long semaphoreRejectedCount;

        /// <summary>
        /// The total number of requests made by this command.
        /// </summary>
        public long TotalRequests { get { return totalCount; } }

        /// <summary>
        /// The total number of errors made by this command.
        /// </summary>
        public long ErrorCount { get { return errorCount; } }

        /// <summary>
        /// Gets the semaphore rejected.
        /// </summary>
        public long SemaphoreRejected { get { return semaphoreRejectedCount; } }

        /// <summary>
        /// The ratio of total requests and error counts in percents.
        /// </summary>
        public int ErrorPercentage { get { return errorPercentage; } }

        /// <summary>
        /// Initializes a new instance of HealthCounts.
        /// </summary>
        /// <param name="total">The total number of requests made by this command.</param>
        /// <param name="error">The total number of errors made by this command.</param>
        public HealthCounts(long total, long error, long semaphoreRejected)
        {
            totalCount = total;
            errorCount = error;
            semaphoreRejectedCount = semaphoreRejected;

            if (total > 0)
            {
                errorPercentage = (int)((double)error / total * 100);
            }
            else
            {
                errorPercentage = 0;
            }
        }
    }
}
