using System;
using System.Web;

namespace GitHub.Services.BlobStore.Common
{
    public enum EdgeType
    {
        Unknown,
        NotEdge,
        AzureFrontDoor,
        BlobTestProxy
    }

    public struct PreauthenticatedUri
    {
        public readonly Uri NotNullUri;
        public readonly EdgeType EdgeType;

        public PreauthenticatedUri(Uri preauthenticatedNotNullUri, EdgeType edgeType)
        {
            if (preauthenticatedNotNullUri == null)
            {
                throw new ArgumentNullException(nameof(preauthenticatedNotNullUri));
            }

            NotNullUri = preauthenticatedNotNullUri;
            EdgeType = edgeType;
        }

        public static PreauthenticatedUri? FromPossiblyNullString(string possiblyNullUri, EdgeType edgeType)
        {
            if (possiblyNullUri == null)
            {
                return (PreauthenticatedUri?)null;
            }
            else
            {
                return new PreauthenticatedUri(new Uri(possiblyNullUri), edgeType);
            }
        }

        public static PreauthenticatedUri? FromPossiblyNullUri(Uri possiblyNullUri, EdgeType edgeType)
        {
            if (possiblyNullUri == null)
            {
                return (PreauthenticatedUri?)null;
            }
            else
            {
                return new PreauthenticatedUri(possiblyNullUri, edgeType);
            }
        }

        private DateTimeOffset GetExpiryTime()
        {
            return DateTimeOffset.Parse(HttpUtility.ParseQueryString(this.NotNullUri.Query).Get("se")).ToUniversalTime();
        }

        public DateTimeOffset ExpiryTime => GetExpiryTime();
    }

    public static class NullablePreauthenticatedUriExtensions
    {
        public static Uri ToPossiblyNullUri(this PreauthenticatedUri? nullable)
        {
            if (nullable.HasValue)
            {
                return nullable.Value.NotNullUri;
            }
            else
            {
                return null;
            }
        }

        public static string ToPossiblyNullAbsoluteUri(this PreauthenticatedUri? nullable)
        {
            if (nullable.HasValue)
            {
                return nullable.Value.NotNullUri.AbsoluteUri;
            }
            else
            {
                return null;
            }
        }
    }
}
