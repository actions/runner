using System.Globalization;

namespace GitHub.Services.BlobStore.WebApi
{
    public static class BlobStoreResources
    {

        public static string BlobNotFoundException(object arg0)
        {
            const string Format = @"The blob with id '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ClientToolNoMatchingReleaseFound()
        {
            const string Format = @"No release could be found based on the provided information.";
            return Format;
        }

        public static string DedupInconsistentAttributeException(object arg0)
        {
            const string Format = @"Unable to retrieve consistent attributes from dedup with id '{0}'. Different results are returned by two fetching operations in tandem for this dedup. This could be a bug of the underlying platform. Retry is expected on the client side.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string DedupNotFoundException(object arg0)
        {
            const string Format = @"The dedup with id '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string EmptyDirectoryNotSupported()
        {
            const string Format = @"Publishing an empty directory is not currently supported.";
            return Format;
        }

        public static string InvalidPath()
        {
            const string Format = @"The path provided is invalid.";
            return Format;
        }

        public static string RemainingBytesError()
        {
            const string Format = @"Content was upload but bytes remained in upload stream.";
            return Format;
        }

        public static string UploadFailed()
        {
            const string Format = @"Content upload was not accepted by the server.";
            return Format;
        }
    }
}
