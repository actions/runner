namespace GitHub.Services.BlobStore.WebApi
{
    public static class ProxyRoutingConstants
    {
        /// <summary>
        /// Route value specified by the client to GET a single blob.
        /// </summary>
        public const string BlobIdRoutePart = "blobId";

        /// <summary>
        /// Route value specified by the client to GET a single SAS URI.
        /// </summary>
        public const string SasUriRoutePart = "sasUri";

        /// <summary>
        /// Route value specified by the client to GET a single Service URI.
        /// </summary>
        public const string ServiceUriRoutePart = "serviceUri";
    }
}
