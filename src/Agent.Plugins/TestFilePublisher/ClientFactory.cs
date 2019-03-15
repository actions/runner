using Microsoft.VisualStudio.Services.WebApi;

namespace Agent.Plugins.Log.TestFilePublisher
{
    public interface IClientFactory
    {
        /// <summary>
        /// Access any pipeline client through factory
        /// </summary>
        T GetClient<T>() where T : VssHttpClientBase;
    }

    public class ClientFactory : IClientFactory
    {
        public ClientFactory(VssConnection vssConnection)
        {
            _vssConnection = vssConnection;
        }

        /// <inheritdoc />
        public T GetClient<T>() where T : VssHttpClientBase
        {
            return _vssConnection.GetClient<T>();
        }

        private readonly VssConnection _vssConnection;
    }
}
