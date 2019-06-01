using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISecureFileStore
    {
        IList<SecureFileReference> GetAuthorizedReferences();

        SecureFile Get(SecureFileReference reference);

        /// <summary>
        /// Gets the <c>ISecureFileResolver</c> used by this store, if any.
        /// </summary>
        ISecureFileResolver Resolver { get; }
    }
}
