using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Web;
using GitHub.Services.BlobStore.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    public static class ProxyUriHelper
    {
        public static Uri GetProxyDownloadUri(BlobIdentifier blobId, ConcurrentDictionary<BlobIdentifier, Uri> blobsToUris, Uri proxyUri, Uri blobServiceUri)
        {
            return ProxyUriHelper.GetProxyDownloadUri(blobId, blobsToUris[blobId], proxyUri, blobServiceUri);
        }

        public static Uri GetProxyDownloadUri(BlobIdentifier blobId, Uri sasUri, Uri proxyUri, Uri blobServiceUri)
        {
            var uriBuilder = new UriBuilder(proxyUri);
            uriBuilder.Path = ProxyConstants.BlobRelativePath;
            
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[ProxyRoutingConstants.BlobIdRoutePart] = blobId.ValueString;
            query[ProxyRoutingConstants.SasUriRoutePart] = sasUri.ToString();
            query[ProxyRoutingConstants.ServiceUriRoutePart] = blobServiceUri.ToString();
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }
    }
}
