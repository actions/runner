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
            var retryHelper = new RetryHelper(Trace, new RetryStrategy
            {
                MaxAttempts = 5,
                GetBackoff = RetryBackoffs.Fixed(TimeSpan.FromMilliseconds(100)),
                OnRetry = (context, ex, _) =>
                {
                    Trace.Info($"Catch exception during connect. {context.MaxAttempts - context.AttemptNumber} attempt left.");
                    Trace.Error(ex);
                },
            });

            return await retryHelper.ExecuteAsync(
                operationName: nameof(EstablishVssConnection),
                operation: async () =>
                {
                    var connection = VssUtil.CreateConnection(serverUrl, credentials, timeout: timeout);
                    await connection.ConnectAsync();
                    return connection;
                });
        }

        protected async Task RetryRequest(Func<Task<OperationOutcome<bool>>> func,
            CancellationToken cancellationToken,
            int maxAttempts = 5
        )
        {
            await RetryRequest<bool>(func, cancellationToken, maxAttempts);
        }

        protected async Task<T> RetryRequest<T>(Func<Task<OperationOutcome<T>>> func,
            CancellationToken cancellationToken,
            int maxAttempts = 5
        )
        {
            var retryHelper = new RetryHelper(Trace, new RetryStrategy
            {
                MaxAttempts = maxAttempts,
                GetBackoff = RetryBackoffs.Random(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)),
                OnRetry = (context, _, backoff) =>
                {
                    Trace.Warning($"Transient failure during request, retrying. Attempt {context.AttemptNumber}/{context.MaxAttempts}. Back off {backoff.TotalSeconds} seconds.");
                },
            });

            return await retryHelper.ExecuteAsync<T>("RetryRequest", func, cancellationToken);
        }
    }
}
