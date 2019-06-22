using System;

namespace GitHub.Services.BlobStore.Common
{
    public enum EdgeCache
    {
        NotAllowed = 0x0,
        Allowed = 0x1,
    }

    public static class EdgeCacheHelper
    {
        public static EdgeCache GetEdgeCacheEnvVar(string envVarPrefix)
        {
            string edgeCacheEnvVar = Environment.GetEnvironmentVariable($"{envVarPrefix}AllowEdge");
            if (string.IsNullOrWhiteSpace(edgeCacheEnvVar))
            {
                return EdgeCache.Allowed;
            }
            else
            {
                bool success = int.TryParse(edgeCacheEnvVar, out int result);
                if (success)
                {
                    return result == 1 ? EdgeCache.Allowed : EdgeCache.NotAllowed;
                }
                else
                {
                    return EdgeCache.NotAllowed;
                }
            }
        }
    }

    public struct BlobIdWithHeaders
    {
        public readonly BlobIdentifier BlobId;
        public readonly string FileName;
        public readonly string ContentType;
        public readonly EdgeCache EdgeCache;
        public readonly DateTimeOffset? ExpiryTime;

        public BlobIdWithHeaders(BlobIdentifier blobId, EdgeCache edgeCache, string filename = null, string contentType = null, DateTimeOffset? expiryTime = null)
        {
            this.BlobId = blobId;
            this.FileName = filename;
            this.ContentType = contentType;
            this.EdgeCache = edgeCache;
            this.ExpiryTime = expiryTime;
        }
    }
}
