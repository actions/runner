using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common
{
    public sealed class RetryExecutionContext
    {
        public RetryExecutionContext(string operationName, int attemptNumber, int maxAttempts, DateTime startTimeUtc)
        {
            OperationName = operationName;
            AttemptNumber = attemptNumber;
            MaxAttempts = maxAttempts;
            StartTimeUtc = startTimeUtc;
        }

        public string OperationName { get; }
        public int AttemptNumber { get; }
        public int MaxAttempts { get; }
        public DateTime StartTimeUtc { get; }
    }

    public sealed class RetryStrategy
    {
        public int MaxAttempts { get; init; }
        public Func<int, int, Exception, TimeSpan> GetBackoff { get; init; }
        public Action<RetryExecutionContext, Exception, TimeSpan> OnRetry { get; init; }
        public Action<RetryExecutionContext, TimeSpan> OnSuccess { get; init; }
        public Action<RetryExecutionContext, Exception, TimeSpan> OnFailure { get; init; }
    }

    public abstract record OperationOutcome<T>
    {
        public sealed record Success(T Value) : OperationOutcome<T>;
        public sealed record TransientFailure(string Reason) : OperationOutcome<T>;
    }

    public static class OperationOutcome
    {
        public static OperationOutcome<T> Success<T>(T value) => new OperationOutcome<T>.Success(value);

        public static OperationOutcome<T> TransientFailure<T>(string reason) => new OperationOutcome<T>.TransientFailure(reason);
    }

    public sealed class RetryHelper
    {
        private readonly Tracing _trace;
        private readonly RetryStrategy _strategy;

        public RetryHelper(Tracing trace, RetryStrategy strategy)
        {
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

            ArgUtil.NotNull(strategy.GetBackoff, $"{nameof(strategy)}.{nameof(strategy.GetBackoff)}");

            if (strategy.MaxAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(strategy)}.{nameof(strategy.MaxAttempts)}");
            }
        }

        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync("operation", operation, cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(
            Func<RetryExecutionContext, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync("operation", operation, cancellationToken);
        }

        public async Task ExecuteAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            ArgUtil.NotNull(operation, nameof(operation));
            await ExecuteAsync(
                "operation",
                async _ =>
                {
                    await operation();
                    return true;
                },
                cancellationToken);
        }

        public async Task ExecuteAsync(
            Func<RetryExecutionContext, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync("operation", operation, cancellationToken);
        }

        public async Task ExecuteAsync(
            string operationName,
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            ArgUtil.NotNull(operation, nameof(operation));
            await ExecuteAsync(
                operationName,
                async _ =>
                {
                    await operation();
                    return true;
                },
                cancellationToken);
        }

        public async Task ExecuteAsync(
            string operationName,
            Func<RetryExecutionContext, Task> operation,
            CancellationToken cancellationToken = default)
        {
            ArgUtil.NotNull(operation, nameof(operation));
            await ExecuteAsync(
                operationName,
                async context =>
                {
                    await operation(context);
                    return true;
                },
                cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(
            string operationName,
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgUtil.NotNull(operation, nameof(operation));
            return await ExecuteAsync(operationName, _ => operation(), cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(
            string operationName,
            Func<RetryExecutionContext, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgUtil.NotNull(operation, nameof(operation));

            operationName ??= "operation";

            var attempt = 0;
            var startTimeUtc = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                attempt++;
                cancellationToken.ThrowIfCancellationRequested();
                var context = new RetryExecutionContext(operationName, attempt, _strategy.MaxAttempts, startTimeUtc);

                try
                {
                    var result = await operation(context);
                    _strategy.OnSuccess?.Invoke(context, stopwatch.Elapsed);
                    return result;
                }
                catch (Exception ex)
                {
                    if (attempt >= _strategy.MaxAttempts)
                    {
                        _trace.Error($"[{operationName}] retry exhausted at attempt {attempt}/{_strategy.MaxAttempts}");
                        _strategy.OnFailure?.Invoke(context, ex, stopwatch.Elapsed);
                        throw;
                    }

                    var backoff = _strategy.GetBackoff(attempt, _strategy.MaxAttempts, ex);
                    _strategy.OnRetry?.Invoke(context, ex, backoff);
                    await Task.Delay(backoff, cancellationToken);
                }
            }
        }
        public async Task<T> ExecuteAsync<T>(
            string operationName,
            Func<Task<OperationOutcome<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgUtil.NotNull(operation, nameof(operation));
            return await ExecuteAsync<T>(operationName, _ => operation(), cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(
            string operationName,
            Func<RetryExecutionContext, Task<OperationOutcome<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgUtil.NotNull(operation, nameof(operation));

            operationName ??= "operation";

            var attempt = 0;
            var startTimeUtc = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            string lastFailureReason = null;

            while (true)
            {
                attempt++;
                cancellationToken.ThrowIfCancellationRequested();
                var context = new RetryExecutionContext(operationName, attempt, _strategy.MaxAttempts, startTimeUtc);

                var outcome = await operation(context);

                if (outcome is OperationOutcome<T>.Success success)
                {
                    _strategy.OnSuccess?.Invoke(context, stopwatch.Elapsed);
                    return success.Value;
                }

                lastFailureReason = ((OperationOutcome<T>.TransientFailure)outcome).Reason;

                if (attempt >= _strategy.MaxAttempts)
                {
                    _trace.Error($"[{operationName}] retry exhausted at attempt {attempt}/{_strategy.MaxAttempts}: {lastFailureReason}");
                    _strategy.OnFailure?.Invoke(context, null, stopwatch.Elapsed);
                    throw new InvalidOperationException($"[{operationName}] failed after {attempt} attempt(s): {lastFailureReason}");
                }

                var backoff = _strategy.GetBackoff(attempt, _strategy.MaxAttempts, null);
                _strategy.OnRetry?.Invoke(context, null, backoff);
                await Task.Delay(backoff, cancellationToken);
            }
        }
    }
}
