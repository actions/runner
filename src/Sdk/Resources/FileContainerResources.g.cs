using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class FileContainerResources
    {
        public static string ArtifactUriNotSupportedException(object arg0)
        {
            const string Format = @"The artifact Uri {0} is not supported.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerNotFoundException(object arg0)
        {
            const string Format = @"The container {0} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerItemNotFoundException(object arg0, object arg1)
        {
            const string Format = @"The item {0} in container {1} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ContainerItemWithDifferentTypeExists(object arg0, object arg1)
        {
            const string Format = @"The items could not be created because an item with type {0} already exists at {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PendingUploadNotFoundException(object arg0)
        {
            const string Format = @"The pending upload {0} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerItemDoesNotExist(object arg0, object arg1)
        {
            const string Format = @"The item {0} of type {1} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ContainerItemCopySourcePendingUpload(object arg0)
        {
            const string Format = @"The source item {0} is in the pending upload state.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerItemCopyTargetChildOfSource(object arg0, object arg1)
        {
            const string Format = @"The target folder {0} of the copy operation is a child of the source folder {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ContainerItemCopyDuplicateTargets(object arg0)
        {
            const string Format = @"The target location {0} is specified for two or more sources.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerAlreadyExists(object arg0)
        {
            const string Format = @"Container with artifact {0} already exists.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UnexpectedContentType(object arg0, object arg1)
        {
            const string Format = @"Requested content type {0} but got back content type {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string NoContentReturned()
        {
            const string Format = @"The request returned no content.";
            return Format;
        }

        public static string GzipNotSupportedOnServer()
        {
            const string Format = @"The server does not support gzipped content.";
            return Format;
        }

        public static string BadCompression()
        {
            const string Format = @"The file length passed in is less than or equal to the compressed stream length.";
            return Format;
        }

        public static string ChunksizeWrongWithContentId(object arg0)
        {
            const string Format = @"The chunk size must be a multiple of {0} bytes when specifying a contentId.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContentIdCollision(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"There was a contentId collision for file {0} with length {1} and file {2} with length {3}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }
    }
}
