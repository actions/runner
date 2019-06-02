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
using System.ComponentModel;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    /// <summary>
    /// Circuit-breaker logic that is hooked into <see cref="Command"/> execution and will stop allowing executions if 
    /// failures have gone past the defined threshold.  It will then allow single retries after a defined sleep window 
    /// until the execution succeeds at which point it will close the circuit and allow executions again.
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Every <see cref="Command"/> request asks this if it is allowed to proceed or not.
        /// This takes into account the half-open logic which allows some requests through when determining if it should be closed again.
        /// </summary>
        /// <returns>True is the request is permitted, otherwise false.</returns>
        bool AllowRequest(ICommandProperties properties);

        /// <summary>
        /// Gets whether the circuit is currently open (tripped).
        /// </summary>
        /// <returns>True if the circuit is open, otherwise false.</returns>
        bool IsOpen(ICommandProperties properties);
        
        /// <summary>
        /// Invoked on successful executions from <see cref="Command"/> as part of feedback mechanism when in a half-open state.
        /// </summary>
        void MarkSuccess();

        /// <summary>
        /// Returns true if it has been longer than time since the circuit breaker was last accessed
        /// </summary>
        bool IsOlderThan(TimeSpan time);

        /// <summary>
        /// Gets the Execute Semaphore associated with this circuit breaker.
        /// Used by <see cref="Command"/> to limit the number of simultaneous calls to Execute.
        /// </summary>
        ITryableSemaphore ExecutionSemaphore { get; }

        /// <summary>
        /// Gets the Fallback Semaphore associated with this circuit breaker
        /// Used by <see cref="Command"/> to limit the number of simultaneous calls to Fallback.
        /// </summary>
        ITryableSemaphore FallbackSemaphore { get; }

        /// <summary>
        /// Gets the Execution rolling number counter associated with this circuit breaker
        /// Used by <see cref="Command"/> to limit the number of calls to Execute.
        /// </summary>
        IRollingNumber ExecutionRequests { get; }

        /// <summary>
        /// Gets the Fallback rolling number counter associated with this circuit breaker
        /// Used by <see cref="Command"/> to limit the number of calls to Fallback.
        /// </summary>
        IRollingNumber FallbackRequests { get; }

        /// <summary>
        /// Gets the current state of the circuit breaker without re-evaluating the conditions
        /// </summary>
        /// <returns>CircuitBreakerStatus.Open, HalfOpen, or Closed</returns>
        CircuitBreakerStatus GetCircuitBreakerState(ICommandProperties properties);
    }


    /// <summary>
    /// Factory of <see cref="ICircuitBreaker"/> instances.
    /// Thread safe and ensures only 1 <see cref="ICircuitBreaker"/> per <see cref="CommandKey"/>.
    /// </summary>
    public static class CircuitBreakerFactory
    {
        /// <summary>
        /// Stores instances of <see cref="ICircuitBreaker"/>.
        /// </summary>
        internal static readonly ConcurrentDictionary<CommandKey, ICircuitBreaker> Instances = new ConcurrentDictionary<CommandKey, ICircuitBreaker>();

        /// <summary>
        /// Gets the <see cref="ICircuitBreaker"/> instance for a given <see cref="CommandKey"/>.
        /// If no circuit breaker exists for the specified command key, a new one will be created using the properties and metrics parameters.
        /// If a circuit breaker already exists, those parameters will be ignored.
        /// </summary>
        /// <param name="commandKey">Command key of command instance requesting the circuit breaker.</param>
        /// <param name="properties">The properties of the specified command.</param>
        /// <param name="metrics">The metrics of the specified command.</param>
        /// <returns>A new or an existing circuit breaker instance.</returns>
        public static ICircuitBreaker GetInstance(CommandKey commandKey, ICommandProperties properties, CommandMetrics metrics, ITime time = null)
        {
            return Instances.GetOrAdd(commandKey, w => new CircuitBreakerImpl(properties, metrics, time));
        }

        /// <summary>
        /// Internal for testing only
        /// </summary>
        internal static void SetInstance(CommandKey commandKey, ICircuitBreaker circuitBreaker)
        {
            Instances.AddOrUpdate(commandKey, circuitBreaker, (k, cb1) => circuitBreaker);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="commandKey">The command key.</param>
        /// <returns></returns>
        public static ICircuitBreaker GetInstance(CommandKey commandKey)
        {
            ICircuitBreaker cbInstance = null;
            Instances.TryGetValue(commandKey, out cbInstance);
            return cbInstance;
        }

        /// <summary>
        /// Clears all circuit breakers that have not been accessed within the given time span. If new requests come in instances will be recreated.
        /// </summary>
        public static void RemoveOlderThan(TimeSpan time)
        {
            ICircuitBreaker cb;

            foreach (var i in Instances)
            {
                if (i.Value.IsOlderThan(time))
                {
                    Instances.TryRemove(i.Key, out cb);
                }
            }
        }

        /// <summary>
        /// Clears all circuit breakers. If new requests come in instances will be recreated.
        /// </summary>
        internal static void Reset()
        {
            Instances.Clear();
        }
    }


    /// <summary>
    /// The default production implementation of <see cref="ICircuitBreaker"/>.
    /// </summary>
    internal class CircuitBreakerImpl : ICircuitBreaker
    {
        /// <summary>
        /// Stores the metrics of the owner command.
        /// </summary>
        private readonly CommandMetrics m_metrics;

        /// <summary>
        /// Stores the timer to use for tracking time. Enables using ITimeMocked for unit tests.
        /// </summary>
        ITime m_time;

        /// <summary>
        /// Stores the state of this circuit breaker.
        /// </summary>
        private AtomicBoolean m_circuitOpen = new AtomicBoolean(false);

        /// <summary>
        /// Stores the last time the circuit breaker was opened or tested.
        /// </summary>
        private AtomicLong m_circuitOpenedOrLastTestedTime = new AtomicLong(0);

        /// <summary>
        /// Stores the last time the circuit breaker was accessed.
        /// </summary>
        private AtomicLong m_circuitAccessedTime = new AtomicLong(0);

        /// <summary>
        /// Stores values use to calculate the randomized backoff.
        /// </summary>
        private AtomicLong m_attempt = new AtomicLong(0);
        internal AtomicLong m_backoffInMilliseconds = new AtomicLong(0);

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerImpl"/> class.
        /// </summary>
        /// <param name="properties">The properties of the owner command.</param>
        /// <param name="metrics">The metrics of the owner command.</param>
        internal CircuitBreakerImpl(ICommandProperties properties, CommandMetrics metrics, ITime time = null)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }

            if (metrics == null)
            {
                throw new ArgumentNullException("metrics");
            }

            m_time = time ?? ITimeDefault.Instance;
            m_metrics = metrics;
            ExecutionSemaphore = new TryableSemaphore();
            FallbackSemaphore = new TryableSemaphore();
            ExecutionRequests = new RollingNumber(m_time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
            FallbackRequests = new RollingNumber(m_time, properties.MetricsRollingStatisticalWindowInMilliseconds, properties.MetricsRollingStatisticalWindowBuckets);
        }

        /// <inheritdoc />
        public bool AllowRequest(ICommandProperties properties)
        {
            // update last accessed time so we can detect stale circuit breakers
            m_circuitAccessedTime.Value = m_time.GetCurrentTimeInMillis();

            if (properties.CircuitBreakerForceOpen)
            {
                // properties have asked us to force the circuit open so we will allow NO requests
                return false;
            }

            if (properties.CircuitBreakerForceClosed)
            {
                // we still want to allow IsOpen() to perform it's calculations so we simulate normal behavior
                IsOpen(properties);

                // properties have asked us to ignore errors so we will ignore the results of isOpen and just allow all traffic through
                return true;
            }

            return !IsOpen(properties) || AllowSingleTest(properties);
        }

        /// <inheritdoc />
        public bool IsOpen(ICommandProperties properties)
        {
            if (m_circuitOpen)
            {
                // if we're open we immediately return true and don't bother attempting to 'close' ourself as that is left to allowSingleTest and a subsequent successful test to close
                return true;
            }

            // we're closed, so let's see if errors have made us so we should trip the circuit open
            HealthCounts health = m_metrics.GetHealthCounts();

            // check if we are past the statisticalWindowVolumeThreshold
            if (health.TotalRequests < properties.CircuitBreakerRequestVolumeThreshold)
            {
                // we are not past the minimum volume threshold for the statisticalWindow so we'll return false immediately and not calculate anything
                return false;
            }

            // check to see if our error rate exceeds the allowable percentage
            if (health.ErrorPercentage < properties.CircuitBreakerErrorThresholdPercentage)
            {
                return false;
            }
            else
            {
                // our failure rate is too high, trip the circuit
                if (m_circuitOpen.CompareAndSet(false, true))
                {
                    // if the previousValue was false then we want to save the currentTime
                    // How could previousValue be true? If another thread was going through this code at the same time a race-condition could have
                    // caused another thread to set it to true already even though we were in the process of doing the same

                    // update this before the m_backoffInMilliseconds time so that we don't end up with a race condition in AllowSingleTest below
                    m_circuitOpenedOrLastTestedTime.Value = m_time.GetCurrentTimeInMillis();

                    // initialize our exponential m_backoffInMilliseconds values and ensure it is > 0 to prevent multiple AllowSingleTests through
                    m_attempt.Value = 1;
                    m_backoffInMilliseconds.Value = Math.Max(1, (long)BackoffTimerHelper.GetExponentialBackoff((int)m_attempt, properties.CircuitBreakerMinBackoff, properties.CircuitBreakerMaxBackoff, properties.CircuitBreakerDeltaBackoff).TotalMilliseconds);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the current state of the circuit breaker without re-evaluating the conditions
        /// </summary>
        /// <returns>CircuitBreakerStatus.Open, HalfOpen, or Closed</returns>
        public CircuitBreakerStatus GetCircuitBreakerState(ICommandProperties properties)
        {
            if (m_circuitOpen || properties.CircuitBreakerForceOpen)
            {
                if (properties.CircuitBreakerForceOpen || m_time.GetCurrentTimeInMillis() < m_circuitOpenedOrLastTestedTime + m_backoffInMilliseconds)
                {
                    return CircuitBreakerStatus.Open;
                }

                // the next request will be allowed as a test
                return CircuitBreakerStatus.HalfOpen;
            }
            else
            {
                return CircuitBreakerStatus.Closed;
            }
        }

        /// <summary>
        /// Gets whether the circuit breaker should permit a single test request.
        /// </summary>
        /// <returns>True if single test is permitted, otherwise false.</returns>
        private bool AllowSingleTest(ICommandProperties properties)
        {
            long timeCircuitOpenedOrWasLastTested = m_circuitOpenedOrLastTestedTime;

            // 1) if the circuit is open
            // 2) and it's been longer than 'm_backoffInMilliseconds' since we opened the circuit
            if (m_circuitOpen && m_time.GetCurrentTimeInMillis() > timeCircuitOpenedOrWasLastTested + m_backoffInMilliseconds)
            {
                // We push the 'circuitOpenedTime' ahead since we have allowed one request to try.
                // If it succeeds the circuit will be closed, otherwise another singleTest will be allowed at the end of the 'm_backoffInMilliseconds'.
                if (m_circuitOpenedOrLastTestedTime.CompareAndSet(timeCircuitOpenedOrWasLastTested, m_time.GetCurrentTimeInMillis()))
                {
                    // if this returns true that means we set the time so we'll return true to allow the singleTest
                    // if it returned false it means another thread raced us and allowed the singleTest before we did

                    // calculate the next exponential backoff value and ensure it is > 0 to prevent multiple AllowSingleTests through
                    m_backoffInMilliseconds.Value = Math.Max(1, ((long)BackoffTimerHelper.GetExponentialBackoff((int)m_attempt.IncrementAndGet(), properties.CircuitBreakerMinBackoff, properties.CircuitBreakerMaxBackoff, properties.CircuitBreakerDeltaBackoff).TotalMilliseconds));

                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public ITryableSemaphore ExecutionSemaphore { get; private set; }

        /// <inheritdoc />
        public ITryableSemaphore FallbackSemaphore { get; private set; }

        /// <inheritdoc />
        public IRollingNumber ExecutionRequests { get; private set; }

        /// <inheritdoc />
        public IRollingNumber FallbackRequests { get; private set; }

        /// <summary>
        /// Pull this logic out into a separate method to facilitate writing unit tests.
        /// </summary>
        internal void Reset()
        {
            // reset the retry count for the exponential backoff
            m_attempt.Value = 0;

            // If we have been 'open' and have a success then we want to close the circuit. This handles the 'singleTest' logic
            m_circuitOpen.Value = false;

            // Reset the statistical window otherwise it will still calculate the failure percentage below the threshold and immediately open the circuit again
            m_metrics.ResetCounter();
        }

        /// <inheritdoc />
        public void MarkSuccess()
        {
            if (m_circuitOpen)
            {
                Reset();
            }
        }

        /// <inheritdoc />
        public bool IsOlderThan(TimeSpan time)
        {
            // if the circuit is currently open, don't release it or we will lose the current timeout/retry state
            if (m_circuitOpen)
            {
                return false;
            }

            return m_time.GetCurrentTimeInMillis() - m_circuitAccessedTime > time.TotalMilliseconds;
        }
    }



    /// <summary>
    /// An implementation of the circuit breaker that does nothing.
    /// Used if circuit breaker is disabled for a command.
    /// </summary>
    internal class CircuitBreakerNoOpImpl : ICircuitBreaker
    {
        internal CircuitBreakerNoOpImpl()
        {
            ExecutionSemaphore = new TryableSemaphoreNoOpImpl();
            FallbackSemaphore = new TryableSemaphoreNoOpImpl();
            ExecutionRequests = new RollingNumberNoOpImpl();
            FallbackRequests = new RollingNumberNoOpImpl();
        }

        /// <inheritdoc />
        public bool AllowRequest(ICommandProperties properties)
        {
            return true;
        }

        /// <inheritdoc />
        public bool IsOpen(ICommandProperties properties)
        {
            return false;
        }

        /// <summary>
        /// Gets the current state of the circuit breaker without re-evaluating the conditions
        /// </summary>
        /// <returns>CircuitBreakerStatus.Open, HalfOpen, or Closed</returns>
        public CircuitBreakerStatus GetCircuitBreakerState(ICommandProperties properties)
        {
            return CircuitBreakerStatus.Closed;
        }

        /// <inheritdoc />
        public void MarkSuccess()
        {
        }

        /// <inheritdoc />
        public bool IsOlderThan(TimeSpan time)
        {
            return false;
        }

        /// <inheritdoc />
        public ITryableSemaphore ExecutionSemaphore { get; private set; }

        /// <inheritdoc />
        public ITryableSemaphore FallbackSemaphore { get; private set; }

        /// <inheritdoc />
        public IRollingNumber ExecutionRequests { get; private set; }

        /// <inheritdoc />
        public IRollingNumber FallbackRequests { get; private set; }
    }

    public enum CircuitBreakerStatus : byte
    {
        /// <summary>
        /// The circuit is closed
        /// </summary>
        Closed = 0,

        /// <summary>
        /// The circuit is half-open: short-circuiting most calls, but ready to allow a test call through
        /// </summary>
        HalfOpen = 1,

        /// <summary>
        /// The circuit is open and short-circuiting calls
        /// </summary>
        Open = 2
    }

    public static class CircuitBreaker
    {
        public static void Reset()
        {
            CommandMetrics.Reset();
            CircuitBreakerFactory.Reset();
        }
    }
}
