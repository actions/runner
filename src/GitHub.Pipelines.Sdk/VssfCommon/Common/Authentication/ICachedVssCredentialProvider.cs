using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Common
{
    public interface ICachedVssCredentialProvider
    {
        /// <summary>
        /// Retrieve a VssCredentials object representing the cached credentials for the specified URI in this particular provider's cache implementation.
        /// If the provider 'knows about' the target URI (it has a cache entry) then knownUri will be set to true.
        /// A return result of null and knownUri == true indicates that the provider has a cache entry for the target URI but was unable to acquire
        /// a valid credential for it. Probably this incidates the user was presented an auth challenge and failed or canceled.
        /// </summary>
        /// <param name="uri">URI of the host for which to find a cached credential</param>
        /// <param name="knownUri">set to true if the target URI was in the cache's known URIs list</param>
        /// <returns>null if no credential associated with the specified URI is found in the cache</returns>
        VssCredentials GetCachedCredentials(Uri uri, out bool knownUri);
    }
}
