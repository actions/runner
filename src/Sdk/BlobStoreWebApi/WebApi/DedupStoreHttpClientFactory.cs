using System;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    public static class DedupStoreHttpClientFactory
    {
        public static IDedupStoreHttpClient GetClient(Uri dedupServiceUri, ArtifactHttpClientFactory clientFactory)
        {
            return (IDedupStoreHttpClient)clientFactory.CreateVssHttpClient(typeof(IDedupStoreHttpClient), typeof(DedupStoreHttpClient), dedupServiceUri);
        }
    }
}
