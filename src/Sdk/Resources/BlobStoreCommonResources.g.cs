using System.Globalization;

namespace GitHub.Services.BlobStore.Common
{
    public static class BlobStoreCommonResources
    {

        public static string InvalidContentHashValue(object arg0)
        {
            const string Format = @"Invalid hash value.  The string value provided {0} is not a valid content identifier/hash value.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidFinalBlockContentLength()
        {
            const string Format = @"Invalid or unexpected content length.  The content length is greater than the block size defined.  The length of any content that is final block content must be less than the block size.";
            return Format;
        }

        public static string InvalidHashLength(object arg0)
        {
            const string Format = @"Input hash {0} must be at least six characters long.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidPartialBlock()
        {
            const string Format = @"Partial final block encountered in identifier computation.";
            return Format;
        }

        public static string InvalidPartialContentBlockLength()
        {
            const string Format = @"Invalid or unexpected content length.  The content length does not equal the block size defined.  The length of any content that is not final block content must match the block size.";
            return Format;
        }

        public static string SymLinkExceptionMessage()
        {
            const string Format = @"Unable to load symbolic/hard linked file.  Check 'fsutil behavior query SymlinkEvaluation' to ensure proper behavior.";
            return Format;
        }
    }
}
