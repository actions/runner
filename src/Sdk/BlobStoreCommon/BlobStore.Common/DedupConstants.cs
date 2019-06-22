using System;

namespace GitHub.Services.BlobStore.Common
{
    public static class DedupConstants
    {
        public const string XpressCompressionHeaderString = "xpress";

        public static readonly TimeSpan MaximumKeepuntil = TimeSpan.FromDays(7);
        public static readonly TimeSpan DefaultClientKeepuntil = TimeSpan.FromDays(2);
    }
}
