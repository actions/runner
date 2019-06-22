using System;
using GitHub.Services.Common;

namespace GitHub.Services.BlobStore.WebApi.Exceptions
{
    /// <summary>
    /// Exception thrown when no matching client tool was found
    /// </summary>
    public class ClientToolNotFoundException : VssServiceException
    {
        private ClientToolNotFoundException(string message)
            : base(message)
        {
        }

        public static ClientToolNotFoundException Create()
        {
            return new ClientToolNotFoundException(BlobStoreResources.ClientToolNoMatchingReleaseFound());
        }
    }
}
