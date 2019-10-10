using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism of resolving an <c>ServiceEndpointReference</c> to a <c>ServiceEndpoint</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IServiceEndpointResolver
    {
        /// <summary>
        /// Adds the endpoint reference as authorized to ensure future retrievals of the endpoint
        /// are allowed regardless of security context.
        /// </summary>
        /// <param name="reference">The endpoint reference which should be considered authorized</param>
        void Authorize(ServiceEndpointReference reference);

        /// <summary>
        /// Attempts to resolve endpoint references to <c>ServiceEndpoint</c> instances.
        /// </summary>
        /// <param name="references">The endpoint references which should be resolved</param>
        /// <returns>The resolved service endpoints</returns>
        IList<ServiceEndpoint> Resolve(ICollection<ServiceEndpointReference> references);

        IList<ServiceEndpointReference> GetAuthorizedReferences();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IServiceEndpointResolverExtensions
    {
        /// <summary>
        /// Attempts to resolve the endpoint reference to a <c>ServiceEndpoint</c>.
        /// </summary>
        /// <param name="reference">The endpoint reference which should be resolved</param>
        /// <returns>The service endpoint if resolved; otherwise, null</returns>
        public static ServiceEndpoint Resolve(
            this IServiceEndpointResolver resolver,
            ServiceEndpointReference reference)
        {
            return resolver.Resolve(new[] { reference }).FirstOrDefault();
        }
    }
}
