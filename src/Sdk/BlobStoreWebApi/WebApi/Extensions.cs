using System;
using System.Linq;
using System.Net.Http.Headers;

namespace GitHub.Services.BlobStore.WebApi
{
    public static class Extensions
    {
        public static bool ContainsContentEncoding(this HttpContentHeaders @this, string contentEncoding)
        {
            return @this.ContentEncoding.Contains(contentEncoding, StringComparer.OrdinalIgnoreCase);
        }

        public static void SetContentEncoding(this HttpContentHeaders @this, string contentEncoding)
        {
            if (!@this.ContainsContentEncoding(contentEncoding))
            {
                @this.ContentEncoding.Add(contentEncoding);
            }
        }
    }
}
