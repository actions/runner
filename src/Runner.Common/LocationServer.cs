using System;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using GitHub.Services.Location.Client;
using GitHub.Services.Location;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(LocationServer))]
    public interface ILocationServer : IRunnerService
    {
        Task ConnectAsync(VssConnection jobConnection);

        Task<ConnectionData> GetConnectionDataAsync();
    }

    public sealed class LocationServer : RunnerService, ILocationServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private LocationHttpClient _locationClient;

        public async Task ConnectAsync(VssConnection jobConnection)
        {
            _connection = jobConnection;
            if (!_connection.HasAuthenticated)
            {
                var retryHelper = new RetryHelper(Trace, new RetryStrategy
                {
                    MaxAttempts = 5,
                    GetBackoff = (_, _, _) => TimeSpan.FromMilliseconds(100),
                    OnRetry = (context, ex, _) =>
                    {
                        Trace.Info($"Catch exception during connect. {context.MaxAttempts - context.AttemptNumber} attempt left.");
                        Trace.Error(ex);
                    },
                });

                await retryHelper.ExecuteAsync(
                    operationName: nameof(ConnectAsync),
                    operation: async () =>
                    {
                        await _connection.ConnectAsync();
                    });
            }

            _locationClient = _connection.GetClient<LocationHttpClient>();
            _hasConnection = true;
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }

        public async Task<ConnectionData> GetConnectionDataAsync()
        {
            CheckConnection();
            return await _locationClient.GetConnectionDataAsync(ConnectOptions.None, 0);
        }
    }
}
