using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class FileContainerResources
    {
        public static string ArtifactUriNotSupportedException(params object[] args)
        {
            const string Format = @"The artifact Uri {0} is not supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerNotFoundException(params object[] args)
        {
            const string Format = @"The container {0} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerItemNotFoundException(params object[] args)
        {
            const string Format = @"The item {0} in container {1} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerItemWithDifferentTypeExists(params object[] args)
        {
            const string Format = @"The items could not be created because an item with type {0} already exists at {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PendingUploadNotFoundException(params object[] args)
        {
            const string Format = @"The pending upload {0} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerItemDoesNotExist(params object[] args)
        {
            const string Format = @"The item {0} of type {1} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerItemCopySourcePendingUpload(params object[] args)
        {
            const string Format = @"The source item {0} is in the pending upload state.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerItemCopyTargetChildOfSource(params object[] args)
        {
            const string Format = @"The target folder {0} of the copy operation is a child of the source folder {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerItemCopyDuplicateTargets(params object[] args)
        {
            const string Format = @"The target location {0} is specified for two or more sources.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerAlreadyExists(params object[] args)
        {
            const string Format = @"Container with artifact {0} already exists.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnexpectedContentType(params object[] args)
        {
            const string Format = @"Requested content type {0} but got back content type {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NoContentReturned(params object[] args)
        {
            const string Format = @"The request returned no content.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GzipNotSupportedOnServer(params object[] args)
        {
            const string Format = @"The server does not support gzipped content.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string BadCompression(params object[] args)
        {
            const string Format = @"The file length passed in is less than or equal to the compressed stream length.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ChunksizeWrongWithContentId(params object[] args)
        {
            const string Format = @"The chunk size must be a multiple of {0} bytes when specifying a contentId.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContentIdCollision(params object[] args)
        {
            const string Format = @"There was a contentId collision for file {0} with length {1} and file {2} with length {3}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
