using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism of resolving an <c>SecureFileReference</c> to a <c>SecureFile</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISecureFileResolver
    {
        /// <summary>
        /// Attempts to resolve secure file references to a <c>SecureFile</c> instances.
        /// </summary>
        /// <param name="reference">The file references which should be resolved</param>
        /// <returns>The resolved secure files</returns>
        IList<SecureFile> Resolve(ICollection<SecureFileReference> references);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ISecureFileResolverExtensions
    {
        /// <summary>
        /// Attempts to resolve the secure file reference to a <c>SecureFile</c>.
        /// </summary>
        /// <param name="reference">The file reference which should be resolved</param>
        /// <returns>The secure file if resolved; otherwise, null</returns>
        public static SecureFile Resolve(
            this ISecureFileResolver resolver,
            SecureFileReference reference)
        {
            return resolver.Resolve(new[] { reference }).FirstOrDefault();
        }
    }
}
