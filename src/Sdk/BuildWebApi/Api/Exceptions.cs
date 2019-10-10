using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AccessDeniedException", "GitHub.Build.WebApi.AccessDeniedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccessDeniedException : VssServiceException
    {
        public AccessDeniedException(String message)
            : base(message)
        {
        }
        public AccessDeniedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected AccessDeniedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildException", "GitHub.Build.WebApi.BuildException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildException : VssServiceException
    {
        public BuildException(String message)
            : base(message)
        {
        }

        public BuildException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AgentsNotFoundException", "GitHub.Build.WebApi.AgentsNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AgentsNotFoundException : BuildException
    {
        public AgentsNotFoundException(String message)
            : base(message)
        {
        }

        public AgentsNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected AgentsNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ArtifactExistsException", "GitHub.Build.WebApi.ArtifactExistsException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ArtifactExistsException : BuildException
    {
        public ArtifactExistsException(String message)
            : base(message)
        {
        }

        public ArtifactExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ArtifactExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ArtifactNotFoundException", "GitHub.Build.WebApi.ArtifactNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ArtifactNotFoundException : BuildException
    {
        public ArtifactNotFoundException(String message)
            : base(message)
        {
        }

        public ArtifactNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ArtifactNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ArtifactTypeNotSupportedException", "GitHub.Build.WebApi.ArtifactTypeNotSupportedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ArtifactTypeNotSupportedException : BuildException
    {
        public ArtifactTypeNotSupportedException(String message)
            : base(message)
        {
        }

        public ArtifactTypeNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ArtifactTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BranchNotFoundException", "GitHub.Build.WebApi.BranchNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BranchNotFoundException : BuildException
    {
        public BranchNotFoundException(String message)
            : base(message)
        {
        }

        public BranchNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BranchNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildControllerNotFoundException", "GitHub.Build.WebApi.BuildControllerNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildControllerNotFoundException : BuildException
    {
        public BuildControllerNotFoundException(String message)
            : base(message)
        {
        }

        public BuildControllerNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildControllerNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class BuildEventNotFoundException : BuildException
    {
        public BuildEventNotFoundException(String message)
            : base(message)
        {
        }

        public BuildEventNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildEventNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class BuildEventPermissionException : BuildException
    {
        public BuildEventPermissionException(String message)
            : base(message)
        {
        }

        public BuildEventPermissionException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildEventPermissionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildExistsException", "GitHub.Build.WebApi.BuildExistsException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildExistsException : BuildException
    {
        public BuildExistsException(String message)
            : base(message)
        {
        }

        public BuildExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildLogNotFoundException", "GitHub.Build.WebApi.BuildLogNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildLogNotFoundException : BuildException
    {
        public BuildLogNotFoundException(String message)
            : base(message)
        {
        }

        public BuildLogNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildLogNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildNotFoundException", "GitHub.Build.WebApi.BuildNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildNotFoundException : BuildException
    {
        public BuildNotFoundException(String message)
            : base(message)
        {
        }

        public BuildNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildNumberFormatException", "GitHub.Build.WebApi.BuildNumberFormatException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildNumberFormatException : BuildException
    {
        public BuildNumberFormatException(String message)
            : base(message)
        {
        }

        public BuildNumberFormatException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildNumberFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildOptionNotSupportedException", "GitHub.Build.WebApi.BuildOptionNotSupportedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildOptionNotSupportedException : BuildException
    {
        public BuildOptionNotSupportedException(String message)
            : base(message)
        {
        }

        public BuildOptionNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildOptionNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildRepositoryTypeNotSupportedException", "GitHub.Build.WebApi.BuildRepositoryTypeNotSupportedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildRepositoryTypeNotSupportedException : BuildException
    {
        public BuildRepositoryTypeNotSupportedException(String message)
            : base(message)
        {
        }

        public BuildRepositoryTypeNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildRepositoryTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildRequestValidationFailedException", "GitHub.Build.WebApi.BuildRequestValidationFailedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildRequestValidationFailedException : BuildException
    {
        public BuildRequestValidationFailedException(String message)
            : base(message)
        {
        }

        public BuildRequestValidationFailedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public BuildRequestValidationFailedException(String message, List<BuildRequestValidationResult> validationResults)
            : base(message)
        {
            ValidationResults.AddRange(validationResults);
        }

        protected BuildRequestValidationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            m_validationResults = (List<BuildRequestValidationResult>)info.GetValue("ValidationResults", typeof(List<BuildRequestValidationResult>));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ValidationResults", ValidationResults);
        }

        [DataMember(Name = "ValidationResults", EmitDefaultValue = false)]
        public List<BuildRequestValidationResult> ValidationResults
        {
            get
            {
                if (m_validationResults == null)
                {
                    m_validationResults = new List<BuildRequestValidationResult>();
                }
                return m_validationResults;
            }
            private set
            {
                m_validationResults = value;
            }
        }

        private List<BuildRequestValidationResult> m_validationResults;
    }

    [Serializable]
    [ExceptionMapping("0.0", "4.1", "BuildRequestValidationFailedException", "GitHub.Build.WebApi.BuildRequestValidationFailedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildRequestValidationWarningException : BuildException
    {
        public BuildRequestValidationWarningException(String message)
            : base(message)
        {
        }

        public BuildRequestValidationWarningException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public BuildRequestValidationWarningException(String message, List<BuildRequestValidationResult> validationResults)
            : base(message)
        {
            ValidationResults.AddRange(validationResults);
        }

        protected BuildRequestValidationWarningException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            m_validationResults = (List<BuildRequestValidationResult>)info.GetValue("ValidationResults", typeof(List<BuildRequestValidationResult>));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ValidationResults", ValidationResults);
        }

        [DataMember(Name = "ValidationResults", EmitDefaultValue = false)]
        public List<BuildRequestValidationResult> ValidationResults
        {
            get
            {
                if (m_validationResults == null)
                {
                    m_validationResults = new List<BuildRequestValidationResult>();
                }
                return m_validationResults;
            }
            private set
            {
                m_validationResults = value;
            }
        }

        private List<BuildRequestValidationResult> m_validationResults;
    }

    [Serializable]
    public class BuildEventStatusInvalidChangeException : BuildException
    {
        public BuildEventStatusInvalidChangeException(String message)
            : base(message)
        {
        }

        public BuildEventStatusInvalidChangeException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildEventStatusInvalidChangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BuildStatusInvalidChangeException", "GitHub.Build.WebApi.BuildStatusInvalidChangeException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BuildStatusInvalidChangeException : BuildException
    {
        public BuildStatusInvalidChangeException(String message)
            : base(message)
        {
        }

        public BuildStatusInvalidChangeException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected BuildStatusInvalidChangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CannotDeleteRetainedBuildException", "GitHub.Build.WebApi.CannotDeleteRetainedBuildException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CannotDeleteRetainedBuildException : BuildException
    {
        public CannotDeleteRetainedBuildException(String message)
            : base(message)
        {
        }
        
        public CannotDeleteRetainedBuildException(String message, IReadOnlyList<Int32> buildIds)
            : base(message)
        {
            RetainedBuildIds = buildIds;
        }

        public CannotDeleteRetainedBuildException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected CannotDeleteRetainedBuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        [DataMember(EmitDefaultValue = false)]
        public IReadOnlyList<Int32> RetainedBuildIds { get; }
    }

    [Serializable]
    public class CouldNotRetrieveSourceVersionDisplayUrlException : BuildException
    {
        public CouldNotRetrieveSourceVersionDisplayUrlException(String message)
            : base(message)
        {
        }

        public CouldNotRetrieveSourceVersionDisplayUrlException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected CouldNotRetrieveSourceVersionDisplayUrlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionDisabledException", "GitHub.Build.WebApi.DefinitionDisabledException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionDisabledException : BuildException
    {
        public DefinitionDisabledException(String message)
            : base(message)
        {
        }

        public DefinitionDisabledException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionDisabledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionExistsException", "GitHub.Build.WebApi.DefinitionExistsException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionExistsException : BuildException
    {
        public DefinitionExistsException(String message)
            : base(message)
        {
        }

        public DefinitionExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class FolderExistsException : BuildException
    {
        public FolderExistsException(String message)
            : base(message)
        {
        }

        public FolderExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected FolderExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class FolderNotFoundException : BuildException
    {
        public FolderNotFoundException(String message)
            : base(message)
        {
        }

        public FolderNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected FolderNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionNotFoundException", "GitHub.Build.WebApi.DefinitionNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionNotFoundException : BuildException
    {
        public DefinitionNotFoundException(String message)
            : base(message)
        {
        }

        public DefinitionNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionNotMatchedException", "GitHub.Build.WebApi.DefinitionNotMatchedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionNotMatchedException : BuildException
    {
        public DefinitionNotMatchedException(String message)
            : base(message)
        {
        }

        public DefinitionNotMatchedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionNotMatchedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionTemplateExistsException", "GitHub.Build.WebApi.DefinitionTemplateExistsException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionTemplateExistsException : BuildException
    {
        public DefinitionTemplateExistsException(String message)
            : base(message)
        {
        }

        public DefinitionTemplateExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionTemplateExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionTemplateNotFoundException", "GitHub.Build.WebApi.DefinitionTemplateNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionTemplateNotFoundException : BuildException
    {
        public DefinitionTemplateNotFoundException(String message)
            : base(message)
        {
        }

        public DefinitionTemplateNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionTemplateNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionTypeNotSupportedException", "GitHub.Build.WebApi.DefinitionTypeNotSupportedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionTypeNotSupportedException : BuildException
    {
        public DefinitionTypeNotSupportedException(String message)
            : base(message)
        {
        }

        public DefinitionTypeNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DuplicateBuildSpecException", "GitHub.Build.WebApi.DuplicateBuildSpecException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DuplicateBuildSpecException : BuildException
    {
        public DuplicateBuildSpecException(String message)
            : base(message)
        {
        }

        public DuplicateBuildSpecException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DuplicateBuildSpecException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class EndpointAccessDeniedException : BuildException
    {
        public EndpointAccessDeniedException(String message)
            : base(message)
        {
        }

        public EndpointAccessDeniedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected EndpointAccessDeniedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ExternalSourceProviderException : BuildException
    {
        public ExternalSourceProviderException(String message)
            : base(message)
        {
        }

        public ExternalSourceProviderException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ExternalSourceProviderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class SecureFileAccessDeniedException : BuildException
    {
        public SecureFileAccessDeniedException(String message)
            : base(message)
        {
        }

        public SecureFileAccessDeniedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected SecureFileAccessDeniedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidArtifactDataException", "GitHub.Build.WebApi.InvalidArtifactDataException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidArtifactDataException : BuildException
    {
        public InvalidArtifactDataException(String message)
            : base(message)
        {
        }

        public InvalidArtifactDataException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidArtifactDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidBuildException", "GitHub.Build.WebApi.InvalidBuildException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidBuildException : BuildException
    {
        public InvalidBuildException(String message)
            : base(message)
        {
        }

        public InvalidBuildException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidBuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidBuildQueryException", "GitHub.Build.WebApi.InvalidBuildQueryException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidBuildQueryException : BuildException
    {
        public InvalidBuildQueryException(String message)
            : base(message)
        {
        }

        public InvalidBuildQueryException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidBuildQueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidContinuationTokenException : BuildException
    {
        public InvalidContinuationTokenException(String message)
            : base(message)
        {
        }

        public InvalidContinuationTokenException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidContinuationTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidDefinitionException : BuildException
    {
        public InvalidDefinitionException(String message)
            : base(message)
        {
        }

        public InvalidDefinitionException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidDefinitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CannotDeleteDefinitionWithRetainedBuildsException : BuildException
    {
        public CannotDeleteDefinitionWithRetainedBuildsException(String message)
            : base(message)
        {
        }

        public CannotDeleteDefinitionWithRetainedBuildsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected CannotDeleteDefinitionWithRetainedBuildsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CannotRestoreDeletedDraftWithoutRestoringParentException : BuildException
    {
        public CannotRestoreDeletedDraftWithoutRestoringParentException(String message)
            : base(message)
        {
        }

        public CannotRestoreDeletedDraftWithoutRestoringParentException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected CannotRestoreDeletedDraftWithoutRestoringParentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidFolderException : BuildException
    {
        public InvalidFolderException(String message)
            : base(message)
        {
        }

        public InvalidFolderException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidFolderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidDefinitionQueryException", "GitHub.Build.WebApi.InvalidDefinitionQueryException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidDefinitionQueryException : BuildException
    {
        public InvalidDefinitionQueryException(String message)
            : base(message)
        {
        }

        public InvalidDefinitionQueryException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidDefinitionQueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidFolderQueryException : BuildException
    {
        public InvalidFolderQueryException(String message)
            : base(message)
        {
        }

        public InvalidFolderQueryException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidFolderQueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidEndpointUrlException : BuildException
    {
        public InvalidEndpointUrlException(String message)
            : base(message)
        {
        }

        public InvalidEndpointUrlException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidEndpointUrlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidLogLocationException", "GitHub.Build.WebApi.InvalidLogLocationException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidLogLocationException : BuildException
    {
        public InvalidLogLocationException(String message)
            : base(message)
        {
        }

        public InvalidLogLocationException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidLogLocationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidProjectException", "GitHub.Build.WebApi.InvalidProjectException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidProjectException : BuildException
    {
        public InvalidProjectException(String message)
            : base(message)
        {
        }

        public InvalidProjectException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidProjectException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidSourceLabelFormatException", "GitHub.Build.WebApi.InvalidSourceLabelFormatException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidSourceLabelFormatException : BuildException
    {
        public InvalidSourceLabelFormatException(String message)
            : base(message)
        {
        }

        public InvalidSourceLabelFormatException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidSourceLabelFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidTemplateException ", "GitHub.Build.WebApi.InvalidTemplateException , GitHub.Build.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class InvalidTemplateException : BuildException
    {
        public InvalidTemplateException(String message)
            : base(message)
        {
        }

        public InvalidTemplateException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidTemplateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "MetaTaskDefinitionMissingException", "GitHub.Build.WebApi.MetaTaskDefinitionMissingException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class MetaTaskDefinitionMissingException : BuildException
    {
        public MetaTaskDefinitionMissingException(String message)
            : base(message)
        {
        }

        public MetaTaskDefinitionMissingException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected MetaTaskDefinitionMissingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MissingEndpointInformationException : BuildException
    {
        public MissingEndpointInformationException(String message)
            : base(message)
        {
        }

        public MissingEndpointInformationException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected MissingEndpointInformationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MissingRepositoryException : BuildException
    {
        public MissingRepositoryException(String message)
            : base(message)
        {
        }

        public MissingRepositoryException(String message, Exception ex)
            : base(message, ex)
        {
        }
        protected MissingRepositoryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MissingTasksForDefinition : BuildException
    {
        public MissingTasksForDefinition(String message)
            : base(message)
        {
        }

        public MissingTasksForDefinition(String message, Exception ex)
            : base(message, ex)
        {
        }
        protected MissingTasksForDefinition(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "NotSupportedOnXamlBuildException", "GitHub.Build.WebApi.NotSupportedOnXamlBuildException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class NotSupportedOnXamlBuildException : BuildException
    {
        public NotSupportedOnXamlBuildException(String message)
            : base(message)
        {
        }

        public NotSupportedOnXamlBuildException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected NotSupportedOnXamlBuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class OrchestrationTypeNotSupportedException : BuildException
    {
        public OrchestrationTypeNotSupportedException(String message)
            : base(message)
        {
        }

        public OrchestrationTypeNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }
        protected OrchestrationTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProcessTemplateDeletedException", "GitHub.Build.WebApi.ProcessTemplateDeletedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProcessTemplateDeletedException : BuildException
    {
        public ProcessTemplateDeletedException(String message)
            : base(message)
        {
        }

        public ProcessTemplateDeletedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ProcessTemplateDeletedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProcessTemplateNotFoundException", "GitHub.Build.WebApi.ProcessTemplateNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProcessTemplateNotFoundException : BuildException
    {
        public ProcessTemplateNotFoundException(String message)
            : base(message)
        {
        }

        public ProcessTemplateNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ProcessTemplateNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProjectConflictException", "GitHub.Build.WebApi.ProjectConflictException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProjectConflictException : BuildException
    {
        public ProjectConflictException(String message)
            : base(message)
        {
        }

        public ProjectConflictException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ProjectConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "QueueExistsException", "GitHub.Build.WebApi.QueueExistsException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class QueueExistsException : BuildException
    {
        public QueueExistsException(String message)
            : base(message)
        {
        }

        public QueueExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected QueueExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "QueueNotFoundException", "GitHub.Build.WebApi.QueueNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class QueueNotFoundException : BuildException
    {
        public QueueNotFoundException(String message)
            : base(message)
        {
        }

        public QueueNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected QueueNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "RecreatingSubscriptionFailedException", "GitHub.Build.WebApi.RecreatingSubscriptionFailedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RecreatingSubscriptionFailedException : BuildException
    {
        public RecreatingSubscriptionFailedException(String message)
            : base(message)
        {
        }

        public RecreatingSubscriptionFailedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected RecreatingSubscriptionFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ReportFormatTypeNotSupportedException", "GitHub.Build.WebApi.ReportFormatTypeNotSupportedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ReportFormatTypeNotSupportedException : BuildException
    {
        public ReportFormatTypeNotSupportedException(String message)
            : base(message)
        {
        }

        public ReportFormatTypeNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ReportFormatTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ReportStreamNotSupportedException", "GitHub.Build.WebApi.ReportStreamNotSupportedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ReportStreamNotSupportedException : BuildException
    {
        public ReportStreamNotSupportedException(String message)
            : base(message)
        {
        }

        public ReportStreamNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ReportStreamNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "RepositoryInformationInvalidException", "GitHub.Build.WebApi.RepositoryInformationInvalidException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RepositoryInformationInvalidException : BuildException
    {
        public RepositoryInformationInvalidException(String message)
            : base(message)
        {
        }

        public RepositoryInformationInvalidException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected RepositoryInformationInvalidException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class RequestContentException : BuildException
    {
        public RequestContentException(String message)
            : base(message)
        {
        }

        public RequestContentException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected RequestContentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "RouteIdConflictException", "GitHub.Build.WebApi.RouteIdConflictException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RouteIdConflictException : BuildException
    {
        public RouteIdConflictException(String message)
            : base(message)
        {
        }

        public RouteIdConflictException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected RouteIdConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TimelineNotFoundException", "GitHub.Build.WebApi.TimelineNotFoundException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TimelineNotFoundException : BuildException
    {
        public TimelineNotFoundException(String message)
            : base(message)
        {
        }

        public TimelineNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected TimelineNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "VariableNameIsReservedException", "GitHub.Build.WebApi.VariableNameIsReservedException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VariableNameIsReservedException : BuildException
    {
        public VariableNameIsReservedException(String message)
            : base(message)
        {
        }

        public VariableNameIsReservedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected VariableNameIsReservedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class VariableGroupsAccessDeniedException : BuildException
    {
        public VariableGroupsAccessDeniedException(String message)
            : base(message)
        {
        }

        public VariableGroupsAccessDeniedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected VariableGroupsAccessDeniedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MetricAggregationTypeNotSupportedException : BuildException
    {
        public MetricAggregationTypeNotSupportedException(String message)
            : base(message)
        {
        }

        public MetricAggregationTypeNotSupportedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected MetricAggregationTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DefinitionTriggerAlreadyExistsException", "GitHub.Build.WebApi.DefinitionTriggerAlreadyExistsException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DefinitionTriggerAlreadyExistsException : BuildException
    {
        public DefinitionTriggerAlreadyExistsException(String message)
            : base(message)
        {
        }

        public DefinitionTriggerAlreadyExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DefinitionTriggerAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidDefinitionInTriggerSourceException", "GitHub.Build.WebApi.InvalidDefinitionInTriggerSourceException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidDefinitionInTriggerSourceException : BuildException
    {
        public InvalidDefinitionInTriggerSourceException(String message)
            : base(message)
        {
        }

        public InvalidDefinitionInTriggerSourceException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidDefinitionInTriggerSourceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CycleDetectedInProvidedBuildCompletionTriggersException", "GitHub.Build.WebApi.CycleDetectedInProvidedBuildCompletionTriggersException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CycleDetectedInProvidedBuildCompletionTriggersException : BuildException
    {
        public CycleDetectedInProvidedBuildCompletionTriggersException(String message)
            : base(message)
        {
        }

        public CycleDetectedInProvidedBuildCompletionTriggersException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected CycleDetectedInProvidedBuildCompletionTriggersException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


    [Serializable]
    [ExceptionMapping("0.0", "3.0", "UnsupportedBuildCompletionTriggerChainException", "GitHub.Build.WebApi.UnsupportedBuildCompletionTriggerChainException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class UnsupportedBuildCompletionTriggerChainException : BuildException
    {
        public UnsupportedBuildCompletionTriggerChainException(String message)
            : base(message)
        {
        }

        public UnsupportedBuildCompletionTriggerChainException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected UnsupportedBuildCompletionTriggerChainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CannotUpdateTriggeredByBuildException", "GitHub.Build.WebApi.CannotUpdateTriggeredByBuildException, GitHub.Build2.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CannotUpdateTriggeredByBuildException : BuildException
    {
        public CannotUpdateTriggeredByBuildException(String message)
            : base(message)
        {
        }

        public CannotUpdateTriggeredByBuildException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected CannotUpdateTriggeredByBuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidGitVersionSpec : BuildException
    {
        public InvalidGitVersionSpec(String message)
            : base(message)
        {
        }

        public InvalidGitVersionSpec(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidGitVersionSpec(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class AmbiguousDefinitionNameException : BuildException
    {
        public AmbiguousDefinitionNameException(String message)
            : base(message)
        {
        }

        public AmbiguousDefinitionNameException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public AmbiguousDefinitionNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class DataProviderException : BuildException
    {
        public DataProviderException(String message)
            : base(message)
        {
        }

        public DataProviderException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DataProviderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
