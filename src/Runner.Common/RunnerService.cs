using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Sdk.WebApi.WebApi.RawClient;

namespace GitHub.Runner.Common
{

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceLocatorAttribute : Attribute
    {
        public static readonly string DefaultPropertyName = "Default";

        public Type Default { get; set; }
    }

    public interface IRunnerService
    {
        void Initialize(IHostContext context);
    }

    public abstract class RunnerService
    {
        protected IHostContext HostContext { get; private set; }
        protected Tracing Trace { get; private set; }

        public string TraceName
        {
            get
            {
                return GetType().Name;
            }
        }

        public virtual void Initialize(IHostContext hostContext)
        {
            HostContext = hostContext;
            Trace = HostContext.GetTrace(TraceName);
            Trace.Entering();
        }

        protected async Task<VssConnection> EstablishVssConnection(Uri serverUrl, VssCredentials credentials, TimeSpan timeout)
        {
            Trace.Info($"EstablishVssConnection");
            Trace.Info($"Establish connection with {timeout.TotalSeconds} seconds timeout.");
            int attemptCount = 5;
            while (attemptCount-- > 0)
            {
                var connection = VssUtil.CreateConnection(serverUrl, credentials, timeout: timeout);
                try
                {
                    await connection.ConnectAsync();
                    return connection;
                }
                catch (Exception ex) when (attemptCount > 0)
                {
                    Trace.Info($"Catch exception during connect. {attemptCount} attempt left.");
                    Trace.Error(ex);

                    await HostContext.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                }
            }

            // should never reach here.
            throw new InvalidOperationException(nameof(EstablishVssConnection));
        }

        protected async Task RetryRequest(Func<Task> func,
            CancellationToken cancellationToken,
            int maxRetryAttemptsCount = 5
        )
        {
            async Task<Unit> wrappedFunc()
            {
                await func();
                return Unit.Value;
            }
            await RetryRequest<Unit>(wrappedFunc, cancellationToken, maxRetryAttemptsCount);
        }
        
        protected async Task<T> RetryRequest<T>(Func<Task<T>> func,
            CancellationToken cancellationToken,
            int maxRetryAttemptsCount = 5
        )
        {
            var retryCount = 0;
            while (true)
            {
                retryCount++;
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await func();
                }
                // TODO: Add handling of non-retriable exceptions: https://github.com/github/actions-broker/issues/122
                catch (Exception ex) when (retryCount < maxRetryAttemptsCount)
                {
                    Trace.Error("Catch exception during request");
                    Trace.Error(ex);
                    var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                    Trace.Warning($"Back off {backOff.TotalSeconds} seconds before next retry. {maxRetryAttemptsCount - retryCount} attempt left.");
                    await Task.Delay(backOff, cancellationToken);
                }
            }
        }
    }
}
