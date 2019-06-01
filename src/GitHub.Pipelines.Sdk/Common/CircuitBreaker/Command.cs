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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    /// <summary>
    /// Provides an asynchronous CircuitBreaker command with support for Run and Fallback Tasks.  
    /// </summary>
    public class CommandAsync
    {
        public const String DontTriggerCircuitBreaker = "{421AC3F1-A306-4C9B-B3F6-5812F9121FC8}";


        internal protected readonly ITime m_time;
        internal protected readonly IEventNotifier m_eventNotifier;
        internal protected readonly ICircuitBreaker m_circuitBreaker;
        internal protected readonly ICommandProperties m_properties;
        internal protected readonly CommandMetrics m_metrics;

        internal protected readonly CommandKey m_CommandKey;
        internal protected readonly CommandGroupKey m_CommandGroup;

        private readonly Func<Task> m_Run;
        private readonly Func<Task> m_Fallback;
        protected readonly bool m_ContinueOnCapturedContext;

        // used to track whenever the user invokes the command using execute() ... also used to know if execution has begun
        internal protected long invocationStartTime = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAsync"/> class.
        /// </summary>
        /// <param name="group">Used to group multiple command metrics.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        public CommandAsync(CommandGroupKey group, Func<Task> run, Func<Task> fallback = null, bool continueOnCapturedContext = false)
            : this(new CommandSetter(group), run, fallback, continueOnCapturedContext)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAsync"/> class.
        /// </summary>
        /// <param name="setter">Enables setting command defaults in code.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        public CommandAsync(CommandSetter setter, Func<Task> run, Func<Task> fallback = null, bool continueOnCapturedContext = false)
            : this(setter.GroupKey, setter.CommandKey, null, new CommandPropertiesDefault(setter.CommandPropertiesDefaults), null, null, run, fallback, continueOnCapturedContext)
        {
            if (run == null)
                throw new ArgumentNullException("run");
        }

        protected CommandAsync(CommandGroupKey group, CommandKey key, ICircuitBreaker circuitBreaker, ICommandProperties properties, CommandMetrics metrics, IEventNotifier eventNotifier, Func<Task> run, Func<Task> fallback, bool continueOnCapturedContext, ITime time = null)
        {
            /*
             * CommandGroup initialization
             */
            if (group == null)
                throw new ArgumentNullException("group");

            m_CommandGroup = group;

            /*
             * CommandKey initialization
             */
            m_CommandKey = key ?? new CommandKey(GetType());

            /*
             * Properties initialization
             */
            m_properties = properties;

            /*
             * EventNotifier initialization
             */
            m_eventNotifier = eventNotifier ?? EventNotifierDefault.Instance;

            /*
             * Time initialization
             */
            m_time = time ?? ITimeDefault.Instance;

            /*
             * Metrics initialization
             */
            m_metrics = metrics ?? CommandMetrics.GetInstance(m_CommandKey, m_CommandGroup, m_properties, m_eventNotifier, m_time);

            /*
             * CircuitBreaker initialization
             */
            if (!m_properties.CircuitBreakerDisabled)
            {
                m_circuitBreaker = circuitBreaker ?? CircuitBreakerFactory.GetInstance(m_CommandKey, m_properties, m_metrics, m_time);
            }
            else
            {
                m_circuitBreaker = new CircuitBreakerNoOpImpl();
            }

            // Save the Run/Fallback delegates and async parameters
            m_Run = run;
            m_Fallback = fallback;
            m_ContinueOnCapturedContext = continueOnCapturedContext;
        }

        /// <returns><see cref="CommandGroupKey"/> used to group together multiple <see cref="CommandAsync"/> objects.</returns>
        public CommandGroupKey CommandGroup { get { return m_CommandGroup; } }

        /// <returns><see cref="CommandKey"/> identifying this command instance for circuit-breaker, metrics and properties.</returns>
        public CommandKey CommandKey { get { return m_CommandKey; } }

        internal ICircuitBreaker CircuitBreaker { get { return m_circuitBreaker; } }

        /// <returns>The <see cref="CommandMetrics"/> associated with this <see cref="CommandAsync"/> instance.</returns>
        public CommandMetrics Metrics { get { return m_metrics; } }

        /// <returns>The <see cref="ICommandProperties"/> associated with this <see cref="CommandAsync"/> instance.</returns>
        public ICommandProperties Properties { get { return m_properties; } }

        /// <returns>Returns true if the associated circuit breaker is open.</returns>
        public bool IsCircuitBreakerOpen { get { return m_circuitBreaker.IsOpen(m_properties); } }

        /// <returns>Returns CircuitBreakerStatus.Open, HalfOpen, or Closed</returns>
        public CircuitBreakerStatus CircuitBreakerState { get { return m_circuitBreaker.GetCircuitBreakerState(m_properties); } }

        /// <summary>
        /// Used for asynchronous execution of command.
        /// </summary>
        /// <returns>Task</returns>
        public Task Execute()
        {
            return Execute(m_Run, m_Fallback, m_ContinueOnCapturedContext);
        }

        /// <summary>
        /// Used for asynchronous execution of command.
        /// </summary>
        /// <param name="run">The action that needs to be executed</param>
        /// <param name="fallback">Fall back action if main action fails</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        /// <returns></returns>
        protected async Task Execute(Func<Task> run, Func<Task> fallback, bool continueOnCapturedContext)
        {
            // Check that this command instance hasn't been used before
            if (Interlocked.CompareExchange(ref invocationStartTime, m_time.GetCurrentTimeInMillis(), -1) != -1)
            {
                throw new InvalidOperationException("This instance can only be executed once. Please instantiate a new instance.");
            }

            // determine if we're allowed to execute
            if (m_circuitBreaker.AllowRequest(m_properties))
            {
                // acquire a concurrency permit
                if (m_circuitBreaker.ExecutionSemaphore.TryAcquire(m_properties.ExecutionMaxConcurrentRequests))
                {
                    try
                    {
                        // ensure we haven't exceeded our executions per window limit 
                        // for performance reasons, this is a lock-less operation so doesn't guarantee exact limits – it can vary as much as there are threads concurrently executing
                        Boolean ExceededRequestLimit = false;
                        if (m_properties.ExecutionMaxRequests != int.MaxValue)
                        {
                            if (m_circuitBreaker.ExecutionRequests.GetRollingSum() < m_properties.ExecutionMaxRequests)
                            {
                                m_circuitBreaker.ExecutionRequests.Increment();
                            }
                            else
                            {
                                ExceededRequestLimit = true;
                            }
                        }
                        if (!ExceededRequestLimit)
                        {
                            try
                            {
                                m_eventNotifier.MarkExecutionConcurrency(CommandGroup, CommandKey, m_circuitBreaker.ExecutionSemaphore.GetNumberOfPermitsUsed());
                                m_eventNotifier.MarkExecutionCount(CommandGroup, CommandKey, m_circuitBreaker.ExecutionRequests.GetRollingSum());

                                // execute the Run command
                                invocationStartTime = m_time.GetCurrentTimeInMillis();
                                try
                                {
                                    await run().ConfigureAwait(continueOnCapturedContext);
                                }
                                catch (AggregateException ex)
                                {
                                    // Un-aggregate
                                    ExceptionDispatchInfo.Capture(ex).Throw();
                                }
                                long elapsedTimeInMilliseconds = m_time.GetCurrentTimeInMillis() - invocationStartTime;

                                // if the run time exceeded the timeout value
                                if (elapsedTimeInMilliseconds > m_properties.ExecutionTimeout.TotalMilliseconds)
                                {
                                    // report timeout
                                    m_metrics.MarkTimeout();
                                }
                                else
                                {
                                    // report success and reset the circuit breaker
                                    m_metrics.MarkSuccess();
                                    m_circuitBreaker.MarkSuccess();
                                    m_eventNotifier.MarkCommandExecution(CommandGroup, CommandKey, elapsedTimeInMilliseconds);
                                }
                            }
                            catch (Exception e)
                            {
                                // if DontTriggerCircuitBreaker is set then propagate the origional exception without any stats tracking or fallback logic
                                if (e.Data.Contains(DontTriggerCircuitBreaker))
                                {
                                    // remove this flag so that other circuit breakers will still execute fallback logic
                                    e.Data.Remove(DontTriggerCircuitBreaker);
                                    throw;
                                }
                                else
                                {
                                    // mark execution failure and save the exception for later diagnosis
                                    m_metrics.MarkFailure();
                                    m_metrics.LastException = e;

                                    String errorMessage = "failed executing run delegate due to an exception";
                                    Trace(CircuitBreakerTracePoints.ExecutionFailure, TraceLevel.Error, s_featurearea, s_classname, CircuitBreakerErrorMessage(errorMessage, m_metrics.LastException));

                                    // attempt the fallback and if it fails, throw the origional exception
                                    if (!await TryFallbackAsync(errorMessage, fallback, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext)) 
                                    {
                                        throw;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // mark execution limit rejection
                            m_metrics.MarkLimitRejected();

                            String errorMessage = "exceeded the execution limit of " + m_properties.ExecutionMaxRequests;
                            String circuitBreakerErrorMessage = CircuitBreakerErrorMessage(errorMessage);
                            Trace(CircuitBreakerTracePoints.ExecutionLimitRejection, TraceLevel.Error, s_featurearea, s_classname, circuitBreakerErrorMessage);

                            // Try the fallback and if it fails, throw a CircuitBreakerExceededExecutionLimitException
                            if (!await TryFallbackAsync(errorMessage, fallback, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext))
                            {
                                throw new CircuitBreakerExceededExecutionLimitException(circuitBreakerErrorMessage);
                            }
                        }
                    }
                    finally
                    {
                        m_circuitBreaker.ExecutionSemaphore.Release();
                    }
                }
                else
                {
                    // mark execution concurrency rejection
                    m_metrics.MarkConcurrencyRejected();

                    String errorMessage = "exceeded the concurrency limit of " + m_properties.ExecutionMaxConcurrentRequests;
                    String circuitBreakerErrorMessage = CircuitBreakerErrorMessage(errorMessage);
                    Trace(CircuitBreakerTracePoints.ExecutionConcurrencyRejection, TraceLevel.Error, s_featurearea, s_classname, circuitBreakerErrorMessage);

                    // Try the fallback and if it fails, throw a CircuitBreakerRejectedSemaphoreExecutionException
                    if (!await TryFallbackAsync(errorMessage, fallback, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext))
                    {
                        throw new CircuitBreakerExceededConcurrencyException(circuitBreakerErrorMessage);
                    }
                }
            }
            else
            {
                // record that we are returning a short-circuited fallback
                m_metrics.MarkShortCircuited();

                String errorMessage = "short-circuited";
                String circuitBreakerErrorMessage = CircuitBreakerErrorMessage(errorMessage, m_metrics.LastException);
                Trace(CircuitBreakerTracePoints.ShortCircuited, TraceLevel.Error, s_featurearea, s_classname, circuitBreakerErrorMessage);

                // Try the fallback and have it throw any exception generated
                if (!await TryFallbackAsync(errorMessage, fallback, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext))
                {
                    // the fallback failed so throw an exception
                    throw new CircuitBreakerShortCircuitException(circuitBreakerErrorMessage);
                }
            }
        }

        protected virtual void Trace(int tracepoint, TraceLevel level, string featurearea, string classname, string message)
        {
            m_eventNotifier.TraceRaw(tracepoint, level, featurearea, classname, message);
        }

        /// <summary>
        /// Provides a CircuitBreaker command with support for Func<TResult> Run and Fallback methods.  
        /// </summary>
        /// <param name="message"></param>
        /// <param name="fallback">The Action delegate to execute.</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        /// <returns>true if the fallback is executed without error.</returns>
        private async Task<Boolean> TryFallbackAsync(String message, Func<Task> fallback, bool continueOnCapturedContext)
        {
            // if there is no fallback, fail fast
            if (fallback == default(Func<Task>))
            {
                return false;
            }

            // if the fallback is not disabled
            if (!m_properties.FallbackDisabled)
            {
                // acquire a concurrency permit
                if (m_circuitBreaker.FallbackSemaphore.TryAcquire(m_properties.FallbackMaxConcurrentRequests))
                {
                    try
                    {
                        // ensure we haven't exceeded our fallback per window limit
                        // for performance reasons, this is a lock-less operation so doesn't guarantee exact limits – it can vary as much as there are threads concurrently executing
                        Boolean ExceededRequestLimit = false;
                        if (m_properties.FallbackMaxRequests != int.MaxValue)
                        {
                            if (m_circuitBreaker.FallbackRequests.GetRollingSum() < m_properties.FallbackMaxRequests)
                            {
                                m_circuitBreaker.FallbackRequests.Increment();
                            }
                            else
                            {
                                ExceededRequestLimit = true;
                            }
                        }
                        if (!ExceededRequestLimit)
                        {
                            // fallback behavior is permitted so attempt
                            try
                            {
                                m_eventNotifier.MarkFallbackConcurrency(CommandGroup, CommandKey, m_circuitBreaker.FallbackSemaphore.GetNumberOfPermitsUsed());
                                m_eventNotifier.MarkFallbackCount(CommandGroup, CommandKey, m_circuitBreaker.FallbackRequests.GetRollingSum());

                                // try the fallback
                                try
                                {
                                    await fallback().ConfigureAwait(continueOnCapturedContext);
                                }
                                catch (AggregateException ex)
                                {
                                    // Un-aggregate
                                    ExceptionDispatchInfo.Capture(ex).Throw();
                                }
                                m_metrics.MarkFallbackSuccess();

                                // return success
                                return true;
                            }
                            catch (Exception fe)
                            {
                                // if DontTriggerCircuitBreaker is set then propagate the original exception without any stats tracking or fallback logic
                                if (!fe.Data.Contains(DontTriggerCircuitBreaker))
                                {
                                    // mark fallback failure
                                    m_metrics.MarkFallbackFailure();

                                    String errorMessage = message + " and failed executing fallback delegate due to an exception";
                                    Trace(CircuitBreakerTracePoints.FallbackFailure, TraceLevel.Error, s_featurearea, s_classname, CircuitBreakerErrorMessage(errorMessage, fe));
                                }
                            }
                        }
                        else
                        {
                            // mark fallback limit rejection
                            m_metrics.MarkFallbackLimitRejected();

                            String errorMessage = message + " and fallback exceeded the fallback limit of " + m_properties.FallbackMaxRequests;
                            Trace(CircuitBreakerTracePoints.FallbackLimitRejection, TraceLevel.Warning, s_featurearea, s_classname, CircuitBreakerErrorMessage(errorMessage));
                        }
                    }
                    finally
                    {
                        m_circuitBreaker.FallbackSemaphore.Release();
                    }
                }
                else
                {
                    // mark fallback concurrency rejection
                    m_metrics.MarkFallbackConcurrencyRejected();

                    String errorMessage = message + " and fallback exceeded the concurrency limit of " + m_properties.FallbackMaxConcurrentRequests;
                    Trace(CircuitBreakerTracePoints.FallbackConcurrencyRejection, TraceLevel.Warning, s_featurearea, s_classname, CircuitBreakerErrorMessage(errorMessage));
                }
            }
            else
            {
                String errorMessage = message + " and fallback disabled";
                Trace(CircuitBreakerTracePoints.FallbackDisabled, TraceLevel.Warning, s_featurearea, s_classname, CircuitBreakerErrorMessage(errorMessage));
            }

            // return failure
            return false;
        }

        private String CircuitBreakerErrorMessage(String message, Exception e = null)
        {
            String errorMessage = String.Format(
                        CultureInfo.CurrentCulture,
                        "Circuit Breaker \"{0}\" {1}. In the last {2} milliseconds, there were: {3} failure, {4} timeout, {5} short circuited, {6} concurrency rejected, and {7} limit rejected.",
                        m_CommandKey.Name,
                        message,
                        m_properties.MetricsRollingStatisticalWindowInMilliseconds,
                        m_metrics.failure.GetRollingSum(),
                        m_metrics.timeout.GetRollingSum(),
                        m_metrics.shortCircuited.GetRollingSum(),
                        m_metrics.concurrencyRejected.GetRollingSum(),
                        m_metrics.limitRejected.GetRollingSum());
            if (e != null)
                errorMessage = errorMessage + $" Last exception: {e.GetType().FullName} {e.Message}";
            return errorMessage;
        }

        private static string s_classname = "Command";
        private static string s_featurearea = "CircuitBreaker";
    }


    /// <summary>
    /// Provides an asynchronous CircuitBreaker command with support for Func<TResult> Run and Fallback Tasks.  
    /// </summary>
    public class CommandAsync<TResult> : CommandAsync
    {
        private readonly Func<Task<TResult>> m_Run;
        private readonly Func<Task<TResult>> m_Fallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandService<TResult>"/> class.
        /// </summary>
        /// <param name="group">Used to group multiple command metrics.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        public CommandAsync(CommandGroupKey group, Func<Task<TResult>> run, Func<Task<TResult>> fallback = null, bool continueOnCapturedContext = false)
            : this(new CommandSetter(group), run, fallback, continueOnCapturedContext)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandService<TResult>"/> class.
        /// </summary>
        /// <param name="setter">Enables setting command defaults in code.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        public CommandAsync(CommandSetter setter, Func<Task<TResult>> run, Func<Task<TResult>> fallback = null, bool continueOnCapturedContext = false)
            : this(setter.GroupKey, setter.CommandKey, null, new CommandPropertiesDefault(setter.CommandPropertiesDefaults), null, null, run, fallback, continueOnCapturedContext)
        {
            if (run == null)
                throw new ArgumentNullException("run");
        }
        protected internal CommandAsync(CommandGroupKey group, CommandKey key, ICircuitBreaker circuitBreaker, ICommandProperties properties, CommandMetrics metrics, IEventNotifier eventNotifier, Func<Task<TResult>> run, Func<Task<TResult>> fallback, bool continueOnCapturedContext, ITime time = null)
            : base(group, key, circuitBreaker, properties, metrics, eventNotifier, null, null, continueOnCapturedContext, time)
        {
            m_Run = run;
            m_Fallback = fallback;
        }

        /// <summary>
        /// Used for asynchronous execution of command.
        /// </summary>
        /// <returns>TResult Result of run or fallback delegate if the command fails for any reason.</returns>
        public new async Task<TResult> Execute()
        {
            TResult result = default(TResult);
            Func<Task> fallback = null;
            if(m_Fallback != null)
            {
                fallback = async () =>
                {
                    result = await m_Fallback().ConfigureAwait(m_ContinueOnCapturedContext);
                };
            }

            await base.Execute(async () =>
            {
                result = await m_Run().ConfigureAwait(m_ContinueOnCapturedContext);
            }, 
            fallback, m_ContinueOnCapturedContext).ConfigureAwait(m_ContinueOnCapturedContext);
            return result;
        }
    }


    /// <summary>
    /// Provides a synchronous CircuitBreaker command with support for Run and Fallback Actions
    /// </summary>
    public class Command : CommandAsync
    {
        private readonly Action m_Run;
        private readonly Action m_Fallback;
        private static Task DummyTask = Task.FromResult(true);

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandService<TResult>"/> class.
        /// </summary>
        /// <param name="group">Used to group multiple command metrics.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        public Command(CommandGroupKey group, Action run, Action fallback = null)
            : this(new CommandSetter(group), run, fallback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandService<TResult>"/> class.
        /// </summary>
        /// <param name="setter">Enables setting command defaults in code.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        public Command(CommandSetter setter, Action run, Action fallback = null)
            : this(setter.GroupKey, setter.CommandKey, null, new CommandPropertiesDefault(setter.CommandPropertiesDefaults), null, null, run, fallback)
        {
            if (run == null)
                throw new ArgumentNullException("run");
        }

        protected internal Command(CommandGroupKey group, CommandKey key, ICircuitBreaker circuitBreaker, ICommandProperties properties, CommandMetrics metrics, IEventNotifier eventNotifier, Action run, Action fallback = null, ITime time = null)
            : base(group, key, circuitBreaker, properties, metrics, eventNotifier, null, null, false, time)
        {
            // Save the Run/Fallback delegates
            m_Run = run;
            m_Fallback = fallback;
        }

        /// <summary>
        /// Used for synchronous execution of command.
        /// </summary>
        /// <returns></returns>
        public new void Execute()
        {
            Execute(m_Run, m_Fallback);
        }

        /// <summary>
        /// Used to convert asynchronous execution of command to synchronous execution.
        /// </summary>
        /// <param name="run">The action that needs to be executed</param>
        /// <param name="fallback">Fall back action if main action fails</param>
        /// <returns></returns>
        protected void Execute(Action run, Action fallback)
        {
            try
            {
                base.Execute(() =>
                {
                    run();
                    return DummyTask;
                },
                fallback != default(Action) ? () =>
                {
                    fallback();
                    return DummyTask;
                } : default(Func<Task>), true).Wait();
            }
            catch (AggregateException ex)
            {
                // Catch Aggregate exception and throws the inner exception that might be expected by the callers
                // Task ends up creating an aggregate exception
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }
    }

    /// <summary>
    /// Provides a synchronous CircuitBreaker command with support for Func<TResult> Run and Fallback delegates.  
    /// </summary>
    public class Command<TResult> : Command
    {
        private readonly Func<TResult> m_Run;
        private readonly Func<TResult> m_Fallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandService<TResult>"/> class.
        /// </summary>
        /// <param name="group">Used to group multiple command metrics.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        public Command(CommandGroupKey group, Func<TResult> run, Func<TResult> fallback = null)
            : this(new CommandSetter(group), run, fallback)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandService<TResult>"/> class.
        /// </summary>
        /// <param name="setter">Enables setting command defaults in code.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        public Command(CommandSetter setter, Func<TResult> run, Func<TResult> fallback = null)
            : this(setter.GroupKey, setter.CommandKey, null, new CommandPropertiesDefault(setter.CommandPropertiesDefaults), null, null, run, fallback)
        {
            if (run == null)
                throw new ArgumentNullException("run");
        }
        protected internal Command(CommandGroupKey group, CommandKey key, ICircuitBreaker circuitBreaker, ICommandProperties properties, CommandMetrics metrics, IEventNotifier eventNotifier, Func<TResult> run, Func<TResult> fallback = null, ITime time = null)
            : base(group, key, circuitBreaker, properties, metrics, eventNotifier, null, null, time)
        {
            m_Run = run;
            m_Fallback = fallback;
        }

        /// <summary>
        /// Used for synchronous execution of command.
        /// </summary>
        /// <returns>TResult Result of run or fallback delegate if the command fails for any reason.</returns>
        public new TResult Execute()
        {
            TResult result = default(TResult);
            base.Execute(() => { result = m_Run(); }, m_Fallback != null ? () => { result = m_Fallback(); } : default(Action));
            return result;
        }
    }
}
