using System;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    public static class BlobStoreHttpClientFactory
    {
        public static IBlobStoreHttpClient GetClient(Uri blobServiceUri, ArtifactHttpClientFactory clientFactory)
        {
            return clientFactory.CreateVssHttpClient<IBlobStoreHttpClient, BlobStore2HttpClient>(blobServiceUri);
        }
    }
}
