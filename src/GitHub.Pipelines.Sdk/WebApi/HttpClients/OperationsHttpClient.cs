using Microsoft.VisualStudio.Services.Common;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Operations
{
    /// <summary>
    /// HttpClient for operations.
    /// </summary>
    public class OperationsHttpClient : OperationsHttpClientBase
    {

        public OperationsHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public OperationsHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OperationsHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OperationsHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OperationsHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// Get an operation by Id, this is here for back compat reason since the generated client rightfully append Async to the method name.
        /// </summary>
        /// <param name="id">The id of the operation.</param>
        /// <param name="userState"></param>
        /// <returns>The operation.</returns>
        public Task<Operation> GetOperation(Guid id, Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetOperationAsync(id, null, userState, cancellationToken);
        }

        public Task<Operation> GetOperationAsync(OperationReference operationReference, Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetOperationAsync(operationReference.Id, operationReference.PluginId, userState, cancellationToken);
        }

    }
}
