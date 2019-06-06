using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides access to service endpoints which are referenced within a pipeline.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IServiceEndpointStore
    {
        /// <summary>
        /// Retrieves the list of all endpoints authorized for use in this store.
        /// </summary>
        /// <returns>The list of <c>ServiceEndpointReference</c> objects authorized for use</returns>
        IList<ServiceEndpointReference> GetAuthorizedReferences();

        /// <summary>
        /// Adds an endpoint reference which should be considered authorized. Future
        /// calls to retrieve this resource will be treated as pre-authorized regardless
        /// of authorization context used.
        /// </summary>
        /// <param name="endpoint">The endpoint which should be authorized</param>
        void Authorize(ServiceEndpointReference endpoint);

        /// <summary>
        /// Attempts to authorize an endpoint for use.
        /// </summary>
        /// <param name="endpoint">The endpoint reference to be resolved</param>
        /// <returns>The endpoint if found and authorized; otherwise, null</returns>
        ServiceEndpoint Get(ServiceEndpointReference endpoint);

        /// <summary>
        /// Gets the <c>IServiceEndpointResolver</c> used by this store, if any.
        /// </summary>
        IServiceEndpointResolver Resolver { get; }
    }
}
