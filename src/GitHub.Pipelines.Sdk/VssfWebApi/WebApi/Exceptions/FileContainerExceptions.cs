using System;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.FileContainer
{
    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    [ExceptionMapping("0.0", "3.0", "FileContainerException", "GitHub.Services.FileContainer.FileContainerException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class FileContainerException : VssServiceException
    {
        public FileContainerException()
        {
            EventId = VssEventId.FileContainerBaseEventId;
        }

        public FileContainerException(String message)
            : base(message)
        {
            EventId = VssEventId.FileContainerBaseEventId;
        }

        public FileContainerException(String message, Exception ex)
            : base(message, ex)
        {
            EventId = VssEventId.FileContainerBaseEventId;
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ArtifactUriNotSupportedException", "GitHub.Services.FileContainer.ArtifactUriNotSupportedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ArtifactUriNotSupportedException : FileContainerException
    {
        public ArtifactUriNotSupportedException(Uri artifactUri) :
            base(FileContainerResources.ArtifactUriNotSupportedException(artifactUri))
        {
        }

        public ArtifactUriNotSupportedException(String message) :
            base(message)
        {
        }

        public ArtifactUriNotSupportedException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerNotFoundException", "GitHub.Services.FileContainer.ContainerNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ContainerNotFoundException : FileContainerException
    {
        public ContainerNotFoundException() :
            base()
        {
        }

        public ContainerNotFoundException(Int64 containerId) :
            base(FileContainerResources.ContainerNotFoundException(containerId))
        {
        }

        public ContainerNotFoundException(String message) :
            base(message)
        {
        }

        public ContainerNotFoundException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemNotFoundException", "GitHub.Services.FileContainer.ContainerItemNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemNotFoundException : FileContainerException
    {
        public ContainerItemNotFoundException() :
            base()
        {
        }

        public ContainerItemNotFoundException(Int64 containerId, String path) :
            base(FileContainerResources.ContainerItemNotFoundException(path, containerId))
        {
        }

        public ContainerItemNotFoundException(ContainerItemType itemType, String existingPath)
            : base(FileContainerResources.ContainerItemDoesNotExist(existingPath, itemType))
        {
        }

        public ContainerItemNotFoundException(String message) :
            base(message)
        {
        }

        public ContainerItemNotFoundException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerWriteAccessDeniedException", "GitHub.Services.FileContainer.ContainerWriteAccessDeniedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ContainerWriteAccessDeniedException : FileContainerException
    {
        public ContainerWriteAccessDeniedException(String message) :
            base(message)
        {
        }

        public ContainerWriteAccessDeniedException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemExistsException", "GitHub.Services.FileContainer.ContainerItemExistsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemExistsException : FileContainerException
    {
        public ContainerItemExistsException(ContainerItemType itemType, String existingPath)
            : base(FileContainerResources.ContainerItemWithDifferentTypeExists(itemType, existingPath))
        {
        }

        public ContainerItemExistsException(String message) :
            base(message)
        {
        }

        public ContainerItemExistsException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemCopyTargetChildOfSourceException", "GitHub.Services.FileContainer.ContainerItemCopyTargetChildOfSourceException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemCopyTargetChildOfSourceException : FileContainerException
    {
        public ContainerItemCopyTargetChildOfSourceException(String targetPath, String sourcePath)
            : base(FileContainerResources.ContainerItemCopyTargetChildOfSource(targetPath, sourcePath))
        {
        }

        public ContainerItemCopyTargetChildOfSourceException(String message) :
            base(message)
        {
        }

        public ContainerItemCopyTargetChildOfSourceException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemCopySourcePendingUploadException", "GitHub.Services.FileContainer.ContainerItemCopySourcePendingUploadException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemCopySourcePendingUploadException : FileContainerException
    {
        public ContainerItemCopySourcePendingUploadException(String sourcePath)
            : base(FileContainerResources.ContainerItemCopySourcePendingUpload(sourcePath))
        {
        }

        public ContainerItemCopySourcePendingUploadException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemCopyDuplicateTargetsException", "GitHub.Services.FileContainer.ContainerItemCopyDuplicateTargetsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemCopyDuplicateTargetsException : FileContainerException
    {
        public ContainerItemCopyDuplicateTargetsException(String targetPath)
            : base(FileContainerResources.ContainerItemCopyDuplicateTargets(targetPath))
        {
        }

        public ContainerItemCopyDuplicateTargetsException(String message, Exception ex) :
            base(message, ex)
        {
        }
}

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "PendingUploadNotFoundException", "GitHub.Services.FileContainer.PendingUploadNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class PendingUploadNotFoundException : FileContainerException
    {
        public PendingUploadNotFoundException(Int32 uploadId) :
            base(FileContainerResources.PendingUploadNotFoundException(uploadId))
        {
        }

        public PendingUploadNotFoundException(String message) :
            base(message)
        {
        }

        public PendingUploadNotFoundException(String message, Exception ex) :
            base(message, ex)
        {
        }
}

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerAlreadyExistsException", "GitHub.Services.FileContainer.ContainerAlreadyExistsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerAlreadyExistsException : FileContainerException
    {
        public ContainerAlreadyExistsException(String artifactUri)
            : base(FileContainerResources.ContainerAlreadyExists(artifactUri))
        {
        }

        public ContainerAlreadyExistsException(String message, Exception ex) :
            base(message, ex)
        {
        }
}

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerUnexpectedContentTypeException", "GitHub.Services.FileContainer.ContainerUnexpectedContentTypeException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerUnexpectedContentTypeException : FileContainerException
    {
        public ContainerUnexpectedContentTypeException(String expectedContent, String actualContent)
            : base(FileContainerResources.UnexpectedContentType(expectedContent, actualContent))
        {
        }

        public ContainerUnexpectedContentTypeException(String message) :
            base(message)
        {
        }

        public ContainerUnexpectedContentTypeException(String message, Exception ex) :
            base(message, ex)
        {
        }
}

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerNoContentException", "GitHub.Services.FileContainer.ContainerNoContentException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerNoContentException : FileContainerException
    {
        public ContainerNoContentException()
            : base(FileContainerResources.NoContentReturned())
        {
        }

        public ContainerNoContentException(String message) :
            base(message)
        {
        }

        public ContainerNoContentException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemContentException", "GitHub.Services.FileContainer.ContainerItemContentException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemContentException : FileContainerException
    {
        public ContainerItemContentException()
            : base(FileContainerResources.NoContentReturned())
        {
        }

        public ContainerItemContentException(String message) :
            base(message)
        {
        }

        public ContainerItemContentException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }


    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerContentIdCollisionException", "GitHub.Services.FileContainer.ContainerContentIdCollisionException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerContentIdCollisionException : FileContainerException
    {
        public ContainerContentIdCollisionException(String fileId1, String length1, String fileId2, String length2)
            : base(FileContainerResources.ContentIdCollision(fileId1, length1, fileId2, length2))
        {
        }

        public ContainerContentIdCollisionException(String message) :
            base(message)
        {
        }

        public ContainerContentIdCollisionException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }


    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemCreateDuplicateItemException", "GitHub.Services.FileContainer.ContainerItemCreateDuplicateItemException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemCreateDuplicateItemException : FileContainerException
    {
        public ContainerItemCreateDuplicateItemException(String targetPath)
            : base(FileContainerResources.ContainerItemCopyDuplicateTargets(targetPath))
        {
        }
        public ContainerItemCreateDuplicateItemException(String message, Exception ex) :
            base(message, ex)
        {
        }
    }


    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerDeleteFailedException", "GitHub.Services.FileContainer.ContainerDeleteFailedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerDeleteFailedException : FileContainerException
    {
        public ContainerDeleteFailedException(String targetContainerPath)
            : base(targetContainerPath)
        {
        }
    }


    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ContainerItemUpdateFailedException", "GitHub.Services.FileContainer.ContainerItemUpdateFailedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContainerItemUpdateFailedException : FileContainerException
    {
        public ContainerItemUpdateFailedException(String targetPath)
            : base(targetPath)
        {
        }
    }
}
